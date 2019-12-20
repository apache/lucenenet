using Lucene.Net.Codecs;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using AtomicInt32 = J2N.Threading.Atomic.AtomicInt32;

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
    using DocValuesProducer = Lucene.Net.Codecs.DocValuesProducer;
    using FieldsProducer = Lucene.Net.Codecs.FieldsProducer;
    using ICoreDisposedListener = Lucene.Net.Index.SegmentReader.ICoreDisposedListener;
    using IOContext = Lucene.Net.Store.IOContext;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using PostingsFormat = Lucene.Net.Codecs.PostingsFormat;
    using StoredFieldsReader = Lucene.Net.Codecs.StoredFieldsReader;
    using TermVectorsReader = Lucene.Net.Codecs.TermVectorsReader;

    /// <summary>
    /// Holds core readers that are shared (unchanged) when
    /// <see cref="SegmentReader"/> is cloned or reopened
    /// </summary>
    internal sealed class SegmentCoreReaders
    {
        // Counts how many other readers share the core objects
        // (freqStream, proxStream, tis, etc.) of this reader;
        // when coreRef drops to 0, these core objects may be
        // closed.  A given instance of SegmentReader may be
        // closed, even though it shares core objects with other
        // SegmentReaders:
        private readonly AtomicInt32 @ref = new AtomicInt32(1);

        internal readonly FieldsProducer fields;
        internal readonly DocValuesProducer normsProducer;

        internal readonly int termsIndexDivisor;

        internal readonly StoredFieldsReader fieldsReaderOrig;
        internal readonly TermVectorsReader termVectorsReaderOrig;
        internal readonly CompoundFileDirectory cfsReader;

        // TODO: make a single thread local w/ a
        // Thingy class holding fieldsReader, termVectorsReader,
        // normsProducer

        internal readonly DisposableThreadLocal<StoredFieldsReader> fieldsReaderLocal;

        private class AnonymousFieldsReaderLocal : DisposableThreadLocal<StoredFieldsReader>
        {
            private readonly SegmentCoreReaders outerInstance;

            public AnonymousFieldsReaderLocal(SegmentCoreReaders outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            protected internal override StoredFieldsReader InitialValue()
            {
                return (StoredFieldsReader)outerInstance.fieldsReaderOrig.Clone();
            }
        }

        internal readonly DisposableThreadLocal<TermVectorsReader> termVectorsLocal;

        private class AnonymousTermVectorsLocal : DisposableThreadLocal<TermVectorsReader>
        {
            private readonly SegmentCoreReaders outerInstance;

            public AnonymousTermVectorsLocal(SegmentCoreReaders outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            protected internal override TermVectorsReader InitialValue()
            {
                return (outerInstance.termVectorsReaderOrig == null) ? null : (TermVectorsReader)outerInstance.termVectorsReaderOrig.Clone();
            }
        }

        internal readonly DisposableThreadLocal<IDictionary<string, object>> normsLocal = new DisposableThreadLocalAnonymousInnerClassHelper3();

        private class DisposableThreadLocalAnonymousInnerClassHelper3 : DisposableThreadLocal<IDictionary<string, object>>
        {
            public DisposableThreadLocalAnonymousInnerClassHelper3()
            {
            }

            protected internal override IDictionary<string, object> InitialValue()
            {
                return new Dictionary<string, object>();
            }
        }

        private readonly ISet<ICoreDisposedListener> coreClosedListeners = new ConcurrentHashSet<ICoreDisposedListener>(IdentityComparer<ICoreDisposedListener>.Default);

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
                    cfsDir = cfsReader = new CompoundFileDirectory(dir, IndexFileNames.SegmentFileName(si.Info.Name, "", IndexFileNames.COMPOUND_FILE_EXTENSION), context, false);
                }
                else
                {
                    cfsReader = null;
                    cfsDir = dir;
                }

                FieldInfos fieldInfos = owner.FieldInfos;

                this.termsIndexDivisor = termsIndexDivisor;
                PostingsFormat format = codec.PostingsFormat;
                SegmentReadState segmentReadState = new SegmentReadState(cfsDir, si.Info, fieldInfos, context, termsIndexDivisor);
                // Ask codec for its Fields
                fields = format.FieldsProducer(segmentReadState);
                Debug.Assert(fields != null);
                // ask codec for its Norms:
                // TODO: since we don't write any norms file if there are no norms,
                // kinda jaky to assume the codec handles the case of no norms file at all gracefully?!

                if (fieldInfos.HasNorms)
                {
                    normsProducer = codec.NormsFormat.NormsProducer(segmentReadState);
                    Debug.Assert(normsProducer != null);
                }
                else
                {
                    normsProducer = null;
                }

                fieldsReaderOrig = si.Info.Codec.StoredFieldsFormat.FieldsReader(cfsDir, si.Info, fieldInfos, context);

                if (fieldInfos.HasVectors) // open term vector files only as needed
                {
                    termVectorsReaderOrig = si.Info.Codec.TermVectorsFormat.VectorsReader(cfsDir, si.Info, fieldInfos, context);
                }
                else
                {
                    termVectorsReaderOrig = null;
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

        internal int RefCount => @ref;

        internal void IncRef()
        {
            int count;
            while ((count = @ref) > 0)
            {
                if (@ref.CompareAndSet(count, count + 1))
                {
                    return;
                }
            }
            throw new ObjectDisposedException(this.GetType().GetTypeInfo().FullName, "SegmentCoreReaders is already closed");
        }

        internal NumericDocValues GetNormValues(FieldInfo fi)
        {
            Debug.Assert(normsProducer != null);

            IDictionary<string, object> normFields = normsLocal.Get();

            object ret;
            normFields.TryGetValue(fi.Name, out ret);
            var norms = ret as NumericDocValues;
            if (norms == null)
            {
                norms = normsProducer.GetNumeric(fi);
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
                    IOUtils.Dispose(termVectorsLocal, fieldsReaderLocal, normsLocal, fields, termVectorsReaderOrig, fieldsReaderOrig, cfsReader, normsProducer);
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
            lock (coreClosedListeners)
            {
                foreach (ICoreDisposedListener listener in coreClosedListeners)
                {
                    // SegmentReader uses our instance as its
                    // coreCacheKey:
                    try
                    {
                        listener.OnDispose(this);
                    }
                    catch (Exception t)
                    {
                        
                        if (th == null)
                        {
                            th = t;
                        }
                        else
                        {
                            th.AddSuppressed(t);
                        }
                    }
                }
                IOUtils.ReThrowUnchecked(th);
            }
        }

        internal void AddCoreDisposedListener(ICoreDisposedListener listener)
        {
            coreClosedListeners.Add(listener);
        }

        internal void RemoveCoreDisposedListener(ICoreDisposedListener listener)
        {
            coreClosedListeners.Remove(listener);
        }

        /// <summary>
        /// Returns approximate RAM bytes used </summary>
        public long RamBytesUsed()
        {
            return ((normsProducer != null) ? normsProducer.RamBytesUsed() : 0) + 
                ((fields != null) ? fields.RamBytesUsed() : 0) + 
                ((fieldsReaderOrig != null) ? fieldsReaderOrig.RamBytesUsed() : 0) + 
                ((termVectorsReaderOrig != null) ? termVectorsReaderOrig.RamBytesUsed() : 0);
        }
    }
}