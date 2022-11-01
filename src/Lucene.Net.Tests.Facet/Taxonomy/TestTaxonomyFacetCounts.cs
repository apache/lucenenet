// Lucene version compatibility level 4.8.1
using Lucene.Net.Index.Extensions;
using Lucene.Net.Support.IO;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using Assert = Lucene.Net.TestFramework.Assert;
using Console = Lucene.Net.Util.SystemConsole;
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

    using DefaultSimilarity = Lucene.Net.Search.Similarities.DefaultSimilarity;
    using DirectoryReader = Lucene.Net.Index.DirectoryReader;
    using DirectoryTaxonomyReader = Lucene.Net.Facet.Taxonomy.Directory.DirectoryTaxonomyReader;
    using DirectoryTaxonomyWriter = Lucene.Net.Facet.Taxonomy.Directory.DirectoryTaxonomyWriter;
    using Document = Lucene.Net.Documents.Document;
    using Field = Lucene.Net.Documents.Field;
    using IndexSearcher = Lucene.Net.Search.IndexSearcher;
    using IndexWriter = Lucene.Net.Index.IndexWriter;
    using IndexWriterConfig = Lucene.Net.Index.IndexWriterConfig;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using MatchAllDocsQuery = Lucene.Net.Search.MatchAllDocsQuery;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using OpenMode = Lucene.Net.Index.OpenMode;
    using PerFieldSimilarityWrapper = Lucene.Net.Search.Similarities.PerFieldSimilarityWrapper;
    using Query = Lucene.Net.Search.Query;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using Similarity = Lucene.Net.Search.Similarities.Similarity;
    using StringField = Lucene.Net.Documents.StringField;
    using Term = Lucene.Net.Index.Term;
    using TermQuery = Lucene.Net.Search.TermQuery;
    using TestUtil = Lucene.Net.Util.TestUtil;

    [TestFixture]
    public class TestTaxonomyFacetCounts : FacetTestCase
    {

        [Test]
        public virtual void TestBasic()
        {
            Store.Directory dir = NewDirectory();
            Store.Directory taxoDir = NewDirectory();

            // Writes facet ords to a separate directory from the
            // main index:
            var taxoWriter = new DirectoryTaxonomyWriter(taxoDir, OpenMode.CREATE);

            FacetsConfig config = new FacetsConfig();
            config.SetHierarchical("Publish Date", true);

            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);

            Document doc = new Document();
            doc.Add(new FacetField("Author", "Bob"));
            doc.Add(new FacetField("Publish Date", "2010", "10", "15"));
            writer.AddDocument(config.Build(taxoWriter, doc));

            doc = new Document();
            doc.Add(new FacetField("Author", "Lisa"));
            doc.Add(new FacetField("Publish Date", "2010", "10", "20"));
            writer.AddDocument(config.Build(taxoWriter, doc));

            doc = new Document();
            doc.Add(new FacetField("Author", "Lisa"));
            doc.Add(new FacetField("Publish Date", "2012", "1", "1"));
            writer.AddDocument(config.Build(taxoWriter, doc));

            doc = new Document();
            doc.Add(new FacetField("Author", "Susan"));
            doc.Add(new FacetField("Publish Date", "2012", "1", "7"));
            writer.AddDocument(config.Build(taxoWriter, doc));

            doc = new Document();
            doc.Add(new FacetField("Author", "Frank"));
            doc.Add(new FacetField("Publish Date", "1999", "5", "5"));
            writer.AddDocument(config.Build(taxoWriter, doc));

            // NRT open
            IndexSearcher searcher = NewSearcher(writer.GetReader());

            // NRT open
            var taxoReader = new DirectoryTaxonomyReader(taxoWriter);

            // Aggregate the facet counts:
            FacetsCollector c = new FacetsCollector();

            // MatchAllDocsQuery is for "browsing" (counts facets
            // for all non-deleted docs in the index); normally
            // you'd use a "normal" query, and use MultiCollector to
            // wrap collecting the "normal" hits and also facets:
            searcher.Search(new MatchAllDocsQuery(), c);

            Facets facets = new FastTaxonomyFacetCounts(taxoReader, config, c);

            // Retrieve & verify results:
            Assert.AreEqual("dim=Publish Date path=[] value=5 childCount=3\n  2010 (2)\n  2012 (2)\n  1999 (1)\n", facets.GetTopChildren(10, "Publish Date").ToString());
            Assert.AreEqual("dim=Author path=[] value=5 childCount=4\n  Lisa (2)\n  Bob (1)\n  Susan (1)\n  Frank (1)\n", facets.GetTopChildren(10, "Author").ToString());

            // Now user drills down on Publish Date/2010:
            DrillDownQuery q2 = new DrillDownQuery(config);
            q2.Add("Publish Date", "2010");
            c = new FacetsCollector();
            searcher.Search(q2, c);
            facets = new FastTaxonomyFacetCounts(taxoReader, config, c);
            Assert.AreEqual("dim=Author path=[] value=2 childCount=2\n  Bob (1)\n  Lisa (1)\n", facets.GetTopChildren(10, "Author").ToString());

            Assert.AreEqual(1, facets.GetSpecificValue("Author", "Lisa"));

            Assert.IsNull(facets.GetTopChildren(10, "Non exitent dim"));

            // Smoke test PrintTaxonomyStats:
            string result;
            using (ByteArrayOutputStream bos = new ByteArrayOutputStream())
            {
                using (StreamWriter w = new StreamWriter(bos, Encoding.UTF8, 2048, true) { AutoFlush = true })
                {
                    PrintTaxonomyStats.PrintStats(taxoReader, w, true);
                }
                result = bos.ToString();
            }
            Assert.IsTrue(result.IndexOf("/Author: 4 immediate children; 5 total categories", StringComparison.Ordinal) != -1);
            Assert.IsTrue(result.IndexOf("/Publish Date: 3 immediate children; 12 total categories", StringComparison.Ordinal) != -1);
            // Make sure at least a few nodes of the tree came out:
            Assert.IsTrue(result.IndexOf("  /1999", StringComparison.Ordinal) != -1);
            Assert.IsTrue(result.IndexOf("  /2012", StringComparison.Ordinal) != -1);
            Assert.IsTrue(result.IndexOf("      /20", StringComparison.Ordinal) != -1);

            IOUtils.Dispose(writer, taxoWriter, searcher.IndexReader, taxoReader, taxoDir, dir);
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
            doc.Add(new FacetField("a", "foo1"));
            writer.AddDocument(config.Build(taxoWriter, doc));

            if (Random.NextBoolean())
            {
                writer.Commit();
            }

            doc = new Document();
            doc.Add(new FacetField("a", "foo2"));
            doc.Add(new FacetField("b", "bar1"));
            writer.AddDocument(config.Build(taxoWriter, doc));

            if (Random.NextBoolean())
            {
                writer.Commit();
            }

            doc = new Document();
            doc.Add(new FacetField("a", "foo3"));
            doc.Add(new FacetField("b", "bar2"));
            doc.Add(new FacetField("c", "baz1"));
            writer.AddDocument(config.Build(taxoWriter, doc));

            // NRT open
            IndexSearcher searcher = NewSearcher(writer.GetReader());

            // NRT open
            var taxoReader = new DirectoryTaxonomyReader(taxoWriter);

            FacetsCollector c = new FacetsCollector();
            searcher.Search(new MatchAllDocsQuery(), c);

            Facets facets = GetTaxonomyFacetCounts(taxoReader, new FacetsConfig(), c);

            // Ask for top 10 labels for any dims that have counts:
            IList<FacetResult> results = facets.GetAllDims(10);

            Assert.AreEqual(3, results.Count);
            Assert.AreEqual("dim=a path=[] value=3 childCount=3\n  foo1 (1)\n  foo2 (1)\n  foo3 (1)\n", results[0].ToString());
            Assert.AreEqual("dim=b path=[] value=2 childCount=2\n  bar1 (1)\n  bar2 (1)\n", results[1].ToString());
            Assert.AreEqual("dim=c path=[] value=1 childCount=1\n  baz1 (1)\n", results[2].ToString());

            IOUtils.Dispose(writer, taxoWriter, searcher.IndexReader, taxoReader, taxoDir, dir);
        }

        [Test]
        public virtual void TestWrongIndexFieldName()
        {
            Store.Directory dir = NewDirectory();
            Store.Directory taxoDir = NewDirectory();

            // Writes facet ords to a separate directory from the
            // main index:
            DirectoryTaxonomyWriter taxoWriter = new DirectoryTaxonomyWriter(taxoDir, OpenMode.CREATE);

            FacetsConfig config = new FacetsConfig();
            config.SetIndexFieldName("a", "$facets2");
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);

            Document doc = new Document();
            doc.Add(new FacetField("a", "foo1"));
            writer.AddDocument(config.Build(taxoWriter, doc));

            // NRT open
            IndexSearcher searcher = NewSearcher(writer.GetReader());

            // NRT open
            var taxoReader = new DirectoryTaxonomyReader(taxoWriter);

            FacetsCollector c = new FacetsCollector();
            searcher.Search(new MatchAllDocsQuery(), c);

            // Uses default $facets field:
            Facets facets;
            if (Random.NextBoolean())
            {
                facets = new FastTaxonomyFacetCounts(taxoReader, config, c);
            }
            else
            {
                OrdinalsReader ordsReader = new DocValuesOrdinalsReader();
                if (Random.NextBoolean())
                {
                    ordsReader = new CachedOrdinalsReader(ordsReader);
                }
                facets = new TaxonomyFacetCounts(ordsReader, taxoReader, config, c);
            }

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

            IOUtils.Dispose(writer, taxoWriter, searcher.IndexReader, taxoReader, taxoDir, dir);
        }

        [Test]
        public virtual void TestReallyNoNormsForDrillDown()
        {
            Store.Directory dir = NewDirectory();
            Store.Directory taxoDir = NewDirectory();
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            iwc.SetSimilarity(new PerFieldSimilarityWrapperAnonymousClass(this));
            ITaxonomyWriter taxoWriter = new DirectoryTaxonomyWriter(taxoDir, OpenMode.CREATE);
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir, iwc);
            FacetsConfig config = new FacetsConfig();

            Document doc = new Document();
            doc.Add(NewTextField("field", "text", Field.Store.NO));
            doc.Add(new FacetField("a", "path"));
            writer.AddDocument(config.Build(taxoWriter, doc));
            IOUtils.Dispose(writer, taxoWriter, dir, taxoDir);
        }

        private sealed class PerFieldSimilarityWrapperAnonymousClass : PerFieldSimilarityWrapper
        {
            private readonly TestTaxonomyFacetCounts outerInstance;

            public PerFieldSimilarityWrapperAnonymousClass(TestTaxonomyFacetCounts outerInstance)
            {
                this.outerInstance = outerInstance;
                sim = new DefaultSimilarity();
            }

            private readonly Similarity sim;

            public override Similarity Get(string name)
            {
                Assert.AreEqual("field", name);
                return sim;
            }
        }

        [Test]
        public virtual void TestMultiValuedHierarchy()
        {
            Store.Directory dir = NewDirectory();
            Store.Directory taxoDir = NewDirectory();
            DirectoryTaxonomyWriter taxoWriter = new DirectoryTaxonomyWriter(taxoDir, OpenMode.CREATE);
            FacetsConfig config = new FacetsConfig();
            config.SetHierarchical("a", true);
            config.SetMultiValued("a", true);
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);

            Document doc = new Document();
            doc.Add(NewTextField("field", "text", Field.Store.NO));
            doc.Add(new FacetField("a", "path", "x"));
            doc.Add(new FacetField("a", "path", "y"));
            writer.AddDocument(config.Build(taxoWriter, doc));

            // NRT open
            IndexSearcher searcher = NewSearcher(writer.GetReader());

            // NRT open
            var taxoReader = new DirectoryTaxonomyReader(taxoWriter);

            // Aggregate the facet counts:
            FacetsCollector c = new FacetsCollector();

            // MatchAllDocsQuery is for "browsing" (counts facets
            // for all non-deleted docs in the index); normally
            // you'd use a "normal" query, and use MultiCollector to
            // wrap collecting the "normal" hits and also facets:
            searcher.Search(new MatchAllDocsQuery(), c);
            Facets facets = GetTaxonomyFacetCounts(taxoReader, config, c);

            try
            {
                facets.GetSpecificValue("a");
                fail("didn't hit expected exception");
            }
            catch (Exception iae) when (iae.IsIllegalArgumentException())
            {
                // expected
            }

            FacetResult result = facets.GetTopChildren(10, "a");
            Assert.AreEqual(1, result.LabelValues.Length);
            Assert.AreEqual(1, (int)result.LabelValues[0].Value);

            IOUtils.Dispose(writer, taxoWriter, searcher.IndexReader, taxoReader, dir, taxoDir);
        }

        [Test]
        public virtual void TestLabelWithDelimiter()
        {
            Store.Directory dir = NewDirectory();
            Store.Directory taxoDir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);
            var taxoWriter = new DirectoryTaxonomyWriter(taxoDir, OpenMode.CREATE);

            FacetsConfig config = new FacetsConfig();
            config.SetMultiValued("dim", true);

            Document doc = new Document();
            doc.Add(NewTextField("field", "text", Field.Store.NO));
            doc.Add(new FacetField("dim", "test\u001Fone"));
            doc.Add(new FacetField("dim", "test\u001Etwo"));
            writer.AddDocument(config.Build(taxoWriter, doc));

            // NRT open
            IndexSearcher searcher = NewSearcher(writer.GetReader());

            // NRT open
            var taxoReader = new DirectoryTaxonomyReader(taxoWriter);

            FacetsCollector c = new FacetsCollector();
            searcher.Search(new MatchAllDocsQuery(), c);

            Facets facets = GetTaxonomyFacetCounts(taxoReader, config, c);
            Assert.AreEqual(1, facets.GetSpecificValue("dim", "test\u001Fone"));
            Assert.AreEqual(1, facets.GetSpecificValue("dim", "test\u001Etwo"));

            FacetResult result = facets.GetTopChildren(10, "dim");
            Assert.AreEqual("dim=dim path=[] value=-1 childCount=2\n  test\u001Fone (1)\n  test\u001Etwo (1)\n", result.ToString(CultureInfo.InvariantCulture));
            IOUtils.Dispose(writer, taxoWriter, searcher.IndexReader, taxoReader, dir, taxoDir);
        }

        [Test]
        public virtual void TestRequireDimCount()
        {
            Store.Directory dir = NewDirectory();
            Store.Directory taxoDir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);
            DirectoryTaxonomyWriter taxoWriter = new DirectoryTaxonomyWriter(taxoDir, OpenMode.CREATE);

            FacetsConfig config = new FacetsConfig();
            config.SetRequireDimCount("dim", true);

            config.SetMultiValued("dim2", true);
            config.SetRequireDimCount("dim2", true);

            config.SetMultiValued("dim3", true);
            config.SetHierarchical("dim3", true);
            config.SetRequireDimCount("dim3", true);

            Document doc = new Document();
            doc.Add(NewTextField("field", "text", Field.Store.NO));
            doc.Add(new FacetField("dim", "a"));
            doc.Add(new FacetField("dim2", "a"));
            doc.Add(new FacetField("dim2", "b"));
            doc.Add(new FacetField("dim3", "a", "b"));
            doc.Add(new FacetField("dim3", "a", "c"));
            writer.AddDocument(config.Build(taxoWriter, doc));

            // NRT open
            IndexSearcher searcher = NewSearcher(writer.GetReader());

            // NRT open
            var taxoReader = new DirectoryTaxonomyReader(taxoWriter);

            FacetsCollector c = new FacetsCollector();
            searcher.Search(new MatchAllDocsQuery(), c);

            Facets facets = GetTaxonomyFacetCounts(taxoReader, config, c);
            Assert.AreEqual(1, facets.GetTopChildren(10, "dim").Value);
            Assert.AreEqual(1, facets.GetTopChildren(10, "dim2").Value);
            Assert.AreEqual(1, facets.GetTopChildren(10, "dim3").Value);
            try
            {
                Assert.AreEqual(1, facets.GetSpecificValue("dim"));
                fail("didn't hit expected exception");
            }
            catch (Exception iae) when (iae.IsIllegalArgumentException())
            {
                // expected
            }
            Assert.AreEqual(1, facets.GetSpecificValue("dim2"));
            Assert.AreEqual(1, facets.GetSpecificValue("dim3"));
            IOUtils.Dispose(writer, taxoWriter, searcher.IndexReader, taxoReader, dir, taxoDir);
        }

        // LUCENE-4583: make sure if we require > 32 KB for one
        // document, we don't hit exc when using Facet42DocValuesFormat
        [Test]
        public virtual void TestManyFacetsInOneDocument()
        {
            AssumeTrue("default Codec doesn't support huge BinaryDocValues", TestUtil.FieldSupportsHugeBinaryDocValues(FacetsConfig.DEFAULT_INDEX_FIELD_NAME));
            Store.Directory dir = NewDirectory();
            Store.Directory taxoDir = NewDirectory();
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir, iwc);
            var taxoWriter = new DirectoryTaxonomyWriter(taxoDir, OpenMode.CREATE);

            FacetsConfig config = new FacetsConfig();
            config.SetMultiValued("dim", true);

            int numLabels = TestUtil.NextInt32(Random, 40000, 100000);

            Document doc = new Document();
            doc.Add(NewTextField("field", "text", Field.Store.NO));
            for (int i = 0; i < numLabels; i++)
            {
                doc.Add(new FacetField("dim", "" + i));
            }
            writer.AddDocument(config.Build(taxoWriter, doc));

            // NRT open
            IndexSearcher searcher = NewSearcher(writer.GetReader());

            // NRT open
            var taxoReader = new DirectoryTaxonomyReader(taxoWriter);

            // Aggregate the facet counts:
            FacetsCollector c = new FacetsCollector();

            // MatchAllDocsQuery is for "browsing" (counts facets
            // for all non-deleted docs in the index); normally
            // you'd use a "normal" query, and use MultiCollector to
            // wrap collecting the "normal" hits and also facets:
            searcher.Search(new MatchAllDocsQuery(), c);
            Facets facets = GetTaxonomyFacetCounts(taxoReader, config, c);

            FacetResult result = facets.GetTopChildren(int.MaxValue, "dim");
            Assert.AreEqual(numLabels, result.LabelValues.Length);
            var allLabels = new JCG.HashSet<string>();
            foreach (LabelAndValue labelValue in result.LabelValues)
            {
                allLabels.Add(labelValue.Label);
                Assert.AreEqual(1, (int)labelValue.Value);
            }
            Assert.AreEqual(numLabels, allLabels.Count);

            IOUtils.Dispose(searcher.IndexReader, taxoWriter, writer, taxoReader, dir, taxoDir);
        }

        // Make sure we catch when app didn't declare field as
        // hierarchical but it was:
        [Test]
        public virtual void TestDetectHierarchicalField()
        {
            Store.Directory dir = NewDirectory();
            Store.Directory taxoDir = NewDirectory();
            var taxoWriter = new DirectoryTaxonomyWriter(taxoDir, OpenMode.CREATE);
            var writer = new RandomIndexWriter(Random, dir);
            FacetsConfig config = new FacetsConfig();

            Document doc = new Document();
            doc.Add(NewTextField("field", "text", Field.Store.NO));
            doc.Add(new FacetField("a", "path", "other"));
            try
            {
                config.Build(taxoWriter, doc);
                fail("did not hit expected exception");
            }
            catch (Exception iae) when (iae.IsIllegalArgumentException())
            {
                // expected
            }
            IOUtils.Dispose(writer, taxoWriter, dir, taxoDir);
        }

        // Make sure we catch when app didn't declare field as
        // multi-valued but it was:
        [Test]
        public virtual void TestDetectMultiValuedField()
        {
            Store.Directory dir = NewDirectory();
            Store.Directory taxoDir = NewDirectory();
            ITaxonomyWriter taxoWriter = new DirectoryTaxonomyWriter(taxoDir, OpenMode.CREATE);
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);
            FacetsConfig config = new FacetsConfig();

            Document doc = new Document();
            doc.Add(NewTextField("field", "text", Field.Store.NO));
            doc.Add(new FacetField("a", "path"));
            doc.Add(new FacetField("a", "path2"));
            try
            {
                config.Build(taxoWriter, doc);
                fail("did not hit expected exception");
            }
            catch (Exception iae) when (iae.IsIllegalArgumentException())
            {
                // expected
            }
            IOUtils.Dispose(writer, taxoWriter, dir, taxoDir);
        }

        [Test]
        public virtual void TestSeparateIndexedFields()
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

            FacetsCollector sfc = new FacetsCollector();
            NewSearcher(r).Search(new MatchAllDocsQuery(), sfc);
            Facets facets1 = GetTaxonomyFacetCounts(taxoReader, config, sfc);
            Facets facets2 = GetTaxonomyFacetCounts(taxoReader, config, sfc, "$b");
            Assert.AreEqual(r.MaxDoc, (int)facets1.GetTopChildren(10, "a").Value);
            Assert.AreEqual(r.MaxDoc, (int)facets2.GetTopChildren(10, "b").Value);
            IOUtils.Dispose(taxoWriter, iw, taxoReader, taxoDir, r, indexDir);
        }

        [Test]
        public virtual void TestCountRoot()
        {
            // LUCENE-4882: FacetsAccumulator threw NPE if a FacetRequest was defined on CP.EMPTY
            Store.Directory indexDir = NewDirectory();
            Store.Directory taxoDir = NewDirectory();

            var taxoWriter = new DirectoryTaxonomyWriter(taxoDir);
            IndexWriter iw = new IndexWriter(indexDir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            FacetsConfig config = new FacetsConfig();
            for (int i = AtLeast(30); i > 0; --i)
            {
                Document doc = new Document();
                doc.Add(new FacetField("a", "1"));
                doc.Add(new FacetField("b", "1"));
                iw.AddDocument(config.Build(taxoWriter, doc));
            }

            DirectoryReader r = DirectoryReader.Open(iw, true);
            DirectoryTaxonomyReader taxoReader = new DirectoryTaxonomyReader(taxoWriter);

            FacetsCollector sfc = new FacetsCollector();
            NewSearcher(r).Search(new MatchAllDocsQuery(), sfc);
            Facets facets = GetTaxonomyFacetCounts(taxoReader, config, sfc);
            foreach (FacetResult result in facets.GetAllDims(10))
            {
                Assert.AreEqual(r.NumDocs, (int)result.Value);
            }

            IOUtils.Dispose(taxoWriter, iw, taxoReader, taxoDir, r, indexDir);
        }

        [Test]
        public virtual void TestGetFacetResultsTwice()
        {
            // LUCENE-4893: counts were multiplied as many times as getFacetResults was called.
            Store.Directory indexDir = NewDirectory();
            Store.Directory taxoDir = NewDirectory();

            var taxoWriter = new DirectoryTaxonomyWriter(taxoDir);
            IndexWriter iw = new IndexWriter(indexDir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            FacetsConfig config = new FacetsConfig();

            Document doc = new Document();
            doc.Add(new FacetField("a", "1"));
            doc.Add(new FacetField("b", "1"));
            iw.AddDocument(config.Build(taxoWriter, doc));

            DirectoryReader r = DirectoryReader.Open(iw, true);
            var taxoReader = new DirectoryTaxonomyReader(taxoWriter);

            FacetsCollector sfc = new FacetsCollector();
            NewSearcher(r).Search(new MatchAllDocsQuery(), sfc);

            Facets facets = GetTaxonomyFacetCounts(taxoReader, config, sfc);
            IList<FacetResult> res1 = facets.GetAllDims(10);
            IList<FacetResult> res2 = facets.GetAllDims(10);
            Assert.AreEqual(res1, res2, aggressive: false, "calling getFacetResults twice should return the .equals()=true result");

            IOUtils.Dispose(taxoWriter, iw, taxoReader, taxoDir, r, indexDir);
        }

        [Test]
        public virtual void TestChildCount()
        {
            // LUCENE-4885: FacetResult.numValidDescendants was not set properly by FacetsAccumulator
            var indexDir = NewDirectory();
            var taxoDir = NewDirectory();

            var taxoWriter = new DirectoryTaxonomyWriter(taxoDir);
            IndexWriter iw = new IndexWriter(indexDir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            FacetsConfig config = new FacetsConfig();
            for (int i = 0; i < 10; i++)
            {
                Document doc = new Document();
                doc.Add(new FacetField("a", Convert.ToString(i, CultureInfo.InvariantCulture)));
                iw.AddDocument(config.Build(taxoWriter, doc));
            }

            DirectoryReader r = DirectoryReader.Open(iw, true);
            DirectoryTaxonomyReader taxoReader = new DirectoryTaxonomyReader(taxoWriter);

            FacetsCollector sfc = new FacetsCollector();
            NewSearcher(r).Search(new MatchAllDocsQuery(), sfc);
            Facets facets = GetTaxonomyFacetCounts(taxoReader, config, sfc);

            Assert.AreEqual(10, facets.GetTopChildren(2, "a").ChildCount);

            IOUtils.Dispose(taxoWriter, iw, taxoReader, taxoDir, r, indexDir);
        }

        private void indexTwoDocs(ITaxonomyWriter taxoWriter, IndexWriter indexWriter, FacetsConfig config, bool withContent)
        {
            for (int i = 0; i < 2; i++)
            {
                Document doc = new Document();
                if (withContent)
                {
                    doc.Add(new StringField("f", "a", Field.Store.NO));
                }
                if (config != null)
                {
                    doc.Add(new FacetField("A", Convert.ToString(i, CultureInfo.InvariantCulture)));
                    indexWriter.AddDocument(config.Build(taxoWriter, doc));
                }
                else
                {
                    indexWriter.AddDocument(doc);
                }
            }

            indexWriter.Commit();
        }

        [Test]
        public virtual void TestSegmentsWithoutCategoriesOrResults()
        {
            // tests the accumulator when there are segments with no results
            var indexDir = NewDirectory();
            var taxoDir = NewDirectory();

            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            //iwc.MergePolicy = NoMergePolicy.INSTANCE; // prevent merges
            IndexWriter indexWriter = new IndexWriter(indexDir, iwc);

            var taxoWriter = new DirectoryTaxonomyWriter(taxoDir);
            FacetsConfig config = new FacetsConfig();
            indexTwoDocs(taxoWriter, indexWriter, config, false); // 1st segment, no content, with categories
            indexTwoDocs(taxoWriter, indexWriter, null, true); // 2nd segment, with content, no categories
            indexTwoDocs(taxoWriter, indexWriter, config, true); // 3rd segment ok
            indexTwoDocs(taxoWriter, indexWriter, null, false); // 4th segment, no content, or categories
            indexTwoDocs(taxoWriter, indexWriter, null, true); // 5th segment, with content, no categories
            indexTwoDocs(taxoWriter, indexWriter, config, true); // 6th segment, with content, with categories
            indexTwoDocs(taxoWriter, indexWriter, null, true); // 7th segment, with content, no categories
            IOUtils.Dispose(indexWriter, taxoWriter);

            DirectoryReader indexReader = DirectoryReader.Open(indexDir);
            var taxoReader = new DirectoryTaxonomyReader(taxoDir);
            IndexSearcher indexSearcher = NewSearcher(indexReader);

            // search for "f:a", only segments 1 and 3 should match results
            Query q = new TermQuery(new Term("f", "a"));
            FacetsCollector sfc = new FacetsCollector();
            indexSearcher.Search(q, sfc);
            Facets facets = GetTaxonomyFacetCounts(taxoReader, config, sfc);
            FacetResult result = facets.GetTopChildren(10, "A");
            Assert.AreEqual(2, result.LabelValues.Length, "wrong number of children");
            foreach (LabelAndValue labelValue in result.LabelValues)
            {
                Assert.AreEqual(2, (int)labelValue.Value, "wrong weight for child " + labelValue.Label);
            }

            IOUtils.Dispose(indexReader, taxoReader, indexDir, taxoDir);
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
                Facets facets = GetTaxonomyFacetCounts(tr, config, fc);

                // Slow, yet hopefully bug-free, faceting:
                var expectedCounts = new JCG.List<Dictionary<string, int>>();
                for (int i = 0; i < numDims; i++)
                {
                    expectedCounts.Add(new Dictionary<string, int>());
                }

                foreach (TestDoc doc in testDocs)
                {
                    if (doc.content.Equals(searchToken, StringComparison.Ordinal))
                    {
                        for (int j = 0; j < numDims; j++)
                        {
                            if (doc.dims[j] != null)
                            {
                                if (!expectedCounts[j].TryGetValue(doc.dims[j], out int v))
                                {
                                    expectedCounts[j][doc.dims[j]] = 1;
                                }
                                else
                                {
                                    expectedCounts[j][doc.dims[j]] = (int)v + 1;
                                }
                            }
                        }
                    }
                }

                JCG.List<FacetResult> expected = new JCG.List<FacetResult>();
                for (int i = 0; i < numDims; i++)
                {
                    JCG.List<LabelAndValue> labelValues = new JCG.List<LabelAndValue>();
                    int totCount = 0;
                    foreach (KeyValuePair<string, int> ent in expectedCounts[i])
                    {
                        labelValues.Add(new LabelAndValue(ent.Key, ent.Value));
                        totCount += ent.Value;
                    }
                    SortLabelValues(labelValues);
                    if (totCount > 0)
                    {
                        expected.Add(new FacetResult("dim" + i, new string[0], totCount, labelValues.ToArray(), labelValues.Count));
                    }
                }

                // Sort by highest value, tie break by value:
                SortFacetResults(expected);

                IList<FacetResult> actual = facets.GetAllDims(10);

                // Messy: fixup ties
                SortTies(actual);

                Assert.AreEqual(expected, actual, aggressive: false);
            }

            IOUtils.Dispose(w, tw, searcher.IndexReader, tr, indexDir, taxoDir);
        }
    }
}