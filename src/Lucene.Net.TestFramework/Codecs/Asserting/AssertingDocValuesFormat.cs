using System.Collections.Generic;
using System.Diagnostics;

namespace Lucene.Net.Codecs.Asserting
{
    using System;
    using AssertingAtomicReader = Lucene.Net.Index.AssertingAtomicReader;
    using BinaryDocValues = Lucene.Net.Index.BinaryDocValues;
    using IBits = Lucene.Net.Util.IBits;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using DocValuesType = Lucene.Net.Index.DocValuesType;
    using FieldInfo = Lucene.Net.Index.FieldInfo;
    using FixedBitSet = Lucene.Net.Util.FixedBitSet;
    using Int64BitSet = Lucene.Net.Util.Int64BitSet;

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

    using Lucene45DocValuesFormat = Lucene.Net.Codecs.Lucene45.Lucene45DocValuesFormat;
    using NumericDocValues = Lucene.Net.Index.NumericDocValues;
    using SegmentReadState = Lucene.Net.Index.SegmentReadState;
    using SegmentWriteState = Lucene.Net.Index.SegmentWriteState;
    using SortedDocValues = Lucene.Net.Index.SortedDocValues;
    using SortedSetDocValues = Lucene.Net.Index.SortedSetDocValues;

    /// <summary>
    /// Just like <see cref="Lucene45DocValuesFormat"/> but with additional asserts.
    /// </summary>
    [DocValuesFormatName("Asserting")]
    public class AssertingDocValuesFormat : DocValuesFormat
    {
        private readonly DocValuesFormat @in = new Lucene45DocValuesFormat();

        public AssertingDocValuesFormat()
            : base()
        {
        }

        public override DocValuesConsumer FieldsConsumer(SegmentWriteState state)
        {
            DocValuesConsumer consumer = @in.FieldsConsumer(state);
            Debug.Assert(consumer != null);
            return new AssertingDocValuesConsumer(consumer, state.SegmentInfo.DocCount);
        }

        public override DocValuesProducer FieldsProducer(SegmentReadState state)
        {
            Debug.Assert(state.FieldInfos.HasDocValues);
            DocValuesProducer producer = @in.FieldsProducer(state);
            Debug.Assert(producer != null);
            return new AssertingDocValuesProducer(producer, state.SegmentInfo.DocCount);
        }

        internal class AssertingDocValuesConsumer : DocValuesConsumer
        {
            internal readonly DocValuesConsumer @in;
            internal readonly int maxDoc;

            internal AssertingDocValuesConsumer(DocValuesConsumer @in, int maxDoc)
            {
                this.@in = @in;
                this.maxDoc = maxDoc;
            }

            public override void AddNumericField(FieldInfo field, IEnumerable<long?> values)
            {
                int count = 0;
                foreach (var v in values)
                {
                    count++;
                }
                Debug.Assert(count == maxDoc);
                CheckIterator(values.GetEnumerator(), maxDoc, true);
                @in.AddNumericField(field, values);
            }

            public override void AddBinaryField(FieldInfo field, IEnumerable<BytesRef> values)
            {
                int count = 0;
                foreach (BytesRef b in values)
                {
                    Debug.Assert(b == null || b.IsValid());
                    count++;
                }
                Debug.Assert(count == maxDoc);
                CheckIterator(values.GetEnumerator(), maxDoc, true);
                @in.AddBinaryField(field, values);
            }

            public override void AddSortedField(FieldInfo field, IEnumerable<BytesRef> values, IEnumerable<long?> docToOrd)
            {
                int valueCount = 0;
                BytesRef lastValue = null;
                foreach (BytesRef b in values)
                {
                    Debug.Assert(b != null);
                    Debug.Assert(b.IsValid());
                    if (valueCount > 0)
                    {
                        Debug.Assert(b.CompareTo(lastValue) > 0);
                    }
                    lastValue = BytesRef.DeepCopyOf(b);
                    valueCount++;
                }
                Debug.Assert(valueCount <= maxDoc);

                FixedBitSet seenOrds = new FixedBitSet(valueCount);

                int count = 0;
                foreach (long? v in docToOrd)
                {
                    Debug.Assert(v != null);
                    int ord = (int)v.Value;
                    Debug.Assert(ord >= -1 && ord < valueCount);
                    if (ord >= 0)
                    {
                        seenOrds.Set(ord);
                    }
                    count++;
                }

                Debug.Assert(count == maxDoc);
                Debug.Assert(seenOrds.Cardinality() == valueCount);
                CheckIterator(values.GetEnumerator(), valueCount, false);
                CheckIterator(docToOrd.GetEnumerator(), maxDoc, false);
                @in.AddSortedField(field, values, docToOrd);
            }

