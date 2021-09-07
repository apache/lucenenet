using Lucene.Net.Documents;
using Lucene.Net.Search;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Index
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

    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using Field = Field;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using Occur = Lucene.Net.Search.Occur;
    using TestUtil = Lucene.Net.Util.TestUtil;

    [TestFixture]
    public class TestParallelAtomicReader : LuceneTestCase
    {
        private IndexSearcher parallel, single;
        private Directory dir, dir1, dir2;

        [Test]
        public virtual void TestQueries()
        {
            single = Single(Random);
            parallel = Parallel(Random);

            QueryTest(new TermQuery(new Term("f1", "v1")));
            QueryTest(new TermQuery(new Term("f1", "v2")));
            QueryTest(new TermQuery(new Term("f2", "v1")));
            QueryTest(new TermQuery(new Term("f2", "v2")));
            QueryTest(new TermQuery(new Term("f3", "v1")));
            QueryTest(new TermQuery(new Term("f3", "v2")));
            QueryTest(new TermQuery(new Term("f4", "v1")));
            QueryTest(new TermQuery(new Term("f4", "v2")));

            BooleanQuery bq1 = new BooleanQuery();
            bq1.Add(new TermQuery(new Term("f1", "v1")), Occur.MUST);
            bq1.Add(new TermQuery(new Term("f4", "v1")), Occur.MUST);
            QueryTest(bq1);

            single.IndexReader.Dispose();
            single = null;
            parallel.IndexReader.Dispose();
            parallel = null;
            dir.Dispose();
            dir = null;
            dir1.Dispose();
            dir1 = null;
            dir2.Dispose();
            dir2 = null;
        }

        [Test]
        public virtual void TestFieldNames()
        {
            Directory dir1 = GetDir1(Random);
            Directory dir2 = GetDir2(Random);
            ParallelAtomicReader pr = new ParallelAtomicReader(SlowCompositeReaderWrapper.Wrap(DirectoryReader.Open(dir1)), SlowCompositeReaderWrapper.Wrap(DirectoryReader.Open(dir2)));
            FieldInfos fieldInfos = pr.FieldInfos;
            Assert.AreEqual(4, fieldInfos.Count);
            Assert.IsNotNull(fieldInfos.FieldInfo("f1"));
            Assert.IsNotNull(fieldInfos.FieldInfo("f2"));
            Assert.IsNotNull(fieldInfos.FieldInfo("f3"));
            Assert.IsNotNull(fieldInfos.FieldInfo("f4"));
            pr.Dispose();
            dir1.Dispose();
            dir2.Dispose();
        }

        [Test]
        public virtual void TestRefCounts1()
        {
            Directory dir1 = GetDir1(Random);
            Directory dir2 = GetDir2(Random);
            AtomicReader ir1, ir2;
            // close subreaders, ParallelReader will not change refCounts, but close on its own close
            ParallelAtomicReader pr = new ParallelAtomicReader(ir1 = SlowCompositeReaderWrapper.Wrap(DirectoryReader.Open(dir1)), ir2 = SlowCompositeReaderWrapper.Wrap(DirectoryReader.Open(dir2)));

            // check RefCounts
            Assert.AreEqual(1, ir1.RefCount);
            Assert.AreEqual(1, ir2.RefCount);
            pr.Dispose();
            Assert.AreEqual(0, ir1.RefCount);
            Assert.AreEqual(0, ir2.RefCount);
            dir1.Dispose();
            dir2.Dispose();
        }

        [Test]
        public virtual void TestRefCounts2()
        {
            Directory dir1 = GetDir1(Random);
            Directory dir2 = GetDir2(Random);
            AtomicReader ir1 = SlowCompositeReaderWrapper.Wrap(DirectoryReader.Open(dir1));
            AtomicReader ir2 = SlowCompositeReaderWrapper.Wrap(DirectoryReader.Open(dir2));
            // don't close subreaders, so ParallelReader will increment refcounts
            ParallelAtomicReader pr = new ParallelAtomicReader(false, ir1, ir2);
            // check RefCounts
            Assert.AreEqual(2, ir1.RefCount);
            Assert.AreEqual(2, ir2.RefCount);
            pr.Dispose();
            Assert.AreEqual(1, ir1.RefCount);
            Assert.AreEqual(1, ir2.RefCount);
            ir1.Dispose();
            ir2.Dispose();
            Assert.AreEqual(0, ir1.RefCount);
            Assert.AreEqual(0, ir2.RefCount);
            dir1.Dispose();
            dir2.Dispose();
        }

        [Test]
        public virtual void TestCloseInnerReader()
        {
            Directory dir1 = GetDir1(Random);
            AtomicReader ir1 = SlowCompositeReaderWrapper.Wrap(DirectoryReader.Open(dir1));

            // with overlapping
            ParallelAtomicReader pr = new ParallelAtomicReader(true, new AtomicReader[] { ir1 }, new AtomicReader[] { ir1 });

            ir1.Dispose();

            try
            {
                pr.Document(0);
                Assert.Fail("ParallelAtomicReader should be already closed because inner reader was closed!");
            }
            catch (Exception e) when (e.IsAlreadyClosedException())
            {
                // pass
            }

            // noop:
            pr.Dispose();
            dir1.Dispose();
        }

        [Test]
        public virtual void TestIncompatibleIndexes()
        {
            // two documents:
            Directory dir1 = GetDir1(Random);

            // one document only:
            Directory dir2 = NewDirectory();
            IndexWriter w2 = new IndexWriter(dir2, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));
            Document d3 = new Document();

            d3.Add(NewTextField("f3", "v1", Field.Store.YES));
            w2.AddDocument(d3);
            w2.Dispose();

            AtomicReader ir1 = SlowCompositeReaderWrapper.Wrap(DirectoryReader.Open(dir1));
            AtomicReader ir2 = SlowCompositeReaderWrapper.Wrap(DirectoryReader.Open(dir2));

            try
            {
                new ParallelAtomicReader(ir1, ir2);
                Assert.Fail("didn't get exptected exception: indexes don't have same number of documents");
            }
            catch (Exception e) when (e.IsIllegalArgumentException())
            {
                // expected exception
            }

            try
            {
                new ParallelAtomicReader(Random.NextBoolean(), new AtomicReader[] { ir1, ir2 }, new AtomicReader[] { ir1, ir2 });
                Assert.Fail("didn't get expected exception: indexes don't have same number of documents");
            }
            catch (Exception e) when (e.IsIllegalArgumentException())
            {
                // expected exception
            }
            // check RefCounts
            Assert.AreEqual(1, ir1.RefCount);
            Assert.AreEqual(1, ir2.RefCount);
            ir1.Dispose();
            ir2.Dispose();
            dir1.Dispose();
            dir2.Dispose();
        }

        [Test]
        public virtual void TestIgnoreStoredFields()
        {
            Directory dir1 = GetDir1(Random);
            Directory dir2 = GetDir2(Random);
            AtomicReader ir1 = SlowCompositeReaderWrapper.Wrap(DirectoryReader.Open(dir1));
            AtomicReader ir2 = SlowCompositeReaderWrapper.Wrap(DirectoryReader.Open(dir2));

            // with overlapping
            ParallelAtomicReader pr = new ParallelAtomicReader(false, new AtomicReader[] { ir1, ir2 }, new AtomicReader[] { ir1 });
            Assert.AreEqual("v1", pr.Document(0).Get("f1"));
            Assert.AreEqual("v1", pr.Document(0).Get("f2"));
            Assert.IsNull(pr.Document(0).Get("f3"));
            Assert.IsNull(pr.Document(0).Get("f4"));
            // check that fields are there
            Assert.IsNotNull(pr.GetTerms("f1"));
            Assert.IsNotNull(pr.GetTerms("f2"));
            Assert.IsNotNull(pr.GetTerms("f3"));
            Assert.IsNotNull(pr.GetTerms("f4"));
            pr.Dispose();

            // no stored fields at all
            pr = new ParallelAtomicReader(false, new AtomicReader[] { ir2 }, new AtomicReader[0]);
            Assert.IsNull(pr.Document(0).Get("f1"));
            Assert.IsNull(pr.Document(0).Get("f2"));
            Assert.IsNull(pr.Document(0).Get("f3"));
            Assert.IsNull(pr.Document(0).Get("f4"));
            // check that fields are there
            Assert.IsNull(pr.GetTerms("f1"));
            Assert.IsNull(pr.GetTerms("f2"));
            Assert.IsNotNull(pr.GetTerms("f3"));
            Assert.IsNotNull(pr.GetTerms("f4"));
            pr.Dispose();

            // without overlapping
            pr = new ParallelAtomicReader(true, new AtomicReader[] { ir2 }, new AtomicReader[] { ir1 });
            Assert.AreEqual("v1", pr.Document(0).Get("f1"));
            Assert.AreEqual("v1", pr.Document(0).Get("f2"));
            Assert.IsNull(pr.Document(0).Get("f3"));
            Assert.IsNull(pr.Document(0).Get("f4"));
            // check that fields are there
            Assert.IsNull(pr.GetTerms("f1"));
            Assert.IsNull(pr.GetTerms("f2"));
            Assert.IsNotNull(pr.GetTerms("f3"));
            Assert.IsNotNull(pr.GetTerms("f4"));
            pr.Dispose();

            // no main readers
            try
            {
                new ParallelAtomicReader(true, new AtomicReader[0], new AtomicReader[] { ir1 });
                Assert.Fail("didn't get expected exception: need a non-empty main-reader array");
            }
            catch (Exception iae) when (iae.IsIllegalArgumentException())
            {
                // pass
            }

            dir1.Dispose();
            dir2.Dispose();
        }

        private void QueryTest(Query query)
        {
            ScoreDoc[] parallelHits = parallel.Search(query, null, 1000).ScoreDocs;
            ScoreDoc[] singleHits = single.Search(query, null, 1000).ScoreDocs;
            Assert.AreEqual(parallelHits.Length, singleHits.Length);
            for (int i = 0; i < parallelHits.Length; i++)
            {
                Assert.AreEqual(parallelHits[i].Score, singleHits[i].Score, 0.001f);
                Document docParallel = parallel.Doc(parallelHits[i].Doc);
                Document docSingle = single.Doc(singleHits[i].Doc);
                Assert.AreEqual(docParallel.Get("f1"), docSingle.Get("f1"));
                Assert.AreEqual(docParallel.Get("f2"), docSingle.Get("f2"));
                Assert.AreEqual(docParallel.Get("f3"), docSingle.Get("f3"));
                Assert.AreEqual(docParallel.Get("f4"), docSingle.Get("f4"));
            }
        }

        // Fields 1-4 indexed together:
        private IndexSearcher Single(Random random)
        {
            dir = NewDirectory();
            IndexWriter w = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random)));
            Document d1 = new Document();
            d1.Add(NewTextField("f1", "v1", Field.Store.YES));
            d1.Add(NewTextField("f2", "v1", Field.Store.YES));
            d1.Add(NewTextField("f3", "v1", Field.Store.YES));
            d1.Add(NewTextField("f4", "v1", Field.Store.YES));
            w.AddDocument(d1);
            Document d2 = new Document();
            d2.Add(NewTextField("f1", "v2", Field.Store.YES));
            d2.Add(NewTextField("f2", "v2", Field.Store.YES));
            d2.Add(NewTextField("f3", "v2", Field.Store.YES));
            d2.Add(NewTextField("f4", "v2", Field.Store.YES));
            w.AddDocument(d2);
            w.Dispose();

            DirectoryReader ir = DirectoryReader.Open(dir);
            return NewSearcher(ir);
        }

        // Fields 1 & 2 in one index, 3 & 4 in other, with ParallelReader:
        private IndexSearcher Parallel(Random random)
        {
            dir1 = GetDir1(random);
            dir2 = GetDir2(random);
            ParallelAtomicReader pr = new ParallelAtomicReader(SlowCompositeReaderWrapper.Wrap(DirectoryReader.Open(dir1)), SlowCompositeReaderWrapper.Wrap(DirectoryReader.Open(dir2)));
            TestUtil.CheckReader(pr);
            return NewSearcher(pr);
        }

        private Directory GetDir1(Random random)
        {
            Directory dir1 = NewDirectory();
            IndexWriter w1 = new IndexWriter(dir1, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random)));
            Document d1 = new Document();
            d1.Add(NewTextField("f1", "v1", Field.Store.YES));
            d1.Add(NewTextField("f2", "v1", Field.Store.YES));
            w1.AddDocument(d1);
            Document d2 = new Document();
            d2.Add(NewTextField("f1", "v2", Field.Store.YES));
            d2.Add(NewTextField("f2", "v2", Field.Store.YES));
            w1.AddDocument(d2);
            w1.Dispose();
            return dir1;
        }

        private Directory GetDir2(Random random)
        {
            Directory dir2 = NewDirectory();
            IndexWriter w2 = new IndexWriter(dir2, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(random)));
            Document d3 = new Document();
            d3.Add(NewTextField("f3", "v1", Field.Store.YES));
            d3.Add(NewTextField("f4", "v1", Field.Store.YES));
            w2.AddDocument(d3);
            Document d4 = new Document();
            d4.Add(NewTextField("f3", "v2", Field.Store.YES));
            d4.Add(NewTextField("f4", "v2", Field.Store.YES));
            w2.AddDocument(d4);
            w2.Dispose();
            return dir2;
        }
    }
}