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

using System;
using System.Collections.Generic;
using System.Text;

using Lucene.Net.Store;
using Lucene.Net.Index;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Search;
using Lucene.Net.Search.Spans;
using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

using NUnit.Framework;

namespace Lucene.Net.Search
{
    [TestFixture]
    public class TestCachingSpanFilter : LuceneTestCase
    {
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

            SpanFilter startFilter = new SpanQueryFilter(new SpanTermQuery(new Term("id", "1")));

            // ignore deletions
            CachingSpanFilter filter = new CachingSpanFilter(startFilter, CachingWrapperFilter.DeletesMode.IGNORE);

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
            filter = new CachingSpanFilter(startFilter, CachingWrapperFilter.DeletesMode.RECACHE);

            writer.AddDocument(doc);
            reader = RefreshReader(reader);
            searcher = new IndexSearcher(reader);

            docs = searcher.Search(new MatchAllDocsQuery(), filter, 1);
            Assert.AreEqual(1, docs.TotalHits, "[query + filter] Should find a hit...");

            constantScore = new ConstantScoreQuery(filter);
            docs = searcher.Search(constantScore, 1);
            Assert.AreEqual(1, docs.TotalHits, "[just filter] Should find a hit...");

            // make sure we get a cache hit when we reopen readers
            // that had no new deletions
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

            docs = searcher.Search(new MatchAllDocsQuery(), filter, 1);
            Assert.AreEqual(0, docs.TotalHits, "[query + filter] Should *not* find a hit...");

            docs = searcher.Search(constantScore, 1);
            Assert.AreEqual(0, docs.TotalHits, "[just filter] Should *not* find a hit...");
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
