using System.Collections.Generic;
using System.Diagnostics;

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

    using Bits = Lucene.Net.Util.Bits;

    using DocValuesType = Lucene.Net.Index.FieldInfo.DocValuesType_e;
    using MultiSortedDocValues = Lucene.Net.Index.MultiDocValues.MultiSortedDocValues;
    using MultiSortedSetDocValues = Lucene.Net.Index.MultiDocValues.MultiSortedSetDocValues;
    using OrdinalMap = Lucene.Net.Index.MultiDocValues.OrdinalMap;

    /// <summary>
    /// this class forces a composite reader (eg a {@link
    /// MultiReader} or <seealso cref="DirectoryReader"/>) to emulate an
    /// atomic reader.  this requires implementing the postings
    /// APIs on-the-fly, using the static methods in {@link
    /// MultiFields}, <seealso cref="MultiDocValues"/>, by stepping through
    /// the sub-readers to merge fields/terms, appending docs, etc.
    ///
    /// <p><b>NOTE</b>: this class almost always results in a
    /// performance hit.  If this is important to your use case,
    /// you'll get better performance by gathering the sub readers using
    /// <seealso cref="IndexReader#getContext()"/> to get the
    /// atomic leaves and then operate per-AtomicReader,
    /// instead of using this class.
    /// </summary>
    public sealed class SlowCompositeReaderWrapper : AtomicReader
    {
        private readonly CompositeReader @in;
        private readonly Fields fields;
        private readonly Bits liveDocs;

        /// <summary>
        /// this method is sugar for getting an <seealso cref="AtomicReader"/> from
        /// an <seealso cref="IndexReader"/> of any kind. If the reader is already atomic,
        /// it is returned unchanged, otherwise wrapped by this class.
        /// </summary>
        public static AtomicReader Wrap(IndexReader reader)
        {
            CompositeReader compositeReader = reader as CompositeReader;
            if (compositeReader != null)
            {
                return new SlowCompositeReaderWrapper(compositeReader);
            }
            else
            {
                Debug.Assert(reader is AtomicReader);
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

        public override Bits GetDocsWithField(string field)
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
            lock (cachedOrdMaps)
            {
                if (!cachedOrdMaps.TryGetValue(field, out map))
                {
                    // uncached, or not a multi dv
                    SortedDocValues dv = MultiDocValues.GetSortedValues(@in, field);
                    MultiSortedDocValues docValues = dv as MultiSortedDocValues;
                    if (docValues != null)
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
            lock (cachedOrdMaps)
            {
                if (!cachedOrdMaps.TryGetValue(field, out map))
                {
                    // uncached, or not a multi dv
                    SortedSetDocValues dv = MultiDocValues.GetSortedSetValues(@in, field);
                    MultiSortedSetDocValues docValues = dv as MultiSortedSetDocValues;
                    if (docValues != null)
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
            // cached ordinal map
            if (FieldInfos.FieldInfo(field).DocValuesType != DocValuesType.SORTED_SET)
            {
                return null;
            }
            Debug.Assert(map != null);
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

        public override int NumDocs
        {
            get
            {
                // Don't call ensureOpen() here (it could affect performance)
                return @in.NumDocs;
            }
        }

        public override int MaxDoc
        {
            get
            {
                // Don't call ensureOpen() here (it could affect performance)
                return @in.MaxDoc;
            }
        }

        public override void Document(int docID, StoredFieldVisitor visitor)
        {
            EnsureOpen();
            @in.Document(docID, visitor);
        }

        public override Bits LiveDocs
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

        public override object CoreCacheKey
        {
            get
            {
                return @in.CoreCacheKey;
            }
        }

        public override object CombinedCoreAndDeletesKey
        {
            get
            {
                return @in.CombinedCoreAndDeletesKey;
            }
        }

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