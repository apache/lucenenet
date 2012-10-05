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
using System.Linq;
using Lucene.Net.Support;
using NUnit.Framework;

namespace Lucene.Net.Util
{
	
    [TestFixture]
	public class TestNumericUtils:LuceneTestCase
	{
		private class AnonymousClassLongRangeBuilder:NumericUtils.LongRangeBuilder
		{
			public AnonymousClassLongRangeBuilder(long lower, long upper, bool useBitSet, Lucene.Net.Util.OpenBitSet bits, System.Collections.IEnumerator neededBounds, System.Collections.IEnumerator neededShifts,TestNumericUtils enclosingInstance)
			{
				InitBlock(lower, upper, useBitSet, bits, neededBounds, neededShifts, enclosingInstance);
			}
			private void  InitBlock(long lower, long upper, bool useBitSet, Lucene.Net.Util.OpenBitSet bits, System.Collections.IEnumerator neededBounds, System.Collections.IEnumerator neededShifts,TestNumericUtils enclosingInstance)
			{
				this.lower = lower;
				this.upper = upper;
				this.useBitSet = useBitSet;
				this.bits = bits;
				this.neededBounds = neededBounds;
                this.neededShifts = neededShifts;
				this.enclosingInstance = enclosingInstance;
			}
			private long lower;
			private long upper;
			private bool useBitSet;
			private Lucene.Net.Util.OpenBitSet bits;
			private System.Collections.IEnumerator neededBounds;
            private System.Collections.IEnumerator neededShifts;
			private TestNumericUtils enclosingInstance;
			public TestNumericUtils Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			//@Override
			public override void  AddRange(long min, long max, int shift)
			{
				Assert.IsTrue(min >= lower && min <= upper && max >= lower && max <= upper, "min, max should be inside bounds");
				if (useBitSet)
					for (long l = min; l <= max; l++)
					{
						Assert.IsFalse(bits.GetAndSet(l - lower), "ranges should not overlap");
                        // extra exit condition to prevent overflow on MAX_VALUE
                        if (l == max) break;
					}
                if (neededBounds == null || neededShifts == null) return;
				// make unsigned longs for easier display and understanding
				min ^= unchecked((long) 0x8000000000000000L);
				max ^= unchecked((long) 0x8000000000000000L);
				//System.out.println("Long.valueOf(0x"+Long.toHexString(min>>>shift)+"L),Long.valueOf(0x"+Long.toHexString(max>>>shift)+"L)/*shift="+shift+"*/,");
                neededShifts.MoveNext();
                Assert.AreEqual(((Int32)neededShifts.Current), shift, "shift");
                neededBounds.MoveNext();
                unchecked
                {
                    Assert.AreEqual((long)neededBounds.Current, Number.URShift(min, shift), "inner min bound");
                    neededBounds.MoveNext();
                    Assert.AreEqual((long)neededBounds.Current, Number.URShift(max, shift), "inner max bound");
                }
			}
		}

		private class AnonymousClassIntRangeBuilder:NumericUtils.IntRangeBuilder
		{
            public AnonymousClassIntRangeBuilder(int lower, int upper, bool useBitSet, Lucene.Net.Util.OpenBitSet bits, IEnumerator<int> neededBounds, IEnumerator<int> neededShifts, TestNumericUtils enclosingInstance)
			{
                InitBlock(lower, upper, useBitSet, bits, neededBounds, neededShifts,enclosingInstance);
			}
            private void InitBlock(int lower, int upper, bool useBitSet, Lucene.Net.Util.OpenBitSet bits, IEnumerator<int> neededBounds, IEnumerator<int> neededShifts, TestNumericUtils enclosingInstance)
			{
				this.lower = lower;
				this.upper = upper;
				this.useBitSet = useBitSet;
				this.bits = bits;
				this.neededBounds = neededBounds;
                this.neededShifts = neededShifts;
				this.enclosingInstance = enclosingInstance;
			}
			private int lower;
			private int upper;
			private bool useBitSet;
			private Lucene.Net.Util.OpenBitSet bits;
            private IEnumerator<int> neededBounds;
            private IEnumerator<int> neededShifts;
			private TestNumericUtils enclosingInstance;
			public TestNumericUtils Enclosing_Instance
			{
				get
				{
					return enclosingInstance;
				}
				
			}
			//@Override
			public override void  AddRange(int min, int max, int shift)
			{
				Assert.IsTrue(min >= lower && min <= upper && max >= lower && max <= upper, "min, max should be inside bounds");
				if (useBitSet)
					for (int i = min; i <= max; i++)
					{
						Assert.IsFalse(bits.GetAndSet(i - lower), "ranges should not overlap");
                        // extra exit condition to prevent overflow on MAX_VALUE
                        if (i == max) break;
					}
                if (neededBounds == null) return;
				// make unsigned ints for easier display and understanding
				min ^= unchecked((int) 0x80000000);
				max ^= unchecked((int) 0x80000000);
                neededShifts.MoveNext();
                Assert.AreEqual(((int)neededShifts.Current), shift,"shift");
				//System.out.println("new Integer(0x"+Integer.toHexString(min>>>shift)+"),new Integer(0x"+Integer.toHexString(max>>>shift)+"),");
                neededBounds.MoveNext();
				Assert.AreEqual(((System.Int32) neededBounds.Current), Number.URShift(min, shift), "inner min bound");
                neededBounds.MoveNext();
				Assert.AreEqual(((System.Int32) neededBounds.Current), Number.URShift(max, shift), "inner max bound");
			}
		}
		
