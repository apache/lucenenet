using Lucene.Net.Randomized.Generators;
using Lucene.Net.Support;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace Lucene.Net.Util
{
    /// <summary>
    /// Licensed to the Apache Software Foundation (ASF) under one or more
    /// contributor license agreements.  See the NOTICE file distributed with
    /// this work for additional information regarding copyright ownership.
    /// The ASF licenses this file to You under the Apache License, Version 2.0
    /// (the "License"); you may not use this file except in compliance with
    /// the License.  You may obtain a copy of the License at
    ///
    ///     http://www.apache.org/licenses/LICENSE-2.0
    ///
    /// Unless required by applicable law or agreed to in writing, software
    /// distributed under the License is distributed on an "AS IS" BASIS,
    /// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
    /// See the License for the specific language governing permissions and
    /// limitations under the License.
    /// </summary>

    [TestFixture]
    public class TestNumericUtils : LuceneTestCase
    {
        [Test]
        public virtual void TestLongConversionAndOrdering()
        {
            // generate a series of encoded longs, each numerical one bigger than the one before
            BytesRef last = null, act = new BytesRef(NumericUtils.BUF_SIZE_LONG);
            for (long l = -100000L; l < 100000L; l++)
            {
                NumericUtils.LongToPrefixCodedBytes(l, 0, act);
                if (last != null)
                {
                    // test if smaller
                    Assert.IsTrue(last.CompareTo(act) < 0, "actual bigger than last (BytesRef)");
                    Assert.IsTrue(last.Utf8ToString().CompareToOrdinal(act.Utf8ToString()) < 0, "actual bigger than last (as String)");
                }
                // test is back and forward conversion works
                Assert.AreEqual(l, NumericUtils.PrefixCodedToLong(act), "forward and back conversion should generate same long");
                // next step
                last = act;
                act = new BytesRef(NumericUtils.BUF_SIZE_LONG);
            }
        }

        [Test]
        public virtual void TestIntConversionAndOrdering()
        {
            // generate a series of encoded ints, each numerical one bigger than the one before
            BytesRef last = null, act = new BytesRef(NumericUtils.BUF_SIZE_INT);
            for (int i = -100000; i < 100000; i++)
            {
                NumericUtils.IntToPrefixCodedBytes(i, 0, act);
                if (last != null)
                {
                    // test if smaller
                    Assert.IsTrue(last.CompareTo(act) < 0, "actual bigger than last (BytesRef)");
                    Assert.IsTrue(last.Utf8ToString().CompareToOrdinal(act.Utf8ToString()) < 0, "actual bigger than last (as String)");
                }
                // test is back and forward conversion works
                Assert.AreEqual(i, NumericUtils.PrefixCodedToInt(act), "forward and back conversion should generate same int");
                // next step
                last = act;
                act = new BytesRef(NumericUtils.BUF_SIZE_INT);
            }
        }

        [Test]
        public virtual void TestLongSpecialValues()
        {
            long[] vals = new long[] { long.MinValue, long.MinValue + 1, long.MinValue + 2, -5003400000000L, -4000L, -3000L, -2000L, -1000L, -1L, 0L, 1L, 10L, 300L, 50006789999999999L, long.MaxValue - 2, long.MaxValue - 1, long.MaxValue };
            BytesRef[] prefixVals = new BytesRef[vals.Length];

            for (int i = 0; i < vals.Length; i++)
            {
                prefixVals[i] = new BytesRef(NumericUtils.BUF_SIZE_LONG);
                NumericUtils.LongToPrefixCodedBytes(vals[i], 0, prefixVals[i]);

                // check forward and back conversion
                Assert.AreEqual(vals[i], NumericUtils.PrefixCodedToLong(prefixVals[i]), "forward and back conversion should generate same long");

                // test if decoding values as int fails correctly
                try
                {
                    NumericUtils.PrefixCodedToInt(prefixVals[i]);
                    Assert.Fail("decoding a prefix coded long value as int should fail");
                }
                catch (FormatException e)
                {
                    // worked
                }
            }

            // check sort order (prefixVals should be ascending)
            for (int i = 1; i < prefixVals.Length; i++)
            {
                Assert.IsTrue(prefixVals[i - 1].CompareTo(prefixVals[i]) < 0, "check sort order");
            }

            // check the prefix encoding, lower precision should have the difference to original value equal to the lower removed bits
            BytesRef @ref = new BytesRef(NumericUtils.BUF_SIZE_LONG);
            for (int i = 0; i < vals.Length; i++)
            {
                for (int j = 0; j < 64; j++)
                {
                    NumericUtils.LongToPrefixCodedBytes(vals[i], j, @ref);
                    long prefixVal = NumericUtils.PrefixCodedToLong(@ref);
                    long mask = (1L << j) - 1L;
                    Assert.AreEqual(vals[i] & mask, vals[i] - prefixVal, "difference between prefix val and original value for " + vals[i] + " with shift=" + j);
                }
            }
        }

        [Test]
        public virtual void TestIntSpecialValues()
        {
            int[] vals = new int[] { int.MinValue, int.MinValue + 1, int.MinValue + 2, -64765767, -4000, -3000, -2000, -1000, -1, 0, 1, 10, 300, 765878989, int.MaxValue - 2, int.MaxValue - 1, int.MaxValue };
            BytesRef[] prefixVals = new BytesRef[vals.Length];

            for (int i = 0; i < vals.Length; i++)
            {
                prefixVals[i] = new BytesRef(NumericUtils.BUF_SIZE_INT);
                NumericUtils.IntToPrefixCodedBytes(vals[i], 0, prefixVals[i]);

                // check forward and back conversion
                Assert.AreEqual(vals[i], NumericUtils.PrefixCodedToInt(prefixVals[i]), "forward and back conversion should generate same int");

                // test if decoding values as long fails correctly
                try
                {
                    NumericUtils.PrefixCodedToLong(prefixVals[i]);
                    Assert.Fail("decoding a prefix coded int value as long should fail");
                }
                catch (FormatException e)
                {
                    // worked
                }
            }

            // check sort order (prefixVals should be ascending)
            for (int i = 1; i < prefixVals.Length; i++)
            {
                Assert.IsTrue(prefixVals[i - 1].CompareTo(prefixVals[i]) < 0, "check sort order");
            }

            // check the prefix encoding, lower precision should have the difference to original value equal to the lower removed bits
            BytesRef @ref = new BytesRef(NumericUtils.BUF_SIZE_LONG);
            for (int i = 0; i < vals.Length; i++)
            {
                for (int j = 0; j < 32; j++)
                {
                    NumericUtils.IntToPrefixCodedBytes(vals[i], j, @ref);
                    int prefixVal = NumericUtils.PrefixCodedToInt(@ref);
                    int mask = (1 << j) - 1;
                    Assert.AreEqual(vals[i] & mask, vals[i] - prefixVal, "difference between prefix val and original value for " + vals[i] + " with shift=" + j);
                }
            }
        }

        [Test]
        public virtual void TestDoubles()
        {
            double[] vals = new double[] { double.NegativeInfinity, -2.3E25, -1.0E15, -1.0, -1.0E-1, -1.0E-2, -0.0, +0.0, 1.0E-2, 1.0E-1, 1.0, 1.0E15, 2.3E25, double.PositiveInfinity, double.NaN };
            long[] longVals = new long[vals.Length];

            // check forward and back conversion
            for (int i = 0; i < vals.Length; i++)
            {
                longVals[i] = NumericUtils.DoubleToSortableLong(vals[i]);
                Assert.IsTrue(vals[i].CompareTo(NumericUtils.SortableLongToDouble(longVals[i])) == 0, "forward and back conversion should generate same double");
            }

            // check sort order (prefixVals should be ascending)
            for (int i = 1; i < longVals.Length; i++)
            {
                Assert.IsTrue(longVals[i - 1] < longVals[i], "check sort order");
            }
        }

        public static readonly double[] DOUBLE_NANs = new double[] { double.NaN, BitConverter.Int64BitsToDouble(0x7ff0000000000001L), BitConverter.Int64BitsToDouble(0x7fffffffffffffffL), BitConverter.Int64BitsToDouble(unchecked((long)0xfff0000000000001L)), BitConverter.Int64BitsToDouble(unchecked((long)0xffffffffffffffffL)) };

        [Test]
        public virtual void TestSortableDoubleNaN()
        {
            long plusInf = NumericUtils.DoubleToSortableLong(double.PositiveInfinity);
            foreach (double nan in DOUBLE_NANs)
            {
                Assert.IsTrue(double.IsNaN(nan));
                long sortable = NumericUtils.DoubleToSortableLong(nan);
                Assert.IsTrue((ulong)sortable > (ulong)plusInf, "Double not sorted correctly: " + nan + ", long repr: " + sortable + ", positive inf.: " + plusInf);
            }
        }

        [Test]
        public virtual void TestFloats()
        {
            float[] vals = new float[] { float.NegativeInfinity, -2.3E25f, -1.0E15f, -1.0f, -1.0E-1f, -1.0E-2f, -0.0f, +0.0f, 1.0E-2f, 1.0E-1f, 1.0f, 1.0E15f, 2.3E25f, float.PositiveInfinity, float.NaN };
            int[] intVals = new int[vals.Length];

            // check forward and back conversion
            for (int i = 0; i < vals.Length; i++)
            {
                intVals[i] = NumericUtils.FloatToSortableInt(vals[i]);
                Assert.IsTrue(vals[i].CompareTo(NumericUtils.SortableIntToFloat(intVals[i])) == 0, "forward and back conversion should generate same double");
            }

            // check sort order (prefixVals should be ascending)
            for (int i = 1; i < intVals.Length; i++)
            {
                Assert.IsTrue(intVals[i - 1] < intVals[i], "check sort order");
            }
        }

        public static readonly float[] FLOAT_NANs = new float[] { float.NaN, Number.IntBitsToFloat(0x7f800001), Number.IntBitsToFloat(0x7fffffff), Number.IntBitsToFloat(unchecked((int)0xff800001)), Number.IntBitsToFloat(unchecked((int)0xffffffff)) };

        [Test]
        public virtual void TestSortableFloatNaN()
        {
            int plusInf = NumericUtils.FloatToSortableInt(float.PositiveInfinity);
            foreach (float nan in FLOAT_NANs)
            {
                Assert.IsTrue(float.IsNaN(nan));
                uint sortable = (uint)NumericUtils.FloatToSortableInt(nan);
                Assert.IsTrue(sortable > plusInf, "Float not sorted correctly: " + nan + ", int repr: " + sortable + ", positive inf.: " + plusInf);
            }
        }

        // INFO: Tests for trieCodeLong()/trieCodeInt() not needed because implicitely tested by range filter tests

        /// <summary>
        /// Note: The neededBounds Iterable must be unsigned (easier understanding what's happening) </summary>
        private void AssertLongRangeSplit(long lower, long upper, int precisionStep, bool useBitSet, IEnumerable<long> expectedBounds, IEnumerable<int> expectedShifts)
        {
            // Cannot use FixedBitSet since the range could be long:
            LongBitSet bits = useBitSet ? new LongBitSet(upper - lower + 1) : null;
            IEnumerator<long> neededBounds = (expectedBounds == null) ? null : expectedBounds.GetEnumerator();
            IEnumerator<int> neededShifts = (expectedShifts == null) ? null : expectedShifts.GetEnumerator();

            NumericUtils.SplitLongRange(new LongRangeBuilderAnonymousInnerClassHelper(this, lower, upper, useBitSet, bits, neededBounds, neededShifts), precisionStep, lower, upper);

            if (useBitSet)
            {
                // after flipping all bits in the range, the cardinality should be zero
                bits.Flip(0, upper - lower + 1);
                Assert.AreEqual(0, bits.Cardinality(), "The sub-range concenated should match the whole range");
            }
        }

        private class LongRangeBuilderAnonymousInnerClassHelper : NumericUtils.LongRangeBuilder
        {
            private readonly TestNumericUtils OuterInstance;

            private long Lower;
            private long Upper;
            private bool UseBitSet;
            private LongBitSet Bits;
            private IEnumerator<long> NeededBounds;
            private IEnumerator<int> NeededShifts;

            public LongRangeBuilderAnonymousInnerClassHelper(TestNumericUtils outerInstance, long lower, long upper, bool useBitSet, LongBitSet bits, IEnumerator<long> neededBounds, IEnumerator<int> neededShifts)
            {
                this.OuterInstance = outerInstance;
                this.Lower = lower;
                this.Upper = upper;
                this.UseBitSet = useBitSet;
                this.Bits = bits;
                this.NeededBounds = neededBounds;
                this.NeededShifts = neededShifts;
            }

            public override void AddRange(long min, long max, int shift)
            {
                Assert.IsTrue(min >= Lower && min <= Upper && max >= Lower && max <= Upper, "min, max should be inside bounds");
                if (UseBitSet)
                {
                    for (long l = min; l <= max; l++)
                    {
                        Assert.IsFalse(Bits.GetAndSet(l - Lower), "ranges should not overlap");
                        // extra exit condition to prevent overflow on MAX_VALUE
                        if (l == max)
                        {
                            break;
                        }
                    }
                }
                if (NeededBounds == null || NeededShifts == null)
                {
                    return;
                }
                // make unsigned longs for easier display and understanding
                min ^= unchecked((long)0x8000000000000000L);
                max ^= unchecked((long)0x8000000000000000L);
                //System.out.println("0x"+Long.toHexString(min>>>shift)+"L,0x"+Long.toHexString(max>>>shift)+"L)/*shift="+shift+"*/,");
                NeededShifts.MoveNext();
                Assert.AreEqual(NeededShifts.Current, shift, "shift");
                NeededBounds.MoveNext();
                Assert.AreEqual(NeededBounds.Current, (long)((ulong)min >> shift), "inner min bound");
                NeededBounds.MoveNext();
                Assert.AreEqual(NeededBounds.Current, (long)((ulong)max >> shift), "inner max bound");
            }
        }

        /// <summary>
        /// LUCENE-2541: NumericRangeQuery errors with endpoints near long min and max values </summary>
        [Test]
        public virtual void TestLongExtremeValues()
        {
            // upper end extremes
            AssertLongRangeSplit(long.MaxValue, long.MaxValue, 1, true, Arrays.AsList(unchecked((long)0xffffffffffffffffL), unchecked((long)0xffffffffffffffffL)), Arrays.AsList(0));
            AssertLongRangeSplit(long.MaxValue, long.MaxValue, 2, true, Arrays.AsList(unchecked((long)0xffffffffffffffffL), unchecked((long)0xffffffffffffffffL)), Arrays.AsList(0));
            AssertLongRangeSplit(long.MaxValue, long.MaxValue, 4, true, Arrays.AsList(unchecked((long)0xffffffffffffffffL), unchecked((long)0xffffffffffffffffL)), Arrays.AsList(0));
            AssertLongRangeSplit(long.MaxValue, long.MaxValue, 6, true, Arrays.AsList(unchecked((long)0xffffffffffffffffL), unchecked((long)0xffffffffffffffffL)), Arrays.AsList(0));
            AssertLongRangeSplit(long.MaxValue, long.MaxValue, 8, true, Arrays.AsList(unchecked((long)0xffffffffffffffffL), unchecked((long)0xffffffffffffffffL)), Arrays.AsList(0));
            AssertLongRangeSplit(long.MaxValue, long.MaxValue, 64, true, Arrays.AsList(unchecked((long)0xffffffffffffffffL), unchecked((long)0xffffffffffffffffL)), Arrays.AsList(0));

            AssertLongRangeSplit(long.MaxValue - 0xfL, long.MaxValue, 4, true, Arrays.AsList(0xfffffffffffffffL, 0xfffffffffffffffL), Arrays.AsList(4));
            AssertLongRangeSplit(long.MaxValue - 0x10L, long.MaxValue, 4, true, Arrays.AsList(unchecked((long)0xffffffffffffffefL), unchecked((long)0xffffffffffffffefL), 0xfffffffffffffffL, 0xfffffffffffffffL), Arrays.AsList(0, 4));

            // lower end extremes
            AssertLongRangeSplit(long.MinValue, long.MinValue, 1, true, Arrays.AsList(0x0000000000000000L, 0x0000000000000000L), Arrays.AsList(0));
            AssertLongRangeSplit(long.MinValue, long.MinValue, 2, true, Arrays.AsList(0x0000000000000000L, 0x0000000000000000L), Arrays.AsList(0));
            AssertLongRangeSplit(long.MinValue, long.MinValue, 4, true, Arrays.AsList(0x0000000000000000L, 0x0000000000000000L), Arrays.AsList(0));
            AssertLongRangeSplit(long.MinValue, long.MinValue, 6, true, Arrays.AsList(0x0000000000000000L, 0x0000000000000000L), Arrays.AsList(0));
            AssertLongRangeSplit(long.MinValue, long.MinValue, 8, true, Arrays.AsList(0x0000000000000000L, 0x0000000000000000L), Arrays.AsList(0));
            AssertLongRangeSplit(long.MinValue, long.MinValue, 64, true, Arrays.AsList(0x0000000000000000L, 0x0000000000000000L), Arrays.AsList(0));

            AssertLongRangeSplit(long.MinValue, long.MinValue + 0xfL, 4, true, Arrays.AsList(0x000000000000000L, 0x000000000000000L), Arrays.AsList(4));
            AssertLongRangeSplit(long.MinValue, long.MinValue + 0x10L, 4, true, Arrays.AsList(0x0000000000000010L, 0x0000000000000010L, 0x000000000000000L, 0x000000000000000L), Arrays.AsList(0, 4));
        }

        [Test]
        public virtual void TestRandomSplit()
        {
            long num = (long)AtLeast(10);
            for (long i = 0; i < num; i++)
            {
                ExecuteOneRandomSplit(Random());
            }
        }

        private void ExecuteOneRandomSplit(Random random)
        {
            long lower = RandomLong(random);
            long len = random.Next(16384 * 1024); // not too large bitsets, else OOME!
            while (lower + len < lower) // overflow
            {
                lower >>= 1;
            }
            AssertLongRangeSplit(lower, lower + len, random.Next(64) + 1, true, null, null);
        }

        private long RandomLong(Random random)
        {
            long val;
            switch (random.Next(4))
            {
                case 0:
                    val = 1L << (random.Next(63)); //  patterns like 0x000000100000 (-1 yields patterns like 0x0000fff)
                    break;

                case 1:
                    val = -1L << (random.Next(63)); // patterns like 0xfffff00000
                    break;

                default:
                    val = random.NextLong();
                    break;
            }

            val += random.Next(5) - 2;

            if (random.NextBoolean())
            {
                if (random.NextBoolean())
                {
                    val += random.Next(100) - 50;
                }
                if (random.NextBoolean())
                {
                    val = ~val;
                }
                if (random.NextBoolean())
                {
                    val = val << 1;
                }
                if (random.NextBoolean())
                {
                    val = (long)((ulong)val >> 1);
                }
            }

            return val;
        }

        [Test]
        public virtual void TestSplitLongRange()
        {
            // a hard-coded "standard" range
            AssertLongRangeSplit(-5000L, 9500L, 4, true, new long[] { 0x7fffffffffffec78L, 0x7fffffffffffec7fL, unchecked((long)0x8000000000002510L), unchecked((long)0x800000000000251cL), 0x7fffffffffffec8L, 0x7fffffffffffecfL, 0x800000000000250L, 0x800000000000250L, 0x7fffffffffffedL, 0x7fffffffffffefL, 0x80000000000020L, 0x80000000000024L, 0x7ffffffffffffL, 0x8000000000001L }, new int[] { 0, 0, 4, 4, 8, 8, 12 });

            // the same with no range splitting
            AssertLongRangeSplit(-5000L, 9500L, 64, true, new long[] { 0x7fffffffffffec78L, unchecked((long)0x800000000000251cL) }, new int[] { 0 });

            // this tests optimized range splitting, if one of the inner bounds
            // is also the bound of the next lower precision, it should be used completely
            AssertLongRangeSplit(0L, 1024L + 63L, 4, true, new long[] { 0x800000000000040L, 0x800000000000043L, 0x80000000000000L, 0x80000000000003L }, new int[] { 4, 8 });

            // the full long range should only consist of a lowest precision range; no bitset testing here, as too much memory needed :-)
            AssertLongRangeSplit(long.MinValue, long.MaxValue, 8, false, new long[] { 0x00L, 0xffL }, new int[] { 56 });

            // the same with precisionStep=4
            AssertLongRangeSplit(long.MinValue, long.MaxValue, 4, false, new long[] { 0x0L, 0xfL }, new int[] { 60 });

            // the same with precisionStep=2
            AssertLongRangeSplit(long.MinValue, long.MaxValue, 2, false, new long[] { 0x0L, 0x3L }, new int[] { 62 });

            // the same with precisionStep=1
            AssertLongRangeSplit(long.MinValue, long.MaxValue, 1, false, new long[] { 0x0L, 0x1L }, new int[] { 63 });

            // a inverse range should produce no sub-ranges
            AssertLongRangeSplit(9500L, -5000L, 4, false, new long[] { }, new int[] { });

            // a 0-length range should reproduce the range itself
            AssertLongRangeSplit(9500L, 9500L, 4, false, new long[] { unchecked((long)0x800000000000251cL), unchecked((long)0x800000000000251cL) }, new int[] { 0 });
        }

        /// <summary>
        /// Note: The neededBounds Iterable must be unsigned (easier understanding what's happening) </summary>
        private void AssertIntRangeSplit(int lower, int upper, int precisionStep, bool useBitSet, IEnumerable<int> expectedBounds, IEnumerable<int> expectedShifts)
        {
            FixedBitSet bits = useBitSet ? new FixedBitSet(upper - lower + 1) : null;
            IEnumerator<int> neededBounds = (expectedBounds == null) ? null : expectedBounds.GetEnumerator();
            IEnumerator<int> neededShifts = (expectedShifts == null) ? null : expectedShifts.GetEnumerator();

            NumericUtils.SplitIntRange(new IntRangeBuilderAnonymousInnerClassHelper(this, lower, upper, useBitSet, bits, neededBounds, neededShifts), precisionStep, lower, upper);

            if (useBitSet)
            {
                // after flipping all bits in the range, the cardinality should be zero
                bits.Flip(0, upper - lower + 1);
                Assert.AreEqual(0, bits.Cardinality(), "The sub-range concenated should match the whole range");
            }
        }

        private class IntRangeBuilderAnonymousInnerClassHelper : NumericUtils.IntRangeBuilder
        {
            private readonly TestNumericUtils OuterInstance;

            private int Lower;
            private int Upper;
            private bool UseBitSet;
            private FixedBitSet Bits;
            private IEnumerator<int> NeededBounds;
            private IEnumerator<int> NeededShifts;

            public IntRangeBuilderAnonymousInnerClassHelper(TestNumericUtils outerInstance, int lower, int upper, bool useBitSet, FixedBitSet bits, IEnumerator<int> neededBounds, IEnumerator<int> neededShifts)
            {
                this.OuterInstance = outerInstance;
                this.Lower = lower;
                this.Upper = upper;
                this.UseBitSet = useBitSet;
                this.Bits = bits;
                this.NeededBounds = neededBounds;
                this.NeededShifts = neededShifts;
            }

            public override void AddRange(int min, int max, int shift)
            {
                Assert.IsTrue(min >= Lower && min <= Upper && max >= Lower && max <= Upper, "min, max should be inside bounds");
                if (UseBitSet)
                {
                    for (int i = min; i <= max; i++)
                    {
                        Assert.IsFalse(Bits.GetAndSet(i - Lower), "ranges should not overlap");
                        // extra exit condition to prevent overflow on MAX_VALUE
                        if (i == max)
                        {
                            break;
                        }
                    }
                }
                if (NeededBounds == null)
                {
                    return;
                }
                // make unsigned ints for easier display and understanding
                min ^= unchecked((int)0x80000000);
                max ^= unchecked((int)0x80000000);
                //System.out.println("0x"+Integer.toHexString(min>>>shift)+",0x"+Integer.toHexString(max>>>shift)+")/*shift="+shift+"*/,");
                NeededShifts.MoveNext();
                Assert.AreEqual(NeededShifts.Current, shift, "shift");
                NeededBounds.MoveNext();
                Assert.AreEqual(NeededBounds.Current, (int)((uint)min >> shift), "inner min bound");
                NeededBounds.MoveNext();
                Assert.AreEqual(NeededBounds.Current, (int)((uint)max >> shift), "inner max bound");
            }
        }

        [Test]
        public virtual void TestSplitIntRange()
        {
            // a hard-coded "standard" range
            AssertIntRangeSplit(-5000, 9500, 4, true, Arrays.AsList(0x7fffec78, 0x7fffec7f, unchecked((int)0x80002510), unchecked((int)0x8000251c), 0x7fffec8, 0x7fffecf, 0x8000250, 0x8000250, 0x7fffed, 0x7fffef, 0x800020, 0x800024, 0x7ffff, 0x80001), Arrays.AsList(0, 0, 4, 4, 8, 8, 12));

            // the same with no range splitting
            AssertIntRangeSplit(-5000, 9500, 32, true, Arrays.AsList(0x7fffec78, unchecked((int)0x8000251c)), Arrays.AsList(0));

            // this tests optimized range splitting, if one of the inner bounds
            // is also the bound of the next lower precision, it should be used completely
            AssertIntRangeSplit(0, 1024 + 63, 4, true, Arrays.AsList(0x8000040, 0x8000043, 0x800000, 0x800003), Arrays.AsList(4, 8));

            // the full int range should only consist of a lowest precision range; no bitset testing here, as too much memory needed :-)
            AssertIntRangeSplit(int.MinValue, int.MaxValue, 8, false, Arrays.AsList(0x00, 0xff), Arrays.AsList(24));

            // the same with precisionStep=4
            AssertIntRangeSplit(int.MinValue, int.MaxValue, 4, false, Arrays.AsList(0x0, 0xf), Arrays.AsList(28));

            // the same with precisionStep=2
            AssertIntRangeSplit(int.MinValue, int.MaxValue, 2, false, Arrays.AsList(0x0, 0x3), Arrays.AsList(30));

            // the same with precisionStep=1
            AssertIntRangeSplit(int.MinValue, int.MaxValue, 1, false, Arrays.AsList(0x0, 0x1), Arrays.AsList(31));

            // a inverse range should produce no sub-ranges
            AssertIntRangeSplit(9500, -5000, 4, false, new List<int>(), new List<int>());

            // a 0-length range should reproduce the range itself
            AssertIntRangeSplit(9500, 9500, 4, false, Arrays.AsList(unchecked((int)0x8000251c), unchecked((int)0x8000251c)), Arrays.AsList(0));
        }
    }
}