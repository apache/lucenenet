using Lucene.Net.Facet.Params;
using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Index;
using Lucene.Net.Support;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace Lucene.Net.Facet.Complements
{
    public sealed class TotalFacetCountsCache
    {
        public static readonly int DEFAULT_CACHE_SIZE = 2;
        private static readonly TotalFacetCountsCache singleton = new TotalFacetCountsCache();

        public static TotalFacetCountsCache GetSingleton()
        {
            return singleton;
        }

        private ConcurrentHashMap<TFCKey, TotalFacetCounts> cache = new ConcurrentHashMap<TFCKey, TotalFacetCounts>();
        private LinkedList<TFCKey> lruKeys = new LinkedList<TFCKey>(); // .NET Port: this collection is not concurrent, so we have to lock around it
        private int maxCacheSize = DEFAULT_CACHE_SIZE;

        private TotalFacetCountsCache()
        {
        }

        public TotalFacetCounts GetTotalCounts(IndexReader indexReader, TaxonomyReader taxonomy, FacetIndexingParams facetIndexingParams)
        {
            TFCKey key = new TFCKey(indexReader, taxonomy, facetIndexingParams);
            TotalFacetCounts tfc = cache[key];
            if (tfc != null)
            {
                MarkRecentlyUsed(key);
                return tfc;
            }

            return ComputeAndCache(key);
        }

        private void MarkRecentlyUsed(TFCKey key)
        {
            lock (this)
            {
                lruKeys.Remove(key);
                lruKeys.AddLast(key);
            }
        }

        private void TrimCache()
        {
            lock (this)
            {
                while (cache.Count > maxCacheSize)
                {
                    TFCKey key;

                    if (lruKeys.Count > 0)
                    {
                        key = lruKeys.First.Value;
                        lruKeys.RemoveFirst();
                    }
                    else
                    {
                        key = cache.Keys.FirstOrDefault();
                    }

                    cache.Remove(key);
                }
            }
        }

        private TotalFacetCounts ComputeAndCache(TFCKey key)
        {
            lock (this)
            {
                TotalFacetCounts tfc = cache[key];
                if (tfc == null)
                {
                    tfc = TotalFacetCounts.Compute(key.indexReader, key.taxonomy, key.facetIndexingParams);
                    lruKeys.AddLast(key);
                    cache[key] = tfc;
                    TrimCache();
                }

                return tfc;
            }
        }

        public void Load(FileInfo inputFile, IndexReader indexReader, TaxonomyReader taxonomy, FacetIndexingParams facetIndexingParams)
        {
            lock (this)
            {
                if (!inputFile.Exists)
                {
                    throw new ArgumentException(@"Exepecting an existing readable file: " + inputFile);
                }

                TFCKey key = new TFCKey(indexReader, taxonomy, facetIndexingParams);
                TotalFacetCounts tfc = TotalFacetCounts.LoadFromFile(inputFile, taxonomy, facetIndexingParams);
                cache[key] = tfc;
                TrimCache();
                MarkRecentlyUsed(key);
            }
        }

        public void Store(FileInfo outputFile, IndexReader indexReader, TaxonomyReader taxonomy, FacetIndexingParams facetIndexingParams)
        {
            TotalFacetCounts tfc = GetTotalCounts(indexReader, taxonomy, facetIndexingParams);
            TotalFacetCounts.StoreToFile(outputFile, tfc);
        }

        private class TFCKey
        {
            internal readonly IndexReader indexReader;
            internal readonly TaxonomyReader taxonomy;
            private readonly IEnumerable<CategoryListParams> clps;
            private readonly int hashCode;
            private readonly int nDels;
            internal readonly FacetIndexingParams facetIndexingParams;

            public TFCKey(IndexReader indexReader, TaxonomyReader taxonomy, FacetIndexingParams facetIndexingParams)
            {
                this.indexReader = indexReader;
                this.taxonomy = taxonomy;
                this.facetIndexingParams = facetIndexingParams;
                this.clps = facetIndexingParams.AllCategoryListParams;
                this.nDels = indexReader.NumDeletedDocs;
                hashCode = indexReader.GetHashCode() ^ taxonomy.GetHashCode();
            }

            public override int GetHashCode()
            {
                return hashCode;
            }

            public override bool Equals(Object other)
            {
                TFCKey o = (TFCKey)other;
                if (indexReader != o.indexReader || taxonomy != o.taxonomy || nDels != o.nDels)
                {
                    return false;
                }

                IEnumerator<CategoryListParams> it1 = clps.GetEnumerator();
                IEnumerator<CategoryListParams> it2 = o.clps.GetEnumerator();
                while (it1.MoveNext() && it2.MoveNext())
                {
                    if (!it1.Current.Equals(it2.Current))
                    {
                        return false;
                    }
                }

                return it1.MoveNext() == it2.MoveNext();
            }
        }

        public void Clear()
        {
            lock (this)
            {
                cache.Clear();
                lruKeys.Clear();
            }
        }

        public int GetCacheSize()
        {
            return maxCacheSize;
        }

        public void SetCacheSize(int size)
        {
            if (size < 1)
                size = 1;
            int origSize = maxCacheSize;
            maxCacheSize = size;
            if (maxCacheSize < origSize)
            {
                TrimCache();
            }
        }
    }
}