        [Test]
		public virtual void  TestLongConversionAndOrdering()
		{
			// generate a series of encoded longs, each numerical one bigger than the one before
			System.String last = null;
			for (long l = - 100000L; l < 100000L; l++)
			{
				System.String act = NumericUtils.LongToPrefixCoded(l);
				if (last != null)
				{
					// test if smaller
					Assert.IsTrue(String.CompareOrdinal(last, act) < 0, "actual bigger than last");
				}
				// test is back and forward conversion works
				Assert.AreEqual(l, NumericUtils.PrefixCodedToLong(act), "forward and back conversion should generate same long");
				// next step
				last = act;
			}
		}
		
        [Test]
		public virtual void  TestIntConversionAndOrdering()
		{
			// generate a series of encoded ints, each numerical one bigger than the one before
			System.String last = null;
			for (int i = - 100000; i < 100000; i++)
			{
				System.String act = NumericUtils.IntToPrefixCoded(i);
				if (last != null)
				{
					// test if smaller
					Assert.IsTrue(String.CompareOrdinal(last, act) < 0, "actual bigger than last");
				}
				// test is back and forward conversion works
				Assert.AreEqual(i, NumericUtils.PrefixCodedToInt(act), "forward and back conversion should generate same int");
				// next step
				last = act;
			}
		}
		
        [Test]
		public virtual void  TestLongSpecialValues()
		{
			long[] vals = new long[]{System.Int64.MinValue, System.Int64.MinValue + 1, System.Int64.MinValue + 2, - 5003400000000L, - 4000L, - 3000L, - 2000L, - 1000L, - 1L, 0L, 1L, 10L, 300L, 50006789999999999L, System.Int64.MaxValue - 2, System.Int64.MaxValue - 1, System.Int64.MaxValue};
			System.String[] prefixVals = new System.String[vals.Length];
			
			for (int i = 0; i < vals.Length; i++)
			{
				prefixVals[i] = NumericUtils.LongToPrefixCoded(vals[i]);
				
				// check forward and back conversion
				Assert.AreEqual(vals[i], NumericUtils.PrefixCodedToLong(prefixVals[i]), "forward and back conversion should generate same long");
				
				// test if decoding values as int fails correctly
			    Assert.Throws<FormatException>(() => NumericUtils.PrefixCodedToInt(prefixVals[i]),
			                                   "decoding a prefix coded long value as int should fail");
			}
			
			// check sort order (prefixVals should be ascending)
			for (int i = 1; i < prefixVals.Length; i++)
			{
				Assert.IsTrue(String.CompareOrdinal(prefixVals[i - 1], prefixVals[i]) < 0, "check sort order");
			}
			
			// check the prefix encoding, lower precision should have the difference to original value equal to the lower removed bits
			for (int i = 0; i < vals.Length; i++)
			{
				for (int j = 0; j < 64; j++)
				{
					long prefixVal = NumericUtils.PrefixCodedToLong(NumericUtils.LongToPrefixCoded(vals[i], j));
					long mask = (1L << j) - 1L;
					Assert.AreEqual(vals[i] & mask, vals[i] - prefixVal, "difference between prefix val and original value for " + vals[i] + " with shift=" + j);
				}
			}
		}
		
