/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;

using NUnit.Framework;

using Lucene.Net.Store;
using Lucene.Net.Index;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Lucene.Net.Search.Spans;
using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

namespace Lucene.Net.Search
{

    [TestFixture]
    public class TestCachingWrapperFilter : LuceneTestCase
    {
        [Test]
        public virtual void TestCachingWorks()
        {
            Directory dir = new RAMDirectory();
            IndexWriter writer = new IndexWriter(dir, new KeywordAnalyzer(), true, IndexWriter.MaxFieldLength.LIMITED);
            writer.Close();

            IndexReader reader = IndexReader.Open(dir, true);

            MockFilter filter = new MockFilter();
            CachingWrapperFilter cacher = new CachingWrapperFilter(filter);

            // first time, nested filter is called
            cacher.GetDocIdSet(reader);
            Assert.IsTrue(filter.WasCalled(), "first time");

            // make sure no exception if cache is holding the wrong DocIdSet
            cacher.GetDocIdSet(reader);

            // second time, nested filter should not be called
            filter.Clear();
            cacher.GetDocIdSet(reader);
            Assert.IsFalse(filter.WasCalled(), "second time");

            reader.Close();
        }


        [Test]
        public void TestNullDocIdSet()
        {
            Directory dir = new RAMDirectory();
            IndexWriter writer = new IndexWriter(dir, new KeywordAnalyzer(), true, IndexWriter.MaxFieldLength.LIMITED);
            writer.Close();

            IndexReader reader = IndexReader.Open(dir, true);

            Filter filter = new AnonymousFilter();

            CachingWrapperFilter cacher = new CachingWrapperFilter(filter);

            // the caching filter should return the empty set constant
            Assert.AreSame(DocIdSet.EMPTY_DOCIDSET, cacher.GetDocIdSet(reader));

            reader.Close();
        }

        class AnonymousFilter : Filter
        {
            public override DocIdSet GetDocIdSet(IndexReader reader)
            {
                return null;
            }
        }

        [Test]
        public void TestNullDocIdSetIterator()
        {
            Directory dir = new RAMDirectory();
            IndexWriter writer = new IndexWriter(dir, new KeywordAnalyzer(), true, IndexWriter.MaxFieldLength.LIMITED);
            writer.Close();

            IndexReader reader = IndexReader.Open(dir, true);

            Filter filter = new AnonymousFilter2();
            CachingWrapperFilter cacher = new CachingWrapperFilter(filter);

            // the caching filter should return the empty set constant
            Assert.AreSame(DocIdSet.EMPTY_DOCIDSET, cacher.GetDocIdSet(reader));

            reader.Close();
        }

        class AnonymousFilter2 : Filter
        {
            class AnonymousDocIdSet : DocIdSet
            {
                public override DocIdSetIterator Iterator()
                {
                    return null;
                }
            }

            public override DocIdSet GetDocIdSet(IndexReader reader)
            {
                return new AnonymousDocIdSet();// base.GetDocIdSet(reader);
            }
        }

        private static void assertDocIdSetCacheable(IndexReader reader, Filter filter, bool shouldCacheable)
        {
            CachingWrapperFilter cacher = new CachingWrapperFilter(filter);
            DocIdSet originalSet = filter.GetDocIdSet(reader);
            DocIdSet cachedSet = cacher.GetDocIdSet(reader);
            Assert.IsTrue(cachedSet.IsCacheable);
            Assert.AreEqual(shouldCacheable, originalSet.IsCacheable);
            //System.out.println("Original: "+originalSet.getClass().getName()+" -- cached: "+cachedSet.getClass().getName());
            if (originalSet.IsCacheable)
            {
                Assert.AreEqual(originalSet.GetType(), cachedSet.GetType(), "Cached DocIdSet must be of same class like uncached, if cacheable");
            }
            else
            {
                Assert.IsTrue(cachedSet is OpenBitSetDISI, "Cached DocIdSet must be an OpenBitSet if the original one was not cacheable");
            }
        }

        [Test]
        public void TestIsCacheable()
        {
            Directory dir = new RAMDirectory();
            IndexWriter writer = new IndexWriter(dir, new KeywordAnalyzer(), true, IndexWriter.MaxFieldLength.LIMITED);
            writer.Close();

            IndexReader reader = IndexReader.Open(dir, true);

            // not cacheable:
            assertDocIdSetCacheable(reader, new QueryWrapperFilter(new TermQuery(new Term("test", "value"))), false);
            // returns default empty docidset, always cacheable:
            assertDocIdSetCacheable(reader, NumericRangeFilter.NewIntRange("test", 10000, -10000, true, true), true);
            // is cacheable:
            assertDocIdSetCacheable(reader, FieldCacheRangeFilter.NewIntRange("test", 10, 20, true, true), true);
            // a openbitset filter is always cacheable
            assertDocIdSetCacheable(reader, new AnonymousFilter3(), true);

            reader.Close();
        }

        class AnonymousFilter3 : Filter
        {
            public override DocIdSet GetDocIdSet(IndexReader reader)
            {
                return new OpenBitSet();
            }
        }

