using J2N.Runtime.CompilerServices;
using J2N.Threading.Atomic;
using Lucene.Net.Diagnostics;
using Lucene.Net.Support.Threading;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using JCG = J2N.Collections.Generic;

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
    using Directory = Lucene.Net.Store.Directory;
    using FieldInfo = Lucene.Net.Index.FieldInfo;
    using FieldInfos = Lucene.Net.Index.FieldInfos;
    using IBits = Lucene.Net.Util.IBits;
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
    /// Reads Lucene 3.x norms format and exposes it via <see cref="Index.DocValues"/> API.
    /// <para/>
    /// @lucene.experimental 
    /// </summary>
    [Obsolete("Only for reading existing 3.x indexes")]
    internal class Lucene3xNormsProducer : DocValuesProducer
    {
        /// <summary>
        /// Norms header placeholder. </summary>
        internal static readonly sbyte[] NORMS_HEADER = { (sbyte)'N', (sbyte)'R', (sbyte)'M', -1 };

        /// <summary>
        /// Extension of norms file. </summary>
        internal const string NORMS_EXTENSION = "nrm";

        /// <summary>
        /// Extension of separate norms file. </summary>
        internal const string SEPARATE_NORMS_EXTENSION = "s";

        private readonly IDictionary<string, NormsDocValues> norms = new Dictionary<string, NormsDocValues>();

        // any .nrm or .sNN files we have open at any time.
        // TODO: just a list, and double-close() separate norms files?
        internal readonly ISet<IndexInput> openFiles = new JCG.HashSet<IndexInput>(IdentityEqualityComparer<IndexInput>.Default);

        // points to a singleNormFile
        internal IndexInput singleNormStream;

        internal readonly int maxdoc;

        private readonly AtomicInt64 ramBytesUsed;

        // note: just like segmentreader in 3.x, we open up all the files here (including separate norms) up front.
        // but we just don't do any seeks or reading yet.
        public Lucene3xNormsProducer(Directory dir, SegmentInfo info, FieldInfos fields, IOContext context)
        {
            Directory separateNormsDir = info.Dir; // separate norms are never inside CFS
            maxdoc = info.DocCount;
            //string segmentName = info.Name; // LUCENENET: IDE0059: Remove unnecessary value assignment
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
                            if (singleNormStream is null)
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
                            bool isUnversioned = (version is null || StringHelper.VersionComparer.Compare(version, "3.2") < 0) && normInput.Length == maxdoc;
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
                if (Debugging.AssertsEnabled) Debugging.Assert(singleNormStream is null || nextNormSeek == singleNormStream.Length, singleNormStream != null ? "len: {0} expected: {1}" : "null", singleNormStream?.Length ?? 0, nextNormSeek);
                success = true;
            }
            finally
            {
                if (!success)
                {
                    IOUtils.DisposeWhileHandlingException(openFiles);
                }
            }
            ramBytesUsed = new AtomicInt64();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    IOUtils.Dispose(openFiles);
                }
                finally
                {
                    norms.Clear();
                    openFiles.Clear();
                    singleNormStream?.Dispose(); // LUCENENET: Dispose singleNormStream and set to null
                    singleNormStream = null;
                }
            }
        }

        private static string GetNormFilename(SegmentInfo info, int number)
        {
            if (HasSeparateNorms(info, number))
            {
                long gen = Convert.ToInt64(info.GetAttribute(Lucene3xSegmentInfoFormat.NORMGEN_PREFIX + number), CultureInfo.InvariantCulture);
                return IndexFileNames.FileNameFromGeneration(info.Name, SEPARATE_NORMS_EXTENSION + number, gen);
            }
            else
            {
                // single file for all norms
                return IndexFileNames.SegmentFileName(info.Name, "", NORMS_EXTENSION);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool HasSeparateNorms(SegmentInfo info, int number)
        {
            string v = info.GetAttribute(Lucene3xSegmentInfoFormat.NORMGEN_PREFIX + number);
            if (v is null)
            {
                return false;
            }
            else
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(Convert.ToInt64(v, CultureInfo.InvariantCulture) != SegmentInfo.NO);
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
                    UninterruptableMonitor.Enter(this);
                    try
                    {
                        if (instance is null)
                        {
                            var bytes = new byte[outerInstance.maxdoc];
                            // some norms share fds
                            UninterruptableMonitor.Enter(file);
                            try
                            {
                                file.Seek(offset);
                                file.ReadBytes(bytes, 0, bytes.Length, false);
                            }
                            finally
                            {
                                UninterruptableMonitor.Exit(file);
                            }
                            // we are done with this file
                            if (file != outerInstance.singleNormStream)
                            {
                                outerInstance.openFiles.Remove(file);
                                file.Dispose();
                            }
                            outerInstance.ramBytesUsed.AddAndGet(RamUsageEstimator.SizeOf(bytes));
                            instance = new NumericDocValuesAnonymousClass(bytes);
                        }
                        return instance;
                    }
                    finally
                    {
                        UninterruptableMonitor.Exit(this);
                    }
                }
            }

            private sealed class NumericDocValuesAnonymousClass : NumericDocValues
            {
                private readonly byte[] bytes;

                public NumericDocValuesAnonymousClass(byte[] bytes)
                {
                    this.bytes = bytes;
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                public override long Get(int docID)
                {
                    return bytes[docID];
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override NumericDocValues GetNumeric(FieldInfo field)
        {
            var dv = norms[field.Name];
            if (Debugging.AssertsEnabled) Debugging.Assert(dv != null);
            return dv.Instance;
        }

        public override BinaryDocValues GetBinary(FieldInfo field)
        {
            throw AssertionError.Create();
        }

        public override SortedDocValues GetSorted(FieldInfo field)
        {
            throw AssertionError.Create();
        }

        public override SortedSetDocValues GetSortedSet(FieldInfo field)
        {
            throw AssertionError.Create();
        }

        public override IBits GetDocsWithField(FieldInfo field)
        {
            throw AssertionError.Create();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override long RamBytesUsed() => ramBytesUsed;

        public override void CheckIntegrity() { }
    }
}