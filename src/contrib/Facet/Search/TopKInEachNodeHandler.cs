using Lucene.Net.Facet.Collections;
using Lucene.Net.Facet.Partitions;
using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Search
{
    public class TopKInEachNodeHandler : PartitionsFacetResultsHandler
    {
        public TopKInEachNodeHandler(TaxonomyReader taxonomyReader, FacetRequest facetRequest, FacetArrays facetArrays)
            : base(taxonomyReader, facetRequest, facetArrays)
        {
        }

        public override IIntermediateFacetResult FetchPartitionResult(int offset)
        {
            int rootNode = this.taxonomyReader.GetOrdinal(facetRequest.categoryPath);
            if (rootNode == TaxonomyReader.INVALID_ORDINAL)
            {
                return null;
            }

            int K = Math.Min(facetRequest.numResults, taxonomyReader.Size);
            IntToObjectMap<AACO> AACOsOfOnePartition = new IntToObjectMap<AACO>();
            int partitionSize = facetArrays.arrayLength;
            int depth = facetRequest.Depth;
            if (depth == 0)
            {
                IntermediateFacetResultWithHash tempFRWH = new IntermediateFacetResultWithHash(facetRequest, AACOsOfOnePartition);
                if (IsSelfPartition(rootNode, facetArrays, offset))
                {
                    tempFRWH.isRootNodeIncluded = true;
                    tempFRWH.rootNodeValue = this.facetRequest.GetValueOf(facetArrays, rootNode % partitionSize);
                }

                return tempFRWH;
            }

            if (depth > short.MaxValue - 3)
            {
                depth = short.MaxValue - 3;
            }

            int endOffset = offset + partitionSize;
            ParallelTaxonomyArrays childrenArray = taxonomyReader.ParallelTaxonomyArrays;
            int[] children = childrenArray.Children;
            int[] siblings = childrenArray.Siblings;
            int totalNumOfDescendantsConsidered = 0;
            PriorityQueue<AggregatedCategory> pq = new AggregatedCategoryHeap(K, this.GetSuitableACComparator());
            AggregatedCategory[] reusables = new AggregatedCategory[2 + K];
            for (int i = 0; i < reusables.Length; i++)
            {
                reusables[i] = new AggregatedCategory(1, 0);
            }

            int[] ordinalStack = new int[depth + 2];
            ordinalStack[0] = rootNode;
            int localDepth = 0;
            int[][] bestSignlingsStack = new int[depth + 2][];
            int[] siblingExplored = new int[depth + 2];
            int[] firstToTheLeftOfPartition = new int[depth + 2];
            int tosOrdinal;
            ordinalStack[++localDepth] = children[rootNode];
            siblingExplored[localDepth] = int.MaxValue;
            siblingExplored[0] = -1;
            while (localDepth > 0)
            {
                tosOrdinal = ordinalStack[localDepth];
                if (tosOrdinal == TaxonomyReader.INVALID_ORDINAL)
                {
                    localDepth--;
                    if (siblingExplored[localDepth] < 0)
                    {
                        ordinalStack[localDepth] = siblings[ordinalStack[localDepth]];
                        continue;
                    }

                    siblingExplored[localDepth]--;
                    if (siblingExplored[localDepth] == -1)
                    {
                        ordinalStack[localDepth] = firstToTheLeftOfPartition[localDepth];
                    }
                    else
                    {
                        ordinalStack[localDepth] = bestSignlingsStack[localDepth][siblingExplored[localDepth]];
                    }

                    continue;
                }

                if (siblingExplored[localDepth] == int.MaxValue)
                {
                    while (tosOrdinal >= endOffset)
                    {
                        tosOrdinal = siblings[tosOrdinal];
                    }

                    pq.Clear();
                    int tosReuslables = reusables.Length - 1;
                    while (tosOrdinal >= offset)
                    {
                        double value = facetRequest.GetValueOf(facetArrays, tosOrdinal % partitionSize);
                        if (value != 0)
                        {
                            totalNumOfDescendantsConsidered++;
                            AggregatedCategory ac = reusables[tosReuslables--];
                            ac.ordinal = tosOrdinal;
                            ac.value = value;
                            ac = pq.InsertWithOverflow(ac);
                            if (null != ac)
                            {
                                totalNumOfDescendantsConsidered--;
                                totalNumOfDescendantsConsidered += CountOnly(ac.ordinal, children, siblings, partitionSize, offset, endOffset, localDepth, depth);
                                reusables[++tosReuslables] = ac;
                            }
                        }

                        tosOrdinal = siblings[tosOrdinal];
                    }

                    firstToTheLeftOfPartition[localDepth] = tosOrdinal;
                    int aaci = pq.Size;
                    int[] ords = new int[aaci];
                    double[] vals = new double[aaci];
                    while (aaci > 0)
                    {
                        AggregatedCategory ac = pq.Pop();
                        ords[--aaci] = ac.ordinal;
                        vals[aaci] = ac.value;
                        reusables[++tosReuslables] = ac;
                    }

                    if (ords.Length > 0)
                    {
                        AACOsOfOnePartition.Put(ordinalStack[localDepth - 1], new AACO(ords, vals));
                        bestSignlingsStack[localDepth] = ords;
                        siblingExplored[localDepth] = ords.Length - 1;
                        ordinalStack[localDepth] = ords[ords.Length - 1];
                    }
                    else
                    {
                        ordinalStack[localDepth] = tosOrdinal;
                        siblingExplored[localDepth] = -1;
                    }

                    continue;
                }

                if (localDepth >= depth)
                {
                    ordinalStack[++localDepth] = TaxonomyReader.INVALID_ORDINAL;
                    continue;
                }

                ordinalStack[++localDepth] = children[tosOrdinal];
                siblingExplored[localDepth] = int.MaxValue;
            }

            IntermediateFacetResultWithHash tempFRWH2 = new IntermediateFacetResultWithHash(facetRequest, AACOsOfOnePartition);
            if (IsSelfPartition(rootNode, facetArrays, offset))
            {
                tempFRWH2.isRootNodeIncluded = true;
                tempFRWH2.rootNodeValue = this.facetRequest.GetValueOf(facetArrays, rootNode % partitionSize);
            }

            tempFRWH2.totalNumOfFacetsConsidered = totalNumOfDescendantsConsidered;
            return tempFRWH2;
        }

        private int CountOnly(int ordinal, int[] youngestChild, int[] olderSibling, int partitionSize, int offset, int endOffset, int currentDepth, int maxDepth)
        {
            int ret = 0;
            if (offset <= ordinal)
            {
                if (0 != facetRequest.GetValueOf(facetArrays, ordinal % partitionSize))
                {
                    ret++;
                }
            }

            if (currentDepth >= maxDepth)
            {
                return ret;
            }

            int yc = youngestChild[ordinal];
            while (yc >= endOffset)
            {
                yc = olderSibling[yc];
            }

            while (yc > TaxonomyReader.INVALID_ORDINAL)
            {
                ret += CountOnly(yc, youngestChild, olderSibling, partitionSize, offset, endOffset, currentDepth + 1, maxDepth);
                yc = olderSibling[yc];
            }

            return ret;
        }

        public override IIntermediateFacetResult MergeResults(params IIntermediateFacetResult[] tmpResults)
        {
            if (tmpResults.Length == 0)
            {
                return null;
            }

            int i = 0;
            for (; (i < tmpResults.Length) && (tmpResults[i] == null); i++)
            {
            }

            if (i == tmpResults.Length)
            {
                return null;
            }

            int K = this.facetRequest.numResults;
            IntermediateFacetResultWithHash tmpToReturn = (IntermediateFacetResultWithHash)tmpResults[i++];
            for (; i < tmpResults.Length; i++)
            {
                IntermediateFacetResultWithHash tfr = (IntermediateFacetResultWithHash)tmpResults[i];
                tmpToReturn.totalNumOfFacetsConsidered += tfr.totalNumOfFacetsConsidered;
                if (tfr.isRootNodeIncluded)
                {
                    tmpToReturn.isRootNodeIncluded = true;
                    tmpToReturn.rootNodeValue = tfr.rootNodeValue;
                }

                IntToObjectMap<AACO> tmpToReturnMapToACCOs = tmpToReturn.mapToAACOs;
                IntToObjectMap<AACO> tfrMapToACCOs = tfr.mapToAACOs;
                IIntIterator tfrIntIterator = tfrMapToACCOs.GetKeyIterator();
                while (tfrIntIterator.HasNext())
                {
                    int tfrkey = tfrIntIterator.Next();
                    AACO tmpToReturnAACO = null;
                    if (null == (tmpToReturnAACO = tmpToReturnMapToACCOs.Get(tfrkey)))
                    {
                        tmpToReturnMapToACCOs.Put(tfrkey, tfrMapToACCOs.Get(tfrkey));
                    }
                    else
                    {
                        AACO tfrAACO = tfrMapToACCOs.Get(tfrkey);
                        int resLength = tfrAACO.ordinals.Length + tmpToReturnAACO.ordinals.Length;
                        if (K < resLength)
                        {
                            resLength = K;
                        }

                        int[] resOrds = new int[resLength];
                        double[] resVals = new double[resLength];
                        int indexIntoTmpToReturn = 0;
                        int indexIntoTFR = 0;
                        ACComparator merger = GetSuitableACComparator();
                        for (int indexIntoRes = 0; indexIntoRes < resLength; indexIntoRes++)
                        {
                            if (indexIntoTmpToReturn >= tmpToReturnAACO.ordinals.Length)
                            {
                                resOrds[indexIntoRes] = tfrAACO.ordinals[indexIntoTFR];
                                resVals[indexIntoRes] = tfrAACO.values[indexIntoTFR];
                                indexIntoTFR++;
                                continue;
                            }

                            if (indexIntoTFR >= tfrAACO.ordinals.Length)
                            {
                                resOrds[indexIntoRes] = tmpToReturnAACO.ordinals[indexIntoTmpToReturn];
                                resVals[indexIntoRes] = tmpToReturnAACO.values[indexIntoTmpToReturn];
                                indexIntoTmpToReturn++;
                                continue;
                            }

                            if (merger.LeftGoesNow(tmpToReturnAACO.ordinals[indexIntoTmpToReturn], tmpToReturnAACO.values[indexIntoTmpToReturn], tfrAACO.ordinals[indexIntoTFR], tfrAACO.values[indexIntoTFR]))
                            {
                                resOrds[indexIntoRes] = tmpToReturnAACO.ordinals[indexIntoTmpToReturn];
                                resVals[indexIntoRes] = tmpToReturnAACO.values[indexIntoTmpToReturn];
                                indexIntoTmpToReturn++;
                            }
                            else
                            {
                                resOrds[indexIntoRes] = tfrAACO.ordinals[indexIntoTFR];
                                resVals[indexIntoRes] = tfrAACO.values[indexIntoTFR];
                                indexIntoTFR++;
                            }
                        }

                        tmpToReturnMapToACCOs.Put(tfrkey, new AACO(resOrds, resVals));
                    }
                }
            }

            return tmpToReturn;
        }

        private class AggregatedCategoryHeap : PriorityQueue<AggregatedCategory>
        {
            private ACComparator merger;
            public AggregatedCategoryHeap(int size, ACComparator merger)
                : base(size)
            {
                this.merger = merger;
            }

            public override bool LessThan(AggregatedCategory arg1, AggregatedCategory arg2)
            {
                return merger.LeftGoesNow(arg2.ordinal, arg2.value, arg1.ordinal, arg1.value);
            }
        }

        private class ResultNodeHeap : PriorityQueue<FacetResultNode>
        {
            private ACComparator merger;
            public ResultNodeHeap(int size, ACComparator merger)
                : base(size)
            {
                this.merger = merger;
            }

            public override bool LessThan(FacetResultNode arg1, FacetResultNode arg2)
            {
                return merger.LeftGoesNow(arg2.ordinal, arg2.value, arg1.ordinal, arg1.value);
            }
        }

        private ACComparator GetSuitableACComparator()
        {
            if (facetRequest.SortOrderValue == FacetRequest.SortOrder.ASCENDING)
            {
                return new AscValueACComparator();
            }
            else
            {
                return new DescValueACComparator();
            }
        }

        private abstract class ACComparator
        {
            internal ACComparator()
            {
            }

            protected internal abstract bool LeftGoesNow(int ord1, double val1, int ord2, double val2);
        }

        private sealed class AscValueACComparator : ACComparator
        {
            internal AscValueACComparator()
            {
            }

            protected internal override bool LeftGoesNow(int ord1, double val1, int ord2, double val2)
            {
                return (val1 == val2) ? (ord1 < ord2) : (val1 < val2);
            }
        }

        private sealed class DescValueACComparator : ACComparator
        {
            internal DescValueACComparator()
            {
            }

            protected internal override bool LeftGoesNow(int ord1, double val1, int ord2, double val2)
            {
                return (val1 == val2) ? (ord1 > ord2) : (val1 > val2);
            }
        }

        public class IntermediateFacetResultWithHash : IIntermediateFacetResult
        {
            protected internal IntToObjectMap<AACO> mapToAACOs;
            internal FacetRequest facetRequest;
            internal bool isRootNodeIncluded;
            internal double rootNodeValue;
            internal int totalNumOfFacetsConsidered;

            public IntermediateFacetResultWithHash(FacetRequest facetReq, IntToObjectMap<AACO> mapToAACOs)
            {
                this.mapToAACOs = mapToAACOs;
                this.facetRequest = facetReq;
                this.isRootNodeIncluded = false;
                this.rootNodeValue = 0.0;
                this.totalNumOfFacetsConsidered = 0;
            }

            public FacetRequest FacetRequest
            {
                get
                {
                    return this.facetRequest;
                }
            }
        }

        private sealed class AggregatedCategory
        {
            internal int ordinal;
            internal double value;
            internal AggregatedCategory(int ord, double val)
            {
                this.ordinal = ord;
                this.value = val;
            }
        }

        public sealed class AACO
        {
            internal int[] ordinals;
            internal double[] values;
            internal AACO(int[] ords, double[] vals)
            {
                this.ordinals = ords;
                this.values = vals;
            }
        }

        public override void LabelResult(FacetResult facetResult)
        {
            if (facetResult == null)
            {
                return;
            }

            FacetResultNode rootNode = facetResult.FacetResultNode;
            RecursivelyLabel(rootNode, facetRequest.NumLabel);
        }

        private void RecursivelyLabel(FacetResultNode node, int numToLabel)
        {
            if (node == null)
            {
                return;
            }

            node.label = taxonomyReader.GetPath(node.ordinal);
            int numLabeled = 0;
            foreach (FacetResultNode frn in node.subResults)
            {
                RecursivelyLabel(frn, numToLabel);
                if (++numLabeled >= numToLabel)
                {
                    return;
                }
            }
        }

        public override FacetResult RearrangeFacetResult(FacetResult facetResult)
        {
            PriorityQueue<FacetResultNode> nodesHeap = new ResultNodeHeap(this.facetRequest.numResults, this.GetSuitableACComparator());
            FacetResultNode topFrn = facetResult.FacetResultNode;
            RearrangeChilrenOfNode(topFrn, nodesHeap);
            return facetResult;
        }

        private void RearrangeChilrenOfNode(FacetResultNode node, PriorityQueue<FacetResultNode> nodesHeap)
        {
            nodesHeap.Clear();
            foreach (FacetResultNode frn in node.subResults)
            {
                nodesHeap.Add(frn);
            }

            int size = nodesHeap.Size;
            List<FacetResultNode> subResults = new List<FacetResultNode>(size);
            while (nodesHeap.Size > 0)
            {
                subResults.Insert(0, nodesHeap.Pop());
            }

            node.subResults = subResults;
            foreach (FacetResultNode frn in node.subResults)
            {
                RearrangeChilrenOfNode(frn, nodesHeap);
            }
        }

        public override FacetResult RenderFacetResult(IIntermediateFacetResult tmpResult)
        {
            IntermediateFacetResultWithHash tmp = (IntermediateFacetResultWithHash)tmpResult;
            int ordinal = this.taxonomyReader.GetOrdinal(this.facetRequest.categoryPath);
            if ((tmp == null) || (ordinal == TaxonomyReader.INVALID_ORDINAL))
            {
                return null;
            }

            double value = Double.NaN;
            if (tmp.isRootNodeIncluded)
            {
                value = tmp.rootNodeValue;
            }

            FacetResultNode root = GenerateNode(ordinal, value, tmp.mapToAACOs);
            return new FacetResult(tmp.facetRequest, root, tmp.totalNumOfFacetsConsidered);
        }

        private FacetResultNode GenerateNode(int ordinal, double val, IntToObjectMap<AACO> mapToAACOs)
        {
            FacetResultNode node = new FacetResultNode(ordinal, val);
            AACO aaco = mapToAACOs.Get(ordinal);
            if (null == aaco)
            {
                return node;
            }

            List<FacetResultNode> list = new List<FacetResultNode>();
            for (int i = 0; i < aaco.ordinals.Length; i++)
            {
                list.Add(GenerateNode(aaco.ordinals[i], aaco.values[i], mapToAACOs));
            }

            node.subResults = list;
            return node;
        }
    }
}
