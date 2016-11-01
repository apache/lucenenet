using Lucene.Net.Index;
using Lucene.Net.Queries.Function;
using Lucene.Net.Search;
using NUnit.Framework;

namespace Lucene.Net.Tests.Queries.Function
{
    /// <summary>
    /// Test FieldScoreQuery search.
    /// <p>
    /// Tests here create an index with a few documents, each having
    /// an int value indexed  field and a float value indexed field.
    /// The values of these fields are later used for scoring.
    /// <p>
    /// The rank tests use Hits to verify that docs are ordered (by score) as expected.
    /// <p>
    /// The exact score tests use TopDocs top to verify the exact score.  
    /// </summary>
    public class TestFieldScoreQuery : FunctionTestSetup
    {
        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            CreateIndex(true);
        }

        /// <summary>
        /// Test that FieldScoreQuery of Type.BYTE returns docs in expected order.
        /// </summary>
        [Test]
        public void TestRankByte()
        {
            // INT field values are small enough to be parsed as byte
            DoTestRank(BYTE_VALUESOURCE);
        }

        /// <summary>
        /// Test that FieldScoreQuery of Type.SHORT returns docs in expected order.
        /// </summary>
        [Test]
        public void TestRankShort()
        {
            // INT field values are small enough to be parsed as short
            DoTestRank(SHORT_VALUESOURCE);
        }

        /// <summary>
        /// Test that FieldScoreQuery of Type.INT returns docs in expected order.
        /// </summary>
        [Test]
        public void TestRankInt()
        {
            DoTestRank(INT_VALUESOURCE);
        }

        /// <summary>
        /// Test that FieldScoreQuery of Type.FLOAT returns docs in expected order.
        /// </summary>
        [Test]
        public void TestRankFloat()
        {
            // INT field can be parsed as float
            DoTestRank(INT_AS_FLOAT_VALUESOURCE);
            // same values, but in flot format
            DoTestRank(FLOAT_VALUESOURCE);
        }

        /// <summary>
        /// Test that FieldScoreQuery returns docs in expected order.
        /// </summary>
        /// <param name="valueSource"></param>
        private void DoTestRank(ValueSource valueSource)
        {
            FunctionQuery functionQuery = new FunctionQuery(valueSource);
            IndexReader r = DirectoryReader.Open(dir);
            IndexSearcher s = NewSearcher(r);
            Log("test: " + functionQuery);
            QueryUtils.Check(Random(), functionQuery, s, Similarity);
            ScoreDoc[] h = s.Search(functionQuery, null, 1000).ScoreDocs;
            assertEquals("All docs should be matched!", N_DOCS, h.Length);
            string prevID = "ID" + (N_DOCS + 1); // greater than all ids of docs in this test
            for (int i = 0; i < h.Length; i++)
            {
                string resID = s.Doc(h[i].Doc).Get(ID_FIELD);
                Log(i + ".   score=" + h[i].Score + "  -  " + resID);
                Log(s.Explain(functionQuery, h[i].Doc));
                assertTrue("res id " + resID + " should be < prev res id " + prevID, resID.CompareTo(prevID) < 0);
                prevID = resID;
            }
            r.Dispose();
        }

        /// <summary>
        /// Test that FieldScoreQuery of Type.BYTE returns the expected scores. </summary>
        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void testExactScoreByte() throws Exception
        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        public virtual void testExactScoreByte()
        {
            // INT field values are small enough to be parsed as byte
            doTestExactScore(BYTE_VALUESOURCE);
        }

        /// <summary>
        /// Test that FieldScoreQuery of Type.SHORT returns the expected scores. </summary>
        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void testExactScoreShort() throws Exception
        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        public virtual void testExactScoreShort()
        {
            // INT field values are small enough to be parsed as short
            doTestExactScore(SHORT_VALUESOURCE);
        }

        /// <summary>
        /// Test that FieldScoreQuery of Type.INT returns the expected scores. </summary>
        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void testExactScoreInt() throws Exception
        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        public virtual void testExactScoreInt()
        {
            doTestExactScore(INT_VALUESOURCE);
        }

        /// <summary>
        /// Test that FieldScoreQuery of Type.FLOAT returns the expected scores. </summary>
        //JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
        //ORIGINAL LINE: @Test public void testExactScoreFloat() throws Exception
        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        public virtual void testExactScoreFloat()
        {
            // INT field can be parsed as float
            doTestExactScore(INT_AS_FLOAT_VALUESOURCE);
            // same values, but in flot format
            doTestExactScore(FLOAT_VALUESOURCE);
        }

        // Test that FieldScoreQuery returns docs with expected score.
        //JAVA TO C# CONVERTER WARNING: Method 'throws' clauses are not available in .NET:
        //ORIGINAL LINE: private void doTestExactScore(ValueSource valueSource) throws Exception
        private void doTestExactScore(ValueSource valueSource)
        {
            FunctionQuery functionQuery = new FunctionQuery(valueSource);
            IndexReader r = DirectoryReader.Open(dir);
            IndexSearcher s = NewSearcher(r);
            TopDocs td = s.Search(functionQuery, null, 1000);
            assertEquals("All docs should be matched!", N_DOCS, td.TotalHits);
            ScoreDoc[] sd = td.ScoreDocs;
            foreach (ScoreDoc aSd in sd)
            {
                float score = aSd.Score;
                Log(s.Explain(functionQuery, aSd.Doc));
                string id = s.IndexReader.Document(aSd.Doc).Get(ID_FIELD);
                float expectedScore = ExpectedFieldScore(id); // "ID7" --> 7.0
                assertEquals("score of " + id + " shuould be " + expectedScore + " != " + score, expectedScore, score, TEST_SCORE_TOLERANCE_DELTA);
            }
            r.Dispose();
        }

    }
}