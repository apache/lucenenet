using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using Lucene.Net.Support;
using NUnit.Framework;

namespace Lucene.Net.Facet.Taxonomy.Directory
{


    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using Document = Lucene.Net.Documents.Document;
    using MemoryOrdinalMap = Lucene.Net.Facet.Taxonomy.Directory.DirectoryTaxonomyWriter.MemoryOrdinalMap;
    using ITaxonomyWriterCache = Lucene.Net.Facet.Taxonomy.WriterCache.ITaxonomyWriterCache;
    using Cl2oTaxonomyWriterCache = Lucene.Net.Facet.Taxonomy.WriterCache.Cl2oTaxonomyWriterCache;
    using LruTaxonomyWriterCache = Lucene.Net.Facet.Taxonomy.WriterCache.LruTaxonomyWriterCache;
    using DirectoryReader = Lucene.Net.Index.DirectoryReader;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using IndexWriter = Lucene.Net.Index.IndexWriter;
    using OpenMode = Lucene.Net.Index.IndexWriterConfig.OpenMode_e;
    using IndexWriterConfig = Lucene.Net.Index.IndexWriterConfig;
    using SegmentInfos = Lucene.Net.Index.SegmentInfos;
    using IndexSearcher = Lucene.Net.Search.IndexSearcher;
    using AlreadyClosedException = Lucene.Net.Store.AlreadyClosedException;
    using Directory = Lucene.Net.Store.Directory;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using TestUtil = Lucene.Net.Util.TestUtil;

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

    public class TestDirectoryTaxonomyWriter : FacetTestCase
    {

        // A No-Op ITaxonomyWriterCache which always discards all given categories, and
        // always returns true in put(), to indicate some cache entries were cleared.
        private static ITaxonomyWriterCache NO_OP_CACHE = new TaxonomyWriterCacheAnonymousInnerClassHelper();

        private class TaxonomyWriterCacheAnonymousInnerClassHelper : ITaxonomyWriterCache
        {
            public virtual void Close()
            {
            }
            public virtual int Get(FacetLabel categoryPath)
            {
                return -1;
            }
            public virtual bool Put(FacetLabel categoryPath, int ordinal)
            {
                return true;
            }
            public virtual bool Full
            {
                get
                {
                    return true;
                }
            }
            public virtual void Clear()
            {
            }

        }

