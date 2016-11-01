using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Randomized;
using Lucene.Net.Randomized.Generators;
using NUnit.Framework;

namespace Lucene.Net.Facet.SortedSet
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


    using Document = Lucene.Net.Documents.Document;
    using Field = Lucene.Net.Documents.Field;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using SlowCompositeReaderWrapper = Lucene.Net.Index.SlowCompositeReaderWrapper;
    using Term = Lucene.Net.Index.Term;
    using IndexSearcher = Lucene.Net.Search.IndexSearcher;
    using MatchAllDocsQuery = Lucene.Net.Search.MatchAllDocsQuery;
    using TermQuery = Lucene.Net.Search.TermQuery;
    using TopDocs = Lucene.Net.Search.TopDocs;
    using Directory = Lucene.Net.Store.Directory;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using TestUtil = Lucene.Net.Util.TestUtil;
    
    [TestFixture]
    public class TestSortedSetDocValuesFacets : FacetTestCase
    {

        // NOTE: TestDrillSideways.testRandom also sometimes
        // randomly uses SortedSetDV
        [Test]
        public virtual void TestBasic()
        {

            AssumeTrue("Test requires SortedSetDV support", DefaultCodecSupportsSortedSet());
            Directory dir = NewDirectory();

            FacetsConfig config = new FacetsConfig();
            config.SetMultiValued("a", true);
            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);

            Document doc = new Document();
            doc.Add(new SortedSetDocValuesFacetField("a", "foo"));
            doc.Add(new SortedSetDocValuesFacetField("a", "bar"));
            doc.Add(new SortedSetDocValuesFacetField("a", "zoo"));
            doc.Add(new SortedSetDocValuesFacetField("b", "baz"));
            writer.AddDocument(config.Build(doc));
            if (Random().NextBoolean())
            {
                writer.Commit();
            }

            doc = new Document();
            doc.Add(new SortedSetDocValuesFacetField("a", "foo"));
            writer.AddDocument(config.Build(doc));

            // NRT open
            IndexSearcher searcher = NewSearcher(writer.Reader);

            // Per-top-reader state:
            SortedSetDocValuesReaderState state = new DefaultSortedSetDocValuesReaderState(searcher.IndexReader);

            FacetsCollector c = new FacetsCollector();

            searcher.Search(new MatchAllDocsQuery(), c);

            SortedSetDocValuesFacetCounts facets = new SortedSetDocValuesFacetCounts(state, c);

            Assert.AreEqual("dim=a path=[] value=4 childCount=3\n  foo (2)\n  bar (1)\n  zoo (1)\n", facets.GetTopChildren(10, "a").ToString());
            Assert.AreEqual("dim=b path=[] value=1 childCount=1\n  baz (1)\n", facets.GetTopChildren(10, "b").ToString());

            // DrillDown:
            DrillDownQuery q = new DrillDownQuery(config);
            q.Add("a", "foo");
            q.Add("b", "baz");
            TopDocs hits = searcher.Search(q, 1);
            Assert.AreEqual(1, hits.TotalHits);

            IOUtils.Close(writer, searcher.IndexReader, dir);
        }

        // LUCENE-5090
        [Test]
        public virtual void TestStaleState()
        {
            AssumeTrue("Test requires SortedSetDV support", DefaultCodecSupportsSortedSet());
            Directory dir = NewDirectory();

            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);

            FacetsConfig config = new FacetsConfig();

            Document doc = new Document();
            doc.Add(new SortedSetDocValuesFacetField("a", "foo"));
            writer.AddDocument(config.Build(doc));

            IndexReader r = writer.Reader;
            SortedSetDocValuesReaderState state = new DefaultSortedSetDocValuesReaderState(r);

            doc = new Document();
            doc.Add(new SortedSetDocValuesFacetField("a", "bar"));
            writer.AddDocument(config.Build(doc));

            doc = new Document();
            doc.Add(new SortedSetDocValuesFacetField("a", "baz"));
            writer.AddDocument(config.Build(doc));

            IndexSearcher searcher = NewSearcher(writer.Reader);

            FacetsCollector c = new FacetsCollector();

            searcher.Search(new MatchAllDocsQuery(), c);

            try
            {
                new SortedSetDocValuesFacetCounts(state, c);
                Fail("did not hit expected exception");
            }
            catch (InvalidOperationException)
            {
                // expected
            }

            r.Dispose();
            writer.Dispose();
            searcher.IndexReader.Dispose();
            dir.Dispose();
        }

        // LUCENE-5333
        [Test]
        public virtual void TestSparseFacets()
        {
            AssumeTrue("Test requires SortedSetDV support", DefaultCodecSupportsSortedSet());
            Directory dir = NewDirectory();

            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);

            FacetsConfig config = new FacetsConfig();

            Document doc = new Document();
            doc.Add(new SortedSetDocValuesFacetField("a", "foo1"));
            writer.AddDocument(config.Build(doc));

            if (Random().NextBoolean())
            {
                writer.Commit();
            }

            doc = new Document();
            doc.Add(new SortedSetDocValuesFacetField("a", "foo2"));
            doc.Add(new SortedSetDocValuesFacetField("b", "bar1"));
            writer.AddDocument(config.Build(doc));

            if (Random().NextBoolean())
            {
                writer.Commit();
            }

            doc = new Document();
            doc.Add(new SortedSetDocValuesFacetField("a", "foo3"));
            doc.Add(new SortedSetDocValuesFacetField("b", "bar2"));
            doc.Add(new SortedSetDocValuesFacetField("c", "baz1"));
            writer.AddDocument(config.Build(doc));

            // NRT open
            IndexSearcher searcher = NewSearcher(writer.Reader);
            writer.Dispose();

            // Per-top-reader state:
            SortedSetDocValuesReaderState state = new DefaultSortedSetDocValuesReaderState(searcher.IndexReader);

            FacetsCollector c = new FacetsCollector();
            searcher.Search(new MatchAllDocsQuery(), c);
            SortedSetDocValuesFacetCounts facets = new SortedSetDocValuesFacetCounts(state, c);

            // Ask for top 10 labels for any dims that have counts:
            IList<FacetResult> results = facets.GetAllDims(10);

            Assert.AreEqual(3, results.Count);
            Assert.AreEqual("dim=a path=[] value=3 childCount=3\n  foo1 (1)\n  foo2 (1)\n  foo3 (1)\n", results[0].ToString());
            Assert.AreEqual("dim=b path=[] value=2 childCount=2\n  bar1 (1)\n  bar2 (1)\n", results[1].ToString());
            Assert.AreEqual("dim=c path=[] value=1 childCount=1\n  baz1 (1)\n", results[2].ToString());

            searcher.IndexReader.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestSomeSegmentsMissing()
        {
            AssumeTrue("Test requires SortedSetDV support", DefaultCodecSupportsSortedSet());
            Directory dir = NewDirectory();

            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);

            FacetsConfig config = new FacetsConfig();

            Document doc = new Document();
            doc.Add(new SortedSetDocValuesFacetField("a", "foo1"));
            writer.AddDocument(config.Build(doc));
            writer.Commit();

            doc = new Document();
            writer.AddDocument(config.Build(doc));
            writer.Commit();

            doc = new Document();
            doc.Add(new SortedSetDocValuesFacetField("a", "foo2"));
            writer.AddDocument(config.Build(doc));
            writer.Commit();

            // NRT open
            IndexSearcher searcher = NewSearcher(writer.Reader);
            writer.Dispose();

            // Per-top-reader state:
            SortedSetDocValuesReaderState state = new DefaultSortedSetDocValuesReaderState(searcher.IndexReader);

            FacetsCollector c = new FacetsCollector();
            searcher.Search(new MatchAllDocsQuery(), c);
            SortedSetDocValuesFacetCounts facets = new SortedSetDocValuesFacetCounts(state, c);

            // Ask for top 10 labels for any dims that have counts:
            Assert.AreEqual("dim=a path=[] value=2 childCount=2\n  foo1 (1)\n  foo2 (1)\n", facets.GetTopChildren(10, "a").ToString());

            searcher.IndexReader.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestSlowCompositeReaderWrapper()
        {
            AssumeTrue("Test requires SortedSetDV support", DefaultCodecSupportsSortedSet());
            Directory dir = NewDirectory();

            RandomIndexWriter writer = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);

            FacetsConfig config = new FacetsConfig();

            Document doc = new Document();
            doc.Add(new SortedSetDocValuesFacetField("a", "foo1"));
            writer.AddDocument(config.Build(doc));

            writer.Commit();

            doc = new Document();
            doc.Add(new SortedSetDocValuesFacetField("a", "foo2"));
            writer.AddDocument(config.Build(doc));

            // NRT open
            IndexSearcher searcher = new IndexSearcher(SlowCompositeReaderWrapper.Wrap(writer.Reader));

            // Per-top-reader state:
            SortedSetDocValuesReaderState state = new DefaultSortedSetDocValuesReaderState(searcher.IndexReader);

            FacetsCollector c = new FacetsCollector();
            searcher.Search(new MatchAllDocsQuery(), c);
            Facets facets = new SortedSetDocValuesFacetCounts(state, c);

            // Ask for top 10 labels for any dims that have counts:
            Assert.AreEqual("dim=a path=[] value=2 childCount=2\n  foo1 (1)\n  foo2 (1)\n", facets.GetTopChildren(10, "a").ToString());

            IOUtils.Close(writer, searcher.IndexReader, dir);
        }


        [Test]
        public virtual void TestRandom()
        {
            AssumeTrue("Test requires SortedSetDV support", DefaultCodecSupportsSortedSet());
            string[] tokens = GetRandomTokens(10);
            Directory indexDir = NewDirectory();
            Directory taxoDir = NewDirectory();

            RandomIndexWriter w = new RandomIndexWriter(Random(), indexDir, Similarity, TimeZone);
            FacetsConfig config = new FacetsConfig();
            int numDocs = AtLeast(1000);
            int numDims = TestUtil.NextInt(Random(), 1, 7);
            IList<TestDoc> testDocs = GetRandomDocs(tokens, numDocs, numDims);
            foreach (TestDoc testDoc in testDocs)
            {
                Document doc = new Document();
                doc.Add(NewStringField("content", testDoc.content, Field.Store.NO));
                for (int j = 0; j < numDims; j++)
                {
                    if (testDoc.dims[j] != null)
                    {
                        doc.Add(new SortedSetDocValuesFacetField("dim" + j, testDoc.dims[j]));
                    }
                }
                w.AddDocument(config.Build(doc));
            }

            // NRT open
            IndexSearcher searcher = NewSearcher(w.Reader);

            // Per-top-reader state:
            SortedSetDocValuesReaderState state = new DefaultSortedSetDocValuesReaderState(searcher.IndexReader);

            int iters = AtLeast(100);
            for (int iter = 0; iter < iters; iter++)
            {
                string searchToken = tokens[Random().Next(tokens.Length)];
                if (VERBOSE)
                {
                    Console.WriteLine("\nTEST: iter content=" + searchToken);
                }
                FacetsCollector fc = new FacetsCollector();
                FacetsCollector.Search(searcher, new TermQuery(new Term("content", searchToken)), 10, fc);
                Facets facets = new SortedSetDocValuesFacetCounts(state, fc);

                // Slow, yet hopefully bug-free, faceting:
                var expectedCounts = new List<Dictionary<string, int?>>();
                for (int i = 0; i < numDims; i++)
                {
                    expectedCounts.Add(new Dictionary<string, int?>());
                }

                foreach (TestDoc doc in testDocs)
                {
                    if (doc.content.Equals(searchToken))
                    {
                        for (int j = 0; j < numDims; j++)
                        {
                            if (doc.dims[j] != null)
                            {
                                int? v; 
                                
                                if (!expectedCounts[j].TryGetValue(doc.dims[j],out v))
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

                List<FacetResult> expected = new List<FacetResult>();
                for (int i = 0; i < numDims; i++)
                {
                    List<LabelAndValue> labelValues = new List<LabelAndValue>();
                    int totCount = 0;
                    foreach (KeyValuePair<string, int?> ent in expectedCounts[i])
                    {
                        labelValues.Add(new LabelAndValue(ent.Key, ent.Value.Value));
                        totCount += ent.Value.Value;
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
                //sortTies(actual);

                CollectionAssert.AreEqual(expected, actual);
            }

            IOUtils.Close(w, searcher.IndexReader, indexDir, taxoDir);
        }
    }

}