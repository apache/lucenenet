using Lucene.Net.Diagnostics;
using Lucene.Net.Support.Threading;
using System.Collections.Generic;

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

    using IBits = Lucene.Net.Util.IBits;
    using MultiSortedDocValues = Lucene.Net.Index.MultiDocValues.MultiSortedDocValues;
    using MultiSortedSetDocValues = Lucene.Net.Index.MultiDocValues.MultiSortedSetDocValues;
    using OrdinalMap = Lucene.Net.Index.MultiDocValues.OrdinalMap;

    /// <summary>
    /// This class forces a composite reader (eg a 
    /// <see cref="MultiReader"/> or <see cref="DirectoryReader"/>) to emulate an
    /// atomic reader.  This requires implementing the postings
    /// APIs on-the-fly, using the static methods in 
    /// <see cref="MultiFields"/>, <see cref="MultiDocValues"/>, by stepping through
    /// the sub-readers to merge fields/terms, appending docs, etc.
    ///
    /// <para/><b>NOTE</b>: This class almost always results in a
    /// performance hit.  If this is important to your use case,
    /// you'll get better performance by gathering the sub readers using
    /// <see cref="IndexReader.Context"/> to get the
    /// atomic leaves and then operate per-AtomicReader,
    /// instead of using this class.
    /// </summary>
    public sealed class SlowCompositeReaderWrapper : AtomicReader
    {
        private readonly CompositeReader @in;
        private readonly Fields fields;
        private readonly IBits liveDocs;

        /// <summary>
        /// This method is sugar for getting an <see cref="AtomicReader"/> from
        /// an <see cref="IndexReader"/> of any kind. If the reader is already atomic,
        /// it is returned unchanged, otherwise wrapped by this class.
        /// </summary>
        public static AtomicReader Wrap(IndexReader reader)
        {
            if (reader is CompositeReader compositeReader)
            {
                return new SlowCompositeReaderWrapper(compositeReader);
            }
            else
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(reader is AtomicReader);
                return (AtomicReader)reader;
            }
        }

        private SlowCompositeReaderWrapper(CompositeReader reader)
            : base()
        {
            @in = reader;
            fields = MultiFields.GetFields(@in);
            liveDocs = MultiFields.GetLiveDocs(@in);
            @in.RegisterParentReader(this);
        }

        public override string ToString()
        {
            return "SlowCompositeReaderWrapper(" + @in + ")";
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
            return MultiDocValues.GetNumericValues(@in, field);
        }

        public override IBits GetDocsWithField(string field)
        {
            EnsureOpen();
            return MultiDocValues.GetDocsWithField(@in, field);
        }

        public override BinaryDocValues GetBinaryDocValues(string field)
        {
            EnsureOpen();
            return MultiDocValues.GetBinaryValues(@in, field);
        }

        public override SortedDocValues GetSortedDocValues(string field)
        {
            EnsureOpen();
            OrdinalMap map = null;
            UninterruptableMonitor.Enter(cachedOrdMaps);
            try
            {
                if (!cachedOrdMaps.TryGetValue(field, out map))
                {
                    // uncached, or not a multi dv
                    SortedDocValues dv = MultiDocValues.GetSortedValues(@in, field);
                    if (dv is MultiSortedDocValues docValues)
                    {
                        map = docValues.Mapping;
                        if (map.owner == CoreCacheKey)
                        {
                            cachedOrdMaps[field] = map;
                        }
                    }
                    return dv;
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(cachedOrdMaps);
            }
            // cached ordinal map
            if (FieldInfos.FieldInfo(field).DocValuesType != DocValuesType.SORTED)
            {
                return null;
            }
            int size = @in.Leaves.Count;
            SortedDocValues[] values = new SortedDocValues[size];
            int[] starts = new int[size + 1];
            for (int i = 0; i < size; i++)
            {
                AtomicReaderContext context = @in.Leaves[i];
                SortedDocValues v = context.AtomicReader.GetSortedDocValues(field) ?? DocValues.EMPTY_SORTED;
                values[i] = v;
                starts[i] = context.DocBase;
            }
            starts[size] = MaxDoc;
            return new MultiSortedDocValues(values, starts, map);
        }

        public override SortedSetDocValues GetSortedSetDocValues(string field)
        {
            EnsureOpen();
            OrdinalMap map = null;
            UninterruptableMonitor.Enter(cachedOrdMaps);
            try
            {
                if (!cachedOrdMaps.TryGetValue(field, out map))
                {
                    // uncached, or not a multi dv
                    SortedSetDocValues dv = MultiDocValues.GetSortedSetValues(@in, field);
                    if (dv is MultiSortedSetDocValues docValues)
                    {
                        map = docValues.Mapping;
                        if (map.owner == CoreCacheKey)
                        {
                            cachedOrdMaps[field] = map;
                        }
                    }
                    return dv;
                }
            }
            finally
            {
                UninterruptableMonitor.Exit(cachedOrdMaps);
            }
            // cached ordinal map
            if (FieldInfos.FieldInfo(field).DocValuesType != DocValuesType.SORTED_SET)
            {
                return null;
            }
            if (Debugging.AssertsEnabled) Debugging.Assert(map != null);
            int size = @in.Leaves.Count;
            var values = new SortedSetDocValues[size];
            int[] starts = new int[size + 1];
            for (int i = 0; i < size; i++)
            {
                AtomicReaderContext context = @in.Leaves[i];
                SortedSetDocValues v = context.AtomicReader.GetSortedSetDocValues(field) ?? DocValues.EMPTY_SORTED_SET;
                values[i] = v;
                starts[i] = context.DocBase;
            }
            starts[size] = MaxDoc;
            return new MultiSortedSetDocValues(values, starts, map);
        }

        // TODO: this could really be a weak map somewhere else on the coreCacheKey,
        // but do we really need to optimize slow-wrapper any more?
        private readonly IDictionary<string, OrdinalMap> cachedOrdMaps = new Dictionary<string, OrdinalMap>();

        public override NumericDocValues GetNormValues(string field)
        {
            EnsureOpen();
            return MultiDocValues.GetNormValues(@in, field);
        }

        public override Fields GetTermVectors(int docID)
        {
            EnsureOpen();
            return @in.GetTermVectors(docID);
        }

        public override int NumDocs =>
            // Don't call ensureOpen() here (it could affect performance)
            @in.NumDocs;

        public override int MaxDoc =>
            // Don't call ensureOpen() here (it could affect performance)
            @in.MaxDoc;

        public override void Document(int docID, StoredFieldVisitor visitor)
        {
            EnsureOpen();
            @in.Document(docID, visitor);
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
                return MultiFields.GetMergedFieldInfos(@in);
            }
        }

        public override object CoreCacheKey => @in.CoreCacheKey;

        public override object CombinedCoreAndDeletesKey => @in.CombinedCoreAndDeletesKey;

        protected internal override void DoClose()
        {
            // TODO: as this is a wrapper, should we really close the delegate?
            @in.Dispose();
        }

        public override void CheckIntegrity()
        {
            EnsureOpen();
            foreach (AtomicReaderContext ctx in @in.Leaves)
            {
                ctx.AtomicReader.CheckIntegrity();
            }
        }
    }
}