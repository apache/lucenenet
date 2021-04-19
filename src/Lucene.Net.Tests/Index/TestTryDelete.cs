using System;
using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using Lucene.Net.Search;
using NUnit.Framework;
using RandomizedTesting.Generators;
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
    using IndexSearcher = Lucene.Net.Search.IndexSearcher;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using RAMDirectory = Lucene.Net.Store.RAMDirectory;
    using ReferenceManager = Lucene.Net.Search.ReferenceManager;
    using SearcherFactory = Lucene.Net.Search.SearcherFactory;
    using SearcherManager = Lucene.Net.Search.SearcherManager;
    using Store = Field.Store;
    using StringField = StringField;
    using TermQuery = Lucene.Net.Search.TermQuery;
    using TopDocs = Lucene.Net.Search.TopDocs;

    [TestFixture]
    public class TestTryDelete : LuceneTestCase
    {
        private static IndexWriter GetWriter(Directory directory)
        {
            MergePolicy policy = new LogByteSizeMergePolicy();
            IndexWriterConfig conf = new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random));
            conf.SetMergePolicy(policy);
            conf.SetOpenMode(OpenMode.CREATE_OR_APPEND);

            IndexWriter writer = new IndexWriter(directory, conf);

            return writer;
        }

        private static Directory CreateIndex()
        {
            Directory directory = new RAMDirectory();

            IndexWriter writer = GetWriter(directory);

            for (int i = 0; i < 10; i++)
            {
                Document doc = new Document();
                doc.Add(new StringField("foo", Convert.ToString(i), Store.YES));
                writer.AddDocument(doc);
            }

            writer.Commit();
            writer.Dispose();

            return directory;
        }

        [Test]
        public virtual void TestTryDeleteDocument()
        {
            Directory directory = CreateIndex();

            IndexWriter writer = GetWriter(directory);

            ReferenceManager<IndexSearcher> mgr = new SearcherManager(writer, true, new SearcherFactory());

            TrackingIndexWriter mgrWriter = new TrackingIndexWriter(writer);

            IndexSearcher searcher = mgr.Acquire();

            TopDocs topDocs = searcher.Search(new TermQuery(new Term("foo", "0")), 100);
            Assert.AreEqual(1, topDocs.TotalHits);

            long result;
            if (Random.NextBoolean())
            {
                IndexReader r = DirectoryReader.Open(writer, true);
                result = mgrWriter.TryDeleteDocument(r, 0);
                r.Dispose();
            }
            else
            {
                result = mgrWriter.TryDeleteDocument(searcher.IndexReader, 0);
            }

            // The tryDeleteDocument should have succeeded:
            Assert.IsTrue(result != -1);

            Assert.IsTrue(writer.HasDeletions());

            if (Random.NextBoolean())
            {
                writer.Commit();
            }

            Assert.IsTrue(writer.HasDeletions());

            mgr.MaybeRefresh();

            searcher = mgr.Acquire();

            topDocs = searcher.Search(new TermQuery(new Term("foo", "0")), 100);

            Assert.AreEqual(0, topDocs.TotalHits);
        }

        [Test]
        public virtual void TestTryDeleteDocumentCloseAndReopen()
        {
            Directory directory = CreateIndex();

            IndexWriter writer = GetWriter(directory);

            ReferenceManager<IndexSearcher> mgr = new SearcherManager(writer, true, new SearcherFactory());

            IndexSearcher searcher = mgr.Acquire();

            TopDocs topDocs = searcher.Search(new TermQuery(new Term("foo", "0")), 100);
            Assert.AreEqual(1, topDocs.TotalHits);

            TrackingIndexWriter mgrWriter = new TrackingIndexWriter(writer);
            long result = mgrWriter.TryDeleteDocument(DirectoryReader.Open(writer, true), 0);

            Assert.AreEqual(1, result);

            writer.Commit();

            Assert.IsTrue(writer.HasDeletions());

            mgr.MaybeRefresh();

            searcher = mgr.Acquire();

            topDocs = searcher.Search(new TermQuery(new Term("foo", "0")), 100);

            Assert.AreEqual(0, topDocs.TotalHits);

            writer.Dispose();

            searcher = new IndexSearcher(DirectoryReader.Open(directory));

            topDocs = searcher.Search(new TermQuery(new Term("foo", "0")), 100);

            Assert.AreEqual(0, topDocs.TotalHits);
        }

        [Test]
        public virtual void TestDeleteDocuments()
        {
            Directory directory = CreateIndex();

            IndexWriter writer = GetWriter(directory);

            ReferenceManager<IndexSearcher> mgr = new SearcherManager(writer, true, new SearcherFactory());

            IndexSearcher searcher = mgr.Acquire();

            TopDocs topDocs = searcher.Search(new TermQuery(new Term("foo", "0")), 100);
            Assert.AreEqual(1, topDocs.TotalHits);

            TrackingIndexWriter mgrWriter = new TrackingIndexWriter(writer);
            long result = mgrWriter.DeleteDocuments(new TermQuery(new Term("foo", "0")));

            Assert.AreEqual(1, result);

            // writer.Commit();

            Assert.IsTrue(writer.HasDeletions());

            mgr.MaybeRefresh();

            searcher = mgr.Acquire();

            topDocs = searcher.Search(new TermQuery(new Term("foo", "0")), 100);

            Assert.AreEqual(0, topDocs.TotalHits);
        }
    }
}