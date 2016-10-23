using Lucene.Net.Attributes;
using NUnit.Framework;
using System;
using System.Diagnostics;

namespace Lucene.Net.Util.Packed
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

    [TestFixture]
    public class TestEliasFanoSequence : LuceneTestCase
    {
        private static EliasFanoEncoder MakeEncoder(long[] values, long indexInterval)
        {
            long upperBound = -1L;
            foreach (long value in values)
            {
                Assert.IsTrue(value >= upperBound); // test data ok
                upperBound = value;
            }
            EliasFanoEncoder efEncoder = new EliasFanoEncoder(values.Length, upperBound, indexInterval);
            foreach (long value in values)
            {
                efEncoder.EncodeNext(value);
            }
            return efEncoder;
        }

        private static void TstDecodeAllNext(long[] values, EliasFanoDecoder efd)
        {
            efd.ToBeforeSequence();
            long nextValue = efd.NextValue();
            foreach (long expValue in values)
            {
                Assert.IsFalse(EliasFanoDecoder.NO_MORE_VALUES == nextValue, "nextValue at end too early");
                Assert.AreEqual(expValue, nextValue);
                nextValue = efd.NextValue();
            }
            Assert.AreEqual(EliasFanoDecoder.NO_MORE_VALUES, nextValue);
        }

        private static void TstDecodeAllPrev(long[] values, EliasFanoDecoder efd)
        {
            efd.ToAfterSequence();
            for (int i = values.Length - 1; i >= 0; i--)
            {
                long previousValue = efd.PreviousValue();
                Assert.IsFalse(EliasFanoDecoder.NO_MORE_VALUES == previousValue, "previousValue at end too early");
                Assert.AreEqual(values[i], previousValue);
            }
            Assert.AreEqual(EliasFanoDecoder.NO_MORE_VALUES, efd.PreviousValue());
        }

        private static void TstDecodeAllAdvanceToExpected(long[] values, EliasFanoDecoder efd)
        {
            efd.ToBeforeSequence();
            long previousValue = -1L;
            long index = 0;
            foreach (long expValue in values)
            {
                if (expValue > previousValue)
                {
                    long advanceValue = efd.AdvanceToValue(expValue);
                    Assert.IsFalse(EliasFanoDecoder.NO_MORE_VALUES == advanceValue, "advanceValue at end too early");
                    Assert.AreEqual(expValue, advanceValue);
                    Assert.AreEqual(index, efd.CurrentIndex());
                    previousValue = expValue;
                }
                index++;
            }
            long advanceValue_ = efd.AdvanceToValue(previousValue + 1);
            Assert.AreEqual(EliasFanoDecoder.NO_MORE_VALUES, advanceValue_, "at end");
        }

        private static void TstDecodeAdvanceToMultiples(long[] values, EliasFanoDecoder efd, long m)
        {
            // test advancing to multiples of m
            Debug.Assert(m > 0);
            long previousValue = -1L;
            long index = 0;
            long mm = m;
            efd.ToBeforeSequence();
            foreach (long expValue in values)
            {
                // mm > previousValue
                if (expValue >= mm)
                {
                    long advanceValue = efd.AdvanceToValue(mm);
                    Assert.IsFalse(EliasFanoDecoder.NO_MORE_VALUES == advanceValue, "advanceValue at end too early");
                    Assert.AreEqual(expValue, advanceValue);
                    Assert.AreEqual(index, efd.CurrentIndex());
                    previousValue = expValue;
                    do
                    {
                        mm += m;
                    } while (mm <= previousValue);
                }
                index++;
            }
            long advanceValue_ = efd.AdvanceToValue(mm);
            Assert.AreEqual(EliasFanoDecoder.NO_MORE_VALUES, advanceValue_);
        }

        private static void TstDecodeBackToMultiples(long[] values, EliasFanoDecoder efd, long m)
        {
            // test backing to multiples of m
            Debug.Assert(m > 0);
            efd.ToAfterSequence();
            int index = values.Length - 1;
            if (index < 0)
            {
                long advanceValue = efd.BackToValue(0);
                Assert.AreEqual(EliasFanoDecoder.NO_MORE_VALUES, advanceValue);
                return; // empty values, nothing to go back to/from
            }
            long expValue = values[index];
            long previousValue = expValue + 1;
            long mm = (expValue / m) * m;
            while (index >= 0)
            {
                expValue = values[index];
                Debug.Assert(mm < previousValue);
                if (expValue <= mm)
                {
                    long backValue_ = efd.BackToValue(mm);
                    Assert.IsFalse(EliasFanoDecoder.NO_MORE_VALUES == backValue_, "backToValue at end too early");
                    Assert.AreEqual(expValue, backValue_);
                    Assert.AreEqual(index, efd.CurrentIndex());
                    previousValue = expValue;
                    do
                    {
                        mm -= m;
                    } while (mm >= previousValue);
                }
                index--;
            }
            long backValue = efd.BackToValue(mm);
            Assert.AreEqual(EliasFanoDecoder.NO_MORE_VALUES, backValue);
        }

        private static void TstEqual(string mes, long[] exp, long[] act)
        {
            Assert.AreEqual(exp.Length, act.Length, mes + ".Length");
            for (int i = 0; i < exp.Length; i++)
            {
                if (exp[i] != act[i])
                {
                    Assert.Fail(mes + "[" + i + "] " + exp[i] + " != " + act[i]);
                }
            }
        }

        private static void TstDecodeAll(EliasFanoEncoder efEncoder, long[] values)
        {
            TstDecodeAllNext(values, efEncoder.Decoder);
            TstDecodeAllPrev(values, efEncoder.Decoder);
            TstDecodeAllAdvanceToExpected(values, efEncoder.Decoder);
        }

        private static void TstEFS(long[] values, long[] expHighLongs, long[] expLowLongs)
        {
            EliasFanoEncoder efEncoder = MakeEncoder(values, EliasFanoEncoder.DEFAULT_INDEX_INTERVAL);
            TstEqual("upperBits", expHighLongs, efEncoder.UpperBits);
            TstEqual("lowerBits", expLowLongs, efEncoder.LowerBits);
            TstDecodeAll(efEncoder, values);
        }

        private static void TstEFS2(long[] values)
        {
            EliasFanoEncoder efEncoder = MakeEncoder(values, EliasFanoEncoder.DEFAULT_INDEX_INTERVAL);
            TstDecodeAll(efEncoder, values);
        }

        private static void TstEFSadvanceToAndBackToMultiples(long[] values, long maxValue, long minAdvanceMultiple)
        {
            EliasFanoEncoder efEncoder = MakeEncoder(values, EliasFanoEncoder.DEFAULT_INDEX_INTERVAL);
            for (long m = minAdvanceMultiple; m <= maxValue; m += 1)
            {
                TstDecodeAdvanceToMultiples(values, efEncoder.Decoder, m);
                TstDecodeBackToMultiples(values, efEncoder.Decoder, m);
            }
        }

        private EliasFanoEncoder TstEFVI(long[] values, long indexInterval, long[] expIndexBits)
        {
            EliasFanoEncoder efEncVI = MakeEncoder(values, indexInterval);
            TstEqual("upperZeroBitPositionIndex", expIndexBits, efEncVI.IndexBits);
            return efEncVI;
        }

        [Test]
        public virtual void TestEmpty()
        {
            long[] values = new long[0];
            long[] expHighBits = new long[0];
            long[] expLowBits = new long[0];
            TstEFS(values, expHighBits, expLowBits);
        }

        [Test]
        public virtual void TestOneValue1()
        {
            long[] values = new long[] { 0 };
            long[] expHighBits = new long[] { 0x1L };
            long[] expLowBits = new long[] { };
            TstEFS(values, expHighBits, expLowBits);
        }

        [Test]
        public virtual void TestTwoValues1()
        {
            long[] values = new long[] { 0, 0 };
            long[] expHighBits = new long[] { 0x3L };
            long[] expLowBits = new long[] { };
            TstEFS(values, expHighBits, expLowBits);
        }

        [Test]
        public virtual void TestOneValue2()
        {
            long[] values = new long[] { 63 };
            long[] expHighBits = new long[] { 2 };
            long[] expLowBits = new long[] { 31 };
            TstEFS(values, expHighBits, expLowBits);
        }

        [Test]
        public virtual void TestOneMaxValue()
        {
            long[] values = new long[] { long.MaxValue };
            long[] expHighBits = new long[] { 2 };
            long[] expLowBits = new long[] { long.MaxValue / 2 };
            TstEFS(values, expHighBits, expLowBits);
        }

        [Test]
        public virtual void TestTwoMinMaxValues()
        {
            long[] values = new long[] { 0, long.MaxValue };
            long[] expHighBits = new long[] { 0x11 };
            long[] expLowBits = new long[] { unchecked((long)0xE000000000000000L), 0x03FFFFFFFFFFFFFFL };
            TstEFS(values, expHighBits, expLowBits);
        }

        [Test]
        public virtual void TestTwoMaxValues()
        {
            long[] values = new long[] { long.MaxValue, long.MaxValue };
            long[] expHighBits = new long[] { 0x18 };
            long[] expLowBits = new long[] { -1L, 0x03FFFFFFFFFFFFFFL };
            TstEFS(values, expHighBits, expLowBits);
        }

        [Test]
        public virtual void TestExample1() // Figure 1 from Vigna 2012 paper
        {
            long[] values = new long[] { 5, 8, 8, 15, 32 };
            long[] expLowBits = new long[] { Convert.ToInt64("0011000001", 2) }; // reverse block and bit order
            long[] expHighBits = new long[] { Convert.ToInt64("1000001011010", 2) }; // reverse block and bit order
            TstEFS(values, expHighBits, expLowBits);
        }

        [Test]
        public virtual void TestHashCodeEquals()
        {
            long[] values = new long[] { 5, 8, 8, 15, 32 };
            EliasFanoEncoder efEncoder1 = MakeEncoder(values, EliasFanoEncoder.DEFAULT_INDEX_INTERVAL);
            EliasFanoEncoder efEncoder2 = MakeEncoder(values, EliasFanoEncoder.DEFAULT_INDEX_INTERVAL);
            Assert.AreEqual(efEncoder1, efEncoder2);
            Assert.AreEqual(efEncoder1.GetHashCode(), efEncoder2.GetHashCode());

            EliasFanoEncoder efEncoder3 = MakeEncoder(new long[] { 1, 2, 3 }, EliasFanoEncoder.DEFAULT_INDEX_INTERVAL);
            Assert.IsFalse(efEncoder1.Equals(efEncoder3));
            Assert.IsFalse(efEncoder3.Equals(efEncoder1));
            Assert.IsFalse(efEncoder1.GetHashCode() == efEncoder3.GetHashCode()); // implementation ok for these.
        }

        [Test]
        public virtual void TestMonotoneSequences()
        {
            for (int s = 2; s < 1222; s++)
            {
                long[] values = new long[s];
                for (int i = 0; i < s; i++)
                {
                    values[i] = (i / 2); // upperbound smaller than number of values, only upper bits encoded
                }
                TstEFS2(values);
            }
        }

        [Test, LongRunningTest, MaxTime(50000), LuceneNetSpecific]
        public virtual void TestMonotoneSequencesLonger()
        {
            for (int s = 2; s < 4422; s++)
            {
                long[] values = new long[s];
                for (int i = 0; i < s; i++)
                {
                    values[i] = (i / 2); // upperbound smaller than number of values, only upper bits encoded
                }
                TstEFS2(values);
            }
        }

        [Test]
        public virtual void TestStrictMonotoneSequences()
        {
            for (int s = 2; s < 1222; s++)
            {
                var values = new long[s];
                for (int i = 0; i < s; i++)
                {
                    values[i] = i * ((long)i - 1) / 2; // Add a gap of (s-1) to previous
                    // s = (s*(s+1) - (s-1)*s)/2
                }
                TstEFS2(values);
            }
        }

        [Test, LongRunningTest, MaxTime(50000), LuceneNetSpecific]
        public virtual void TestStrictMonotoneSequencesLonger()
        {
            for (int s = 2; s < 4422; s++)
            {
                var values = new long[s];
                for (int i = 0; i < s; i++)
                {
                    values[i] = i * ((long)i - 1) / 2; // Add a gap of (s-1) to previous
                    // s = (s*(s+1) - (s-1)*s)/2
                }
                TstEFS2(values);
            }
        }

        [Test]
        public virtual void TestHighBitLongZero()
        {
            const int s = 65;
            long[] values = new long[s];
            for (int i = 0; i < s - 1; i++)
            {
                values[i] = 0;
            }
            values[s - 1] = 128;
            long[] expHighBits = new long[] { -1, 0, 0, 1 };
            long[] expLowBits = new long[0];
            TstEFS(values, expHighBits, expLowBits);
        }

        [Test]
        public virtual void TestAdvanceToAndBackToMultiples()
        {
            for (int s = 2; s < 130; s++)
            {
                long[] values = new long[s];
                for (int i = 0; i < s; i++)
                {
                    values[i] = i * ((long)i + 1) / 2; // Add a gap of s to previous
                    // s = (s*(s+1) - (s-1)*s)/2
                }
                TstEFSadvanceToAndBackToMultiples(values, values[s - 1], 10);
            }
        }

        [Test]
        public virtual void TestEmptyIndex()
        {
            long indexInterval = 2;
            long[] emptyLongs = new long[0];
            TstEFVI(emptyLongs, indexInterval, emptyLongs);
        }

        [Test]
        public virtual void TestMaxContentEmptyIndex()
        {
            long indexInterval = 2;
            long[] twoLongs = new long[] { 0, 1 };
            long[] emptyLongs = new long[0];
            TstEFVI(twoLongs, indexInterval, emptyLongs);
        }

        [Test]
        public virtual void TestMinContentNonEmptyIndex()
        {
            long indexInterval = 2;
            long[] twoLongs = new long[] { 0, 2 };
            long[] indexLongs = new long[] { 3 }; // high bits 1001, index position after zero bit.
            TstEFVI(twoLongs, indexInterval, indexLongs);
        }

        [Test]
        public virtual void TestIndexAdvanceToLast()
        {
            long indexInterval = 2;
            long[] twoLongs = new long[] { 0, 2 };
            long[] indexLongs = new long[] { 3 }; // high bits 1001
            EliasFanoEncoder efEncVI = TstEFVI(twoLongs, indexInterval, indexLongs);
            Assert.AreEqual(2, efEncVI.Decoder.AdvanceToValue(2));
        }

        [Test]
        public virtual void TestIndexAdvanceToAfterLast()
        {
            long indexInterval = 2;
            long[] twoLongs = new long[] { 0, 2 };
            long[] indexLongs = new long[] { 3 }; // high bits 1001
            EliasFanoEncoder efEncVI = TstEFVI(twoLongs, indexInterval, indexLongs);
            Assert.AreEqual(EliasFanoDecoder.NO_MORE_VALUES, efEncVI.Decoder.AdvanceToValue(3));
        }

        [Test]
        public virtual void TestIndexAdvanceToFirst()
        {
            long indexInterval = 2;
            long[] twoLongs = new long[] { 0, 2 };
            long[] indexLongs = new long[] { 3 }; // high bits 1001
            EliasFanoEncoder efEncVI = TstEFVI(twoLongs, indexInterval, indexLongs);
            Assert.AreEqual(0, efEncVI.Decoder.AdvanceToValue(0));
        }

        [Test]
        public virtual void TestTwoIndexEntries()
        {
            long indexInterval = 2;
            long[] twoLongs = new long[] { 0, 1, 2, 3, 4, 5 };
            long[] indexLongs = new long[] { 4 + 8 * 16 }; // high bits 0b10101010101
            EliasFanoEncoder efEncVI = TstEFVI(twoLongs, indexInterval, indexLongs);
            EliasFanoDecoder efDecVI = efEncVI.Decoder;
            Assert.AreEqual(0, efDecVI.AdvanceToValue(0), "advance 0");
            Assert.AreEqual(5, efDecVI.AdvanceToValue(5), "advance 5");
            Assert.AreEqual(EliasFanoDecoder.NO_MORE_VALUES, efDecVI.AdvanceToValue(5), "advance 6");
        }

        [Test]
        public virtual void TestExample2a() // Figure 2 from Vigna 2012 paper
        {
            long indexInterval = 4;
            long[] values = new long[] { 5, 8, 8, 15, 32 }; // two low bits, high values 1,2,2,3,8.
            long[] indexLongs = new long[] { 8 + 12 * 16 }; // high bits 0b 0001 0000 0101 1010
            EliasFanoEncoder efEncVI = TstEFVI(values, indexInterval, indexLongs);
            EliasFanoDecoder efDecVI = efEncVI.Decoder;
            Assert.AreEqual(32, efDecVI.AdvanceToValue(22), "advance 22");
        }

        [Test]
        public virtual void TestExample2b() // Figure 2 from Vigna 2012 paper
        {
            long indexInterval = 4;
            long[] values = new long[] { 5, 8, 8, 15, 32 }; // two low bits, high values 1,2,2,3,8.
            long[] indexLongs = new long[] { 8 + 12 * 16 }; // high bits 0b 0001 0000 0101 1010
            EliasFanoEncoder efEncVI = TstEFVI(values, indexInterval, indexLongs);
            EliasFanoDecoder efDecVI = efEncVI.Decoder;
            Assert.AreEqual(5, efDecVI.NextValue(), "initial next");
            Assert.AreEqual(32, efDecVI.AdvanceToValue(22), "advance 22");
        }

        [Test]
        public virtual void TestExample2NoIndex1() // Figure 2 from Vigna 2012 paper, no index, test broadword selection.
        {
            long indexInterval = 16;
            long[] values = new long[] { 5, 8, 8, 15, 32 }; // two low bits, high values 1,2,2,3,8.
            long[] indexLongs = new long[0]; // high bits 0b 0001 0000 0101 1010
            EliasFanoEncoder efEncVI = TstEFVI(values, indexInterval, indexLongs);
            EliasFanoDecoder efDecVI = efEncVI.Decoder;
            Assert.AreEqual(32, efDecVI.AdvanceToValue(22), "advance 22");
        }

        [Test]
        public virtual void TestExample2NoIndex2() // Figure 2 from Vigna 2012 paper, no index, test broadword selection.
        {
            long indexInterval = 16;
            long[] values = new long[] { 5, 8, 8, 15, 32 }; // two low bits, high values 1,2,2,3,8.
            long[] indexLongs = new long[0]; // high bits 0b 0001 0000 0101 1010
            EliasFanoEncoder efEncVI = TstEFVI(values, indexInterval, indexLongs);
            EliasFanoDecoder efDecVI = efEncVI.Decoder;
            Assert.AreEqual(5, efDecVI.NextValue(), "initial next");
            Assert.AreEqual(32, efDecVI.AdvanceToValue(22), "advance 22");
        }
    }
}