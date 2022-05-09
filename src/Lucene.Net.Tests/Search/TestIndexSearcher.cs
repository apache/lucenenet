using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Support.Threading;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

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

    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using Field = Field;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;

    [TestFixture]
    public class TestIndexSearcher : LuceneTestCase
    {
        private Directory dir;
        private IndexReader reader;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            dir = NewDirectory();
            RandomIndexWriter iw = new RandomIndexWriter(Random, dir);
            for (int i = 0; i < 100; i++)
            {
                Document doc = new Document();
                doc.Add(NewStringField("field", Convert.ToString(i), Field.Store.NO));
                doc.Add(NewStringField("field2", Convert.ToString(i % 2 == 0), Field.Store.NO));
                iw.AddDocument(doc);
            }
            reader = iw.GetReader();
            iw.Dispose();
        }

        [TearDown]
        public override void TearDown()
        {
            reader.Dispose();
            dir.Dispose();
            base.TearDown();
        }

        // should not throw exception
        [Test]
        public virtual void TestHugeN()
        {
            TaskScheduler service = new LimitedConcurrencyLevelTaskScheduler(4);

            IndexSearcher[] searchers = new IndexSearcher[] { new IndexSearcher(reader), new IndexSearcher(reader, service) };
            Query[] queries = new Query[] { new MatchAllDocsQuery(), new TermQuery(new Term("field", "1")) };
            Sort[] sorts = new Sort[] { null, new Sort(new SortField("field2", SortFieldType.STRING)) };
            Filter[] filters = new Filter[] { null, new QueryWrapperFilter(new TermQuery(new Term("field2", "true"))) };
            ScoreDoc[] afters = new ScoreDoc[] { null, new FieldDoc(0, 0f, new object[] { new BytesRef("boo!") }) };

            foreach (IndexSearcher searcher in searchers)
            {
                foreach (ScoreDoc after in afters)
                {
                    foreach (Query query in queries)
                    {
                        foreach (Sort sort in sorts)
                        {
                            foreach (Filter filter in filters)
                            {
                                searcher.Search(query, int.MaxValue);
                                searcher.SearchAfter(after, query, int.MaxValue);
                                searcher.Search(query, filter, int.MaxValue);
                                searcher.SearchAfter(after, query, filter, int.MaxValue);
                                if (sort != null)
                                {
                                    searcher.Search(query, int.MaxValue, sort);
                                    searcher.Search(query, filter, int.MaxValue, sort);
                                    searcher.Search(query, filter, int.MaxValue, sort, true, true);
                                    searcher.Search(query, filter, int.MaxValue, sort, true, false);
                                    searcher.Search(query, filter, int.MaxValue, sort, false, true);
                                    searcher.Search(query, filter, int.MaxValue, sort, false, false);
                                    searcher.SearchAfter(after, query, filter, int.MaxValue, sort);
                                    searcher.SearchAfter(after, query, filter, int.MaxValue, sort, true, true);
                                    searcher.SearchAfter(after, query, filter, int.MaxValue, sort, true, false);
                                    searcher.SearchAfter(after, query, filter, int.MaxValue, sort, false, true);
                                    searcher.SearchAfter(after, query, filter, int.MaxValue, sort, false, false);
                                }
                            }
                        }
                    }
                }
            }

            // LUCENENET: .NET doesn't have a way to shut down the TaskScheduler explicitly
            //TestUtil.ShutdownExecutorService(service);
        }

        [Test]
        public virtual void TestSearchAfterPassedMaxDoc()
        {
            // LUCENE-5128: ensure we get a meaningful message if searchAfter exceeds maxDoc
            Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random, dir);
            w.AddDocument(new Document());
            IndexReader r = w.GetReader();
            w.Dispose();

            IndexSearcher s = new IndexSearcher(r);
            try
            {
                s.SearchAfter(new ScoreDoc(r.MaxDoc, 0.54f), new MatchAllDocsQuery(), 10);
                Assert.Fail("should have hit IllegalArgumentException when searchAfter exceeds maxDoc");
            }
            catch (Exception e) when (e.IsIllegalArgumentException())
            {
                // ok
            }
            finally
            {
                IOUtils.Dispose(r, dir);
            }
        }
    }
}