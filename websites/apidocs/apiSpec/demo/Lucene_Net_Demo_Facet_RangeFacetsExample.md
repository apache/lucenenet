---
uid: Lucene.Net.Demo.Facet.RangeFacetsExample
example: [*content]
---

```
using Lucene.Net.Analysis.Core;
using Lucene.Net.Documents;
using Lucene.Net.Facet;
using Lucene.Net.Facet.Range;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using System;

namespace Lucene.Net.Demo.Facet
{
    /// <summary>
    /// Shows simple usage of dynamic range faceting.
    /// </summary>
    public sealed class RangeFacetsExample : IDisposable
    {
        /// <summary>
        /// Using a constant for all functionality related to a specific index
        /// is the best strategy. This allows you to upgrade Lucene.Net first
        /// and plan the upgrade of the index binary format for a later time. 
        /// Once the index is upgraded, you simply need to update the constant 
        /// version and redeploy your application.
        /// </summary>
        private const LuceneVersion EXAMPLE_VERSION = LuceneVersion.LUCENE_48;

        private readonly Directory indexDir = new RAMDirectory();
        private IndexSearcher searcher;
        private readonly long nowSec = DateTime.Now.Ticks;

        internal readonly Int64Range PAST_HOUR;
        internal readonly Int64Range PAST_SIX_HOURS;
        internal readonly Int64Range PAST_DAY;

        /// <summary>Constructor</summary>
        public RangeFacetsExample()
        {
            PAST_HOUR = new Int64Range("Past hour", nowSec - 3600, true, nowSec, true);
            PAST_SIX_HOURS = new Int64Range("Past six hours", nowSec - 6 * 3600, true, nowSec, true);
            PAST_DAY = new Int64Range("Past day", nowSec - 24 * 3600, true, nowSec, true);
        }

        /// <summary>Build the example index.</summary>
        public void Index()
        {
            using IndexWriter indexWriter = new IndexWriter(indexDir, new IndexWriterConfig(EXAMPLE_VERSION,
                new WhitespaceAnalyzer(EXAMPLE_VERSION)));
            // Add documents with a fake timestamp, 1000 sec before
            // "now", 2000 sec before "now", ...:
            for (int i = 0; i < 100; i++)
            {
                Document doc = new Document();
                long then = nowSec - i * 1000;
                // Add as doc values field, so we can compute range facets:
                doc.Add(new NumericDocValuesField("timestamp", then));
                // Add as numeric field so we can drill-down:
                doc.Add(new Int64Field("timestamp", then, Field.Store.NO));
                indexWriter.AddDocument(doc);
            }

            // Open near-real-time searcher
            searcher = new IndexSearcher(DirectoryReader.Open(indexWriter, true));
        }

        private static FacetsConfig GetConfig()
        {
            return new FacetsConfig();
        }

        /// <summary>User runs a query and counts facets.</summary>
        public FacetResult Search()
        {
            // Aggregates the facet counts
            FacetsCollector fc = new FacetsCollector();

            // MatchAllDocsQuery is for "browsing" (counts facets
            // for all non-deleted docs in the index); normally
            // you'd use a "normal" query:
            FacetsCollector.Search(searcher, new MatchAllDocsQuery(), 10, fc);

            Facets facets = new Int64RangeFacetCounts("timestamp", fc,
                                                     PAST_HOUR,
                                                     PAST_SIX_HOURS,
                                                     PAST_DAY);
            return facets.GetTopChildren(10, "timestamp");
        }

        /// <summary>User drills down on the specified range.</summary>
        public TopDocs DrillDown(Int64Range range)
        {
            // Passing no baseQuery means we drill down on all
            // documents ("browse only"):
            DrillDownQuery q = new DrillDownQuery(GetConfig());

            q.Add("timestamp", NumericRangeQuery.NewInt64Range("timestamp", range.Min, range.Max, range.MinInclusive, range.MaxInclusive));

            return searcher.Search(q, 10);
        }

        public void Dispose()
        {
            searcher?.IndexReader?.Dispose();
            indexDir?.Dispose();
        }

        /// <summary>Runs the search and drill-down examples and prints the results.</summary>
        public static void Main(string[] args)
        {
            using RangeFacetsExample example = new RangeFacetsExample();
            example.Index();

            Console.WriteLine("Facet counting example:");
            Console.WriteLine("-----------------------");
            Console.WriteLine(example.Search());

            Console.WriteLine("\n");
            Console.WriteLine("Facet drill-down example (timestamp/Past six hours):");
            Console.WriteLine("---------------------------------------------");
            TopDocs hits = example.DrillDown(example.PAST_SIX_HOURS);
            Console.WriteLine(hits.TotalHits + " TotalHits");
        }
    }
}
```