        [Test]
        public virtual void TestCommit()
        {
            // Verifies that nothing is committed to the underlying Directory, if
            // commit() wasn't called.
            Directory dir = NewDirectory();
            var ltw = new DirectoryTaxonomyWriter(dir, OpenMode.CREATE_OR_APPEND, NO_OP_CACHE);
            Assert.False(DirectoryReader.IndexExists(dir));
            ltw.Commit(); // first commit, so that an index will be created
            ltw.AddCategory(new FacetLabel("a"));

            IndexReader r = DirectoryReader.Open(dir);
            Assert.AreEqual(1, r.NumDocs, "No categories should have been committed to the underlying directory");
            r.Dispose();
            ltw.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestCommitUserData()
        {
            // Verifies taxonomy commit data
            Directory dir = NewDirectory();
            var taxoWriter = new DirectoryTaxonomyWriter(dir, OpenMode.CREATE_OR_APPEND, NO_OP_CACHE);
            taxoWriter.AddCategory(new FacetLabel("a"));
            taxoWriter.AddCategory(new FacetLabel("b"));
            IDictionary<string, string> userCommitData = new Dictionary<string, string>();
            userCommitData["testing"] = "1 2 3";
            taxoWriter.CommitData = userCommitData;
            taxoWriter.Dispose();
            var r = DirectoryReader.Open(dir);
            Assert.AreEqual(3, r.NumDocs, "2 categories plus root should have been committed to the underlying directory");
            var readUserCommitData = r.IndexCommit.UserData;
            Assert.True("1 2 3".Equals(readUserCommitData["testing"]), "wrong value extracted from commit data");
            Assert.NotNull(DirectoryTaxonomyWriter.INDEX_EPOCH + " not found in commitData", readUserCommitData[DirectoryTaxonomyWriter.INDEX_EPOCH]);
            r.Dispose();

            // open DirTaxoWriter again and commit, INDEX_EPOCH should still exist
            // in the commit data, otherwise DirTaxoReader.refresh() might not detect
            // that the taxonomy index has been recreated.
            taxoWriter = new DirectoryTaxonomyWriter(dir, OpenMode.CREATE_OR_APPEND, NO_OP_CACHE);
            taxoWriter.AddCategory(new FacetLabel("c")); // add a category so that commit will happen


            taxoWriter.CommitData = new Dictionary<string, string>()
		    {
			    {"just", "data"}
		    };
            taxoWriter.Commit();

            // verify taxoWriter.getCommitData()
            Assert.NotNull(DirectoryTaxonomyWriter.INDEX_EPOCH + " not found in taoxWriter.commitData", taxoWriter.CommitData[DirectoryTaxonomyWriter.INDEX_EPOCH]);
            taxoWriter.Dispose();

            r = DirectoryReader.Open(dir);
            readUserCommitData = r.IndexCommit.UserData;
            Assert.NotNull(DirectoryTaxonomyWriter.INDEX_EPOCH + " not found in commitData", readUserCommitData[DirectoryTaxonomyWriter.INDEX_EPOCH]);
            r.Dispose();

            dir.Dispose();
        }

        [Test]
        public virtual void TestRollback()
        {
            // Verifies that if rollback is called, DTW is closed.
            Directory dir = NewDirectory();
            var dtw = new DirectoryTaxonomyWriter(dir);
            dtw.AddCategory(new FacetLabel("a"));
            dtw.Rollback();
            try
            {
                dtw.AddCategory(new FacetLabel("a"));
                Fail("should not have succeeded to add a category following rollback.");
            }
            catch (AlreadyClosedException)
            {
                // expected
            }

            dir.Dispose();
        }

        [Test]
        public virtual void TestRecreateRollback()
        {
            // Tests rollback with OpenMode.CREATE
            Directory dir = NewDirectory();
            (new DirectoryTaxonomyWriter(dir)).Dispose();
            Assert.AreEqual(1, getEpoch(dir));
            (new DirectoryTaxonomyWriter(dir, OpenMode.CREATE)).Rollback();
            Assert.AreEqual(1, getEpoch(dir));

            dir.Dispose();
        }

        [Test]
        public virtual void TestEnsureOpen()
        {
            // verifies that an exception is thrown if DTW was closed
            Directory dir = NewDirectory();
            DirectoryTaxonomyWriter dtw = new DirectoryTaxonomyWriter(dir);
            dtw.Dispose();
            try
            {
                dtw.AddCategory(new FacetLabel("a"));
                Fail("should not have succeeded to add a category following close.");
            }
            catch (AlreadyClosedException)
            {
                // expected
            }
            dir.Dispose();
        }

        private void TouchTaxo(DirectoryTaxonomyWriter taxoWriter, FacetLabel cp)
        {
            taxoWriter.AddCategory(cp);
            taxoWriter.CommitData = new Dictionary<string, string>()
		{
			{"just", "data"}
		};
            taxoWriter.Commit();
        }

        [Test]
        public virtual void TestRecreateAndRefresh()
        {
            // DirTaxoWriter lost the INDEX_EPOCH property if it was opened in
            // CREATE_OR_APPEND (or commit(userData) called twice), which could lead to
            // DirTaxoReader succeeding to refresh().
            Directory dir = NewDirectory();

            DirectoryTaxonomyWriter taxoWriter = new DirectoryTaxonomyWriter(dir, OpenMode.CREATE_OR_APPEND, NO_OP_CACHE);
            TouchTaxo(taxoWriter, new FacetLabel("a"));

            var taxoReader = new DirectoryTaxonomyReader(dir);

            TouchTaxo(taxoWriter, new FacetLabel("b"));

            var newtr = TaxonomyReader.OpenIfChanged(taxoReader);
            taxoReader.Dispose();
            taxoReader = newtr;
            Assert.AreEqual(1, Convert.ToInt32(taxoReader.CommitUserData[DirectoryTaxonomyWriter.INDEX_EPOCH]));

            // now recreate the taxonomy, and check that the epoch is preserved after opening DirTW again.
            taxoWriter.Dispose();
            taxoWriter = new DirectoryTaxonomyWriter(dir, OpenMode.CREATE, NO_OP_CACHE);
            TouchTaxo(taxoWriter, new FacetLabel("c"));
            taxoWriter.Dispose();

            taxoWriter = new DirectoryTaxonomyWriter(dir, OpenMode.CREATE_OR_APPEND, NO_OP_CACHE);
            TouchTaxo(taxoWriter, new FacetLabel("d"));
            taxoWriter.Dispose();

            newtr = TaxonomyReader.OpenIfChanged(taxoReader);
            taxoReader.Dispose();
            taxoReader = newtr;
            Assert.AreEqual(2, Convert.ToInt32(taxoReader.CommitUserData[DirectoryTaxonomyWriter.INDEX_EPOCH]));

            taxoReader.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestBackwardsCompatibility()
        {
            // tests that if the taxonomy index doesn't have the INDEX_EPOCH
            // property (supports pre-3.6 indexes), all still works.
            Directory dir = NewDirectory();

            // create an empty index first, so that DirTaxoWriter initializes indexEpoch to 1.
            (new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, null))).Dispose();

            var taxoWriter = new DirectoryTaxonomyWriter(dir, OpenMode.CREATE_OR_APPEND, NO_OP_CACHE);
            taxoWriter.Dispose();

            var taxoReader = new DirectoryTaxonomyReader(dir);
            Assert.AreEqual(1, Convert.ToInt32(taxoReader.CommitUserData[DirectoryTaxonomyWriter.INDEX_EPOCH]));
            Assert.Null(TaxonomyReader.OpenIfChanged(taxoReader));
            (taxoReader).Dispose();

            dir.Dispose();
        }

