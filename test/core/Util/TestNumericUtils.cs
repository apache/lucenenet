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


	public class TestNumericUtils : LuceneTestCase
	{

	  public virtual void TestLongConversionAndOrdering()
	  {
		// generate a series of encoded longs, each numerical one bigger than the one before
		BytesRef last = null, act = new BytesRef(NumericUtils.BUF_SIZE_LONG);
		for (long l = -100000L; l < 100000L; l++)
		{
		  NumericUtils.longToPrefixCodedBytes(l, 0, act);
		  if (last != null)
		  {
			// test if smaller
			Assert.IsTrue("actual bigger than last (BytesRef)", last.compareTo(act) < 0);
			Assert.IsTrue("actual bigger than last (as String)", last.utf8ToString().compareTo(act.utf8ToString()) < 0);
		  }
		  // test is back and forward conversion works
		  Assert.AreEqual("forward and back conversion should generate same long", l, NumericUtils.prefixCodedToLong(act));
		  // next step
		  last = act;
		  act = new BytesRef(NumericUtils.BUF_SIZE_LONG);
		}
	  }

	  public virtual void TestIntConversionAndOrdering()
	  {
		// generate a series of encoded ints, each numerical one bigger than the one before
		BytesRef last = null, act = new BytesRef(NumericUtils.BUF_SIZE_INT);
		for (int i = -100000; i < 100000; i++)
		{
		  NumericUtils.intToPrefixCodedBytes(i, 0, act);
		  if (last != null)
		  {
			// test if smaller
			Assert.IsTrue("actual bigger than last (BytesRef)", last.compareTo(act) < 0);
			Assert.IsTrue("actual bigger than last (as String)", last.utf8ToString().compareTo(act.utf8ToString()) < 0);
		  }
		  // test is back and forward conversion works
		  Assert.AreEqual("forward and back conversion should generate same int", i, NumericUtils.prefixCodedToInt(act));
		  // next step
		  last = act;
		  act = new BytesRef(NumericUtils.BUF_SIZE_INT);
		}
	  }

	  public virtual void TestLongSpecialValues()
	  {
		long[] vals = new long[]{long.MinValue, long.MinValue+1, long.MinValue+2, -5003400000000L, -4000L, -3000L, -2000L, -1000L, -1L, 0L, 1L, 10L, 300L, 50006789999999999L, long.MaxValue-2, long.MaxValue-1, long.MaxValue};
		BytesRef[] prefixVals = new BytesRef[vals.Length];

		for (int i = 0; i < vals.Length; i++)
		{
		  prefixVals[i] = new BytesRef(NumericUtils.BUF_SIZE_LONG);
		  NumericUtils.longToPrefixCodedBytes(vals[i], 0, prefixVals[i]);

		  // check forward and back conversion
		  Assert.AreEqual("forward and back conversion should generate same long", vals[i], NumericUtils.prefixCodedToLong(prefixVals[i]));

		  // test if decoding values as int fails correctly
		  try
		  {
			NumericUtils.prefixCodedToInt(prefixVals[i]);
			Assert.Fail("decoding a prefix coded long value as int should fail");
		  }
		  catch (NumberFormatException e)
		  {
			// worked
		  }
		}

		// check sort order (prefixVals should be ascending)
		for (int i = 1; i < prefixVals.Length; i++)
		{
		  Assert.IsTrue("check sort order", prefixVals[i - 1].compareTo(prefixVals[i]) < 0);
		}

		// check the prefix encoding, lower precision should have the difference to original value equal to the lower removed bits
		BytesRef @ref = new BytesRef(NumericUtils.BUF_SIZE_LONG);
		for (int i = 0; i < vals.Length; i++)
		{
		  for (int j = 0; j < 64; j++)
		  {
			NumericUtils.longToPrefixCodedBytes(vals[i], j, @ref);
			long prefixVal = NumericUtils.prefixCodedToLong(@ref);
			long mask = (1L << j) - 1L;
			Assert.AreEqual("difference between prefix val and original value for " + vals[i] + " with shift=" + j, vals[i] & mask, vals[i] - prefixVal);
		  }
		}
	  }

	  public virtual void TestIntSpecialValues()
	  {
		int[] vals = new int[]{int.MinValue, int.MinValue+1, int.MinValue+2, -64765767, -4000, -3000, -2000, -1000, -1, 0, 1, 10, 300, 765878989, int.MaxValue-2, int.MaxValue-1, int.MaxValue};
		BytesRef[] prefixVals = new BytesRef[vals.Length];

		for (int i = 0; i < vals.Length; i++)
		{
		  prefixVals[i] = new BytesRef(NumericUtils.BUF_SIZE_INT);
		  NumericUtils.intToPrefixCodedBytes(vals[i], 0, prefixVals[i]);

		  // check forward and back conversion
		  Assert.AreEqual("forward and back conversion should generate same int", vals[i], NumericUtils.prefixCodedToInt(prefixVals[i]));

		  // test if decoding values as long fails correctly
		  try
		  {
			NumericUtils.prefixCodedToLong(prefixVals[i]);
			Assert.Fail("decoding a prefix coded int value as long should fail");
		  }
		  catch (NumberFormatException e)
		  {
			// worked
		  }
		}

		// check sort order (prefixVals should be ascending)
		for (int i = 1; i < prefixVals.Length; i++)
		{
		  Assert.IsTrue("check sort order", prefixVals[i - 1].compareTo(prefixVals[i]) < 0);
		}

		// check the prefix encoding, lower precision should have the difference to original value equal to the lower removed bits
		BytesRef @ref = new BytesRef(NumericUtils.BUF_SIZE_LONG);
		for (int i = 0; i < vals.Length; i++)
		{
		  for (int j = 0; j < 32; j++)
		  {
			NumericUtils.intToPrefixCodedBytes(vals[i], j, @ref);
			int prefixVal = NumericUtils.prefixCodedToInt(@ref);
			int mask = (1 << j) - 1;
			Assert.AreEqual("difference between prefix val and original value for " + vals[i] + " with shift=" + j, vals[i] & mask, vals[i] - prefixVal);
		  }
		}
	  }

	  public virtual void TestDoubles()
	  {
		double[] vals = new double[]{double.NegativeInfinity, -2.3E25, -1.0E15, -1.0, -1.0E-1, -1.0E-2, -0.0, +0.0, 1.0E-2, 1.0E-1, 1.0, 1.0E15, 2.3E25, double.PositiveInfinity, double.NaN};
		long[] longVals = new long[vals.Length];

		// check forward and back conversion
		for (int i = 0; i < vals.Length; i++)
		{
		  longVals[i] = NumericUtils.doubleToSortableLong(vals[i]);
		  Assert.IsTrue("forward and back conversion should generate same double", vals[i].CompareTo(NumericUtils.sortableLongToDouble(longVals[i])) == 0);
		}

		// check sort order (prefixVals should be ascending)
		for (int i = 1; i < longVals.Length; i++)
		{
		  Assert.IsTrue("check sort order", longVals[i - 1] < longVals[i]);
		}
	  }

	  public static readonly double[] DOUBLE_NANs = new double[] {double.NaN, double.longBitsToDouble(0x7ff0000000000001L), double.longBitsToDouble(0x7fffffffffffffffL), double.longBitsToDouble(0xfff0000000000001L), double.longBitsToDouble(0xffffffffffffffffL)};

	  public virtual void TestSortableDoubleNaN()
	  {
		long plusInf = NumericUtils.doubleToSortableLong(double.PositiveInfinity);
		foreach (double nan in DOUBLE_NANs)
		{
		  Assert.IsTrue(double.IsNaN(nan));
		  long sortable = NumericUtils.doubleToSortableLong(nan);
		  Assert.IsTrue("Double not sorted correctly: " + nan + ", long repr: " + sortable + ", positive inf.: " + plusInf, sortable > plusInf);
		}
	  }

	  public virtual void TestFloats()
	  {
		float[] vals = new float[]{float.NegativeInfinity, -2.3E25f, -1.0E15f, -1.0f, -1.0E-1f, -1.0E-2f, -0.0f, +0.0f, 1.0E-2f, 1.0E-1f, 1.0f, 1.0E15f, 2.3E25f, float.PositiveInfinity, float.NaN};
		int[] intVals = new int[vals.Length];

		// check forward and back conversion
		for (int i = 0; i < vals.Length; i++)
		{
		  intVals[i] = NumericUtils.floatToSortableInt(vals[i]);
		  Assert.IsTrue("forward and back conversion should generate same double", vals[i].CompareTo(NumericUtils.sortableIntToFloat(intVals[i])) == 0);
		}

		// check sort order (prefixVals should be ascending)
		for (int i = 1; i < intVals.Length; i++)
		{
		  Assert.IsTrue("check sort order", intVals[i - 1] < intVals[i]);
		}
	  }

	  public static readonly float[] FLOAT_NANs = new float[] {float.NaN, float.intBitsToFloat(0x7f800001), float.intBitsToFloat(0x7fffffff), float.intBitsToFloat(0xff800001), float.intBitsToFloat(0xffffffff)};

	  public virtual void TestSortableFloatNaN()
	  {
		int plusInf = NumericUtils.floatToSortableInt(float.PositiveInfinity);
		foreach (float nan in FLOAT_NANs)
		{
		  Assert.IsTrue(float.IsNaN(nan));
		  int sortable = NumericUtils.floatToSortableInt(nan);
		  Assert.IsTrue("Float not sorted correctly: " + nan + ", int repr: " + sortable + ", positive inf.: " + plusInf, sortable > plusInf);
		}
	  }

	  // INFO: Tests for trieCodeLong()/trieCodeInt() not needed because implicitely tested by range filter tests

	  /// <summary>
	  /// Note: The neededBounds Iterable must be unsigned (easier understanding what's happening) </summary>
	  private void AssertLongRangeSplit(long lower, long upper, int precisionStep, bool useBitSet, IEnumerable<long?> expectedBounds, IEnumerable<int?> expectedShifts)
	  {
		// Cannot use FixedBitSet since the range could be long:
		LongBitSet bits = useBitSet ? new LongBitSet(upper - lower + 1) : null;
		IEnumerator<long?> neededBounds = (expectedBounds == null) ? null : expectedBounds.GetEnumerator();
		IEnumerator<int?> neededShifts = (expectedShifts == null) ? null : expectedShifts.GetEnumerator();

		NumericUtils.splitLongRange(new LongRangeBuilderAnonymousInnerClassHelper(this, lower, upper, useBitSet, bits, neededBounds, neededShifts), precisionStep, lower, upper);

		if (useBitSet)
		{
		  // after flipping all bits in the range, the cardinality should be zero
		  bits.flip(0,upper - lower + 1);
		  Assert.AreEqual("The sub-range concenated should match the whole range", 0, bits.cardinality());
		}
	  }

	  private class LongRangeBuilderAnonymousInnerClassHelper : NumericUtils.LongRangeBuilder
	  {
		  private readonly TestNumericUtils OuterInstance;

		  private long Lower;
		  private long Upper;
		  private bool UseBitSet;
		  private LongBitSet Bits;
//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
//ORIGINAL LINE: private IEnumerator<long?> neededBounds;
		  private IEnumerator<long?> NeededBounds;
//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
//ORIGINAL LINE: private IEnumerator<int?> neededShifts;
		  private IEnumerator<int?> NeededShifts;

		  public LongRangeBuilderAnonymousInnerClassHelper<T1, T2>(TestNumericUtils outerInstance, long lower, long upper, bool useBitSet, LongBitSet bits, IEnumerator<T1> neededBounds, IEnumerator<T2> neededShifts)
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
			Assert.IsTrue("min, max should be inside bounds", min >= Lower && min <= Upper && max >= Lower && max <= Upper);
			if (UseBitSet)
			{
				for (long l = min; l <= max; l++)
				{
			  Assert.IsFalse("ranges should not overlap", Bits.getAndSet(l - Lower));
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
			min ^= 0x8000000000000000L;
			max ^= 0x8000000000000000L;
			//System.out.println("0x"+Long.toHexString(min>>>shift)+"L,0x"+Long.toHexString(max>>>shift)+"L)/*shift="+shift+"*/,");
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
			Assert.AreEqual("shift", (int)NeededShifts.next(), shift);
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
			Assert.AreEqual("inner min bound", (long)NeededBounds.next(), (long)((ulong)min >> shift));
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
			Assert.AreEqual("inner max bound", (long)NeededBounds.next(), (long)((ulong)max >> shift));
		  }
	  }

	  /// <summary>
	  /// LUCENE-2541: NumericRangeQuery errors with endpoints near long min and max values </summary>
	  public virtual void TestLongExtremeValues()
	  {
		// upper end extremes
		AssertLongRangeSplit(long.MaxValue, long.MaxValue, 1, true, Arrays.asList(0xffffffffffffffffL,0xffffffffffffffffL), Arrays.asList(0));
		AssertLongRangeSplit(long.MaxValue, long.MaxValue, 2, true, Arrays.asList(0xffffffffffffffffL,0xffffffffffffffffL), Arrays.asList(0));
		AssertLongRangeSplit(long.MaxValue, long.MaxValue, 4, true, Arrays.asList(0xffffffffffffffffL,0xffffffffffffffffL), Arrays.asList(0));
		AssertLongRangeSplit(long.MaxValue, long.MaxValue, 6, true, Arrays.asList(0xffffffffffffffffL,0xffffffffffffffffL), Arrays.asList(0));
		AssertLongRangeSplit(long.MaxValue, long.MaxValue, 8, true, Arrays.asList(0xffffffffffffffffL,0xffffffffffffffffL), Arrays.asList(0));
		AssertLongRangeSplit(long.MaxValue, long.MaxValue, 64, true, Arrays.asList(0xffffffffffffffffL,0xffffffffffffffffL), Arrays.asList(0));

		AssertLongRangeSplit(long.MaxValue-0xfL, long.MaxValue, 4, true, Arrays.asList(0xfffffffffffffffL,0xfffffffffffffffL), Arrays.asList(4));
		AssertLongRangeSplit(long.MaxValue-0x10L, long.MaxValue, 4, true, Arrays.asList(0xffffffffffffffefL,0xffffffffffffffefL, 0xfffffffffffffffL,0xfffffffffffffffL), Arrays.asList(0, 4));

		// lower end extremes
		AssertLongRangeSplit(long.MinValue, long.MinValue, 1, true, Arrays.asList(0x0000000000000000L,0x0000000000000000L), Arrays.asList(0));
		AssertLongRangeSplit(long.MinValue, long.MinValue, 2, true, Arrays.asList(0x0000000000000000L,0x0000000000000000L), Arrays.asList(0));
		AssertLongRangeSplit(long.MinValue, long.MinValue, 4, true, Arrays.asList(0x0000000000000000L,0x0000000000000000L), Arrays.asList(0));
		AssertLongRangeSplit(long.MinValue, long.MinValue, 6, true, Arrays.asList(0x0000000000000000L,0x0000000000000000L), Arrays.asList(0));
		AssertLongRangeSplit(long.MinValue, long.MinValue, 8, true, Arrays.asList(0x0000000000000000L,0x0000000000000000L), Arrays.asList(0));
		AssertLongRangeSplit(long.MinValue, long.MinValue, 64, true, Arrays.asList(0x0000000000000000L,0x0000000000000000L), Arrays.asList(0));

		AssertLongRangeSplit(long.MinValue, long.MinValue+0xfL, 4, true, Arrays.asList(0x000000000000000L,0x000000000000000L), Arrays.asList(4));
		AssertLongRangeSplit(long.MinValue, long.MinValue+0x10L, 4, true, Arrays.asList(0x0000000000000010L,0x0000000000000010L, 0x000000000000000L,0x000000000000000L), Arrays.asList(0, 4));
	  }

	  public virtual void TestRandomSplit()
	  {
		long num = (long) atLeast(10);
		for (long i = 0; i < num; i++)
		{
		  ExecuteOneRandomSplit(random());
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
			val = random.nextLong();
		break;
		}

		val += random.Next(5) - 2;

		if (random.nextBoolean())
		{
		  if (random.nextBoolean())
		  {
			  val += random.Next(100) - 50;
		  }
		  if (random.nextBoolean())
		  {
			  val = ~val;
		  }
		  if (random.nextBoolean())
		  {
			  val = val << 1;
		  }
		  if (random.nextBoolean())
		  {
			  val = (long)((ulong)val >> 1);
		  }
		}

		return val;
	  }

	  public virtual void TestSplitLongRange()
	  {
		// a hard-coded "standard" range
		AssertLongRangeSplit(-5000L, 9500L, 4, true, Arrays.asList(0x7fffffffffffec78L,0x7fffffffffffec7fL, 0x8000000000002510L,0x800000000000251cL, 0x7fffffffffffec8L, 0x7fffffffffffecfL, 0x800000000000250L, 0x800000000000250L, 0x7fffffffffffedL, 0x7fffffffffffefL, 0x80000000000020L, 0x80000000000024L, 0x7ffffffffffffL, 0x8000000000001L), Arrays.asList(0, 0, 4, 4, 8, 8, 12));

		// the same with no range splitting
		AssertLongRangeSplit(-5000L, 9500L, 64, true, Arrays.asList(0x7fffffffffffec78L,0x800000000000251cL), Arrays.asList(0));

		// this tests optimized range splitting, if one of the inner bounds
		// is also the bound of the next lower precision, it should be used completely
		AssertLongRangeSplit(0L, 1024L + 63L, 4, true, Arrays.asList(0x800000000000040L, 0x800000000000043L, 0x80000000000000L, 0x80000000000003L), Arrays.asList(4, 8));

		// the full long range should only consist of a lowest precision range; no bitset testing here, as too much memory needed :-)
		AssertLongRangeSplit(long.MinValue, long.MaxValue, 8, false, Arrays.asList(0x00L,0xffL), Arrays.asList(56));

		// the same with precisionStep=4
		AssertLongRangeSplit(long.MinValue, long.MaxValue, 4, false, Arrays.asList(0x0L,0xfL), Arrays.asList(60));

		// the same with precisionStep=2
		AssertLongRangeSplit(long.MinValue, long.MaxValue, 2, false, Arrays.asList(0x0L,0x3L), Arrays.asList(62));

		// the same with precisionStep=1
		AssertLongRangeSplit(long.MinValue, long.MaxValue, 1, false, Arrays.asList(0x0L,0x1L), Arrays.asList(63));

		// a inverse range should produce no sub-ranges
		AssertLongRangeSplit(9500L, -5000L, 4, false, Collections.emptyList<long?>(), Collections.emptyList<int?>());

		// a 0-length range should reproduce the range itself
		AssertLongRangeSplit(9500L, 9500L, 4, false, Arrays.asList(0x800000000000251cL,0x800000000000251cL), Arrays.asList(0));
	  }

	  /// <summary>
	  /// Note: The neededBounds Iterable must be unsigned (easier understanding what's happening) </summary>
	  private void AssertIntRangeSplit(int lower, int upper, int precisionStep, bool useBitSet, IEnumerable<int?> expectedBounds, IEnumerable<int?> expectedShifts)
	  {
		FixedBitSet bits = useBitSet ? new FixedBitSet(upper - lower + 1) : null;
		IEnumerator<int?> neededBounds = (expectedBounds == null) ? null : expectedBounds.GetEnumerator();
		IEnumerator<int?> neededShifts = (expectedShifts == null) ? null : expectedShifts.GetEnumerator();

		NumericUtils.splitIntRange(new IntRangeBuilderAnonymousInnerClassHelper(this, lower, upper, useBitSet, bits, neededBounds, neededShifts), precisionStep, lower, upper);

		if (useBitSet)
		{
		  // after flipping all bits in the range, the cardinality should be zero
		  bits.flip(0, upper - lower + 1);
		  Assert.AreEqual("The sub-range concenated should match the whole range", 0, bits.cardinality());
		}
	  }

	  private class IntRangeBuilderAnonymousInnerClassHelper : NumericUtils.IntRangeBuilder
	  {
		  private readonly TestNumericUtils OuterInstance;

		  private int Lower;
		  private int Upper;
		  private bool UseBitSet;
		  private FixedBitSet Bits;
//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
//ORIGINAL LINE: private IEnumerator<int?> neededBounds;
		  private IEnumerator<int?> NeededBounds;
//JAVA TO C# CONVERTER TODO TASK: Java wildcard generics are not converted to .NET:
//ORIGINAL LINE: private IEnumerator<int?> neededShifts;
		  private IEnumerator<int?> NeededShifts;

		  public IntRangeBuilderAnonymousInnerClassHelper<T1, T2>(TestNumericUtils outerInstance, int lower, int upper, bool useBitSet, FixedBitSet bits, IEnumerator<T1> neededBounds, IEnumerator<T2> neededShifts)
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
			Assert.IsTrue("min, max should be inside bounds", min >= Lower && min <= Upper && max >= Lower && max <= Upper);
			if (UseBitSet)
			{
				for (int i = min; i <= max; i++)
				{
			  Assert.IsFalse("ranges should not overlap", Bits.getAndSet(i - Lower));
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
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
			Assert.AreEqual("shift", (int)NeededShifts.next(), shift);
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
			Assert.AreEqual("inner min bound", (int)NeededBounds.next(), (int)((uint)min >> shift));
//JAVA TO C# CONVERTER TODO TASK: Java iterators are only converted within the context of 'while' and 'for' loops:
			Assert.AreEqual("inner max bound", (int)NeededBounds.next(), (int)((uint)max >> shift));
		  }
	  }

	  public virtual void TestSplitIntRange()
	  {
		// a hard-coded "standard" range
		AssertIntRangeSplit(-5000, 9500, 4, true, Arrays.asList(0x7fffec78,0x7fffec7f, 0x80002510,0x8000251c, 0x7fffec8, 0x7fffecf, 0x8000250, 0x8000250, 0x7fffed, 0x7fffef, 0x800020, 0x800024, 0x7ffff, 0x80001), Arrays.asList(0, 0, 4, 4, 8, 8, 12));

		// the same with no range splitting
		AssertIntRangeSplit(-5000, 9500, 32, true, Arrays.asList(0x7fffec78,0x8000251c), Arrays.asList(0));

		// this tests optimized range splitting, if one of the inner bounds
		// is also the bound of the next lower precision, it should be used completely
		AssertIntRangeSplit(0, 1024 + 63, 4, true, Arrays.asList(0x8000040, 0x8000043, 0x800000, 0x800003), Arrays.asList(4, 8));

		// the full int range should only consist of a lowest precision range; no bitset testing here, as too much memory needed :-)
		AssertIntRangeSplit(int.MinValue, int.MaxValue, 8, false, Arrays.asList(0x00,0xff), Arrays.asList(24));

		// the same with precisionStep=4
		AssertIntRangeSplit(int.MinValue, int.MaxValue, 4, false, Arrays.asList(0x0,0xf), Arrays.asList(28));

		// the same with precisionStep=2
		AssertIntRangeSplit(int.MinValue, int.MaxValue, 2, false, Arrays.asList(0x0,0x3), Arrays.asList(30));

		// the same with precisionStep=1
		AssertIntRangeSplit(int.MinValue, int.MaxValue, 1, false, Arrays.asList(0x0,0x1), Arrays.asList(31));

		// a inverse range should produce no sub-ranges
		AssertIntRangeSplit(9500, -5000, 4, false, Collections.emptyList<int?>(), Collections.emptyList<int?>());

		// a 0-length range should reproduce the range itself
		AssertIntRangeSplit(9500, 9500, 4, false, Arrays.asList(0x8000251c,0x8000251c), Arrays.asList(0));
	  }

	}

}