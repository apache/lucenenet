using System;
using Lucene.Net.Support;
using Lucene.Net.Test.Support;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Test.Util
{
    [TestFixture]
    public class TestSorterTemplate : LuceneTestCase
    {
        private Random random = new Random();

        private static readonly int SLOW_SORT_THRESHOLD = 1000;

        // A sorter template that compares only the last 32 bits
        internal class Last32BitsSorterTemplate : SorterTemplate
        {

            long[] arr;
            long pivot;

            internal Last32BitsSorterTemplate(long[] arr)
            {
                this.arr = arr;
            }

            protected internal override void Swap(int i, int j)
            {
                long tmp = arr[i];
                arr[i] = arr[j];
                arr[j] = tmp;
            }

            private int CompareValues(long i, long j)
            {
                // only compare the last 32 bits
                long a = i & 0xFFFFFFFFL;
                long b = j & 0xFFFFFFFFL;
                return a < b ? -1 : a == b ? 0 : 1;
            }

            protected internal override int Compare(int i, int j)
            {
                return CompareValues(arr[i], arr[j]);
            }

            protected internal override void SetPivot(int i)
            {
                pivot = arr[i];
            }

            protected internal override int ComparePivot(int j)
            {
                return CompareValues(pivot, arr[j]);
            }

            protected override void Merge(int lo, int pivot, int hi, int len1, int len2)
            {
                // TimSort and MergeSort should call runMerge to sort out trivial cases
                Assert.IsTrue(len1 >= 1);
                Assert.IsTrue(len2 >= 1);
                Assert.IsTrue(len1 + len2 >= 3);
                Assert.IsTrue(Compare(lo, pivot) > 0);
                Assert.IsTrue(Compare(pivot - 1, hi - 1) > 0);
                Assert.IsFalse(Compare(pivot - 1, pivot) <= 0);
                base.Merge(lo, pivot, hi, len1, len2);
            }

        }

        void TestSort(int[] intArr)
        {
            // we modify the array as a long[] and store the original ord in the first 32 bits
            // to be able to check stability
            long[] arr = ToLongsAndOrds(intArr);

            // use MergeSort as a reference
            // Assert.AreEqual checks for sorting + stability
            // Assert.AreEqual(ToInts) checks for sorting only
            long[] mergeSorted = Arrays.CopyOf(arr, arr.Length);
            new Last32BitsSorterTemplate(mergeSorted).MergeSort(0, arr.Length - 1);

            if (arr.Length < SLOW_SORT_THRESHOLD)
            {
                long[] insertionSorted = Arrays.CopyOf(arr, arr.Length);
                new Last32BitsSorterTemplate(insertionSorted).InsertionSort(0, arr.Length - 1);
                Assert.AreEqual(mergeSorted, insertionSorted);

                long[] binarySorted = Arrays.CopyOf(arr, arr.Length);
                new Last32BitsSorterTemplate(binarySorted).BinarySort(0, arr.Length - 1);
                Assert.AreEqual(mergeSorted, binarySorted);
            }

            long[] quickSorted = Arrays.CopyOf(arr, arr.Length);
            new Last32BitsSorterTemplate(quickSorted).QuickSort(0, arr.Length - 1);
            Assert.AreEqual(ToInts(mergeSorted), ToInts(quickSorted));

            long[] timSorted = Arrays.CopyOf(arr, arr.Length);
            new Last32BitsSorterTemplate(timSorted).TimSort(0, arr.Length - 1);
            Assert.AreEqual(mergeSorted, timSorted);
        }

        private int[] ToInts(long[] longArr)
        {
            int[] arr = new int[longArr.Length];
            for (int i = 0; i < longArr.Length; ++i)
            {
                arr[i] = (int)longArr[i];
            }
            return arr;
        }

        private long[] ToLongsAndOrds(int[] intArr)
        {
            long[] arr = new long[intArr.Length];
            for (int i = 0; i < intArr.Length; ++i)
            {
                arr[i] = (((long)i) << 32) | (intArr[i] & 0xFFFFFFFFL);
            }
            return arr;
        }

        int RandomLength()
        {
            return _TestUtil.NextInt(random, 1, random.NextBool() ? SLOW_SORT_THRESHOLD : 100000);
        }

        [Test]
        public void TestEmpty()
        {
            TestSort(new int[0]);
        }

        [Test]
        public void TestAscending()
        {
            int Length = RandomLength();
            int[] arr = new int[Length];
            arr[0] = random.Next(10);
            for (int i = 1; i < arr.Length; ++i)
            {
                arr[i] = arr[i - 1] + _TestUtil.NextInt(random, 0, 10);
            }
            TestSort(arr);
        }

        [Test]
        public void TestDescending()
        {
            int Length = RandomLength();
            int[] arr = new int[Length];
            arr[0] = random.Next(10);
            for (int i = 1; i < arr.Length; ++i)
            {
                arr[i] = arr[i - 1] - _TestUtil.NextInt(random, 0, 10);
            }
            TestSort(arr);
        }

        [Test]
        public void TestStrictlyDescending()
        {
            int Length = RandomLength();
            int[] arr = new int[Length];
            arr[0] = random.Next(10);
            for (int i = 1; i < arr.Length; ++i)
            {
                arr[i] = arr[i - 1] - _TestUtil.NextInt(random, 1, 10);
            }
            TestSort(arr);
        }

        [Test]
        public void TestRandom1()
        {
            int Length = RandomLength();
            int[] arr = new int[Length];
            for (int i = 1; i < arr.Length; ++i)
            {
                arr[i] = random.Next();
            }
            TestSort(arr);
        }

        [Test]
        public void TestRandom2()
        {
            int Length = RandomLength();
            int[] arr = new int[Length];
            for (int i = 1; i < arr.Length; ++i)
            {
                arr[i] = random.Next(10);
            }
            TestSort(arr);
        }
    }
}
