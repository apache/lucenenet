using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Lucene.Net.Support;

namespace Lucene.Net.Codecs.Lucene3x
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    using BinaryDocValues = Lucene.Net.Index.BinaryDocValues;
    using IBits = Lucene.Net.Util.IBits;
    using Directory = Lucene.Net.Store.Directory;
    using FieldInfo = Lucene.Net.Index.FieldInfo;
    using FieldInfos = Lucene.Net.Index.FieldInfos;
    using IndexFileNames = Lucene.Net.Index.IndexFileNames;
    using IndexInput = Lucene.Net.Store.IndexInput;
    using IOContext = Lucene.Net.Store.IOContext;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using NumericDocValues = Lucene.Net.Index.NumericDocValues;
    using RamUsageEstimator = Lucene.Net.Util.RamUsageEstimator;
    using SegmentInfo = Lucene.Net.Index.SegmentInfo;
    using SortedDocValues = Lucene.Net.Index.SortedDocValues;
    using SortedSetDocValues = Lucene.Net.Index.SortedSetDocValues;
    using StringHelper = Lucene.Net.Util.StringHelper;

    /// <summary>
    /// Reads Lucene 3.x norms format and exposes it via DocValues API
    /// @lucene.experimental </summary>
    /// @deprecated Only for reading existing 3.x indexes
    [Obsolete("Only for reading existing 3.x indexes")]
    internal class Lucene3xNormsProducer : DocValuesProducer
    {
        /// <summary>
        /// norms header placeholder </summary>
        internal static readonly sbyte[] NORMS_HEADER = { (sbyte)'N', (sbyte)'R', (sbyte)'M', -1 };

        /// <summary>
        /// Extension of norms file </summary>
        internal const string NORMS_EXTENSION = "nrm";

        /// <summary>
        /// Extension of separate norms file </summary>
        internal const string SEPARATE_NORMS_EXTENSION = "s";

        private readonly IDictionary<string, NormsDocValues> norms = new Dictionary<string, NormsDocValues>();

        // any .nrm or .sNN files we have open at any time.
        // TODO: just a list, and double-close() separate norms files?
        internal readonly ISet<IndexInput> openFiles = new IdentityHashSet<IndexInput>();

        // points to a singleNormFile
        internal IndexInput singleNormStream;

        internal readonly int maxdoc;

        private readonly AtomicLong ramBytesUsed;

        // note: just like segmentreader in 3.x, we open up all the files here (including separate norms) up front.
        // but we just don't do any seeks or reading yet.
        public Lucene3xNormsProducer(Directory dir, SegmentInfo info, FieldInfos fields, IOContext context)
        {
            Directory separateNormsDir = info.Dir; // separate norms are never inside CFS
            maxdoc = info.DocCount;
            string segmentName = info.Name;
            bool success = false;
            try
            {
                long nextNormSeek = NORMS_HEADER.Length; //skip header (header unused for now)
                foreach (FieldInfo fi in fields)
                {
                    if (fi.HasNorms)
                    {
                        string fileName = GetNormFilename(info, fi.Number);
                        Directory d = HasSeparateNorms(info, fi.Number) ? separateNormsDir : dir;

                        // singleNormFile means multiple norms share this file
                        bool singleNormFile = IndexFileNames.MatchesExtension(fileName, NORMS_EXTENSION);
                        IndexInput normInput = null;
                        long normSeek;

                        if (singleNormFile)
                        {
                            normSeek = nextNormSeek;
                            if (singleNormStream == null)
                            {
                                singleNormStream = d.OpenInput(fileName, context);
                                openFiles.Add(singleNormStream);
                            }
                            // All norms in the .nrm file can share a single IndexInput since
                            // they are only used in a synchronized context.
                            // If this were to change in the future, a clone could be done here.
                            normInput = singleNormStream;
                        }
                        else
                        {
                            normInput = d.OpenInput(fileName, context);
                            openFiles.Add(normInput);
                            // if the segment was created in 3.2 or after, we wrote the header for sure,
                            // and don't need to do the sketchy file size check. otherwise, we check
                            // if the size is exactly equal to maxDoc to detect a headerless file.
                            // NOTE: remove this check in Lucene 5.0!
                            string version = info.Version;
                            bool isUnversioned = (version == null || StringHelper.VersionComparator.Compare(version, "3.2") < 0) && normInput.Length == maxdoc;
                            if (isUnversioned)
                            {
                                normSeek = 0;
                            }
                            else
                            {
                                normSeek = NORMS_HEADER.Length;
                            }
                        }
                        NormsDocValues norm = new NormsDocValues(this, normInput, normSeek);
                        norms[fi.Name] = norm;
                        nextNormSeek += maxdoc; // increment also if some norms are separate
                    }
                }
                // TODO: change to a real check? see LUCENE-3619
                Debug.Assert(singleNormStream == null || nextNormSeek == singleNormStream.Length, singleNormStream != null ? "len: " + singleNormStream.Length + " expected: " + nextNormSeek : "null");
                success = true;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.CloseWhileHandlingException(openFiles);
                }
            }
            ramBytesUsed = new AtomicLong();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    IOUtils.Close(openFiles.ToArray());
                }
                finally
                {
                    norms.Clear();
                    openFiles.Clear();
                }
            }
        }

        private static string GetNormFilename(SegmentInfo info, int number)
        {
            if (HasSeparateNorms(info, number))
            {
                long gen = Convert.ToInt64(info.GetAttribute(Lucene3xSegmentInfoFormat.NORMGEN_PREFIX + number));
                return IndexFileNames.FileNameFromGeneration(info.Name, SEPARATE_NORMS_EXTENSION + number, gen);
            }
            else
            {
                // single file for all norms
                return IndexFileNames.SegmentFileName(info.Name, "", NORMS_EXTENSION);
            }
        }

        private static bool HasSeparateNorms(SegmentInfo info, int number)
        {
            string v = info.GetAttribute(Lucene3xSegmentInfoFormat.NORMGEN_PREFIX + number);
            if (v == null)
            {
                return false;
            }
            else
            {
                Debug.Assert(Convert.ToInt64(v) != SegmentInfo.NO);
                return true;
            }
        }

        // holds a file+offset pointing to a norms, and lazy-loads it
        // to a singleton NumericDocValues instance
        private sealed class NormsDocValues
        {
            private readonly Lucene3xNormsProducer outerInstance;

            private readonly IndexInput file;
            private readonly long offset;
            private NumericDocValues instance;

            public NormsDocValues(Lucene3xNormsProducer outerInstance, IndexInput normInput, long normSeek)
            {
                this.outerInstance = outerInstance;
                this.file = normInput;
                this.offset = normSeek;
            }

            internal NumericDocValues Instance
            {
                get
                {
                    lock (this)
                    {
                        if (instance == null)
                        {
                            var bytes = new byte[outerInstance.maxdoc];
                            // some norms share fds
                            lock (file)
                            {
                                file.Seek(offset);
                                file.ReadBytes(bytes, 0, bytes.Length, false);
                            }
                            // we are done with this file
                            if (file != outerInstance.singleNormStream)
                            {
                                outerInstance.openFiles.Remove(file);
                                file.Dispose();
                            }
                            outerInstance.ramBytesUsed.AddAndGet(RamUsageEstimator.SizeOf(bytes));
                            instance = new NumericDocValuesAnonymousInnerClassHelper(this, bytes);
                        }
                        return instance;
                    }
                }
            }

            private class NumericDocValuesAnonymousInnerClassHelper : NumericDocValues
            {
                private readonly byte[] bytes;

                public NumericDocValuesAnonymousInnerClassHelper(NormsDocValues outerInstance, byte[] bytes)
                {
                    this.bytes = bytes;
                }

                public override long Get(int docID)
                {
                    return bytes[docID];
                }
            }
        }

        public override NumericDocValues GetNumeric(FieldInfo field)
        {
            var dv = norms[field.Name];
            Debug.Assert(dv != null);
            return dv.Instance;
        }

        public override BinaryDocValues GetBinary(FieldInfo field)
        {
            throw new InvalidOperationException();
        }

        public override SortedDocValues GetSorted(FieldInfo field)
        {
            throw new InvalidOperationException();
        }

        public override SortedSetDocValues GetSortedSet(FieldInfo field)
        {
            throw new InvalidOperationException();
        }

        public override IBits GetDocsWithField(FieldInfo field)
        {
            throw new InvalidOperationException();
        }

        public override long RamBytesUsed()
        {
            return ramBytesUsed.Get();
        }

        public override void CheckIntegrity()
        {
        }
    }
}