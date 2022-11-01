using Lucene.Net.Documents;
using Lucene.Net.Index.Extensions;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using Assert = Lucene.Net.TestFramework.Assert;
using Console = Lucene.Net.Util.SystemConsole;

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

    using DefaultSimilarity = Lucene.Net.Search.Similarities.DefaultSimilarity;
    using Directory = Lucene.Net.Store.Directory;
    using DirectoryReader = Lucene.Net.Index.DirectoryReader;
    using Document = Documents.Document;
    using Field = Field;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using IOContext = Lucene.Net.Store.IOContext;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;
    using RAMDirectory = Lucene.Net.Store.RAMDirectory;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using Similarity = Lucene.Net.Search.Similarities.Similarity;
    using Term = Lucene.Net.Index.Term;
    using TestUtil = Lucene.Net.Util.TestUtil;

    /// <summary>
    /// Test BooleanQuery2 against BooleanQuery by overriding the standard query parser.
    /// this also tests the scoring order of BooleanQuery.
    /// </summary>
    [TestFixture]
    public class TestBoolean2 : LuceneTestCase
    {
        private static IndexSearcher searcher;
        private static IndexSearcher bigSearcher;
        private static IndexReader reader;
        private static IndexReader littleReader;
        private static int NUM_EXTRA_DOCS = 6000;

        public const string field = "field";
        private static Directory directory;
        private static Directory dir2;
        private static int mulFactor;

        /// <summary>
        /// LUCENENET specific
        /// Is non-static because NewIndexWriterConfig is no longer static.
        /// </summary>
        [OneTimeSetUp]
        public override void BeforeClass()
        {
            base.BeforeClass();

            directory = NewDirectory();
            RandomIndexWriter writer = new RandomIndexWriter(Random, directory, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMergePolicy(NewLogMergePolicy()));
            for (int i = 0; i < docFields.Length; i++)
            {
                Document doc = new Document();
                doc.Add(NewTextField(field, docFields[i], Field.Store.NO));
                writer.AddDocument(doc);
            }
            writer.Dispose();
            littleReader = DirectoryReader.Open(directory);
            searcher = NewSearcher(littleReader);
            // this is intentionally using the baseline sim, because it compares against bigSearcher (which uses a random one)
            searcher.Similarity = new DefaultSimilarity();

            // Make big index
            dir2 = new MockDirectoryWrapper(Random, new RAMDirectory(directory, IOContext.DEFAULT));

            // First multiply small test index:
            mulFactor = 1;
            int docCount = 0;
            if (Verbose)
            {
                Console.WriteLine("\nTEST: now copy index...");
            }
            do
            {
                if (Verbose)
                {
                    Console.WriteLine("\nTEST: cycle...");
                }
                Directory copy = new MockDirectoryWrapper(Random, new RAMDirectory(dir2, IOContext.DEFAULT));
                RandomIndexWriter w = new RandomIndexWriter(Random, dir2);
                w.AddIndexes(copy);
                docCount = w.MaxDoc;
                w.Dispose();
                mulFactor *= 2;
            } while (docCount < 3000);

            RandomIndexWriter riw = new RandomIndexWriter(Random, dir2, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)).SetMaxBufferedDocs(TestUtil.NextInt32(Random, 50, 1000)));
            Document doc_ = new Document();
            doc_.Add(NewTextField("field2", "xxx", Field.Store.NO));
            for (int i = 0; i < NUM_EXTRA_DOCS / 2; i++)
            {
                riw.AddDocument(doc_);
            }
            doc_ = new Document();
            doc_.Add(NewTextField("field2", "big bad bug", Field.Store.NO));
            for (int i = 0; i < NUM_EXTRA_DOCS / 2; i++)
            {
                riw.AddDocument(doc_);
            }
            reader = riw.GetReader();
            bigSearcher = NewSearcher(reader);
            riw.Dispose();
        }

        [OneTimeTearDown]
        public override void AfterClass()
        {
            reader.Dispose();
            littleReader.Dispose();
            dir2.Dispose();
            directory.Dispose();
            searcher = null;
            reader = null;
            littleReader = null;
            dir2 = null;
            directory = null;
            bigSearcher = null;
            base.AfterClass();
        }

        private static readonly string[] docFields = new string[] { "w1 w2 w3 w4 w5", "w1 w3 w2 w3", "w1 xx w2 yy w3", "w1 w3 xx w2 yy w3" };

        public virtual void QueriesTest(Query query, int[] expDocNrs)
        {
            TopScoreDocCollector collector = TopScoreDocCollector.Create(1000, false);
            searcher.Search(query, null, collector);
            ScoreDoc[] hits1 = collector.GetTopDocs().ScoreDocs;

            collector = TopScoreDocCollector.Create(1000, true);
            searcher.Search(query, null, collector);
            ScoreDoc[] hits2 = collector.GetTopDocs().ScoreDocs;

            Assert.AreEqual(mulFactor * collector.TotalHits, bigSearcher.Search(query, 1).TotalHits);

            CheckHits.CheckHitsQuery(query, hits1, hits2, expDocNrs);
        }

        [Test]
        public virtual void TestQueries01()
        {
            BooleanQuery query = new BooleanQuery();
            query.Add(new TermQuery(new Term(field, "w3")), Occur.MUST);
            query.Add(new TermQuery(new Term(field, "xx")), Occur.MUST);
            int[] expDocNrs = new int[] { 2, 3 };
            QueriesTest(query, expDocNrs);
        }

        [Test]
        public virtual void TestQueries02()
        {
            BooleanQuery query = new BooleanQuery();
            query.Add(new TermQuery(new Term(field, "w3")), Occur.MUST);
            query.Add(new TermQuery(new Term(field, "xx")), Occur.SHOULD);
            int[] expDocNrs = new int[] { 2, 3, 1, 0 };
            QueriesTest(query, expDocNrs);
        }

        [Test]
        public virtual void TestQueries03()
        {
            BooleanQuery query = new BooleanQuery();
            query.Add(new TermQuery(new Term(field, "w3")), Occur.SHOULD);
            query.Add(new TermQuery(new Term(field, "xx")), Occur.SHOULD);
            int[] expDocNrs = new int[] { 2, 3, 1, 0 };
            QueriesTest(query, expDocNrs);
        }

        [Test]
        public virtual void TestQueries04()
        {
            BooleanQuery query = new BooleanQuery();
            query.Add(new TermQuery(new Term(field, "w3")), Occur.SHOULD);
            query.Add(new TermQuery(new Term(field, "xx")), Occur.MUST_NOT);
            int[] expDocNrs = new int[] { 1, 0 };
            QueriesTest(query, expDocNrs);
        }

        [Test]
        public virtual void TestQueries05()
        {
            BooleanQuery query = new BooleanQuery();
            query.Add(new TermQuery(new Term(field, "w3")), Occur.MUST);
            query.Add(new TermQuery(new Term(field, "xx")), Occur.MUST_NOT);
            int[] expDocNrs = new int[] { 1, 0 };
            QueriesTest(query, expDocNrs);
        }

        [Test]
        public virtual void TestQueries06()
        {
            BooleanQuery query = new BooleanQuery();
            query.Add(new TermQuery(new Term(field, "w3")), Occur.MUST);
            query.Add(new TermQuery(new Term(field, "xx")), Occur.MUST_NOT);
            query.Add(new TermQuery(new Term(field, "w5")), Occur.MUST_NOT);
            int[] expDocNrs = new int[] { 1 };
            QueriesTest(query, expDocNrs);
        }

        [Test]
        public virtual void TestQueries07()
        {
            BooleanQuery query = new BooleanQuery();
            query.Add(new TermQuery(new Term(field, "w3")), Occur.MUST_NOT);
            query.Add(new TermQuery(new Term(field, "xx")), Occur.MUST_NOT);
            query.Add(new TermQuery(new Term(field, "w5")), Occur.MUST_NOT);
            int[] expDocNrs = new int[] { };
            QueriesTest(query, expDocNrs);
        }

        [Test]
        public virtual void TestQueries08()
        {
            BooleanQuery query = new BooleanQuery();
            query.Add(new TermQuery(new Term(field, "w3")), Occur.MUST);
            query.Add(new TermQuery(new Term(field, "xx")), Occur.SHOULD);
            query.Add(new TermQuery(new Term(field, "w5")), Occur.MUST_NOT);
            int[] expDocNrs = new int[] { 2, 3, 1 };
            QueriesTest(query, expDocNrs);
        }

        [Test]
        public virtual void TestQueries09()
        {
            BooleanQuery query = new BooleanQuery();
            query.Add(new TermQuery(new Term(field, "w3")), Occur.MUST);
            query.Add(new TermQuery(new Term(field, "xx")), Occur.MUST);
            query.Add(new TermQuery(new Term(field, "w2")), Occur.MUST);
            query.Add(new TermQuery(new Term(field, "zz")), Occur.SHOULD);
            int[] expDocNrs = new int[] { 2, 3 };
            QueriesTest(query, expDocNrs);
        }

        [Test]
        public virtual void TestQueries10()
        {
            BooleanQuery query = new BooleanQuery();
            query.Add(new TermQuery(new Term(field, "w3")), Occur.MUST);
            query.Add(new TermQuery(new Term(field, "xx")), Occur.MUST);
            query.Add(new TermQuery(new Term(field, "w2")), Occur.MUST);
            query.Add(new TermQuery(new Term(field, "zz")), Occur.SHOULD);

            int[] expDocNrs = new int[] { 2, 3 };
            Similarity oldSimilarity = searcher.Similarity;
            try
            {
                searcher.Similarity = new DefaultSimilarityAnonymousClass(this);
                QueriesTest(query, expDocNrs);
            }
            finally
            {
                searcher.Similarity = oldSimilarity;
            }
        }

        private sealed class DefaultSimilarityAnonymousClass : DefaultSimilarity
        {
            private readonly TestBoolean2 outerInstance;

            public DefaultSimilarityAnonymousClass(TestBoolean2 outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            public override float Coord(int overlap, int maxOverlap)
            {
                return overlap / ((float)maxOverlap - 1);
            }
        }

        [Test]
        public virtual void TestRandomQueries()
        {
            string[] vals = new string[] { "w1", "w2", "w3", "w4", "w5", "xx", "yy", "zzz" };

            int tot = 0;

            BooleanQuery q1 = null;
            try
            {
                // increase number of iterations for more complete testing
                int num = AtLeast(20);
                for (int i = 0; i < num; i++)
                {
                    int level = Random.Next(3);
                    q1 = RandBoolQuery(new J2N.Randomizer(Random.NextInt64()), Random.NextBoolean(), level, field, vals, null);

                    // Can't sort by relevance since floating point numbers may not quite
                    // match up.
                    Sort sort = Sort.INDEXORDER;

                    QueryUtils.Check(Random, q1, searcher); // baseline sim
                    try
                    {
                        // a little hackish, QueryUtils.check is too costly to do on bigSearcher in this loop.
                        searcher.Similarity = bigSearcher.Similarity; // random sim
                        QueryUtils.Check(Random, q1, searcher);
                    }
                    finally
                    {
                        searcher.Similarity = new DefaultSimilarity(); // restore
                    }

                    TopFieldCollector collector = TopFieldCollector.Create(sort, 1000, false, true, true, true);

                    searcher.Search(q1, null, collector);
                    ScoreDoc[] hits1 = collector.GetTopDocs().ScoreDocs;

                    collector = TopFieldCollector.Create(sort, 1000, false, true, true, false);

                    searcher.Search(q1, null, collector);
                    ScoreDoc[] hits2 = collector.GetTopDocs().ScoreDocs;
                    tot += hits2.Length;
                    CheckHits.CheckEqual(q1, hits1, hits2);

                    BooleanQuery q3 = new BooleanQuery();
                    q3.Add(q1, Occur.SHOULD);
                    q3.Add(new PrefixQuery(new Term("field2", "b")), Occur.SHOULD);
                    TopDocs hits4 = bigSearcher.Search(q3, 1);
                    Assert.AreEqual(mulFactor * collector.TotalHits + NUM_EXTRA_DOCS / 2, hits4.TotalHits);
                }
            }
            catch (Exception e) when (e.IsException())
            {
                // For easier debugging
                Console.WriteLine("failed query: " + q1);
                throw; // LUCENENET: CA2200: Rethrow to preserve stack details (https://docs.microsoft.com/en-us/visualstudio/code-quality/ca2200-rethrow-to-preserve-stack-details)
            }

            // System.out.println("Total hits:"+tot);
        }

        // used to set properties or change every BooleanQuery
        // generated from randBoolQuery.
        public interface ICallback
        {
            void PostCreate(BooleanQuery q);
        }

        // Random rnd is passed in so that the exact same random query may be created
        // more than once.
        public static BooleanQuery RandBoolQuery(Random rnd, bool allowMust, int level, string field, string[] vals, ICallback cb)
        {
            BooleanQuery current = new BooleanQuery(rnd.Next() < 0);
            for (int i = 0; i < rnd.Next(vals.Length) + 1; i++)
            {
                int qType = 0; // term query
                if (level > 0)
                {
                    qType = rnd.Next(10);
                }
                Query q;
                if (qType < 3)
                {
                    q = new TermQuery(new Term(field, vals[rnd.Next(vals.Length)]));
                }
                else if (qType < 4)
                {
                    Term t1 = new Term(field, vals[rnd.Next(vals.Length)]);
                    Term t2 = new Term(field, vals[rnd.Next(vals.Length)]);
                    PhraseQuery pq = new PhraseQuery();
                    pq.Add(t1);
                    pq.Add(t2);
                    pq.Slop = 10; // increase possibility of matching
                    q = pq;
                }
                else if (qType < 7)
                {
                    q = new WildcardQuery(new Term(field, "w*"));
                }
                else
                {
                    q = RandBoolQuery(rnd, allowMust, level - 1, field, vals, cb);
                }

                int r = rnd.Next(10);
                Occur occur;
                if (r < 2)
                {
                    occur = Occur.MUST_NOT;
                }
                else if (r < 5)
                {
                    if (allowMust)
                    {
                        occur = Occur.MUST;
                    }
                    else
                    {
                        occur = Occur.SHOULD;
                    }
                }
                else
                {
                    occur = Occur.SHOULD;
                }

                current.Add(q, occur);
            }
            if (cb != null)
            {
                cb.PostCreate(current);
            }
            return current;
        }
    }
}