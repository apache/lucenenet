using System;
using System.Text;
using Lucene.Net.Documents;

namespace Lucene.Net.Search
{
    using Lucene.Net.Randomized.Generators;
    using Lucene.Net.Support;
    using NUnit.Framework;
    using System.Collections.Generic;
    using System.IO;
    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using Bits = Lucene.Net.Util.Bits;
    using DefaultSimilarity = Lucene.Net.Search.Similarities.DefaultSimilarity;
    using Directory = Lucene.Net.Store.Directory;

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

    using Document = Documents.Document;
    using Field = Field;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using NumericDocValuesField = NumericDocValuesField;
    using Occur = Lucene.Net.Search.BooleanClause.Occur;
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
            RandomIndexWriter w = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);

            Document doc = new Document();
            doc.Add(NewStringField("id", "0", Field.Store.YES));
            doc.Add(NewTextField("field", "wizard the the the the the oz", Field.Store.NO));
            w.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("id", "1", Field.Store.YES));
            // 1 extra token, but wizard and oz are close;
            doc.Add(NewTextField("field", "wizard oz the the the the the the", Field.Store.NO));
            w.AddDocument(doc);
            IndexReader r = w.Reader;
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
            RandomIndexWriter w = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);

            Document doc = new Document();
            doc.Add(NewStringField("id", "0", Field.Store.YES));
            doc.Add(NewTextField("field", "wizard the the the the the oz", Field.Store.NO));
            w.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("id", "1", Field.Store.YES));
            // 1 extra token, but wizard and oz are close;
            doc.Add(NewTextField("field", "wizard oz the the the the the the", Field.Store.NO));
            w.AddDocument(doc);
            IndexReader r = w.Reader;
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

            TopDocs hits2 = new QueryRescorerAnonymousInnerClassHelper(this, pq)
              .Rescore(searcher, hits, 10);

            // Resorting didn't change the order:
            Assert.AreEqual(2, hits2.TotalHits);
            Assert.AreEqual("0", searcher.Doc(hits2.ScoreDocs[0].Doc).Get("id"));
            Assert.AreEqual("1", searcher.Doc(hits2.ScoreDocs[1].Doc).Get("id"));

            r.Dispose();
            dir.Dispose();
        }

        private class QueryRescorerAnonymousInnerClassHelper : QueryRescorer
        {
            private readonly TestQueryRescorer OuterInstance;

            public QueryRescorerAnonymousInnerClassHelper(TestQueryRescorer outerInstance, PhraseQuery pq)
                : base(pq)
            {
                this.OuterInstance = outerInstance;
            }

            protected internal override float Combine(float firstPassScore, bool secondPassMatches, float secondPassScore)
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
            RandomIndexWriter w = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);

            Document doc = new Document();
            doc.Add(NewStringField("id", "0", Field.Store.YES));
            doc.Add(NewTextField("field", "wizard the the the the the oz", Field.Store.NO));
            w.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("id", "1", Field.Store.YES));
            // 1 extra token, but wizard and oz are close;
            doc.Add(NewTextField("field", "wizard oz the the the the the the", Field.Store.NO));
            w.AddDocument(doc);
            IndexReader r = w.Reader;
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

            Rescorer rescorer = new QueryRescorerAnonymousInnerClassHelper2(this, pq);

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

        private class QueryRescorerAnonymousInnerClassHelper2 : QueryRescorer
        {
            private readonly TestQueryRescorer OuterInstance;

            public QueryRescorerAnonymousInnerClassHelper2(TestQueryRescorer outerInstance, PhraseQuery pq)
                : base(pq)
            {
                this.OuterInstance = outerInstance;
            }

            protected internal override float Combine(float firstPassScore, bool secondPassMatches, float secondPassScore)
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
            RandomIndexWriter w = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);

            Document doc = new Document();
            doc.Add(NewStringField("id", "0", Field.Store.YES));
            doc.Add(NewTextField("field", "wizard the the the the the oz", Field.Store.NO));
            w.AddDocument(doc);
            doc = new Document();
            doc.Add(NewStringField("id", "1", Field.Store.YES));
            // 1 extra token, but wizard and oz are close;
            doc.Add(NewTextField("field", "wizard oz the the the the the the", Field.Store.NO));
            w.AddDocument(doc);
            IndexReader r = w.Reader;
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
            RandomIndexWriter w = new RandomIndexWriter(Random(), dir, Similarity, TimeZone);

            int[] idToNum = new int[numDocs];
            int maxValue = TestUtil.NextInt(Random(), 10, 1000000);
            for (int i = 0; i < numDocs; i++)
            {
                Document doc = new Document();
                doc.Add(NewStringField("id", "" + i, Field.Store.YES));
                int numTokens = TestUtil.NextInt(Random(), 1, 10);
                StringBuilder b = new StringBuilder();
                for (int j = 0; j < numTokens; j++)
                {
                    b.Append("a ");
                }
                doc.Add(NewTextField("field", b.ToString(), Field.Store.NO));
                idToNum[i] = Random().Next(maxValue);
                doc.Add(new NumericDocValuesField("num", idToNum[i]));
                w.AddDocument(doc);
            }
            IndexReader r = w.Reader;
            w.Dispose();

            IndexSearcher s = NewSearcher(r);
            int numHits = TestUtil.NextInt(Random(), 1, numDocs);
            bool reverse = Random().NextBoolean();

            //System.out.println("numHits=" + numHits + " reverse=" + reverse);
            TopDocs hits = s.Search(new TermQuery(new Term("field", "a")), numHits);

            TopDocs hits2 = new QueryRescorerAnonymousInnerClassHelper3(this, new FixedScoreQuery(idToNum, reverse))
              .Rescore(s, hits, numHits);

            int[] expected = new int[numHits];
            for (int i = 0; i < numHits; i++)
            {
                expected[i] = hits.ScoreDocs[i].Doc;
            }

            int reverseInt = reverse ? -1 : 1;

            Array.Sort(expected, new ComparatorAnonymousInnerClassHelper(this, idToNum, r, reverseInt));

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

        private class QueryRescorerAnonymousInnerClassHelper3 : QueryRescorer
        {
            private readonly TestQueryRescorer OuterInstance;

            public QueryRescorerAnonymousInnerClassHelper3(TestQueryRescorer outerInstance, FixedScoreQuery fixedScoreQuery)
                : base(fixedScoreQuery)
            {
                this.OuterInstance = outerInstance;
            }

            protected internal override float Combine(float firstPassScore, bool secondPassMatches, float secondPassScore)
            {
                return secondPassScore;
            }
        }

        private class ComparatorAnonymousInnerClassHelper : IComparer<int>
        {
            private readonly TestQueryRescorer OuterInstance;

            private int[] IdToNum;
            private IndexReader r;
            private int ReverseInt;

            public ComparatorAnonymousInnerClassHelper(TestQueryRescorer outerInstance, int[] idToNum, IndexReader r, int reverseInt)
            {
                this.OuterInstance = outerInstance;
                this.IdToNum = idToNum;
                this.r = r;
                this.ReverseInt = reverseInt;
            }

            public virtual int Compare(int a, int b)
            {
                try
                {
                    int av = IdToNum[Convert.ToInt32(r.Document(a).Get("id"))];
                    int bv = IdToNum[Convert.ToInt32(r.Document(b).Get("id"))];
                    if (av < bv)
                    {
                        return -ReverseInt;
                    }
                    else if (bv < av)
                    {
                        return ReverseInt;
                    }
                    else
                    {
                        // Tie break by docID, ascending
                        return a - b;
                    }
                }
                catch (IOException ioe)
                {
                    throw new Exception(ioe.Message, ioe);
                }
            }
        }

        /// <summary>
        /// Just assigns score == idToNum[doc("id")] for each doc. </summary>
        private class FixedScoreQuery : Query
        {
            internal readonly int[] IdToNum;
            internal readonly bool Reverse;

            public FixedScoreQuery(int[] idToNum, bool reverse)
            {
                this.IdToNum = idToNum;
                this.Reverse = reverse;
            }

            public override Weight CreateWeight(IndexSearcher searcher)
            {
                return new WeightAnonymousInnerClassHelper(this);
            }

            private class WeightAnonymousInnerClassHelper : Weight
            {
                private readonly FixedScoreQuery OuterInstance;

                public WeightAnonymousInnerClassHelper(FixedScoreQuery outerInstance)
                {
                    this.OuterInstance = outerInstance;
                }

                public override Query Query
                {
                    get
                    {
                        return OuterInstance;
                    }
                }

                public override float ValueForNormalization
                {
                    get
                    {
                        return 1.0f;
                    }
                }

                public override void Normalize(float queryNorm, float topLevelBoost)
                {
                }

                public override Scorer Scorer(AtomicReaderContext context, Bits acceptDocs)
                {
                    return new ScorerAnonymousInnerClassHelper(this, context);
                }

                private class ScorerAnonymousInnerClassHelper : Scorer
                {
                    private readonly WeightAnonymousInnerClassHelper OuterInstance;

                    private AtomicReaderContext Context;

                    public ScorerAnonymousInnerClassHelper(WeightAnonymousInnerClassHelper outerInstance, AtomicReaderContext context)
                        : base(null)
                    {
                        this.OuterInstance = outerInstance;
                        this.Context = context;
                        docID = -1;
                    }

                    internal int docID;

                    public override int DocID()
                    {
                        return docID;
                    }

                    public override int Freq()
                    {
                        return 1;
                    }

                    public override long Cost()
                    {
                        return 1;
                    }

                    public override int NextDoc()
                    {
                        docID++;
                        if (docID >= Context.Reader.MaxDoc)
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

                    public override float Score()
                    {
                        int num = OuterInstance.OuterInstance.IdToNum[Convert.ToInt32(Context.Reader.Document(docID).Get("id"))];
                        if (OuterInstance.OuterInstance.Reverse)
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
                return "FixedScoreQuery " + IdToNum.Length + " ids; reverse=" + Reverse;
            }

            public override bool Equals(object o)
            {
                if ((o is FixedScoreQuery) == false)
                {
                    return false;
                }
                FixedScoreQuery other = (FixedScoreQuery)o;
                return Number.FloatToIntBits(Boost) == Number.FloatToIntBits(other.Boost) && Reverse == other.Reverse && Arrays.Equals(IdToNum, other.IdToNum);
            }

            public override object Clone()
            {
                return new FixedScoreQuery(IdToNum, Reverse);
            }

            public override int GetHashCode()
            {
                int PRIME = 31;
                int hash = base.GetHashCode();
                if (Reverse)
                {
                    hash = PRIME * hash + 3623;
                }
                hash = PRIME * hash + Arrays.GetHashCode(IdToNum);
                return hash;
            }
        }
    }
}