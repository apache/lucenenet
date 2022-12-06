using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using NUnit.Framework;
using System;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Search
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
    using IBits = Lucene.Net.Util.IBits;
    using Directory = Lucene.Net.Store.Directory;
    using DirectoryReader = Lucene.Net.Index.DirectoryReader;
    using Document = Documents.Document;
    using Field = Field;
    using FixedBitSet = Lucene.Net.Util.FixedBitSet;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using SerialMergeScheduler = Lucene.Net.Index.SerialMergeScheduler;
    using SlowCompositeReaderWrapper = Lucene.Net.Index.SlowCompositeReaderWrapper;
    using StringField = StringField;
    using Term = Lucene.Net.Index.Term;

    [TestFixture]
    public class TestCachingWrapperFilter : LuceneTestCase
    {
        private Directory dir;
        private DirectoryReader ir;
        private IndexSearcher @is;
        private RandomIndexWriter iw;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            dir = NewDirectory();
            iw = new RandomIndexWriter(Random, dir);
            Document doc = new Document();
            Field idField = new StringField("id", "", Field.Store.NO);
            doc.Add(idField);
            // add 500 docs with id 0..499
            for (int i = 0; i < 500; i++)
            {
                idField.SetStringValue(Convert.ToString(i));
                iw.AddDocument(doc);
            }
            // delete 20 of them
            for (int i = 0; i < 20; i++)
            {
                iw.DeleteDocuments(new Term("id", Convert.ToString(Random.Next(iw.MaxDoc))));
            }
            ir = iw.GetReader();
            @is = NewSearcher(ir);
        }

        [TearDown]
        public override void TearDown()
        {
            IOUtils.Dispose(iw, ir, dir);
            base.TearDown();
        }

        private void AssertFilterEquals(Filter f1, Filter f2)
        {
            Query query = new MatchAllDocsQuery();
            TopDocs hits1 = @is.Search(query, f1, ir.MaxDoc);
            TopDocs hits2 = @is.Search(query, f2, ir.MaxDoc);
            Assert.AreEqual(hits1.TotalHits, hits2.TotalHits);
            CheckHits.CheckEqual(query, hits1.ScoreDocs, hits2.ScoreDocs);
            // now do it again to confirm caching works
            TopDocs hits3 = @is.Search(query, f1, ir.MaxDoc);
            TopDocs hits4 = @is.Search(query, f2, ir.MaxDoc);
            Assert.AreEqual(hits3.TotalHits, hits4.TotalHits);
            CheckHits.CheckEqual(query, hits3.ScoreDocs, hits4.ScoreDocs);
        }

        /// <summary>
        /// test null iterator </summary>
        [Test]
        public virtual void TestEmpty()
        {
            Query query = new BooleanQuery();
            Filter expected = new QueryWrapperFilter(query);
            Filter actual = new CachingWrapperFilter(expected);
            AssertFilterEquals(expected, actual);
        }

        /// <summary>
        /// test iterator returns NO_MORE_DOCS </summary>
        [Test]
        public virtual void TestEmpty2()
        {
            BooleanQuery query = new BooleanQuery();
            query.Add(new TermQuery(new Term("id", "0")), Occur.MUST);
            query.Add(new TermQuery(new Term("id", "0")), Occur.MUST_NOT);
            Filter expected = new QueryWrapperFilter(query);
            Filter actual = new CachingWrapperFilter(expected);
            AssertFilterEquals(expected, actual);
        }

        /// <summary>
        /// test null docidset </summary>
        [Test]
        public virtual void TestEmpty3()
        {
            Filter expected = new PrefixFilter(new Term("bogusField", "bogusVal"));
            Filter actual = new CachingWrapperFilter(expected);
            AssertFilterEquals(expected, actual);
        }

        /// <summary>
        /// test iterator returns single document </summary>
        [Test]
        public virtual void TestSingle()
        {
            for (int i = 0; i < 10; i++)
            {
                int id = Random.Next(ir.MaxDoc);
                Query query = new TermQuery(new Term("id", Convert.ToString(id)));
                Filter expected = new QueryWrapperFilter(query);
                Filter actual = new CachingWrapperFilter(expected);
                AssertFilterEquals(expected, actual);
            }
        }

        /// <summary>
        /// test sparse filters (match single documents) </summary>
        [Test]
        public virtual void TestSparse()
        {
            for (int i = 0; i < 10; i++)
            {
                int id_start = Random.Next(ir.MaxDoc - 1);
                int id_end = id_start + 1;
                Query query = TermRangeQuery.NewStringRange("id", Convert.ToString(id_start), Convert.ToString(id_end), true, true);
                Filter expected = new QueryWrapperFilter(query);
                Filter actual = new CachingWrapperFilter(expected);
                AssertFilterEquals(expected, actual);
            }
        }

        /// <summary>
        /// test dense filters (match entire index) </summary>
        [Test]
        public virtual void TestDense()
        {
            Query query = new MatchAllDocsQuery();
            Filter expected = new QueryWrapperFilter(query);
            Filter actual = new CachingWrapperFilter(expected);
            AssertFilterEquals(expected, actual);
        }

        [Test]
        public virtual void TestCachingWorks()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);
            writer.Dispose();

            IndexReader reader = SlowCompositeReaderWrapper.Wrap(DirectoryReader.Open(dir));
            AtomicReaderContext context = (AtomicReaderContext)reader.Context;
            MockFilter filter = new MockFilter();
            CachingWrapperFilter cacher = new CachingWrapperFilter(filter);

            // first time, nested filter is called
            DocIdSet strongRef = cacher.GetDocIdSet(context, (context.AtomicReader).LiveDocs);
            Assert.IsTrue(filter.WasCalled(), "first time");

            // make sure no exception if cache is holding the wrong docIdSet
            cacher.GetDocIdSet(context, (context.AtomicReader).LiveDocs);

            // second time, nested filter should not be called
            filter.Clear();
            cacher.GetDocIdSet(context, (context.AtomicReader).LiveDocs);
            Assert.IsFalse(filter.WasCalled(), "second time");

            reader.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestNullDocIdSet()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);
            writer.Dispose();

            IndexReader reader = SlowCompositeReaderWrapper.Wrap(DirectoryReader.Open(dir));
            AtomicReaderContext context = (AtomicReaderContext)reader.Context;

            Filter filter = new FilterAnonymousClass(this, context);
            CachingWrapperFilter cacher = new CachingWrapperFilter(filter);

            // the caching filter should return the empty set constant
            //Assert.IsNull(cacher.GetDocIdSet(context, "second time", (context.AtomicReader).LiveDocs));
            Assert.IsNull(cacher.GetDocIdSet(context, (context.AtomicReader).LiveDocs));

            reader.Dispose();
            dir.Dispose();
        }

        private sealed class FilterAnonymousClass : Filter
        {
            private readonly TestCachingWrapperFilter outerInstance;

            private AtomicReaderContext context;

            public FilterAnonymousClass(TestCachingWrapperFilter outerInstance, AtomicReaderContext context)
            {
                this.outerInstance = outerInstance;
                this.context = context;
            }

            public override DocIdSet GetDocIdSet(AtomicReaderContext context, IBits acceptDocs)
            {
                return null;
            }
        }

        [Test]
        public virtual void TestNullDocIdSetIterator()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);
            writer.Dispose();

            IndexReader reader = SlowCompositeReaderWrapper.Wrap(DirectoryReader.Open(dir));
            AtomicReaderContext context = (AtomicReaderContext)reader.Context;

            Filter filter = new FilterAnonymousClass2(this, context);
            CachingWrapperFilter cacher = new CachingWrapperFilter(filter);

            // the caching filter should return the empty set constant
            Assert.IsNull(cacher.GetDocIdSet(context, (context.AtomicReader).LiveDocs));

            reader.Dispose();
            dir.Dispose();
        }

        private sealed class FilterAnonymousClass2 : Filter
        {
            private readonly TestCachingWrapperFilter outerInstance;

            private AtomicReaderContext context;

            public FilterAnonymousClass2(TestCachingWrapperFilter outerInstance, AtomicReaderContext context)
            {
                this.outerInstance = outerInstance;
                this.context = context;
            }

            public override DocIdSet GetDocIdSet(AtomicReaderContext context, IBits acceptDocs)
            {
                return new DocIdSetAnonymousClass(this);
            }

            private sealed class DocIdSetAnonymousClass : DocIdSet
            {
                private readonly FilterAnonymousClass2 outerInstance;

                public DocIdSetAnonymousClass(FilterAnonymousClass2 outerInstance)
                {
                    this.outerInstance = outerInstance;
                }

                public override DocIdSetIterator GetIterator()
                {
                    return null;
                }
            }
        }

        private static void AssertDocIdSetCacheable(IndexReader reader, Filter filter, bool shouldCacheable)
        {
            Assert.IsTrue(reader.Context is AtomicReaderContext);
            AtomicReaderContext context = (AtomicReaderContext)reader.Context;
            CachingWrapperFilter cacher = new CachingWrapperFilter(filter);
            DocIdSet originalSet = filter.GetDocIdSet(context, (context.AtomicReader).LiveDocs);
            DocIdSet cachedSet = cacher.GetDocIdSet(context, (context.AtomicReader).LiveDocs);
            if (originalSet is null)
            {
                Assert.IsNull(cachedSet);
            }
            if (cachedSet is null)
            {
                Assert.IsTrue(originalSet is null || originalSet.GetIterator() is null);
            }
            else
            {
                Assert.IsTrue(cachedSet.IsCacheable);
                Assert.AreEqual(shouldCacheable, originalSet.IsCacheable);
                //System.out.println("Original: "+originalSet.getClass().getName()+" -- cached: "+cachedSet.getClass().getName());
                if (originalSet.IsCacheable)
                {
                    Assert.AreEqual(originalSet.GetType(), cachedSet.GetType(), "Cached DocIdSet must be of same class like uncached, if cacheable");
                }
                else
                {
                    Assert.IsTrue(cachedSet is FixedBitSet || cachedSet is null, "Cached DocIdSet must be an FixedBitSet if the original one was not cacheable");
                }
            }
        }

        [Test]
        public virtual void TestIsCacheAble()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir);
            writer.AddDocument(new Document());
            writer.Dispose();

            IndexReader reader = SlowCompositeReaderWrapper.Wrap(DirectoryReader.Open(dir));

            // not cacheable:
            AssertDocIdSetCacheable(reader, new QueryWrapperFilter(new TermQuery(new Term("test", "value"))), false);
            // returns default empty docidset, always cacheable:
            AssertDocIdSetCacheable(reader, NumericRangeFilter.NewInt32Range("test", Convert.ToInt32(10000), Convert.ToInt32(-10000), true, true), true);
            // is cacheable:
            AssertDocIdSetCacheable(reader, FieldCacheRangeFilter.NewInt32Range("test", Convert.ToInt32(10), Convert.ToInt32(20), true, true), true);
            // a fixedbitset filter is always cacheable
            AssertDocIdSetCacheable(reader, new FilterAnonymousClass3(this), true);

            reader.Dispose();
            dir.Dispose();
        }

        private sealed class FilterAnonymousClass3 : Filter
        {
            private readonly TestCachingWrapperFilter outerInstance;

            public FilterAnonymousClass3(TestCachingWrapperFilter outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            public override DocIdSet GetDocIdSet(AtomicReaderContext context, IBits acceptDocs)
            {
                return new FixedBitSet(context.Reader.MaxDoc);
            }
        }

        [Test]
        public virtual void TestEnforceDeletions()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMergeScheduler(new SerialMergeScheduler()).SetMergePolicy(NewLogMergePolicy(10)));
            // asserts below requires no unexpected merges:

            // NOTE: cannot use writer.getReader because RIW (on
            // flipping a coin) may give us a newly opened reader,
            // but we use .reopen on this reader below and expect to
            // (must) get an NRT reader:
            DirectoryReader reader = DirectoryReader.Open(writer.IndexWriter, true);
            // same reason we don't wrap?
            IndexSearcher searcher = NewSearcher(reader, false);

            // add a doc, refresh the reader, and check that it's there
            Document doc = new Document();
            doc.Add(NewStringField("id", "1", Field.Store.YES));
            writer.AddDocument(doc);

            reader = RefreshReader(reader);
            searcher = NewSearcher(reader, false);

            TopDocs docs = searcher.Search(new MatchAllDocsQuery(), 1);
            Assert.AreEqual(1, docs.TotalHits, "Should find a hit...");

            Filter startFilter = new QueryWrapperFilter(new TermQuery(new Term("id", "1")));

            CachingWrapperFilter filter = new CachingWrapperFilter(startFilter);

            docs = searcher.Search(new MatchAllDocsQuery(), filter, 1);
            Assert.IsTrue(filter.GetSizeInBytes() > 0);

            Assert.AreEqual(1, docs.TotalHits, "[query + filter] Should find a hit...");

            Query constantScore = new ConstantScoreQuery(filter);
            docs = searcher.Search(constantScore, 1);
            Assert.AreEqual(1, docs.TotalHits, "[just filter] Should find a hit...");

            // make sure we get a cache hit when we reopen reader
            // that had no change to deletions

            // fake delete (deletes nothing):
            writer.DeleteDocuments(new Term("foo", "bar"));

            IndexReader oldReader = reader;
            reader = RefreshReader(reader);
            Assert.IsTrue(reader == oldReader);
            int missCount = filter.missCount;
            docs = searcher.Search(constantScore, 1);
            Assert.AreEqual(1, docs.TotalHits, "[just filter] Should find a hit...");

            // cache hit:
            Assert.AreEqual(missCount, filter.missCount);

            // now delete the doc, refresh the reader, and see that it's not there
            writer.DeleteDocuments(new Term("id", "1"));

            // NOTE: important to hold ref here so GC doesn't clear
            // the cache entry!  Else the assert below may sometimes
            // fail:
            oldReader = reader;
            reader = RefreshReader(reader);

            searcher = NewSearcher(reader, false);

            missCount = filter.missCount;
            docs = searcher.Search(new MatchAllDocsQuery(), filter, 1);
            Assert.AreEqual(0, docs.TotalHits, "[query + filter] Should *not* find a hit...");

            // cache hit
            Assert.AreEqual(missCount, filter.missCount);
            docs = searcher.Search(constantScore, 1);
            Assert.AreEqual(0, docs.TotalHits, "[just filter] Should *not* find a hit...");

            // apply deletes dynamically:
            filter = new CachingWrapperFilter(startFilter);
            writer.AddDocument(doc);
            reader = RefreshReader(reader);
            searcher = NewSearcher(reader, false);

            docs = searcher.Search(new MatchAllDocsQuery(), filter, 1);
            Assert.AreEqual(1, docs.TotalHits, "[query + filter] Should find a hit...");
            missCount = filter.missCount;
            Assert.IsTrue(missCount > 0);
            constantScore = new ConstantScoreQuery(filter);
            docs = searcher.Search(constantScore, 1);
            Assert.AreEqual(1, docs.TotalHits, "[just filter] Should find a hit...");
            Assert.AreEqual(missCount, filter.missCount);

            writer.AddDocument(doc);

            // NOTE: important to hold ref here so GC doesn't clear
            // the cache entry!  Else the assert below may sometimes
            // fail:
            oldReader = reader;

            reader = RefreshReader(reader);
            searcher = NewSearcher(reader, false);

            docs = searcher.Search(new MatchAllDocsQuery(), filter, 1);
            Assert.AreEqual(2, docs.TotalHits, "[query + filter] Should find 2 hits...");
            Assert.IsTrue(filter.missCount > missCount);
            missCount = filter.missCount;

            constantScore = new ConstantScoreQuery(filter);
            docs = searcher.Search(constantScore, 1);
            Assert.AreEqual(2, docs.TotalHits, "[just filter] Should find a hit...");
            Assert.AreEqual(missCount, filter.missCount);

            // now delete the doc, refresh the reader, and see that it's not there
            writer.DeleteDocuments(new Term("id", "1"));

            reader = RefreshReader(reader);
            searcher = NewSearcher(reader, false);

            docs = searcher.Search(new MatchAllDocsQuery(), filter, 1);
            Assert.AreEqual(0, docs.TotalHits, "[query + filter] Should *not* find a hit...");
            // CWF reused the same entry (it dynamically applied the deletes):
            Assert.AreEqual(missCount, filter.missCount);

            docs = searcher.Search(constantScore, 1);
            Assert.AreEqual(0, docs.TotalHits, "[just filter] Should *not* find a hit...");
            // CWF reused the same entry (it dynamically applied the deletes):
            Assert.AreEqual(missCount, filter.missCount);

            // NOTE: silliness to make sure JRE does not eliminate
            // our holding onto oldReader to prevent
            // CachingWrapperFilter's WeakHashMap from dropping the
            // entry:
            Assert.IsTrue(oldReader != null);

            reader.Dispose();
            writer.Dispose();
            dir.Dispose();
        }

        private static DirectoryReader RefreshReader(DirectoryReader reader)
        {
            DirectoryReader oldReader = reader;
            reader = DirectoryReader.OpenIfChanged(reader);
            if (reader != null)
            {
                oldReader.Dispose();
                return reader;
            }
            else
            {
                return oldReader;
            }
        }
    }
}