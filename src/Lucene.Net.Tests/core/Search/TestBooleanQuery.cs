using System;
using System.Collections.Generic;
using Lucene.Net.Documents;

namespace Lucene.Net.Search
{
    using Index;
    using NUnit.Framework;
    using Support;
    using System.Threading.Tasks;
    using Util;

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

    using Analyzer = Lucene.Net.Analysis.Analyzer;
    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using DefaultSimilarity = Lucene.Net.Search.Similarities.DefaultSimilarity;
    using Directory = Lucene.Net.Store.Directory;
    using DirectoryReader = Lucene.Net.Index.DirectoryReader;
    using Document = Documents.Document;
    using Field = Field;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using IndexWriter = Lucene.Net.Index.IndexWriter;
    using IndexWriterConfig = Lucene.Net.Index.IndexWriterConfig;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using SpanQuery = Lucene.Net.Search.Spans.SpanQuery;
    using SpanTermQuery = Lucene.Net.Search.Spans.SpanTermQuery;
    using Term = Lucene.Net.Index.Term;
    using TextField = TextField;

    [TestFixture]
    public class TestBooleanQuery : LuceneTestCase
    {
        [Test]
        public virtual void TestEquality()
        {
            BooleanQuery bq1 = new BooleanQuery();
            bq1.Add(new TermQuery(new Term("field", "value1")), BooleanClause.Occur.SHOULD);
            bq1.Add(new TermQuery(new Term("field", "value2")), BooleanClause.Occur.SHOULD);
            BooleanQuery nested1 = new BooleanQuery();
            nested1.Add(new TermQuery(new Term("field", "nestedvalue1")), BooleanClause.Occur.SHOULD);
            nested1.Add(new TermQuery(new Term("field", "nestedvalue2")), BooleanClause.Occur.SHOULD);
            bq1.Add(nested1, BooleanClause.Occur.SHOULD);

            BooleanQuery bq2 = new BooleanQuery();
            bq2.Add(new TermQuery(new Term("field", "value1")), BooleanClause.Occur.SHOULD);
            bq2.Add(new TermQuery(new Term("field", "value2")), BooleanClause.Occur.SHOULD);
            BooleanQuery nested2 = new BooleanQuery();
            nested2.Add(new TermQuery(new Term("field", "nestedvalue1")), BooleanClause.Occur.SHOULD);
            nested2.Add(new TermQuery(new Term("field", "nestedvalue2")), BooleanClause.Occur.SHOULD);
            bq2.Add(nested2, BooleanClause.Occur.SHOULD);

            Assert.IsTrue(bq1.Equals(bq2));
            //Assert.AreEqual(bq1, bq2);
        }

        [Test]
        public virtual void TestException()
        {
            try
            {
                BooleanQuery.MaxClauseCount = 0;
                Assert.Fail();
            }
            catch (System.ArgumentException e)
            {
                // okay
            }
        }

        // LUCENE-1630
        [Test]
        public virtual void TestNullOrSubScorer()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            Document doc = new Document();
            doc.Add(NewTextField("field", "a b c d", Field.Store.NO));
            w.AddDocument(doc);

            IndexReader r = w.Reader;
            IndexSearcher s = NewSearcher(r);
            // this test relies upon coord being the default implementation,
            // otherwise scores are different!
            s.Similarity = new DefaultSimilarity();

            BooleanQuery q = new BooleanQuery();
            q.Add(new TermQuery(new Term("field", "a")), BooleanClause.Occur.SHOULD);

            // LUCENE-2617: make sure that a term not in the index still contributes to the score via coord factor
            float score = s.Search(q, 10).MaxScore;
            Query subQuery = new TermQuery(new Term("field", "not_in_index"));
            subQuery.Boost = 0;
            q.Add(subQuery, BooleanClause.Occur.SHOULD);
            float score2 = s.Search(q, 10).MaxScore;
            Assert.AreEqual(score * .5F, score2, 1e-6);

            // LUCENE-2617: make sure that a clause not in the index still contributes to the score via coord factor
            BooleanQuery qq = (BooleanQuery)q.Clone();
            PhraseQuery phrase = new PhraseQuery();
            phrase.Add(new Term("field", "not_in_index"));
            phrase.Add(new Term("field", "another_not_in_index"));
            phrase.Boost = 0;
            qq.Add(phrase, BooleanClause.Occur.SHOULD);
            score2 = s.Search(qq, 10).MaxScore;
            Assert.AreEqual(score * (1 / 3F), score2, 1e-6);

            // now test BooleanScorer2
            subQuery = new TermQuery(new Term("field", "b"));
            subQuery.Boost = 0;
            q.Add(subQuery, BooleanClause.Occur.MUST);
            score2 = s.Search(q, 10).MaxScore;
            Assert.AreEqual(score * (2 / 3F), score2, 1e-6);

            // PhraseQuery w/ no terms added returns a null scorer
            PhraseQuery pq = new PhraseQuery();
            q.Add(pq, BooleanClause.Occur.SHOULD);
            Assert.AreEqual(1, s.Search(q, 10).TotalHits);

