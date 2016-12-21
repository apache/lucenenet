using System;
using Lucene.Net.Documents;

namespace Lucene.Net.Index
{
    using BinaryDocValuesField = BinaryDocValuesField;
    using BinaryDocValuesUpdate = Lucene.Net.Index.DocValuesUpdate.BinaryDocValuesUpdate;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
    using FixedBitSet = Lucene.Net.Util.FixedBitSet;
    using InPlaceMergeSorter = Lucene.Net.Util.InPlaceMergeSorter;
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
    /// <seealso cref="BinaryDocValuesField"/>.
    ///
    /// @lucene.experimental
    /// </summary>
    internal class BinaryDocValuesFieldUpdates : AbstractDocValuesFieldUpdates
    {
        new internal sealed class Iterator : AbstractDocValuesFieldUpdates.Iterator
        {
            private readonly PagedGrowableWriter Offsets;
            private readonly int Size;
            private readonly PagedGrowableWriter Lengths;
            private readonly PagedMutable Docs;
            private readonly FixedBitSet DocsWithField;
            private long Idx = 0; // long so we don't overflow if size == Integer.MAX_VALUE
            private int Doc_Renamed = -1;
            private readonly BytesRef Value_Renamed;
            private int Offset, Length;

            internal Iterator(int size, PagedGrowableWriter offsets, PagedGrowableWriter lengths, PagedMutable docs, BytesRef values, FixedBitSet docsWithField)
            {
                this.Offsets = offsets;
                this.Size = size;
                this.Lengths = lengths;
                this.Docs = docs;
                this.DocsWithField = docsWithField;
                Value_Renamed = (BytesRef)values.Clone();
            }

            public object Value()
            {
                if (Offset == -1)
                {
                    return null;
                }
                else
                {
                    Value_Renamed.Offset = Offset;
                    Value_Renamed.Length = Length;
                    return Value_Renamed;
                }
            }

            public int NextDoc()
            {
                if (Idx >= Size)
                {
                    Offset = -1;
                    return Doc_Renamed = DocIdSetIterator.NO_MORE_DOCS;
                }
                Doc_Renamed = (int)Docs.Get(Idx);
                ++Idx;
                while (Idx < Size && Docs.Get(Idx) == Doc_Renamed)
                {
                    ++Idx;
                }
                // idx points to the "next" element
                long prevIdx = Idx - 1;
                if (!DocsWithField.Get((int)prevIdx))
                {
                    Offset = -1;
                }
                else
                {
                    // cannot change 'value' here because nextDoc is called before the
                    // value is used, and it's a waste to clone the BytesRef when we
                    // obtain the value
                    Offset = (int)Offsets.Get(prevIdx);
                    Length = (int)Lengths.Get(prevIdx);
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
                Offset = -1;
                Idx = 0;
            }
        }

        private FixedBitSet DocsWithField;
        private PagedMutable Docs;
        private PagedGrowableWriter Offsets, Lengths;
        private BytesRef Values;
        private int Size;

        public BinaryDocValuesFieldUpdates(string field, int maxDoc)
            : base(field, DocValuesFieldUpdates.Type_e.BINARY)
        {
            DocsWithField = new FixedBitSet(64);
            Docs = new PagedMutable(1, 1024, PackedInts.BitsRequired(maxDoc - 1), PackedInts.COMPACT);
            Offsets = new PagedGrowableWriter(1, 1024, 1, PackedInts.FAST);
            Lengths = new PagedGrowableWriter(1, 1024, 1, PackedInts.FAST);
            Values = new BytesRef(16); // start small
            Size = 0;
        }

        public override void Add(int doc, object value)
        {
            // TODO: if the Sorter interface changes to take long indexes, we can remove that limitation
            if (Size == int.MaxValue)
            {
                throw new InvalidOperationException("cannot support more than Integer.MAX_VALUE doc/value entries");
            }

            BytesRef val = (BytesRef)value;
            if (val == null)
            {
                val = BinaryDocValuesUpdate.MISSING;
            }

            // grow the structures to have room for more elements
            if (Docs.Size() == Size)
            {
                Docs = Docs.Grow(Size + 1);
                Offsets = Offsets.Grow(Size + 1);
                Lengths = Lengths.Grow(Size + 1);
                DocsWithField = FixedBitSet.EnsureCapacity(DocsWithField, (int)Docs.Size());
            }

            if (val != BinaryDocValuesUpdate.MISSING)
            {
                // only mark the document as having a value in that field if the value wasn't set to null (MISSING)
                DocsWithField.Set(Size);
            }

            Docs.Set(Size, doc);
            Offsets.Set(Size, Values.Length);
            Lengths.Set(Size, val.Length);
            Values.Append(val);
            ++Size;
        }

        public override AbstractDocValuesFieldUpdates.Iterator GetIterator()
        {
            PagedMutable docs = this.Docs;
            PagedGrowableWriter offsets = this.Offsets;
            PagedGrowableWriter lengths = this.Lengths;
            BytesRef values = this.Values;
            FixedBitSet docsWithField = this.DocsWithField;
            new InPlaceMergeSorterAnonymousInnerClassHelper(this, docs, offsets, lengths, docsWithField).Sort(0, Size);

            return new Iterator(Size, offsets, lengths, docs, values, docsWithField);
        }

        private class InPlaceMergeSorterAnonymousInnerClassHelper : InPlaceMergeSorter
        {
            private readonly BinaryDocValuesFieldUpdates OuterInstance;

            private PagedMutable Docs;
            private PagedGrowableWriter Offsets;
            private PagedGrowableWriter Lengths;
            private FixedBitSet DocsWithField;

            public InPlaceMergeSorterAnonymousInnerClassHelper(BinaryDocValuesFieldUpdates outerInstance, PagedMutable docs, PagedGrowableWriter offsets, PagedGrowableWriter lengths, FixedBitSet docsWithField)
            {
                this.OuterInstance = outerInstance;
                this.Docs = docs;
                this.Offsets = offsets;
                this.Lengths = lengths;
                this.DocsWithField = docsWithField;
            }

            protected override void Swap(int i, int j)
            {
                long tmpDoc = Docs.Get(j);
                Docs.Set(j, Docs.Get(i));
                Docs.Set(i, tmpDoc);

                long tmpOffset = Offsets.Get(j);
                Offsets.Set(j, Offsets.Get(i));
                Offsets.Set(i, tmpOffset);

                long tmpLength = Lengths.Get(j);
                Lengths.Set(j, Lengths.Get(i));
                Lengths.Set(i, tmpLength);

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
            BinaryDocValuesFieldUpdates otherUpdates = (BinaryDocValuesFieldUpdates)other;
            int newSize = Size + otherUpdates.Size;
            if (newSize > int.MaxValue)
            {
                throw new InvalidOperationException("cannot support more than Integer.MAX_VALUE doc/value entries; size=" + Size + " other.size=" + otherUpdates.Size);
            }
            Docs = Docs.Grow(newSize);
            Offsets = Offsets.Grow(newSize);
            Lengths = Lengths.Grow(newSize);
            DocsWithField = FixedBitSet.EnsureCapacity(DocsWithField, (int)Docs.Size());
            for (int i = 0; i < otherUpdates.Size; i++)
            {
                int doc = (int)otherUpdates.Docs.Get(i);
                if (otherUpdates.DocsWithField.Get(i))
                {
                    DocsWithField.Set(Size);
                }
                Docs.Set(Size, doc);
                Offsets.Set(Size, Values.Length + otherUpdates.Offsets.Get(i)); // correct relative offset
                Lengths.Set(Size, otherUpdates.Lengths.Get(i));
                ++Size;
            }
            Values.Append(otherUpdates.Values);
        }

        public override bool Any()
        {
            return Size > 0;
        }
    }
}