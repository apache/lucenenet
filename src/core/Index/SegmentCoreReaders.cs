using Lucene.Net.Codecs;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using ICoreClosedListener = Lucene.Net.Index.SegmentReader.ICoreClosedListener;

namespace Lucene.Net.Index
{
    internal sealed class SegmentCoreReaders
    {
        // Counts how many other reader share the core objects
        // (freqStream, proxStream, tis, etc.) of this reader;
        // when coreRef drops to 0, these core objects may be
        // closed.  A given instance of SegmentReader may be
        // closed, even those it shares core objects with other
        // SegmentReaders:
        private int ref_renamed = 1;

        internal readonly FieldInfos fieldInfos;

        internal readonly FieldsProducer fields;
        internal readonly DocValuesProducer dvProducer;
        internal readonly DocValuesProducer normsProducer;

        internal readonly int termsIndexDivisor;

        private readonly SegmentReader owner;

        internal readonly StoredFieldsReader fieldsReaderOrig;
        internal readonly TermVectorsReader termVectorsReaderOrig;
        internal readonly CompoundFileDirectory cfsReader;

        // TODO: make a single thread local w/ a
        // Thingy class holding fieldsReader, termVectorsReader,
        // normsProducer, dvProducer

        internal readonly CloseableThreadLocal<StoredFieldsReader> fieldsReaderLocal;

        private sealed class AnonymousFieldsReaderLocal : CloseableThreadLocal<StoredFieldsReader>
        {
            private readonly SegmentCoreReaders parent;

            public AnonymousFieldsReaderLocal(SegmentCoreReaders parent)
            {
                this.parent = parent;
            }

            public override StoredFieldsReader InitialValue()
            {
                return (StoredFieldsReader)parent.fieldsReaderOrig.Clone();
            }
        }

        internal readonly CloseableThreadLocal<TermVectorsReader> termVectorsLocal;

        private sealed class AnonymousTermVectorsLocal : CloseableThreadLocal<TermVectorsReader>
        {
            private readonly SegmentCoreReaders parent;

            public AnonymousTermVectorsLocal(SegmentCoreReaders parent)
            {
                this.parent = parent;
            }

            public override TermVectorsReader InitialValue()
            {
                return (parent.termVectorsReaderOrig == null) ? null : (TermVectorsReader)parent.termVectorsReaderOrig.Clone();
            }
        }

        internal readonly CloseableThreadLocal<IDictionary<string, object>> docValuesLocal = new AnonymousDocValuesLocal();

        private sealed class AnonymousDocValuesLocal : CloseableThreadLocal<IDictionary<string, object>>
        {
            public override IDictionary<string, object> InitialValue()
            {
                return new HashMap<string, object>();
            }
        }

        internal readonly CloseableThreadLocal<IDictionary<string, object>> normsLocal = new AnonymousDocValuesLocal();

        private readonly ISet<ICoreClosedListener> coreClosedListeners = new ConcurrentHashSet<ICoreClosedListener>(new IdentityComparer<ICoreClosedListener>());