            // A required clause which returns null scorer should return null scorer to
            // IndexSearcher.
            q = new BooleanQuery();
            pq = new PhraseQuery();
            q.Add(new TermQuery(new Term("field", "a")), BooleanClause.Occur.SHOULD);
            q.Add(pq, BooleanClause.Occur.MUST);
            Assert.AreEqual(0, s.Search(q, 10).TotalHits);

            DisjunctionMaxQuery dmq = new DisjunctionMaxQuery(1.0f);
            dmq.Add(new TermQuery(new Term("field", "a")));
            dmq.Add(pq);
            Assert.AreEqual(1, s.Search(dmq, 10).TotalHits);

            r.Dispose();
            w.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestDeMorgan()
        {
            Directory dir1 = NewDirectory();
            RandomIndexWriter iw1 = new RandomIndexWriter(Random(), dir1, Similarity, TimeZone);
            Document doc1 = new Document();
            doc1.Add(NewTextField("field", "foo bar", Field.Store.NO));
            iw1.AddDocument(doc1);
            IndexReader reader1 = iw1.Reader;
            iw1.Dispose();

            Directory dir2 = NewDirectory();
            RandomIndexWriter iw2 = new RandomIndexWriter(Random(), dir2, Similarity, TimeZone);
            Document doc2 = new Document();
            doc2.Add(NewTextField("field", "foo baz", Field.Store.NO));
            iw2.AddDocument(doc2);
            IndexReader reader2 = iw2.Reader;
            iw2.Dispose();

            BooleanQuery query = new BooleanQuery(); // Query: +foo -ba*
            query.Add(new TermQuery(new Term("field", "foo")), BooleanClause.Occur.MUST);
            WildcardQuery wildcardQuery = new WildcardQuery(new Term("field", "ba*"));
            wildcardQuery.SetRewriteMethod(MultiTermQuery.SCORING_BOOLEAN_QUERY_REWRITE);
            query.Add(wildcardQuery, BooleanClause.Occur.MUST_NOT);

            MultiReader multireader = new MultiReader(reader1, reader2);
            IndexSearcher searcher = NewSearcher(multireader);
            Assert.AreEqual(0, searcher.Search(query, 10).TotalHits);


            Task foo = new Task(TestDeMorgan);

            TaskScheduler es = TaskScheduler.Default;
            searcher = new IndexSearcher(multireader, es);
            if (VERBOSE)
            {
                Console.WriteLine("rewritten form: " + searcher.Rewrite(query));
            }
            Assert.AreEqual(0, searcher.Search(query, 10).TotalHits);

            multireader.Dispose();
            reader1.Dispose();
            reader2.Dispose();
            dir1.Dispose();
            dir2.Dispose();
        }

        [Test]
        public virtual void TestBS2DisjunctionNextVsAdvance()
        {
            Directory d = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random(), d, Similarity, TimeZone);
            int numDocs = AtLeast(300);
            for (int docUpto = 0; docUpto < numDocs; docUpto++)
            {
                string contents = "a";
                if (Random().Next(20) <= 16)
                {
                    contents += " b";
                }
                if (Random().Next(20) <= 8)
                {
                    contents += " c";
                }
                if (Random().Next(20) <= 4)
                {
                    contents += " d";
                }
                if (Random().Next(20) <= 2)
                {
                    contents += " e";
                }
                if (Random().Next(20) <= 1)
                {
                    contents += " f";
                }
                Document doc = new Document();
                doc.Add(new TextField("field", contents, Field.Store.NO));
                w.AddDocument(doc);
            }
            w.ForceMerge(1);
            IndexReader r = w.Reader;
            IndexSearcher s = NewSearcher(r);
            w.Dispose();

