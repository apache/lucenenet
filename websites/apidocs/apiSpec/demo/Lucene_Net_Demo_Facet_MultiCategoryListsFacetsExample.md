---
uid: Lucene.Net.Demo.Facet.MultiCategoryListsFacetsExample
example: [*content]
---

```
using Lucene.Net.Analysis.Core;
using Lucene.Net.Documents;
using Lucene.Net.Facet;
using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Facet.Taxonomy.Directory;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;

namespace Lucene.Net.Demo.Facet
{
    /// <summary>
    /// Demonstrates indexing categories into different indexed fields.
    /// </summary>
    public class MultiCategoryListsFacetsExample
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
        private readonly Directory taxoDir = new RAMDirectory();
        private readonly FacetsConfig config = new FacetsConfig();

        /// <summary>Creates a new instance and populates the catetory list params mapping.</summary>
        public MultiCategoryListsFacetsExample()
        {
            config.SetIndexFieldName("Author", "author");
            config.SetIndexFieldName("Publish Date", "pubdate");
            config.SetHierarchical("Publish Date", true);
        }

        /// <summary>Build the example index.</summary>
        private void Index()
        {
            using IndexWriter indexWriter = new IndexWriter(indexDir, new IndexWriterConfig(EXAMPLE_VERSION,
                new WhitespaceAnalyzer(EXAMPLE_VERSION)));
            // Writes facet ords to a separate directory from the main index
            using DirectoryTaxonomyWriter taxoWriter = new DirectoryTaxonomyWriter(taxoDir);

            indexWriter.AddDocument(config.Build(taxoWriter, new Document
            {
                new FacetField("Author", "Bob"),
                new FacetField("Publish Date", "2010", "10", "15")
            }));

            indexWriter.AddDocument(config.Build(taxoWriter, new Document
            {
                new FacetField("Author", "Lisa"),
                new FacetField("Publish Date", "2010", "10", "20")
            }));

            indexWriter.AddDocument(config.Build(taxoWriter, new Document
            {
                new FacetField("Author", "Lisa"),
                new FacetField("Publish Date", "2012", "1", "1")
            }));

            indexWriter.AddDocument(config.Build(taxoWriter, new Document
            {
                new FacetField("Author", "Susan"),
                new FacetField("Publish Date", "2012", "1", "7")
            }));

            indexWriter.AddDocument(config.Build(taxoWriter, new Document
            {
                new FacetField("Author", "Frank"),
                new FacetField("Publish Date", "1999", "5", "5")
            }));
        }

        /// <summary>User runs a query and counts facets.</summary>
        private IList<FacetResult> Search()
        {
            IList<FacetResult> results = new List<FacetResult>();

            using DirectoryReader indexReader = DirectoryReader.Open(indexDir);
            using TaxonomyReader taxoReader = new DirectoryTaxonomyReader(taxoDir);

            IndexSearcher searcher = new IndexSearcher(indexReader);
            FacetsCollector fc = new FacetsCollector();

            // MatchAllDocsQuery is for "browsing" (counts facets
            // for all non-deleted docs in the index); normally
            // you'd use a "normal" query:
            FacetsCollector.Search(searcher, new MatchAllDocsQuery(), 10, fc);

            // Retrieve results

            // Count both "Publish Date" and "Author" dimensions
            Facets author = new FastTaxonomyFacetCounts("author", taxoReader, config, fc);
            results.Add(author.GetTopChildren(10, "Author"));

            Facets pubDate = new FastTaxonomyFacetCounts("pubdate", taxoReader, config, fc);
            results.Add(pubDate.GetTopChildren(10, "Publish Date"));

            return results;
        }

        /// <summary>Runs the search example.</summary>
        public IList<FacetResult> RunSearch()
        {
            Index();
            return Search();
        }

        /// <summary>Runs the search example and prints the results.</summary>
        public static void Main(string[] args)
        {
            Console.WriteLine("Facet counting over multiple category lists example:");
            Console.WriteLine("-----------------------");
            IList<FacetResult> results = new MultiCategoryListsFacetsExample().RunSearch();

            Console.WriteLine("Author: " + results[0]);
            Console.WriteLine("Publish Date: " + results[1]);
        }
    }
}
```