        [Test]
        public void TestEnforceDeletions()
        {
            Directory dir = new MockRAMDirectory();
            IndexWriter writer = new IndexWriter(dir, new WhitespaceAnalyzer(), IndexWriter.MaxFieldLength.UNLIMITED);
            IndexReader reader = writer.GetReader();
            IndexSearcher searcher = new IndexSearcher(reader);

            // add a doc, refresh the reader, and check that its there
            Document doc = new Document();
            doc.Add(new Field("id", "1", Field.Store.YES, Field.Index.NOT_ANALYZED));
            writer.AddDocument(doc);

            reader = RefreshReader(reader);
            searcher = new IndexSearcher(reader);

            TopDocs docs = searcher.Search(new MatchAllDocsQuery(), 1);
            Assert.AreEqual(1, docs.TotalHits, "Should find a hit...");

            Filter startFilter = new QueryWrapperFilter(new TermQuery(new Term("id", "1")));

            // ignore deletions
            CachingWrapperFilter filter = new CachingWrapperFilter(startFilter, CachingWrapperFilter.DeletesMode.IGNORE);

            docs = searcher.Search(new MatchAllDocsQuery(), filter, 1);
            Assert.AreEqual(1, docs.TotalHits, "[query + filter] Should find a hit...");
            ConstantScoreQuery constantScore = new ConstantScoreQuery(filter);
            docs = searcher.Search(constantScore, 1);
            Assert.AreEqual(1, docs.TotalHits, "[just filter] Should find a hit...");

            // now delete the doc, refresh the reader, and see that it's not there
            writer.DeleteDocuments(new Term("id", "1"));

            reader = RefreshReader(reader);
            searcher = new IndexSearcher(reader);

            docs = searcher.Search(new MatchAllDocsQuery(), filter, 1);
            Assert.AreEqual(0, docs.TotalHits, "[query + filter] Should *not* find a hit...");

            docs = searcher.Search(constantScore, 1);
            Assert.AreEqual(1, docs.TotalHits, "[just filter] Should find a hit...");


            // force cache to regenerate:
            filter = new CachingWrapperFilter(startFilter, CachingWrapperFilter.DeletesMode.RECACHE);

            writer.AddDocument(doc);
            reader = RefreshReader(reader);
            searcher = new IndexSearcher(reader);

            docs = searcher.Search(new MatchAllDocsQuery(), filter, 1);
            Assert.AreEqual(1, docs.TotalHits, "[query + filter] Should find a hit...");

            constantScore = new ConstantScoreQuery(filter);
            docs = searcher.Search(constantScore, 1);
            Assert.AreEqual(1, docs.TotalHits, "[just filter] Should find a hit...");

            // make sure we get a cache hit when we reopen reader
            // that had no change to deletions
            IndexReader newReader = RefreshReader(reader);
            Assert.IsTrue(reader != newReader);
            reader = newReader;
            searcher = new IndexSearcher(reader);
            int missCount = filter.missCount;
            docs = searcher.Search(constantScore, 1);
            Assert.AreEqual(1, docs.TotalHits, "[just filter] Should find a hit...");
            Assert.AreEqual(missCount, filter.missCount);

            // now delete the doc, refresh the reader, and see that it's not there
            writer.DeleteDocuments(new Term("id", "1"));

            reader = RefreshReader(reader);
            searcher = new IndexSearcher(reader);

            missCount = filter.missCount;
            docs = searcher.Search(new MatchAllDocsQuery(), filter, 1);
            Assert.AreEqual(missCount + 1, filter.missCount);
            Assert.AreEqual(0, docs.TotalHits, "[query + filter] Should *not* find a hit...");
            docs = searcher.Search(constantScore, 1);
            Assert.AreEqual(0, docs.TotalHits, "[just filter] Should *not* find a hit...");


            // apply deletions dynamically
            filter = new CachingWrapperFilter(startFilter, CachingWrapperFilter.DeletesMode.DYNAMIC);

            writer.AddDocument(doc);
            reader = RefreshReader(reader);
            searcher = new IndexSearcher(reader);

            docs = searcher.Search(new MatchAllDocsQuery(), filter, 1);
            Assert.AreEqual(1, docs.TotalHits, "[query + filter] Should find a hit...");
            constantScore = new ConstantScoreQuery(filter);
            docs = searcher.Search(constantScore, 1);
            Assert.AreEqual(1, docs.TotalHits, "[just filter] Should find a hit...");

            // now delete the doc, refresh the reader, and see that it's not there
            writer.DeleteDocuments(new Term("id", "1"));

            reader = RefreshReader(reader);
            searcher = new IndexSearcher(reader);

            docs = searcher.Search(new MatchAllDocsQuery(), filter, 1);
            Assert.AreEqual(0, docs.TotalHits, "[query + filter] Should *not* find a hit...");

            missCount = filter.missCount;
            docs = searcher.Search(constantScore, 1);
            Assert.AreEqual(0, docs.TotalHits, "[just filter] Should *not* find a hit...");

            // doesn't count as a miss
            Assert.AreEqual(missCount, filter.missCount);
        }

        private static IndexReader RefreshReader(IndexReader reader)
        {
            IndexReader oldReader = reader;
            reader = reader.Reopen();
            if (reader != oldReader)
            {
                oldReader.Close();
            }
            return reader;
        }
    }
}