        [Test]
		public virtual void  TestIntSpecialValues()
		{
			int[] vals = new int[]{System.Int32.MinValue, System.Int32.MinValue + 1, System.Int32.MinValue + 2, - 64765767, - 4000, - 3000, - 2000, - 1000, - 1, 0, 1, 10, 300, 765878989, System.Int32.MaxValue - 2, System.Int32.MaxValue - 1, System.Int32.MaxValue};
			System.String[] prefixVals = new System.String[vals.Length];
			
			for (int i = 0; i < vals.Length; i++)
			{
				prefixVals[i] = NumericUtils.IntToPrefixCoded(vals[i]);
				
				// check forward and back conversion
				Assert.AreEqual(vals[i], NumericUtils.PrefixCodedToInt(prefixVals[i]), "forward and back conversion should generate same int");
				
				// test if decoding values as long fails correctly
			    Assert.Throws<FormatException>(() => NumericUtils.PrefixCodedToLong(prefixVals[i]),
			                                   "decoding a prefix coded int value as long should fail");
			}
			
			// check sort order (prefixVals should be ascending)
			for (int i = 1; i < prefixVals.Length; i++)
			{
				Assert.IsTrue(String.CompareOrdinal(prefixVals[i - 1], prefixVals[i]) < 0, "check sort order");
			}
			
			// check the prefix encoding, lower precision should have the difference to original value equal to the lower removed bits
			for (int i = 0; i < vals.Length; i++)
			{
				for (int j = 0; j < 32; j++)
				{
					int prefixVal = NumericUtils.PrefixCodedToInt(NumericUtils.IntToPrefixCoded(vals[i], j));
					int mask = (1 << j) - 1;
					Assert.AreEqual(vals[i] & mask, vals[i] - prefixVal, "difference between prefix val and original value for " + vals[i] + " with shift=" + j);
				}
			}
		}
		
