using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OrdinalMap = Lucene.Net.Index.MultiDocValues.OrdinalMap;
using MultiSortedDocValues = Lucene.Net.Index.MultiDocValues.MultiSortedDocValues;
using DocValuesType = Lucene.Net.Index.FieldInfo.DocValuesType;
using Lucene.Net.Support;

namespace Lucene.Net.Index
{
    public sealed class SlowCompositeReaderWrapper : AtomicReader
    {
        private readonly CompositeReader in_renamed;
        private readonly Fields fields;
        private readonly IBits liveDocs;

        public static AtomicReader Wrap(IndexReader reader)
        {
            if (reader is CompositeReader)
            {
                return new SlowCompositeReaderWrapper((CompositeReader)reader);
            }
            else
            {
                //assert reader instanceof AtomicReader;
                return (AtomicReader)reader;
            }
        }

        public SlowCompositeReaderWrapper(CompositeReader reader)
            : base()
        {
            in_renamed = reader;
            fields = MultiFields.GetFields(in_renamed);
            liveDocs = MultiFields.GetLiveDocs(in_renamed);
            in_renamed.RegisterParentReader(this);
        }

        public override string ToString()
        {
            return "SlowCompositeReaderWrapper(" + in_renamed + ")";
        }

        public override Fields Fields
        {
            get
            {
                EnsureOpen();
                return fields;
            }
        }

        public override NumericDocValues GetNumericDocValues(string field)
        {
            EnsureOpen();
            return MultiDocValues.GetNumericValues(in_renamed, field);
        }

        public override BinaryDocValues GetBinaryDocValues(string field)
        {
            EnsureOpen();
            return MultiDocValues.GetBinaryValues(in_renamed, field);
        }

        public override SortedDocValues GetSortedDocValues(string field)
        {
            EnsureOpen();
            OrdinalMap map = null;
            lock (cachedOrdMaps)
            {
                map = cachedOrdMaps[field];
                if (map == null)
                {
                    // uncached, or not a multi dv
                    SortedDocValues dv = MultiDocValues.GetSortedValues(in_renamed, field);
                    if (dv is MultiSortedDocValues)
                    {
                        map = ((MultiSortedDocValues)dv).mapping;
                        if (map.owner == CoreCacheKey)
                        {
                            cachedOrdMaps[field] = map;
                        }
                    }
                    return dv;
                }
            }
            // cached ordinal map
            if (FieldInfos.FieldInfo(field).DocValuesTypeValue != DocValuesType.SORTED)
            {
                return null;
            }
            int size = in_renamed.Leaves.Count;
            SortedDocValues[] values = new SortedDocValues[size];
            int[] starts = new int[size + 1];
            for (int i = 0; i < size; i++)
            {
                AtomicReaderContext context = in_renamed.Leaves[i];
                SortedDocValues v = ((AtomicReader)context.Reader).GetSortedDocValues(field);
                if (v == null)
                {
                    v = SortedDocValues.EMPTY;
                }
                values[i] = v;
                starts[i] = context.docBase;
            }
            starts[size] = MaxDoc;
            return new MultiSortedDocValues(values, starts, map);
        }

        public override SortedSetDocValues GetSortedSetDocValues(string field)
        {
            EnsureOpen();
            OrdinalMap map = null;
            lock (cachedOrdMaps)
            {
                map = cachedOrdMaps[field];
                if (map == null)
                {
                    // uncached, or not a multi dv
                    SortedSetDocValues dv = MultiDocValues.GetSortedSetValues(in_renamed, field);
                    if (dv is MultiDocValues.MultiSortedSetDocValues)
                    {
                        map = ((MultiDocValues.MultiSortedSetDocValues)dv).mapping;
                        if (map.owner == CoreCacheKey)
                        {
                            cachedOrdMaps[field] = map;
                        }
                    }
                    return dv;
                }
            }
            // cached ordinal map
            if (FieldInfos.FieldInfo(field).DocValuesTypeValue != DocValuesType.SORTED_SET)
            {
                return null;
            }
            //assert map != null;
            int size = in_renamed.Leaves.Count;
            SortedSetDocValues[] values = new SortedSetDocValues[size];
            int[] starts = new int[size + 1];
            for (int i = 0; i < size; i++)
            {
                AtomicReaderContext context = in_renamed.Leaves[i];
                SortedSetDocValues v = ((AtomicReader)context.Reader).GetSortedSetDocValues(field);
                if (v == null)
                {
                    v = SortedSetDocValues.EMPTY;
                }
                values[i] = v;
                starts[i] = context.docBase;
            }
            starts[size] = MaxDoc;
            return new MultiDocValues.MultiSortedSetDocValues(values, starts, map);
        }

        // TODO: this could really be a weak map somewhere else on the coreCacheKey,
        // but do we really need to optimize slow-wrapper any more?
        private readonly IDictionary<String, OrdinalMap> cachedOrdMaps = new HashMap<String, OrdinalMap>();

        public override NumericDocValues GetNormValues(string field)
        {
            EnsureOpen();
            return MultiDocValues.GetNormValues(in_renamed, field);
        }

        public override Fields GetTermVectors(int docID)
        {
            EnsureOpen();
            return in_renamed.GetTermVectors(docID);
        }

        public override int NumDocs
        {
            get { return in_renamed.NumDocs; }
        }

        public override int MaxDoc
        {
            get { return in_renamed.MaxDoc; }
        }

        public override void Document(int docID, StoredFieldVisitor visitor)
        {
            EnsureOpen();
            in_renamed.Document(docID, visitor);
        }

        public override IBits LiveDocs
        {
            get
            {
                EnsureOpen();
                return liveDocs;
            }
        }

        public override FieldInfos FieldInfos
        {
            get
            {
                EnsureOpen();
                return MultiFields.GetMergedFieldInfos(in_renamed);
            }
        }

        public override object CoreCacheKey
        {
            get
            {
                return in_renamed.CoreCacheKey;
            }
        }

        public override object CombinedCoreAndDeletesKey
        {
            get
            {
                return in_renamed.CombinedCoreAndDeletesKey;
            }
        }

        protected override void DoClose()
        {
            // TODO: as this is a wrapper, should we really close the delegate?
            in_renamed.Dispose();
        }
    }
}
