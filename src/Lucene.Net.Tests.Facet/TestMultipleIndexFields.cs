// Lucene version compatibility level 4.8.1
using System.Collections.Generic;
using NUnit.Framework;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Facet
{
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


    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using MockTokenizer = Lucene.Net.Analysis.MockTokenizer;
    using Document = Lucene.Net.Documents.Document;
    using Field = Lucene.Net.Documents.Field;
    using TextField = Lucene.Net.Documents.TextField;
    using TaxonomyReader = Lucene.Net.Facet.Taxonomy.TaxonomyReader;
    using ITaxonomyWriter = Lucene.Net.Facet.Taxonomy.ITaxonomyWriter;
    using DirectoryTaxonomyReader = Lucene.Net.Facet.Taxonomy.Directory.DirectoryTaxonomyReader;
    using DirectoryTaxonomyWriter = Lucene.Net.Facet.Taxonomy.Directory.DirectoryTaxonomyWriter;
    using AtomicReader = Lucene.Net.Index.AtomicReader;
    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using OpenMode = Lucene.Net.Index.OpenMode;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using IndexSearcher = Lucene.Net.Search.IndexSearcher;
    using MatchAllDocsQuery = Lucene.Net.Search.MatchAllDocsQuery;
    using Directory = Lucene.Net.Store.Directory;
    using IOUtils = Lucene.Net.Util.IOUtils;

    public class TestMultipleIndexFields : FacetTestCase
    {

        private static readonly FacetField[] CATEGORIES = new FacetField[] {
            new FacetField("Author", "Mark Twain"),
            new FacetField("Author", "Stephen King"),
            new FacetField("Author", "Kurt Vonnegut"),
            new FacetField("Band", "Rock & Pop", "The Beatles"),
            new FacetField("Band", "Punk", "The Ramones"),
            new FacetField("Band", "Rock & Pop", "U2"),
            new FacetField("Band", "Rock & Pop", "REM"),
            new FacetField("Band", "Rock & Pop", "Dave Matthews Band"),
            new FacetField("Composer", "Bach")
        };

        private FacetsConfig GetConfig()
        {
            FacetsConfig config = new FacetsConfig();
            config.SetHierarchical("Band", true);
            return config;
        }

        [Test]
        public virtual void TestDefault()
        {
            Directory indexDir = NewDirectory();
            Directory taxoDir = NewDirectory();

            // create and open an index writer
            var iw = new RandomIndexWriter(Random, indexDir, NewIndexWriterConfig(
                TEST_VERSION_CURRENT, new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false)));
            // create and open a taxonomy writer
            var tw = new DirectoryTaxonomyWriter(taxoDir, OpenMode.CREATE);
            var config = GetConfig();

            seedIndex(tw, iw, config);

            IndexReader ir = iw.GetReader();
            tw.Commit();

            // prepare index reader and taxonomy.
            var tr = new DirectoryTaxonomyReader(taxoDir);

            // prepare searcher to search against
            IndexSearcher searcher = NewSearcher(ir);

            FacetsCollector sfc = PerformSearch(tr, ir, searcher);

            // Obtain facets results and hand-test them
            AssertCorrectResults(GetTaxonomyFacetCounts(tr, config, sfc));

            assertOrdinalsExist("$facets", ir);

            IOUtils.Dispose(tr, ir, iw, tw, indexDir, taxoDir);
        }

        [Test]
        public virtual void TestCustom()
        {
            Directory indexDir = NewDirectory();
            Directory taxoDir = NewDirectory();

            // create and open an index writer
            RandomIndexWriter iw = new RandomIndexWriter(Random, indexDir, NewIndexWriterConfig(
                TEST_VERSION_CURRENT, new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false)));
            // create and open a taxonomy writer
            var tw = new DirectoryTaxonomyWriter(taxoDir, OpenMode.CREATE);

            FacetsConfig config = GetConfig();
            config.SetIndexFieldName("Author", "$author");
            seedIndex(tw, iw, config);

            IndexReader ir = iw.GetReader();
            tw.Commit();

            // prepare index reader and taxonomy.
            var tr = new DirectoryTaxonomyReader(taxoDir);

            // prepare searcher to search against
            IndexSearcher searcher = NewSearcher(ir);

            FacetsCollector sfc = PerformSearch(tr, ir, searcher);

            IDictionary<string, Facets> facetsMap = new Dictionary<string, Facets>();
            facetsMap["Author"] = GetTaxonomyFacetCounts(tr, config, sfc, "$author");
            Facets facets = new MultiFacets(facetsMap, GetTaxonomyFacetCounts(tr, config, sfc));

            // Obtain facets results and hand-test them
            AssertCorrectResults(facets);

            assertOrdinalsExist("$facets", ir);
            assertOrdinalsExist("$author", ir);

            IOUtils.Dispose(tr, ir, iw, tw, indexDir, taxoDir);
        }

        [Test]
        public virtual void TestTwoCustomsSameField()
        {
            Directory indexDir = NewDirectory();
            Directory taxoDir = NewDirectory();

            // create and open an index writer
            RandomIndexWriter iw = new RandomIndexWriter(Random, indexDir, NewIndexWriterConfig(
                TEST_VERSION_CURRENT, new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false)));
            // create and open a taxonomy writer
            var tw = new DirectoryTaxonomyWriter(taxoDir, OpenMode.CREATE);

            FacetsConfig config = GetConfig();
            config.SetIndexFieldName("Band", "$music");
            config.SetIndexFieldName("Composer", "$music");
            seedIndex(tw, iw, config);

            IndexReader ir = iw.GetReader();
            tw.Commit();

            // prepare index reader and taxonomy.
            var tr = new DirectoryTaxonomyReader(taxoDir);

            // prepare searcher to search against
            IndexSearcher searcher = NewSearcher(ir);

            FacetsCollector sfc = PerformSearch(tr, ir, searcher);

            IDictionary<string, Facets> facetsMap = new Dictionary<string, Facets>();
            Facets facets2 = GetTaxonomyFacetCounts(tr, config, sfc, "$music");
            facetsMap["Band"] = facets2;
            facetsMap["Composer"] = facets2;
            Facets facets = new MultiFacets(facetsMap, GetTaxonomyFacetCounts(tr, config, sfc));

            // Obtain facets results and hand-test them
            AssertCorrectResults(facets);

            assertOrdinalsExist("$facets", ir);
            assertOrdinalsExist("$music", ir);
            assertOrdinalsExist("$music", ir);

            IOUtils.Dispose(tr, ir, iw, tw, indexDir, taxoDir);
        }

        private void assertOrdinalsExist(string field, IndexReader ir)
        {
            foreach (AtomicReaderContext context in ir.Leaves)
            {
                AtomicReader r = context.AtomicReader;
                if (r.GetBinaryDocValues(field) != null)
                {
                    return; // not all segments must have this DocValues
                }
            }
            fail("no ordinals found for " + field);
        }

        [Test]
        public virtual void TestDifferentFieldsAndText()
        {
            Directory indexDir = NewDirectory();
            Directory taxoDir = NewDirectory();

            // create and open an index writer
            var iw = new RandomIndexWriter(Random, indexDir, NewIndexWriterConfig(
                TEST_VERSION_CURRENT, new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false)));
            // create and open a taxonomy writer
            var tw = new DirectoryTaxonomyWriter(taxoDir, OpenMode.CREATE);

            FacetsConfig config = GetConfig();
            config.SetIndexFieldName("Band", "$bands");
            config.SetIndexFieldName("Composer", "$composers");
            seedIndex(tw, iw, config);

            IndexReader ir = iw.GetReader();
            tw.Commit();

            // prepare index reader and taxonomy.
            var tr = new DirectoryTaxonomyReader(taxoDir);

            // prepare searcher to search against
            IndexSearcher searcher = NewSearcher(ir);

            FacetsCollector sfc = PerformSearch(tr, ir, searcher);

            IDictionary<string, Facets> facetsMap = new Dictionary<string, Facets>();
            facetsMap["Band"] = GetTaxonomyFacetCounts(tr, config, sfc, "$bands");
            facetsMap["Composer"] = GetTaxonomyFacetCounts(tr, config, sfc, "$composers");
            Facets facets = new MultiFacets(facetsMap, GetTaxonomyFacetCounts(tr, config, sfc));

            // Obtain facets results and hand-test them
            AssertCorrectResults(facets);
            assertOrdinalsExist("$facets", ir);
            assertOrdinalsExist("$bands", ir);
            assertOrdinalsExist("$composers", ir);

            IOUtils.Dispose(tr, ir, iw, tw, indexDir, taxoDir);
        }

        [Test]
        public virtual void TestSomeSameSomeDifferent()
        {
            Directory indexDir = NewDirectory();
            Directory taxoDir = NewDirectory();

            // create and open an index writer
            RandomIndexWriter iw = new RandomIndexWriter(Random, indexDir, NewIndexWriterConfig(
                TEST_VERSION_CURRENT, new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false)));
            // create and open a taxonomy writer
            ITaxonomyWriter tw = new DirectoryTaxonomyWriter(taxoDir, OpenMode.CREATE);

            FacetsConfig config = GetConfig();
            config.SetIndexFieldName("Band", "$music");
            config.SetIndexFieldName("Composer", "$music");
            config.SetIndexFieldName("Author", "$literature");
            seedIndex(tw, iw, config);

            IndexReader ir = iw.GetReader();
            tw.Commit();

            // prepare index reader and taxonomy.
            var tr = new DirectoryTaxonomyReader(taxoDir);

            // prepare searcher to search against
            IndexSearcher searcher = NewSearcher(ir);

            FacetsCollector sfc = PerformSearch(tr, ir, searcher);

            IDictionary<string, Facets> facetsMap = new Dictionary<string, Facets>();
            Facets facets2 = GetTaxonomyFacetCounts(tr, config, sfc, "$music");
            facetsMap["Band"] = facets2;
            facetsMap["Composer"] = facets2;
            facetsMap["Author"] = GetTaxonomyFacetCounts(tr, config, sfc, "$literature");
            Facets facets = new MultiFacets(facetsMap, GetTaxonomyFacetCounts(tr, config, sfc));

            // Obtain facets results and hand-test them
            AssertCorrectResults(facets);
            assertOrdinalsExist("$music", ir);
            assertOrdinalsExist("$literature", ir);

            IOUtils.Dispose(tr, ir, iw, tw);
            IOUtils.Dispose(indexDir, taxoDir);
        }

        
        private void AssertCorrectResults(Facets facets)
        {
            Assert.AreEqual(5, facets.GetSpecificValue("Band"), 0);
            Assert.AreEqual("dim=Band path=[] value=5 childCount=2\n  Rock & Pop (4)\n  Punk (1)\n", facets.GetTopChildren(10, "Band").ToString());
            Assert.AreEqual("dim=Band path=[Rock & Pop] value=4 childCount=4\n  The Beatles (1)\n  U2 (1)\n  REM (1)\n  Dave Matthews Band (1)\n", facets.GetTopChildren(10, "Band", "Rock & Pop").ToString());
            Assert.AreEqual("dim=Author path=[] value=3 childCount=3\n  Mark Twain (1)\n  Stephen King (1)\n  Kurt Vonnegut (1)\n", facets.GetTopChildren(10, "Author").ToString());
        }

        
        private static FacetsCollector PerformSearch(TaxonomyReader tr, IndexReader ir, IndexSearcher searcher)
        {
            FacetsCollector fc = new FacetsCollector();
            FacetsCollector.Search(searcher, new MatchAllDocsQuery(), 10, fc);
            return fc;
        }

        private static void seedIndex(ITaxonomyWriter tw, RandomIndexWriter iw, FacetsConfig config)
        {
            foreach (FacetField ff in CATEGORIES)
            {
                Document doc = new Document();
                doc.Add(ff);
                doc.Add(new TextField("content", "alpha", Field.Store.YES));
                iw.AddDocument(config.Build(tw, doc));
            }
        }
    }
}