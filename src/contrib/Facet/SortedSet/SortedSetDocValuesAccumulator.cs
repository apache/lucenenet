using Lucene.Net.Facet.Params;
using Lucene.Net.Facet.Search;
using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Index;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.SortedSet
{
    public class SortedSetDocValuesAccumulator : FacetsAccumulator
    {
        readonly SortedSetDocValuesReaderState state;
        readonly SortedSetDocValues dv;
        readonly string field;

        public SortedSetDocValuesAccumulator(FacetSearchParams fsp, SortedSetDocValuesReaderState state)
            : base(fsp, null, null, new FacetArrays((int)state.DocValues.ValueCount))
        {
            this.state = state;
            this.field = state.Field;
            dv = state.DocValues;

            foreach (FacetRequest request in fsp.facetRequests)
            {
                if (!(request is CountFacetRequest))
                {
                    throw new ArgumentException(@"this collector only supports CountFacetRequest; got " + request);
                }

                if (request.categoryPath.length != 1)
                {
                    throw new ArgumentException(@"this collector only supports depth 1 CategoryPath; got " + request.categoryPath);
                }

                if (request.Depth != 1)
                {
                    throw new ArgumentException(@"this collector only supports depth=1; got " + request.Depth);
                }

                string dim = request.categoryPath.components[0];
                SortedSetDocValuesReaderState.OrdRange ordRange = state.GetOrdRange(dim);
                if (ordRange == null)
                {
                    throw new ArgumentException("dim \"" + dim + "\" does not exist");
                }
            }
        }

        public override IFacetsAggregator Aggregator
        {
            get
            {
                return new AnonymousFacetsAggregator(this);
            }
        }

        private sealed class AnonymousFacetsAggregator : IFacetsAggregator
        {
            public AnonymousFacetsAggregator(SortedSetDocValuesAccumulator parent)
            {
                this.parent = parent;
            }

            private readonly SortedSetDocValuesAccumulator parent;

            public void Aggregate(FacetsCollector.MatchingDocs matchingDocs, CategoryListParams clp, FacetArrays facetArrays)
            {
                SortedSetDocValues segValues = matchingDocs.context.AtomicReader.GetSortedSetDocValues(parent.field);
                if (segValues == null)
                {
                    return;
                }

                int[] counts = facetArrays.GetIntArray();
                int maxDoc = matchingDocs.context.AtomicReader.MaxDoc;
                if (parent.dv is MultiDocValues.MultiSortedSetDocValues)
                {
                    MultiDocValues.OrdinalMap ordinalMap = ((MultiDocValues.MultiSortedSetDocValues)parent.dv).mapping;
                    int segOrd = matchingDocs.context.ord;
                    int numSegOrds = (int)segValues.ValueCount;
                    if (matchingDocs.totalHits < numSegOrds / 10)
                    {
                        int doc = 0;
                        while (doc < maxDoc && (doc = matchingDocs.bits.NextSetBit(doc)) != -1)
                        {
                            segValues.SetDocument(doc);
                            int term = (int)segValues.NextOrd();
                            while (term != SortedSetDocValues.NO_MORE_ORDS)
                            {
                                counts[(int)ordinalMap.GetGlobalOrd(segOrd, term)]++;
                                term = (int)segValues.NextOrd();
                            }

                            ++doc;
                        }
                    }
                    else
                    {
                        int[] segCounts = new int[numSegOrds];
                        int doc = 0;
                        while (doc < maxDoc && (doc = matchingDocs.bits.NextSetBit(doc)) != -1)
                        {
                            segValues.SetDocument(doc);
                            int term = (int)segValues.NextOrd();
                            while (term != SortedSetDocValues.NO_MORE_ORDS)
                            {
                                segCounts[term]++;
                                term = (int)segValues.NextOrd();
                            }

                            ++doc;
                        }

                        for (int ord = 0; ord < numSegOrds; ord++)
                        {
                            int count = segCounts[ord];
                            if (count != 0)
                            {
                                counts[(int)ordinalMap.GetGlobalOrd(segOrd, ord)] += count;
                            }
                        }
                    }
                }
                else
                {
                    int doc = 0;
                    while (doc < maxDoc && (doc = matchingDocs.bits.NextSetBit(doc)) != -1)
                    {
                        segValues.SetDocument(doc);
                        int term = (int)segValues.NextOrd();
                        while (term != SortedSetDocValues.NO_MORE_ORDS)
                        {
                            counts[term]++;
                            term = (int)segValues.NextOrd();
                        }

                        ++doc;
                    }
                }
            }

            public void RollupValues(FacetRequest fr, int ordinal, int[] children, int[] siblings, FacetArrays facetArrays)
            {
            }

            public bool RequiresDocScores
            {
                get
                {
                    return false;
                }
            }
        }

        class TopCountPQ : Lucene.Net.Util.PriorityQueue<FacetResultNode>
        {
            public TopCountPQ(int topN)
                : base(topN, false)
            {
            }

            public override bool LessThan(FacetResultNode a, FacetResultNode b)
            {
                if (a.value < b.value)
                {
                    return true;
                }
                else if (a.value > b.value)
                {
                    return false;
                }
                else
                {
                    return a.ordinal > b.ordinal;
                }
            }
        }

        public override List<FacetResult> Accumulate(List<FacetsCollector.MatchingDocs> matchingDocs)
        {
            IFacetsAggregator aggregator = Aggregator;
            foreach (CategoryListParams clp in GetCategoryLists())
            {
                foreach (FacetsCollector.MatchingDocs md in matchingDocs)
                {
                    aggregator.Aggregate(md, clp, facetArrays);
                }
            }

            List<FacetResult> results = new List<FacetResult>();
            int[] counts = facetArrays.GetIntArray();
            BytesRef scratch = new BytesRef();
            foreach (FacetRequest request in searchParams.facetRequests)
            {
                string dim = request.categoryPath.components[0];
                SortedSetDocValuesReaderState.OrdRange ordRange = state.GetOrdRange(dim);
                if (request.numResults >= ordRange.end - ordRange.start + 1)
                {
                    List<FacetResultNode> nodes = new List<FacetResultNode>();
                    int dimCount = 0;
                    for (int ord = ordRange.start; ord <= ordRange.end; ord++)
                    {
                        if (counts[ord] != 0)
                        {
                            dimCount += counts[ord];
                            FacetResultNode node = new FacetResultNode(ord, counts[ord]);
                            dv.LookupOrd(ord, scratch);
                            node.label = new CategoryPath(scratch.Utf8ToString().Split(new[] { state.separatorRegex }, StringSplitOptions.None));
                            nodes.Add(node);
                        }
                    }

                    nodes.Sort(new AnonymousComparator());
                    CategoryListParams.OrdinalPolicy op = searchParams.indexingParams.GetCategoryListParams(request.categoryPath).GetOrdinalPolicy(dim);
                    if (op == CategoryListParams.OrdinalPolicy.ALL_BUT_DIMENSION)
                    {
                        dimCount = 0;
                    }

                    FacetResultNode rootNode = new FacetResultNode(-1, dimCount);
                    rootNode.label = new CategoryPath(new string[] { dim } );
                    rootNode.subResults = nodes;
                    results.Add(new FacetResult(request, rootNode, nodes.Count));
                    continue;
                }

                TopCountPQ q = new TopCountPQ(request.numResults);
                int bottomCount = 0;
                int dimCount2 = 0;
                int childCount = 0;
                FacetResultNode reuse = null;
                for (int ord = ordRange.start; ord <= ordRange.end; ord++)
                {
                    if (counts[ord] > 0)
                    {
                        childCount++;
                        if (counts[ord] > bottomCount)
                        {
                            dimCount2 += counts[ord];
                            if (reuse == null)
                            {
                                reuse = new FacetResultNode(ord, counts[ord]);
                            }
                            else
                            {
                                reuse.ordinal = ord;
                                reuse.value = counts[ord];
                            }

                            reuse = q.InsertWithOverflow(reuse);
                            if (q.Size == request.numResults)
                            {
                                bottomCount = (int)q.Top().value;
                            }
                        }
                    }
                }

                CategoryListParams.OrdinalPolicy op2 = searchParams.indexingParams.GetCategoryListParams(request.categoryPath).GetOrdinalPolicy(dim);
                if (op2 == CategoryListParams.OrdinalPolicy.ALL_BUT_DIMENSION)
                {
                    dimCount2 = 0;
                }

                FacetResultNode rootNode2 = new FacetResultNode(-1, dimCount2);
                rootNode2.label = new CategoryPath(new string[] { dim } );
                FacetResultNode[] childNodes = new FacetResultNode[q.Size];
                for (int i = childNodes.Length - 1; i >= 0; i--)
                {
                    childNodes[i] = q.Pop();
                    dv.LookupOrd(childNodes[i].ordinal, scratch);
                    childNodes[i].label = new CategoryPath(scratch.Utf8ToString().Split(new[] { state.separatorRegex }, StringSplitOptions.None));
                }

                rootNode2.subResults = childNodes;
                results.Add(new FacetResult(request, rootNode2, childCount));
            }

            return results;
        }

        private sealed class AnonymousComparator : IComparer<FacetResultNode>
        {
            public int Compare(FacetResultNode o1, FacetResultNode o2)
            {
                int value = (int)(o2.value - o1.value);
                if (value == 0)
                {
                    value = o1.ordinal - o2.ordinal;
                }

                return value;
            }
        }
    }
}
