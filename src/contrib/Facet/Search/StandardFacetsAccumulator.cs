using Lucene.Net.Facet.Complements;
using Lucene.Net.Facet.Params;
using Lucene.Net.Facet.Partitions;
using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Facet.Util;
using Lucene.Net.Index;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace Lucene.Net.Facet.Search
{
    public class StandardFacetsAccumulator : FacetsAccumulator
    {
        //private static readonly Logger logger = Logger.GetLogger(typeof(StandardFacetsAccumulator).GetName());
        public static readonly double DEFAULT_COMPLEMENT_THRESHOLD = 0.6;
        public static readonly double DISABLE_COMPLEMENT = double.PositiveInfinity;
        public static readonly double FORCE_COMPLEMENT = 0;
        protected int partitionSize;
        protected int maxPartitions;
        protected bool isUsingComplements;
        private TotalFacetCounts totalFacetCounts;
        private Object accumulateGuard;
        private double complementThreshold;

        public StandardFacetsAccumulator(FacetSearchParams searchParams, IndexReader indexReader, TaxonomyReader taxonomyReader)
            : this(searchParams, indexReader, taxonomyReader, new FacetArrays(PartitionsUtils.PartitionSize(searchParams.indexingParams, taxonomyReader)))
        {
        }

        public StandardFacetsAccumulator(FacetSearchParams searchParams, IndexReader indexReader, TaxonomyReader taxonomyReader, FacetArrays facetArrays)
            : base(searchParams, indexReader, taxonomyReader, facetArrays)
        {
            isUsingComplements = false;
            partitionSize = PartitionsUtils.PartitionSize(searchParams.indexingParams, taxonomyReader);
            maxPartitions = (int)Math.Ceiling(this.taxonomyReader.Size / (double)partitionSize);
            accumulateGuard = new Object();
        }

        public virtual List<FacetResult> Accumulate(IScoredDocIDs docids)
        {
            lock (accumulateGuard)
            {
                isUsingComplements = ShouldComplement(docids);
                if (isUsingComplements)
                {
                    try
                    {
                        totalFacetCounts = TotalFacetCountsCache.GetSingleton().GetTotalCounts(indexReader, taxonomyReader, searchParams.indexingParams);
                        if (totalFacetCounts != null)
                        {
                            docids = ScoredDocIdsUtils.GetComplementSet(docids, indexReader);
                        }
                        else
                        {
                            isUsingComplements = false;
                        }
                    }
                    catch (NotSupportedException e)
                    {
                        Debug.WriteLine("IndexReader used does not support completents: {0}", e);
                        
                        isUsingComplements = false;
                    }
                    catch (IOException e)
                    {
                        Debug.WriteLine("Failed to load/calculate total counts (complement counting disabled): {0}", e);

                        isUsingComplements = false;
                    }
                    catch (Exception e)
                    {
                        throw new IOException("PANIC: Got unexpected exception while trying to get/calculate total counts", e);
                    }
                }

                docids = ActualDocsToAccumulate(docids);
                HashMap<FacetRequest, IIntermediateFacetResult> fr2tmpRes = new HashMap<FacetRequest, IIntermediateFacetResult>();
                try
                {
                    for (int part = 0; part < maxPartitions; part++)
                    {
                        FillArraysForPartition(docids, facetArrays, part);
                        int offset = part * partitionSize;
                        HashSet<FacetRequest> handledRequests = new HashSet<FacetRequest>();
                        foreach (FacetRequest fr in searchParams.facetRequests)
                        {
                            if (handledRequests.Add(fr))
                            {
                                PartitionsFacetResultsHandler frHndlr = (PartitionsFacetResultsHandler)CreateFacetResultsHandler(fr);
                                IIntermediateFacetResult res4fr = frHndlr.FetchPartitionResult(offset);
                                IIntermediateFacetResult oldRes = fr2tmpRes[fr];
                                if (oldRes != null)
                                {
                                    res4fr = frHndlr.MergeResults(oldRes, res4fr);
                                }

                                fr2tmpRes[fr] = res4fr;
                            }
                        }
                    }
                }
                finally
                {
                    facetArrays.Free();
                }

                List<FacetResult> res = new List<FacetResult>();
                foreach (FacetRequest fr in searchParams.facetRequests)
                {
                    PartitionsFacetResultsHandler frHndlr = (PartitionsFacetResultsHandler)CreateFacetResultsHandler(fr);
                    IIntermediateFacetResult tmpResult = fr2tmpRes[fr];
                    if (tmpResult == null)
                    {
                        res.Add(EmptyResult(taxonomyReader.GetOrdinal(fr.categoryPath), fr));
                        continue;
                    }

                    FacetResult facetRes = frHndlr.RenderFacetResult(tmpResult);
                    frHndlr.LabelResult(facetRes);
                    res.Add(facetRes);
                }

                return res;
            }
        }

        protected virtual bool MayComplement()
        {
            foreach (FacetRequest freq in searchParams.facetRequests)
            {
                if (!(freq is CountFacetRequest))
                {
                    return false;
                }
            }

            return true;
        }

        protected override FacetResultsHandler CreateFacetResultsHandler(FacetRequest fr)
        {
            if (fr.ResultModeValue == FacetRequest.ResultMode.PER_NODE_IN_TREE)
            {
                return new TopKInEachNodeHandler(taxonomyReader, fr, facetArrays);
            }
            else
            {
                return new TopKFacetResultsHandler(taxonomyReader, fr, facetArrays);
            }
        }

        protected virtual IScoredDocIDs ActualDocsToAccumulate(IScoredDocIDs docids)
        {
            return docids;
        }

        protected virtual bool ShouldComplement(IScoredDocIDs docids)
        {
            return MayComplement() && (docids.Size > indexReader.NumDocs * ComplementThreshold);
        }

        private void FillArraysForPartition(IScoredDocIDs docids, FacetArrays facetArrays, int partition)
        {
            if (isUsingComplements)
            {
                InitArraysByTotalCounts(facetArrays, partition, docids.Size);
            }
            else
            {
                facetArrays.Free();
            }

            HashMap<ICategoryListIterator, IAggregator> categoryLists = GetCategoryListMap(facetArrays, partition);
            IntsRef ordinals = new IntsRef(32);
            foreach (var entry in categoryLists)
            {
                IScoredDocIDsIterator iterator = docids.Iterator();
                ICategoryListIterator categoryListIter = entry.Key;
                IAggregator aggregator = entry.Value;
                IEnumerator<AtomicReaderContext> contexts = indexReader.Leaves.GetEnumerator();
                AtomicReaderContext current = null;
                int maxDoc = -1;
                while (iterator.Next())
                {
                    int docID = iterator.DocID;
                    if (docID >= maxDoc)
                    {
                        bool iteratorDone = false;
                        do
                        {
                            if (!contexts.MoveNext())
                            {
                                throw new Exception(@"ScoredDocIDs contains documents outside this reader's segments !?");
                            }

                            current = contexts.Current;
                            maxDoc = current.docBase + current.Reader.MaxDoc;
                            if (docID < maxDoc)
                            {
                                bool validSegment = categoryListIter.SetNextReader(current);
                                validSegment &= aggregator.SetNextReader(current);
                                if (!validSegment)
                                {
                                    while (docID < maxDoc && iterator.Next())
                                    {
                                        docID = iterator.DocID;
                                    }

                                    if (docID < maxDoc)
                                    {
                                        iteratorDone = true;
                                    }
                                }
                            }
                        }
                        while (docID >= maxDoc);
                        if (iteratorDone)
                        {
                            break;
                        }
                    }

                    docID -= current.docBase;
                    categoryListIter.GetOrdinals(docID, ordinals);
                    if (ordinals.length == 0)
                    {
                        continue;
                    }

                    aggregator.Aggregate(docID, iterator.Score, ordinals);
                }
            }
        }

        private void InitArraysByTotalCounts(FacetArrays facetArrays, int partition, int nAccumulatedDocs)
        {
            int[] intArray = facetArrays.GetIntArray();
            totalFacetCounts.FillTotalCountsForPartition(intArray, partition);
            double totalCountsFactor = TotalCountsFactor;
            if (totalCountsFactor < 1.0)
            {
                int delta = nAccumulatedDocs + 1;
                for (int i = 0; i < intArray.Length; i++)
                {
                    intArray[i] *= (int)totalCountsFactor;
                    intArray[i] += delta;
                }
            }
        }

        protected virtual double TotalCountsFactor
        {
            get
            {
                return 1;
            }
        }

        protected virtual HashMap<ICategoryListIterator, IAggregator> GetCategoryListMap(FacetArrays facetArrays, int partition)
        {
            HashMap<ICategoryListIterator, IAggregator> categoryLists = new HashMap<ICategoryListIterator, IAggregator>();
            FacetIndexingParams indexingParams = searchParams.indexingParams;
            foreach (FacetRequest facetRequest in searchParams.facetRequests)
            {
                IAggregator categoryAggregator = facetRequest.CreateAggregator(isUsingComplements, facetArrays, taxonomyReader);
                ICategoryListIterator cli = indexingParams.GetCategoryListParams(facetRequest.categoryPath).CreateCategoryListIterator(partition);
                IAggregator old = categoryLists[cli] = categoryAggregator;
                if (old != null && !old.Equals(categoryAggregator))
                {
                    throw new Exception(@"Overriding existing category list with different aggregator");
                }
            }

            return categoryLists;
        }

        public override List<FacetResult> Accumulate(List<FacetsCollector.MatchingDocs> matchingDocs)
        {
            return Accumulate(new MatchingDocsAsScoredDocIDs(matchingDocs));
        }

        public virtual double ComplementThreshold
        {
            get
            {
                return complementThreshold;
            }
            set
            {
                this.complementThreshold = value;
            }
        }
        
        public virtual bool IsUsingComplements
        {
            get
            {
                return isUsingComplements;
            }
        }
    }
}
