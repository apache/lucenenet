using Lucene.Net.Documents;
using System;
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

    using DocIdSetIterator = Lucene.Net.Search.DocIdSetIterator;
    using FixedBitSet = Lucene.Net.Util.FixedBitSet;
    using InPlaceMergeSorter = Lucene.Net.Util.InPlaceMergeSorter;
    using NumericDocValuesUpdate = Lucene.Net.Index.DocValuesUpdate.NumericDocValuesUpdate;
    using PackedInts = Lucene.Net.Util.Packed.PackedInts;
    using PagedGrowableWriter = Lucene.Net.Util.Packed.PagedGrowableWriter;
    using PagedMutable = Lucene.Net.Util.Packed.PagedMutable;

    /// <summary>
    /// A <seealso cref="AbstractDocValuesFieldUpdates"/> which holds updates of documents, of a single
    /// <seealso cref="NumericDocValuesField"/>.
    ///
    /// @lucene.experimental
    /// </summary>
    internal class NumericDocValuesFieldUpdates : AbstractDocValuesFieldUpdates
    {
        new internal sealed class Iterator : AbstractDocValuesFieldUpdates.IIterator
        {
            private readonly int size;
            private readonly PagedGrowableWriter values;
            private readonly FixedBitSet docsWithField;
            private readonly PagedMutable docs;
            private long idx = 0; // long so we don't overflow if size == Integer.MAX_VALUE
            private int doc = -1;
            private long? value = null;

            internal Iterator(int size, PagedGrowableWriter values, FixedBitSet docsWithField, PagedMutable docs)
            {
                this.size = size;
                this.values = values;
                this.docsWithField = docsWithField;
                this.docs = docs;
            }

            public object Value
            {
                get { return value; }
            }

            public int NextDoc()
            {
                if (idx >= size)
                {
                    value = null;
                    return doc = DocIdSetIterator.NO_MORE_DOCS;
                }
                doc = (int)docs.Get(idx);
                ++idx;
                while (idx < size && docs.Get(idx) == doc)
                {
                    ++idx;
                }
                if (!docsWithField.Get((int)(idx - 1)))
                {
                    value = null;
                }
                else
                {
                    // idx points to the "next" element
                    value = Convert.ToInt64(values.Get(idx - 1));
                }
                return doc;
            }

            public int Doc
            {
                get { return doc; }
            }

            public void Reset()
            {
                doc = -1;
                value = null;
                idx = 0;
            }
        }

        private FixedBitSet docsWithField;
        private PagedMutable docs;
        private PagedGrowableWriter values;
        private int size;

        public NumericDocValuesFieldUpdates(string field, int maxDoc)
            : base(field, DocValuesFieldUpdates.Type.NUMERIC)
        {
            docsWithField = new FixedBitSet(64);
            docs = new PagedMutable(1, 1024, PackedInts.BitsRequired(maxDoc - 1), PackedInts.COMPACT);
            values = new PagedGrowableWriter(1, 1024, 1, PackedInts.FAST);
            size = 0;
        }

        public override void Add(int doc, object value)
        {
            // TODO: if the Sorter interface changes to take long indexes, we can remove that limitation
            if (size == int.MaxValue)
            {
                throw new InvalidOperationException("cannot support more than Integer.MAX_VALUE doc/value entries");
            }

            long? val = (long?)value;
            if (val == null)
            {
                val = NumericDocValuesUpdate.MISSING;
            }

            // grow the structures to have room for more elements
            if (docs.Size() == size)
            {
                docs = docs.Grow(size + 1);
                values = values.Grow(size + 1);
                docsWithField = FixedBitSet.EnsureCapacity(docsWithField, (int)docs.Size());
            }

            if (val != NumericDocValuesUpdate.MISSING)
            {
                // only mark the document as having a value in that field if the value wasn't set to null (MISSING)
                docsWithField.Set(size);
            }

            docs.Set(size, doc);
            values.Set(size, (long)val);
            ++size;
        }

        public override AbstractDocValuesFieldUpdates.IIterator GetIterator()
        {
            PagedMutable docs = this.docs;
            PagedGrowableWriter values = this.values;
            FixedBitSet docsWithField = this.docsWithField;
            new InPlaceMergeSorterAnonymousInnerClassHelper(this, docs, values, docsWithField).Sort(0, size);

            return new Iterator(size, values, docsWithField, docs);
        }

        private class InPlaceMergeSorterAnonymousInnerClassHelper : InPlaceMergeSorter
        {
            private readonly NumericDocValuesFieldUpdates outerInstance;

            private PagedMutable docs;
            private PagedGrowableWriter values;
            private FixedBitSet docsWithField;

            public InPlaceMergeSorterAnonymousInnerClassHelper(NumericDocValuesFieldUpdates outerInstance, PagedMutable docs, PagedGrowableWriter values, FixedBitSet docsWithField)
            {
                this.outerInstance = outerInstance;
                this.docs = docs;
                this.values = values;
                this.docsWithField = docsWithField;
            }

            protected override void Swap(int i, int j)
            {
                long tmpDoc = docs.Get(j);
                docs.Set(j, docs.Get(i));
                docs.Set(i, tmpDoc);

                long tmpVal = values.Get(j);
                values.Set(j, values.Get(i));
                values.Set(i, tmpVal);

                bool tmpBool = docsWithField.Get(j);
                if (docsWithField.Get(i))
                {
                    docsWithField.Set(j);
                }
                else
                {
                    docsWithField.Clear(j);
                }
                if (tmpBool)
                {
                    docsWithField.Set(i);
                }
                else
                {
                    docsWithField.Clear(i);
                }
            }

            protected override int Compare(int i, int j)
            {
                int x = (int)docs.Get(i);
                int y = (int)docs.Get(j);
                return (x < y) ? -1 : ((x == y) ? 0 : 1);
            }
        }

        public override void Merge(AbstractDocValuesFieldUpdates other)
        {
            Debug.Assert(other is NumericDocValuesFieldUpdates);
            NumericDocValuesFieldUpdates otherUpdates = (NumericDocValuesFieldUpdates)other;
            if (size + otherUpdates.size > int.MaxValue)
            {
                throw new InvalidOperationException("cannot support more than Integer.MAX_VALUE doc/value entries; size=" + size + " other.size=" + otherUpdates.size);
            }
            docs = docs.Grow(size + otherUpdates.size);
            values = values.Grow(size + otherUpdates.size);
            docsWithField = FixedBitSet.EnsureCapacity(docsWithField, (int)docs.Size());
            for (int i = 0; i < otherUpdates.size; i++)
            {
                int doc = (int)otherUpdates.docs.Get(i);
                if (otherUpdates.docsWithField.Get(i))
                {
                    docsWithField.Set(size);
                }
                docs.Set(size, doc);
                values.Set(size, otherUpdates.values.Get(i));
                ++size;
            }
        }

        public override bool Any()
        {
            return size > 0;
        }
    }
}