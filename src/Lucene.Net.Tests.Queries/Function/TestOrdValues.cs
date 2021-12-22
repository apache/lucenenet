// Lucene version compatibility level 4.8.1
using J2N.Text;
using Lucene.Net.Index;
using Lucene.Net.Queries.Function;
using Lucene.Net.Queries.Function.ValueSources;
using Lucene.Net.Search;
using NUnit.Framework;
using System;

namespace Lucene.Net.Tests.Queries.Function
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
    /// Test search based on OrdFieldSource and ReverseOrdFieldSource.
    /// <p/>
    /// Tests here create an index with a few documents, each having
    /// an indexed "id" field.
    /// The ord values of this field are later used for scoring.
    /// <p/>
    /// The order tests use Hits to verify that docs are ordered as expected.
    /// <p/>
    /// The exact score tests use TopDocs top to verify the exact score.
    /// </summary>
    public class TestOrdValues : FunctionTestSetup
    {
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            CreateIndex(false);
        }

        /// <summary>
        /// Test OrdFieldSource
        /// </summary>
        [Test]
        public void TestOrdFieldRank()
        {
            DoTestRank(ID_FIELD, true);
        }

        /// <summary>
        /// Test ReverseOrdFieldSource
        /// </summary>
        [Test]
        public void TestReverseOrdFieldRank()
        {
            DoTestRank(ID_FIELD, false);
        }

        /// <summary>
        /// Test that queries based on reverse/ordFieldScore scores correctly
        /// </summary>
        /// <param name="field"></param>
        /// <param name="inOrder"></param>
        private void DoTestRank(string field, bool inOrder)
        {
            IndexReader r = DirectoryReader.Open(dir);
            IndexSearcher s = NewSearcher(r);
            ValueSource vs;
            if (inOrder)
            {
                vs = new OrdFieldSource(field);
            }
            else
            {
                vs = new ReverseOrdFieldSource(field);
            }

            Query q = new FunctionQuery(vs);
            Log("test: " + q);
            QueryUtils.Check(Random, q, s);
            ScoreDoc[] h = s.Search(q, null, 1000).ScoreDocs;
            assertEquals("All docs should be matched!", N_DOCS, h.Length);
            string prevID = inOrder
                ? "IE"  // greater than all ids of docs in this test ("ID0001", etc.)
                : "IC"; // smaller than all ids of docs in this test ("ID0001", etc.)

            for (int i = 0; i < h.Length; i++)
            {
                string resID = s.Doc(h[i].Doc).Get(ID_FIELD);
                Log(i + ".   score=" + h[i].Score + "  -  " + resID);
                Log(s.Explain(q, h[i].Doc));
                if (inOrder)
                {
                    assertTrue("res id " + resID + " should be < prev res id " + prevID, resID.CompareToOrdinal(prevID) < 0);
                }
                else
                {
                    assertTrue("res id " + resID + " should be > prev res id " + prevID, resID.CompareToOrdinal(prevID) > 0);
                }
                prevID = resID;
            }
            r.Dispose();
        }

        /// <summary>
        /// Test exact score for OrdFieldSource
        /// </summary>
        [Test]
        public void TestOrdFieldExactScore()
        {
            DoTestExactScore(ID_FIELD, true);
        }

        /// <summary>
        /// Test exact score for ReverseOrdFieldSource
        /// </summary>
        [Test]
        public void TestReverseOrdFieldExactScore()
        {
            DoTestExactScore(ID_FIELD, false);
        }


        /// <summary>
        /// Test that queries based on reverse/ordFieldScore returns docs with expected score.
        /// </summary>
        /// <param name="field"></param>
        /// <param name="inOrder"></param>
        private void DoTestExactScore(string field, bool inOrder)
        {
            IndexReader r = DirectoryReader.Open(dir);
            IndexSearcher s = NewSearcher(r);
            ValueSource vs;
            if (inOrder)
            {
                vs = new OrdFieldSource(field);
            }
            else
            {
                vs = new ReverseOrdFieldSource(field);
            }
            Query q = new FunctionQuery(vs);
            TopDocs td = s.Search(q, null, 1000);
            assertEquals("All docs should be matched!", N_DOCS, td.TotalHits);
            ScoreDoc[] sd = td.ScoreDocs;
            for (int i = 0; i < sd.Length; i++)
            {
                float score = sd[i].Score;
                string id = s.IndexReader.Document(sd[i].Doc).Get(ID_FIELD);
                Log("-------- " + i + ". Explain doc " + id);
                Log(s.Explain(q, sd[i].Doc));
                float expectedScore = N_DOCS - i - 1;
                assertEquals("score of result " + i + " shuould be " + expectedScore + " != " + score, expectedScore, score, TEST_SCORE_TOLERANCE_DELTA);
                string expectedId = inOrder
                    ? Id2String(N_DOCS - i) // in-order ==> larger  values first
                    : Id2String(i + 1);     // reverse  ==> smaller values first
                assertTrue("id of result " + i + " shuould be " + expectedId + " != " + score, expectedId.Equals(id, StringComparison.Ordinal));
            }
            r.Dispose();
        }

        // LUCENE-1250
        [Test]
        public void TestEqualsNull()
        {
            OrdFieldSource ofs = new OrdFieldSource("f");
            assertFalse(ofs.Equals(null));

            ReverseOrdFieldSource rofs = new ReverseOrdFieldSource("f");
            assertFalse(rofs.Equals(null));
        }
    }
}
