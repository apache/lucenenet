/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

// Add NuGet References:

// Lucene.Net.Analysis.Common
// Lucene.Net.Facet

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
    /// Shows simple usage of faceted indexing and search.
    /// </summary>
    public class SimpleFacetsExample
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

        /// <summary>Constructor</summary>
        public SimpleFacetsExample()
        {
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
        private IList<FacetResult> FacetsWithSearch()
        {
            using DirectoryReader indexReader = DirectoryReader.Open(indexDir);
            using TaxonomyReader taxoReader = new DirectoryTaxonomyReader(taxoDir);
            IndexSearcher searcher = new IndexSearcher(indexReader);

            FacetsCollector fc = new FacetsCollector();

            // MatchAllDocsQuery is for "browsing" (counts facets
            // for all non-deleted docs in the index); normally
            // you'd use a "normal" query:
            FacetsCollector.Search(searcher, new MatchAllDocsQuery(), 10, fc);

            Facets facets = new FastTaxonomyFacetCounts(taxoReader, config, fc);

            // Retrieve results
            IList<FacetResult> results = new List<FacetResult>
            {
                // Count both "Publish Date" and "Author" dimensions
                facets.GetTopChildren(10, "Author"),
                facets.GetTopChildren(10, "Publish Date")
            };

            return results;
        }

        /// <summary>User runs a query and counts facets only without collecting the matching documents.</summary>
        private IList<FacetResult> FacetsOnly()
        {
            using DirectoryReader indexReader = DirectoryReader.Open(indexDir);
            using TaxonomyReader taxoReader = new DirectoryTaxonomyReader(taxoDir);
            IndexSearcher searcher = new IndexSearcher(indexReader);

            FacetsCollector fc = new FacetsCollector();

            // MatchAllDocsQuery is for "browsing" (counts facets
            // for all non-deleted docs in the index); normally
            // you'd use a "normal" query:
            searcher.Search(new MatchAllDocsQuery(), null /*Filter */, fc);

            Facets facets = new FastTaxonomyFacetCounts(taxoReader, config, fc);

            // Retrieve results
            IList<FacetResult> results = new List<FacetResult>
            {
                // Count both "Publish Date" and "Author" dimensions
                facets.GetTopChildren(10, "Author"),
                facets.GetTopChildren(10, "Publish Date")
            };

            return results;
        }

        /// <summary>
        /// User drills down on 'Publish Date/2010', and we
        /// return facets for 'Author'
        /// </summary>
        private FacetResult DrillDown()
        {
            using DirectoryReader indexReader = DirectoryReader.Open(indexDir);
            using TaxonomyReader taxoReader = new DirectoryTaxonomyReader(taxoDir);
            IndexSearcher searcher = new IndexSearcher(indexReader);

            // Passing no baseQuery means we drill down on all
            // documents ("browse only"):
            DrillDownQuery q = new DrillDownQuery(config);

            // Now user drills down on Publish Date/2010:
            q.Add("Publish Date", "2010");
            FacetsCollector fc = new FacetsCollector();
            FacetsCollector.Search(searcher, q, 10, fc);

            // Retrieve results
            Facets facets = new FastTaxonomyFacetCounts(taxoReader, config, fc);
            FacetResult result = facets.GetTopChildren(10, "Author");

            return result;
        }

        /// <summary>
        /// User drills down on 'Publish Date/2010', and we
        /// return facets for both 'Publish Date' and 'Author',
        /// using DrillSideways.
        /// </summary>
        private IList<FacetResult> DrillSideways()
        {
            using DirectoryReader indexReader = DirectoryReader.Open(indexDir);
            using TaxonomyReader taxoReader = new DirectoryTaxonomyReader(taxoDir);
            IndexSearcher searcher = new IndexSearcher(indexReader);

            // Passing no baseQuery means we drill down on all
            // documents ("browse only"):
            DrillDownQuery q = new DrillDownQuery(config);

            // Now user drills down on Publish Date/2010:
            q.Add("Publish Date", "2010");

            DrillSideways ds = new DrillSideways(searcher, config, taxoReader);
            DrillSidewaysResult result = ds.Search(q, 10);

            // Retrieve results
            IList<FacetResult> facets = result.Facets.GetAllDims(10);

            return facets;
        }

        /// <summary>Runs the search example.</summary>
        public IList<FacetResult> RunFacetOnly()
        {
            Index();
            return FacetsOnly();
        }

        /// <summary>Runs the search example.</summary>
        public IList<FacetResult> RunSearch()
        {
            Index();
            return FacetsWithSearch();
        }

        /// <summary>Runs the drill-down example.</summary>
        public FacetResult RunDrillDown()
        {
            Index();
            return DrillDown();
        }

        /// <summary>Runs the drill-sideways example.</summary>
        public IList<FacetResult> RunDrillSideways()
        {
            Index();
            return DrillSideways();
        }

        /// <summary>Runs the search and drill-down examples and prints the results.</summary>
        public static void Main(string[] args)
        {
            Console.WriteLine("Facet counting example:");
            Console.WriteLine("-----------------------");
            SimpleFacetsExample example1 = new SimpleFacetsExample();
            IList<FacetResult> results1 = example1.RunFacetOnly();
            Console.WriteLine("Author: " + results1[0]);
            Console.WriteLine("Publish Date: " + results1[1]);

            Console.WriteLine("Facet counting example (combined facets and search):");
            Console.WriteLine("-----------------------");
            SimpleFacetsExample example = new SimpleFacetsExample();
            IList<FacetResult> results = example.RunSearch();
            Console.WriteLine("Author: " + results[0]);
            Console.WriteLine("Publish Date: " + results[1]);

            Console.WriteLine();
            Console.WriteLine("Facet drill-down example (Publish Date/2010):");
            Console.WriteLine("---------------------------------------------");
            Console.WriteLine("Author: " + example.RunDrillDown());

            Console.WriteLine();
            Console.WriteLine("Facet drill-sideways example (Publish Date/2010):");
            Console.WriteLine("---------------------------------------------");
            foreach (FacetResult result in example.RunDrillSideways())
            {
                Console.WriteLine(result);
            }
        }
    }
}
