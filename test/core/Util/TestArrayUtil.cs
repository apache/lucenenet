using System;
using Lucene.Net.Support;
using Lucene.Net.Test.Support;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Test.Util
{
    [TestFixture]
    public class TestArrayUtil : LuceneTestCase
    {
        // Ensure ArrayUtil.getNextSize gives linear amortized cost of realloc/copy
        [Test]
        public void TestGrowth()
        {
            int currentSize = 0;
            long copyCost = 0;

            // Make sure ArrayUtil hits int.MaxValue, if we insist:
            while (currentSize != int.MaxValue)
            {
                int nextSize = ArrayUtil.Oversize(1 + currentSize, RamUsageEstimator.NUM_BYTES_OBJECT_REF);
                Assert.IsTrue(nextSize > currentSize);
                if (currentSize > 0)
                {
                    copyCost += currentSize;
                    var copyCostPerElement = ((double)copyCost) / currentSize;
                    Assert.IsTrue("cost " + copyCostPerElement, copyCostPerElement < 10.0);
                }
                currentSize = nextSize;
            }
        }

        [Test]
        public void TextMaxSize()
        {
            // Intentionally pass invalid elemSizes:
            for (var elemSize = 0; elemSize < 10; elemSize++)
            {
                assertEquals(int.MaxValue, ArrayUtil.Oversize(int.MaxValue, elemSize));
                assertEquals(int.MaxValue, ArrayUtil.Oversize(int.MaxValue - 1, elemSize));
            }
        }

        [Test]
        public void TestInvalidElementSizes()
        {
            var rnd = new Random();
            int num = AtLeast(10000);
            for (var iter = 0; iter < num; iter++)
            {
                var minTargetSize = rnd.Next(int.MaxValue);
                var elemSize = rnd.Next(11);
                var v = ArrayUtil.Oversize(minTargetSize, elemSize);
                Assert.IsTrue(v >= minTargetSize);
            }
        }

        [Test]
        public void TestParseInt()
        {
            int test;
            try
            {
                test = ArrayUtil.ParseInt("".ToCharArray());
                Assert.IsTrue(false);
            }
            catch (FormatException e)
            {
                //expected
            }
            try
            {
                test = ArrayUtil.ParseInt("foo".ToCharArray());
                Assert.IsTrue(false);
            }
            catch (FormatException e)
            {
                //expected
            }
            try
            {
                test = ArrayUtil.ParseInt(long.MaxValue.ToString().ToCharArray());
                Assert.IsTrue(false);
            }
            catch (FormatException e)
            {
                //expected
            }
            try
            {
                test = ArrayUtil.ParseInt("0.34".ToCharArray());
                Assert.IsTrue(false);
            }
            catch (FormatException e)
            {
                //expected
            }

            try
            {
                test = ArrayUtil.ParseInt("1".ToCharArray());
                Assert.IsTrue(test == 1, test + " does not equal: " + 1);
                test = ArrayUtil.ParseInt("-10000".ToCharArray());
                Assert.IsTrue(test == -10000, test + " does not equal: " + -10000);
                test = ArrayUtil.ParseInt("1923".ToCharArray());
                Assert.IsTrue(test == 1923, test + " does not equal: " + 1923);
                test = ArrayUtil.ParseInt("-1".ToCharArray());
                Assert.IsTrue(test == -1, test + " does not equal: " + -1);
                test = ArrayUtil.ParseInt("foo 1923 bar".ToCharArray(), 4, 4);
                Assert.IsTrue(test == 1923, test + " does not equal: " + 1923);
            }
            catch (FormatException e)
            {
                Console.WriteLine(e.StackTrace);
                Assert.IsTrue(false);
            }
        }

        [Test]
        public void TestSliceEquals()
        {
            var left = "this is equal";
            var right = left;
            var leftChars = left.ToCharArray();
            var rightChars = right.ToCharArray();
            Assert.IsTrue(ArrayUtil.Equals(leftChars, 0, rightChars, 0, left.Length), left + " does not equal: " + right);

            Assert.IsFalse(ArrayUtil.Equals(leftChars, 1, rightChars, 0, left.Length), left + " does not equal: " + right);
            Assert.IsFalse(ArrayUtil.Equals(leftChars, 1, rightChars, 2, left.Length), left + " does not equal: " + right);

            Assert.IsFalse(ArrayUtil.Equals(leftChars, 25, rightChars, 0, left.Length), left + " does not equal: " + right);
            Assert.IsFalse(ArrayUtil.Equals(leftChars, 12, rightChars, 0, left.Length), left + " does not equal: " + right);
        }

        private int[] CreateRandomArray(int maxSize)
        {
            var rnd = new Random();
            var a = new int[rnd.Next(maxSize) + 1];
            for (var i = 0; i < a.Length; i++)
            {
                a[i] = rnd.Next(a.Length);
            }
            return a;
        }

        [Test]
        public virtual void TestQuickSort()
        {
            int num = AtLeast(50);
            for (var i = 0; i < num; i++)
            {
                int[] a1 = CreateRandomArray(2000), a2 = (int[])a1.Clone();
                ArrayUtil.QuickSort(a1);
                Array.Sort(a2);
                Assert.IsTrue(a2.Equals(a1));

                a1 = CreateRandomArray(2000);
                a2 = (int[])a1.Clone();
                ArrayUtil.QuickSort(a1, Collections.ReverseOrder());
                Array.Sort(a2, Collections.ReverseOrder());
                Assert.Equals(a1, a2);
                // reverse back, so we can test that completely backwards sorted array (worst case) is working:
                ArrayUtil.QuickSort(a1);
                Array.Sort(a2);
                Assert.Equals(a2, a1);
            }
        }

        private int[] CreateSparseRandomArray(int maxSize)
        {
            var rnd = new Random();
            var a = new int[rnd.Next(maxSize) + 1];
            for (var i = 0; i < a.Length; i++)
            {
                a[i] = rnd.Next(2);
            }
            return a;
        }

        [Test]
        public virtual void TestQuickToMergeSortFallback()
        {
            int num = AtLeast(50);
            for (var i = 0; i < num; i++)
            {
                int[] a1 = CreateSparseRandomArray(40000), a2 = (int[])a1.Clone();
                ArrayUtil.QuickSort(a1);
                Array.Sort(a2);
                Assert.Equals(a2, a1);
            }
        }

        [Test]
        public virtual void TestMergeSort()
        {
            int num = AtLeast(50);
            for (var i = 0; i < num; i++)
            {
                int[] a1 = CreateRandomArray(2000), a2 = (int[])a1.Clone();
                ArrayUtil.MergeSort(a1);
                Array.Sort(a2);
                Assert.Equals(a2, a1);

                a1 = CreateRandomArray(2000);
                a2 = (int[])a1.Clone();
                ArrayUtil.MergeSort(a1, Collections.ReverseOrder());
                Array.Sort(a2, Collections.ReverseOrder());
                Assert.Equals(a2, a1);
                // reverse back, so we can test that completely backwards sorted array (worst case) is working:
                ArrayUtil.MergeSort(a1);
                Array.Sort(a2);
                Assert.Equals(a2, a1);
            }
        }

        [Test]
        public virtual void TestTimSort()
        {
            int num = AtLeast(65);
            for (var i = 0; i < num; i++)
            {
                int[] a1 = CreateRandomArray(2000), a2 = (int[])a1.Clone();
                ArrayUtil.TimSort(a1);
                Array.Sort(a2);
                Assert.Equals(a2, a1);

                a1 = CreateRandomArray(2000);
                a2 = (int[])a1.Clone();
                ArrayUtil.TimSort(a1, Collections.ReverseOrder());
                Array.Sort(a2, Collections.ReverseOrder());
                Assert.Equals(a2, a1);
                // reverse back, so we can test that completely backwards sorted array (worst case) is working:
                ArrayUtil.TimSort(a1);
                Array.Sort(a2);
                Assert.Equals(a2, a1);
            }
        }

        [Test]
        public virtual void TestInsertionSort()
        {
            for (int i = 0, c = AtLeast(500); i < c; i++)
            {
                int[] a1 = CreateRandomArray(30), a2 = (int[])a1.Clone();
                ArrayUtil.InsertionSort(a1);
                Array.Sort(a2);
                Assert.Equals(a2, a1);

                a1 = CreateRandomArray(30);
                a2 = (int[])a1.Clone();
                ArrayUtil.InsertionSort(a1, Collections.ReverseOrder());
                Array.Sort(a2, Collections.ReverseOrder());
                Assert.Equals(a2, a1);
                // reverse back, so we can test that completely backwards sorted array (worst case) is working:
                ArrayUtil.InsertionSort(a1);
                Array.Sort(a2);
                Assert.Equals(a2, a1);
            }
        }

        [Test]
        public virtual void TestBinarySort()
        {
            for (int i = 0, c = AtLeast(500); i < c; i++)
            {
                int[] a1 = CreateRandomArray(30), a2 = (int[])a1.Clone();
                ArrayUtil.BinarySort(a1);
                Array.Sort(a2);
                Assert.Equals(a2, a1);

                a1 = CreateRandomArray(30);
                a2 = (int[])a1.Clone();
                ArrayUtil.BinarySort(a1, Collections.ReverseOrder());
                Array.Sort(a2, Collections.ReverseOrder());
                Assert.Equals(a2, a1);
                // reverse back, so we can test that completely backwards sorted array (worst case) is working:
                ArrayUtil.BinarySort(a1);
                Array.Sort(a2);
                Assert.Equals(a2, a1);
            }
        }

        internal class Item : IComparable<Item>
        {
            int val, order;

            internal Item(int val, int order)
            {
                this.val = val;
                this.order = order;
            }

            public int CompareTo(Item other)
            {
                return this.order - other.order;
            }

            public override string ToString()
            {
                return val.ToString();
            }
        }

        [Test]
        public virtual void TestMergeSortStability()
        {
            var rnd = new Random();
            var items = new Item[100];
            for (var i = 0; i < items.Length; i++)
            {
                // half of the items have value but same order. The value of this items is sorted,
                // so they should always be in order after sorting.
                // The other half has defined order, but no (-1) value (they should appear after
                // all above, when sorted).
                var equal = rnd.NextBool();
                items[i] = new Item(equal ? (i + 1) : -1, equal ? 0 : (rnd.Next(1000) + 1));
            }

            if (VERBOSE) Console.WriteLine("Before: " + Arrays.ToString(items));
            // if you replace this with ArrayUtil.quickSort(), test should fail:
            ArrayUtil.MergeSort(items);
            if (VERBOSE) Console.WriteLine("Sorted: " + Arrays.ToString(items));

            var last = items[0];
            for (var i = 1; i < items.Length; i++)
            {
                var act = items[i];
                if (act.Order == 0)
                {
                    // order of "equal" items should be not mixed up
                    Assert.IsTrue(act.Val > last.Val);
                }
                Assert.IsTrue(act.Order >= last.Order);
                last = act;
            }
        }

        [Test]
        public virtual void TestTimSortStability()
        {
            var rnd = new Random();
            var items = new Item[100];
            for (var i = 0; i < items.Length; i++)
            {
                // half of the items have value but same order. The value of this items is sorted,
                // so they should always be in order after sorting.
                // The other half has defined order, but no (-1) value (they should appear after
                // all above, when sorted).
                var equal = rnd.NextBool();
                items[i] = new Item(equal ? (i + 1) : -1, equal ? 0 : (rnd.Next(1000) + 1));
            }

            if (VERBOSE) Console.WriteLine("Before: " + Arrays.ToString(items));
            // if you replace this with ArrayUtil.quickSort(), test should fail:
            ArrayUtil.TimSort(items);
            if (VERBOSE) Console.WriteLine("Sorted: " + Arrays.ToString(items));

            var last = items[0];
            for (var i = 1; i < items.Length; i++)
            {
                var act = items[i];
                if (act.Order == 0)
                {
                    // order of "equal" items should be not mixed up
                    Assert.IsTrue(act.Val > last.Val);
                }
                Assert.IsTrue(act.Order >= last.Order);
                last = act;
            }
        }

        // Should produce no excepetions
        [Test]
        public virtual void TestEmptyArraySort()
        {
            var a = new int[0];
            ArrayUtil.QuickSort(a);
            ArrayUtil.MergeSort(a);
            ArrayUtil.InsertionSort(a);
            ArrayUtil.BinarySort(a);
            ArrayUtil.TimSort(a);
            ArrayUtil.QuickSort(a, Collections.ReverseOrder());
            ArrayUtil.MergeSort(a, Collections.ReverseOrder());
            ArrayUtil.TimSort(a, Collections.ReverseOrder());
            ArrayUtil.InsertionSort(a, Collections.ReverseOrder());
            ArrayUtil.BinarySort(a, Collections.ReverseOrder());
        }
    }
}
