using System;
using Lucene.Net.JavaCompatibility;
using Lucene.Net.Randomized.Generators;
using Lucene.Net.Support;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Test.Util
{
    [TestFixture]
    public class TestArrayUtil : LuceneTestCase
    {
        [Test]
        public void TestGrowth()
        {
            int currentSize = 0;
            long copyCost = 0;
            while (currentSize != int.MaxValue)
            {
                int nextSize = ArrayUtil.Oversize(1 + currentSize, RamUsageEstimator.NUM_BYTES_OBJECT_REF);
                assertTrue(nextSize > currentSize);
                if (currentSize > 0)
                {
                    copyCost += currentSize;
                    double copyCostPerElement = ((double)copyCost) / currentSize;
                    assertTrue(@"cost " + copyCostPerElement, copyCostPerElement < 10.0);
                }

                currentSize = nextSize;
            }
        }

        [Test]
        public void TestMaxSize()
        {
            for (int elemSize = 0; elemSize < 10; elemSize++)
            {
                assertEquals(int.MaxValue, ArrayUtil.Oversize(int.MaxValue, elemSize));
                assertEquals(int.MaxValue, ArrayUtil.Oversize(int.MaxValue - 1, elemSize));
            }
        }

        [Test]
        public void TestInvalidElementSizes()
        {
            Random rnd = Random();
            int num = AtLeast(10000);
            for (int iter = 0; iter < num; iter++)
            {
                int minTargetSize = rnd.nextInt(int.MaxValue);
                int elemSize = rnd.nextInt(11);
                int v = ArrayUtil.Oversize(minTargetSize, elemSize);
                assertTrue(v >= minTargetSize);
            }
        }

        [Test]
        public void TestParseInt()
        {
            int test;
            try
            {
                test = ArrayUtil.ParseInt(@"".ToCharArray());
                assertTrue(false);
            }
            catch (FormatException e)
            {
            }

            try
            {
                test = ArrayUtil.ParseInt(@"foo".ToCharArray());
                assertTrue(false);
            }
            catch (FormatException e)
            {
            }

            try
            {
                test = ArrayUtil.ParseInt(long.MaxValue.ToString().ToCharArray());
                assertTrue(false);
            }
            catch (FormatException e)
            {
            }

            try
            {
                test = ArrayUtil.ParseInt(@"0.34".ToCharArray());
                assertTrue(false);
            }
            catch (FormatException e)
            {
            }

            try
            {
                test = ArrayUtil.ParseInt(@"1".ToCharArray());
                assertTrue(test + @" does not equal: " + 1, test == 1);
                test = ArrayUtil.ParseInt(@"-10000".ToCharArray());
                assertTrue(test + @" does not equal: " + -10000, test == -10000);
                test = ArrayUtil.ParseInt(@"1923".ToCharArray());
                assertTrue(test + @" does not equal: " + 1923, test == 1923);
                test = ArrayUtil.ParseInt(@"-1".ToCharArray());
                assertTrue(test + @" does not equal: " + -1, test == -1);
                test = ArrayUtil.ParseInt(@"foo 1923 bar".ToCharArray(), 4, 4);
                assertTrue(test + @" does not equal: " + 1923, test == 1923);
            }
            catch (FormatException e)
            {
                e.printStackTrace();
                assertTrue(false);
            }
        }

        [Test]
        public void TestSliceEquals()
        {
            string left = @"this is equal";
            string right = left;
            char[] leftChars = left.ToCharArray();
            char[] rightChars = right.ToCharArray();
            assertTrue(left + @" does not equal: " + right, ArrayUtil.Equals(leftChars, 0, rightChars, 0, left.Length));
            assertFalse(left + @" does not equal: " + right, ArrayUtil.Equals(leftChars, 1, rightChars, 0, left.Length));
            assertFalse(left + @" does not equal: " + right, ArrayUtil.Equals(leftChars, 1, rightChars, 2, left.Length));
            assertFalse(left + @" does not equal: " + right, ArrayUtil.Equals(leftChars, 25, rightChars, 0, left.Length));
            assertFalse(left + @" does not equal: " + right, ArrayUtil.Equals(leftChars, 12, rightChars, 0, left.Length));
        }

        private int[] CreateRandomArray(int maxSize)
        {
            Random rnd = Random();
            var a = new int[rnd.nextInt(maxSize) + 1];
            for (int i = 0; i < a.Length; i++)
            {
                a[i] = rnd.nextInt(a.Length);
            }

            return a;
        }

        [Test]
        public void TestQuickSort()
        {
            int num = AtLeast(50);
            for (int i = 0; i < num; i++)
            {
                var a1 = CreateRandomArray(2000);
                var a2 = a1.clone();
                ArrayUtil.QuickSort(a1);
                Arrays.Sort(a2);
                assertArrayEquals(a2, a1);
                a1 = CreateRandomArray(2000);
                a2 = a1.clone();
                ArrayUtil.QuickSort(a1, Collections.ReverseOrder<int>());
                Arrays.Sort(a2, Collections.ReverseOrder<int>());
                assertArrayEquals(a2, a1);
                ArrayUtil.QuickSort(a1);
                Arrays.Sort(a2);
                assertArrayEquals(a2, a1);
            }
        }

        private int[] CreateSparseRandomArray(int maxSize)
        {
            Random rnd = Random();
            var a = new int[rnd.nextInt(maxSize) + 1];
            for (int i = 0; i < a.Length; i++)
            {
                a[i] = rnd.nextInt(2);
            }

            return a;
        }

        [Test]
        public void TestQuickToMergeSortFallback()
        {
            int num = AtLeast(50);
            for (int i = 0; i < num; i++)
            {
                var a1 = CreateSparseRandomArray(40000);
                var a2 = a1.clone();
                ArrayUtil.QuickSort(a1);
                Arrays.Sort(a2);
                assertArrayEquals(a2, a1);
            }
        }

        [Test]
        public void TestMergeSort()
        {
            int num = AtLeast(50);
            for (int i = 0; i < num; i++)
            {
                var a1 = CreateRandomArray(2000);
                var a2 = a1.clone();
                ArrayUtil.MergeSort(a1);
                Arrays.Sort(a2);
                assertArrayEquals(a2, a1);
                a1 = CreateRandomArray(2000);
                a2 = a1.clone();
                ArrayUtil.MergeSort(a1, Collections.ReverseOrder<int>());
                Arrays.Sort(a2, Collections.ReverseOrder<int>());
                assertArrayEquals(a2, a1);
                ArrayUtil.MergeSort(a1);
                Arrays.Sort(a2);
                assertArrayEquals(a2, a1);
            }
        }

        [Test]
        public void TestTimSort()
        {
            int num = AtLeast(65);
            for (int i = 0; i < num; i++)
            {
                var a1 = CreateRandomArray(2000);
                var a2 = a1.clone();
                ArrayUtil.TimSort(a1);
                Arrays.Sort(a2);
                assertArrayEquals(a2, a1);
                a1 = CreateRandomArray(2000);
                a2 = a1.clone();
                ArrayUtil.TimSort(a1, Collections.ReverseOrder<int>());
                Arrays.Sort(a2, Collections.ReverseOrder<int>());
                assertArrayEquals(a2, a1);
                ArrayUtil.TimSort(a1);
                Arrays.Sort(a2);
                assertArrayEquals(a2, a1);
            }
        }

        [Test]
        public void TestInsertionSort()
        {
            for (int i = 0, c = AtLeast(500); i < c; i++)
            {
                var a1 = CreateRandomArray(30);
                var a2 = a1.clone();
                ArrayUtil.InsertionSort(a1);
                Arrays.Sort(a2);
                assertArrayEquals(a2, a1);
                a1 = CreateRandomArray(30);
                a2 = a1.clone();
                ArrayUtil.InsertionSort(a1, Collections.ReverseOrder<int>());
                Arrays.Sort(a2, Collections.ReverseOrder<int>());
                assertArrayEquals(a2, a1);
                ArrayUtil.InsertionSort(a1);
                Arrays.Sort(a2);
                assertArrayEquals(a2, a1);
            }
        }

        [Test]
        public void TestBinarySort()
        {
            for (int i = 0, c = AtLeast(500); i < c; i++)
            {
                var a1 = CreateRandomArray(30);
                var a2 = a1.clone();
                ArrayUtil.BinarySort(a1);
                Arrays.Sort(a2);
                assertArrayEquals(a2, a1);
                a1 = CreateRandomArray(30);
                a2 = a1.clone();
                ArrayUtil.BinarySort(a1, Collections.ReverseOrder<int>());
                Arrays.Sort(a2, Collections.ReverseOrder<int>());
                assertArrayEquals(a2, a1);
                ArrayUtil.BinarySort(a1);
                Arrays.Sort(a2);
                assertArrayEquals(a2, a1);
            }
        }

        class Item : IComparable<Item>
        {
            public readonly int val, order;
            public Item(int val, int order)
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
        public void TestMergeSortStability()
        {
            Random rnd = Random();
            Item[] items = new Item[100];
            for (int i = 0; i < items.Length; i++)
            {
                bool equal = rnd.NextBoolean();
                items[i] = new Item(equal ? (i + 1) : -1, equal ? 0 : (rnd.nextInt(1000) + 1));
            }

            if (VERBOSE)
                Console.WriteLine(@"Before: " + Arrays.ToString(items));
            ArrayUtil.MergeSort(items);
            if (VERBOSE)
                Console.WriteLine(@"Sorted: " + Arrays.ToString(items));
            Item last = items[0];
            for (int i = 1; i < items.Length; i++)
            {
                Item act = items[i];
                if (act.order == 0)
                {
                    assertTrue(act.val > last.val);
                }

                assertTrue(act.order >= last.order);
                last = act;
            }
        }

        [Test]
        public void TestTimSortStability()
        {
            Random rnd = Random();
            Item[] items = new Item[100];
            for (int i = 0; i < items.Length; i++)
            {
                bool equal = rnd.NextBoolean();
                items[i] = new Item(equal ? (i + 1) : -1, equal ? 0 : (rnd.nextInt(1000) + 1));
            }

            if (VERBOSE)
                Console.WriteLine(@"Before: " + Arrays.ToString(items));
            ArrayUtil.TimSort(items);
            if (VERBOSE)
                Console.WriteLine(@"Sorted: " + Arrays.ToString(items));
            Item last = items[0];
            for (int i = 1; i < items.Length; i++)
            {
                Item act = items[i];
                if (act.order == 0)
                {
                    assertTrue(act.val > last.val);
                }

                assertTrue(act.order >= last.order);
                last = act;
            }
        }

        [Test]
        public void TestEmptyArraySort()
        {
            var a = new int[0];
            ArrayUtil.QuickSort(a);
            ArrayUtil.MergeSort(a);
            ArrayUtil.InsertionSort(a);
            ArrayUtil.BinarySort(a);
            ArrayUtil.TimSort(a);
            ArrayUtil.QuickSort(a, Collections.ReverseOrder<int>());
            ArrayUtil.MergeSort(a, Collections.ReverseOrder<int>());
            ArrayUtil.TimSort(a, Collections.ReverseOrder<int>());
            ArrayUtil.InsertionSort(a, Collections.ReverseOrder<int>());
            ArrayUtil.BinarySort(a, Collections.ReverseOrder<int>());
        }
    }
}