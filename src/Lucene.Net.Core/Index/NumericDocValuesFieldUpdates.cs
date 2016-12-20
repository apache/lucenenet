using System;
using System.Diagnostics;
using Lucene.Net.Documents;

namespace Lucene.Net.Index
{
    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
    using FixedBitSet = Lucene.Net.Util.FixedBitSet;
    using InPlaceMergeSorter = Lucene.Net.Util.InPlaceMergeSorter;
    using NumericDocValuesField = NumericDocValuesField;
    using NumericDocValuesUpdate = Lucene.Net.Index.DocValuesUpdate.NumericDocValuesUpdate;
    using PackedInts = Lucene.Net.Util.Packed.PackedInts;
    using PagedGrowableWriter = Lucene.Net.Util.Packed.PagedGrowableWriter;
    using PagedMutable = Lucene.Net.Util.Packed.PagedMutable;

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

    /// <summary>
    /// A <seealso cref="AbstractDocValuesFieldUpdates"/> which holds updates of documents, of a single
    /// <seealso cref="NumericDocValuesField"/>.
    ///
    /// @lucene.experimental
    /// </summary>
    internal class NumericDocValuesFieldUpdates : AbstractDocValuesFieldUpdates
    {
        internal sealed class Iterator : AbstractDocValuesFieldUpdates.Iterator
        {
            internal readonly int Size;
            internal readonly PagedGrowableWriter Values;
            internal readonly FixedBitSet DocsWithField;
            internal readonly PagedMutable Docs;
            internal long Idx = 0; // long so we don't overflow if size == Integer.MAX_VALUE
            internal int Doc_Renamed = -1;
            internal long? Value_Renamed = null;

            internal Iterator(int size, PagedGrowableWriter values, FixedBitSet docsWithField, PagedMutable docs)
            {
                this.Size = size;
                this.Values = values;
                this.DocsWithField = docsWithField;
                this.Docs = docs;
            }

            public object Value()
            {
                return Value_Renamed;
            }

            public int NextDoc()
            {
                if (Idx >= Size)
                {
                    Value_Renamed = null;
                    return Doc_Renamed = DocIdSetIterator.NO_MORE_DOCS;
                }
                Doc_Renamed = (int)Docs.Get(Idx);
                ++Idx;
                while (Idx < Size && Docs.Get(Idx) == Doc_Renamed)
                {
                    ++Idx;
                }
                if (!DocsWithField.Get((int)(Idx - 1)))
                {
                    Value_Renamed = null;
                }
                else
                {
                    // idx points to the "next" element
                    Value_Renamed = Convert.ToInt64(Values.Get(Idx - 1));
                }
                return Doc_Renamed;
            }

            public int Doc()
            {
                return Doc_Renamed;
            }

            public void Reset()
            {
                Doc_Renamed = -1;
                Value_Renamed = null;
                Idx = 0;
            }
        }

        private FixedBitSet DocsWithField;
        private PagedMutable Docs;
        private PagedGrowableWriter Values;
        private int Size;

        public NumericDocValuesFieldUpdates(string field, int maxDoc)
            : base(field, DocValuesFieldUpdates.Type_e.NUMERIC)
        {
            DocsWithField = new FixedBitSet(64);
            Docs = new PagedMutable(1, 1024, PackedInts.BitsRequired(maxDoc - 1), PackedInts.COMPACT);
            Values = new PagedGrowableWriter(1, 1024, 1, PackedInts.FAST);
            Size = 0;
        }

        public override void Add(int doc, object value)
        {
            // TODO: if the Sorter interface changes to take long indexes, we can remove that limitation
            if (Size == int.MaxValue)
            {
                throw new InvalidOperationException("cannot support more than Integer.MAX_VALUE doc/value entries");
            }

            long? val = (long?)value;
            if (val == null)
            {
                val = NumericDocValuesUpdate.MISSING;
            }

            // grow the structures to have room for more elements
            if (Docs.Size() == Size)
            {
                Docs = Docs.Grow(Size + 1);
                Values = Values.Grow(Size + 1);
                DocsWithField = FixedBitSet.EnsureCapacity(DocsWithField, (int)Docs.Size());
            }

            if (val != NumericDocValuesUpdate.MISSING)
            {
                // only mark the document as having a value in that field if the value wasn't set to null (MISSING)
                DocsWithField.Set(Size);
            }

            Docs.Set(Size, doc);
            Values.Set(Size, (long)val);
            ++Size;
        }

        public override AbstractDocValuesFieldUpdates.Iterator GetIterator()
        {
            PagedMutable docs = this.Docs;
            PagedGrowableWriter values = this.Values;
            FixedBitSet docsWithField = this.DocsWithField;
            new InPlaceMergeSorterAnonymousInnerClassHelper(this, docs, values, docsWithField).Sort(0, Size);

            return new Iterator(Size, values, docsWithField, docs);
        }

        private class InPlaceMergeSorterAnonymousInnerClassHelper : InPlaceMergeSorter
        {
            private readonly NumericDocValuesFieldUpdates OuterInstance;

            private PagedMutable Docs;
            private PagedGrowableWriter Values;
            private FixedBitSet DocsWithField;

            public InPlaceMergeSorterAnonymousInnerClassHelper(NumericDocValuesFieldUpdates outerInstance, PagedMutable docs, PagedGrowableWriter values, FixedBitSet docsWithField)
            {
                this.OuterInstance = outerInstance;
                this.Docs = docs;
                this.Values = values;
                this.DocsWithField = docsWithField;
            }

            protected override void Swap(int i, int j)
            {
                long tmpDoc = Docs.Get(j);
                Docs.Set(j, Docs.Get(i));
                Docs.Set(i, tmpDoc);

                long tmpVal = Values.Get(j);
                Values.Set(j, Values.Get(i));
                Values.Set(i, tmpVal);

                bool tmpBool = DocsWithField.Get(j);
                if (DocsWithField.Get(i))
                {
                    DocsWithField.Set(j);
                }
                else
                {
                    DocsWithField.Clear(j);
                }
                if (tmpBool)
                {
                    DocsWithField.Set(i);
                }
                else
                {
                    DocsWithField.Clear(i);
                }
            }

            protected override int Compare(int i, int j)
            {
                int x = (int)Docs.Get(i);
                int y = (int)Docs.Get(j);
                return (x < y) ? -1 : ((x == y) ? 0 : 1);
            }
        }

        public override void Merge(AbstractDocValuesFieldUpdates other)
        {
            Debug.Assert(other is NumericDocValuesFieldUpdates);
            NumericDocValuesFieldUpdates otherUpdates = (NumericDocValuesFieldUpdates)other;
            if (Size + otherUpdates.Size > int.MaxValue)
            {
                throw new InvalidOperationException("cannot support more than Integer.MAX_VALUE doc/value entries; size=" + Size + " other.size=" + otherUpdates.Size);
            }
            Docs = Docs.Grow(Size + otherUpdates.Size);
            Values = Values.Grow(Size + otherUpdates.Size);
            DocsWithField = FixedBitSet.EnsureCapacity(DocsWithField, (int)Docs.Size());
            for (int i = 0; i < otherUpdates.Size; i++)
            {
                int doc = (int)otherUpdates.Docs.Get(i);
                if (otherUpdates.DocsWithField.Get(i))
                {
                    DocsWithField.Set(Size);
                }
                Docs.Set(Size, doc);
                Values.Set(Size, otherUpdates.Values.Get(i));
                ++Size;
            }
        }

        public override bool Any()
        {
            return Size > 0;
        }
    }
}