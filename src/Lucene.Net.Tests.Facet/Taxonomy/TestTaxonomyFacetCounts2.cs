// Lucene version compatibility level 4.8.1
using J2N.Collections.Generic.Extensions;
using Lucene.Net.Index;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using System.Collections.Generic;
using System.Globalization;
using Assert = Lucene.Net.TestFramework.Assert;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Facet.Taxonomy
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

    using DirectoryReader = Lucene.Net.Index.DirectoryReader;
    using DirectoryTaxonomyReader = Lucene.Net.Facet.Taxonomy.Directory.DirectoryTaxonomyReader;
    using DirectoryTaxonomyWriter = Lucene.Net.Facet.Taxonomy.Directory.DirectoryTaxonomyWriter;
    using Document = Lucene.Net.Documents.Document;
    using IndexSearcher = Lucene.Net.Search.IndexSearcher;
    using IndexWriter = Lucene.Net.Index.IndexWriter;
    using IndexWriterConfig = Lucene.Net.Index.IndexWriterConfig;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using MatchAllDocsQuery = Lucene.Net.Search.MatchAllDocsQuery;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using Store = Lucene.Net.Documents.Field.Store;
    using StringField = Lucene.Net.Documents.StringField;
    using Term = Lucene.Net.Index.Term;
    using TermQuery = Lucene.Net.Search.TermQuery;
    [TestFixture]
    public class TestTaxonomyFacetCounts2 : FacetTestCase
    {

        private static readonly Term A = new Term("f", "a");
        private const string CP_A = "A", CP_B = "B";
        private const string CP_C = "C", CP_D = "D"; // indexed w/ NO_PARENTS
        private const int NUM_CHILDREN_CP_A = 5, NUM_CHILDREN_CP_B = 3;
        private const int NUM_CHILDREN_CP_C = 5, NUM_CHILDREN_CP_D = 5;
        private static readonly FacetField[] CATEGORIES_A, CATEGORIES_B;
        private static readonly FacetField[] CATEGORIES_C, CATEGORIES_D;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1810:Initialize reference type static fields inline", Justification = "Complexity")]
        static TestTaxonomyFacetCounts2()
        {
            CATEGORIES_A = new FacetField[NUM_CHILDREN_CP_A];
            for (int i = 0; i < NUM_CHILDREN_CP_A; i++)
            {
                CATEGORIES_A[i] = new FacetField(CP_A, Convert.ToString(i, CultureInfo.InvariantCulture));
            }
            CATEGORIES_B = new FacetField[NUM_CHILDREN_CP_B];
            for (int i = 0; i < NUM_CHILDREN_CP_B; i++)
            {
                CATEGORIES_B[i] = new FacetField(CP_B, Convert.ToString(i, CultureInfo.InvariantCulture));
            }

            // NO_PARENTS categories
            CATEGORIES_C = new FacetField[NUM_CHILDREN_CP_C];
            for (int i = 0; i < NUM_CHILDREN_CP_C; i++)
            {
                CATEGORIES_C[i] = new FacetField(CP_C, Convert.ToString(i, CultureInfo.InvariantCulture));
            }

            // Multi-level categories
            CATEGORIES_D = new FacetField[NUM_CHILDREN_CP_D];
            for (int i = 0; i < NUM_CHILDREN_CP_D; i++)
            {
                string val = Convert.ToString(i, CultureInfo.InvariantCulture);
                CATEGORIES_D[i] = new FacetField(CP_D, val, val + val); // e.g. D/1/11, D/2/22...
            }
        }

        private static Net.Store.Directory indexDir, taxoDir;
        private static IDictionary<string, int> allExpectedCounts, termExpectedCounts;

        [OneTimeTearDown]
        public override void AfterClass() // LUCENENET specific - renamed from AfterClassCountingFacetsAggregatorTest() to ensure calling order
        {
            IOUtils.Dispose(indexDir, taxoDir);
            base.AfterClass();
        }

        private static IList<FacetField> RandomCategories(Random random)
        {
            // add random categories from the two dimensions, ensuring that the same
            // category is not added twice.
            int numFacetsA = random.Next(3) + 1; // 1-3
            int numFacetsB = random.Next(2) + 1; // 1-2
            JCG.List<FacetField> categories_a = new JCG.List<FacetField>();
            categories_a.AddRange(CATEGORIES_A);
            JCG.List<FacetField> categories_b = new JCG.List<FacetField>();
            categories_b.AddRange(CATEGORIES_B);
            categories_a.Shuffle(Random);
            categories_b.Shuffle(Random);

            JCG.List<FacetField> categories = new JCG.List<FacetField>();
            categories.AddRange(categories_a.GetView(0, numFacetsA)); // LUCENENET: Checked length for correctness
            categories.AddRange(categories_b.GetView(0, numFacetsB)); // LUCENENET: Checked length for correctness

            // add the NO_PARENT categories
            categories.Add(CATEGORIES_C[Util.LuceneTestCase.Random.Next(NUM_CHILDREN_CP_C)]);
            categories.Add(CATEGORIES_D[Util.LuceneTestCase.Random.Next(NUM_CHILDREN_CP_D)]);

            return categories;
        }

        private static void AddField(Document doc)
        {
            doc.Add(new StringField(A.Field, A.Text, Store.NO));
        }

        private static void AddFacets(Document doc, FacetsConfig config, bool updateTermExpectedCounts)
        {
            IList<FacetField> docCategories = RandomCategories(Random);
            foreach (FacetField ff in docCategories)
            {
                doc.Add(ff);
                string cp = ff.Dim + "/" + ff.Path[0];
                allExpectedCounts[cp] = allExpectedCounts[cp] + 1;
                if (updateTermExpectedCounts)
                {
                    termExpectedCounts[cp] = termExpectedCounts[cp] + 1;
                }
            }
            // add 1 to each NO_PARENTS dimension
            allExpectedCounts[CP_B] = allExpectedCounts[CP_B] + 1;
            allExpectedCounts[CP_C] = allExpectedCounts[CP_C] + 1;
            allExpectedCounts[CP_D] = allExpectedCounts[CP_D] + 1;
            if (updateTermExpectedCounts)
            {
                termExpectedCounts[CP_B] = termExpectedCounts[CP_B] + 1;
                termExpectedCounts[CP_C] = termExpectedCounts[CP_C] + 1;
                termExpectedCounts[CP_D] = termExpectedCounts[CP_D] + 1;
            }
        }

        private static FacetsConfig GetConfig()
        {
            FacetsConfig config = new FacetsConfig();
            config.SetMultiValued("A", true);
            config.SetMultiValued("B", true);
            config.SetRequireDimCount("B", true);
            config.SetHierarchical("D", true);
            return config;
        }

        private static void IndexDocsNoFacets(IndexWriter indexWriter)
        {
            int numDocs = AtLeast(2);
            for (int i = 0; i < numDocs; i++)
            {
                Document doc = new Document();
                AddField(doc);
                indexWriter.AddDocument(doc);
            }
            indexWriter.Commit(); // flush a segment
        }

        private static void IndexDocsWithFacetsNoTerms(IndexWriter indexWriter, ITaxonomyWriter taxoWriter, IDictionary<string, int> expectedCounts)
        {
            Random random = Random;
            int numDocs = AtLeast(random, 2);
            FacetsConfig config = GetConfig();
            for (int i = 0; i < numDocs; i++)
            {
                Document doc = new Document();
                AddFacets(doc, config, false);
                indexWriter.AddDocument(config.Build(taxoWriter, doc));
            }
            indexWriter.Commit(); // flush a segment
        }

        private static void IndexDocsWithFacetsAndTerms(IndexWriter indexWriter, ITaxonomyWriter taxoWriter, IDictionary<string, int> expectedCounts)
        {
            Random random = Random;
            int numDocs = AtLeast(random, 2);
            FacetsConfig config = GetConfig();
            for (int i = 0; i < numDocs; i++)
            {
                Document doc = new Document();
                AddFacets(doc, config, true);
                AddField(doc);
                indexWriter.AddDocument(config.Build(taxoWriter, doc));
            }
            indexWriter.Commit(); // flush a segment
        }

        private static void IndexDocsWithFacetsAndSomeTerms(IndexWriter indexWriter, ITaxonomyWriter taxoWriter, IDictionary<string, int> expectedCounts)
        {
            Random random = Random;
            int numDocs = AtLeast(random, 2);
            FacetsConfig config = GetConfig();
            for (int i = 0; i < numDocs; i++)
            {
                Document doc = new Document();
                bool hasContent = random.NextBoolean();
                if (hasContent)
                {
                    AddField(doc);
                }
                AddFacets(doc, config, hasContent);
                indexWriter.AddDocument(config.Build(taxoWriter, doc));
            }
            indexWriter.Commit(); // flush a segment
        }

        // initialize expectedCounts w/ 0 for all categories
        private static IDictionary<string, int> newCounts()
        {
            IDictionary<string, int> counts = new Dictionary<string, int>();
            counts[CP_A] = 0;
            counts[CP_B] = 0;
            counts[CP_C] = 0;
            counts[CP_D] = 0;
            foreach (FacetField ff in CATEGORIES_A)
            {
                counts[ff.Dim + "/" + ff.Path[0]] = 0;
            }
            foreach (FacetField ff in CATEGORIES_B)
            {
                counts[ff.Dim + "/" + ff.Path[0]] = 0;
            }
            foreach (FacetField ff in CATEGORIES_C)
            {
                counts[ff.Dim + "/" + ff.Path[0]] = 0;
            }
            foreach (FacetField ff in CATEGORIES_D)
            {
                counts[ff.Dim + "/" + ff.Path[0]] = 0;
            }
            return counts;
        }

        [OneTimeSetUp]
        public override void BeforeClass() // LUCENENET specific - renamed from BeforeClassCountingFacetsAggregatorTest() to ensure calling order
        {
            base.BeforeClass();

            indexDir = NewDirectory();
            taxoDir = NewDirectory();

            // create an index which has:
            // 1. Segment with no categories, but matching results
            // 2. Segment w/ categories, but no results
            // 3. Segment w/ categories and results
            // 4. Segment w/ categories, but only some results

            IndexWriterConfig conf = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            conf.MergePolicy = NoMergePolicy.COMPOUND_FILES; // prevent merges, so we can control the index segments
            IndexWriter indexWriter = new IndexWriter(indexDir, conf);
            ITaxonomyWriter taxoWriter = new DirectoryTaxonomyWriter(taxoDir);

            allExpectedCounts = newCounts();
            termExpectedCounts = newCounts();

            // segment w/ no categories
            IndexDocsNoFacets(indexWriter);

            // segment w/ categories, no content
            IndexDocsWithFacetsNoTerms(indexWriter, taxoWriter, allExpectedCounts);

            // segment w/ categories and content
            IndexDocsWithFacetsAndTerms(indexWriter, taxoWriter, allExpectedCounts);

            // segment w/ categories and some content
            IndexDocsWithFacetsAndSomeTerms(indexWriter, taxoWriter, allExpectedCounts);

            IOUtils.Dispose(indexWriter, taxoWriter);
        }

        [Test]
        public virtual void TestDifferentNumResults()
        {
            // test the collector w/ FacetRequests and different numResults
            DirectoryReader indexReader = DirectoryReader.Open(indexDir);
            var taxoReader = new DirectoryTaxonomyReader(taxoDir);
            IndexSearcher searcher = NewSearcher(indexReader);

            FacetsCollector sfc = new FacetsCollector();
            TermQuery q = new TermQuery(A);
            searcher.Search(q, sfc);
            Facets facets = GetTaxonomyFacetCounts(taxoReader, GetConfig(), sfc);
            FacetResult result = facets.GetTopChildren(NUM_CHILDREN_CP_A, CP_A);
            Assert.AreEqual(-1, (int)result.Value);
            foreach (LabelAndValue labelValue in result.LabelValues)
            {
                Assert.AreEqual(termExpectedCounts[CP_A + "/" + labelValue.Label], labelValue.Value);
            }
            result = facets.GetTopChildren(NUM_CHILDREN_CP_B, CP_B);
            Assert.AreEqual(termExpectedCounts[CP_B], result.Value);
            foreach (LabelAndValue labelValue in result.LabelValues)
            {
                Assert.AreEqual(termExpectedCounts[CP_B + "/" + labelValue.Label], labelValue.Value);
            }

            IOUtils.Dispose(indexReader, taxoReader);
        }

        [Test]
        public virtual void TestAllCounts()
        {
            DirectoryReader indexReader = DirectoryReader.Open(indexDir);
            var taxoReader = new DirectoryTaxonomyReader(taxoDir);
            IndexSearcher searcher = NewSearcher(indexReader);

            FacetsCollector sfc = new FacetsCollector();
            searcher.Search(new MatchAllDocsQuery(), sfc);

            Facets facets = GetTaxonomyFacetCounts(taxoReader, GetConfig(), sfc);

            FacetResult result = facets.GetTopChildren(NUM_CHILDREN_CP_A, CP_A);
            Assert.AreEqual(-1, (int)result.Value);
            int prevValue = int.MaxValue;
            foreach (LabelAndValue labelValue in result.LabelValues)
            {
                Assert.AreEqual(allExpectedCounts[CP_A + "/" + labelValue.Label], labelValue.Value);
                Assert.IsTrue((int)labelValue.Value <= prevValue, "wrong sort order of sub results: labelValue.value=" + labelValue.Value + " prevValue=" + prevValue);
                prevValue = (int)labelValue.Value;
            }

            result = facets.GetTopChildren(NUM_CHILDREN_CP_B, CP_B);
            Assert.AreEqual(allExpectedCounts[CP_B], result.Value);
            prevValue = int.MaxValue;
            foreach (LabelAndValue labelValue in result.LabelValues)
            {
                Assert.AreEqual(allExpectedCounts[CP_B + "/" + labelValue.Label], labelValue.Value);
                Assert.IsTrue((int)labelValue.Value <= prevValue, "wrong sort order of sub results: labelValue.value=" + labelValue.Value + " prevValue=" + prevValue);
                prevValue = (int)labelValue.Value;
            }

            IOUtils.Dispose(indexReader, taxoReader);
        }

        [Test]
        public virtual void TestBigNumResults()
        {
            DirectoryReader indexReader = DirectoryReader.Open(indexDir);
            var taxoReader = new DirectoryTaxonomyReader(taxoDir);
            IndexSearcher searcher = NewSearcher(indexReader);

            FacetsCollector sfc = new FacetsCollector();
            searcher.Search(new MatchAllDocsQuery(), sfc);

            Facets facets = GetTaxonomyFacetCounts(taxoReader, GetConfig(), sfc);

            FacetResult result = facets.GetTopChildren(int.MaxValue, CP_A);
            Assert.AreEqual(-1, (int)result.Value);
            foreach (LabelAndValue labelValue in result.LabelValues)
            {
                Assert.AreEqual(allExpectedCounts[CP_A + "/" + labelValue.Label], labelValue.Value);
            }
            result = facets.GetTopChildren(int.MaxValue, CP_B);
            Assert.AreEqual(allExpectedCounts[CP_B], result.Value);
            foreach (LabelAndValue labelValue in result.LabelValues)
            {
                Assert.AreEqual(allExpectedCounts[CP_B + "/" + labelValue.Label], labelValue.Value);
            }

            IOUtils.Dispose(indexReader, taxoReader);
        }

        [Test]
        public virtual void TestNoParents()
        {
            DirectoryReader indexReader = DirectoryReader.Open(indexDir);
            var taxoReader = new DirectoryTaxonomyReader(taxoDir);
            IndexSearcher searcher = NewSearcher(indexReader);

            var sfc = new FacetsCollector();
            searcher.Search(new MatchAllDocsQuery(), sfc);

            Facets facets = GetTaxonomyFacetCounts(taxoReader, GetConfig(), sfc);

            FacetResult result = facets.GetTopChildren(NUM_CHILDREN_CP_C, CP_C);
            Assert.AreEqual(allExpectedCounts[CP_C], result.Value);
            foreach (LabelAndValue labelValue in result.LabelValues)
            {
                Assert.AreEqual(allExpectedCounts[CP_C + "/" + labelValue.Label], labelValue.Value);
            }
            result = facets.GetTopChildren(NUM_CHILDREN_CP_D, CP_D);
            Assert.AreEqual(allExpectedCounts[CP_C], result.Value);
            foreach (LabelAndValue labelValue in result.LabelValues)
            {
                Assert.AreEqual(allExpectedCounts[CP_D + "/" + labelValue.Label], labelValue.Value);
            }

            IOUtils.Dispose(indexReader, taxoReader);
        }
    }
}