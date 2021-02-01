---
uid: Lucene.Net.Demo.Facet.SimpleSortedSetFacetsExample
example: [*content]
---

```
using Lucene.Net.Analysis.Core;
using Lucene.Net.Documents;
using Lucene.Net.Facet;
using Lucene.Net.Facet.SortedSet;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;

namespace Lucene.Net.Demo.Facet
{
    /// <summary>
    /// Shows simple usage of faceted indexing and search
    /// using <see cref="SortedSetDocValuesFacetField"/> and 
    /// <see cref="SortedSetDocValuesFacetCounts"/>.
    /// </summary>
    public class SimpleSortedSetFacetsExample
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
        private readonly FacetsConfig config = new FacetsConfig();

        /// <summary>Build the example index.</summary>
        private void Index()
        {
            using IndexWriter indexWriter = new IndexWriter(indexDir,
                new IndexWriterConfig(EXAMPLE_VERSION,
                new WhitespaceAnalyzer(EXAMPLE_VERSION)));

            indexWriter.AddDocument(config.Build(new Document
            {
                new SortedSetDocValuesFacetField("Author", "Bob"),
                new SortedSetDocValuesFacetField("Publish Year", "2010")
            }));

            indexWriter.AddDocument(config.Build(new Document
            {
                new SortedSetDocValuesFacetField("Author", "Lisa"),
                new SortedSetDocValuesFacetField("Publish Year", "2010")
            }));

            indexWriter.AddDocument(config.Build(new Document
            {
                new SortedSetDocValuesFacetField("Author", "Lisa"),
                new SortedSetDocValuesFacetField("Publish Year", "2012")
            }));

            indexWriter.AddDocument(config.Build(new Document
            {
                new SortedSetDocValuesFacetField("Author", "Susan"),
                new SortedSetDocValuesFacetField("Publish Year", "2012")
            }));

            indexWriter.AddDocument(config.Build(new Document
            {
                new SortedSetDocValuesFacetField("Author", "Frank"),
                new SortedSetDocValuesFacetField("Publish Year", "1999")
            }));
        }

        /// <summary>User runs a query and counts facets.</summary>
        private IList<FacetResult> Search()
        {
            using DirectoryReader indexReader = DirectoryReader.Open(indexDir);
            IndexSearcher searcher = new IndexSearcher(indexReader);
            SortedSetDocValuesReaderState state = new DefaultSortedSetDocValuesReaderState(indexReader);

            // Aggregatses the facet counts
            FacetsCollector fc = new FacetsCollector();

            // MatchAllDocsQuery is for "browsing" (counts facets
            // for all non-deleted docs in the index); normally
            // you'd use a "normal" query:
            FacetsCollector.Search(searcher, new MatchAllDocsQuery(), 10, fc);

            // Retrieve results
            Facets facets = new SortedSetDocValuesFacetCounts(state, fc);

            IList<FacetResult> results = new List<FacetResult>
            {
                facets.GetTopChildren(10, "Author"),
                facets.GetTopChildren(10, "Publish Year")
            };

            return results;
        }

        /// <summary>User drills down on 'Publish Year/2010'.</summary>
        private FacetResult DrillDown()
        {
            using DirectoryReader indexReader = DirectoryReader.Open(indexDir);
            IndexSearcher searcher = new IndexSearcher(indexReader);
            SortedSetDocValuesReaderState state = new DefaultSortedSetDocValuesReaderState(indexReader);

            // Now user drills down on Publish Year/2010:
            DrillDownQuery q = new DrillDownQuery(config);
            q.Add("Publish Year", "2010");
            FacetsCollector fc = new FacetsCollector();
            FacetsCollector.Search(searcher, q, 10, fc);

            // Retrieve results
            Facets facets = new SortedSetDocValuesFacetCounts(state, fc);
            FacetResult result = facets.GetTopChildren(10, "Author");

            return result;
        }

        /// <summary>Runs the search example.</summary>
        public IList<FacetResult> RunSearch()
        {
            Index();
            return Search();
        }

        /// <summary>Runs the drill-down example.</summary>
        public FacetResult RunDrillDown()
        {
            Index();
            return DrillDown();
        }

        /// <summary>Runs the search and drill-down examples and prints the results.</summary>
        public static void Main(string[] args)
        {
            Console.WriteLine("Facet counting example:");
            Console.WriteLine("-----------------------");
            SimpleSortedSetFacetsExample example = new SimpleSortedSetFacetsExample();
            IList<FacetResult> results = example.RunSearch();
            Console.WriteLine("Author: " + results[0]);
            Console.WriteLine("Publish Year: " + results[0]);

            Console.WriteLine();
            Console.WriteLine("Facet drill-down example (Publish Year/2010):");
            Console.WriteLine("---------------------------------------------");
            Console.WriteLine("Author: " + example.RunDrillDown());
        }
    }
}
```
