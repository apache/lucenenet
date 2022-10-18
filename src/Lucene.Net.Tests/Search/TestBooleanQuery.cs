using Lucene.Net.Documents;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JCG = J2N.Collections.Generic;
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
    using MultiReader = Lucene.Net.Index.MultiReader;
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
            bq1.Add(new TermQuery(new Term("field", "value1")), Occur.SHOULD);
            bq1.Add(new TermQuery(new Term("field", "value2")), Occur.SHOULD);
            BooleanQuery nested1 = new BooleanQuery();
            nested1.Add(new TermQuery(new Term("field", "nestedvalue1")), Occur.SHOULD);
            nested1.Add(new TermQuery(new Term("field", "nestedvalue2")), Occur.SHOULD);
            bq1.Add(nested1, Occur.SHOULD);

            BooleanQuery bq2 = new BooleanQuery();
            bq2.Add(new TermQuery(new Term("field", "value1")), Occur.SHOULD);
            bq2.Add(new TermQuery(new Term("field", "value2")), Occur.SHOULD);
            BooleanQuery nested2 = new BooleanQuery();
            nested2.Add(new TermQuery(new Term("field", "nestedvalue1")), Occur.SHOULD);
            nested2.Add(new TermQuery(new Term("field", "nestedvalue2")), Occur.SHOULD);
            bq2.Add(nested2, Occur.SHOULD);

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
            catch (ArgumentOutOfRangeException) // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            {
                // okay
            }
        }

        // LUCENE-1630
        [Test]
        public virtual void TestNullOrSubScorer()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random, dir);
            Document doc = new Document();
            doc.Add(NewTextField("field", "a b c d", Field.Store.NO));
            w.AddDocument(doc);

            IndexReader r = w.GetReader();
            IndexSearcher s = NewSearcher(r);
            // this test relies upon coord being the default implementation,
            // otherwise scores are different!
            s.Similarity = new DefaultSimilarity();

            BooleanQuery q = new BooleanQuery();
            q.Add(new TermQuery(new Term("field", "a")), Occur.SHOULD);

            // LUCENE-2617: make sure that a term not in the index still contributes to the score via coord factor
            float score = s.Search(q, 10).MaxScore;
            Query subQuery = new TermQuery(new Term("field", "not_in_index"));
            subQuery.Boost = 0;
            q.Add(subQuery, Occur.SHOULD);
            float score2 = s.Search(q, 10).MaxScore;
            Assert.AreEqual(score * .5F, score2, 1e-6);

            // LUCENE-2617: make sure that a clause not in the index still contributes to the score via coord factor
            BooleanQuery qq = (BooleanQuery)q.Clone();
            PhraseQuery phrase = new PhraseQuery();
            phrase.Add(new Term("field", "not_in_index"));
            phrase.Add(new Term("field", "another_not_in_index"));
            phrase.Boost = 0;
            qq.Add(phrase, Occur.SHOULD);
            score2 = s.Search(qq, 10).MaxScore;
            Assert.AreEqual(score * (1 / 3F), score2, 1e-6);

            // now test BooleanScorer2
            subQuery = new TermQuery(new Term("field", "b"));
            subQuery.Boost = 0;
            q.Add(subQuery, Occur.MUST);
            score2 = s.Search(q, 10).MaxScore;
            Assert.AreEqual(score * (2 / 3F), score2, 1e-6);

            // PhraseQuery w/ no terms added returns a null scorer
            PhraseQuery pq = new PhraseQuery();
            q.Add(pq, Occur.SHOULD);
            Assert.AreEqual(1, s.Search(q, 10).TotalHits);

            // A required clause which returns null scorer should return null scorer to
            // IndexSearcher.
            q = new BooleanQuery();
            pq = new PhraseQuery();
            q.Add(new TermQuery(new Term("field", "a")), Occur.SHOULD);
            q.Add(pq, Occur.MUST);
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
            RandomIndexWriter iw1 = new RandomIndexWriter(Random, dir1);
            Document doc1 = new Document();
            doc1.Add(NewTextField("field", "foo bar", Field.Store.NO));
            iw1.AddDocument(doc1);
            IndexReader reader1 = iw1.GetReader();
            iw1.Dispose();

            Directory dir2 = NewDirectory();
            RandomIndexWriter iw2 = new RandomIndexWriter(Random, dir2);
            Document doc2 = new Document();
            doc2.Add(NewTextField("field", "foo baz", Field.Store.NO));
            iw2.AddDocument(doc2);
            IndexReader reader2 = iw2.GetReader();
            iw2.Dispose();

            BooleanQuery query = new BooleanQuery(); // Query: +foo -ba*
            query.Add(new TermQuery(new Term("field", "foo")), Occur.MUST);
            WildcardQuery wildcardQuery = new WildcardQuery(new Term("field", "ba*"));
            wildcardQuery.MultiTermRewriteMethod = (MultiTermQuery.SCORING_BOOLEAN_QUERY_REWRITE);
            query.Add(wildcardQuery, Occur.MUST_NOT);

            MultiReader multireader = new MultiReader(reader1, reader2);
            IndexSearcher searcher = NewSearcher(multireader);
            Assert.AreEqual(0, searcher.Search(query, 10).TotalHits);


            Task foo = new Task(TestDeMorgan);

            TaskScheduler es = TaskScheduler.Default;
            searcher = new IndexSearcher(multireader, es);
            if (Verbose)
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
            RandomIndexWriter w = new RandomIndexWriter(Random, d);
            int numDocs = AtLeast(300);
            for (int docUpto = 0; docUpto < numDocs; docUpto++)
            {
                string contents = "a";
                if (Random.Next(20) <= 16)
                {
                    contents += " b";
                }
                if (Random.Next(20) <= 8)
                {
                    contents += " c";
                }
                if (Random.Next(20) <= 4)
                {
                    contents += " d";
                }
                if (Random.Next(20) <= 2)
                {
                    contents += " e";
                }
                if (Random.Next(20) <= 1)
                {
                    contents += " f";
                }
                Document doc = new Document();
                doc.Add(new TextField("field", contents, Field.Store.NO));
                w.AddDocument(doc);
            }
            w.ForceMerge(1);
            IndexReader r = w.GetReader();
            IndexSearcher s = NewSearcher(r);
            w.Dispose();

            for (int iter = 0; iter < 10 * RandomMultiplier; iter++)
            {
                if (Verbose)
                {
                    Console.WriteLine("iter=" + iter);
                }
                IList<string> terms = new JCG.List<string> { "a", "b", "c", "d", "e", "f" };
                int numTerms = TestUtil.NextInt32(Random, 1, terms.Count);
                while (terms.Count > numTerms)
                {
                    terms.RemoveAt(Random.Next(terms.Count));
                }

                if (Verbose)
                {
                    Console.WriteLine("  terms=" + terms);
                }

                BooleanQuery q = new BooleanQuery();
                foreach (string term in terms)
                {
                    q.Add(new BooleanClause(new TermQuery(new Term("field", term)), Occur.SHOULD));
                }

                Weight weight = s.CreateNormalizedWeight(q);

                Scorer scorer = weight.GetScorer(s.m_leafContexts[0], null);

                // First pass: just use .NextDoc() to gather all hits
                IList<ScoreDoc> hits = new JCG.List<ScoreDoc>();
                while (scorer.NextDoc() != DocIdSetIterator.NO_MORE_DOCS)
                {
                    hits.Add(new ScoreDoc(scorer.DocID, scorer.GetScore()));
                }

                if (Verbose)
                {
                    Console.WriteLine("  " + hits.Count + " hits");
                }

                // Now, randomly next/advance through the list and
                // verify exact match:
                for (int iter2 = 0; iter2 < 10; iter2++)
                {
                    weight = s.CreateNormalizedWeight(q);
                    scorer = weight.GetScorer(s.m_leafContexts[0], null);

                    if (Verbose)
                    {
                        Console.WriteLine("  iter2=" + iter2);
                    }

                    int upto = -1;
                    while (upto < hits.Count)
                    {
                        int nextUpto;
                        int nextDoc;
                        int left = hits.Count - upto;
                        if (left == 1 || Random.nextBoolean())
                        {
                            // next
                            nextUpto = 1 + upto;
                            nextDoc = scorer.NextDoc();
                        }
                        else
                        {
                            // advance
                            int inc = TestUtil.NextInt32(Random, 1, left - 1);
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

                            // LUCENENET: For some weird reason, on x86 in .NET Framework (optimizations enabled), using == (as they did in Lucene) doesn't work with optimizations enabled, but using AreEqual with epsilon of 0f does.
                            // Test for precise float equality:
                            Assert.AreEqual(hit.Score, scorer.GetScore(), 0f, "doc " + hit.Doc + " has wrong score: expected=" + hit.Score + " actual=" + scorer.GetScore());
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
            Analyzer indexerAnalyzer = new MockAnalyzer(Random);

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
            query.Add(sq1, Occur.SHOULD);
            query.Add(sq2, Occur.SHOULD);
            TopScoreDocCollector collector = TopScoreDocCollector.Create(1000, true);
            searcher.Search(query, collector);
            hits = collector.GetTopDocs().ScoreDocs.Length;
            foreach (ScoreDoc scoreDoc in collector.GetTopDocs().ScoreDocs)
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
            RandomIndexWriter w = new RandomIndexWriter(Random, dir);
            Document doc = new Document();
            doc.Add(NewTextField("field", "some text here", Field.Store.NO));
            w.AddDocument(doc);
            IndexReader r = w.GetReader();
            w.Dispose();
            IndexSearcher s = new IndexSearcherAnonymousClass(this, r);
            BooleanQuery bq = new BooleanQuery();
            bq.Add(new TermQuery(new Term("field", "some")), Occur.SHOULD);
            bq.Add(new TermQuery(new Term("field", "text")), Occur.SHOULD);
            bq.Add(new TermQuery(new Term("field", "here")), Occur.SHOULD);
            bq.MinimumNumberShouldMatch = 2;
            s.Search(bq, 10);
            r.Dispose();
            dir.Dispose();
        }

        private sealed class IndexSearcherAnonymousClass : IndexSearcher
        {
            private readonly TestBooleanQuery outerInstance;

            public IndexSearcherAnonymousClass(TestBooleanQuery outerInstance, IndexReader r)
                : base(r)
            {
                this.outerInstance = outerInstance;
            }

            protected override void Search(IList<AtomicReaderContext> leaves, Weight weight, ICollector collector)
            {
                Assert.AreEqual(-1, collector.GetType().Name.IndexOf("OutOfOrder", StringComparison.Ordinal));
                base.Search(leaves, weight, collector);
            }
        }
    }
}
 