        public SegmentCoreReaders(SegmentReader owner, Directory dir, SegmentInfoPerCommit si, IOContext context, int termsIndexDivisor)
        {
            // .NET Port: These lines are necessary as we can't use "this" inline above
            fieldsReaderLocal = new AnonymousFieldsReaderLocal(this);
            termVectorsLocal = new AnonymousTermVectorsLocal(this);

            if (termsIndexDivisor == 0)
            {
                throw new ArgumentException("indexDivisor must be < 0 (don't load terms index) or greater than 0 (got 0)");
            }

            Codec codec = si.info.Codec;
            Directory cfsDir; // confusing name: if (cfs) its the cfsdir, otherwise its the segment's directory.

            bool success = false;

            try
            {
                if (si.info.UseCompoundFile)
                {
                    cfsDir = cfsReader = new CompoundFileDirectory(dir, IndexFileNames.SegmentFileName(si.info.name, "", IndexFileNames.COMPOUND_FILE_EXTENSION), context, false);
                }
                else
                {
                    cfsReader = null;
                    cfsDir = dir;
                }
                fieldInfos = codec.FieldInfosFormat.FieldInfosReader.Read(cfsDir, si.info.name, IOContext.READONCE);

                this.termsIndexDivisor = termsIndexDivisor;
                PostingsFormat format = codec.PostingsFormat;
                SegmentReadState segmentReadState = new SegmentReadState(cfsDir, si.info, fieldInfos, context, termsIndexDivisor);
                // Ask codec for its Fields
                fields = format.FieldsProducer(segmentReadState);
                //assert fields != null;
                // ask codec for its Norms: 
                // TODO: since we don't write any norms file if there are no norms,
                // kinda jaky to assume the codec handles the case of no norms file at all gracefully?!

                if (fieldInfos.HasDocValues)
                {
                    dvProducer = codec.DocValuesFormat.FieldsProducer(segmentReadState);
                    //assert dvProducer != null;
                }
                else
                {
                    dvProducer = null;
                }

                if (fieldInfos.HasNorms)
                {
                    normsProducer = codec.NormsFormat.NormsProducer(segmentReadState);
                    //assert normsProducer != null;
                }
                else
                {
                    normsProducer = null;
                }

                fieldsReaderOrig = si.info.Codec.StoredFieldsFormat.FieldsReader(cfsDir, si.info, fieldInfos, context);

                if (fieldInfos.HasVectors)
                { // open term vector files only as needed
                    termVectorsReaderOrig = si.info.Codec.TermVectorsFormat.VectorsReader(cfsDir, si.info, fieldInfos, context);
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

            // Must assign this at the end -- if we hit an
            // exception above core, we don't want to attempt to
            // purge the FieldCache (will hit NPE because core is
            // not assigned yet).
            this.owner = owner;
        }

        internal void IncRef()
        {
            Interlocked.Increment(ref ref_renamed);
        }

        internal NumericDocValues GetNumericDocValues(String field)
        {
            FieldInfo fi = fieldInfos.FieldInfo(field);
            if (fi == null)
            {
                // Field does not exist
                return null;
            }
            if (fi.DocValuesTypeValue == null)
            {
                // Field was not indexed with doc values
                return null;
            }
            if (fi.DocValuesTypeValue != FieldInfo.DocValuesType.NUMERIC)
            {
                // DocValues were not numeric
                return null;
            }

            //assert dvProducer != null;

            IDictionary<String, Object> dvFields = docValuesLocal.Get();

            NumericDocValues dvs = (NumericDocValues)dvFields[field];
            if (dvs == null)
            {
                dvs = dvProducer.GetNumeric(fi);
                dvFields[field] = dvs;
            }

            return dvs;
        }

        internal BinaryDocValues GetBinaryDocValues(String field)
        {
            FieldInfo fi = fieldInfos.FieldInfo(field);
            if (fi == null)
            {
                // Field does not exist
                return null;
            }
            if (fi.DocValuesTypeValue == null)
            {
                // Field was not indexed with doc values
                return null;
            }
            if (fi.DocValuesTypeValue != FieldInfo.DocValuesType.BINARY)
            {
                // DocValues were not binary
                return null;
            }

            //assert dvProducer != null;

            IDictionary<String, Object> dvFields = docValuesLocal.Get();

            BinaryDocValues dvs = (BinaryDocValues)dvFields[field];
            if (dvs == null)
            {
                dvs = dvProducer.GetBinary(fi);
                dvFields[field] = dvs;
            }

            return dvs;
        }

        internal SortedDocValues GetSortedDocValues(String field)
        {
            FieldInfo fi = fieldInfos.FieldInfo(field);
            if (fi == null)
            {
                // Field does not exist
                return null;
            }
            if (fi.DocValuesTypeValue == null)
            {
                // Field was not indexed with doc values
                return null;
            }
            if (fi.DocValuesTypeValue != FieldInfo.DocValuesType.SORTED)
            {
                // DocValues were not sorted
                return null;
            }

            //assert dvProducer != null;

            IDictionary<String, Object> dvFields = docValuesLocal.Get();

            SortedDocValues dvs = (SortedDocValues)dvFields[field];
            if (dvs == null)
            {
                dvs = dvProducer.GetSorted(fi);
                dvFields[field] = dvs;
            }

            return dvs;
        }

        internal SortedSetDocValues GetSortedSetDocValues(String field)
        {
            FieldInfo fi = fieldInfos.FieldInfo(field);
            if (fi == null)
            {
                // Field does not exist
                return null;
            }
            if (fi.DocValuesTypeValue == null)
            {
                // Field was not indexed with doc values
                return null;
            }
            if (fi.DocValuesTypeValue != FieldInfo.DocValuesType.SORTED_SET)
            {
                // DocValues were not sorted
                return null;
            }

            //assert dvProducer != null;

            IDictionary<String, Object> dvFields = docValuesLocal.Get();

            SortedSetDocValues dvs = (SortedSetDocValues)dvFields[field];
            if (dvs == null)
            {
                dvs = dvProducer.GetSortedSet(fi);
                dvFields[field] = dvs;
            }

            return dvs;
        }

        internal NumericDocValues GetNormValues(String field)
        {
            FieldInfo fi = fieldInfos.FieldInfo(field);
            if (fi == null)
            {
                // Field does not exist
                return null;
            }
            if (!fi.HasNorms)
            {
                return null;
            }

            //assert normsProducer != null;

            IDictionary<String, Object> normFields = normsLocal.Get();

            NumericDocValues norms = (NumericDocValues)normFields[field];
            if (norms == null)
            {
                norms = normsProducer.GetNumeric(fi);
                normFields[field] = norms;
            }

            return norms;
        }

        internal void DecRef()
        {
            if (Interlocked.Decrement(ref ref_renamed) == 0)
            {
                IOUtils.Close(termVectorsLocal, fieldsReaderLocal, docValuesLocal, normsLocal, fields, dvProducer,
                              termVectorsReaderOrig, fieldsReaderOrig, cfsReader, normsProducer);
                NotifyCoreClosedListeners();
            }
        }

        private void NotifyCoreClosedListeners()
        {
            lock (coreClosedListeners)
            {
                foreach (ICoreClosedListener listener in coreClosedListeners)
                {
                    listener.OnClose(owner);
                }
            }
        }

        internal void AddCoreClosedListener(ICoreClosedListener listener)
        {
            coreClosedListeners.Add(listener);
        }

        internal void RemoveCoreClosedListener(ICoreClosedListener listener)
        {
            coreClosedListeners.Remove(listener);
        }

        public override string ToString()
        {
            return "SegmentCoreReader(owner=" + owner + ")";
        }
    }
}