            public override void AddSortedSetField(FieldInfo field, IEnumerable<BytesRef> values, IEnumerable<long?> docToOrdCount, IEnumerable<long?> ords)
            {
                long valueCount = 0;
                BytesRef lastValue = null;
                foreach (BytesRef b in values)
                {
                    Debug.Assert(b != null);
                    Debug.Assert(b.IsValid());
                    if (valueCount > 0)
                    {
                        Debug.Assert(b.CompareTo(lastValue) > 0);
                    }
                    lastValue = BytesRef.DeepCopyOf(b);
                    valueCount++;
                }

                int docCount = 0;
                long ordCount = 0;
                Int64BitSet seenOrds = new Int64BitSet(valueCount);
                using (IEnumerator<long?> ordIterator = ords.GetEnumerator())
                {
                    foreach (long? v in docToOrdCount)
                    {
                        Debug.Assert(v != null);
                        int count = (int)v.Value;
                        Debug.Assert(count >= 0);
                        docCount++;
                        ordCount += count;

                        long lastOrd = -1;
                        for (int i = 0; i < count; i++)
                        {
                            ordIterator.MoveNext();
                            long? o = ordIterator.Current;
                            Debug.Assert(o != null);
                            long ord = o.Value;
                            Debug.Assert(ord >= 0 && ord < valueCount);
                            Debug.Assert(ord > lastOrd, "ord=" + ord + ",lastOrd=" + lastOrd);
                            seenOrds.Set(ord);
                            lastOrd = ord;
                        }
                    }
                    Debug.Assert(ordIterator.MoveNext() == false);

                    Debug.Assert(docCount == maxDoc);
                    Debug.Assert(seenOrds.Cardinality() == valueCount);
                    CheckIterator(values.GetEnumerator(), valueCount, false);
                    CheckIterator(docToOrdCount.GetEnumerator(), maxDoc, false);
                    CheckIterator(ords.GetEnumerator(), ordCount, false);
                    @in.AddSortedSetField(field, values, docToOrdCount, ords);
                }
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                    @in.Dispose();
            }
        }

        internal class AssertingNormsConsumer : DocValuesConsumer
        {
            internal readonly DocValuesConsumer @in;
            internal readonly int maxDoc;

            internal AssertingNormsConsumer(DocValuesConsumer @in, int maxDoc)
            {
                this.@in = @in;
                this.maxDoc = maxDoc;
            }

            public override void AddNumericField(FieldInfo field, IEnumerable<long?> values)
            {
                int count = 0;
                foreach (long? v in values)
                {
                    Debug.Assert(v != null);
                    count++;
                }
                Debug.Assert(count == maxDoc);
                CheckIterator(values.GetEnumerator(), maxDoc, false);
                @in.AddNumericField(field, values);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                    @in.Dispose();
            }

            public override void AddBinaryField(FieldInfo field, IEnumerable<BytesRef> values)
            {
                throw new InvalidOperationException();
            }

            public override void AddSortedField(FieldInfo field, IEnumerable<BytesRef> values, IEnumerable<long?> docToOrd)
            {
                throw new InvalidOperationException();
            }

            public override void AddSortedSetField(FieldInfo field, IEnumerable<BytesRef> values, IEnumerable<long?> docToOrdCount, IEnumerable<long?> ords)
            {
                throw new InvalidOperationException();
            }
        }

        private static void CheckIterator<T>(IEnumerator<T> iterator, long expectedSize, bool allowNull)
        {
            try
            {
                for (long i = 0; i < expectedSize; i++)
                {
                    bool hasNext = iterator.MoveNext();
                    Debug.Assert(hasNext);
                    T v = iterator.Current;
                    Debug.Assert(allowNull || v != null);
                    try
                    {
                        iterator.Reset();
                        throw new InvalidOperationException("broken iterator (supports remove): " + iterator);
                    }
                    catch (System.NotSupportedException)
                    {
                        // ok
                    }
                }
                Debug.Assert(!iterator.MoveNext());
                /*try
                {
                  //iterator.next();
                  throw new InvalidOperationException("broken iterator (allows next() when hasNext==false) " + iterator);
                }
                catch (Exception)
                {
                  // ok
                }*/
            }
            finally
            {
                iterator.Dispose();
            }
        }

        internal class AssertingDocValuesProducer : DocValuesProducer
        {
            internal readonly DocValuesProducer @in;
            internal readonly int maxDoc;

            internal AssertingDocValuesProducer(DocValuesProducer @in, int maxDoc)
            {
                this.@in = @in;
                this.maxDoc = maxDoc;
            }

            public override NumericDocValues GetNumeric(FieldInfo field)
            {
                Debug.Assert(field.DocValuesType == DocValuesType.NUMERIC || field.NormType == DocValuesType.NUMERIC);
                NumericDocValues values = @in.GetNumeric(field);
                Debug.Assert(values != null);
                return new AssertingAtomicReader.AssertingNumericDocValues(values, maxDoc);
            }

            public override BinaryDocValues GetBinary(FieldInfo field)
            {
                Debug.Assert(field.DocValuesType == DocValuesType.BINARY);
                BinaryDocValues values = @in.GetBinary(field);
                Debug.Assert(values != null);
                return new AssertingAtomicReader.AssertingBinaryDocValues(values, maxDoc);
            }

            public override SortedDocValues GetSorted(FieldInfo field)
            {
                Debug.Assert(field.DocValuesType == DocValuesType.SORTED);
                SortedDocValues values = @in.GetSorted(field);
                Debug.Assert(values != null);
                return new AssertingAtomicReader.AssertingSortedDocValues(values, maxDoc);
            }

            public override SortedSetDocValues GetSortedSet(FieldInfo field)
            {
                Debug.Assert(field.DocValuesType == DocValuesType.SORTED_SET);
                SortedSetDocValues values = @in.GetSortedSet(field);
                Debug.Assert(values != null);
                return new AssertingAtomicReader.AssertingSortedSetDocValues(values, maxDoc);
            }

            public override IBits GetDocsWithField(FieldInfo field)
            {
                Debug.Assert(field.DocValuesType != DocValuesType.NONE);
                IBits bits = @in.GetDocsWithField(field);
                Debug.Assert(bits != null);
                Debug.Assert(bits.Length == maxDoc);
                return new AssertingAtomicReader.AssertingBits(bits);
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                    @in.Dispose();
            }

            public override long RamBytesUsed()
            {
                return @in.RamBytesUsed();
            }

            public override void CheckIntegrity()
            {
                @in.CheckIntegrity();
            }
        }
    }
}