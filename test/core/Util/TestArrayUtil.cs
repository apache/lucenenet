using System;

namespace Lucene.Net.Util
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


	public class TestArrayUtil : LuceneTestCase
	{

	  // Ensure ArrayUtil.getNextSize gives linear amortized cost of realloc/copy
	  public virtual void TestGrowth()
	  {
		int currentSize = 0;
		long copyCost = 0;

		// Make sure ArrayUtil hits Integer.MAX_VALUE, if we insist:
		while (currentSize != int.MaxValue)
		{
		  int nextSize = ArrayUtil.oversize(1 + currentSize, RamUsageEstimator.NUM_BYTES_OBJECT_REF);
		  Assert.IsTrue(nextSize > currentSize);
		  if (currentSize > 0)
		  {
			copyCost += currentSize;
			double copyCostPerElement = ((double) copyCost) / currentSize;
			Assert.IsTrue("cost " + copyCostPerElement, copyCostPerElement < 10.0);
		  }
		  currentSize = nextSize;
		}
	  }

	  public virtual void TestMaxSize()
	  {
		// intentionally pass invalid elemSizes:
		for (int elemSize = 0;elemSize < 10;elemSize++)
		{
		  Assert.AreEqual(int.MaxValue, ArrayUtil.oversize(int.MaxValue, elemSize));
		  Assert.AreEqual(int.MaxValue, ArrayUtil.oversize(int.MaxValue-1, elemSize));
		}
	  }

	  public virtual void TestInvalidElementSizes()
	  {
		Random rnd = random();
		int num = atLeast(10000);
		for (int iter = 0; iter < num; iter++)
		{
		  int minTargetSize = rnd.Next(int.MaxValue);
		  int elemSize = rnd.Next(11);
		  int v = ArrayUtil.oversize(minTargetSize, elemSize);
		  Assert.IsTrue(v >= minTargetSize);
		}
	  }

	  public virtual void TestParseInt()
	  {
		int test;
		try
		{
		  test = ArrayUtil.parseInt("".ToCharArray());
		  Assert.IsTrue(false);
		}
		catch (NumberFormatException e)
		{
		  //expected
		}
		try
		{
		  test = ArrayUtil.parseInt("foo".ToCharArray());
		  Assert.IsTrue(false);
		}
		catch (NumberFormatException e)
		{
		  //expected
		}
		try
		{
		  test = ArrayUtil.parseInt(Convert.ToString(long.MaxValue).ToCharArray());
		  Assert.IsTrue(false);
		}
		catch (NumberFormatException e)
		{
		  //expected
		}
		try
		{
		  test = ArrayUtil.parseInt("0.34".ToCharArray());
		  Assert.IsTrue(false);
		}
		catch (NumberFormatException e)
		{
		  //expected
		}

		try
		{
		  test = ArrayUtil.parseInt("1".ToCharArray());
		  Assert.IsTrue(test + " does not equal: " + 1, test == 1);
		  test = ArrayUtil.parseInt("-10000".ToCharArray());
		  Assert.IsTrue(test + " does not equal: " + -10000, test == -10000);
		  test = ArrayUtil.parseInt("1923".ToCharArray());
		  Assert.IsTrue(test + " does not equal: " + 1923, test == 1923);
		  test = ArrayUtil.parseInt("-1".ToCharArray());
		  Assert.IsTrue(test + " does not equal: " + -1, test == -1);
		  test = ArrayUtil.parseInt("foo 1923 bar".ToCharArray(), 4, 4);
		  Assert.IsTrue(test + " does not equal: " + 1923, test == 1923);
		}
		catch (NumberFormatException e)
		{
		  Console.WriteLine(e.ToString());
		  Console.Write(e.StackTrace);
		  Assert.IsTrue(false);
		}

	  }

	  public virtual void TestSliceEquals()
	  {
		string left = "this is equal";
		string right = left;
		char[] leftChars = left.ToCharArray();
		char[] rightChars = right.ToCharArray();
		Assert.IsTrue(left + " does not equal: " + right, ArrayUtil.Equals(leftChars, 0, rightChars, 0, left.Length));

		Assert.IsFalse(left + " does not equal: " + right, ArrayUtil.Equals(leftChars, 1, rightChars, 0, left.Length));
		Assert.IsFalse(left + " does not equal: " + right, ArrayUtil.Equals(leftChars, 1, rightChars, 2, left.Length));

		Assert.IsFalse(left + " does not equal: " + right, ArrayUtil.Equals(leftChars, 25, rightChars, 0, left.Length));
		Assert.IsFalse(left + " does not equal: " + right, ArrayUtil.Equals(leftChars, 12, rightChars, 0, left.Length));
	  }

	  private int?[] CreateRandomArray(int maxSize)
	  {
		Random rnd = random();
		int?[] a = new int?[rnd.Next(maxSize) + 1];
		for (int i = 0; i < a.Length; i++)
		{
		  a[i] = Convert.ToInt32(rnd.Next(a.Length));
		}
		return a;
	  }

	  public virtual void TestIntroSort()
	  {
		int num = atLeast(50);
		for (int i = 0; i < num; i++)
		{
		  int?[] a1 = CreateRandomArray(2000), a2 = a1.clone();
		  ArrayUtil.IntroSort(a1);
		  Arrays.sort(a2);
		  assertArrayEquals(a2, a1);

		  a1 = CreateRandomArray(2000);
		  a2 = a1.clone();
		  ArrayUtil.IntroSort(a1, Collections.reverseOrder());
		  Arrays.sort(a2, Collections.reverseOrder());
		  assertArrayEquals(a2, a1);
		  // reverse back, so we can test that completely backwards sorted array (worst case) is working:
		  ArrayUtil.IntroSort(a1);
		  Arrays.sort(a2);
		  assertArrayEquals(a2, a1);
		}
	  }

	  private int?[] CreateSparseRandomArray(int maxSize)
	  {
		Random rnd = random();
		int?[] a = new int?[rnd.Next(maxSize) + 1];
		for (int i = 0; i < a.Length; i++)
		{
		  a[i] = Convert.ToInt32(rnd.Next(2));
		}
		return a;
	  }

	  // this is a test for LUCENE-3054 (which fails without the merge sort fall back with stack overflow in most cases)
	  public virtual void TestQuickToHeapSortFallback()
	  {
		int num = atLeast(50);
		for (int i = 0; i < num; i++)
		{
		  int?[] a1 = CreateSparseRandomArray(40000), a2 = a1.clone();
		  ArrayUtil.IntroSort(a1);
		  Arrays.sort(a2);
		  assertArrayEquals(a2, a1);
		}
	  }

	  public virtual void TestTimSort()
	  {
		int num = atLeast(50);
		for (int i = 0; i < num; i++)
		{
		  int?[] a1 = CreateRandomArray(2000), a2 = a1.clone();
		  ArrayUtil.timSort(a1);
		  Arrays.sort(a2);
		  assertArrayEquals(a2, a1);

		  a1 = CreateRandomArray(2000);
		  a2 = a1.clone();
		  ArrayUtil.timSort(a1, Collections.reverseOrder());
		  Arrays.sort(a2, Collections.reverseOrder());
		  assertArrayEquals(a2, a1);
		  // reverse back, so we can test that completely backwards sorted array (worst case) is working:
		  ArrayUtil.timSort(a1);
		  Arrays.sort(a2);
		  assertArrayEquals(a2, a1);
		}
	  }

	  internal class Item : IComparable<Item>
	  {
		internal readonly int Val, Order;

		internal Item(int val, int order)
		{
		  this.Val = val;
		  this.Order = order;
		}

		public virtual int CompareTo(Item other)
		{
		  return this.Order - other.Order;
		}

		public override string ToString()
		{
		  return Convert.ToString(Val);
		}
	  }

	  public virtual void TestMergeSortStability()
	  {
		Random rnd = random();
		Item[] items = new Item[100];
		for (int i = 0; i < items.Length; i++)
		{
		  // half of the items have value but same order. The value of this items is sorted,
		  // so they should always be in order after sorting.
		  // The other half has defined order, but no (-1) value (they should appear after
		  // all above, when sorted).
		  bool equal = rnd.nextBoolean();
		  items[i] = new Item(equal ? (i + 1) : -1, equal ? 0 : (rnd.Next(1000) + 1));
		}

		if (VERBOSE)
		{
			Console.WriteLine("Before: " + Arrays.ToString(items));
		}
		// if you replace this with ArrayUtil.quickSort(), test should fail:
		ArrayUtil.timSort(items);
		if (VERBOSE)
		{
			Console.WriteLine("Sorted: " + Arrays.ToString(items));
		}

		Item last = items[0];
		for (int i = 1; i < items.Length; i++)
		{
		  Item act = items[i];
		  if (act.Order == 0)
		  {
			// order of "equal" items should be not mixed up
			Assert.IsTrue(act.Val > last.Val);
		  }
		  Assert.IsTrue(act.Order >= last.Order);
		  last = act;
		}
	  }

	  public virtual void TestTimSortStability()
	  {
		Random rnd = random();
		Item[] items = new Item[100];
		for (int i = 0; i < items.Length; i++)
		{
		  // half of the items have value but same order. The value of this items is sorted,
		  // so they should always be in order after sorting.
		  // The other half has defined order, but no (-1) value (they should appear after
		  // all above, when sorted).
		  bool equal = rnd.nextBoolean();
		  items[i] = new Item(equal ? (i + 1) : -1, equal ? 0 : (rnd.Next(1000) + 1));
		}

		if (VERBOSE)
		{
			Console.WriteLine("Before: " + Arrays.ToString(items));
		}
		// if you replace this with ArrayUtil.quickSort(), test should fail:
		ArrayUtil.timSort(items);
		if (VERBOSE)
		{
			Console.WriteLine("Sorted: " + Arrays.ToString(items));
		}

		Item last = items[0];
		for (int i = 1; i < items.Length; i++)
		{
		  Item act = items[i];
		  if (act.Order == 0)
		  {
			// order of "equal" items should be not mixed up
			Assert.IsTrue(act.Val > last.Val);
		  }
		  Assert.IsTrue(act.Order >= last.Order);
		  last = act;
		}
	  }

	  // should produce no exceptions
	  public virtual void TestEmptyArraySort()
	  {
		int?[] a = new int?[0];
		ArrayUtil.IntroSort(a);
		ArrayUtil.timSort(a);
		ArrayUtil.IntroSort(a, Collections.reverseOrder());
		ArrayUtil.timSort(a, Collections.reverseOrder());
	  }

	}

}