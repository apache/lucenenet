using Lucene.Net.Codecs.Lucene45;
using Lucene.Net.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;

namespace Lucene.Net.Codecs.Asserting
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
            if (Debugging.AssertsEnabled) Debugging.ThrowAssertIf(consumer != null);
            return new AssertingDocValuesConsumer(consumer, state.SegmentInfo.DocCount);
        }

        public override DocValuesProducer FieldsProducer(SegmentReadState state)
        {
            if (Debugging.AssertsEnabled) Debugging.ThrowAssertIf(state.FieldInfos.HasDocValues);
            DocValuesProducer producer = @in.FieldsProducer(state);
            if (Debugging.AssertsEnabled) Debugging.ThrowAssertIf(producer != null);
            return new AssertingDocValuesProducer(producer, state.SegmentInfo.DocCount);
        }

        internal class AssertingDocValuesConsumer : DocValuesConsumer
        {
            private readonly DocValuesConsumer @in;
            private readonly int maxDoc;

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
                if (Debugging.AssertsEnabled) Debugging.ThrowAssertIf(count == maxDoc);
                CheckIterator(values.GetEnumerator(), maxDoc, true);
                @in.AddNumericField(field, values);
            }

            public override void AddBinaryField(FieldInfo field, IEnumerable<BytesRef> values)
            {
                int count = 0;
                foreach (BytesRef b in values)
                {
                    if (Debugging.AssertsEnabled) Debugging.ThrowAssertIf(b == null || b.IsValid());
                    count++;
                }
                if (Debugging.AssertsEnabled) Debugging.ThrowAssertIf(count == maxDoc);
                CheckIterator(values.GetEnumerator(), maxDoc, true);
                @in.AddBinaryField(field, values);
            }

            public override void AddSortedField(FieldInfo field, IEnumerable<BytesRef> values, IEnumerable<long?> docToOrd)
            {
                int valueCount = 0;
                BytesRef lastValue = null;
                foreach (BytesRef b in values)
                {
                    if (Debugging.AssertsEnabled) Debugging.ThrowAssertIf(b != null);
                    if (Debugging.AssertsEnabled) Debugging.ThrowAssertIf(b.IsValid());
                    if (valueCount > 0)
                    {
                        if (Debugging.AssertsEnabled) Debugging.ThrowAssertIf(b.CompareTo(lastValue) > 0);
                    }
                    lastValue = BytesRef.DeepCopyOf(b);
                    valueCount++;
                }
                if (Debugging.AssertsEnabled) Debugging.ThrowAssertIf(valueCount <= maxDoc);

                FixedBitSet seenOrds = new FixedBitSet(valueCount);

                int count = 0;
                foreach (long? v in docToOrd)
                {
                    if (Debugging.AssertsEnabled) Debugging.ThrowAssertIf(v != null);
                    int ord = (int)v.Value;
                    if (Debugging.AssertsEnabled) Debugging.ThrowAssertIf(ord >= -1 && ord < valueCount);
                    if (ord >= 0)
                    {
                        seenOrds.Set(ord);
                    }
                    count++;
                }

                if (Debugging.AssertsEnabled) Debugging.ThrowAssertIf(count == maxDoc);
                if (Debugging.AssertsEnabled) Debugging.ThrowAssertIf(seenOrds.Cardinality() == valueCount);
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
                    if (Debugging.AssertsEnabled) Debugging.ThrowAssertIf(b != null);
                    if (Debugging.AssertsEnabled) Debugging.ThrowAssertIf(b.IsValid());
                    if (valueCount > 0)
                    {
                        if (Debugging.AssertsEnabled) Debugging.ThrowAssertIf(b.CompareTo(lastValue) > 0);
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
                        if (Debugging.AssertsEnabled) Debugging.ThrowAssertIf(v != null);
                        int count = (int)v.Value;
                        if (Debugging.AssertsEnabled) Debugging.ThrowAssertIf(count >= 0);
                        docCount++;
                        ordCount += count;

                        long lastOrd = -1;
                        for (int i = 0; i < count; i++)
                        {
                            ordIterator.MoveNext();
                            long? o = ordIterator.Current;
                            if (Debugging.AssertsEnabled) Debugging.ThrowAssertIf(o != null);
                            long ord = o.Value;
                            if (Debugging.AssertsEnabled) Debugging.ThrowAssertIf(ord >= 0 && ord < valueCount);
                            if (Debugging.AssertsEnabled && Debugging.ShouldAssert(ord > lastOrd)) Debugging.ThrowAssert("ord={0},lastOrd={1}", ord, lastOrd);
                            seenOrds.Set(ord);
                            lastOrd = ord;
                        }
                    }
                    if (Debugging.AssertsEnabled) Debugging.ThrowAssertIf(ordIterator.MoveNext() == false);

                    if (Debugging.AssertsEnabled) Debugging.ThrowAssertIf(docCount == maxDoc);
                    if (Debugging.AssertsEnabled) Debugging.ThrowAssertIf(seenOrds.Cardinality() == valueCount);
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
            private readonly DocValuesConsumer @in;
            private readonly int maxDoc;

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
                    if (Debugging.AssertsEnabled) Debugging.ThrowAssertIf(v != null);
                    count++;
                }
                if (Debugging.AssertsEnabled) Debugging.ThrowAssertIf(count == maxDoc);
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
                    if (Debugging.AssertsEnabled) Debugging.ThrowAssertIf(hasNext);
                    T v = iterator.Current;
                    if (Debugging.AssertsEnabled) Debugging.ThrowAssertIf(allowNull || v != null);

                    // LUCENE.NET specific. removed call to Reset().
                    //try
                    //{
                    //    iterator.Reset();
                    //    throw new InvalidOperationException("broken iterator (supports remove): " + iterator);
                    //}
                    //catch (NotSupportedException)
                    //{
                    //    // ok
                    //}
                }
                if (Debugging.AssertsEnabled) Debugging.ThrowAssertIf(!iterator.MoveNext());
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
            private readonly DocValuesProducer @in;
            private readonly int maxDoc;

            internal AssertingDocValuesProducer(DocValuesProducer @in, int maxDoc)
            {
                this.@in = @in;
                this.maxDoc = maxDoc;
            }

            public override NumericDocValues GetNumeric(FieldInfo field)
            {
                if (Debugging.AssertsEnabled) Debugging.ThrowAssertIf(field.DocValuesType == DocValuesType.NUMERIC || field.NormType == DocValuesType.NUMERIC);
                NumericDocValues values = @in.GetNumeric(field);
                if (Debugging.AssertsEnabled) Debugging.ThrowAssertIf(values != null);
                return new AssertingNumericDocValues(values, maxDoc);
            }

            public override BinaryDocValues GetBinary(FieldInfo field)
            {
                if (Debugging.AssertsEnabled) Debugging.ThrowAssertIf(field.DocValuesType == DocValuesType.BINARY);
                BinaryDocValues values = @in.GetBinary(field);
                if (Debugging.AssertsEnabled) Debugging.ThrowAssertIf(values != null);
                return new AssertingBinaryDocValues(values, maxDoc);
            }

            public override SortedDocValues GetSorted(FieldInfo field)
            {
                if (Debugging.AssertsEnabled) Debugging.ThrowAssertIf(field.DocValuesType == DocValuesType.SORTED);
                SortedDocValues values = @in.GetSorted(field);
                if (Debugging.AssertsEnabled) Debugging.ThrowAssertIf(values != null);
                return new AssertingSortedDocValues(values, maxDoc);
            }

            public override SortedSetDocValues GetSortedSet(FieldInfo field)
            {
                if (Debugging.AssertsEnabled) Debugging.ThrowAssertIf(field.DocValuesType == DocValuesType.SORTED_SET);
                SortedSetDocValues values = @in.GetSortedSet(field);
                if (Debugging.AssertsEnabled) Debugging.ThrowAssertIf(values != null);
                return new AssertingSortedSetDocValues(values, maxDoc);
            }

            public override IBits GetDocsWithField(FieldInfo field)
            {
                if (Debugging.AssertsEnabled) Debugging.ThrowAssertIf(field.DocValuesType != DocValuesType.NONE);
                IBits bits = @in.GetDocsWithField(field);
                if (Debugging.AssertsEnabled) Debugging.ThrowAssertIf(bits != null);
                if (Debugging.AssertsEnabled) Debugging.ThrowAssertIf(bits.Length == maxDoc);
                return new AssertingBits(bits);
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