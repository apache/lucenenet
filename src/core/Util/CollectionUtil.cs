using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util
{
    public static class CollectionUtil
    {
        private abstract class ListSorterTemplate<T> : SorterTemplate
        {
            protected readonly IList<T> list;

            public ListSorterTemplate(IList<T> list)
            {
                this.list = list;
            }

            protected internal abstract int Compare(T a, T b);

            protected internal override void Swap(int i, int j)
            {
                Collections.Swap(list, i, j);
            }

            protected internal override int Compare(int i, int j)
            {
                return Compare(list[i], list[j]);
            }

            protected internal override void SetPivot(int i)
            {
                pivot = list[i];
            }

            protected internal override int ComparePivot(int j)
            {
                return Compare(pivot, list[j]);
            }

            private T pivot;
        }

        private abstract class ListMergeSorterTemplate<T> : ListSorterTemplate<T>
        {
            private readonly int threshold; // maximum length of a merge that can be made using extra memory
            private readonly T[] tmp;

            public ListMergeSorterTemplate(IList<T> list, float overheadRatio)
                : base(list)
            {
                this.threshold = (int)(list.Count * overheadRatio);
                T[] tmpBuf = new T[threshold];
                this.tmp = tmpBuf;
            }

            private void MergeWithExtraMemory(int lo, int pivot, int hi, int len1, int len2)
            {
                for (int k = 0; k < len1; ++k)
                {
                    tmp[k] = list[lo + k];
                }

                int i = 0, j = pivot, dest = lo;
                while (i < len1 && j < hi)
                {
                    if (Compare(tmp[i], list[j]) <= 0)
                    {
                        list[dest++] = tmp[i++];
                    }
                    else
                    {
                        list[dest++] = list[j++];
                    }
                }
                while (i < len1)
                {
                    list[dest++] = tmp[i++];
                }
                //assert j == dest;
            }

            protected override void Merge(int lo, int pivot, int hi, int len1, int len2)
            {
                if (len1 <= threshold)
                {
                    MergeWithExtraMemory(lo, pivot, hi, len1, len2);
                }
                else
                {
                    // since this method recurses to run merge on smaller arrays, it will
                    // end up using mergeWithExtraMemory
                    base.Merge(lo, pivot, hi, len1, len2);
                }
            }
        }

        private class ListSorterTemplateWithComparer<T> : ListSorterTemplate<T>
        {
            private readonly IComparer<T> comp;

            public ListSorterTemplateWithComparer(IList<T> list, IComparer<T> comp)
                : base(list)
            {
                this.comp = comp;
            }

            protected internal override int Compare(T a, T b)
            {
                return comp.Compare(a, b);
            }
        }

        private static SorterTemplate GetSorter<T>(IList<T> list, IComparer<T> comp)
        {
            return new ListSorterTemplateWithComparer<T>(list, comp);
        }

        private class NaturalListSorterTemplate<T> : ListSorterTemplate<T>
            where T : IComparable<T>
        {
            public NaturalListSorterTemplate(IList<T> list)
                : base(list)
            {
            }

            protected internal override int Compare(T a, T b)
            {
                return a.CompareTo(b);
            }
        }

        private static SorterTemplate GetSorter<T>(IList<T> list)
            where T : IComparable<T>
        {
            return new NaturalListSorterTemplate<T>(list);
        }

        private class ListMergeSorterTemplateWithCustomComparer<T> : ListMergeSorterTemplate<T>
        {
            private readonly IComparer<T> comp;

            public ListMergeSorterTemplateWithCustomComparer(IList<T> list, IComparer<T> comp)
                : base(list, ArrayUtil.MERGE_OVERHEAD_RATIO)
            {
                this.comp = comp;
            }

            protected internal override int Compare(T a, T b)
            {
                return comp.Compare(a, b);
            }
        }

        private static SorterTemplate GetMergeSorter<T>(IList<T> list, IComparer<T> comp)
        {
            if (list.Count < ArrayUtil.MERGE_EXTRA_MEMORY_THRESHOLD)
            {
                return GetSorter<T>(list, comp);
            }
            else
            {
                return new ListMergeSorterTemplateWithCustomComparer<T>(list, comp);
            }
        }

        private class NaturalListMergeSorterTemplate<T> : ListMergeSorterTemplate<T>
            where T : IComparable<T>
        {
            public NaturalListMergeSorterTemplate(IList<T> list)
                : base(list, ArrayUtil.MERGE_OVERHEAD_RATIO)
            {
            }

            protected internal override int Compare(T a, T b)
            {
                return a.CompareTo(b);
            }
        }

        private static SorterTemplate GetMergeSorter<T>(IList<T> list)
            where T : IComparable<T>
        {
            if (list.Count < ArrayUtil.MERGE_EXTRA_MEMORY_THRESHOLD)
            {
                return GetSorter(list);
            }
            else
            {
                return new NaturalListMergeSorterTemplate<T>(list);
            }
        }

        public static void QuickSort<T>(IList<T> list, IComparer<T> comp)
        {
            int size = list.Count;
            if (size <= 1) return;
            GetSorter(list, comp).QuickSort(0, size - 1);
        }

        public static void QuickSort<T>(IList<T> list)
            where T : IComparable<T>
        {
            int size = list.Count;
            if (size <= 1) return;
            GetSorter(list).QuickSort(0, size - 1);
        }

        public static void MergeSort<T>(IList<T> list, IComparer<T> comp)
        {
            int size = list.Count;
            if (size <= 1) return;
            GetMergeSorter(list, comp).MergeSort(0, size - 1);
        }

        public static void MergeSort<T>(IList<T> list)
            where T : IComparable<T>
        {
            int size = list.Count;
            if (size <= 1) return;
            GetMergeSorter(list).MergeSort(0, size - 1);
        }

        public static void TimSort<T>(IList<T> list, IComparer<T> comp)
        {
            int size = list.Count;
            if (size <= 1) return;
            GetMergeSorter(list, comp).TimSort(0, size - 1);
        }

        public static void TimSort<T>(IList<T> list)
            where T : IComparable<T>
        {
            int size = list.Count;
            if (size <= 1) return;
            GetMergeSorter(list).TimSort(0, size - 1);
        }

        public static void InsertionSort<T>(IList<T> list, IComparer<T> comp)
        {
            int size = list.Count;
            if (size <= 1) return;
            GetMergeSorter(list, comp).InsertionSort(0, size - 1);
        }

        public static void InsertionSort<T>(IList<T> list)
            where T : IComparable<T>
        {
            int size = list.Count;
            if (size <= 1) return;
            GetMergeSorter(list).InsertionSort(0, size - 1);
        }

        public static void BinarySort<T>(IList<T> list, IComparer<T> comp)
        {
            int size = list.Count;
            if (size <= 1) return;
            GetSorter(list, comp).BinarySort(0, size - 1);
        }

        public static void BinarySort<T>(IList<T> list)
            where T : IComparable<T>
        {
            int size = list.Count;
            if (size <= 1) return;
            GetSorter(list).BinarySort(0, size - 1);
        }
    }
}