        [Test]
		public virtual void  TestDoubles()
		{
			double[] vals = new double[]{System.Double.NegativeInfinity, - 2.3e25, - 1.0e15, - 1.0, - 1.0e-1, - 1.0e-2, - 0.0, + 0.0, 1.0e-2, 1.0e-1, 1.0, 1.0e15, 2.3e25, System.Double.PositiveInfinity};
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
		
        [Test]
		public virtual void  TestFloats()
		{
			float[] vals = new float[]{System.Single.NegativeInfinity, - 2.3e25f, - 1.0e15f, - 1.0f, - 1.0e-1f, - 1.0e-2f, - 0.0f, + 0.0f, 1.0e-2f, 1.0e-1f, 1.0f, 1.0e15f, 2.3e25f, System.Single.PositiveInfinity};
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
		
		// INFO: Tests for trieCodeLong()/trieCodeInt() not needed because implicitely tested by range filter tests
		
		/// <summary>Note: The neededBounds iterator must be unsigned (easier understanding what's happening) </summary>
        internal virtual void AssertLongRangeSplit(long lower, long upper, int precisionStep, bool useBitSet, IEnumerator<long> neededBounds, IEnumerator<int> neededShifts)
		{
		    OpenBitSet bits = useBitSet ? new OpenBitSet(upper - lower + 1) : null;

		    NumericUtils.SplitLongRange(
		        new AnonymousClassLongRangeBuilder(lower, upper, useBitSet, bits, neededBounds, neededShifts, this),
		        precisionStep, lower, upper);

		    if (useBitSet)
		    {
		        // after flipping all bits in the range, the cardinality should be zero
		        bits.Flip(0, upper - lower + 1);
		        Assert.IsTrue(bits.IsEmpty(), "The sub-range concenated should match the whole range");
		    }
		}

        /* LUCENE-2541: NumericRangeQuery errors with endpoints near long min and max values */
        [Test]
        public void TestLongExtremeValues()
        {
            // upper end extremes
            AssertLongRangeSplit(long.MaxValue, long.MaxValue, 1, true,
                new ulong[] { 0xffffffffffffffffL, 0xffffffffffffffffL }.Cast<long>().GetEnumerator(),
                new int[] { 0 }.AsEnumerable().GetEnumerator());

            AssertLongRangeSplit(long.MaxValue, long.MaxValue, 2, true,
                new ulong[] { 0xffffffffffffffffL, 0xffffffffffffffffL }.Cast<long>().GetEnumerator(),
                new int[] { 0 }.AsEnumerable().GetEnumerator());

            AssertLongRangeSplit(long.MaxValue, long.MaxValue, 4, true,
                new ulong[] { 0xffffffffffffffffL, 0xffffffffffffffffL }.Cast<long>().GetEnumerator(),
                new int[] { 0 }.AsEnumerable().GetEnumerator());

            AssertLongRangeSplit(long.MaxValue, long.MaxValue, 6, true,
                new ulong[] { 0xffffffffffffffffL, 0xffffffffffffffffL }.Cast<long>().GetEnumerator(),
                new int[] { 0 }.AsEnumerable().GetEnumerator());

            AssertLongRangeSplit(long.MaxValue, long.MaxValue, 8, true,
                new ulong[] { 0xffffffffffffffffL, 0xffffffffffffffffL }.Cast<long>().GetEnumerator(),
                new int[] { 0 }.AsEnumerable().GetEnumerator());

            AssertLongRangeSplit(long.MaxValue, long.MaxValue, 64, true,
                new ulong[] { 0xffffffffffffffffL, 0xffffffffffffffffL }.Cast<long>().GetEnumerator(),
                new int[] { 0 }.AsEnumerable().GetEnumerator());
            
            AssertLongRangeSplit(long.MaxValue - 0xfL, long.MaxValue, 4, true,
                new ulong[] { 0xfffffffffffffffL, 0xfffffffffffffffL }.Cast<long>().GetEnumerator(),
                new int[] { 4 }.AsEnumerable().GetEnumerator());
            AssertLongRangeSplit(long.MaxValue - 0x10L, long.MaxValue, 4, true,
                new ulong[] { 0xffffffffffffffefL, 0xffffffffffffffefL, 0xfffffffffffffffL, 0xfffffffffffffffL }.Cast<long>().GetEnumerator(),
                new int[] { 0, 4 }.AsEnumerable().GetEnumerator());

            // lower end extremes
            AssertLongRangeSplit(long.MinValue, long.MinValue, 1, true,
                new long[] { 0x0000000000000000L, 0x0000000000000000L }.Cast<long>().GetEnumerator(),
                new int[] { 0 }.AsEnumerable().GetEnumerator());

            AssertLongRangeSplit(long.MinValue, long.MinValue, 2, true,
                new long[] { 0x0000000000000000L, 0x0000000000000000L }.Cast<long>().GetEnumerator(),
                new int[] { 0 }.AsEnumerable().GetEnumerator());

            AssertLongRangeSplit(long.MinValue, long.MinValue, 4, true,
                new long[] { 0x0000000000000000L, 0x0000000000000000L }.Cast<long>().GetEnumerator(),
                new int[] { 0 }.AsEnumerable().GetEnumerator());

            AssertLongRangeSplit(long.MinValue, long.MinValue, 6, true,
                new long[] { 0x0000000000000000L, 0x0000000000000000L }.Cast<long>().GetEnumerator(),
                new int[] { 0 }.AsEnumerable().GetEnumerator());

            AssertLongRangeSplit(long.MinValue, long.MinValue, 8, true,
                new long[] { 0x0000000000000000L, 0x0000000000000000L }.Cast<long>().GetEnumerator(),
                new int[] { 0 }.AsEnumerable().GetEnumerator());

            AssertLongRangeSplit(long.MinValue, long.MinValue, 64, true,
                new long[] { 0x0000000000000000L, 0x0000000000000000L }.Cast<long>().GetEnumerator(),
                new int[] { 0 }.AsEnumerable().GetEnumerator());


            AssertLongRangeSplit(long.MinValue, long.MinValue + 0xfL, 4, true,
                new long[] { 0x000000000000000L, 0x000000000000000L }.Cast<long>().GetEnumerator(),
                new int[] { 4 }.AsEnumerable().GetEnumerator());
            AssertLongRangeSplit(long.MinValue, long.MinValue + 0x10L, 4, true,
                new long[] { 0x0000000000000010L, 0x0000000000000010L, 0x000000000000000L, 0x000000000000000L }.Cast<long>().GetEnumerator(),
                new int[] { 0, 4 }.AsEnumerable().GetEnumerator());
        }

        [Test]
        public void TestRandomSplit()
        {
            Random random = new Random();
            for (int i = 0; i < 100; i++)
            {
                ExecuteOneRandomSplit(random);
            }
        }

        private void ExecuteOneRandomSplit(Random random)
        {
            long lower = RandomLong(random);
            long len = (long)random.Next(16384 * 1024); // not too large bitsets, else OOME!
            while (lower + len < lower)
            { // overflow
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
                    val = random.Next();
                    break;
            }

            val += random.Next(5) - 2;

            if (random.Next(2) == 1)
            {
                if (random.Next(2) == 1) val += random.Next(100) - 50;
                if (random.Next(2) == 1) val = ~val;
                if (random.Next(2) == 1) val = val << 1;
                if (random.Next(2) == 1) val = Number.URShift(val, 1);
            }

            return val;
        }


        [Test]
		public void  TestSplitLongRange()
		{
			// a hard-coded "standard" range
            AssertLongRangeSplit(- 5000L, 9500L, 4, true,
                                 new System.Int64[]
                                     {
                                         0x7fffffffffffec78L, 0x7fffffffffffec7fL, unchecked((long) (0x8000000000002510L)),
                                         unchecked((long) (0x800000000000251cL)), 0x7fffffffffffec8L, 0x7fffffffffffecfL,
                                         0x800000000000250L, 0x800000000000250L, 0x7fffffffffffedL, 0x7fffffffffffefL,
                                         0x80000000000020L, 0x80000000000024L, 0x7ffffffffffffL, 0x8000000000001L
                                     }.Cast<long>().GetEnumerator(), new int[] {0, 0, 4, 4, 8, 8, 12}.Cast<int>().GetEnumerator());
			
			// the same with no range splitting
            AssertLongRangeSplit(-5000L, 9500L, 64, true,
                                 new System.Int64[] {0x7fffffffffffec78L, unchecked((long) (0x800000000000251cL))}.Cast
                                     <long>().GetEnumerator(), new int[] { 0 }.Cast<int>().GetEnumerator());
			
			// this tests optimized range splitting, if one of the inner bounds
			// is also the bound of the next lower precision, it should be used completely
            AssertLongRangeSplit(0L, 1024L + 63L, 4, true,
                                 new System.Int64[]
                                     {0x800000000000040L, 0x800000000000043L, 0x80000000000000L, 0x80000000000003L}.Cast
                                     <long>().GetEnumerator(), new int[] { 4, 8 }.Cast<int>().GetEnumerator());
			
			// the full long range should only consist of a lowest precision range; no bitset testing here, as too much memory needed :-)
            AssertLongRangeSplit(System.Int64.MinValue, System.Int64.MaxValue, 8, false,
                                 new System.Int64[] {0x00L, 0xffL}.Cast<long>().GetEnumerator(),
                                 new int[] { 56 }.Cast<int>().GetEnumerator());
			
			// the same with precisionStep=4
            AssertLongRangeSplit(System.Int64.MinValue, System.Int64.MaxValue, 4, false,
                                 new System.Int64[] {0x0L, 0xfL}.Cast<long>().GetEnumerator(),
                                 new int[] { 60 }.Cast<int>().GetEnumerator());
			
			// the same with precisionStep=2
            AssertLongRangeSplit(System.Int64.MinValue, System.Int64.MaxValue, 2, false,
                                 new System.Int64[] {0x0L, 0x3L}.Cast<long>().GetEnumerator(),
                                 new int[] {62}.Cast<int>().GetEnumerator());
			
			// the same with precisionStep=1
            AssertLongRangeSplit(System.Int64.MinValue, System.Int64.MaxValue, 1, false,
                                 new System.Int64[] {0x0L, 0x1L}.ToList().GetEnumerator(),
                                 new int[] {63}.Cast<int>().GetEnumerator());
			
			// a inverse range should produce no sub-ranges
            AssertLongRangeSplit(9500L, -5000L, 4, false,
                                 Enumerable.Empty<long>().GetEnumerator(),
                                 new int[] {}.Cast<int>().GetEnumerator());
			
			// a 0-length range should reproduce the range itsself
            AssertLongRangeSplit(9500L, 9500L, 4, false, new long[]
                                                             {
                                                                 unchecked((long) (0x800000000000251cL)),
                                                                 unchecked((long) (0x800000000000251cL))
                                                             }.Cast<long>().GetEnumerator(),
                                 new int[] {0}.Cast<int>().GetEnumerator());
		}
		

		/// <summary>Note: The neededBounds iterator must be unsigned (easier understanding what's happening) </summary>
		protected internal virtual void  AssertIntRangeSplit(int lower, int upper, int precisionStep, bool useBitSet, IEnumerator<int> neededBounds, IEnumerator<int> neededShifts)
		{
		    OpenBitSet bits = useBitSet ? new OpenBitSet(upper - lower + 1) : null;

            NumericUtils.SplitIntRange(new AnonymousClassIntRangeBuilder(lower, upper, useBitSet, bits, neededBounds, neededShifts,this), precisionStep, lower, upper);
			
			if (useBitSet)
			{
				// after flipping all bits in the range, the cardinality should be zero
				bits.Flip(0, upper - lower + 1);
				Assert.IsTrue(bits.IsEmpty(), "The sub-range concenated should match the whole range");
			}
		}
		
        [Test]
		public virtual void  TestSplitIntRange()
		{
			// a hard-coded "standard" range
            AssertIntRangeSplit(- 5000, 9500, 4, true,
                                new System.Int32[]
                                    {
                                        0x7fffec78, 0x7fffec7f, unchecked((System.Int32) 0x80002510),
                                        unchecked((System.Int32) 0x8000251c), 0x7fffec8, 0x7fffecf, 0x8000250, 0x8000250,
                                        0x7fffed, 0x7fffef, 0x800020, 0x800024, 0x7ffff, 0x80001
                                    }.Cast<int>().GetEnumerator
                                    (), new int[] { 0, 0, 4, 4, 8, 8, 12 }.Cast<int>().GetEnumerator());
			
			// the same with no range splitting
            AssertIntRangeSplit(-5000, 9500, 32, true,
                                new System.Int32[] {0x7fffec78, unchecked((System.Int32) 0x8000251c)}.Cast<int>().
                                    GetEnumerator(), new int[] { 0 }.Cast<int>().GetEnumerator());
			
			// this tests optimized range splitting, if one of the inner bounds
			// is also the bound of the next lower precision, it should be used completely
            AssertIntRangeSplit(0, 1024 + 63, 4, true,
                                new System.Int32[] {0x8000040, 0x8000043, 0x800000, 0x800003}.Cast<int>().GetEnumerator(),
                                new int[] { 4, 8 }.Cast<int>().GetEnumerator());
			
			// the full int range should only consist of a lowest precision range; no bitset testing here, as too much memory needed :-)
            AssertIntRangeSplit(System.Int32.MinValue, System.Int32.MaxValue, 8, false,
                                new System.Int32[] {0x00, 0xff}.Cast<int>().GetEnumerator(),
                                new int[] { 24 }.Cast<int>().GetEnumerator());
			
			// the same with precisionStep=4
            AssertIntRangeSplit(System.Int32.MinValue, System.Int32.MaxValue, 4, false,
                                new System.Int32[] {0x0, 0xf}.Cast<int>().GetEnumerator(),
                                new int[] {28}.Cast<int>().GetEnumerator());
			
			// the same with precisionStep=2
            AssertIntRangeSplit(System.Int32.MinValue, System.Int32.MaxValue, 2, false,
                                new System.Int32[] {0x0, 0x3}.Cast<int>().GetEnumerator(),
                                new int[] {30}.Cast<int>().GetEnumerator());
			
			// the same with precisionStep=1
            AssertIntRangeSplit(System.Int32.MinValue, System.Int32.MaxValue, 1, false,
                                new System.Int32[] {0x0, 0x1}.Cast<int>().GetEnumerator(),
                                new int[] {31}.Cast<int>().GetEnumerator());
			
			// a inverse range should produce no sub-ranges
            AssertIntRangeSplit(9500, -5000, 4, false, Enumerable.Empty<int>().GetEnumerator(),
                                new int[] {}.Cast<int>().GetEnumerator());
			
			// a 0-length range should reproduce the range itsself
            AssertIntRangeSplit(9500, 9500, 4, false, new System.Int32[]
                                                          {
                                                              unchecked((System.Int32) 0x8000251c),
                                                              unchecked((System.Int32) 0x8000251c)
                                                          }.Cast<int>().GetEnumerator(),
                                new int[] {0}.Cast<int>().GetEnumerator());
		}
	}
}