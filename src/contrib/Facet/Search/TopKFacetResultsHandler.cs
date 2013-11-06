using Lucene.Net.Facet.Partitions;
using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Facet.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Search
{
    public class TopKFacetResultsHandler : PartitionsFacetResultsHandler
    {
        public TopKFacetResultsHandler(TaxonomyReader taxonomyReader, FacetRequest facetRequest, FacetArrays facetArrays)
            : base(taxonomyReader, facetRequest, facetArrays)
        {
        }

        public override IIntermediateFacetResult FetchPartitionResult(int offset)
        {
            TopKFacetResult res = null;
            int ordinal = taxonomyReader.GetOrdinal(facetRequest.categoryPath);
            if (ordinal != TaxonomyReader.INVALID_ORDINAL)
            {
                double value = 0;
                if (IsSelfPartition(ordinal, facetArrays, offset))
                {
                    int partitionSize = facetArrays.arrayLength;
                    value = facetRequest.GetValueOf(facetArrays, ordinal % partitionSize);
                }

                FacetResultNode parentResultNode = new FacetResultNode(ordinal, value);
                IHeap<FacetResultNode> heap = ResultSortUtils.CreateSuitableHeap(facetRequest);
                int totalFacets = HeapDescendants(ordinal, heap, parentResultNode, offset);
                res = new TopKFacetResult(facetRequest, parentResultNode, totalFacets);
                res.SetHeap(heap);
            }

            return res;
        }

        public override IIntermediateFacetResult MergeResults(params IIntermediateFacetResult[] tmpResults)
        {
            int ordinal = taxonomyReader.GetOrdinal(facetRequest.categoryPath);
            FacetResultNode resNode = new FacetResultNode(ordinal, 0);
            int totalFacets = 0;
            IHeap<FacetResultNode> heap = null;
            foreach (IIntermediateFacetResult tmpFres in tmpResults)
            {
                TopKFacetResult fres = (TopKFacetResult)tmpFres;
                totalFacets += fres.NumValidDescendants;
                resNode.value += fres.FacetResultNode.value;
                IHeap<FacetResultNode> tmpHeap = fres.GetHeap();
                if (heap == null)
                {
                    heap = tmpHeap;
                    continue;
                }

                for (int i = tmpHeap.Size; i > 0; i--)
                {
                    heap.InsertWithOverflow(tmpHeap.Pop());
                }
            }

            TopKFacetResult res = new TopKFacetResult(facetRequest, resNode, totalFacets);
            res.SetHeap(heap);
            return res;
        }

        private int HeapDescendants(int ordinal, IHeap<FacetResultNode> pq, FacetResultNode parentResultNode, int offset)
        {
            int partitionSize = facetArrays.arrayLength;
            int endOffset = offset + partitionSize;
            ParallelTaxonomyArrays childrenArray = taxonomyReader.ParallelTaxonomyArrays;
            int[] children = childrenArray.Children;
            int[] siblings = childrenArray.Siblings;
            FacetResultNode reusable = null;
            int localDepth = 0;
            int depth = facetRequest.Depth;
            int[] ordinalStack = new int[2 + Math.Min(short.MaxValue, depth)];
            int childrenCounter = 0;
            int tosOrdinal;
            int yc = children[ordinal];
            while (yc >= endOffset)
            {
                yc = siblings[yc];
            }

            ordinalStack[++localDepth] = yc;
            while (localDepth > 0)
            {
                tosOrdinal = ordinalStack[localDepth];
                if (tosOrdinal == TaxonomyReader.INVALID_ORDINAL)
                {
                    localDepth--;
                    ordinalStack[localDepth] = siblings[ordinalStack[localDepth]];
                    continue;
                }

                if (tosOrdinal >= offset)
                {
                    int relativeOrdinal = tosOrdinal % partitionSize;
                    double value = facetRequest.GetValueOf(facetArrays, relativeOrdinal);
                    if (value != 0 && !Double.IsNaN(value))
                    {
                        if (reusable == null)
                        {
                            reusable = new FacetResultNode(tosOrdinal, value);
                        }
                        else
                        {
                            reusable.ordinal = tosOrdinal;
                            reusable.value = value;
                            reusable.subResults.Clear();
                            reusable.label = null;
                        }

                        ++childrenCounter;
                        reusable = pq.InsertWithOverflow(reusable);
                    }
                }

                if (localDepth < depth)
                {
                    yc = children[tosOrdinal];
                    while (yc >= endOffset)
                    {
                        yc = siblings[yc];
                    }

                    ordinalStack[++localDepth] = yc;
                }
                else
                {
                    ordinalStack[++localDepth] = TaxonomyReader.INVALID_ORDINAL;
                }
            }

            return childrenCounter;
        }

        public override FacetResult RenderFacetResult(IIntermediateFacetResult tmpResult)
        {
            TopKFacetResult res = (TopKFacetResult)tmpResult;
            if (res != null)
            {
                IHeap<FacetResultNode> heap = res.GetHeap();
                FacetResultNode resNode = res.FacetResultNode;
                if (resNode.subResults == FacetResultNode.EMPTY_SUB_RESULTS)
                {
                    resNode.subResults = new List<FacetResultNode>();
                }

                for (int i = heap.Size; i > 0; i--)
                {
                    resNode.subResults.Insert(0, heap.Pop());
                }
            }

            return res;
        }

        public override FacetResult RearrangeFacetResult(FacetResult facetResult)
        {
            TopKFacetResult res = (TopKFacetResult)facetResult;
            IHeap<FacetResultNode> heap = res.GetHeap();
            heap.Clear();
            FacetResultNode topFrn = res.FacetResultNode;
            foreach (FacetResultNode frn in topFrn.subResults)
            {
                heap.Add(frn);
            }

            int size = heap.Size;
            List<FacetResultNode> subResults = new List<FacetResultNode>(size);
            for (int i = heap.Size; i > 0; i--)
            {
                subResults.Insert(0, heap.Pop());
            }

            topFrn.subResults = subResults;
            return res;
        }

        public override void LabelResult(FacetResult facetResult)
        {
            if (facetResult != null)
            {
                FacetResultNode facetResultNode = facetResult.FacetResultNode;
                if (facetResultNode != null)
                {
                    facetResultNode.label = taxonomyReader.GetPath(facetResultNode.ordinal);
                    int num2label = facetRequest.NumLabel;
                    foreach (FacetResultNode frn in facetResultNode.subResults)
                    {
                        if (--num2label < 0)
                        {
                            break;
                        }

                        frn.label = taxonomyReader.GetPath(frn.ordinal);
                    }
                }
            }
        }

        private class TopKFacetResult : FacetResult, IIntermediateFacetResult
        {
            private IHeap<FacetResultNode> heap;

            internal TopKFacetResult(FacetRequest facetRequest, FacetResultNode facetResultNode, int totalFacets)
                : base(facetRequest, facetResultNode, totalFacets)
            {
            }

            public virtual IHeap<FacetResultNode> GetHeap()
            {
                return heap;
            }

            public virtual void SetHeap(IHeap<FacetResultNode> heap)
            {
                this.heap = heap;
            }
        }
    }
}