        [Test]
        public virtual void TestConcurrency()
        {
            int ncats = AtLeast(100000); // add many categories
            int range = ncats * 3; // affects the categories selection
            AtomicInteger numCats = new AtomicInteger(ncats);
            Directory dir = NewDirectory();
            var values = new ConcurrentDictionary<string, string>();
            double d = Random().NextDouble();
            ITaxonomyWriterCache cache;
            if (d < 0.7)
            {
                // this is the fastest, yet most memory consuming
                cache = new Cl2oTaxonomyWriterCache(1024, 0.15f, 3);
            }
            else if (TEST_NIGHTLY && d > 0.98)
            {
                // this is the slowest, but tests the writer concurrency when no caching is done.
                // only pick it during NIGHTLY tests, and even then, with very low chances.
                cache = NO_OP_CACHE;
            }
            else
            {
                // this is slower than CL2O, but less memory consuming, and exercises finding categories on disk too.
                cache = new LruTaxonomyWriterCache(ncats / 10);
            }
            if (VERBOSE)
            {
                Console.WriteLine("TEST: use cache=" + cache);
            }
            var tw = new DirectoryTaxonomyWriter(dir, OpenMode.CREATE, cache);
            ThreadClass[] addThreads = new ThreadClass[AtLeast(4)];
            for (int z = 0; z < addThreads.Length; z++)
            {
                addThreads[z] = new ThreadAnonymousInnerClassHelper(this, range, numCats, values, tw);
            }

            foreach (var t in addThreads)
            {
                t.Start();
            }
            foreach (var t in addThreads)
            {
                t.Join();
            }
            tw.Dispose();

            DirectoryTaxonomyReader dtr = new DirectoryTaxonomyReader(dir);
            // +1 for root category
            if (values.Count + 1 != dtr.Size)
            {
                foreach (string value in values.Keys)
                {
                    FacetLabel label = new FacetLabel(FacetsConfig.StringToPath(value));
                    if (dtr.GetOrdinal(label) == -1)
                    {
                        Console.WriteLine("FAIL: path=" + label + " not recognized");
                    }
                }
                Fail("mismatch number of categories");
            }

            int[] parents = dtr.ParallelTaxonomyArrays.Parents;
            foreach (string cat in values.Keys)
            {
                FacetLabel cp = new FacetLabel(FacetsConfig.StringToPath(cat));
                Assert.True(dtr.GetOrdinal(cp) > 0, "category not found " + cp);
                int level = cp.Length;
                int parentOrd = 0; // for root, parent is always virtual ROOT (ord=0)
                FacetLabel path = new FacetLabel();
                for (int i = 0; i < level; i++)
                {
                    path = cp.Subpath(i + 1);
                    int ord = dtr.GetOrdinal(path);
                    Assert.AreEqual(parentOrd, parents[ord], "invalid parent for cp=" + path);
                    parentOrd = ord; // next level should have this parent
                }
            }

            IOUtils.Close(dtr, dir);
        }

        private class ThreadAnonymousInnerClassHelper : ThreadClass
        {
            private readonly TestDirectoryTaxonomyWriter outerInstance;

            private int range;
            private AtomicInteger numCats;
            private ConcurrentDictionary<string, string> values;
            private Lucene.Net.Facet.Taxonomy.Directory.DirectoryTaxonomyWriter tw;

            public ThreadAnonymousInnerClassHelper(TestDirectoryTaxonomyWriter outerInstance, int range, AtomicInteger numCats, ConcurrentDictionary<string, string> values, Lucene.Net.Facet.Taxonomy.Directory.DirectoryTaxonomyWriter tw)
            {
                this.outerInstance = outerInstance;
                this.range = range;
                this.numCats = numCats;
                this.values = values;
                this.tw = tw;
            }

