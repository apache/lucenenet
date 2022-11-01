using Lucene.Net.Documents;
using Lucene.Net.Support;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using System.Collections.Generic;
using System.Text;
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
    using DefaultSimilarity = Lucene.Net.Search.Similarities.DefaultSimilarity;
    using Directory = Lucene.Net.Store.Directory;
    using Document = Documents.Document;
    using Field = Field;
    using IBits = Lucene.Net.Util.IBits;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using NumericDocValuesField = NumericDocValuesField;
    using RandomIndexWriter = Lucene.Net.Index.RandomIndexWriter;
    using SpanNearQuery = Lucene.Net.Search.Spans.SpanNearQuery;
    using SpanQuery = Lucene.Net.Search.Spans.SpanQuery;
    using SpanTermQuery = Lucene.Net.Search.Spans.SpanTermQuery;
    using Term = Lucene.Net.Index.Term;
    using TestUtil = Lucene.Net.Util.TestUtil;

    [SuppressCodecs("Lucene3x")]
    [TestFixture]
    public class TestQueryRescorer : LuceneTestCase
    {
        private IndexSearcher GetSearcher(IndexReader r)
        {
            IndexSearcher searcher = NewSearcher(r);

            // We rely on more tokens = lower score:
            searcher.Similarity = new DefaultSimilarity();

            return searcher;
        }

        [Test]
        public virtual void TestBasic()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random, dir);

            Document doc = new Document();
            doc.Add(NewStringField("id", "0", Field.Store.YES));
            doc.Add(NewTextField("field", "wizard the the the the the oz", Field.Store.NO));
            w.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("id", "1", Field.Store.YES));
            // 1 extra token, but wizard and oz are close;
            doc.Add(NewTextField("field", "wizard oz the the the the the the", Field.Store.NO));
            w.AddDocument(doc);
            IndexReader r = w.GetReader();
            w.Dispose();

            // Do ordinary BooleanQuery:
            BooleanQuery bq = new BooleanQuery();
            bq.Add(new TermQuery(new Term("field", "wizard")), Occur.SHOULD);
            bq.Add(new TermQuery(new Term("field", "oz")), Occur.SHOULD);
            IndexSearcher searcher = GetSearcher(r);
            searcher.Similarity = new DefaultSimilarity();

            TopDocs hits = searcher.Search(bq, 10);
            Assert.AreEqual(2, hits.TotalHits);
            Assert.AreEqual("0", searcher.Doc(hits.ScoreDocs[0].Doc).Get("id"));
            Assert.AreEqual("1", searcher.Doc(hits.ScoreDocs[1].Doc).Get("id"));

            // Now, resort using PhraseQuery:
            PhraseQuery pq = new PhraseQuery();
            pq.Slop = 5;
            pq.Add(new Term("field", "wizard"));
            pq.Add(new Term("field", "oz"));

            TopDocs hits2 = QueryRescorer.Rescore(searcher, hits, pq, 2.0, 10);

            // Resorting changed the order:
            Assert.AreEqual(2, hits2.TotalHits);
            Assert.AreEqual("1", searcher.Doc(hits2.ScoreDocs[0].Doc).Get("id"));
            Assert.AreEqual("0", searcher.Doc(hits2.ScoreDocs[1].Doc).Get("id"));

            // Resort using SpanNearQuery:
            SpanTermQuery t1 = new SpanTermQuery(new Term("field", "wizard"));
            SpanTermQuery t2 = new SpanTermQuery(new Term("field", "oz"));
            SpanNearQuery snq = new SpanNearQuery(new SpanQuery[] { t1, t2 }, 0, true);

            TopDocs hits3 = QueryRescorer.Rescore(searcher, hits, snq, 2.0, 10);

            // Resorting changed the order:
            Assert.AreEqual(2, hits3.TotalHits);
            Assert.AreEqual("1", searcher.Doc(hits3.ScoreDocs[0].Doc).Get("id"));
            Assert.AreEqual("0", searcher.Doc(hits3.ScoreDocs[1].Doc).Get("id"));

            r.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestCustomCombine()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random, dir);

            Document doc = new Document();
            doc.Add(NewStringField("id", "0", Field.Store.YES));
            doc.Add(NewTextField("field", "wizard the the the the the oz", Field.Store.NO));
            w.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("id", "1", Field.Store.YES));
            // 1 extra token, but wizard and oz are close;
            doc.Add(NewTextField("field", "wizard oz the the the the the the", Field.Store.NO));
            w.AddDocument(doc);
            IndexReader r = w.GetReader();
            w.Dispose();

            // Do ordinary BooleanQuery:
            BooleanQuery bq = new BooleanQuery();
            bq.Add(new TermQuery(new Term("field", "wizard")), Occur.SHOULD);
            bq.Add(new TermQuery(new Term("field", "oz")), Occur.SHOULD);
            IndexSearcher searcher = GetSearcher(r);

            TopDocs hits = searcher.Search(bq, 10);
            Assert.AreEqual(2, hits.TotalHits);
            Assert.AreEqual("0", searcher.Doc(hits.ScoreDocs[0].Doc).Get("id"));
            Assert.AreEqual("1", searcher.Doc(hits.ScoreDocs[1].Doc).Get("id"));

            // Now, resort using PhraseQuery, but with an
            // opposite-world combine:
            PhraseQuery pq = new PhraseQuery();
            pq.Slop = 5;
            pq.Add(new Term("field", "wizard"));
            pq.Add(new Term("field", "oz"));

            TopDocs hits2 = new QueryRescorerAnonymousClass(this, pq)
              .Rescore(searcher, hits, 10);

            // Resorting didn't change the order:
            Assert.AreEqual(2, hits2.TotalHits);
            Assert.AreEqual("0", searcher.Doc(hits2.ScoreDocs[0].Doc).Get("id"));
            Assert.AreEqual("1", searcher.Doc(hits2.ScoreDocs[1].Doc).Get("id"));

            r.Dispose();
            dir.Dispose();
        }

        private sealed class QueryRescorerAnonymousClass : QueryRescorer
        {
            private readonly TestQueryRescorer outerInstance;

            public QueryRescorerAnonymousClass(TestQueryRescorer outerInstance, PhraseQuery pq)
                : base(pq)
            {
                this.outerInstance = outerInstance;
            }

            protected override float Combine(float firstPassScore, bool secondPassMatches, float secondPassScore)
            {
                float score = firstPassScore;
                if (secondPassMatches)
                {
                    score -= (float)(2.0 * secondPassScore);
                }
                return score;
            }
        }

        [Test]
        public virtual void TestExplain()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random, dir);

            Document doc = new Document();
            doc.Add(NewStringField("id", "0", Field.Store.YES));
            doc.Add(NewTextField("field", "wizard the the the the the oz", Field.Store.NO));
            w.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("id", "1", Field.Store.YES));
            // 1 extra token, but wizard and oz are close;
            doc.Add(NewTextField("field", "wizard oz the the the the the the", Field.Store.NO));
            w.AddDocument(doc);
            IndexReader r = w.GetReader();
            w.Dispose();

            // Do ordinary BooleanQuery:
            BooleanQuery bq = new BooleanQuery();
            bq.Add(new TermQuery(new Term("field", "wizard")), Occur.SHOULD);
            bq.Add(new TermQuery(new Term("field", "oz")), Occur.SHOULD);
            IndexSearcher searcher = GetSearcher(r);

            TopDocs hits = searcher.Search(bq, 10);
            Assert.AreEqual(2, hits.TotalHits);
            Assert.AreEqual("0", searcher.Doc(hits.ScoreDocs[0].Doc).Get("id"));
            Assert.AreEqual("1", searcher.Doc(hits.ScoreDocs[1].Doc).Get("id"));

            // Now, resort using PhraseQuery:
            PhraseQuery pq = new PhraseQuery();
            pq.Add(new Term("field", "wizard"));
            pq.Add(new Term("field", "oz"));

            Rescorer rescorer = new QueryRescorerAnonymousClass2(this, pq);

            TopDocs hits2 = rescorer.Rescore(searcher, hits, 10);

            // Resorting changed the order:
            Assert.AreEqual(2, hits2.TotalHits);
            Assert.AreEqual("1", searcher.Doc(hits2.ScoreDocs[0].Doc).Get("id"));
            Assert.AreEqual("0", searcher.Doc(hits2.ScoreDocs[1].Doc).Get("id"));

            int docID = hits2.ScoreDocs[0].Doc;
            Explanation explain = rescorer.Explain(searcher, searcher.Explain(bq, docID), docID);
            string s = explain.ToString();
            Assert.IsTrue(s.Contains("TestQueryRescorer+"));
            Assert.IsTrue(s.Contains("combined first and second pass score"));
            Assert.IsTrue(s.Contains("first pass score"));
            Assert.IsTrue(s.Contains("= second pass score"));
            Assert.AreEqual(hits2.ScoreDocs[0].Score, explain.Value, 0.0f);

            docID = hits2.ScoreDocs[1].Doc;
            explain = rescorer.Explain(searcher, searcher.Explain(bq, docID), docID);
            s = explain.ToString();
            Assert.IsTrue(s.Contains("TestQueryRescorer+"));
            Assert.IsTrue(s.Contains("combined first and second pass score"));
            Assert.IsTrue(s.Contains("first pass score"));
            Assert.IsTrue(s.Contains("no second pass score"));
            Assert.IsFalse(s.Contains("= second pass score"));
            Assert.IsTrue(s.Contains("NON-MATCH"));
            Assert.IsTrue(Math.Abs(hits2.ScoreDocs[1].Score - explain.Value) < 0.0000001f);

            r.Dispose();
            dir.Dispose();
        }

        private sealed class QueryRescorerAnonymousClass2 : QueryRescorer
        {
            private readonly TestQueryRescorer outerInstance;

            public QueryRescorerAnonymousClass2(TestQueryRescorer outerInstance, PhraseQuery pq)
                : base(pq)
            {
                this.outerInstance = outerInstance;
            }

            protected override float Combine(float firstPassScore, bool secondPassMatches, float secondPassScore)
            {
                float score = firstPassScore;
                if (secondPassMatches)
                {
                    score += (float)(2.0 * secondPassScore);
                }
                return score;
            }
        }

        [Test]
        public virtual void TestMissingSecondPassScore()
        {
            Directory dir = NewDirectory();
            RandomIndexWriter w = new RandomIndexWriter(Random, dir);

            Document doc = new Document();
            doc.Add(NewStringField("id", "0", Field.Store.YES));
            doc.Add(NewTextField("field", "wizard the the the the the oz", Field.Store.NO));
            w.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("id", "1", Field.Store.YES));
            // 1 extra token, but wizard and oz are close;
            doc.Add(NewTextField("field", "wizard oz the the the the the the", Field.Store.NO));
            w.AddDocument(doc);
            IndexReader r = w.GetReader();
            w.Dispose();

            // Do ordinary BooleanQuery:
            BooleanQuery bq = new BooleanQuery();
            bq.Add(new TermQuery(new Term("field", "wizard")), Occur.SHOULD);
            bq.Add(new TermQuery(new Term("field", "oz")), Occur.SHOULD);
            IndexSearcher searcher = GetSearcher(r);

            TopDocs hits = searcher.Search(bq, 10);
            Assert.AreEqual(2, hits.TotalHits);
            Assert.AreEqual("0", searcher.Doc(hits.ScoreDocs[0].Doc).Get("id"));
            Assert.AreEqual("1", searcher.Doc(hits.ScoreDocs[1].Doc).Get("id"));

            // Now, resort using PhraseQuery, no slop:
            PhraseQuery pq = new PhraseQuery();
            pq.Add(new Term("field", "wizard"));
            pq.Add(new Term("field", "oz"));

            TopDocs hits2 = QueryRescorer.Rescore(searcher, hits, pq, 2.0, 10);

            // Resorting changed the order:
            Assert.AreEqual(2, hits2.TotalHits);
            Assert.AreEqual("1", searcher.Doc(hits2.ScoreDocs[0].Doc).Get("id"));
            Assert.AreEqual("0", searcher.Doc(hits2.ScoreDocs[1].Doc).Get("id"));

            // Resort using SpanNearQuery:
            SpanTermQuery t1 = new SpanTermQuery(new Term("field", "wizard"));
            SpanTermQuery t2 = new SpanTermQuery(new Term("field", "oz"));
            SpanNearQuery snq = new SpanNearQuery(new SpanQuery[] { t1, t2 }, 0, true);

            TopDocs hits3 = QueryRescorer.Rescore(searcher, hits, snq, 2.0, 10);

            // Resorting changed the order:
            Assert.AreEqual(2, hits3.TotalHits);
            Assert.AreEqual("1", searcher.Doc(hits3.ScoreDocs[0].Doc).Get("id"));
            Assert.AreEqual("0", searcher.Doc(hits3.ScoreDocs[1].Doc).Get("id"));

            r.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestRandom()
        {
            Directory dir = NewDirectory();
            int numDocs = AtLeast(1000);
            RandomIndexWriter w = new RandomIndexWriter(Random, dir);

            int[] idToNum = new int[numDocs];
            int maxValue = TestUtil.NextInt32(Random, 10, 1000000);
            for (int i = 0; i < numDocs; i++)
            {
                Document doc = new Document();
                doc.Add(NewStringField("id", "" + i, Field.Store.YES));
                int numTokens = TestUtil.NextInt32(Random, 1, 10);
                StringBuilder b = new StringBuilder();
                for (int j = 0; j < numTokens; j++)
                {
                    b.Append("a ");
                }
                doc.Add(NewTextField("field", b.ToString(), Field.Store.NO));
                idToNum[i] = Random.Next(maxValue);
                doc.Add(new NumericDocValuesField("num", idToNum[i]));
                w.AddDocument(doc);
            }
            IndexReader r = w.GetReader();
            w.Dispose();

            IndexSearcher s = NewSearcher(r);
            int numHits = TestUtil.NextInt32(Random, 1, numDocs);
            bool reverse = Random.NextBoolean();

            //System.out.println("numHits=" + numHits + " reverse=" + reverse);
            TopDocs hits = s.Search(new TermQuery(new Term("field", "a")), numHits);

            TopDocs hits2 = new QueryRescorerAnonymousClass3(this, new FixedScoreQuery(idToNum, reverse))
              .Rescore(s, hits, numHits);

            int[] expected = new int[numHits];
            for (int i = 0; i < numHits; i++)
            {
                expected[i] = hits.ScoreDocs[i].Doc;
            }

            int reverseInt = reverse ? -1 : 1;

            Array.Sort(expected,
                Comparer<int>.Create((a, b) =>
                {
                    try
                    {
                        int av = idToNum[Convert.ToInt32(r.Document(a).Get("id"))];
                        int bv = idToNum[Convert.ToInt32(r.Document(b).Get("id"))];
                        if (av < bv)
                        {
                            return -reverseInt;
                        }
                        else if (bv < av)
                        {
                            return reverseInt;
                        }
                        else
                        {
                            // Tie break by docID, ascending
                            return a - b;
                        }
                    }
                    catch (Exception ioe) when (ioe.IsIOException())
                    {
                        throw RuntimeException.Create(ioe);
                    }
                })
            );

            bool fail = false;
            for (int i = 0; i < numHits; i++)
            {
                //System.out.println("expected=" + expected[i] + " vs " + hits2.ScoreDocs[i].Doc + " v=" + idToNum[Integer.parseInt(r.Document(expected[i]).Get("id"))]);
                if ((int)expected[i] != hits2.ScoreDocs[i].Doc)
                {
                    //System.out.println("  diff!");
                    fail = true;
                }
            }
            Assert.IsFalse(fail);

            r.Dispose();
            dir.Dispose();
        }

        private sealed class QueryRescorerAnonymousClass3 : QueryRescorer
        {
            private readonly TestQueryRescorer outerInstance;

            public QueryRescorerAnonymousClass3(TestQueryRescorer outerInstance, FixedScoreQuery fixedScoreQuery)
                : base(fixedScoreQuery)
            {
                this.outerInstance = outerInstance;
            }

            protected override float Combine(float firstPassScore, bool secondPassMatches, float secondPassScore)
            {
                return secondPassScore;
            }
        }
        
        /// <summary>
        /// Just assigns score == idToNum[doc("id")] for each doc. </summary>
        private class FixedScoreQuery : Query
        {
            private readonly int[] idToNum;
            private readonly bool reverse;

            public FixedScoreQuery(int[] idToNum, bool reverse)
            {
                this.idToNum = idToNum;
                this.reverse = reverse;
            }

            public override Weight CreateWeight(IndexSearcher searcher)
            {
                return new WeightAnonymousClass(this);
            }

            private sealed class WeightAnonymousClass : Weight
            {
                private readonly FixedScoreQuery outerInstance;

                public WeightAnonymousClass(FixedScoreQuery outerInstance)
                {
                    this.outerInstance = outerInstance;
                }

                public override Query Query => outerInstance;

                public override float GetValueForNormalization()
                {
                    return 1.0f;
                }

                public override void Normalize(float queryNorm, float topLevelBoost)
                {
                }

                public override Scorer GetScorer(AtomicReaderContext context, IBits acceptDocs)
                {
                    return new ScorerAnonymousClass(this, context);
                }

                private sealed class ScorerAnonymousClass : Scorer
                {
                    private readonly WeightAnonymousClass outerInstance;

                    private readonly AtomicReaderContext context;

                    public ScorerAnonymousClass(WeightAnonymousClass outerInstance, AtomicReaderContext context)
                        : base(null)
                    {
                        this.outerInstance = outerInstance;
                        this.context = context;
                        docID = -1;
                    }

                    internal int docID;

                    public override int DocID => docID;

                    public override int Freq => 1;

                    public override long GetCost()
                    {
                        return 1;
                    }

                    public override int NextDoc()
                    {
                        docID++;
                        if (docID >= context.Reader.MaxDoc)
                        {
                            return NO_MORE_DOCS;
                        }
                        return docID;
                    }

                    public override int Advance(int target)
                    {
                        docID = target;
                        return docID;
                    }

                    public override float GetScore()
                    {
                        int num = outerInstance.outerInstance.idToNum[Convert.ToInt32(context.Reader.Document(docID).Get("id"))];
                        if (outerInstance.outerInstance.reverse)
                        {
                            //System.out.println("score doc=" + docID + " num=" + num);
                            return num;
                        }
                        else
                        {
                            //System.out.println("score doc=" + docID + " num=" + -num);
                            return -num;
                        }
                    }
                }

                public override Explanation Explain(AtomicReaderContext context, int doc)
                {
                    return null;
                }
            }

            public override void ExtractTerms(ISet<Term> terms)
            {
            }

            public override string ToString(string field)
            {
                return "FixedScoreQuery " + idToNum.Length + " ids; reverse=" + reverse;
            }

            public override bool Equals(object o)
            {
                if ((o is FixedScoreQuery) == false)
                {
                    return false;
                }
                FixedScoreQuery other = (FixedScoreQuery)o;
                return J2N.BitConversion.SingleToInt32Bits(Boost) == J2N.BitConversion.SingleToInt32Bits(other.Boost) && reverse == other.reverse && Arrays.Equals(idToNum, other.idToNum);
            }

            public override object Clone()
            {
                return new FixedScoreQuery(idToNum, reverse);
            }

            public override int GetHashCode()
            {
                int PRIME = 31;
                int hash = base.GetHashCode();
                if (reverse)
                {
                    hash = PRIME * hash + 3623;
                }
                hash = PRIME * hash + Arrays.GetHashCode(idToNum);
                return hash;
            }
        }
    }
}