using J2N;
using J2N.Runtime.CompilerServices;
using Lucene.Net.Diagnostics;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Index
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

    using Codec = Lucene.Net.Codecs.Codec;
    using CompoundFileDirectory = Lucene.Net.Store.CompoundFileDirectory;
    using Directory = Lucene.Net.Store.Directory;
    using DocValuesFormat = Lucene.Net.Codecs.DocValuesFormat;
    using DocValuesProducer = Lucene.Net.Codecs.DocValuesProducer;
    using IBits = Lucene.Net.Util.IBits;
    using IOContext = Lucene.Net.Store.IOContext;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using StoredFieldsReader = Lucene.Net.Codecs.StoredFieldsReader;
    using TermVectorsReader = Lucene.Net.Codecs.TermVectorsReader;

    /// <summary>
    /// <see cref="IndexReader"/> implementation over a single segment.
    /// <para/>
    /// Instances pointing to the same segment (but with different deletes, etc)
    /// may share the same core data.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public sealed class SegmentReader : AtomicReader
    {
        private readonly SegmentCommitInfo si;
        private readonly IBits liveDocs;

        // Normally set to si.docCount - si.delDocCount, unless we
        // were created as an NRT reader from IW, in which case IW
        // tells us the docCount:
        private readonly int numDocs;

        internal readonly SegmentCoreReaders core;
        internal readonly SegmentDocValues segDocValues;

        internal readonly DisposableThreadLocal<IDictionary<string, object>> docValuesLocal =
            new DisposableThreadLocal<IDictionary<string, object>>(() => new Dictionary<string, object>());

        internal readonly DisposableThreadLocal<IDictionary<string, IBits>> docsWithFieldLocal =
            new DisposableThreadLocal<IDictionary<string, IBits>>(() => new Dictionary<string, IBits>());

        internal readonly IDictionary<string, DocValuesProducer> dvProducersByField = new Dictionary<string, DocValuesProducer>();
        internal readonly ISet<DocValuesProducer> dvProducers = new JCG.HashSet<DocValuesProducer>(IdentityEqualityComparer<DocValuesProducer>.Default);

        private readonly FieldInfos fieldInfos; // LUCENENET specific - since it is readonly, made all internal classes use property

        private readonly IList<long> dvGens = new JCG.List<long>();

        /// <summary>
        /// Constructs a new <see cref="SegmentReader"/> with a new core. </summary>
        /// <exception cref="CorruptIndexException"> if the index is corrupt </exception>
        /// <exception cref="IOException"> if there is a low-level IO error </exception>
        // TODO: why is this public?
        public SegmentReader(SegmentCommitInfo si, int termInfosIndexDivisor, IOContext context)
        {
            this.si = si;
            // TODO if the segment uses CFS, we may open the CFS file twice: once for
            // reading the FieldInfos (if they are not gen'd) and second time by
            // SegmentCoreReaders. We can open the CFS here and pass to SCR, but then it
            // results in less readable code (resource not closed where it was opened).
            // Best if we could somehow read FieldInfos in SCR but not keep it there, but
            // constructors don't allow returning two things...
            fieldInfos = ReadFieldInfos(si);
            core = new SegmentCoreReaders(this, si.Info.Dir, si, context, termInfosIndexDivisor);
            segDocValues = new SegmentDocValues();

            bool success = false;
            Codec codec = si.Info.Codec;
            try
            {
                if (si.HasDeletions)
                {
                    // NOTE: the bitvector is stored using the regular directory, not cfs
                    liveDocs = codec.LiveDocsFormat.ReadLiveDocs(Directory, si, IOContext.READ_ONCE);
                }
                else
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(si.DelCount == 0);
                    liveDocs = null;
                }
                numDocs = si.Info.DocCount - si.DelCount;

                if (FieldInfos.HasDocValues)
                {
                    InitDocValuesProducers(codec);
                }

                success = true;
            }
            finally
            {
                // With lock-less commits, it's entirely possible (and
                // fine) to hit a FileNotFound exception above.  In
                // this case, we want to explicitly close any subset
                // of things that were opened so that we don't have to
                // wait for a GC to do so.
                if (!success)
                {
                    DoClose();
                }
            }
        }

        /// <summary>
        /// Create new <see cref="SegmentReader"/> sharing core from a previous
        /// <see cref="SegmentReader"/> and loading new live docs from a new
        /// deletes file. Used by <see cref="DirectoryReader.OpenIfChanged(DirectoryReader)"/>.
        /// </summary>
        internal SegmentReader(SegmentCommitInfo si, SegmentReader sr)
            : this(si, sr, si.Info.Codec.LiveDocsFormat.ReadLiveDocs(si.Info.Dir, si, IOContext.READ_ONCE), si.Info.DocCount - si.DelCount)
        {
        }

        /// <summary>
        /// Create new <see cref="SegmentReader"/> sharing core from a previous
        /// <see cref="SegmentReader"/> and using the provided in-memory
        /// liveDocs.  Used by <see cref="IndexWriter"/> to provide a new NRT
        /// reader
        /// </summary>
        internal SegmentReader(SegmentCommitInfo si, SegmentReader sr, IBits liveDocs, int numDocs)
        {
            this.si = si;
            this.liveDocs = liveDocs;
            this.numDocs = numDocs;
            this.core = sr.core;
            core.IncRef();
            this.segDocValues = sr.segDocValues;

            //    System.out.println("[" + Thread.currentThread().getName() + "] SR.init: sharing reader: " + sr + " for gens=" + sr.genDVProducers.keySet());

            // increment refCount of DocValuesProducers that are used by this reader
            bool success = false;
            try
            {
                Codec codec = si.Info.Codec;
                if (si.FieldInfosGen == -1)
                {
                    fieldInfos = sr.FieldInfos;
                }
                else
                {
                    fieldInfos = ReadFieldInfos(si);
                }

                if (FieldInfos.HasDocValues)
                {
                    InitDocValuesProducers(codec);
                }
                success = true;
            }
            finally
            {
                if (!success)
                {
                    DoClose();
                }
            }
        }

        // initialize the per-field DocValuesProducer
        private void InitDocValuesProducers(Codec codec)
        {
            Directory dir = core.cfsReader ?? si.Info.Dir;
            DocValuesFormat dvFormat = codec.DocValuesFormat;
            IDictionary<long, IList<FieldInfo>> genInfos = GetGenInfos();

            //      System.out.println("[" + Thread.currentThread().getName() + "] SR.initDocValuesProducers: segInfo=" + si + "; gens=" + genInfos.keySet());

            // TODO: can we avoid iterating over fieldinfos several times and creating maps of all this stuff if dv updates do not exist?

            foreach (KeyValuePair<long, IList<FieldInfo>> e in genInfos)
            {
                long gen = e.Key;
                IList<FieldInfo> infos = e.Value;
                DocValuesProducer dvp = segDocValues.GetDocValuesProducer(gen, si, IOContext.READ, dir, dvFormat, infos, TermInfosIndexDivisor);
                foreach (FieldInfo fi in infos)
                {
                    dvProducersByField[fi.Name] = dvp;
                    dvProducers.Add(dvp);
                }
            }

            dvGens.AddRange(genInfos.Keys);
        }

        /// <summary>
        /// Reads the most recent <see cref="Index.FieldInfos"/> of the given segment info.
        /// <para/>
        /// @lucene.internal
        /// </summary>
        internal static FieldInfos ReadFieldInfos(SegmentCommitInfo info)
        {
            Directory dir;
            bool closeDir;
            if (info.FieldInfosGen == -1 && info.Info.UseCompoundFile)
            {
                // no fieldInfos gen and segment uses a compound file
                dir = new CompoundFileDirectory(info.Info.Dir, IndexFileNames.SegmentFileName(info.Info.Name, "", IndexFileNames.COMPOUND_FILE_EXTENSION), IOContext.READ_ONCE, false);
                closeDir = true;
            }
            else
            {
                // gen'd FIS are read outside CFS, or the segment doesn't use a compound file
                dir = info.Info.Dir;
                closeDir = false;
            }

            try
            {
                // LUCENENET specific: We created the segments names wrong in 4.8.0-beta00001 - 4.8.0-beta00015,
                // so we added a switch to be able to read these indexes in later versions. This logic as well as an
                // optimization on the first 100 segment values is implmeneted in SegmentInfos.SegmentNumberToString().
                string segmentSuffix = info.FieldInfosGen == -1 ? string.Empty : SegmentInfos.SegmentNumberToString(info.FieldInfosGen);
                return info.Info.Codec.FieldInfosFormat.FieldInfosReader.Read(dir, info.Info.Name, segmentSuffix, IOContext.READ_ONCE);
            }
            finally
            {
                if (closeDir)
                {
                    dir.Dispose();
                }
            }
        }

        // returns a gen->List<FieldInfo> mapping. Fields without DV updates have gen=-1
        private IDictionary<long, IList<FieldInfo>> GetGenInfos()
        {
            IDictionary<long, IList<FieldInfo>> genInfos = new Dictionary<long, IList<FieldInfo>>();
            foreach (FieldInfo fi in FieldInfos)
            {
                if (fi.DocValuesType == DocValuesType.NONE)
                {
                    continue;
                }
                long gen = fi.DocValuesGen;
                if (!genInfos.TryGetValue(gen, out IList<FieldInfo> infos) || infos is null)
                {
                    infos = new JCG.List<FieldInfo>();
                    genInfos[gen] = infos;
                }
                infos.Add(fi);
            }
            return genInfos;
        }

        public override IBits LiveDocs
        {
            get
            {
                EnsureOpen();
                return liveDocs;
            }
        }

        protected internal override void DoClose()
        {
            //System.out.println("SR.close seg=" + si);
            try
            {
                core.DecRef();
            }
            finally
            {
                dvProducersByField.Clear();
                try
                {
                    IOUtils.Dispose(docValuesLocal, docsWithFieldLocal);
                }
                finally
                {
                    segDocValues.DecRef(dvGens);
                }
            }
        }

        public override FieldInfos FieldInfos
        {
            get
            {
                EnsureOpen();
                return fieldInfos;
            }
        }

        /// <summary>
        /// Expert: retrieve thread-private 
        /// <see cref="StoredFieldsReader"/>
        /// <para/>
        /// @lucene.internal
        /// </summary>
        public StoredFieldsReader FieldsReader
        {
            get
            {
                EnsureOpen();
                return core.fieldsReaderLocal.Value;
            }
        }

        public override void Document(int docID, StoredFieldVisitor visitor)
        {
            CheckBounds(docID);
            FieldsReader.VisitDocument(docID, visitor);
        }

        public override Fields Fields
        {
            get
            {
                EnsureOpen();
                return core.fields;
            }
        }

        public override int NumDocs =>
            // Don't call ensureOpen() here (it could affect performance)
            numDocs;

        public override int MaxDoc =>
            // Don't call ensureOpen() here (it could affect performance)
            si.Info.DocCount;

        /// <summary>
        /// Expert: retrieve thread-private
        /// <see cref="Codecs.TermVectorsReader"/>
        /// <para/>
        /// @lucene.internal
        /// </summary>
        public TermVectorsReader TermVectorsReader
        {
            get
            {
                EnsureOpen();
                return core.termVectorsLocal.Value;
            }
        }

        public override Fields GetTermVectors(int docID)
        {
            TermVectorsReader termVectorsReader = TermVectorsReader;
            if (termVectorsReader is null)
            {
                return null;
            }
            CheckBounds(docID);
            return termVectorsReader.Get(docID);
        }

        private void CheckBounds(int docID)
        {
            if (docID < 0 || docID >= MaxDoc)
            {
                throw new IndexOutOfRangeException("docID must be >= 0 and < maxDoc=" + MaxDoc + " (got docID=" + docID + ")");
            }
        }

        public override string ToString()
        {
            // SegmentInfo.toString takes dir and number of
            // *pending* deletions; so we reverse compute that here:
            return si.ToString(si.Info.Dir, si.Info.DocCount - numDocs - si.DelCount);
        }

        /// <summary>
        /// Return the name of the segment this reader is reading.
        /// </summary>
        public string SegmentName => si.Info.Name;

        /// <summary>
        /// Return the <see cref="SegmentCommitInfo"/> of the segment this reader is reading.
        /// </summary>
        public SegmentCommitInfo SegmentInfo => si;

        /// <summary>
        /// Returns the directory this index resides in. </summary>
        public Directory Directory =>
            // Don't ensureOpen here -- in certain cases, when a
            // cloned/reopened reader needs to commit, it may call
            // this method on the closed original reader
            si.Info.Dir;

        // this is necessary so that cloned SegmentReaders (which
        // share the underlying postings data) will map to the
        // same entry in the FieldCache.  See LUCENE-1579.
        public override object CoreCacheKey =>
            // NOTE: if this ever changes, be sure to fix
            // SegmentCoreReader.notifyCoreClosedListeners to match!
            // Today it passes "this" as its coreCacheKey:
            core;

        public override object CombinedCoreAndDeletesKey => this;

        /// <summary>
        /// Returns term infos index divisor originally passed to
        /// <see cref="SegmentReader(SegmentCommitInfo, int, IOContext)"/>.
        /// </summary>
        public int TermInfosIndexDivisor => core.termsIndexDivisor;

        // returns the FieldInfo that corresponds to the given field and type, or
        // null if the field does not exist, or not indexed as the requested
        // DovDocValuesType.
        private FieldInfo GetDVField(string field, DocValuesType type)
        {
            FieldInfo fi = FieldInfos.FieldInfo(field);
            if (fi is null)
            {
                // Field does not exist
                return null;
            }
            if (fi.DocValuesType == DocValuesType.NONE)
            {
                // Field was not indexed with doc values
                return null;
            }
            if (fi.DocValuesType != type)
            {
                // Field DocValues are different than requested type
                return null;
            }

            return fi;
        }

        public override NumericDocValues GetNumericDocValues(string field)
        {
            EnsureOpen();
            FieldInfo fi = GetDVField(field, DocValuesType.NUMERIC);
            if (fi is null)
            {
                return null;
            }

            IDictionary<string, object> dvFields = docValuesLocal.Value;

            if (!dvFields.TryGetValue(field, out object dvsDummy) || !(dvsDummy is NumericDocValues dvs))
            {
                dvProducersByField.TryGetValue(field, out DocValuesProducer dvProducer);
                if (Debugging.AssertsEnabled) Debugging.Assert(dvProducer != null);
                dvs = dvProducer.GetNumeric(fi);
                dvFields[field] = dvs;
            }

            return dvs;
        }

        public override IBits GetDocsWithField(string field)
        {
            EnsureOpen();
            FieldInfo fi = FieldInfos.FieldInfo(field);
            if (fi is null)
            {
                // Field does not exist
                return null;
            }
            if (fi.DocValuesType == DocValuesType.NONE)
            {
                // Field was not indexed with doc values
                return null;
            }

            IDictionary<string, IBits> dvFields = docsWithFieldLocal.Value;

            if (!dvFields.TryGetValue(field, out IBits dvs) || dvs is null)
            {
                dvProducersByField.TryGetValue(field, out DocValuesProducer dvProducer);
                if (Debugging.AssertsEnabled) Debugging.Assert(dvProducer != null);
                dvs = dvProducer.GetDocsWithField(fi);
                dvFields[field] = dvs;
            }

            return dvs;
        }

        public override BinaryDocValues GetBinaryDocValues(string field)
        {
            EnsureOpen();
            FieldInfo fi = GetDVField(field, DocValuesType.BINARY);
            if (fi is null)
            {
                return null;
            }

            IDictionary<string, object> dvFields = docValuesLocal.Value;

            if (!dvFields.TryGetValue(field, out object ret) || !(ret is BinaryDocValues dvs))
            {
                dvProducersByField.TryGetValue(field, out DocValuesProducer dvProducer);
                if (Debugging.AssertsEnabled) Debugging.Assert(dvProducer != null);
                dvs = dvProducer.GetBinary(fi);
                dvFields[field] = dvs;
            }

            return dvs;
        }

        public override SortedDocValues GetSortedDocValues(string field)
        {
            EnsureOpen();
            FieldInfo fi = GetDVField(field, DocValuesType.SORTED);
            if (fi is null)
            {
                return null;
            }

            IDictionary<string, object> dvFields = docValuesLocal.Value;

            if (!dvFields.TryGetValue(field, out object ret) || !(ret is SortedDocValues dvs))
            {
                dvProducersByField.TryGetValue(field, out DocValuesProducer dvProducer);
                if (Debugging.AssertsEnabled) Debugging.Assert(dvProducer != null);
                dvs = dvProducer.GetSorted(fi);
                dvFields[field] = dvs;
            }

            return dvs;
        }

        public override SortedSetDocValues GetSortedSetDocValues(string field)
        {
            EnsureOpen();
            FieldInfo fi = GetDVField(field, DocValuesType.SORTED_SET);
            if (fi is null)
            {
                return null;
            }

            IDictionary<string, object> dvFields = docValuesLocal.Value;

            if (!dvFields.TryGetValue(field, out object ret) || !(ret is SortedSetDocValues dvs))
            {
                dvProducersByField.TryGetValue(field, out DocValuesProducer dvProducer);
                if (Debugging.AssertsEnabled) Debugging.Assert(dvProducer != null);
                dvs = dvProducer.GetSortedSet(fi);
                dvFields[field] = dvs;
            }

            return dvs;
        }

        public override NumericDocValues GetNormValues(string field)
        {
            EnsureOpen();
            FieldInfo fi = FieldInfos.FieldInfo(field);
            if (fi is null || !fi.HasNorms)
            {
                // Field does not exist or does not index norms
                return null;
            }
            return core.GetNormValues(fi);
        }

        /// <summary>
        /// Called when the shared core for this <see cref="SegmentReader"/>
        /// is disposed.
        /// <para/>
        /// This listener is called only once all <see cref="SegmentReader"/>s
        /// sharing the same core are disposed.  At this point it
        /// is safe for apps to evict this reader from any caches
        /// keyed on <see cref="CoreCacheKey"/>.  This is the same
        /// interface that <see cref="Search.IFieldCache"/> uses, internally,
        /// to evict entries.
        /// <para/>
        /// NOTE: This was CoreClosedListener in Lucene.
        /// <para/>
        /// @lucene.experimental
        /// </summary>
        public interface ICoreDisposedListener 
        {
            /// <summary>
            /// Invoked when the shared core of the original 
            /// <see cref="SegmentReader"/> has disposed.
            /// </summary>
            void OnDispose(object ownerCoreCacheKey);
        }

        /// <summary>
        /// Expert: adds a <see cref="ICoreDisposedListener"/> to this reader's shared core </summary>
        public void AddCoreDisposedListener(ICoreDisposedListener listener)
        {
            EnsureOpen();
            core.AddCoreDisposedListener(listener);
        }

        /// <summary>
        /// Expert: removes a <see cref="ICoreDisposedListener"/> from this reader's shared core </summary>
        public void RemoveCoreDisposedListener(ICoreDisposedListener listener)
        {
            EnsureOpen();
            core.RemoveCoreDisposedListener(listener);
        }

        /// <summary>
        /// Returns approximate RAM Bytes used </summary>
        public long RamBytesUsed()
        {
            EnsureOpen();
            long ramBytesUsed = 0;
            if (dvProducers != null)
            {
                foreach (DocValuesProducer producer in dvProducers)
                {
                    ramBytesUsed += producer.RamBytesUsed();
                }
            }
            if (core != null)
            {
                ramBytesUsed += core.RamBytesUsed();
            }
            return ramBytesUsed;
        }

        public override void CheckIntegrity()
        {
            EnsureOpen();

            // stored fields
            FieldsReader.CheckIntegrity();

            // term vectors
            TermVectorsReader termVectorsReader = TermVectorsReader;
            if (termVectorsReader != null)
            {
                termVectorsReader.CheckIntegrity();
            }

            // terms/postings
            if (core.fields != null)
            {
                core.fields.CheckIntegrity();
            }

            // norms
            if (core.normsProducer != null)
            {
                core.normsProducer.CheckIntegrity();
            }

            // docvalues
            if (dvProducers != null)
            {
                foreach (DocValuesProducer producer in dvProducers)
                {
                    producer.CheckIntegrity();
                }
            }
        }
    }
}