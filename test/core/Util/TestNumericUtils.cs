/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using Lucene.Net.Support;
using Lucene.Net.Test.Support;
using NUnit.Framework;

namespace Lucene.Net.Util
{
    [TestFixture]
    public class TestNumericUtils : LuceneTestCase
    {
        [Test]
        public virtual void TestLongConversionAndOrdering()
        {
            // generate a series of encoded longs, each numerical one bigger than the one before
            BytesRef last = null, act = new BytesRef(NumericUtils.BUF_SIZE_LONG);
            for (var l = -100000L; l < 100000L; l++)
            {
                NumericUtils.LongToPrefixCodedBytes(l, 0, act);
                if (last != null)
                {
                    // test if smaller
                    Assert.IsTrue(last.CompareTo(act) < 0, "actual bigger than last (BytesRef)");
                    Assert.IsTrue(last.Utf8ToString().CompareTo(act.Utf8ToString()) < 0, "actual bigger than last (as string)");
                }
                // test is back and forward conversion works
                Assert.IsTrue(l.Equals(NumericUtils.PrefixCodedToLong(act)), "forward and back conversion should generate same long");
                //Assert.Equals("forward and back conversion should generate same long", l,
                             //NumericUtils.PrefixCodedToLong(act));
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
            for (var i = -100000; i < 100000; i++)
            {
                NumericUtils.IntToPrefixCodedBytes(i, 0, act);
                if (last != null)
                {
                    // test if smaller
                    Assert.IsTrue(last.CompareTo(act) < 0, "actual bigger than last (BytesRef)");
                    Assert.IsTrue(last.Utf8ToString().CompareTo(act.Utf8ToString()) < 0, "actual bigger than last (as string)");
                }
                // test is back and forward conversion works
                Assert.IsTrue(i.Equals(NumericUtils.PrefixCodedToInt(act)), "forward and back conversion should generate same int");
                //Assert.Equals("forward and back conversion should generate same int", i,
                             //NumericUtils.PrefixCodedToInt(act));
                // next step
                last = act;
                act = new BytesRef(NumericUtils.BUF_SIZE_INT);
            }
        }

        [Test]
        public virtual void TestLongSpecialValues()
        {
            var vals = new long[]
                {
                    long.MinValue, long.MinValue + 1, long.MinValue + 2, -5003400000000L,
                    -4000L, -3000L, -2000L, -1000L, -1L, 0L, 1L, 10L, 300L, 50006789999999999L, long.MaxValue - 2,
                    long.MaxValue - 1, long.MaxValue
                };
            var prefixVals = new BytesRef[vals.Length];

            for (var i = 0; i < vals.Length; i++)
            {
                prefixVals[i] = new BytesRef(NumericUtils.BUF_SIZE_LONG);
                NumericUtils.LongToPrefixCodedBytes(vals[i], 0, prefixVals[i]);

                // check forward and back conversion
                Assert.IsTrue(vals[i].Equals(NumericUtils.PrefixCodedToLong(prefixVals[i])), "forward and back conversion should generate same long");
                //.Equals("forward and back conversion should generate same long", vals[i],
                             //NumericUtils.PrefixCodedToLong(prefixVals[i]));

                Assert.Throws<FormatException>(() => NumericUtils.PrefixCodedToInt(prefixVals[i]),
                                               "decoding a prefix coded long value as int should fail");
            }

            // check sort order (prefixVals should be ascending)
            for (var i = 1; i < prefixVals.Length; i++)
            {
                Assert.IsTrue(prefixVals[i - 1].CompareTo(prefixVals[i]) < 0, "check sort order");
            }

            // check the prefix encoding, lower precision should have the difference to original value equal to the lower removed bits
            var bytesRef = new BytesRef(NumericUtils.BUF_SIZE_LONG);
            foreach (var t in vals)
            {
                for (var j = 0; j < 64; j++)
                {
                    NumericUtils.LongToPrefixCodedBytes(t, j, bytesRef);
                    var prefixVal = NumericUtils.PrefixCodedToLong(bytesRef);
                    var mask = (1L << j) - 1L;
                    Assert.IsTrue((t & mask).Equals(t - prefixVal), "difference between prefix val and original value for " + t + " with shift=" + j);
                    //Assert.Equals(t & mask, t - prefixVal); //, "difference between prefix val and original value for " + t + " with shift=" + j);
                }
            }
        }

        [Test]
        public virtual void TestIntSpecialValues()
        {
            var vals = new int[]
                {
                    int.MinValue, int.MinValue + 1, int.MinValue + 2, -64765767,
                    -4000, -3000, -2000, -1000, -1, 0, 1, 10, 300, 765878989, int.MaxValue - 2, int.MaxValue - 1,
                    int.MaxValue
                };
            var prefixVals = new BytesRef[vals.Length];

            for (var i = 0; i < vals.Length; i++)
            {
                prefixVals[i] = new BytesRef(NumericUtils.BUF_SIZE_INT);
                NumericUtils.IntToPrefixCodedBytes(vals[i], 0, prefixVals[i]);

                // check forward and back conversion
                Assert.Equals(vals[i], NumericUtils.PrefixCodedToInt(prefixVals[i])); //"forward and back conversion should generate same int", 

                Assert.Throws<FormatException>(() => NumericUtils.PrefixCodedToLong(prefixVals[i]), "decoding a prefix coded int value as long should fail");
            }

            // check sort order (prefixVals should be ascending)
            for (var i = 1; i < prefixVals.Length; i++)
            {
                Assert.IsTrue(prefixVals[i - 1].CompareTo(prefixVals[i]) < 0, "check sort order");
            }

            // check the prefix encoding, lower precision should have the difference to original value equal to the lower removed bits
            var bytesRef = new BytesRef(NumericUtils.BUF_SIZE_LONG);
            foreach (var t in vals)
            {
                for (var j = 0; j < 32; j++)
                {
                    NumericUtils.IntToPrefixCodedBytes(t, j, bytesRef);
                    var prefixVal = NumericUtils.PrefixCodedToInt(bytesRef);
                    var mask = (1 << j) - 1;
                    Assert.IsTrue((t & mask).Equals(t - prefixVal), 
                                  "difference between prefix val and original value for " + t + " with shift=" + j);
                    //Assert.Equals(
                    // "difference between prefix val and original value for " + vals[i] + " with shift=" + j,
                    // vals[i] & mask, vals[i] - prefixVal);
                }
            }
        }

        [Test]
        public virtual void TestDoubles()
        {
            var vals = new double[]
                {
                    double.NegativeInfinity, -2.3E25, -1.0E15, -1.0, -1.0E-1, -1.0E-2, -0.0,
                    +0.0, 1.0E-2, 1.0E-1, 1.0, 1.0E15, 2.3E25, double.PositiveInfinity, double.NaN
                };
            var longVals = new long[vals.Length];

            // check forward and back conversion
            for (var i = 0; i < vals.Length; i++)
            {
                longVals[i] = NumericUtils.DoubleToSortableLong(vals[i]);
                Assert.IsTrue(vals[i].CompareTo(NumericUtils.SortableLongToDouble(longVals[i])) == 0,
                    "forward and back conversion should generate same double");
                           //double.Compare(vals[i], NumericUtils.SortableLongToDouble(longVals[i])) == 0);
            }

            // check sort order (prefixVals should be ascending)
            for (var i = 1; i < longVals.Length; i++)
            {
                Assert.IsTrue(longVals[i - 1] < longVals[i], "check sort order");
            }
        }

        public static readonly double[] DOUBLE_NANs =
            {
                double.NaN,
                double.LongBitsToDouble(0x7ff0000000000001L),
                double.LongBitsToDouble(0x7fffffffffffffffL),
                double.LongBitsToDouble(0xfff0000000000001L),
                double.LongBitsToDouble(0xffffffffffffffffL)
            };

        [Test]
        public virtual void TestSortableDoubleNaN()
        {
            var plusInf = NumericUtils.DoubleToSortableLong(double.PositiveInfinity);
            foreach (var nan in DOUBLE_NANs)
            {
                Assert.IsTrue(double.IsNaN(nan));
                var sortable = NumericUtils.DoubleToSortableLong(nan);
                Assert.IsTrue(sortable > plusInf,
                    "double not sorted correctly: " + nan + ", long repr: "
                           + sortable + ", positive inf.: " + plusInf);
            }
        }

        [Test]
        public virtual void TestFloats()
        {
            var vals = new float[]
                {
                    float.NegativeInfinity, -2.3E25f, -1.0E15f, -1.0f, -1.0E-1f, -1.0E-2f, -0.0f,
                    +0.0f, 1.0E-2f, 1.0E-1f, 1.0f, 1.0E15f, 2.3E25f, float.PositiveInfinity, float.NaN
                };
            var intVals = new int[vals.Length];

            // check forward and back conversion
            for (var i = 0; i < vals.Length; i++)
            {
                intVals[i] = NumericUtils.FloatToSortableInt(vals[i]);
                Assert.IsTrue(vals[i].CompareTo(NumericUtils.SortableIntToFloat(intVals[i])) == 0,
                    "forward and back conversion should generate same double");
            }

            // check sort order (prefixVals should be ascending)
            for (var i = 1; i < intVals.Length; i++)
            {
                Assert.IsTrue(intVals[i - 1] < intVals[i], "check sort order");
            }
        }

        public static readonly float[] FLOAT_NANs =
            {
                float.NaN,
                float.IntBitsToFloat(0x7f800001),
                float.IntBitsToFloat(0x7fffffff),
                float.IntBitsToFloat(0xff800001),
                float.IntBitsToFloat(0xffffffff)
            };

        [Test]
        public virtual void TestSortableFloatNaN()
        {
            var plusInf = NumericUtils.FloatToSortableInt(float.PositiveInfinity);
            foreach (var nan in FLOAT_NANs)
            {
                Assert.IsTrue(float.IsNaN(nan));
                var sortable = NumericUtils.FloatToSortableInt(nan);
                Assert.IsTrue(sortable > plusInf, "float not sorted correctly: " + nan + ", int repr: "
                           + sortable + ", positive inf.: " + plusInf);
            }
        }

        // INFO: Tests for trieCodeLong()/trieCodeInt() not needed because implicitely tested by range filter tests

        private sealed class AnonymousLongRangeBuilder : NumericUtils.LongRangeBuilder
        {
            private long lower, upper;
            private OpenBitSet bits;
            private bool useBitSet;
            private IEnumerator<long> neededShifts, neededBounds;
 
            public AnonymousLongRangeBuilder(long lower, long upper, OpenBitSet bits, bool useBitSet,
                                             IEnumerator<long> neededShifts, IEnumerator<long> neededBounds)
            {
                this.lower = lower;
                this.upper = upper;
                this.bits = bits;
                this.useBitSet = useBitSet;
                this.neededBounds = neededBounds;
                this.neededShifts = neededShifts;
            }

            public override void AddRange(long min, long max, int shift)
            {
                Assert.IsTrue(min >= lower && min <= upper && max >= lower && max <= upper,
                    "min, max should be inside bounds");
                if (useBitSet)
                    for (long l = min; l <= max; l++)
                    {
                        Assert.IsFalse(bits.GetAndSet(l - lower), "ranges should not overlap");
                        // extra exit condition to prevent overflow on MaxValue
                        if (l == max) break;
                    }
                if (neededBounds == null || neededShifts == null)
                    return;
                // make unsigned longs for easier display and understanding
                min ^= 0x8000000000000000L;
                max ^= 0x8000000000000000L;
                //System.out.println("0x"+long.toHexString(min>>>shift)+"L,0x"+long.toHexString(max>>>shift)+"L)/*shift="+shift+"*/,");
                Assert.IsTrue(neededShifts.MoveNext());
                Assert.IsTrue(neededShifts.Current.Equals(shift), "shift");
                Assert.IsTrue(neededBounds.MoveNext());
                Assert.IsTrue(neededBounds.Current.Equals(Number.URShift(min, shift)), "inner min bound");
                Assert.IsTrue(neededBounds.MoveNext());
                Assert.IsTrue(neededBounds.Current.Equals(Number.URShift(max, shift)), "inner max bound");
            }
        }

        /** Note: The neededBounds IEnumerable must be unsigned (easier understanding what's happening) */

        private void AssertLongRangeSplit(long lower, long upper, int precisionStep,
                                          bool useBitSet, IEnumerable<long> expectedBounds, IEnumerable<int> expectedShifts
            )
        {
            // Cannot use FixedBitSet since the range could be long:
            var bits = useBitSet ? new OpenBitSet(upper - lower + 1) : null;
            var neededBounds = (expectedBounds == null) ? null : expectedBounds.GetEnumerator();
            var neededShifts = (expectedShifts == null) ? null : expectedShifts.GetEnumerator();

            NumericUtils.SplitLongRange(new AnonymousLongRangeBuilder(), precisionStep, lower, upper);

            if (useBitSet)
            {
                // after flipping all bits in the range, the cardinality should be zero
                bits.Flip(0, upper - lower + 1);
                Assert.IsTrue(bits.Cardinality.Equals(0), "The sub-range concenated should match the whole range");
            }
        }

        /** LUCENE-2541: NumericRangeQuery errors with endpoints near long min and max values */
        [Test]
        public virtual void TestLongExtremeValues()
        {
            // upper end extremes
            AssertLongRangeSplit(long.MaxValue, long.MaxValue, 1, true, new long[]
                {
                    0xffffffffffffffffL, 
                    0xffffffffffffffffL
                }, new int[] {0});

            AssertLongRangeSplit(long.MaxValue, long.MaxValue, 2, true, new long[]
                {
                    0xffffffffffffffffL,
                    0xffffffffffffffffL
                }, new int[] {0});

            AssertLongRangeSplit(long.MaxValue, long.MaxValue, 4, true, new long[]
                {
                    0xffffffffffffffffL, 
                    0xffffffffffffffffL
                }, new int[] {0});

            AssertLongRangeSplit(long.MaxValue, long.MaxValue, 6, true, new long[]
                {
                    0xffffffffffffffffL, 
                    0xffffffffffffffffL
                }, new int[] {0});

            AssertLongRangeSplit(long.MaxValue, long.MaxValue, 8, true, new long[]
                {
                    0xffffffffffffffffL,
                    0xffffffffffffffffL
                }, new int[] {0});

            AssertLongRangeSplit(long.MaxValue, long.MinValue, 64, true, new long[]
                {
                    0xffffffffffffffffL,
                    0xffffffffffffffffL
                }, new int[] {0});

            AssertLongRangeSplit(long.MaxValue - 0xfL, long.MaxValue, 4, true, new long[]
                {
                    0xfffffffffffffffL,
                    0xfffffffffffffffL
                }, new int[] {4});

            AssertLongRangeSplit(long.MaxValue - 0x10L, long.MaxValue, 4, true, new long[]
                {
                    0xffffffffffffffefL,
                    0xffffffffffffffefL,
                    0xfffffffffffffffL,
                    0xfffffffffffffffL
                }, new int[] {0, 4});

            // lower end extremes
            AssertLongRangeSplit(long.MinValue, long.MinValue, 1, true, new long[]
                {
                    0x0000000000000000L, 
                    0x0000000000000000L
                }, new int[] {0});

            AssertLongRangeSplit(long.MinValue, long.MinValue, 2, true, new long[]
                {
                    0x0000000000000000L, 
                    0x0000000000000000L
                }, new int[] {0});

            AssertLongRangeSplit(long.MinValue, long.MinValue, 4, true, new long[]
                {
                     0x0000000000000000L, 
                     0x0000000000000000L
                }, new int[] {0});

            AssertLongRangeSplit(long.MinValue, long.MinValue, 6, true, new long[]
                {
                    0x0000000000000000L,
                    0x0000000000000000L
                }, new int[] {0});

            AssertLongRangeSplit(long.MinValue, long.MinValue, 8, true, new long[]
                {
                    0x0000000000000000L,
                    0x0000000000000000L
                }, new int[] {0});

            AssertLongRangeSplit(long.MinValue, long.MinValue, 64, true, new long[]
                {
                    0x0000000000000000L,
                    0x0000000000000000L
                }, new int[] {0});

            AssertLongRangeSplit(long.MinValue, long.MaxValue + 0xfL, 4, true, new long[]
                {
                    0x000000000000000L, 
                    0x000000000000000L
                }, new int[] {4});

            AssertLongRangeSplit(long.MinValue, long.MaxValue + 0x10L, 4, true, new long[]
                {
                    0x0000000000000010L, 
                    0x0000000000000010L,
                    0x000000000000000L,
                    0x000000000000000L
                }, 
                new int[] {0, 4});
        }

        [Test]
        public virtual void TestRandomSplit()
        {
            var num = (long) AtLeast(10);
            for (long i = 0; i < num; i++)
            {
                ExecuteOneRandomSplit(new Random());
            }
        }

        private void ExecuteOneRandomSplit(Random random)
        {
            var lower = RandomLong(random);
            long len = random.Next(16384*1024); // not too large bitsets, else OOME!
            while (lower + len < lower)
            {
                // overflow
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
                    val = 1L << (random.Next(63));
                        //  patterns like 0x000000100000 (-1 yields patterns like 0x0000fff)
                    break;
                case 1:
                    val = -1L << (random.Next(63)); // patterns like 0xfffff00000
                    break;
                default:
                    val = random.NextLong();
            }

            val += random.Next(5) - 2;

            if (random.NextBool())
            {
                if (random.NextBool()) val += random.Next(100) - 50;
                if (random.NextBool()) val = ~val;
                if (random.NextBool()) val = val << 1;
                if (random.NextBool()) val = Number.URShift(val, 1);
            }

            return val;
        }

        [Test]
        public virtual void TestSplitLongRange()
        {
            // a hard-coded "standard" range
            AssertLongRangeSplit(-5000L, 9500L, 4, true, new long[]
                {
                    0x7fffffffffffec78L,
                    0x7fffffffffffec7fL,
                    0x8000000000002510L,
                    0x800000000000251cL,
                    0x7fffffffffffec8L,
                    0x7fffffffffffecfL,
                    0x800000000000250L,
                    0x800000000000250L,
                    0x7fffffffffffedL,
                    0x7fffffffffffefL,
                    0x80000000000020L,
                    0x80000000000024L,
                    0x7ffffffffffffL,
                    0x8000000000001L
                },
                new int[]
                    {
                        0,
                        0,
                        4,
                        4,
                        8,
                        8,
                        12
                    });

            // the same with no range splitting
            AssertLongRangeSplit(-5000L, 9500L, 64, true, new long[] { 0x7fffffffffffec78L, 0x800000000000251cL}, new int[] {0});

            // this tests optimized range splitting, if one of the inner bounds
            // is also the bound of the next lower precision, it should be used completely
            AssertLongRangeSplit(0L, 1024L + 63L, 4, true, new long[] {0x800000000000040L, 0x800000000000043L, 0x80000000000000L, 0x80000000000003L}, new int[] {4, 8});

            // the full long range should only consist of a lowest precision range; no bitset testing here, as too much memory needed :-)
            AssertLongRangeSplit(long.MinValue, long.MaxValue, 8, false, new long[] { 0x00L, 0xffL}, new int[] {56});

            // the same with precisionStep=4
            AssertLongRangeSplit(long.MinValue, long.MaxValue, 4, false, new long[] {0x0L, 0xfL}, new int[] {60});

            // the same with precisionStep=2
            AssertLongRangeSplit(long.MinValue, long.MaxValue, 2, false, new long[] {0x0L, 0x3L}, new int[] {62});

            // the same with precisionStep=1
            AssertLongRangeSplit(long.MinValue, long.MaxValue, 1, false, new long[] {0x0L, 0x1L}, new int[] {63});

            // a inverse range should produce no sub-ranges
            AssertLongRangeSplit(9500L, -5000L, 4, false, new long[0], new int[0]);

            // a 0-Length range should reproduce the range itself
            AssertLongRangeSplit(9500L, 9500L, 4, false, new long[] { 0x800000000000251cL, 0x800000000000251cL }, new int[] {0});
        }

        private sealed class AnonymousIntRangeBuilder : NumericUtils.IntRangeBuilder
        {
            private int lower, upper;
            private bool useBitSet;
            private FixedBitSet bits;
            private IEnumerator<int> neededBounds, neededShifts; 

            public override void AddRange(int min, int max, int shift)
            {
                Assert.IsTrue(min >= lower && min <= upper && max >= lower && max <= upper,
                    "min, max should be inside bounds");
                if (useBitSet)
                    for (int i = min; i <= max; i++)
                    {
                        Assert.IsFalse(bits.GetAndSet(i - lower), "ranges should not overlap");
                        // extra exit condition to prevent overflow on MaxValue
                        if (i == max) break;
                    }
                if (neededBounds == null)
                    return;
                // make unsigned ints for easier display and understanding
                min ^= 0x80000000;
                max ^= 0x80000000;
                //System.out.println("0x"+int.toHexString(min>>>shift)+",0x"+int.toHexString(max>>>shift)+")/*shift="+shift+"*/,");
                Assert.IsTrue(neededShifts.MoveNext());
                Assert.IsTrue(neededShifts.Current.Equals(shift), "shift");
                Assert.IsTrue(neededBounds.MoveNext());
                Assert.IsTrue(neededBounds.Current.Equals(Number.URShift(min, shift)), "inner min bound");
                Assert.IsTrue(neededBounds.MoveNext());
                Assert.IsTrue(neededBounds.Current.Equals(Number.URShift(max, shift)), "inner max bound");
            }
        }

        /** Note: The neededBounds IEnumerable must be unsigned (easier understanding what's happening) */

        private void AssertIntRangeSplit(int lower, int upper, int precisionStep,
                                         bool useBitSet, IEnumerable<int> expectedBounds, IEnumerable<int> expectedShifts)
        {
            var bits = useBitSet ? new FixedBitSet(upper - lower + 1) : null;
            var neededBounds = (expectedBounds == null) ? null : expectedBounds.GetEnumerator();
            var neededShifts = (expectedShifts == null) ? null : expectedShifts.GetEnumerator();

            NumericUtils.SplitIntRange(new AnonymousIntRangeBuilder(), precisionStep, lower, upper);

            if (useBitSet)
            {
                // after flipping all bits in the range, the cardinality should be zero
                bits.Flip(0, upper - lower + 1);
                Assert.IsTrue(bits.Cardinality().Equals(0), "The sub-range concenated should match the whole range");
            }
        }

        [Test]
        public virtual void TestSplitIntRange()
        {
            // a hard-coded "standard" range
            AssertIntRangeSplit(-5000, 9500, 4, true, new int[] {
                0x7fffec78, 0x7fffec7f,
                0x80002510, 0x8000251c,
                0x7fffec8, 0x7fffecf,
                0x8000250, 0x8000250,
                0x7fffed, 0x7fffef,
                0x800020, 0x800024,
                0x7ffff, 0x80001
            }, new int[] {0, 0, 4, 4, 8, 8, 12 } );

            // the same with no range splitting
            AssertIntRangeSplit(-5000, 9500, 32, true, new int[] {0x7fffec78, 0x8000251c}, new int[] {0});

            // this tests optimized range splitting, if one of the inner bounds
            // is also the bound of the next lower precision, it should be used completely
            AssertIntRangeSplit(0, 1024 + 63, 4, true, new int[] {0x8000040, 0x8000043, 0x800000, 0x800003}, new int[] {4, 8});

            // the full int range should only consist of a lowest precision range; no bitset testing here, as too much memory needed :-)
            AssertIntRangeSplit(int.MinValue, int.MaxValue, 8, false, new int[] {0x00, 0xff}, new int[] {24});

            // the same with precisionStep=4
            AssertIntRangeSplit(int.MinValue, int.MaxValue, 4, false, new int[] {0x0, 0xf}, new int[] {28});

            // the same with precisionStep=2
            AssertIntRangeSplit(int.MinValue, int.MinValue, 2, false, new int[] {0x0, 0x3}, new int[] {30});

            // the same with precisionStep=1
            AssertIntRangeSplit(int.MinValue, int.MaxValue, 1, false, new int[] { 0x0, 0x1 }, new int[] { 31 });

            // a inverse range should produce no sub-ranges
            AssertIntRangeSplit(9500, -5000, 4, false, new int[0], new int[0]);

            // a 0-Length range should reproduce the range itself
            AssertIntRangeSplit(9500, 9500, 4, false, new int[] { 0x8000251c, 0x8000251c }, new int[] { 0 });
        }
    }
}