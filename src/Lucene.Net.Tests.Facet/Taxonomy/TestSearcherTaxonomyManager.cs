using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Lucene.Net.Support;
using NUnit.Framework;

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


    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using Document = Lucene.Net.Documents.Document;
    using SearcherAndTaxonomy = Lucene.Net.Facet.Taxonomy.SearcherTaxonomyManager.SearcherAndTaxonomy;
    using DirectoryTaxonomyWriter = Lucene.Net.Facet.Taxonomy.Directory.DirectoryTaxonomyWriter;
    using IndexWriter = Lucene.Net.Index.IndexWriter;
    using IndexWriterConfig = Lucene.Net.Index.IndexWriterConfig;
    using TieredMergePolicy = Lucene.Net.Index.TieredMergePolicy;
    using MatchAllDocsQuery = Lucene.Net.Search.MatchAllDocsQuery;
    using Lucene.Net.Search;
    using Directory = Lucene.Net.Store.Directory;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using TestUtil = Lucene.Net.Util.TestUtil;
    [TestFixture]
    public class TestSearcherTaxonomyManager : FacetTestCase
    {

        private class IndexerThread : ThreadClass
        {

            internal IndexWriter w;
            internal FacetsConfig config;
            internal ITaxonomyWriter tw;
            internal ReferenceManager<SearcherAndTaxonomy> mgr;
            internal int ordLimit;
            internal AtomicBoolean stop;

            public IndexerThread(IndexWriter w, FacetsConfig config, ITaxonomyWriter tw, ReferenceManager<SearcherAndTaxonomy> mgr, int ordLimit, AtomicBoolean stop)
            {
                this.w = w;
                this.config = config;
                this.tw = tw;
                this.mgr = mgr;
                this.ordLimit = ordLimit;
                this.stop = stop;
            }

            public override void Run()
            {
                try
                {
                    var seen = new HashSet<string>();
                    IList<string> paths = new List<string>();
                    while (true)
                    {
                        Document doc = new Document();
                        int numPaths = TestUtil.NextInt(Random(), 1, 5);
                        for (int i = 0; i < numPaths; i++)
                        {
                            string path;
                            if (paths.Count > 0 && Random().Next(5) != 4)
                            {
                                // Use previous path
                                path = paths[Random().Next(paths.Count)];
                            }
                            else
                            {
                                // Create new path
                                path = null;
                                while (true)
                                {
                                    path = TestUtil.RandomRealisticUnicodeString(Random());
                                    if (path.Length != 0 && !seen.Contains(path))
                                    {
                                        seen.Add(path);
                                        paths.Add(path);
                                        break;
                                    }
                                }
                            }
                            doc.Add(new FacetField("field", path));
                        }
                        try
                        {
                            w.AddDocument(config.Build(tw, doc));
                            if (mgr != null && Random().NextDouble() < 0.02)
                            {
                                w.Commit();
                                tw.Commit();
                                mgr.MaybeRefresh();
                            }
                        }
                        catch (IOException ioe)
                        {
                            throw new Exception(ioe.Message, ioe);
                        }

                        if (VERBOSE)
                        {
                            Console.WriteLine("TW size=" + tw.Size + " vs " + ordLimit);
                        }

                        if (tw.Size >= ordLimit)
                        {
                            break;
                        }
                    }
                }
                finally
                {
                    stop.Set(true);
                }
            }

        }

        [Test]
        public virtual void TestNrt()
        {
            Store.Directory dir = NewDirectory();
            Store.Directory taxoDir = NewDirectory();
            IndexWriterConfig iwc = NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random()));
            // Don't allow tiny maxBufferedDocs; it can make this
            // test too slow:
            iwc.SetMaxBufferedDocs(Math.Max(500, iwc.MaxBufferedDocs));

            // MockRandom/AlcololicMergePolicy are too slow:
            TieredMergePolicy tmp = new TieredMergePolicy();
            tmp.FloorSegmentMB = .001;
            iwc.SetMergePolicy(tmp);
            IndexWriter w = new IndexWriter(dir, iwc);
            var tw = new DirectoryTaxonomyWriter(taxoDir);
            FacetsConfig config = new FacetsConfig();
            config.SetMultiValued("field", true);
            AtomicBoolean stop = new AtomicBoolean();

            // How many unique facets to index before stopping:
            int ordLimit = TEST_NIGHTLY ? 100000 : 6000;

            var indexer = new IndexerThread(w, config, tw, null, ordLimit, stop);

            var mgr = new SearcherTaxonomyManager(w, true, null, tw);

            var reopener = new ThreadAnonymousInnerClassHelper(this, stop, mgr);

            reopener.Name = "reopener";
            reopener.Start();

            indexer.Name = "indexer";
            indexer.Start();

            try
            {
                while (!stop.Get())
                {
                    SearcherAndTaxonomy pair = mgr.Acquire();
                    try
                    {
                        //System.out.println("search maxOrd=" + pair.taxonomyReader.getSize());
                        FacetsCollector sfc = new FacetsCollector();
                        pair.Searcher.Search(new MatchAllDocsQuery(), sfc);
                        Facets facets = GetTaxonomyFacetCounts(pair.TaxonomyReader, config, sfc);
                        FacetResult result = facets.GetTopChildren(10, "field");
                        if (pair.Searcher.IndexReader.NumDocs > 0)
                        {
                            //System.out.println(pair.taxonomyReader.getSize());
                            Assert.True(result.ChildCount > 0);
                            Assert.True(result.LabelValues.Length > 0);
                        }

                        //if (VERBOSE) {
                        //System.out.println("TEST: facets=" + FacetTestUtils.toString(results.get(0)));
                        //}
                    }
                    finally
                    {
                        mgr.Release(pair);
                    }
                }
            }
            finally
            {
                indexer.Join();
                reopener.Join();
            }

            if (VERBOSE)
            {
                Console.WriteLine("TEST: now stop");
            }

            IOUtils.Close(mgr, tw, w, taxoDir, dir);
        }

        private class ThreadAnonymousInnerClassHelper : ThreadClass
        {
            private readonly TestSearcherTaxonomyManager outerInstance;

            private AtomicBoolean stop;
            private Lucene.Net.Facet.Taxonomy.SearcherTaxonomyManager mgr;

            public ThreadAnonymousInnerClassHelper(TestSearcherTaxonomyManager outerInstance, AtomicBoolean stop, Lucene.Net.Facet.Taxonomy.SearcherTaxonomyManager mgr)
            {
                this.outerInstance = outerInstance;
                this.stop = stop;
                this.mgr = mgr;
            }

            public override void Run()
            {
                while (!stop.Get())
                {
                    try
                    {
                        // Sleep for up to 20 msec:
                        Thread.Sleep(Random().Next(20));

                        if (VERBOSE)
                        {
                            Console.WriteLine("TEST: reopen");
                        }

                        mgr.MaybeRefresh();

                        if (VERBOSE)
                        {
                            Console.WriteLine("TEST: reopen done");
                        }
                    }
                    catch (Exception ioe)
                    {
                        throw new Exception(ioe.Message, ioe);
                    }
                }
            }
        }

        
        [Test]
        public virtual void TestDirectory()
        {
            Store.Directory indexDir = NewDirectory();
            Store.Directory taxoDir = NewDirectory();
            IndexWriter w = new IndexWriter(indexDir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
            var tw = new DirectoryTaxonomyWriter(taxoDir);
            // first empty commit
            w.Commit();
            tw.Commit();
            var mgr = new SearcherTaxonomyManager(indexDir, taxoDir, null);
            FacetsConfig config = new FacetsConfig();
            config.SetMultiValued("field", true);
            AtomicBoolean stop = new AtomicBoolean();

            // How many unique facets to index before stopping:
            int ordLimit = TEST_NIGHTLY ? 100000 : 6000;

            var indexer = new IndexerThread(w, config, tw, mgr, ordLimit, stop);
            indexer.Start();

            try
            {
                while (!stop.Get())
                {
                    SearcherAndTaxonomy pair = mgr.Acquire();
                    try
                    {
                        //System.out.println("search maxOrd=" + pair.taxonomyReader.getSize());
                        FacetsCollector sfc = new FacetsCollector();
                        pair.Searcher.Search(new MatchAllDocsQuery(), sfc);
                        Facets facets = GetTaxonomyFacetCounts(pair.TaxonomyReader, config, sfc);
                        FacetResult result = facets.GetTopChildren(10, "field");
                        if (pair.Searcher.IndexReader.NumDocs > 0)
                        {
                            //System.out.println(pair.taxonomyReader.getSize());
                            Assert.True(result.ChildCount > 0);
                            Assert.True(result.LabelValues.Length > 0);
                        }

                        //if (VERBOSE) {
                        //System.out.println("TEST: facets=" + FacetTestUtils.toString(results.get(0)));
                        //}
                    }
                    finally
                    {
                        mgr.Release(pair);
                    }
                }
            }
            finally
            {
                indexer.Join();
            }

            if (VERBOSE)
            {
                Console.WriteLine("TEST: now stop");
            }

            IOUtils.Close(mgr, tw, w, taxoDir, indexDir);
        }

        [Test]
        public virtual void TestReplaceTaxonomyNrt()
        {
            Store.Directory dir = NewDirectory();
            Store.Directory taxoDir = NewDirectory();
            IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
            var tw = new DirectoryTaxonomyWriter(taxoDir);

            Store.Directory taxoDir2 = NewDirectory();
            var tw2 = new DirectoryTaxonomyWriter(taxoDir2);
            tw2.Dispose();

            var mgr = new SearcherTaxonomyManager(w, true, null, tw);
            w.AddDocument(new Document());
            tw.ReplaceTaxonomy(taxoDir2);
            taxoDir2.Dispose();

            try
            {
                mgr.MaybeRefresh();
                Fail("should have hit exception");
            }
            catch (InvalidOperationException)
            {
                // expected
            }

            IOUtils.Close(mgr, tw, w, taxoDir, dir);
        }

        [Test]
        public virtual void TestReplaceTaxonomyDirectory()
        {
            Store.Directory indexDir = NewDirectory();
            Store.Directory taxoDir = NewDirectory();
            IndexWriter w = new IndexWriter(indexDir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
            var tw = new DirectoryTaxonomyWriter(taxoDir);
            w.Commit();
            tw.Commit();

            Store.Directory taxoDir2 = NewDirectory();
            var tw2 = new DirectoryTaxonomyWriter(taxoDir2);
            tw2.AddCategory(new FacetLabel("a", "b"));
            tw2.Dispose();

            var mgr = new SearcherTaxonomyManager(indexDir, taxoDir, null);
            SearcherAndTaxonomy pair = mgr.Acquire();
            try
            {
                Assert.AreEqual(1, pair.TaxonomyReader.Count);
            }
            finally
            {
                mgr.Release(pair);
            }

            w.AddDocument(new Document());
            tw.ReplaceTaxonomy(taxoDir2);
            taxoDir2.Dispose();
            w.Commit();
            tw.Commit();

            mgr.MaybeRefresh();
            pair = mgr.Acquire();
            try
            {
                Assert.AreEqual(3, pair.TaxonomyReader.Count);
            }
            finally
            {
                mgr.Release(pair);
            }

            IOUtils.Close(mgr, tw, w, taxoDir, indexDir);
        }

    }

}