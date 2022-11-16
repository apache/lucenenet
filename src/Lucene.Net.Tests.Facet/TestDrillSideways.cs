// Lucene version compatibility level 4.8.1 + LUCENE-6001
using J2N.Collections.Generic.Extensions;
using J2N.Text;
using Lucene.Net.Diagnostics;
using Lucene.Net.Search;
using Lucene.Net.Support;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Assert = Lucene.Net.TestFramework.Assert;
using Console = Lucene.Net.Util.SystemConsole;
using JCG = J2N.Collections.Generic;

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

    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using DefaultSortedSetDocValuesReaderState = Lucene.Net.Facet.SortedSet.DefaultSortedSetDocValuesReaderState;
    using Directory = Lucene.Net.Store.Directory;
    using DirectoryTaxonomyReader = Lucene.Net.Facet.Taxonomy.Directory.DirectoryTaxonomyReader;
    using DirectoryTaxonomyWriter = Lucene.Net.Facet.Taxonomy.Directory.DirectoryTaxonomyWriter;
    using DocIdSet = Lucene.Net.Search.DocIdSet;
    using Document = Lucene.Net.Documents.Document;
    using Field = Lucene.Net.Documents.Field;
    using Filter = Lucene.Net.Search.Filter;
    using FixedBitSet = Lucene.Net.Util.FixedBitSet;
    using IBits = Lucene.Net.Util.IBits;
    using ICollector = Lucene.Net.Search.ICollector;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using IndexSearcher = Lucene.Net.Search.IndexSearcher;
    using IndexWriterConfig = Lucene.Net.Index.IndexWriterConfig;
    using InfoStream = Lucene.Net.Util.InfoStream;
    using InPlaceMergeSorter = Lucene.Net.Util.InPlaceMergeSorter;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using MatchAllDocsQuery = Lucene.Net.Search.MatchAllDocsQuery;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using OpenMode = Lucene.Net.Index.OpenMode;
    using Query = Lucene.Net.Search.Query;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using ScoreDoc = Lucene.Net.Search.ScoreDoc;
    using Scorer = Lucene.Net.Search.Scorer;
    using Sort = Lucene.Net.Search.Sort;
    using SortedSetDocValuesFacetField = Lucene.Net.Facet.SortedSet.SortedSetDocValuesFacetField;
    using SortedSetDocValuesReaderState = Lucene.Net.Facet.SortedSet.SortedSetDocValuesReaderState;
    using SortField = Lucene.Net.Search.SortField;
    using StringField = Lucene.Net.Documents.StringField;
    using TaxonomyReader = Lucene.Net.Facet.Taxonomy.TaxonomyReader;
    using Term = Lucene.Net.Index.Term;
    using TermQuery = Lucene.Net.Search.TermQuery;
    using TestUtil = Lucene.Net.Util.TestUtil;
    using TopDocs = Lucene.Net.Search.TopDocs;


    [TestFixture]
    public class TestDrillSideways : FacetTestCase
    {

        [Test]
        public virtual void TestBasic()
        {
            Directory dir = NewDirectory();
            Directory taxoDir = NewDirectory();

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

            //System.out.println("searcher=" + searcher);

            // NRT open
            var taxoReader = new DirectoryTaxonomyReader(taxoWriter);

            DrillSideways ds = new DrillSideways(searcher, config, taxoReader);

            //  case: drill-down on a single field; in this
            // case the drill-sideways + drill-down counts ==
            // drill-down of just the query: 
            DrillDownQuery ddq = new DrillDownQuery(config);
            ddq.Add("Author", "Lisa");
            DrillSidewaysResult r = ds.Search(null, ddq, 10);
            Assert.AreEqual(2, r.Hits.TotalHits);
            // Publish Date is only drill-down, and Lisa published
            // one in 2012 and one in 2010:
            Assert.AreEqual("dim=Publish Date path=[] value=2 childCount=2\n  2010 (1)\n  2012 (1)\n", r.Facets.GetTopChildren(10, "Publish Date").ToString());

            // Author is drill-sideways + drill-down: Lisa
            // (drill-down) published twice, and Frank/Susan/Bob
            // published once:
            Assert.AreEqual("dim=Author path=[] value=5 childCount=4\n  Lisa (2)\n  Bob (1)\n  Susan (1)\n  Frank (1)\n", r.Facets.GetTopChildren(10, "Author").ToString());

            // Same simple case, but no baseQuery (pure browse):
            // drill-down on a single field; in this case the
            // drill-sideways + drill-down counts == drill-down of
            // just the query:
            ddq = new DrillDownQuery(config);
            ddq.Add("Author", "Lisa");
            r = ds.Search(null, ddq, 10);

            Assert.AreEqual(2, r.Hits.TotalHits);
            // Publish Date is only drill-down, and Lisa published
            // one in 2012 and one in 2010:
            Assert.AreEqual("dim=Publish Date path=[] value=2 childCount=2\n  2010 (1)\n  2012 (1)\n", r.Facets.GetTopChildren(10, "Publish Date").ToString());

            // Author is drill-sideways + drill-down: Lisa
            // (drill-down) published twice, and Frank/Susan/Bob
            // published once:
            Assert.AreEqual("dim=Author path=[] value=5 childCount=4\n  Lisa (2)\n  Bob (1)\n  Susan (1)\n  Frank (1)\n", r.Facets.GetTopChildren(10, "Author").ToString());

            // Another simple case: drill-down on single fields
            // but OR of two values
            ddq = new DrillDownQuery(config);
            ddq.Add("Author", "Lisa");
            ddq.Add("Author", "Bob");
            r = ds.Search(null, ddq, 10);
            Assert.AreEqual(3, r.Hits.TotalHits);
            // Publish Date is only drill-down: Lisa and Bob
            // (drill-down) published twice in 2010 and once in 2012:
            Assert.AreEqual("dim=Publish Date path=[] value=3 childCount=2\n  2010 (2)\n  2012 (1)\n", r.Facets.GetTopChildren(10, "Publish Date").ToString());
            // Author is drill-sideways + drill-down: Lisa
            // (drill-down) published twice, and Frank/Susan/Bob
            // published once:
            Assert.AreEqual("dim=Author path=[] value=5 childCount=4\n  Lisa (2)\n  Bob (1)\n  Susan (1)\n  Frank (1)\n", r.Facets.GetTopChildren(10, "Author").ToString());

            Assert.IsTrue(r.Facets is MultiFacets);
            IList<FacetResult> allResults = r.Facets.GetAllDims(10);
            Assert.AreEqual(2, allResults.Count);
            Assert.AreEqual("dim=Author path=[] value=5 childCount=4\n  Lisa (2)\n  Bob (1)\n  Susan (1)\n  Frank (1)\n", allResults[0].ToString());
            Assert.AreEqual("dim=Publish Date path=[] value=3 childCount=2\n  2010 (2)\n  2012 (1)\n", allResults[1].ToString());

            // More interesting case: drill-down on two fields
            ddq = new DrillDownQuery(config);
            ddq.Add("Author", "Lisa");
            ddq.Add("Publish Date", "2010");
            r = ds.Search(null, ddq, 10);
            Assert.AreEqual(1, r.Hits.TotalHits);
            // Publish Date is drill-sideways + drill-down: Lisa
            // (drill-down) published once in 2010 and once in 2012:
            Assert.AreEqual("dim=Publish Date path=[] value=2 childCount=2\n  2010 (1)\n  2012 (1)\n", r.Facets.GetTopChildren(10, "Publish Date").ToString());
            // Author is drill-sideways + drill-down:
            // only Lisa & Bob published (once each) in 2010:
            Assert.AreEqual("dim=Author path=[] value=2 childCount=2\n  Bob (1)\n  Lisa (1)\n", r.Facets.GetTopChildren(10, "Author").ToString());

            // Even more interesting case: drill down on two fields,
            // but one of them is OR
            ddq = new DrillDownQuery(config);

            // Drill down on Lisa or Bob:
            ddq.Add("Author", "Lisa");
            ddq.Add("Publish Date", "2010");
            ddq.Add("Author", "Bob");
            r = ds.Search(null, ddq, 10);
            Assert.AreEqual(2, r.Hits.TotalHits);
            // Publish Date is both drill-sideways + drill-down:
            // Lisa or Bob published twice in 2010 and once in 2012:
            Assert.AreEqual("dim=Publish Date path=[] value=3 childCount=2\n  2010 (2)\n  2012 (1)\n", r.Facets.GetTopChildren(10, "Publish Date").ToString());
            // Author is drill-sideways + drill-down:
            // only Lisa & Bob published (once each) in 2010:
            Assert.AreEqual("dim=Author path=[] value=2 childCount=2\n  Bob (1)\n  Lisa (1)\n", r.Facets.GetTopChildren(10, "Author").ToString());

            // Test drilling down on invalid field:
            ddq = new DrillDownQuery(config);
            ddq.Add("Foobar", "Baz");
            r = ds.Search(null, ddq, 10);
            Assert.AreEqual(0, r.Hits.TotalHits);
            Assert.IsNull(r.Facets.GetTopChildren(10, "Publish Date"));
            Assert.IsNull(r.Facets.GetTopChildren(10, "Foobar"));

            // Test drilling down on valid term or'd with invalid term:
            ddq = new DrillDownQuery(config);
            ddq.Add("Author", "Lisa");
            ddq.Add("Author", "Tom");
            r = ds.Search(null, ddq, 10);
            Assert.AreEqual(2, r.Hits.TotalHits);
            // Publish Date is only drill-down, and Lisa published
            // one in 2012 and one in 2010:
            Assert.AreEqual("dim=Publish Date path=[] value=2 childCount=2\n  2010 (1)\n  2012 (1)\n", r.Facets.GetTopChildren(10, "Publish Date").ToString());
            // Author is drill-sideways + drill-down: Lisa
            // (drill-down) published twice, and Frank/Susan/Bob
            // published once:
            Assert.AreEqual("dim=Author path=[] value=5 childCount=4\n  Lisa (2)\n  Bob (1)\n  Susan (1)\n  Frank (1)\n", r.Facets.GetTopChildren(10, "Author").ToString());

            // LUCENE-4915: test drilling down on a dimension but
            // NOT facet counting it:
            ddq = new DrillDownQuery(config);
            ddq.Add("Author", "Lisa");
            ddq.Add("Author", "Tom");
            r = ds.Search(null, ddq, 10);
            Assert.AreEqual(2, r.Hits.TotalHits);
            // Publish Date is only drill-down, and Lisa published
            // one in 2012 and one in 2010:
            Assert.AreEqual("dim=Publish Date path=[] value=2 childCount=2\n  2010 (1)\n  2012 (1)\n", r.Facets.GetTopChildren(10, "Publish Date").ToString());

            // Test main query gets null scorer:
            ddq = new DrillDownQuery(config, new TermQuery(new Term("foobar", "baz")));
            ddq.Add("Author", "Lisa");
            r = ds.Search(null, ddq, 10);

            Assert.AreEqual(0, r.Hits.TotalHits);
            Assert.IsNull(r.Facets.GetTopChildren(10, "Publish Date"));
            Assert.IsNull(r.Facets.GetTopChildren(10, "Author"));
            IOUtils.Dispose(searcher.IndexReader, taxoReader, writer, taxoWriter, dir, taxoDir);
        }

        [Test]
        public virtual void TestSometimesInvalidDrillDown()
        {
            Directory dir = NewDirectory();
            Directory taxoDir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);

            // Writes facet ords to a separate directory from the
            // main index:
            DirectoryTaxonomyWriter taxoWriter = new DirectoryTaxonomyWriter(taxoDir, OpenMode.CREATE);

            FacetsConfig config = new FacetsConfig();
            config.SetHierarchical("Publish Date", true);

            Document doc = new Document();
            doc.Add(new FacetField("Author", "Bob"));
            doc.Add(new FacetField("Publish Date", "2010", "10", "15"));
            writer.AddDocument(config.Build(taxoWriter, doc));

            doc = new Document();
            doc.Add(new FacetField("Author", "Lisa"));
            doc.Add(new FacetField("Publish Date", "2010", "10", "20"));
            writer.AddDocument(config.Build(taxoWriter, doc));

            writer.Commit();

            // 2nd segment has no Author:
            doc = new Document();
            doc.Add(new FacetField("Foobar", "Lisa"));
            doc.Add(new FacetField("Publish Date", "2012", "1", "1"));
            writer.AddDocument(config.Build(taxoWriter, doc));

            // NRT open
            IndexSearcher searcher = NewSearcher(writer.GetReader());

            //System.out.println("searcher=" + searcher);

            // NRT open
            var taxoReader = new DirectoryTaxonomyReader(taxoWriter);

            DrillDownQuery ddq = new DrillDownQuery(config);
            ddq.Add("Author", "Lisa");
            DrillSidewaysResult r = (new DrillSideways(searcher, config, taxoReader)).Search(null, ddq, 10);

            Assert.AreEqual(1, r.Hits.TotalHits);
            // Publish Date is only drill-down, and Lisa published
            // one in 2012 and one in 2010:
            Assert.AreEqual("dim=Publish Date path=[] value=1 childCount=1\n  2010 (1)\n", r.Facets.GetTopChildren(10, "Publish Date").ToString());
            // Author is drill-sideways + drill-down: Lisa
            // (drill-down) published once, and Bob
            // published once:
            Assert.AreEqual("dim=Author path=[] value=2 childCount=2\n  Bob (1)\n  Lisa (1)\n", r.Facets.GetTopChildren(10, "Author").ToString());

            IOUtils.Dispose(searcher.IndexReader, taxoReader, writer, taxoWriter, dir, taxoDir);
        }

        [Test]
        public virtual void TestMultipleRequestsPerDim()
        {
            Directory dir = NewDirectory();
            Directory taxoDir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);

            // Writes facet ords to a separate directory from the
            // main index:
            var taxoWriter = new DirectoryTaxonomyWriter(taxoDir, OpenMode.CREATE);

            FacetsConfig config = new FacetsConfig();
            config.SetHierarchical("dim", true);

            Document doc = new Document();
            doc.Add(new FacetField("dim", "a", "x"));
            writer.AddDocument(config.Build(taxoWriter, doc));

            doc = new Document();
            doc.Add(new FacetField("dim", "a", "y"));
            writer.AddDocument(config.Build(taxoWriter, doc));

            doc = new Document();
            doc.Add(new FacetField("dim", "a", "z"));
            writer.AddDocument(config.Build(taxoWriter, doc));

            doc = new Document();
            doc.Add(new FacetField("dim", "b"));
            writer.AddDocument(config.Build(taxoWriter, doc));

            doc = new Document();
            doc.Add(new FacetField("dim", "c"));
            writer.AddDocument(config.Build(taxoWriter, doc));

            doc = new Document();
            doc.Add(new FacetField("dim", "d"));
            writer.AddDocument(config.Build(taxoWriter, doc));

            // NRT open
            IndexSearcher searcher = NewSearcher(writer.GetReader());

            //System.out.println("searcher=" + searcher);

            // NRT open
            var taxoReader = new DirectoryTaxonomyReader(taxoWriter);

            DrillDownQuery ddq = new DrillDownQuery(config);
            ddq.Add("dim", "a");
            DrillSidewaysResult r = new DrillSideways(searcher, config, taxoReader).Search(null, ddq, 10);

            Assert.AreEqual(3, r.Hits.TotalHits);
            Assert.AreEqual("dim=dim path=[] value=6 childCount=4\n  a (3)\n  b (1)\n  c (1)\n  d (1)\n", r.Facets.GetTopChildren(10, "dim").ToString());
            Assert.AreEqual("dim=dim path=[a] value=3 childCount=3\n  x (1)\n  y (1)\n  z (1)\n", r.Facets.GetTopChildren(10, "dim", "a").ToString());

            IOUtils.Dispose(searcher.IndexReader, taxoReader, writer, taxoWriter, dir, taxoDir);
        }

        internal class Doc : IComparable<Doc>
        {
            internal string id;
            internal string contentToken;

            public Doc()
            {
            }

            // -1 if the doc is missing this dim, else the index
            // -into the values for this dim:
            internal int[] dims;

            // 2nd value per dim for the doc (so we test
            // multi-valued fields):
            internal int[] dims2;
            internal bool deleted;

            public virtual int CompareTo(Doc other)
            {
                return id.CompareToOrdinal(other.id);
            }
        }

        private double aChance, bChance, cChance;

        private string randomContentToken(bool isQuery)
        {
            double d = Random.NextDouble();
            if (isQuery)
            {
                if (d < 0.33)
                {
                    return "a";
                }
                else if (d < 0.66)
                {
                    return "b";
                }
                else
                {
                    return "c";
                }
            }
            else
            {
                if (d <= aChance)
                {
                    return "a";
                }
                else if (d < aChance + bChance)
                {
                    return "b";
                }
                else
                {
                    return "c";
                }
            }
        }

        [Test]
        public virtual void TestRandom()
        {
            bool canUseDV = DefaultCodecSupportsSortedSet;

            while (aChance == 0.0)
            {
                aChance = Random.NextDouble();
            }
            while (bChance == 0.0)
            {
                bChance = Random.NextDouble();
            }
            while (cChance == 0.0)
            {
                cChance = Random.NextDouble();
            }
            //aChance = .01;
            //bChance = 0.5;
            //cChance = 1.0;
            double sum = aChance + bChance + cChance;
            aChance /= sum;
            bChance /= sum;
            cChance /= sum;

            int numDims = TestUtil.NextInt32(Random, 2, 5);
            //int numDims = 3;
            int numDocs = AtLeast(3000);
            //int numDocs = 20;
            if (Verbose)
            {
                Console.WriteLine("numDims=" + numDims + " numDocs=" + numDocs + " aChance=" + aChance + " bChance=" + bChance + " cChance=" + cChance);
            }
            string[][] dimValues = new string[numDims][];
            int valueCount = 2;

            for (int dim = 0; dim < numDims; dim++)
            {
                var values = new JCG.HashSet<string>();
                while (values.Count < valueCount)
                {
                    var str = TestUtil.RandomRealisticUnicodeString(Random);
                    //String s = TestUtil.randomString(Random());
                    if (str.Length > 0)
                    {
                        values.Add(str);
                    }
                }
                dimValues[dim] = values.ToArray();
                valueCount *= 2;
            }

            IList<Doc> docs = new JCG.List<Doc>();
            for (int i = 0; i < numDocs; i++)
            {
                Doc doc = new Doc();
                doc.id = "" + i;
                doc.contentToken = randomContentToken(false);
                doc.dims = new int[numDims];
                doc.dims2 = new int[numDims];
                for (int dim = 0; dim < numDims; dim++)
                {
                    if (Random.Next(5) == 3)
                    {
                        // This doc is missing this dim:
                        doc.dims[dim] = -1;
                    }
                    else if (dimValues[dim].Length <= 4)
                    {
                        int dimUpto = 0;
                        doc.dims[dim] = dimValues[dim].Length - 1;
                        while (dimUpto < dimValues[dim].Length)
                        {
                            if (Random.NextBoolean())
                            {
                                doc.dims[dim] = dimUpto;
                                break;
                            }
                            dimUpto++;
                        }
                    }
                    else
                    {
                        doc.dims[dim] = Random.Next(dimValues[dim].Length);
                    }

                    if (Random.Next(5) == 3)
                    {
                        // 2nd value:
                        doc.dims2[dim] = Random.Next(dimValues[dim].Length);
                    }
                    else
                    {
                        doc.dims2[dim] = -1;
                    }
                }
                docs.Add(doc);
            }

            Directory d = NewDirectory();
            Directory td = NewDirectory();

            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            iwc.SetInfoStream(InfoStream.NO_OUTPUT);
            var w = new RandomIndexWriter(Random, d, iwc);
            var tw = new DirectoryTaxonomyWriter(td, OpenMode.CREATE);
            FacetsConfig config = new FacetsConfig();
            for (int i = 0; i < numDims; i++)
            {
                config.SetMultiValued("dim" + i, true);
            }

            bool doUseDV = canUseDV && Random.NextBoolean();

            foreach (Doc rawDoc in docs)
            {
                Document doc = new Document();
                doc.Add(NewStringField("id", rawDoc.id, Field.Store.YES));
                doc.Add(NewStringField("content", rawDoc.contentToken, Field.Store.NO));

                if (Verbose)
                {
                    Console.WriteLine("  doc id=" + rawDoc.id + " token=" + rawDoc.contentToken);
                }
                for (int dim = 0; dim < numDims; dim++)
                {
                    int dimValue = rawDoc.dims[dim];
                    if (dimValue != -1)
                    {
                        if (doUseDV)
                        {
                            doc.Add(new SortedSetDocValuesFacetField("dim" + dim, dimValues[dim][dimValue]));
                        }
                        else
                        {
                            doc.Add(new FacetField("dim" + dim, dimValues[dim][dimValue]));
                        }
                        doc.Add(new StringField("dim" + dim, dimValues[dim][dimValue], Field.Store.YES));
                        if (Verbose)
                        {
                            Console.WriteLine("    dim" + dim + "=" + new BytesRef(dimValues[dim][dimValue]));
                        }
                    }
                    int dimValue2 = rawDoc.dims2[dim];
                    if (dimValue2 != -1)
                    {
                        if (doUseDV)
                        {
                            doc.Add(new SortedSetDocValuesFacetField("dim" + dim, dimValues[dim][dimValue2]));
                        }
                        else
                        {
                            doc.Add(new FacetField("dim" + dim, dimValues[dim][dimValue2]));
                        }
                        doc.Add(new StringField("dim" + dim, dimValues[dim][dimValue2], Field.Store.YES));
                        if (Verbose)
                        {
                            Console.WriteLine("      dim" + dim + "=" + new BytesRef(dimValues[dim][dimValue2]));
                        }
                    }
                }

                w.AddDocument(config.Build(tw, doc));
            }

            if (Random.NextBoolean())
            {
                // Randomly delete a few docs:
                int numDel = TestUtil.NextInt32(Random, 1, (int)(numDocs * 0.05));
                if (Verbose)
                {
                    Console.WriteLine("delete " + numDel);
                }
                int delCount = 0;
                while (delCount < numDel)
                {
                    Doc doc = docs[Random.Next(docs.Count)];
                    if (!doc.deleted)
                    {
                        if (Verbose)
                        {
                            Console.WriteLine("  delete id=" + doc.id);
                        }
                        doc.deleted = true;
                        w.DeleteDocuments(new Term("id", doc.id));
                        delCount++;
                    }
                }
            }

            if (Random.NextBoolean())
            {
                if (Verbose)
                {
                    Console.WriteLine("TEST: forceMerge(1)...");
                }
                w.ForceMerge(1);
            }
            IndexReader r = w.GetReader();

            SortedSetDocValuesReaderState sortedSetDVState;
            IndexSearcher s = NewSearcher(r);

            if (doUseDV)
            {
                sortedSetDVState = new DefaultSortedSetDocValuesReaderState(s.IndexReader);
            }
            else
            {
                sortedSetDVState = null;
            }

            if (Verbose)
            {
                Console.WriteLine("r.numDocs() = " + r.NumDocs);
            }

            // NRT open
            var tr = new DirectoryTaxonomyReader(tw);

            int numIters = AtLeast(10);

            for (int iter = 0; iter < numIters; iter++)
            {
                string contentToken = Random.Next(30) == 17 ? null : randomContentToken(true);
                int numDrillDown = TestUtil.NextInt32(Random, 1, Math.Min(4, numDims));
                if (Verbose)
                {
                    Console.WriteLine("\nTEST: iter=" + iter + " baseQuery=" + contentToken + " numDrillDown=" + numDrillDown + " useSortedSetDV=" + doUseDV);
                }

                string[][] drillDowns = new string[numDims][];

                int count = 0;
                bool anyMultiValuedDrillDowns = false;
                while (count < numDrillDown)
                {
                    int dim = Random.Next(numDims);
                    if (drillDowns[dim] is null)
                    {
                        if (Random.NextBoolean())
                        {
                            // Drill down on one value:
                            drillDowns[dim] = new string[] { dimValues[dim][Random.Next(dimValues[dim].Length)] };
                        }
                        else
                        {
                            int orCount = TestUtil.NextInt32(Random, 1, Math.Min(5, dimValues[dim].Length));
                            drillDowns[dim] = new string[orCount];
                            anyMultiValuedDrillDowns |= orCount > 1;
                            for (int i = 0; i < orCount; i++)
                            {
                                while (true)
                                {
                                    string value = dimValues[dim][Random.Next(dimValues[dim].Length)];
                                    for (int j = 0; j < i; j++)
                                    {
                                        if (value.Equals(drillDowns[dim][j], StringComparison.Ordinal))
                                        {
                                            value = null;
                                            break;
                                        }
                                    }
                                    if (value != null)
                                    {
                                        drillDowns[dim][i] = value;
                                        break;
                                    }
                                }
                            }
                        }
                        if (Verbose)
                        {
                            BytesRef[] values = new BytesRef[drillDowns[dim].Length];
                            for (int i = 0; i < values.Length; i++)
                            {
                                values[i] = new BytesRef(drillDowns[dim][i]);
                            }
                            Console.WriteLine("  dim" + dim + "=" + Arrays.ToString(values));
                        }
                        count++;
                    }
                }

                Query baseQuery;
                if (contentToken is null)
                {
                    baseQuery = new MatchAllDocsQuery();
                }
                else
                {
                    baseQuery = new TermQuery(new Term("content", contentToken));
                }

                DrillDownQuery ddq = new DrillDownQuery(config, baseQuery);

                for (int dim = 0; dim < numDims; dim++)
                {
                    if (drillDowns[dim] != null)
                    {
                        foreach (string value in drillDowns[dim])
                        {
                            ddq.Add("dim" + dim, value);
                        }
                    }
                }

                Filter filter;
                if (Random.Next(7) == 6)
                {
                    if (Verbose)
                    {
                        Console.WriteLine("  only-even filter");
                    }
                    filter = new FilterAnonymousClass(this);
                }
                else
                {
                    filter = null;
                }

                // Verify docs are always collected in order.  If we
                // had an AssertingScorer it could catch it when
                // Weight.scoresDocsOutOfOrder lies!:
                new DrillSideways(s, config, tr).Search(ddq, new CollectorAnonymousClass(this));

                // Also separately verify that DS respects the
                // scoreSubDocsAtOnce method, to ensure that all
                // subScorers are on the same docID:
                if (!anyMultiValuedDrillDowns)
                {
                    // Can only do this test when there are no OR'd
                    // drill-down values, because in that case it's
                    // easily possible for one of the DD terms to be on
                    // a future docID:
                    new DrillSidewaysAnonymousClass(this, s, config, tr)
                        .Search(ddq, new AssertingSubDocsAtOnceCollector());
                }

                TestFacetResult expected = slowDrillSidewaysSearch(s, docs, contentToken, drillDowns, dimValues, filter);

                Sort sort = new Sort(new SortField("id", SortFieldType.STRING));
                DrillSideways ds;
                if (doUseDV)
                {
                    ds = new DrillSideways(s, config, sortedSetDVState);
                }
                else
                {
                    ds = new DrillSidewaysAnonymousClass2(this, s, config, tr);
                }

                // Retrieve all facets:
                DrillSidewaysResult actual = ds.Search(ddq, filter, null, numDocs, sort, true, true);

                TopDocs hits = s.Search(baseQuery, numDocs);
                IDictionary<string, float> scores = new Dictionary<string, float>();
                foreach (ScoreDoc sd in hits.ScoreDocs)
                {
                    scores[s.Doc(sd.Doc).Get("id")] = sd.Score;
                }
                if (Verbose)
                {
                    Console.WriteLine("  verify all facets");
                }
                VerifyEquals(dimValues, s, expected, actual, scores, doUseDV);

                // Make sure drill down doesn't change score:
                TopDocs ddqHits = s.Search(ddq, filter, numDocs);
                Assert.AreEqual(expected.Hits.Count, ddqHits.TotalHits);
                for (int i = 0; i < expected.Hits.Count; i++)
                {
                    // Score should be IDENTICAL:
                    Assert.AreEqual(scores[expected.Hits[i].id], ddqHits.ScoreDocs[i].Score);
                }
            }

            IOUtils.Dispose(r, tr, w, tw, d, td);
        }

        private sealed class FilterAnonymousClass : Filter
        {
            private readonly TestDrillSideways outerInstance;

            public FilterAnonymousClass(TestDrillSideways outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            public override DocIdSet GetDocIdSet(AtomicReaderContext context, IBits acceptDocs)
            {
                int maxDoc = context.Reader.MaxDoc;
                FixedBitSet bits = new FixedBitSet(maxDoc);
                for (int docID = 0; docID < maxDoc; docID++)
                {
                    // Keeps only the even ids:
                    if ((acceptDocs is null || acceptDocs.Get(docID)) && (Convert.ToInt32(context.Reader.Document(docID).Get("id")) & 1) == 0)
                    {
                        bits.Set(docID);
                    }
                }
                return bits;
            }
        }

        private sealed class CollectorAnonymousClass : ICollector
        {
            private readonly TestDrillSideways outerInstance;

            public CollectorAnonymousClass(TestDrillSideways outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            internal int lastDocID;

            public void SetScorer(Scorer scorer)
            {
            }

            public void Collect(int doc)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(doc > lastDocID);
                lastDocID = doc;
            }

            public void SetNextReader(AtomicReaderContext context)
            {
                lastDocID = -1;
            }

            public bool AcceptsDocsOutOfOrder => false;
        }

        private sealed class DrillSidewaysAnonymousClass : DrillSideways
        {
            private readonly TestDrillSideways outerInstance;

            public DrillSidewaysAnonymousClass(TestDrillSideways outerInstance, IndexSearcher s, FacetsConfig config, TaxonomyReader tr)
                : base(s, config, tr)
            {
                this.outerInstance = outerInstance;
            }

            protected override bool ScoreSubDocsAtOnce => true;
        }

        private sealed class DrillSidewaysAnonymousClass2 : DrillSideways
        {
            private readonly TestDrillSideways outerInstance;

            public DrillSidewaysAnonymousClass2(TestDrillSideways outerInstance, IndexSearcher s, FacetsConfig config, TaxonomyReader tr)
                : base(s, config, tr)
            {
                this.outerInstance = outerInstance;
            }

            protected override Facets BuildFacetsResult(FacetsCollector drillDowns, FacetsCollector[] drillSideways, string[] drillSidewaysDims)
            {
                IDictionary<string, Facets> drillSidewaysFacets = new Dictionary<string, Facets>();
                Facets drillDownFacets = outerInstance.GetTaxonomyFacetCounts(m_taxoReader, m_config, drillDowns);
                if (drillSideways != null)
                {
                    for (int i = 0; i < drillSideways.Length; i++)
                    {
                        drillSidewaysFacets[drillSidewaysDims[i]] = outerInstance.GetTaxonomyFacetCounts(m_taxoReader, m_config, drillSideways[i]);
                    }
                }

                if (drillSidewaysFacets.Count == 0)
                {
                    return drillDownFacets;
                }
                else
                {
                    return new MultiFacets(drillSidewaysFacets, drillDownFacets);
                }
            }
        }

        private class Counters
        {
            internal int[][] counts;

            public Counters(string[][] dimValues)
            {
                counts = new int[dimValues.Length][];
                for (int dim = 0; dim < dimValues.Length; dim++)
                {
                    counts[dim] = new int[dimValues[dim].Length];
                }
            }

            public virtual void Inc(int[] dims, int[] dims2)
            {
                Inc(dims, dims2, -1);
            }

            public virtual void Inc(int[] dims, int[] dims2, int onlyDim)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(dims.Length == counts.Length);
                if (Debugging.AssertsEnabled) Debugging.Assert(dims2.Length == counts.Length);
                for (int dim = 0; dim < dims.Length; dim++)
                {
                    if (onlyDim == -1 || dim == onlyDim)
                    {
                        if (dims[dim] != -1)
                        {
                            counts[dim][dims[dim]]++;
                        }
                        if (dims2[dim] != -1 && dims2[dim] != dims[dim])
                        {
                            counts[dim][dims2[dim]]++;
                        }
                    }
                }
            }
        }

        internal class TestFacetResult
        {
            internal IList<Doc> Hits;
            internal int[][] Counts;
            internal int[] UniqueCounts;
            public TestFacetResult()
            {
            }
        }

        private int[] GetTopNOrds(int[] counts, string[] values, int topN)
        {
            int[] ids = new int[counts.Length];
            for (int i = 0; i < ids.Length; i++)
            {
                ids[i] = i;
            }

            // Naive (on purpose, to reduce bug in tester/gold):
            // sort all ids, then return top N slice:
            new InPlaceMergeSorterAnonymousClass(this, counts, values, ids).Sort(0, ids.Length);

            if (topN > ids.Length)
            {
                topN = ids.Length;
            }

            int numSet = topN;
            for (int i = 0; i < topN; i++)
            {
                if (counts[ids[i]] == 0)
                {
                    numSet = i;
                    break;
                }
            }

            int[] topNIDs = new int[numSet];
            Arrays.Copy(ids, 0, topNIDs, 0, topNIDs.Length);
            return topNIDs;
        }

        private sealed class InPlaceMergeSorterAnonymousClass : InPlaceMergeSorter
        {
            private readonly TestDrillSideways outerInstance;

            private readonly int[] counts;
            private readonly string[] values;
            private readonly int[] ids;

            public InPlaceMergeSorterAnonymousClass(TestDrillSideways outerInstance, int[] counts, string[] values, int[] ids)
            {
                this.outerInstance = outerInstance;
                this.counts = counts;
                this.values = values;
                this.ids = ids;
            }


            protected override void Swap(int i, int j)
            {
                int id = ids[i];
                ids[i] = ids[j];
                ids[j] = id;
            }

            protected override int Compare(int i, int j)
            {
                int counti = counts[ids[i]];
                int countj = counts[ids[j]];
                // Sort by count descending...
                if (counti > countj)
                {
                    return -1;
                }
                else if (counti < countj)
                {
                    return 1;
                }
                else
                {
                    // ... then by label ascending:
                    return new BytesRef(values[ids[i]]).CompareTo(new BytesRef(values[ids[j]]));
                }
            }

        }

        private TestFacetResult slowDrillSidewaysSearch(IndexSearcher s, IList<Doc> docs, string contentToken, string[][] drillDowns, string[][] dimValues, Filter onlyEven)
        {
            int numDims = dimValues.Length;

            JCG.List<Doc> hits = new JCG.List<Doc>();
            Counters drillDownCounts = new Counters(dimValues);
            Counters[] drillSidewaysCounts = new Counters[dimValues.Length];
            for (int dim = 0; dim < numDims; dim++)
            {
                drillSidewaysCounts[dim] = new Counters(dimValues);
            }

            if (Verbose)
            {
                Console.WriteLine("  compute expected");
            }

            foreach (Doc doc in docs)
            {
                if (doc.deleted)
                {
                    continue;
                }
                if (onlyEven != null & (Convert.ToInt32(doc.id, CultureInfo.InvariantCulture) & 1) != 0)
                {
                    continue;
                }
                if (contentToken is null || doc.contentToken.Equals(contentToken, StringComparison.Ordinal))
                {
                    int failDim = -1;
                    for (int dim = 0; dim < numDims; dim++)
                    {
                        if (drillDowns[dim] != null)
                        {
                            string docValue = doc.dims[dim] == -1 ? null : dimValues[dim][doc.dims[dim]];
                            string docValue2 = doc.dims2[dim] == -1 ? null : dimValues[dim][doc.dims2[dim]];
                            bool matches = false;
                            foreach (string value in drillDowns[dim])
                            {
                                if (value.Equals(docValue, StringComparison.Ordinal) || value.Equals(docValue2, StringComparison.Ordinal))
                                {
                                    matches = true;
                                    break;
                                }
                            }
                            if (!matches)
                            {
                                if (failDim == -1)
                                {
                                    // Doc could be a near-miss, if no other dim fails
                                    failDim = dim;
                                }
                                else
                                {
                                    // Doc isn't a hit nor a near-miss
                                    goto nextDocContinue;
                                }
                            }
                        }
                    }

                    if (failDim == -1)
                    {
                        if (Verbose)
                        {
                            Console.WriteLine("    exp: id=" + doc.id + " is a hit");
                        }
                        // Hit:
                        hits.Add(doc);
                        drillDownCounts.Inc(doc.dims, doc.dims2);
                        for (int dim = 0; dim < dimValues.Length; dim++)
                        {
                            drillSidewaysCounts[dim].Inc(doc.dims, doc.dims2);
                        }
                    }
                    else
                    {
                        if (Verbose)
                        {
                            Console.WriteLine("    exp: id=" + doc.id + " is a near-miss on dim=" + failDim);
                        }
                        drillSidewaysCounts[failDim].Inc(doc.dims, doc.dims2, failDim);
                    }
                }
            nextDocContinue:;
            }
            //nextDocBreak:// Not referenced

            IDictionary<string, int> idToDocID = new Dictionary<string, int>();
            for (int i = 0; i < s.IndexReader.MaxDoc; i++)
            {
                idToDocID[s.Doc(i).Get("id")] = i;
            }

            hits.Sort();

            TestFacetResult res = new TestFacetResult();
            res.Hits = hits;
            res.Counts = new int[numDims][];
            res.UniqueCounts = new int[numDims];
            for (int dim = 0; dim < numDims; dim++)
            {
                if (drillDowns[dim] != null)
                {
                    res.Counts[dim] = drillSidewaysCounts[dim].counts[dim];
                }
                else
                {
                    res.Counts[dim] = drillDownCounts.counts[dim];
                }
                int uniqueCount = 0;
                for (int j = 0; j < res.Counts[dim].Length; j++)
                {
                    if (res.Counts[dim][j] != 0)
                    {
                        uniqueCount++;
                    }
                }
                res.UniqueCounts[dim] = uniqueCount;
            }

            return res;
        }

        internal virtual void VerifyEquals(string[][] dimValues, IndexSearcher s, TestFacetResult expected, DrillSidewaysResult actual, IDictionary<string, float> scores, bool isSortedSetDV)
        {
            if (Verbose)
            {
                Console.WriteLine("  verify totHits=" + expected.Hits.Count);
            }
            Assert.AreEqual(expected.Hits.Count, actual.Hits.TotalHits);
            Assert.AreEqual(expected.Hits.Count, actual.Hits.ScoreDocs.Length);
            for (int i = 0; i < expected.Hits.Count; i++)
            {
                if (Verbose)
                {
                    Console.WriteLine("    hit " + i + " expected=" + expected.Hits[i].id);
                }
                Assert.AreEqual(expected.Hits[i].id, s.Doc(actual.Hits.ScoreDocs[i].Doc).Get("id"));
                // Score should be IDENTICAL:
                Assert.AreEqual(scores[expected.Hits[i].id], actual.Hits.ScoreDocs[i].Score, 0.0f);
            }

            for (int dim = 0; dim < expected.Counts.Length; dim++)
            {
                int topN = Random.NextBoolean() ? dimValues[dim].Length : TestUtil.NextInt32(Random, 1, dimValues[dim].Length);
                FacetResult fr = actual.Facets.GetTopChildren(topN, "dim" + dim);
                if (Verbose)
                {
                    Console.WriteLine("    dim" + dim + " topN=" + topN + " (vs " + dimValues[dim].Length + " unique values)");
                    Console.WriteLine("      actual");
                }

                int idx = 0;
                IDictionary<string, int> actualValues = new Dictionary<string, int>();

                if (fr != null)
                {
                    foreach (LabelAndValue labelValue in fr.LabelValues)
                    {
                        actualValues[labelValue.Label] = (int)labelValue.Value;
                        if (Verbose)
                        {
                            Console.WriteLine("        " + idx + ": " + new BytesRef(labelValue.Label) + ": " + labelValue.Value);
                            idx++;
                        }
                    }
                    Assert.AreEqual(expected.UniqueCounts[dim], fr.ChildCount, "dim=" + dim);
                }

                if (topN < dimValues[dim].Length)
                {
                    int[] topNIDs = GetTopNOrds(expected.Counts[dim], dimValues[dim], topN);
                    if (Verbose)
                    {
                        idx = 0;
                        Console.WriteLine("      expected (sorted)");
                        for (int i = 0; i < topNIDs.Length; i++)
                        {
                            int expectedOrd = topNIDs[i];
                            string value = dimValues[dim][expectedOrd];
                            Console.WriteLine("        " + idx + ": " + new BytesRef(value) + ": " + expected.Counts[dim][expectedOrd]);
                            idx++;
                        }
                    }
                    if (Verbose)
                    {
                        Console.WriteLine("      topN=" + topN + " expectedTopN=" + topNIDs.Length);
                    }

                    if (fr != null)
                    {
                        Assert.AreEqual(topNIDs.Length, fr.LabelValues.Length);
                    }
                    else
                    {
                        Assert.AreEqual(0, topNIDs.Length);
                    }
                    for (int i = 0; i < topNIDs.Length; i++)
                    {
                        int expectedOrd = topNIDs[i];
                        Assert.AreEqual(expected.Counts[dim][expectedOrd], (int)fr.LabelValues[i].Value);
                        if (isSortedSetDV)
                        {
                            // Tie-break facet labels are only in unicode
                            // order with SortedSetDVFacets:
                            assertEquals("value @ idx=" + i, dimValues[dim][expectedOrd], fr.LabelValues[i].Label);
                        }
                    }
                }
                else
                {

                    if (Verbose)
                    {
                        idx = 0;
                        Console.WriteLine("      expected (unsorted)");
                        for (int i = 0; i < dimValues[dim].Length; i++)
                        {
                            string value = dimValues[dim][i];
                            if (expected.Counts[dim][i] != 0)
                            {
                                Console.WriteLine("        " + idx + ": " + new BytesRef(value) + ": " + expected.Counts[dim][i]);
                                idx++;
                            }
                        }
                    }

                    int setCount = 0;
                    for (int i = 0; i < dimValues[dim].Length; i++)
                    {
                        string value = dimValues[dim][i];
                        if (expected.Counts[dim][i] != 0)
                        {
                            Assert.IsTrue(actualValues.ContainsKey(value));
                            Assert.AreEqual(expected.Counts[dim][i], (int)actualValues[value]);
                            setCount++;
                        }
                        else
                        {
                            Assert.IsFalse(actualValues.ContainsKey(value));
                        }
                    }
                    Assert.AreEqual(setCount, actualValues.Count);
                }
            }
        }

        [Test]
        public virtual void TestEmptyIndex()
        {
            // LUCENE-5045: make sure DrillSideways works with an empty index
            Directory dir = NewDirectory();
            Directory taxoDir = NewDirectory();
            var writer = new RandomIndexWriter(Random, dir);
            var taxoWriter = new DirectoryTaxonomyWriter(taxoDir, OpenMode.CREATE);
            IndexSearcher searcher = NewSearcher(writer.GetReader());
            var taxoReader = new DirectoryTaxonomyReader(taxoWriter);

            // Count "Author"
            FacetsConfig config = new FacetsConfig();
            DrillSideways ds = new DrillSideways(searcher, config, taxoReader);
            DrillDownQuery ddq = new DrillDownQuery(config);
            ddq.Add("Author", "Lisa");

            DrillSidewaysResult r = ds.Search(ddq, 10); // this used to fail on IllegalArgEx
            Assert.AreEqual(0, r.Hits.TotalHits);

            r = ds.Search(ddq, null, null, 10, new Sort(new SortField("foo", SortFieldType.INT32)), false, false); // this used to fail on IllegalArgEx
            Assert.AreEqual(0, r.Hits.TotalHits);

            IOUtils.Dispose(writer, taxoWriter, searcher.IndexReader, taxoReader, dir, taxoDir);
        }

        // LUCENENET: From Lucene 4.10.4
        [Test]
        public void TestScorer()
        {
            // LUCENE-6001 some scorers, eg ReqExlScorer, can hit NPE if cost is called after nextDoc
            Directory dir = NewDirectory();
            Directory taxoDir = NewDirectory();

            // Writes facet ords to a separate directory from the
            // main index:
            DirectoryTaxonomyWriter taxoWriter = new DirectoryTaxonomyWriter(taxoDir, OpenMode.CREATE);

            FacetsConfig config = new FacetsConfig();

            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);

            Document doc = new Document();
            doc.Add(NewTextField("field", "foo bar", Field.Store.NO));
            doc.Add(new FacetField("Author", "Bob"));
            doc.Add(new FacetField("dim", "a"));
            writer.AddDocument(config.Build(taxoWriter, doc));

            // NRT open
            IndexSearcher searcher = NewSearcher(writer.GetReader());

            // NRT open
            TaxonomyReader taxoReader = new DirectoryTaxonomyReader(taxoWriter);

            DrillSideways ds = new DrillSideways(searcher, config, taxoReader);

            BooleanQuery bq = new BooleanQuery(true);
            bq.Add(new TermQuery(new Term("field", "foo")), Occur.MUST);
            bq.Add(new TermQuery(new Term("field", "bar")), Occur.MUST_NOT);
            DrillDownQuery ddq = new DrillDownQuery(config, bq);
            ddq.Add("field", "foo");
            ddq.Add("author", bq);
            ddq.Add("dim", bq);
            DrillSidewaysResult r = ds.Search(null, ddq, 10);
            assertEquals(0, r.Hits.TotalHits);

            writer.Dispose();
            IOUtils.Dispose(searcher.IndexReader, taxoReader, taxoWriter, dir, taxoDir);
        }
    }
}