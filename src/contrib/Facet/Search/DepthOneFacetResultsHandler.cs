using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Search
{
    public abstract class DepthOneFacetResultsHandler : FacetResultsHandler
    {
        private class FacetResultNodeQueue : PriorityQueue<FacetResultNode>
        {
            public FacetResultNodeQueue(int maxSize, bool prepopulate)
                : base(maxSize, prepopulate)
            {
            }

            protected override FacetResultNode SentinelObject
            {
                get
                {
                    return new FacetResultNode(TaxonomyReader.INVALID_ORDINAL, 0);
                }
            }

            public override bool LessThan(FacetResultNode a, FacetResultNode b)
            {
                if (a.value < b.value)
                    return true;
                if (a.value > b.value)
                    return false;
                return a.ordinal < b.ordinal;
            }
        }

        public DepthOneFacetResultsHandler(TaxonomyReader taxonomyReader, FacetRequest facetRequest, FacetArrays facetArrays)
            : base(taxonomyReader, facetRequest, facetArrays)
        {
        }

        protected abstract double ValueOf(int ordinal);
        protected abstract void AddSiblings(int ordinal, int[] siblings, List<FacetResultNode> nodes);
        protected abstract int AddSiblings(int ordinal, int[] siblings, PriorityQueue<FacetResultNode> pq);
        public override FacetResult Compute()
        {
            ParallelTaxonomyArrays arrays = taxonomyReader.ParallelTaxonomyArrays;
            int[] children = arrays.Children;
            int[] siblings = arrays.Siblings;
            int rootOrd = taxonomyReader.GetOrdinal(facetRequest.categoryPath);
            FacetResultNode root = new FacetResultNode(rootOrd, ValueOf(rootOrd));
            root.label = facetRequest.categoryPath;
            if (facetRequest.numResults > taxonomyReader.Size)
            {
                List<FacetResultNode> nodes = new List<FacetResultNode>();
                int child = children[rootOrd];
                AddSiblings(child, siblings, nodes);
                nodes.Sort(new AnonymousComparator());
                root.subResults = nodes;
                return new FacetResult(facetRequest, root, nodes.Count);
            }

            PriorityQueue<FacetResultNode> pq = new FacetResultNodeQueue(facetRequest.numResults, true);
            int numSiblings = AddSiblings(children[rootOrd], siblings, pq);
            int pqsize = pq.Size;
            int size = numSiblings < pqsize ? numSiblings : pqsize;
            for (int i = pqsize - size; i > 0; i--)
            {
                pq.Pop();
            }

            FacetResultNode[] subResults = new FacetResultNode[size];
            for (int i = size - 1; i >= 0; i--)
            {
                FacetResultNode node = pq.Pop();
                node.label = taxonomyReader.GetPath(node.ordinal);
                subResults[i] = node;
            }

            root.subResults = subResults;
            return new FacetResult(facetRequest, root, numSiblings);
        }

        private sealed class AnonymousComparator : IComparer<FacetResultNode>
        {
            public int Compare(FacetResultNode o1, FacetResultNode o2)
            {
                int value = (int)(o2.value - o1.value);
                if (value == 0)
                {
                    value = o2.ordinal - o1.ordinal;
                }

                return value;
            }
        }
    }
}
