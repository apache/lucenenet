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
// Lucene.Net.Expressions
// Lucene.Net.Facet

using Lucene.Net.Analysis.Core;
using Lucene.Net.Documents;
using Lucene.Net.Expressions;
using Lucene.Net.Expressions.JS;
using Lucene.Net.Facet;
using Lucene.Net.Facet.Taxonomy;
using Lucene.Net.Facet.Taxonomy.Directory;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using System;

namespace Lucene.Net.Demo.Facet
{
    /// <summary>
    /// Shows facets aggregation by an expression.
    /// </summary>
    public class ExpressionAggregationFacetsExample
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

        /// <summary>Build the example index.</summary>
        private void Index()
        {
            using IndexWriter indexWriter = new IndexWriter(indexDir, new IndexWriterConfig(EXAMPLE_VERSION,
                new WhitespaceAnalyzer(EXAMPLE_VERSION)));
            // Writes facet ords to a separate directory from the main index
            using DirectoryTaxonomyWriter taxoWriter = new DirectoryTaxonomyWriter(taxoDir);

            indexWriter.AddDocument(config.Build(taxoWriter, new Document
                {
                    new TextField("c", "foo bar", Field.Store.NO),
                    new NumericDocValuesField("popularity", 5L),
                    new FacetField("A", "B")
                }));

            indexWriter.AddDocument(config.Build(taxoWriter, new Document
                {
                    new TextField("c", "foo foo bar", Field.Store.NO),
                    new NumericDocValuesField("popularity", 3L),
                    new FacetField("A", "C")
                }));
        }

        /// <summary>User runs a query and aggregates facets.</summary>
        private FacetResult Search()
        {
            using DirectoryReader indexReader = DirectoryReader.Open(indexDir);
            using TaxonomyReader taxoReader = new DirectoryTaxonomyReader(taxoDir);
            IndexSearcher searcher = new IndexSearcher(indexReader);

            // Aggregate categories by an expression that combines the document's score
            // and its popularity field
            Expression expr = JavascriptCompiler.Compile("_score * sqrt(popularity)");
            SimpleBindings bindings = new SimpleBindings
            {
                new SortField("_score", SortFieldType.SCORE),     // the score of the document
                new SortField("popularity", SortFieldType.INT64), // the value of the 'popularity' field
            };

            // Aggregates the facet values
            FacetsCollector fc = new FacetsCollector(true);

            // MatchAllDocsQuery is for "browsing" (counts facets
            // for all non-deleted docs in the index); normally
            // you'd use a "normal" query:
            FacetsCollector.Search(searcher, new MatchAllDocsQuery(), 10, fc);

            // Retrieve results
            Facets facets = new TaxonomyFacetSumValueSource(taxoReader, config, fc, expr.GetValueSource(bindings));
            FacetResult result = facets.GetTopChildren(10, "A");

            return result;
        }

        /// <summary>Runs the search example.</summary>
        public FacetResult RunSearch()
        {
            Index();
            return Search();
        }

        /// <summary>Runs the search and drill-down examples and prints the results.</summary>
        public static void Main(string[] args)
        {
            Console.WriteLine("Facet counting example:");
            Console.WriteLine("-----------------------");
            FacetResult result = new ExpressionAggregationFacetsExample().RunSearch();
            Console.WriteLine(result);
        }
    }
}
