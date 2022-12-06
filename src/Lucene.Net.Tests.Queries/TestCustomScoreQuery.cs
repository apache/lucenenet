// Lucene version compatibility level 4.8.1
using System;
using System.Collections.Generic;
using Lucene.Net.Index;
using Lucene.Net.Queries;
using Lucene.Net.Queries.Function;
using Lucene.Net.Search;
using Lucene.Net.Tests.Queries.Function;
using NUnit.Framework;

namespace Lucene.Net.Tests.Queries
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

    /// <summary>
    /// Test CustomScoreQuery search.
    /// </summary>
    public class TestCustomScoreQuery : FunctionTestSetup
    {
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            CreateIndex(true);
        }

        /// <summary>
        /// Test that CustomScoreQuery of Type.BYTE returns the expected scores.
        /// </summary>
        [Test]
        public virtual void TestCustomScoreByte()
        {
            // INT field values are small enough to be parsed as byte
            DoTestCustomScore(BYTE_VALUESOURCE, 1.0);
            DoTestCustomScore(BYTE_VALUESOURCE, 2.0);
        }

        /// <summary>
        /// Test that CustomScoreQuery of Type.SHORT returns the expected scores.
        /// </summary>
        [Test]
        public virtual void TestCustomScoreShort()
        {
            // INT field values are small enough to be parsed as short
            DoTestCustomScore(SHORT_VALUESOURCE, 1.0);
            DoTestCustomScore(SHORT_VALUESOURCE, 3.0);
        }

        /// <summary>
        /// Test that CustomScoreQuery of Type.INT returns the expected scores.
        /// </summary>
        [Test]
        public virtual void TestCustomScoreInt()
        {
            DoTestCustomScore(INT_VALUESOURCE, 1.0);
            DoTestCustomScore(INT_VALUESOURCE, 4.0);
        }

        /// <summary>
        /// Test that CustomScoreQuery of Type.FLOAT returns the expected scores.
        /// </summary>
        [Test]
        public virtual void TestCustomScoreFloat()
        {
            // INT field can be parsed as float
            DoTestCustomScore(INT_AS_FLOAT_VALUESOURCE, 1.0);
            DoTestCustomScore(INT_AS_FLOAT_VALUESOURCE, 5.0);

            // same values, but in float format
            DoTestCustomScore(FLOAT_VALUESOURCE, 1.0);
            DoTestCustomScore(FLOAT_VALUESOURCE, 6.0);
        }

        // must have static class otherwise serialization tests fail
        private class CustomAddQuery : CustomScoreQuery
        {
            // constructor
            internal CustomAddQuery(Query q, FunctionQuery qValSrc)
                : base(q, qValSrc)
            {
            }

            public override string Name => "customAdd";

            protected override CustomScoreProvider GetCustomScoreProvider(AtomicReaderContext context)
            {
                return new CustomScoreProviderAnonymousClass(context);
            }

            private sealed class CustomScoreProviderAnonymousClass : CustomScoreProvider
            {
                public CustomScoreProviderAnonymousClass(AtomicReaderContext context) : base(context)
                {
                }

                public override float CustomScore(int doc, float subQueryScore, float valSrcScore)
                {
                    return subQueryScore + valSrcScore;
                }

                public override Explanation CustomExplain(int doc, Explanation subQueryExpl, Explanation valSrcExpl)
                {
                    float valSrcScore = valSrcExpl is null ? 0 : valSrcExpl.Value;
                    Explanation exp = new Explanation(valSrcScore + subQueryExpl.Value, "custom score: sum of:");
                    exp.AddDetail(subQueryExpl);
                    if (valSrcExpl != null)
                    {
                        exp.AddDetail(valSrcExpl);
                    }
                    return exp;
                }
            }
        }

        // must have static class otherwise serialization tests fail
        private class CustomMulAddQuery : CustomScoreQuery
        {
            // constructor
            internal CustomMulAddQuery(Query q, FunctionQuery qValSrc1, FunctionQuery qValSrc2)
                : base(q, qValSrc1, qValSrc2)
            {
            }

            public override string Name => "customMulAdd";

            protected override CustomScoreProvider GetCustomScoreProvider(AtomicReaderContext context)
            {
                return new CustomScoreProviderAnonymousClass(context);
            }

            private sealed class CustomScoreProviderAnonymousClass : CustomScoreProvider
            {
                public CustomScoreProviderAnonymousClass(AtomicReaderContext context) : base(context)
                {
                }

                public override float CustomScore(int doc, float subQueryScore, float[] valSrcScores)
                {
                    if (valSrcScores.Length == 0)
                    {
                        return subQueryScore;
                    }
                    if (valSrcScores.Length == 1)
                    {
                        return subQueryScore + valSrcScores[0];
                        // confirm that skipping beyond the last doc, on the
                        // previous reader, hits NO_MORE_DOCS
                    }
                    return (subQueryScore + valSrcScores[0]) * valSrcScores[1]; // we know there are two
                }

                public override Explanation CustomExplain(int doc, Explanation subQueryExpl, Explanation[] valSrcExpls)
                {
                    if (valSrcExpls.Length == 0)
                    {
                        return subQueryExpl;
                    }
                    Explanation exp = new Explanation(valSrcExpls[0].Value + subQueryExpl.Value, "sum of:");
                    exp.AddDetail(subQueryExpl);
                    exp.AddDetail(valSrcExpls[0]);
                    if (valSrcExpls.Length == 1)
                    {
                        exp.Description = "CustomMulAdd, sum of:";
                        return exp;
                    }
                    Explanation exp2 = new Explanation(valSrcExpls[1].Value * exp.Value, "custom score: product of:");
                    exp2.AddDetail(valSrcExpls[1]);
                    exp2.AddDetail(exp);
                    return exp2;
                }
            }
        }

        private sealed class CustomExternalQuery : CustomScoreQuery
        {
            protected override CustomScoreProvider GetCustomScoreProvider(AtomicReaderContext context)
            {
                FieldCache.Int32s values = FieldCache.DEFAULT.GetInt32s(context.AtomicReader, INT_FIELD, false);
                return new CustomScoreProviderAnonymousClass(context, values);
            }
            
            private sealed class CustomScoreProviderAnonymousClass : CustomScoreProvider
            {
                private FieldCache.Int32s values;

                public CustomScoreProviderAnonymousClass(AtomicReaderContext context, FieldCache.Int32s values) : base(context)
                {
                    this.values = values;
                }

                public override float CustomScore(int doc, float subScore, float valSrcScore)
                {
                    assertTrue(doc <= m_context.AtomicReader.MaxDoc);
                    return values.Get(doc);
                }
            }

            public CustomExternalQuery(Query q) : base(q)
            {
            }
        }
        
        [Test]
        public virtual void TestCustomExternalQuery()
        {
            BooleanQuery q1 = new BooleanQuery();
            q1.Add(new TermQuery(new Term(TEXT_FIELD, "first")), Occur.SHOULD);
            q1.Add(new TermQuery(new Term(TEXT_FIELD, "aid")), Occur.SHOULD);
            q1.Add(new TermQuery(new Term(TEXT_FIELD, "text")), Occur.SHOULD);

            Query q = new CustomExternalQuery(q1);
            Log(q);

            IndexReader r = DirectoryReader.Open(dir);
            IndexSearcher s = NewSearcher(r);
            TopDocs hits = s.Search(q, 1000);
            assertEquals(N_DOCS, hits.TotalHits);
            for (int i = 0; i < N_DOCS; i++)
            {
                int doc = hits.ScoreDocs[i].Doc;
                float score = hits.ScoreDocs[i].Score;
                assertEquals("doc=" + doc, (float)1 + (4 * doc) % N_DOCS, score, 0.0001);
            }
            r.Dispose();
        }
       
        [Test] 
        public virtual void TestRewrite()
        {
            IndexReader r = DirectoryReader.Open(dir);
            IndexSearcher s = NewSearcher(r);

            Query q = new TermQuery(new Term(TEXT_FIELD, "first"));
            CustomScoreQuery original = new CustomScoreQuery(q);
            CustomScoreQuery rewritten = (CustomScoreQuery)original.Rewrite(s.IndexReader);
            assertTrue("rewritten query should be identical, as TermQuery does not rewrite", original == rewritten);
            assertTrue("no hits for query", s.Search(rewritten, 1).TotalHits > 0);
            assertEquals(s.Search(q, 1).TotalHits, s.Search(rewritten, 1).TotalHits);

            q = new TermRangeQuery(TEXT_FIELD, null, null, true, true); // everything
            original = new CustomScoreQuery(q);
            rewritten = (CustomScoreQuery)original.Rewrite(s.IndexReader);
            assertTrue("rewritten query should not be identical, as TermRangeQuery rewrites", original != rewritten);
            assertTrue("no hits for query", s.Search(rewritten, 1).TotalHits > 0);
            assertEquals(s.Search(q, 1).TotalHits, s.Search(original, 1).TotalHits);
            assertEquals(s.Search(q, 1).TotalHits, s.Search(rewritten, 1).TotalHits);

            r.Dispose();
        }

        // Test that FieldScoreQuery returns docs with expected score.
        private void DoTestCustomScore(ValueSource valueSource, double dboost)
        {
            float boost = (float)dboost;
            FunctionQuery functionQuery = new FunctionQuery(valueSource);
            IndexReader r = DirectoryReader.Open(dir);
            IndexSearcher s = NewSearcher(r);

            // regular (boolean) query.
            BooleanQuery q1 = new BooleanQuery();
            q1.Add(new TermQuery(new Term(TEXT_FIELD, "first")), Occur.SHOULD);
            q1.Add(new TermQuery(new Term(TEXT_FIELD, "aid")), Occur.SHOULD);
            q1.Add(new TermQuery(new Term(TEXT_FIELD, "text")), Occur.SHOULD);
            Log(q1);

            // custom query, that should score the same as q1.
            BooleanQuery q2CustomNeutral = new BooleanQuery(true);
            Query q2CustomNeutralInner = new CustomScoreQuery(q1);
            q2CustomNeutral.Add(q2CustomNeutralInner, Occur.SHOULD);
            // a little tricky: we split the boost across an outer BQ and CustomScoreQuery
            // this ensures boosting is correct across all these functions (see LUCENE-4935)
            q2CustomNeutral.Boost = (float)Math.Sqrt(dboost);
            q2CustomNeutralInner.Boost = (float)Math.Sqrt(dboost);
            Log(q2CustomNeutral);

            // custom query, that should (by default) multiply the scores of q1 by that of the field
            CustomScoreQuery q3CustomMul = new CustomScoreQuery(q1, functionQuery);
            q3CustomMul.IsStrict = true;
            q3CustomMul.Boost = boost;
            Log(q3CustomMul);

            // custom query, that should add the scores of q1 to that of the field
            CustomScoreQuery q4CustomAdd = new CustomAddQuery(q1, functionQuery);
            q4CustomAdd.IsStrict = true;
            q4CustomAdd.Boost = boost;
            Log(q4CustomAdd);

            // custom query, that multiplies and adds the field score to that of q1
            CustomScoreQuery q5CustomMulAdd = new CustomMulAddQuery(q1, functionQuery, functionQuery);
            q5CustomMulAdd.IsStrict = true;
            q5CustomMulAdd.Boost = boost;
            Log(q5CustomMulAdd);

            // do al the searches 
            TopDocs td1 = s.Search(q1, null, 1000);
            TopDocs td2CustomNeutral = s.Search(q2CustomNeutral, null, 1000);
            TopDocs td3CustomMul = s.Search(q3CustomMul, null, 1000);
            TopDocs td4CustomAdd = s.Search(q4CustomAdd, null, 1000);
            TopDocs td5CustomMulAdd = s.Search(q5CustomMulAdd, null, 1000);

            // put results in map so we can verify the scores although they have changed
            IDictionary<int, float> h1               = TopDocsToMap(td1);
            IDictionary<int, float> h2CustomNeutral  = TopDocsToMap(td2CustomNeutral);
            IDictionary<int, float> h3CustomMul      = TopDocsToMap(td3CustomMul);
            IDictionary<int, float> h4CustomAdd      = TopDocsToMap(td4CustomAdd);
            IDictionary<int, float> h5CustomMulAdd   = TopDocsToMap(td5CustomMulAdd);

            VerifyResults(boost, s,
                h1, h2CustomNeutral, h3CustomMul, h4CustomAdd, h5CustomMulAdd,
                q1, q2CustomNeutral, q3CustomMul, q4CustomAdd, q5CustomMulAdd);
            r.Dispose();
        }

        // verify results are as expected.
        private void VerifyResults(float boost, IndexSearcher s,
            IDictionary<int, float> h1, IDictionary<int, float> h2customNeutral, IDictionary<int, float> h3CustomMul, IDictionary<int, float> h4CustomAdd, IDictionary<int, float> h5CustomMulAdd,
            Query q1, Query q2, Query q3, Query q4, Query q5)
        {

            // verify numbers of matches
            Log("#hits = " + h1.Count);
            assertEquals("queries should have same #hits", h1.Count, h2customNeutral.Count);
            assertEquals("queries should have same #hits", h1.Count, h3CustomMul.Count);
            assertEquals("queries should have same #hits", h1.Count, h4CustomAdd.Count);
            assertEquals("queries should have same #hits", h1.Count, h5CustomMulAdd.Count);

            QueryUtils.Check(Random, q1, s, Rarely());
            QueryUtils.Check(Random, q2, s, Rarely());
            QueryUtils.Check(Random, q3, s, Rarely());
            QueryUtils.Check(Random, q4, s, Rarely());
            QueryUtils.Check(Random, q5, s, Rarely());

            // verify scores ratios
            foreach (int doc in h1.Keys)
            {
                Log("doc = " + doc);

                float fieldScore = ExpectedFieldScore(s.IndexReader.Document(doc).Get(ID_FIELD));
                Log("fieldScore = " + fieldScore);
                assertTrue("fieldScore should not be 0", fieldScore > 0);

                float score1 = h1[doc];
                LogResult("score1=", s, q1, doc, score1);

                float score2 = h2customNeutral[doc];
                LogResult("score2=", s, q2, doc, score2);
                assertEquals("same score (just boosted) for neutral", boost * score1, score2, CheckHits.ExplainToleranceDelta(boost * score1, score2));

                float score3 = h3CustomMul[doc];
                LogResult("score3=", s, q3, doc, score3);
                assertEquals("new score for custom mul", boost * fieldScore * score1, score3, CheckHits.ExplainToleranceDelta(boost * fieldScore * score1, score3));

                float score4 = h4CustomAdd[doc];
                LogResult("score4=", s, q4, doc, score4);
                assertEquals("new score for custom add", boost * (fieldScore + score1), score4, CheckHits.ExplainToleranceDelta(boost * (fieldScore + score1), score4));

                float score5 = h5CustomMulAdd[doc];
                LogResult("score5=", s, q5, doc, score5);
                assertEquals("new score for custom mul add", boost * fieldScore * (score1 + fieldScore), score5, CheckHits.ExplainToleranceDelta(boost * fieldScore * (score1 + fieldScore), score5));
            }
        }
        
        private void LogResult(string msg, IndexSearcher s, Query q, int doc, float score1)
        {
            Log(msg + " " + score1);
            Log("Explain by: " + q);
            Log(s.Explain(q, doc));
        }

        /// <summary>
        /// Since custom scoring modified the order of docs, map results
        /// by doc ids so that we can later compare/verify them.
        /// </summary>
        /// <param name="td"></param>
        /// <returns></returns>
        private IDictionary<int, float> TopDocsToMap(TopDocs td)
        {
            var h = new Dictionary<int, float>();
            for (int i = 0; i < td.TotalHits; i++)
            {
                h[td.ScoreDocs[i].Doc] = td.ScoreDocs[i].Score;
            }
            return h;
        }

    }
}
