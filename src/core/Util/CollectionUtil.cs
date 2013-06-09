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
            protected readonly List<T> list;

            public ListSorterTemplate(List<T> list)
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

            public ListMergeSorterTemplate(List<T> list, float overheadRatio)
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

            public ListSorterTemplateWithComparer(List<T> list, IComparer<T> comp)
                : base(list)
            {
                this.comp = comp;
            }

            protected internal override int Compare(T a, T b)
            {
                return comp.Compare(a, b);
            }
        }

        private static SorterTemplate GetSorter<T>(List<T> list, IComparer<T> comp)
        {
            return new ListSorterTemplateWithComparer<T>(list, comp);
        }

        private class NaturalListSorterTemplate<T> : ListSorterTemplate<T>
            where T : IComparable<T>
        {
            public NaturalListSorterTemplate(List<T> list)
                : base(list)
            {
            }

            protected internal override int Compare(T a, T b)
            {
                return a.CompareTo(b);
            }
        }

        private static SorterTemplate GetSorter<T>(List<T> list)
            where T : IComparable<T>
        {
            return new NaturalListSorterTemplate<T>(list);
        }

        // PI Left Off: line 149: private static <T> SorterTemplate getMergeSorter(final List<T> list, final Comparator<? super T> comp) {
    }
}