            public override void Run()
            {
                Random random = Random();
                while (numCats.DecrementAndGet() > 0)
                {
                    try
                    {
                        int value = random.Next(range);
                        FacetLabel cp = new FacetLabel(Convert.ToString(value / 1000), Convert.ToString(value / 10000), Convert.ToString(value / 100000), Convert.ToString(value));
                        int ord = tw.AddCategory(cp);
                        Assert.True(tw.GetParent(ord) != -1, "invalid parent for ordinal " + ord + ", category " + cp);
                        string l1 = FacetsConfig.PathToString(cp.Components, 1);
                        string l2 = FacetsConfig.PathToString(cp.Components, 2);
                        string l3 = FacetsConfig.PathToString(cp.Components, 3);
                        string l4 = FacetsConfig.PathToString(cp.Components, 4);
                        values[l1] = l1;
                        values[l2] = l2;
                        values[l3] = l3;
                        values[l4] = l4;
                    }
                    catch (IOException e)
                    {
                        throw new Exception(e.Message, e);
                    }
                }
            }
        }

        private long getEpoch(Directory taxoDir)
        {
            SegmentInfos infos = new SegmentInfos();
            infos.Read(taxoDir);
            return Convert.ToInt64(infos.UserData[DirectoryTaxonomyWriter.INDEX_EPOCH]);
        }

        [Test]
        public virtual void TestReplaceTaxonomy()
        {
            Directory input = NewDirectory();
            var taxoWriter = new DirectoryTaxonomyWriter(input);
            int ordA = taxoWriter.AddCategory(new FacetLabel("a"));
            taxoWriter.Dispose();

            Directory dir = NewDirectory();
            taxoWriter = new DirectoryTaxonomyWriter(dir);
            int ordB = taxoWriter.AddCategory(new FacetLabel("b"));
            taxoWriter.AddCategory(new FacetLabel("c"));
            taxoWriter.Commit();

            long origEpoch = getEpoch(dir);

            // replace the taxonomy with the input one
            taxoWriter.ReplaceTaxonomy(input);

            // LUCENE-4633: make sure that category "a" is not added again in any case
            taxoWriter.AddTaxonomy(input, new MemoryOrdinalMap());
            Assert.AreEqual(2, taxoWriter.Size, "no categories should have been added"); // root + 'a'
            Assert.AreEqual(ordA, taxoWriter.AddCategory(new FacetLabel("a")), "category 'a' received new ordinal?");

            // add the same category again -- it should not receive the same ordinal !
            int newOrdB = taxoWriter.AddCategory(new FacetLabel("b"));
            Assert.AreNotSame(ordB, newOrdB, "new ordinal cannot be the original ordinal");
            Assert.AreEqual(2, newOrdB, "ordinal should have been 2 since only one category was added by replaceTaxonomy");

            taxoWriter.Dispose();

            long newEpoch = getEpoch(dir);
            Assert.True(origEpoch < newEpoch, "index epoch should have been updated after replaceTaxonomy");

            dir.Dispose();
            input.Dispose();
        }

