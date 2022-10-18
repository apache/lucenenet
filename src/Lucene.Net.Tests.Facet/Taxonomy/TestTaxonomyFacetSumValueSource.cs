// Lucene version compatibility level 4.8.1
using Lucene.Net.Diagnostics;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using JCG = J2N.Collections.Generic;
using Assert = Lucene.Net.TestFramework.Assert;
using Console = Lucene.Net.Util.SystemConsole;

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


    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using ConstantScoreQuery = Lucene.Net.Search.ConstantScoreQuery;
    using DirectoryReader = Lucene.Net.Index.DirectoryReader;
    using DirectoryTaxonomyReader = Lucene.Net.Facet.Taxonomy.Directory.DirectoryTaxonomyReader;
    using DirectoryTaxonomyWriter = Lucene.Net.Facet.Taxonomy.Directory.DirectoryTaxonomyWriter;
    using Document = Lucene.Net.Documents.Document;
    using DoubleDocValues = Lucene.Net.Queries.Function.DocValues.DoubleDocValues;
    using Field = Lucene.Net.Documents.Field;
    using FunctionQuery = Lucene.Net.Queries.Function.FunctionQuery;
    using FunctionValues = Lucene.Net.Queries.Function.FunctionValues;
    using IndexSearcher = Lucene.Net.Search.IndexSearcher;
    using IndexWriter = Lucene.Net.Index.IndexWriter;
    using Int32Field = Lucene.Net.Documents.Int32Field;
    using Int32FieldSource = Lucene.Net.Queries.Function.ValueSources.Int32FieldSource;
    using Int64FieldSource = Lucene.Net.Queries.Function.ValueSources.Int64FieldSource;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using MatchAllDocsQuery = Lucene.Net.Search.MatchAllDocsQuery;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using NumericDocValuesField = Lucene.Net.Documents.NumericDocValuesField;
    using OpenMode = Lucene.Net.Index.OpenMode;
    using Query = Lucene.Net.Search.Query;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using Scorer = Lucene.Net.Search.Scorer;
    using SingleDocValuesField = Lucene.Net.Documents.SingleDocValuesField;
    using SingleFieldSource = Lucene.Net.Queries.Function.ValueSources.SingleFieldSource;
    using StringField = Lucene.Net.Documents.StringField;
    using Term = Lucene.Net.Index.Term;
    using TermQuery = Lucene.Net.Search.TermQuery;
    using TestUtil = Lucene.Net.Util.TestUtil;
    using TopDocs = Lucene.Net.Search.TopDocs;
    using ValueSource = Lucene.Net.Queries.Function.ValueSource;

    [TestFixture]
    public class TestTaxonomyFacetSumValueSource : FacetTestCase
    {

        [Test]
        public virtual void TestBasic()
        {
            Store.Directory dir = NewDirectory();
            Store.Directory taxoDir = NewDirectory();

            // Writes facet ords to a separate directory from the
            // main index:
            DirectoryTaxonomyWriter taxoWriter = new DirectoryTaxonomyWriter(taxoDir, OpenMode.CREATE);

            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);
            FacetsConfig config = new FacetsConfig();

            // Reused across documents, to add the necessary facet
            // fields:
            Document doc = new Document();
            doc.Add(new Int32Field("num", 10, Field.Store.NO));
            doc.Add(new FacetField("Author", "Bob"));
            writer.AddDocument(config.Build(taxoWriter, doc));

            doc = new Document();
            doc.Add(new Int32Field("num", 20, Field.Store.NO));
            doc.Add(new FacetField("Author", "Lisa"));
            writer.AddDocument(config.Build(taxoWriter, doc));

            doc = new Document();
            doc.Add(new Int32Field("num", 30, Field.Store.NO));
            doc.Add(new FacetField("Author", "Lisa"));
            writer.AddDocument(config.Build(taxoWriter, doc));

            doc = new Document();
            doc.Add(new Int32Field("num", 40, Field.Store.NO));
            doc.Add(new FacetField("Author", "Susan"));
            writer.AddDocument(config.Build(taxoWriter, doc));

            doc = new Document();
            doc.Add(new Int32Field("num", 45, Field.Store.NO));
            doc.Add(new FacetField("Author", "Frank"));
            writer.AddDocument(config.Build(taxoWriter, doc));

            // NRT open
            IndexSearcher searcher = NewSearcher(writer.GetReader());
            writer.Dispose();

            // NRT open
            var taxoReader = new DirectoryTaxonomyReader(taxoWriter);
            taxoWriter.Dispose();

            // Aggregate the facet counts:
            FacetsCollector c = new FacetsCollector();

            // MatchAllDocsQuery is for "browsing" (counts facets
            // for all non-deleted docs in the index); normally
            // you'd use a "normal" query and one of the
            // Facets.search utility methods:
            searcher.Search(new MatchAllDocsQuery(), c);

            TaxonomyFacetSumValueSource facets = new TaxonomyFacetSumValueSource(taxoReader, new FacetsConfig(), c, new Int32FieldSource("num"));

            // Retrieve & verify results:
            Assert.AreEqual("dim=Author path=[] value=145.0 childCount=4\n  Lisa (50.0)\n  Frank (45.0)\n  Susan (40.0)\n  Bob (10.0)\n", facets.GetTopChildren(10, "Author").ToString(CultureInfo.InvariantCulture));

            taxoReader.Dispose();
            searcher.IndexReader.Dispose();
            dir.Dispose();
            taxoDir.Dispose();
        }

        // LUCENE-5333
        [Test]
        public virtual void TestSparseFacets()
        {
            Store.Directory dir = NewDirectory();
            Store.Directory taxoDir = NewDirectory();

            // Writes facet ords to a separate directory from the
            // main index:
            var taxoWriter = new DirectoryTaxonomyWriter(taxoDir, OpenMode.CREATE);

            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);
            FacetsConfig config = new FacetsConfig();

            Document doc = new Document();
            doc.Add(new Int32Field("num", 10, Field.Store.NO));
            doc.Add(new FacetField("a", "foo1"));
            writer.AddDocument(config.Build(taxoWriter, doc));

            if (Random.NextBoolean())
            {
                writer.Commit();
            }

            doc = new Document();
            doc.Add(new Int32Field("num", 20, Field.Store.NO));
            doc.Add(new FacetField("a", "foo2"));
            doc.Add(new FacetField("b", "bar1"));
            writer.AddDocument(config.Build(taxoWriter, doc));

            if (Random.NextBoolean())
            {
                writer.Commit();
            }

            doc = new Document();
            doc.Add(new Int32Field("num", 30, Field.Store.NO));
            doc.Add(new FacetField("a", "foo3"));
            doc.Add(new FacetField("b", "bar2"));
            doc.Add(new FacetField("c", "baz1"));
            writer.AddDocument(config.Build(taxoWriter, doc));

            // NRT open
            IndexSearcher searcher = NewSearcher(writer.GetReader());
            writer.Dispose();

            // NRT open
            var taxoReader = new DirectoryTaxonomyReader(taxoWriter);
            taxoWriter.Dispose();

            FacetsCollector c = new FacetsCollector();
            searcher.Search(new MatchAllDocsQuery(), c);

            TaxonomyFacetSumValueSource facets = new TaxonomyFacetSumValueSource(taxoReader, new FacetsConfig(), c, new Int32FieldSource("num"));

            // Ask for top 10 labels for any dims that have counts:
            IList<FacetResult> results = facets.GetAllDims(10);

            Assert.AreEqual(3, results.Count);
            Assert.AreEqual("dim=a path=[] value=60.0 childCount=3\n  foo3 (30.0)\n  foo2 (20.0)\n  foo1 (10.0)\n", results[0].ToString(CultureInfo.InvariantCulture));
            Assert.AreEqual("dim=b path=[] value=50.0 childCount=2\n  bar2 (30.0)\n  bar1 (20.0)\n", results[1].ToString(CultureInfo.InvariantCulture));
            Assert.AreEqual("dim=c path=[] value=30.0 childCount=1\n  baz1 (30.0)\n", results[2].ToString(CultureInfo.InvariantCulture));

            IOUtils.Dispose(searcher.IndexReader, taxoReader, dir, taxoDir);
        }

        [Test]
        public virtual void TestWrongIndexFieldName()
        {
            Store.Directory dir = NewDirectory();
            Store.Directory taxoDir = NewDirectory();

            // Writes facet ords to a separate directory from the
            // main index:
            var taxoWriter = new DirectoryTaxonomyWriter(taxoDir, OpenMode.CREATE);

            FacetsConfig config = new FacetsConfig();
            config.SetIndexFieldName("a", "$facets2");

            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);

            Document doc = new Document();
            doc.Add(new Int32Field("num", 10, Field.Store.NO));
            doc.Add(new FacetField("a", "foo1"));
            writer.AddDocument(config.Build(taxoWriter, doc));

            // NRT open
            IndexSearcher searcher = NewSearcher(writer.GetReader());
            writer.Dispose();

            // NRT open
            var taxoReader = new DirectoryTaxonomyReader(taxoWriter);
            taxoWriter.Dispose();

            FacetsCollector c = new FacetsCollector();
            searcher.Search(new MatchAllDocsQuery(), c);

            TaxonomyFacetSumValueSource facets = new TaxonomyFacetSumValueSource(taxoReader, config, c, new Int32FieldSource("num"));

            // Ask for top 10 labels for any dims that have counts:
            IList<FacetResult> results = facets.GetAllDims(10);
            Assert.IsTrue(results.Count == 0);

            try
            {
                facets.GetSpecificValue("a");
                fail("should have hit exc");
            }
            catch (Exception iae) when (iae.IsIllegalArgumentException())
            {
                // expected
            }

            try
            {
                facets.GetTopChildren(10, "a");
                fail("should have hit exc");
            }
            catch (Exception iae) when (iae.IsIllegalArgumentException())
            {
                // expected
            }

            IOUtils.Dispose(searcher.IndexReader, taxoReader, dir, taxoDir);
        }

        [Test]
        public virtual void TestSumScoreAggregator()
        {
            Store.Directory indexDir = NewDirectory();
            Store.Directory taxoDir = NewDirectory();

            DirectoryTaxonomyWriter taxoWriter = new DirectoryTaxonomyWriter(taxoDir);
            IndexWriter iw = new IndexWriter(indexDir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));

            FacetsConfig config = new FacetsConfig();

            for (int i = AtLeast(30); i > 0; --i)
            {
                Document doc = new Document();
                if (Random.NextBoolean()) // don't match all documents
                {
                    doc.Add(new StringField("f", "v", Field.Store.NO));
                }
                doc.Add(new FacetField("dim", "a"));
                iw.AddDocument(config.Build(taxoWriter, doc));
            }

            DirectoryReader r = DirectoryReader.Open(iw, true);
            DirectoryTaxonomyReader taxoReader = new DirectoryTaxonomyReader(taxoWriter);

            FacetsCollector fc = new FacetsCollector(true);
            ConstantScoreQuery csq = new ConstantScoreQuery(new MatchAllDocsQuery());
            csq.Boost = 2.0f;

            TopDocs td = FacetsCollector.Search(NewSearcher(r), csq, 10, fc);

            Facets facets = new TaxonomyFacetSumValueSource(taxoReader, config, fc, new TaxonomyFacetSumValueSource.ScoreValueSource());

            int expected = (int)(td.MaxScore * td.TotalHits);
            Assert.AreEqual(expected, (int)facets.GetSpecificValue("dim", "a"));

            IOUtils.Dispose(iw, taxoWriter, taxoReader, taxoDir, r, indexDir);
        }

        [Test]
        public virtual void TestNoScore()
        {
            Store.Directory indexDir = NewDirectory();
            Store.Directory taxoDir = NewDirectory();

            DirectoryTaxonomyWriter taxoWriter = new DirectoryTaxonomyWriter(taxoDir);
            IndexWriter iw = new IndexWriter(indexDir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            FacetsConfig config = new FacetsConfig();
            for (int i = 0; i < 4; i++)
            {
                Document doc = new Document();
                doc.Add(new NumericDocValuesField("price", (i + 1)));
                doc.Add(new FacetField("a", Convert.ToString(i % 2, CultureInfo.InvariantCulture)));
                iw.AddDocument(config.Build(taxoWriter, doc));
            }

            DirectoryReader r = DirectoryReader.Open(iw, true);
            DirectoryTaxonomyReader taxoReader = new DirectoryTaxonomyReader(taxoWriter);

            FacetsCollector sfc = new FacetsCollector();
            NewSearcher(r).Search(new MatchAllDocsQuery(), sfc);
            Facets facets = new TaxonomyFacetSumValueSource(taxoReader, config, sfc, new Int64FieldSource("price"));
            Assert.AreEqual("dim=a path=[] value=10.0 childCount=2\n  1 (6.0)\n  0 (4.0)\n", facets.GetTopChildren(10, "a").ToString(CultureInfo.InvariantCulture));

            IOUtils.Dispose(taxoWriter, iw, taxoReader, taxoDir, r, indexDir);
        }

        [Test]
        public virtual void TestWithScore()
        {
            Store.Directory indexDir = NewDirectory();
            Store.Directory taxoDir = NewDirectory();

            DirectoryTaxonomyWriter taxoWriter = new DirectoryTaxonomyWriter(taxoDir);
            IndexWriter iw = new IndexWriter(indexDir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));

            FacetsConfig config = new FacetsConfig();
            for (int i = 0; i < 4; i++)
            {
                Document doc = new Document();
                doc.Add(new NumericDocValuesField("price", (i + 1)));
                doc.Add(new FacetField("a", Convert.ToString(i % 2, CultureInfo.InvariantCulture)));
                iw.AddDocument(config.Build(taxoWriter, doc));
            }

            DirectoryReader r = DirectoryReader.Open(iw, true);
            DirectoryTaxonomyReader taxoReader = new DirectoryTaxonomyReader(taxoWriter);

            ValueSource valueSource = new ValueSourceAnonymousClass(this);

            FacetsCollector fc = new FacetsCollector(true);
            // score documents by their 'price' field - makes asserting the correct counts for the categories easier
            Query q = new FunctionQuery(new Int64FieldSource("price"));
            FacetsCollector.Search(NewSearcher(r), q, 10, fc);
            Facets facets = new TaxonomyFacetSumValueSource(taxoReader, config, fc, valueSource);

            Assert.AreEqual("dim=a path=[] value=10.0 childCount=2\n  1 (6.0)\n  0 (4.0)\n", facets.GetTopChildren(10, "a").ToString(CultureInfo.InvariantCulture));

            IOUtils.Dispose(taxoWriter, iw, taxoReader, taxoDir, r, indexDir);
        }

        private sealed class ValueSourceAnonymousClass : ValueSource
        {
            private readonly TestTaxonomyFacetSumValueSource outerInstance;

            public ValueSourceAnonymousClass(TestTaxonomyFacetSumValueSource outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            public override FunctionValues GetValues(IDictionary context, AtomicReaderContext readerContext)
            {
                Scorer scorer = (Scorer)context["scorer"];
                if (Debugging.AssertsEnabled) Debugging.Assert(scorer != null);
                return new DoubleDocValuesAnonymousClass(this, scorer);
            }

            private sealed class DoubleDocValuesAnonymousClass : DoubleDocValues
            {
                private readonly ValueSourceAnonymousClass outerInstance;

                private readonly Scorer scorer;

                public DoubleDocValuesAnonymousClass(ValueSourceAnonymousClass outerInstance, Scorer scorer)
                    : base(null) //todo: value source
                {
                    this.outerInstance = outerInstance;
                    this.scorer = scorer;
                }



                public override double DoubleVal(int document)
                {
                    try
                    {
                        return scorer.GetScore();
                    }
                    catch (Exception exception) when (exception.IsIOException())
                    {
                        throw RuntimeException.Create(exception);
                    }
                }
            }

            public override bool Equals(object o)
            {
                return o == this;
            }
            public override int GetHashCode()
            {
                return RuntimeHelpers.GetHashCode(this);
            }

            public override string GetDescription()
            {
                return "score()";
            }
        }

        [Test]
        public virtual void TestRollupValues()
        {
            Store.Directory indexDir = NewDirectory();
            Store.Directory taxoDir = NewDirectory();

            var taxoWriter = new DirectoryTaxonomyWriter(taxoDir);
            IndexWriter iw = new IndexWriter(indexDir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            FacetsConfig config = new FacetsConfig();
            config.SetHierarchical("a", true);
            //config.setRequireDimCount("a", true);

            for (int i = 0; i < 4; i++)
            {
                Document doc = new Document();
                doc.Add(new NumericDocValuesField("price", (i + 1)));
                doc.Add(new FacetField("a", Convert.ToString(i % 2, CultureInfo.InvariantCulture), "1"));
                iw.AddDocument(config.Build(taxoWriter, doc));
            }

            DirectoryReader r = DirectoryReader.Open(iw, true);
            DirectoryTaxonomyReader taxoReader = new DirectoryTaxonomyReader(taxoWriter);

            ValueSource valueSource = new Int64FieldSource("price");
            FacetsCollector sfc = new FacetsCollector();
            NewSearcher(r).Search(new MatchAllDocsQuery(), sfc);
            Facets facets = new TaxonomyFacetSumValueSource(taxoReader, config, sfc, valueSource);

            Assert.AreEqual("dim=a path=[] value=10.0 childCount=2\n  1 (6.0)\n  0 (4.0)\n", facets.GetTopChildren(10, "a").ToString(CultureInfo.InvariantCulture));

            IOUtils.Dispose(taxoWriter, iw, taxoReader, taxoDir, r, indexDir);
        }

        [Test]
        public virtual void TestCountAndSumScore()
        {
            Store.Directory indexDir = NewDirectory();
            Store.Directory taxoDir = NewDirectory();

            var taxoWriter = new DirectoryTaxonomyWriter(taxoDir);
            IndexWriter iw = new IndexWriter(indexDir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            FacetsConfig config = new FacetsConfig();
            config.SetIndexFieldName("b", "$b");

            for (int i = AtLeast(30); i > 0; --i)
            {
                Document doc = new Document();
                doc.Add(new StringField("f", "v", Field.Store.NO));
                doc.Add(new FacetField("a", "1"));
                doc.Add(new FacetField("b", "1"));
                iw.AddDocument(config.Build(taxoWriter, doc));
            }

            DirectoryReader r = DirectoryReader.Open(iw, true);
            var taxoReader = new DirectoryTaxonomyReader(taxoWriter);

            FacetsCollector fc = new FacetsCollector(true);
            FacetsCollector.Search(NewSearcher(r), new MatchAllDocsQuery(), 10, fc);

            Facets facets1 = GetTaxonomyFacetCounts(taxoReader, config, fc);
            Facets facets2 = new TaxonomyFacetSumValueSource(new DocValuesOrdinalsReader("$b"), taxoReader, config, fc, new TaxonomyFacetSumValueSource.ScoreValueSource());

            Assert.AreEqual(r.MaxDoc, (int)facets1.GetTopChildren(10, "a").Value);
            Assert.AreEqual(r.MaxDoc, (double)facets2.GetTopChildren(10, "b").Value, 1E-10);
            IOUtils.Dispose(taxoWriter, iw, taxoReader, taxoDir, r, indexDir);
        }

        [Test]
        public virtual void TestRandom()
        {
            string[] tokens = GetRandomTokens(10);
            Store.Directory indexDir = NewDirectory();
            Store.Directory taxoDir = NewDirectory();

            RandomIndexWriter w = new RandomIndexWriter(Random, indexDir);
            var tw = new DirectoryTaxonomyWriter(taxoDir);
            FacetsConfig config = new FacetsConfig();
            int numDocs = AtLeast(1000);
            int numDims = TestUtil.NextInt32(Random, 1, 7);
            IList<TestDoc> testDocs = GetRandomDocs(tokens, numDocs, numDims);
            foreach (TestDoc testDoc in testDocs)
            {
                Document doc = new Document();
                doc.Add(NewStringField("content", testDoc.content, Field.Store.NO));
                testDoc.value = Random.NextSingle();
                doc.Add(new SingleDocValuesField("value", testDoc.value));
                for (int j = 0; j < numDims; j++)
                {
                    if (testDoc.dims[j] != null)
                    {
                        doc.Add(new FacetField("dim" + j, testDoc.dims[j]));
                    }
                }
                w.AddDocument(config.Build(tw, doc));
            }

            // NRT open
            IndexSearcher searcher = NewSearcher(w.GetReader());

            // NRT open
            var tr = new DirectoryTaxonomyReader(tw);

            ValueSource values = new SingleFieldSource("value");

            int iters = AtLeast(100);
            for (int iter = 0; iter < iters; iter++)
            {
                string searchToken = tokens[Random.Next(tokens.Length)];
                if (Verbose)
                {
                    Console.WriteLine("\nTEST: iter content=" + searchToken);
                }
                FacetsCollector fc = new FacetsCollector();
                FacetsCollector.Search(searcher, new TermQuery(new Term("content", searchToken)), 10, fc);
                Facets facets = new TaxonomyFacetSumValueSource(tr, config, fc, values);

                // Slow, yet hopefully bug-free, faceting:
                var expectedValues = new JCG.List<Dictionary<string, float>>(numDims);
                for (int i = 0; i < numDims; i++)
                {
                    expectedValues.Add(new Dictionary<string, float>());
                }

                foreach (TestDoc doc in testDocs)
                {
                    if (doc.content.Equals(searchToken, StringComparison.Ordinal))
                    {
                        for (int j = 0; j < numDims; j++)
                        {
                            if (doc.dims[j] != null)
                            {
                                if (!expectedValues[j].TryGetValue(doc.dims[j], out float v))
                                {
                                    expectedValues[j][doc.dims[j]] = doc.value;
                                }
                                else
                                {
                                    expectedValues[j][doc.dims[j]] = (float)v + doc.value;
                                }
                            }
                        }
                    }
                }

                JCG.List<FacetResult> expected = new JCG.List<FacetResult>();
                for (int i = 0; i < numDims; i++)
                {
                    JCG.List<LabelAndValue> labelValues = new JCG.List<LabelAndValue>();
                    float totValue = 0;
                    foreach (KeyValuePair<string, float> ent in expectedValues[i])
                    {
                        labelValues.Add(new LabelAndValue(ent.Key, ent.Value));
                        totValue += ent.Value;
                    }
                    SortLabelValues(labelValues);
                    if (totValue > 0)
                    {
                        expected.Add(new FacetResult("dim" + i, new string[0], totValue, labelValues.ToArray(), labelValues.Count));
                    }
                }

                // Sort by highest value, tie break by value:
                SortFacetResults(expected);

                IList<FacetResult> actual = facets.GetAllDims(10);

                // Messy: fixup ties
                SortTies(actual);

                if (Verbose)
                {
                    Console.WriteLine("expected=\n" + expected.ToString());
                    Console.WriteLine("actual=\n" + actual.ToString());
                }

                AssertFloatValuesEquals(expected, actual);
            }

            IOUtils.Dispose(w, tw, searcher.IndexReader, tr, indexDir, taxoDir);
        }
    }
}