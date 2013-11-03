using Lucene.Net.Facet.Search;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Util
{
    public static class ResultSortUtils
    {
        public static IHeap<FacetResultNode> CreateSuitableHeap(FacetRequest facetRequest)
        {
            int nresults = facetRequest.numResults;
            bool accending = (facetRequest.SortOrderValue == FacetRequest.SortOrder.ASCENDING);

            if (nresults == int.MaxValue)
            {
                return new AllValueHeap(accending);
            }

            if (accending)
            {
                return new MaxValueHeap(nresults);
            }
            else
            {
                return new MinValueHeap(nresults);
            }
        }

        private class MinValueHeap : PriorityQueue<FacetResultNode>, IHeap<FacetResultNode>
        {
            public MinValueHeap(int size)
                : base(size)
            {
            }

            public override bool LessThan(FacetResultNode arg0, FacetResultNode arg1)
            {
                double value0 = arg0.value;
                double value1 = arg1.value;
                int valueCompare = value0.CompareTo(value1);
                if (valueCompare == 0)
                {
                    return arg0.ordinal < arg1.ordinal;
                }

                return valueCompare < 0;
            }
        }

        private class MaxValueHeap : PriorityQueue<FacetResultNode>, IHeap<FacetResultNode>
        {
            public MaxValueHeap(int size)
                : base(size)
            {
            }

            public override bool LessThan(FacetResultNode arg0, FacetResultNode arg1)
            {
                double value0 = arg0.value;
                double value1 = arg1.value;
                int valueCompare = value0.CompareTo(value1);
                if (valueCompare == 0)
                {
                    return arg0.ordinal > arg1.ordinal;
                }

                return valueCompare > 0;
            }
        }

        private class AllValueHeap : IHeap<FacetResultNode>
        {
            private List<FacetResultNode> resultNodes = new List<FacetResultNode>();
            readonly bool accending;
            private bool isReady = false;

            public AllValueHeap(bool accending)
            {
                this.accending = accending;
            }

            public FacetResultNode InsertWithOverflow(FacetResultNode node)
            {
                resultNodes.Add(node);
                return null;
            }

            public FacetResultNode Pop()
            {
                if (!isReady)
                {
                    resultNodes.Sort(new AnonymousComparator(this));
                    isReady = true;
                }

                var tmp = resultNodes[0];
                resultNodes.RemoveAt(0);
                return tmp;
            }

            private sealed class AnonymousComparator : IComparer<FacetResultNode>
            {
                public AnonymousComparator(AllValueHeap parent)
                {
                    this.parent = parent;
                }

                private readonly AllValueHeap parent;

                public int Compare(FacetResultNode o1, FacetResultNode o2)
                {
                    int value = o1.value.CompareTo(o2.value);
                    if (value == 0)
                    {
                        value = o1.ordinal - o2.ordinal;
                    }

                    if (parent.accending)
                    {
                        value = -value;
                    }

                    return value;
                }
            }

            public int Size
            {
                get
                {
                    return resultNodes.Count;
                }
            }

            public FacetResultNode Top()
            {
                if (resultNodes.Count > 0)
                {
                    return resultNodes[0];
                }

                return null;
            }

            public FacetResultNode Add(FacetResultNode frn)
            {
                resultNodes.Add(frn);
                return null;
            }

            public void Clear()
            {
                resultNodes.Clear();
            }
        }
    }
}
