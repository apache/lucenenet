using Lucene.Net.Facet.Params;
using Lucene.Net.Facet.Search;
using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Facet.Util;
using Lucene.Net.Index;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Lucene.Net.Facet.Complements
{
    public class TotalFacetCounts
    {
        private int[][] totalCounts = null;
        private readonly TaxonomyReader taxonomy;
        private readonly FacetIndexingParams facetIndexingParams;
        private static int atomicGen4Test = 1;

        enum CreationType
        {
            Computed,
            Loaded
        }

        readonly int gen4test;
        readonly CreationType createType4test;

        private TotalFacetCounts(TaxonomyReader taxonomy, FacetIndexingParams facetIndexingParams, 
            int[][] counts, CreationType createType4Test)
        {
            this.taxonomy = taxonomy;
            this.facetIndexingParams = facetIndexingParams;
            this.totalCounts = counts;
            this.createType4test = createType4Test;
            this.gen4test = Interlocked.Increment(ref atomicGen4Test);
        }

        public virtual void FillTotalCountsForPartition(int[] partitionArray, int partition)
        {
            int partitionSize = partitionArray.Length;
            int[] countArray = totalCounts[partition];
            if (countArray == null)
            {
                countArray = new int[partitionSize];
                totalCounts[partition] = countArray;
            }

            int length = Math.Min(partitionSize, countArray.Length);
            Array.Copy(countArray, 0, partitionArray, 0, length);
        }

        public virtual int GetTotalCount(int ordinal)
        {
            int partition = PartitionsUtils.PartitionNumber(facetIndexingParams, ordinal);
            int offset = ordinal % PartitionsUtils.PartitionSize(facetIndexingParams, taxonomy);
            return totalCounts[partition][offset];
        }

        internal static TotalFacetCounts LoadFromFile(FileInfo inputFile, TaxonomyReader taxonomy, FacetIndexingParams facetIndexingParams)
        {
            var fstream = inputFile.OpenRead();
            var dis = new BinaryReader(fstream);
            try
            {
                int[][] counts = new int[dis.ReadInt32()][];
                for (int i = 0; i < counts.Length; i++)
                {
                    int size = dis.ReadInt32();
                    if (size < 0)
                    {
                        counts[i] = null;
                    }
                    else
                    {
                        counts[i] = new int[size];
                        for (int j = 0; j < size; j++)
                        {
                            counts[i][j] = dis.ReadInt32();
                        }
                    }
                }

                return new TotalFacetCounts(taxonomy, facetIndexingParams, counts, CreationType.Loaded);
            }
            finally
            {
                dis.Dispose();
                fstream.Dispose();
            }
        }

        internal static void StoreToFile(FileInfo outputFile, TotalFacetCounts tfc)
        {
            var fstream = outputFile.OpenWrite();
            var dos = new BinaryWriter(fstream);
            try
            {
                dos.Write(tfc.totalCounts.Length);
                foreach (int[] counts in tfc.totalCounts)
                {
                    if (counts == null)
                    {
                        dos.Write(-1);
                    }
                    else
                    {
                        dos.Write(counts.Length);
                        foreach (int i in counts)
                        {
                            dos.Write(i);
                        }
                    }
                }
            }
            finally
            {
                dos.Dispose();
                fstream.Dispose();
            }
        }

        private static readonly FacetRequest DUMMY_REQ = new CountFacetRequest(CategoryPath.EMPTY, 1);

        internal static TotalFacetCounts Compute(IndexReader indexReader, TaxonomyReader taxonomy, FacetIndexingParams facetIndexingParams)
        {
            int partitionSize = PartitionsUtils.PartitionSize(facetIndexingParams, taxonomy);

            int[][] counts = new int[(int)Math.Ceiling(taxonomy.Size / (float)partitionSize)][];

            // .NET Port: this is needed since we can't initialize jagged array sizes
            for (int i = 0; i < counts.Length; i++)
            {
                counts[i] = new int[partitionSize];
            }

            FacetSearchParams newSearchParams = new FacetSearchParams(facetIndexingParams, DUMMY_REQ);
            StandardFacetsAccumulator sfa = new AnonymousStandardFacetsAccumulator(newSearchParams, indexReader, taxonomy, counts, facetIndexingParams);
            sfa.ComplementThreshold = StandardFacetsAccumulator.DISABLE_COMPLEMENT;
            sfa.Accumulate(ScoredDocIdsUtils.CreateAllDocsScoredDocIDs(indexReader));
            return new TotalFacetCounts(taxonomy, facetIndexingParams, counts, CreationType.Computed);
        }

        private sealed class AnonymousStandardFacetsAccumulator : StandardFacetsAccumulator
        {
            public AnonymousStandardFacetsAccumulator(FacetSearchParams newSearchParams, IndexReader indexReader, TaxonomyReader taxonomyReader, int[][] counts, FacetIndexingParams facetIndexingParams)
                : base(newSearchParams, indexReader, taxonomyReader)
            {
                this.counts = counts;
                this.facetIndexingParams = facetIndexingParams;
            }

            private readonly int[][] counts;
            private readonly FacetIndexingParams facetIndexingParams;

            protected override HashMap<ICategoryListIterator, IAggregator> GetCategoryListMap(FacetArrays facetArrays, int partition)
            {
                IAggregator aggregator = new CountingAggregator(counts[partition]);
                HashMap<ICategoryListIterator, IAggregator> map = new HashMap<ICategoryListIterator, IAggregator>();
                foreach (CategoryListParams clp in facetIndexingParams.AllCategoryListParams)
                {
                    map[clp.CreateCategoryListIterator(partition)] = aggregator;
                }

                return map;
            }
        }
    }
}