        [Test]
        public virtual void TestReaderFreshness()
        {
            // ensures that the internal index reader is always kept fresh. Previously,
            // this simple scenario failed, if the cache just evicted the category that
            // is being added.
            Directory dir = NewDirectory();
            DirectoryTaxonomyWriter taxoWriter = new DirectoryTaxonomyWriter(dir, OpenMode.CREATE, NO_OP_CACHE);
            int o1 = taxoWriter.AddCategory(new FacetLabel("a"));
            int o2 = taxoWriter.AddCategory(new FacetLabel("a"));
            Assert.True(o1 == o2, "ordinal for same category that is added twice should be the same !");
            taxoWriter.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestCommitNoEmptyCommits()
        {
            // LUCENE-4972: DTW used to create empty commits even if no changes were made
            Directory dir = NewDirectory();
            DirectoryTaxonomyWriter taxoWriter = new DirectoryTaxonomyWriter(dir);
            taxoWriter.AddCategory(new FacetLabel("a"));
            taxoWriter.Commit();

            long gen1 = SegmentInfos.GetLastCommitGeneration(dir);
            taxoWriter.Commit();
            long gen2 = SegmentInfos.GetLastCommitGeneration(dir);
            Assert.AreEqual(gen1, gen2, "empty commit should not have changed the index");

            taxoWriter.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestCloseNoEmptyCommits()
        {
            // LUCENE-4972: DTW used to create empty commits even if no changes were made
            Directory dir = NewDirectory();
            DirectoryTaxonomyWriter taxoWriter = new DirectoryTaxonomyWriter(dir);
            taxoWriter.AddCategory(new FacetLabel("a"));
            taxoWriter.Commit();

            long gen1 = SegmentInfos.GetLastCommitGeneration(dir);
            taxoWriter.Dispose();
            long gen2 = SegmentInfos.GetLastCommitGeneration(dir);
            Assert.AreEqual(gen1, gen2, "empty commit should not have changed the index");

            taxoWriter.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestPrepareCommitNoEmptyCommits()
        {
            // LUCENE-4972: DTW used to create empty commits even if no changes were made
            Directory dir = NewDirectory();
            DirectoryTaxonomyWriter taxoWriter = new DirectoryTaxonomyWriter(dir);
            taxoWriter.AddCategory(new FacetLabel("a"));
            taxoWriter.PrepareCommit();
            taxoWriter.Commit();

            long gen1 = SegmentInfos.GetLastCommitGeneration(dir);
            taxoWriter.PrepareCommit();
            taxoWriter.Commit();
            long gen2 = SegmentInfos.GetLastCommitGeneration(dir);
            Assert.AreEqual(gen1, gen2, "empty commit should not have changed the index");

            taxoWriter.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestHugeLabel()
        {
            Directory indexDir = NewDirectory(), taxoDir = NewDirectory();
            IndexWriter indexWriter = new IndexWriter(indexDir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random())));
            DirectoryTaxonomyWriter taxoWriter = new DirectoryTaxonomyWriter(taxoDir, OpenMode.CREATE, new Cl2oTaxonomyWriterCache(2, 1f, 1));
            FacetsConfig config = new FacetsConfig();

            // Add one huge label:
            string bigs = null;
            int ordinal = -1;

            int len = FacetLabel.MAX_CATEGORY_PATH_LENGTH - 4; // for the dimension and separator
            bigs = TestUtil.RandomSimpleString(Random(), len, len);
            FacetField ff = new FacetField("dim", bigs);
            FacetLabel cp = new FacetLabel("dim", bigs);
            ordinal = taxoWriter.AddCategory(cp);
            Document doc = new Document();
            doc.Add(ff);
            indexWriter.AddDocument(config.Build(taxoWriter, doc));

            // Add tiny ones to cause a re-hash
            for (int i = 0; i < 3; i++)
            {
                string s = TestUtil.RandomSimpleString(Random(), 1, 10);
                taxoWriter.AddCategory(new FacetLabel("dim", s));
                doc = new Document();
                doc.Add(new FacetField("dim", s));
                indexWriter.AddDocument(config.Build(taxoWriter, doc));
            }

            // when too large components were allowed to be added, this resulted in a new added category
            Assert.AreEqual(ordinal, taxoWriter.AddCategory(cp));

            IOUtils.Close(indexWriter, taxoWriter);

            DirectoryReader indexReader = DirectoryReader.Open(indexDir);
            var taxoReader = new DirectoryTaxonomyReader(taxoDir);
            IndexSearcher searcher = new IndexSearcher(indexReader);
            DrillDownQuery ddq = new DrillDownQuery(new FacetsConfig());
            ddq.Add("dim", bigs);
            Assert.AreEqual(1, searcher.Search(ddq, 10).TotalHits);

            IOUtils.Close(indexReader, taxoReader, indexDir, taxoDir);
        }

        [Test]
        public virtual void TestReplaceTaxoWithLargeTaxonomy()
        {
            var srcTaxoDir = NewDirectory();
            var targetTaxoDir = NewDirectory();

            // build source, large, taxonomy
            DirectoryTaxonomyWriter taxoWriter = new DirectoryTaxonomyWriter(srcTaxoDir);
            int ord = taxoWriter.AddCategory(new FacetLabel("A", "1", "1", "1", "1", "1", "1"));
            taxoWriter.Dispose();

            taxoWriter = new DirectoryTaxonomyWriter(targetTaxoDir);
            int ordinal = taxoWriter.AddCategory(new FacetLabel("B", "1"));
            Assert.AreEqual(1, taxoWriter.GetParent(ordinal)); // call getParent to initialize taxoArrays
            taxoWriter.Commit();

            taxoWriter.ReplaceTaxonomy(srcTaxoDir);
            Assert.AreEqual(ord - 1, taxoWriter.GetParent(ord));
            taxoWriter.Dispose();

            srcTaxoDir.Dispose();
            targetTaxoDir.Dispose();
        }

    }

}