            for (int iter = 0; iter < 10 * RANDOM_MULTIPLIER; iter++)
            {
                if (VERBOSE)
                {
                    Console.WriteLine("iter=" + iter);
                }
                IList<string> terms = new List<string>(Arrays.AsList("a", "b", "c", "d", "e", "f"));
                int numTerms = TestUtil.NextInt(Random(), 1, terms.Count);
                while (terms.Count > numTerms)
                {
                    terms.RemoveAt(Random().Next(terms.Count));
                }

                if (VERBOSE)
                {
                    Console.WriteLine("  terms=" + terms);
                }

                BooleanQuery q = new BooleanQuery();
                foreach (string term in terms)
                {
                    q.Add(new BooleanClause(new TermQuery(new Term("field", term)), BooleanClause.Occur.SHOULD));
                }

                Weight weight = s.CreateNormalizedWeight(q);

                Scorer scorer = weight.Scorer(s.LeafContexts[0], null);

                // First pass: just use .NextDoc() to gather all hits
                IList<ScoreDoc> hits = new List<ScoreDoc>();
                while (scorer.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
                {
                    hits.Add(new ScoreDoc(scorer.DocID(), scorer.Score()));
                }

                if (VERBOSE)
                {
                    Console.WriteLine("  " + hits.Count + " hits");
                }

                // Now, randomly next/advance through the list and
                // verify exact match:
                for (int iter2 = 0; iter2 < 10; iter2++)
                {
                    weight = s.CreateNormalizedWeight(q);
                    scorer = weight.Scorer(s.LeafContexts[0], null);

                    if (VERBOSE)
                    {
                        Console.WriteLine("  iter2=" + iter2);
                    }

                    int upto = -1;
                    while (upto < hits.Count)
                    {
                        int nextUpto;
                        int nextDoc;
                        int left = hits.Count - upto;
                        if (left == 1 || Random().nextBoolean())
                        {
                            // next
                            nextUpto = 1 + upto;
                            nextDoc = scorer.NextDoc();
                        }
                        else
                        {
                            // advance
                            int inc = TestUtil.NextInt(Random(), 1, left - 1);
                            nextUpto = inc + upto;
                            nextDoc = scorer.Advance(hits[nextUpto].Doc);
                        }

                        if (nextUpto == hits.Count)
                        {
                            Assert.AreEqual(DocIdSetIterator.NO_MORE_DOCS, nextDoc);
                        }
                        else
                        {
                            ScoreDoc hit = hits[nextUpto];
                            Assert.AreEqual(hit.Doc, nextDoc);
                            // Test for precise float equality:
                            Assert.IsTrue(hit.Score == scorer.Score(), "doc " + hit.Doc + " has wrong score: expected=" + hit.Score + " actual=" + scorer.Score());
                        }
                        upto = nextUpto;
                    }
                }
            }

            r.Dispose();
            d.Dispose();
        }

        // LUCENE-4477 / LUCENE-4401:
        [Test]
        public virtual void TestBooleanSpanQuery()
        {
            bool failed = false;
            int hits = 0;
            Directory directory = NewDirectory();
            Analyzer indexerAnalyzer = new MockAnalyzer(Random());

            IndexWriterConfig config = new IndexWriterConfig(TEST_VERSION_CURRENT, indexerAnalyzer);
            IndexWriter writer = new IndexWriter(directory, config);
            string FIELD = "content";
            Document d = new Document();
            d.Add(new TextField(FIELD, "clockwork orange", Field.Store.YES));
            writer.AddDocument(d);
            writer.Dispose();

            IndexReader indexReader = DirectoryReader.Open(directory);
            IndexSearcher searcher = NewSearcher(indexReader);

            BooleanQuery query = new BooleanQuery();
            SpanQuery sq1 = new SpanTermQuery(new Term(FIELD, "clockwork"));
            SpanQuery sq2 = new SpanTermQuery(new Term(FIELD, "clckwork"));
            query.Add(sq1, BooleanClause.Occur.SHOULD);
            query.Add(sq2, BooleanClause.Occur.SHOULD);
            TopScoreDocCollector collector = TopScoreDocCollector.Create(1000, true);
            searcher.Search(query, collector);
            hits = collector.TopDocs().ScoreDocs.Length;
            foreach (ScoreDoc scoreDoc in collector.TopDocs().ScoreDocs)
            {
                Console.WriteLine(scoreDoc.Doc);
            }
            indexReader.Dispose();
            Assert.AreEqual(failed, false, "Bug in boolean query composed of span queries");
            Assert.AreEqual(hits, 1, "Bug in boolean query composed of span queries");
            directory.Dispose();
        }

        // LUCENE-5487
        [Test]
        public virtual void TestInOrderWithMinShouldMatch()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);
            Document doc = new Document();
            doc.Add(NewTextField("field", "some text here", Field.Store.NO));
            w.AddDocument(doc);
            IndexReader r = w.Reader;
            w.Dispose();
            IndexSearcher s = new IndexSearcherAnonymousInnerClassHelper(this, r);
            BooleanQuery bq = new BooleanQuery();
            bq.Add(new TermQuery(new Term("field", "some")), BooleanClause.Occur.SHOULD);
            bq.Add(new TermQuery(new Term("field", "text")), BooleanClause.Occur.SHOULD);
            bq.Add(new TermQuery(new Term("field", "here")), BooleanClause.Occur.SHOULD);
            bq.MinimumNumberShouldMatch = 2;
            s.Search(bq, 10);
            r.Dispose();
            dir.Dispose();
        }

        private class IndexSearcherAnonymousInnerClassHelper : IndexSearcher
        {
            private readonly TestBooleanQuery OuterInstance;

            public IndexSearcherAnonymousInnerClassHelper(TestBooleanQuery outerInstance, IndexReader r)
                : base(r)
            {
                this.OuterInstance = outerInstance;
            }

            protected internal override void Search(IList<AtomicReaderContext> leaves, Weight weight, Collector collector)
            {
                Assert.AreEqual(-1, collector.GetType().Name.IndexOf("OutOfOrder"));
                base.Search(leaves, weight, collector);
            }
        }
    }
}
 