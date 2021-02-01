---
uid: Lucene.Net.Demo.Facet.AssociationsFacetsExample
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
    /// Shows example usage of category associations.
    /// </summary>
    public class AssociationsFacetsExample
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
        private readonly FacetsConfig config;

        /// <summary>Empty constructor</summary>
        public AssociationsFacetsExample()
        {
            config = new FacetsConfig();
            config.SetMultiValued("tags", true);
            config.SetIndexFieldName("tags", "$tags");
            config.SetMultiValued("genre", true);
            config.SetIndexFieldName("genre", "$genre");
        }

        /// <summary>Build the example index.</summary>
        private void Index()
        {
            IndexWriterConfig iwc = new IndexWriterConfig(EXAMPLE_VERSION,
                                                  new WhitespaceAnalyzer(EXAMPLE_VERSION));
            using IndexWriter indexWriter = new IndexWriter(indexDir, iwc);

            // Writes facet ords to a separate directory from the main index
            using DirectoryTaxonomyWriter taxoWriter = new DirectoryTaxonomyWriter(taxoDir);
            Document doc = new Document();
            // 3 occurrences for tag 'lucene'

            doc.AddInt32AssociationFacetField(3, "tags", "lucene");
            // 87% confidence level of genre 'computing'
            doc.AddSingleAssociationFacetField(0.87f, "genre", "computing");
            indexWriter.AddDocument(config.Build(taxoWriter, doc));

            doc = new Document();
            // 1 occurrence for tag 'lucene'
            doc.AddInt32AssociationFacetField(1, "tags", "lucene");
            // 2 occurrence for tag 'solr'
            doc.AddInt32AssociationFacetField(2, "tags", "solr");
            // 75% confidence level of genre 'computing'
            doc.AddSingleAssociationFacetField(0.75f, "genre", "computing");
            // 34% confidence level of genre 'software'
            doc.AddSingleAssociationFacetField(0.34f, "genre", "software");
            indexWriter.AddDocument(config.Build(taxoWriter, doc));
        }

        /// <summary>User runs a query and aggregates facets by summing their association values.</summary>
        private IList<FacetResult> SumAssociations()
        {
            using DirectoryReader indexReader = DirectoryReader.Open(indexDir);
            using TaxonomyReader taxoReader = new DirectoryTaxonomyReader(taxoDir);
            IndexSearcher searcher = new IndexSearcher(indexReader);

            FacetsCollector fc = new FacetsCollector();

            // MatchAllDocsQuery is for "browsing" (counts facets
            // for all non-deleted docs in the index); normally
            // you'd use a "normal" query:
            FacetsCollector.Search(searcher, new MatchAllDocsQuery(), 10, fc);

            Facets tags = new TaxonomyFacetSumInt32Associations("$tags", taxoReader, config, fc);
            Facets genre = new TaxonomyFacetSumSingleAssociations("$genre", taxoReader, config, fc);

            // Retrieve results
            IList<FacetResult> results = new List<FacetResult>
            {
                tags.GetTopChildren(10, "tags"),
                genre.GetTopChildren(10, "genre")
            };

            return results;
        }

        /// <summary>User drills down on 'tags/solr'.</summary>
        private FacetResult DrillDown()
        {
            using DirectoryReader indexReader = DirectoryReader.Open(indexDir);
            using TaxonomyReader taxoReader = new DirectoryTaxonomyReader(taxoDir);
            IndexSearcher searcher = new IndexSearcher(indexReader);

            // Passing no baseQuery means we drill down on all
            // documents ("browse only"):
            DrillDownQuery q = new DrillDownQuery(config);

            // Now user drills down on Publish Date/2010:
            q.Add("tags", "solr");
            FacetsCollector fc = new FacetsCollector();
            FacetsCollector.Search(searcher, q, 10, fc);

            // Retrieve results
            Facets facets = new TaxonomyFacetSumSingleAssociations("$genre", taxoReader, config, fc);
            FacetResult result = facets.GetTopChildren(10, "genre");

            return result;
        }

        /// <summary>Runs summing association example.</summary>
        public IList<FacetResult> RunSumAssociations()
        {
            Index();
            return SumAssociations();
        }

        /// <summary>Runs the drill-down example.</summary>
        public FacetResult RunDrillDown()
        {
            Index();
            return DrillDown();
        }


        /// <summary>Runs the sum int/float associations examples and prints the results.</summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Demo shows use of optional args argument")]
        public static void Main(string[] args)
        {
            Console.WriteLine("Sum associations example:");
            Console.WriteLine("-------------------------");
            IList<FacetResult> results = new AssociationsFacetsExample().RunSumAssociations();
            Console.WriteLine("tags: " + results[0]);
            Console.WriteLine("genre: " + results[1]);
        }
    }
}
```
