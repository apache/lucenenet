using Lucene.Net.Diagnostics;
using Lucene.Net.Documents;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Lucene.Net.Util.Packed;
using System.Runtime.CompilerServices;

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

    /// <summary>
    /// A <see cref="DocValuesFieldUpdates"/> which holds updates of documents, of a single
    /// <see cref="NumericDocValuesField"/>.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    internal class NumericDocValuesFieldUpdates : DocValuesFieldUpdates
    {
        internal sealed class Iterator : DocValuesFieldUpdatesIterator<long?>
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

            public override long? Value => value;

            public override int NextDoc()
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
                    value = values.Get(idx - 1);
                }
                return doc;
            }

            public override int Doc => doc;

            public override void Reset()
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
            : base(field, DocValuesFieldUpdatesType.NUMERIC)
        {
            docsWithField = new FixedBitSet(64);
            docs = new PagedMutable(1, 1024, PackedInt32s.BitsRequired(maxDoc - 1), PackedInt32s.COMPACT);
            values = new PagedGrowableWriter(1, 1024, 1, PackedInt32s.FAST);
            size = 0;
        }

        // LUCENENET specific: Pass iterator instead of the value, since this class knows the type to retrieve, but the caller does not.
        public override void AddFromIterator(int doc, DocValuesFieldUpdatesIterator iterator)
        {
            Add(doc, ((Iterator)iterator).Value);
        }

        // LUCENENET specific: Pass DocValuesUpdate instead of the value, since this class knows the type to retrieve, but the caller does not.
        public override void AddFromUpdate(int doc, DocValuesUpdate update)
        {
            Add(doc, ((NumericDocValuesUpdate)update).value);
        }

        private void Add(int doc, long? value) // LUCENENET specific: Marked private instead of public and changed the value parameter type
        {
            // TODO: if the Sorter interface changes to take long indexes, we can remove that limitation
            if (size == int.MaxValue)
            {
                throw IllegalStateException.Create("cannot support more than System.Int32.MaxValue doc/value entries");
            }

            long? val = value;
            if (val is null)
            {
                val = NumericDocValuesUpdate.MISSING;
            }

            // grow the structures to have room for more elements
            if (docs.Count == size)
            {
                docs = docs.Grow(size + 1);
                values = values.Grow(size + 1);
                docsWithField = FixedBitSet.EnsureCapacity(docsWithField, (int)docs.Count);
            }

            if (val != NumericDocValuesUpdate.MISSING)
            {
                // only mark the document as having a value in that field if the value wasn't set to null (MISSING)
                docsWithField.Set(size);
            }

            docs.Set(size, doc);
            values.Set(size, val.Value);
            ++size;
        }

        public override DocValuesFieldUpdatesIterator GetIterator()
        {
            PagedMutable docs = this.docs;
            PagedGrowableWriter values = this.values;
            FixedBitSet docsWithField = this.docsWithField;
            new InPlaceMergeSorterAnonymousClass(docs, values, docsWithField).Sort(0, size);

            return new Iterator(size, values, docsWithField, docs);
        }

        private sealed class InPlaceMergeSorterAnonymousClass : InPlaceMergeSorter
        {
            private readonly PagedMutable docs;
            private readonly PagedGrowableWriter values;
            private readonly FixedBitSet docsWithField;

            public InPlaceMergeSorterAnonymousClass(PagedMutable docs, PagedGrowableWriter values, FixedBitSet docsWithField)
            {
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

        [MethodImpl(MethodImplOptions.NoInlining)]
        public override void Merge(DocValuesFieldUpdates other)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(other is NumericDocValuesFieldUpdates);
            NumericDocValuesFieldUpdates otherUpdates = (NumericDocValuesFieldUpdates)other;
            if (size + otherUpdates.size > int.MaxValue)
            {
                throw IllegalStateException.Create("cannot support more than System.Int32.MaxValue doc/value entries; size=" + size + " other.size=" + otherUpdates.size);
            }
            docs = docs.Grow(size + otherUpdates.size);
            values = values.Grow(size + otherUpdates.size);
            docsWithField = FixedBitSet.EnsureCapacity(docsWithField, (int)docs.Count);
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