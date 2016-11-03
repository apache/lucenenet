using Lucene.Net.Codecs;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Lucene.Net.Index
{
    using Lucene.Net.Support;
    using Lucene.Net.Util;
    using AlreadyClosedException = Lucene.Net.Store.AlreadyClosedException;

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
    using CoreClosedListener = Lucene.Net.Index.SegmentReader.CoreClosedListener;
    using Directory = Lucene.Net.Store.Directory;
    using DocValuesProducer = Lucene.Net.Codecs.DocValuesProducer;
    using FieldsProducer = Lucene.Net.Codecs.FieldsProducer;
    using IOContext = Lucene.Net.Store.IOContext;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using PostingsFormat = Lucene.Net.Codecs.PostingsFormat;
    using StoredFieldsReader = Lucene.Net.Codecs.StoredFieldsReader;
    using TermVectorsReader = Lucene.Net.Codecs.TermVectorsReader;

    /// <summary>
    /// Holds core readers that are shared (unchanged) when
    /// SegmentReader is cloned or reopened
    /// </summary>
    public sealed class SegmentCoreReaders
    {
        // Counts how many other readers share the core objects
        // (freqStream, proxStream, tis, etc.) of this reader;
        // when coreRef drops to 0, these core objects may be
        // closed.  A given instance of SegmentReader may be
        // closed, even though it shares core objects with other
        // SegmentReaders:
        private readonly AtomicInteger @ref = new AtomicInteger(1);

        internal readonly FieldsProducer Fields;
        internal readonly DocValuesProducer NormsProducer;

        internal readonly int TermsIndexDivisor;

        internal readonly StoredFieldsReader FieldsReaderOrig;
        internal readonly TermVectorsReader TermVectorsReaderOrig;
        internal readonly CompoundFileDirectory CfsReader;

        // TODO: make a single thread local w/ a
        // Thingy class holding fieldsReader, termVectorsReader,
        // normsProducer

        internal readonly IDisposableThreadLocal<StoredFieldsReader> fieldsReaderLocal;

        private class AnonymousFieldsReaderLocal : IDisposableThreadLocal<StoredFieldsReader>
        {
            private readonly SegmentCoreReaders OuterInstance;

            public AnonymousFieldsReaderLocal(SegmentCoreReaders outerInstance)
            {
                OuterInstance = outerInstance;
            }

            protected internal override StoredFieldsReader InitialValue()
            {
                return (StoredFieldsReader)OuterInstance.FieldsReaderOrig.Clone();
            }
        }

        internal readonly IDisposableThreadLocal<TermVectorsReader> termVectorsLocal;

        private class AnonymousTermVectorsLocal : IDisposableThreadLocal<TermVectorsReader>
        {
            private readonly SegmentCoreReaders OuterInstance;

            public AnonymousTermVectorsLocal(SegmentCoreReaders outerInstance)
            {
                OuterInstance = outerInstance;
            }

            protected internal override TermVectorsReader InitialValue()
            {
                return (OuterInstance.TermVectorsReaderOrig == null) ? null : (TermVectorsReader)OuterInstance.TermVectorsReaderOrig.Clone();
            }
        }

        internal readonly IDisposableThreadLocal<IDictionary<string, object>> normsLocal = new IDisposableThreadLocalAnonymousInnerClassHelper3();

        private class IDisposableThreadLocalAnonymousInnerClassHelper3 : IDisposableThreadLocal<IDictionary<string, object>>
        {
            public IDisposableThreadLocalAnonymousInnerClassHelper3()
            {
            }

            protected internal override IDictionary<string, object> InitialValue()
            {
                return new Dictionary<string, object>();
            }
        }

        private readonly ISet<CoreClosedListener> CoreClosedListeners = new ConcurrentHashSet<CoreClosedListener>(new IdentityComparer<CoreClosedListener>());

        internal SegmentCoreReaders(SegmentReader owner, Directory dir, SegmentCommitInfo si, IOContext context, int termsIndexDivisor)
        {
            fieldsReaderLocal = new AnonymousFieldsReaderLocal(this);
            termVectorsLocal = new AnonymousTermVectorsLocal(this);

            if (termsIndexDivisor == 0)
            {
                throw new System.ArgumentException("indexDivisor must be < 0 (don't load terms index) or greater than 0 (got 0)");
            }

            Codec codec = si.Info.Codec;
            Directory cfsDir; // confusing name: if (cfs) its the cfsdir, otherwise its the segment's directory.

            bool success = false;

            try
            {
                if (si.Info.UseCompoundFile)
                {
                    cfsDir = CfsReader = new CompoundFileDirectory(dir, IndexFileNames.SegmentFileName(si.Info.Name, "", IndexFileNames.COMPOUND_FILE_EXTENSION), context, false);
                }
                else
                {
                    CfsReader = null;
                    cfsDir = dir;
                }

                FieldInfos fieldInfos = owner.FieldInfos_Renamed;

                this.TermsIndexDivisor = termsIndexDivisor;
                PostingsFormat format = codec.PostingsFormat();
                SegmentReadState segmentReadState = new SegmentReadState(cfsDir, si.Info, fieldInfos, context, termsIndexDivisor);
                // Ask codec for its Fields
                Fields = format.FieldsProducer(segmentReadState);
                Debug.Assert(Fields != null);
                // ask codec for its Norms:
                // TODO: since we don't write any norms file if there are no norms,
                // kinda jaky to assume the codec handles the case of no norms file at all gracefully?!

                if (fieldInfos.HasNorms())
                {
                    NormsProducer = codec.NormsFormat().NormsProducer(segmentReadState);
                    Debug.Assert(NormsProducer != null);
                }
                else
                {
                    NormsProducer = null;
                }

                StoredFieldsFormat sff = si.Info.Codec.StoredFieldsFormat();

#if !NETSTANDARD
                try
                {
#endif
                    FieldsReaderOrig = sff.FieldsReader(cfsDir, si.Info, fieldInfos, context);
#if !NETSTANDARD
                }
                catch (System.AccessViolationException ave)
                {
                }
#endif
                //FieldsReaderOrig = si.Info.Codec.StoredFieldsFormat().FieldsReader(cfsDir, si.Info, fieldInfos, context);

                if (fieldInfos.HasVectors()) // open term vector files only as needed
                {
                    TermVectorsReaderOrig = si.Info.Codec.TermVectorsFormat().VectorsReader(cfsDir, si.Info, fieldInfos, context);
                }
                else
                {
                    TermVectorsReaderOrig = null;
                }

                success = true;
            }
            finally
            {
                if (!success)
                {
                    DecRef();
                }
            }
        }

        internal int RefCount
        {
            get
            {
                return @ref.Get();
            }
        }

        internal void IncRef()
        {
            int count;
            while ((count = @ref.Get()) > 0)
            {
                if (@ref.CompareAndSet(count, count + 1))
                {
                    return;
                }
            }
            throw new AlreadyClosedException("SegmentCoreReaders is already closed");
        }

        internal NumericDocValues GetNormValues(FieldInfo fi)
        {
            Debug.Assert(NormsProducer != null);

            IDictionary<string, object> normFields = normsLocal.Get();

            object ret;
            normFields.TryGetValue(fi.Name, out ret);
            var norms = ret as NumericDocValues;
            if (norms == null)
            {
                norms = NormsProducer.GetNumeric(fi);
                normFields[fi.Name] = norms;
            }

            return norms;
        }

        internal void DecRef()
        {
            if (@ref.DecrementAndGet() == 0)
            {
                Exception th = null;
                try
                {
                    IOUtils.Close(termVectorsLocal, fieldsReaderLocal, normsLocal, Fields, TermVectorsReaderOrig, FieldsReaderOrig, CfsReader, NormsProducer);
                }
                catch (Exception throwable)
                {
                    th = throwable;
                }
                finally
                {
                    NotifyCoreClosedListeners(th);
                }
            }
        }

        private void NotifyCoreClosedListeners(Exception th)
        {
            lock (CoreClosedListeners)
            {
                foreach (CoreClosedListener listener in CoreClosedListeners)
                {
                    // SegmentReader uses our instance as its
                    // coreCacheKey:
                    try
                    {
                        listener.OnClose(this);
                    }
                    catch (Exception)
                    {
                    }
                }
                IOUtils.ReThrowUnchecked(th);
            }
        }

        internal void AddCoreClosedListener(CoreClosedListener listener)
        {
            CoreClosedListeners.Add(listener);
        }

        internal void RemoveCoreClosedListener(CoreClosedListener listener)
        {
            CoreClosedListeners.Remove(listener);
        }

        /// <summary>
        /// Returns approximate RAM bytes used </summary>
        public long RamBytesUsed()
        {
            return ((NormsProducer != null) ? NormsProducer.RamBytesUsed() : 0) + ((Fields != null) ? Fields.RamBytesUsed() : 0) + ((FieldsReaderOrig != null) ? FieldsReaderOrig.RamBytesUsed() : 0) + ((TermVectorsReaderOrig != null) ? TermVectorsReaderOrig.RamBytesUsed() : 0);
        }
    }
}