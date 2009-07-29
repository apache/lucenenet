/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;

using NUnit.Framework;

using SimpleAnalyzer = Lucene.Net.Analysis.SimpleAnalyzer;
using WhitespaceAnalyzer = Lucene.Net.Analysis.WhitespaceAnalyzer;
using Document = Lucene.Net.Documents.Document;
using Field = Lucene.Net.Documents.Field;
using IndexReader = Lucene.Net.Index.IndexReader;
using IndexWriter = Lucene.Net.Index.IndexWriter;
using Term = Lucene.Net.Index.Term;
using Directory = Lucene.Net.Store.Directory;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;

namespace Lucene.Net.Search
{

    [TestFixture]
    public class TestConstantScoreRangeQuery : BaseTestRangeFilter
    {
        private class AnonymousClassHitCollector : HitCollector
        {
            public AnonymousClassHitCollector(TestConstantScoreRangeQuery enclosingInstance)
            {
                InitBlock(enclosingInstance);
            }
            private void InitBlock(TestConstantScoreRangeQuery enclosingInstance)
            {
                this.enclosingInstance = enclosingInstance;
            }
            private TestConstantScoreRangeQuery enclosingInstance;
            public TestConstantScoreRangeQuery Enclosing_Instance
            {
                get
                {
                    return enclosingInstance;
                }

            }
            public override void Collect(int doc, float score)
            {
                Enclosing_Instance.AssertEquals("score for doc " + doc + " was not correct", 1.0f, score);
            }
        }

        /// <summary>threshold for comparing floats </summary>
        public const float SCORE_COMP_THRESH = 1e-6f;

        public TestConstantScoreRangeQuery(string name)
            : base(name)
        {
        }
        public TestConstantScoreRangeQuery()
            : base()
        {
        }

        internal Directory small;

        internal virtual void AssertEquals(string m, float e, float a)
        {
            Assert.AreEqual(e, a, m, SCORE_COMP_THRESH);
        }

        static public void AssertEquals(string m, int e, int a)
        {
            Assert.AreEqual(e, a, m);
        }

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();
            string[] data = new string[] { "A 1 2 3 4 5 6", "Z       4 5 6", null, "B   2   4 5 6", "Y     3   5 6", null, "C     3     6", "X       4 5 6" };

            small = new RAMDirectory();
            IndexWriter writer = new IndexWriter(small, new WhitespaceAnalyzer(), true, IndexWriter.MaxFieldLength.LIMITED);

            for (int i = 0; i < data.Length; i++)
            {
                Lucene.Net.Documents.Document doc = new Lucene.Net.Documents.Document();
                doc.Add(new Field("id", System.Convert.ToString(i), Field.Store.YES, Field.Index.NOT_ANALYZED)); //Field.Keyword("id",string.valueOf(i)));
                doc.Add(new Field("all", "all", Field.Store.YES, Field.Index.NOT_ANALYZED)); //Field.Keyword("all","all"));
                if (null != data[i])
                {
                    doc.Add(new Field("data", data[i], Field.Store.YES, Field.Index.ANALYZED)); //Field.Text("data",data[i]));
                }
                writer.AddDocument(doc);
            }

            writer.Optimize();
            writer.Close();
        }



        /// <summary>macro for readability </summary>
        public static Query Csrq(string f, string l, string h, bool il, bool ih)
        {
            return new ConstantScoreRangeQuery(f, l, h, il, ih);
        }

        /// <summary>macro for readability </summary>
        public static Query Csrq(string f, string l, string h, bool il, bool ih, System.Globalization.CompareInfo c)
        {
            return new ConstantScoreRangeQuery(f, l, h, il, ih, c);
        }

        [Test]
        public virtual void TestBasics()
        {
            QueryUtils.Check(Csrq("data", "1", "6", T, T));
            QueryUtils.Check(Csrq("data", "A", "Z", T, T));
            QueryUtils.CheckUnequal(Csrq("data", "1", "6", T, T), Csrq("data", "A", "Z", T, T));
        }

        [Test]
        public void TestBasicsCollating()
        {
            System.Globalization.CompareInfo c = System.Globalization.CultureInfo.GetCultureInfo("en-us").CompareInfo;
            QueryUtils.Check(Csrq("data", "1", "6", T, T, c));
            QueryUtils.Check(Csrq("data", "A", "Z", T, T, c));
            QueryUtils.CheckUnequal(Csrq("data", "1", "6", T, T, c), Csrq("data", "A", "Z", T, T, c));
        }

        [Test]
        public virtual void TestEqualScores()
        {
            // NOTE: uses index build in *this* SetUp

            IndexReader reader = IndexReader.Open(small);
            IndexSearcher search = new IndexSearcher(reader);

            ScoreDoc[] result;

            // some hits match more terms then others, score should be the same

            result = search.Search(Csrq("data", "1", "6", T, T), null, 1000).scoreDocs;
            int numHits = result.Length;
            Assert.AreEqual(6, numHits, "wrong number of results");
            float score = result[0].score;
            for (int i = 1; i < numHits; i++)
            {
                AssertEquals("score for " + i + " was not the same", score, result[i].score);
            }
        }

        [Test]
        public virtual void TestBoost()
        {
            // NOTE: uses index build in *this* SetUp

            IndexReader reader = IndexReader.Open(small);
            IndexSearcher search = new IndexSearcher(reader);

            // test for correct application of query normalization
            // must use a non score normalizing method for this.
            Query q = Csrq("data", "1", "6", T, T);
            q.SetBoost(100);
            search.Search(q, null, new AnonymousClassHitCollector(this));


            //
            // Ensure that boosting works to score one clause of a query higher
            // than another.
            //
            Query q1 = Csrq("data", "A", "A", T, T); // matches document #0
            q1.SetBoost(.1f);
            Query q2 = Csrq("data", "Z", "Z", T, T); // matches document #1
            BooleanQuery bq = new BooleanQuery(true);
            bq.Add(q1, BooleanClause.Occur.SHOULD);
            bq.Add(q2, BooleanClause.Occur.SHOULD);

            ScoreDoc[] hits = search.Search(bq, null, 1000).scoreDocs;
            Assert.AreEqual(1, hits[0].doc);
            Assert.AreEqual(0, hits[1].doc);
            Assert.IsTrue(hits[0].score > hits[1].score);

            q1 = Csrq("data", "A", "A", T, T); // matches document #0
            q1.SetBoost(10f);
            q2 = Csrq("data", "Z", "Z", T, T); // matches document #1
            bq = new BooleanQuery(true);
            bq.Add(q1, BooleanClause.Occur.SHOULD);
            bq.Add(q2, BooleanClause.Occur.SHOULD);

            hits = search.Search(bq, null, 1000).scoreDocs;
            Assert.AreEqual(0, hits[0].doc);
            Assert.AreEqual(1, hits[1].doc);
            Assert.IsTrue(hits[0].score > hits[1].score);
        }

        [Test]
        public virtual void TestBooleanOrderUnAffected()
        {
            // NOTE: uses index build in *this* SetUp

            IndexReader reader = IndexReader.Open(small);
            IndexSearcher search = new IndexSearcher(reader);

            // first do a regular RangeQuery which uses term expansion so
            // docs with more terms in range get higher scores

            Query rq = new RangeQuery(new Term("data", "1"), new Term("data", "4"), T);

            ScoreDoc[] expected = search.Search(rq, null, 1000).scoreDocs;
            int numHits = expected.Length;

            // now do a bool where which also contains a
            // ConstantScoreRangeQuery and make sure hte order is the same

            BooleanQuery q = new BooleanQuery();
            q.Add(rq, BooleanClause.Occur.MUST); //T, F);
            q.Add(Csrq("data", "1", "6", T, T), BooleanClause.Occur.MUST); //T, F);

            ScoreDoc[] actual = search.Search(q, null, 1000).scoreDocs;

            Assert.AreEqual(numHits, actual.Length, "wrong number of hits");
            for (int i = 0; i < numHits; i++)
            {
                Assert.AreEqual(expected[i].doc, actual[i].doc, "mismatch in docid for hit#" + i);
            }
        }

        [Test]
        public void TestRangeQueryId()
        {
            // NOTE: uses index build in *super* setUp

            IndexReader reader = IndexReader.Open(signedIndex.index);
            IndexSearcher search = new IndexSearcher(reader);

            int medId = ((maxId - minId) / 2);

            string minIP = Pad(minId);
            string maxIP = Pad(maxId);
            string medIP = Pad(medId);

            int numDocs = reader.NumDocs();

            Assert.AreEqual(numDocs, 1 + maxId - minId, "num of docs");

            ScoreDoc[] result;

            // test id, bounded on both ends

            result = search.Search(Csrq("id", minIP, maxIP, T, T), null, numDocs).scoreDocs;
            Assert.AreEqual(numDocs, result.Length, "find all");

            result = search.Search(Csrq("id", minIP, maxIP, T, F), null, numDocs).scoreDocs;
            Assert.AreEqual(numDocs - 1, result.Length, "all but last");

            result = search.Search(Csrq("id", minIP, maxIP, F, T), null, numDocs).scoreDocs;
            Assert.AreEqual(numDocs - 1, result.Length, "all but first");

            result = search.Search(Csrq("id", minIP, maxIP, F, F), null, numDocs).scoreDocs;
            Assert.AreEqual(numDocs - 2, result.Length, "all but ends");

            result = search.Search(Csrq("id", medIP, maxIP, T, T), null, numDocs).scoreDocs;
            Assert.AreEqual(1 + maxId - medId, result.Length, "med and up");

            result = search.Search(Csrq("id", minIP, medIP, T, T), null, numDocs).scoreDocs;
            Assert.AreEqual(1 + medId - minId, result.Length, "up to med");

            // unbounded id

            result = search.Search(Csrq("id", minIP, null, T, F), null, numDocs).scoreDocs;
            Assert.AreEqual(numDocs, result.Length, "min and up");

            result = search.Search(Csrq("id", null, maxIP, F, T), null, numDocs).scoreDocs;
            Assert.AreEqual(numDocs, result.Length, "max and down");

            result = search.Search(Csrq("id", minIP, null, F, F), null, numDocs).scoreDocs;
            Assert.AreEqual(numDocs - 1, result.Length, "not min, but up");

            result = search.Search(Csrq("id", null, maxIP, F, F), null, numDocs).scoreDocs;
            Assert.AreEqual(numDocs - 1, result.Length, "not max, but down");

            result = search.Search(Csrq("id", medIP, maxIP, T, F), null, numDocs).scoreDocs;
            Assert.AreEqual(maxId - medId, result.Length, "med and up, not max");

            result = search.Search(Csrq("id", minIP, medIP, F, T), null, numDocs).scoreDocs;
            Assert.AreEqual(medId - minId, result.Length, "not min, up to med");

            // very small sets

            result = search.Search(Csrq("id", minIP, minIP, F, F), null, numDocs).scoreDocs;
            Assert.AreEqual(0, result.Length, "min,min,F,F");
            result = search.Search(Csrq("id", medIP, medIP, F, F), null, numDocs).scoreDocs;
            Assert.AreEqual(0, result.Length, "med,med,F,F");
            result = search.Search(Csrq("id", maxIP, maxIP, F, F), null, numDocs).scoreDocs;
            Assert.AreEqual(0, result.Length, "max,max,F,F");

            result = search.Search(Csrq("id", minIP, minIP, T, T), null, numDocs).scoreDocs;
            Assert.AreEqual(1, result.Length, "min,min,T,T");
            result = search.Search(Csrq("id", null, minIP, F, T), null, numDocs).scoreDocs;
            Assert.AreEqual(1, result.Length, "nul,min,F,T");

            result = search.Search(Csrq("id", maxIP, maxIP, T, T), null, numDocs).scoreDocs;
            Assert.AreEqual(1, result.Length, "max,max,T,T");
            result = search.Search(Csrq("id", maxIP, null, T, F), null, numDocs).scoreDocs;
            Assert.AreEqual(1, result.Length, "max,nul,T,T");

            result = search.Search(Csrq("id", medIP, medIP, T, T), null, numDocs).scoreDocs;
            Assert.AreEqual(1, result.Length, "med,med,T,T");

        }


        [Test]
        public void TestRangeQueryIdCollating()
        {
            // NOTE: uses index build in *super* setUp

            IndexReader reader = IndexReader.Open(signedIndex.index);
            IndexSearcher search = new IndexSearcher(reader);

            int medId = ((maxId - minId) / 2);

            string minIP = Pad(minId);
            string maxIP = Pad(maxId);
            string medIP = Pad(medId);

            int numDocs = reader.NumDocs();

            Assert.AreEqual(numDocs, 1 + maxId - minId, "num of docs");

            ScoreDoc[] result;

            System.Globalization.CompareInfo c = System.Globalization.CultureInfo.GetCultureInfo("en-us").CompareInfo;

            // test id, bounded on both ends

            result = search.Search(Csrq("id", minIP, maxIP, T, T, c), null, numDocs).scoreDocs;
            Assert.AreEqual(numDocs, result.Length, "find all");

            result = search.Search(Csrq("id", minIP, maxIP, T, F, c), null, numDocs).scoreDocs;
            Assert.AreEqual(numDocs - 1, result.Length, "all but last");

            result = search.Search(Csrq("id", minIP, maxIP, F, T, c), null, numDocs).scoreDocs;
            Assert.AreEqual(numDocs - 1, result.Length, "all but first");

            result = search.Search(Csrq("id", minIP, maxIP, F, F, c), null, numDocs).scoreDocs;
            Assert.AreEqual(numDocs - 2, result.Length, "all but ends");

            result = search.Search(Csrq("id", medIP, maxIP, T, T, c), null, numDocs).scoreDocs;
            Assert.AreEqual(1 + maxId - medId, result.Length, "med and up");

            result = search.Search(Csrq("id", minIP, medIP, T, T, c), null, numDocs).scoreDocs;
            Assert.AreEqual(1 + medId - minId, result.Length, "up to med");

            // unbounded id

            result = search.Search(Csrq("id", minIP, null, T, F, c), null, numDocs).scoreDocs;
            Assert.AreEqual(numDocs, result.Length, "min and up");

            result = search.Search(Csrq("id", null, maxIP, F, T, c), null, numDocs).scoreDocs;
            Assert.AreEqual(numDocs, result.Length, "max and down");

            result = search.Search(Csrq("id", minIP, null, F, F, c), null, numDocs).scoreDocs;
            Assert.AreEqual(numDocs - 1, result.Length, "not min, but up");

            result = search.Search(Csrq("id", null, maxIP, F, F, c), null, numDocs).scoreDocs;
            Assert.AreEqual(numDocs - 1, result.Length, "not max, but down");

            result = search.Search(Csrq("id", medIP, maxIP, T, F, c), null, numDocs).scoreDocs;
            Assert.AreEqual(maxId - medId, result.Length, "med and up, not max");

            result = search.Search(Csrq("id", minIP, medIP, F, T, c), null, numDocs).scoreDocs;
            Assert.AreEqual(medId - minId, result.Length, "not min, up to med");

            // very small sets

            result = search.Search(Csrq("id", minIP, minIP, F, F, c), null, numDocs).scoreDocs;
            Assert.AreEqual(0, result.Length, "min,min,F,F,c");
            result = search.Search(Csrq("id", medIP, medIP, F, F, c), null, numDocs).scoreDocs;
            Assert.AreEqual(0, result.Length, "med,med,F,F,c");
            result = search.Search(Csrq("id", maxIP, maxIP, F, F, c), null, numDocs).scoreDocs;
            Assert.AreEqual(0, result.Length, "max,max,F,F,c");

            result = search.Search(Csrq("id", minIP, minIP, T, T, c), null, numDocs).scoreDocs;
            Assert.AreEqual(1, result.Length, "min,min,T,T,c");
            result = search.Search(Csrq("id", null, minIP, F, T, c), null, numDocs).scoreDocs;
            Assert.AreEqual(1, result.Length, "nul,min,F,T,c");

            result = search.Search(Csrq("id", maxIP, maxIP, T, T, c), null, numDocs).scoreDocs;
            Assert.AreEqual(1, result.Length, "max,max,T,T,c");
            result = search.Search(Csrq("id", maxIP, null, T, F, c), null, numDocs).scoreDocs;
            Assert.AreEqual(1, result.Length, "max,nul,T,T,c");

            result = search.Search(Csrq("id", medIP, medIP, T, T, c), null, numDocs).scoreDocs;
            Assert.AreEqual(1, result.Length, "med,med,T,T,c");
        }


        [Test]
        public void TestRangeQueryRand()
        {
            // NOTE: uses index build in *super* setUp

            IndexReader reader = IndexReader.Open(signedIndex.index);
            IndexSearcher search = new IndexSearcher(reader);

            string minRP = Pad(signedIndex.minR);
            string maxRP = Pad(signedIndex.maxR);

            int numDocs = reader.NumDocs();

            Assert.AreEqual(numDocs, 1 + maxId - minId, "num of docs");

            ScoreDoc[] result;

            // test extremes, bounded on both ends

            result = search.Search(Csrq("rand", minRP, maxRP, T, T), null, numDocs).scoreDocs;
            Assert.AreEqual(numDocs, result.Length, "find all");

            result = search.Search(Csrq("rand", minRP, maxRP, T, F), null, numDocs).scoreDocs;
            Assert.AreEqual(numDocs - 1, result.Length, "all but biggest");

            result = search.Search(Csrq("rand", minRP, maxRP, F, T), null, numDocs).scoreDocs;
            Assert.AreEqual(numDocs - 1, result.Length, "all but smallest");

            result = search.Search(Csrq("rand", minRP, maxRP, F, F), null, numDocs).scoreDocs;
            Assert.AreEqual(numDocs - 2, result.Length, "all but extremes");

            // unbounded

            result = search.Search(Csrq("rand", minRP, null, T, F), null, numDocs).scoreDocs;
            Assert.AreEqual(numDocs, result.Length, "smallest and up");

            result = search.Search(Csrq("rand", null, maxRP, F, T), null, numDocs).scoreDocs;
            Assert.AreEqual(numDocs, result.Length, "biggest and down");

            result = search.Search(Csrq("rand", minRP, null, F, F), null, numDocs).scoreDocs;
            Assert.AreEqual(numDocs - 1, result.Length, "not smallest, but up");

            result = search.Search(Csrq("rand", null, maxRP, F, F), null, numDocs).scoreDocs;
            Assert.AreEqual(numDocs - 1, result.Length, "not biggest, but down");

            // very small sets

            result = search.Search(Csrq("rand", minRP, minRP, F, F), null, numDocs).scoreDocs;
            Assert.AreEqual(0, result.Length, "min,min,F,F");
            result = search.Search(Csrq("rand", maxRP, maxRP, F, F), null, numDocs).scoreDocs;
            Assert.AreEqual(0, result.Length, "max,max,F,F");

            result = search.Search(Csrq("rand", minRP, minRP, T, T), null, numDocs).scoreDocs;
            Assert.AreEqual(1, result.Length, "min,min,T,T");
            result = search.Search(Csrq("rand", null, minRP, F, T), null, numDocs).scoreDocs;
            Assert.AreEqual(1, result.Length, "nul,min,F,T");

            result = search.Search(Csrq("rand", maxRP, maxRP, T, T), null, numDocs).scoreDocs;
            Assert.AreEqual(1, result.Length, "max,max,T,T");
            result = search.Search(Csrq("rand", maxRP, null, T, F), null, numDocs).scoreDocs;
            Assert.AreEqual(1, result.Length, "max,nul,T,T");

        }

        [Test]
        public void TestRangeQueryRandCollating()
        {
            // NOTE: uses index build in *super* setUp

            // using the unsigned index because collation seems to ignore hyphens
            IndexReader reader = IndexReader.Open(unsignedIndex.index);
            IndexSearcher search = new IndexSearcher(reader);

            string minRP = Pad(unsignedIndex.minR);
            string maxRP = Pad(unsignedIndex.maxR);

            int numDocs = reader.NumDocs();

            Assert.AreEqual(numDocs, 1 + maxId - minId, "num of docs");

            ScoreDoc[] result;

            System.Globalization.CompareInfo c = System.Globalization.CultureInfo.GetCultureInfo("en-us").CompareInfo;

            // test extremes, bounded on both ends

            result = search.Search(Csrq("rand", minRP, maxRP, T, T, c), null, numDocs).scoreDocs;
            Assert.AreEqual(numDocs, result.Length, "find all");

            result = search.Search(Csrq("rand", minRP, maxRP, T, F, c), null, numDocs).scoreDocs;
            Assert.AreEqual(numDocs - 1, result.Length, "all but biggest");

            result = search.Search(Csrq("rand", minRP, maxRP, F, T, c), null, numDocs).scoreDocs;
            Assert.AreEqual(numDocs - 1, result.Length, "all but smallest");

            result = search.Search(Csrq("rand", minRP, maxRP, F, F, c), null, numDocs).scoreDocs;
            Assert.AreEqual(numDocs - 2, result.Length, "all but extremes");

            // unbounded

            result = search.Search(Csrq("rand", minRP, null, T, F, c), null, numDocs).scoreDocs;
            Assert.AreEqual(numDocs, result.Length, "smallest and up");

            result = search.Search(Csrq("rand", null, maxRP, F, T, c), null, numDocs).scoreDocs;
            Assert.AreEqual(numDocs, result.Length, "biggest and down");

            result = search.Search(Csrq("rand", minRP, null, F, F, c), null, numDocs).scoreDocs;
            Assert.AreEqual(numDocs - 1, result.Length, "not smallest, but up");

            result = search.Search(Csrq("rand", null, maxRP, F, F, c), null, numDocs).scoreDocs;
            Assert.AreEqual(numDocs - 1, result.Length, "not biggest, but down");

            // very small sets

            result = search.Search(Csrq("rand", minRP, minRP, F, F, c), null, numDocs).scoreDocs;
            Assert.AreEqual(0, result.Length, "min,min,F,F,c");
            result = search.Search(Csrq("rand", maxRP, maxRP, F, F, c), null, numDocs).scoreDocs;
            Assert.AreEqual(0, result.Length, "max,max,F,F,c");

            result = search.Search(Csrq("rand", minRP, minRP, T, T, c), null, numDocs).scoreDocs;
            Assert.AreEqual(1, result.Length, "min,min,T,T,c");
            result = search.Search(Csrq("rand", null, minRP, F, T, c), null, numDocs).scoreDocs;
            Assert.AreEqual(1, result.Length, "nul,min,F,T,c");

            result = search.Search(Csrq("rand", maxRP, maxRP, T, T, c), null, numDocs).scoreDocs;
            Assert.AreEqual(1, result.Length, "max,max,T,T,c");
            result = search.Search(Csrq("rand", maxRP, null, T, F, c), null, numDocs).scoreDocs;
            Assert.AreEqual(1, result.Length, "max,nul,T,T,c");
        }

        [Test]
        public void TestFarsi()
        {

            /* build an index */
            RAMDirectory farsiIndex = new RAMDirectory();
            IndexWriter writer = new IndexWriter(farsiIndex, new SimpleAnalyzer(), T,
                                                 IndexWriter.MaxFieldLength.LIMITED);
            Document doc = new Document();
            doc.Add(new Field("content", "\u0633\u0627\u0628",
                              Field.Store.YES, Field.Index.NOT_ANALYZED));
            doc.Add(new Field("body", "body",
                              Field.Store.YES, Field.Index.NOT_ANALYZED));
            writer.AddDocument(doc);

            writer.Optimize();
            writer.Close();

            IndexReader reader = IndexReader.Open(farsiIndex);
            IndexSearcher search = new IndexSearcher(reader);

            // Neither Java 1.4.2 nor 1.5.0 has Farsi Locale collation available in
            // RuleBasedCollator.  However, the Arabic Locale seems to order the Farsi
            // characters properly.
            System.Globalization.CompareInfo c = System.Globalization.CultureInfo.GetCultureInfo("ar").CompareInfo;

            // Unicode order would include U+0633 in [ U+062F - U+0698 ], but Farsi
            // orders the U+0698 character before the U+0633 character, so the single
            // index Term below should NOT be returned by a ConstantScoreRangeQuery
            // with a Farsi Collator (or an Arabic one for the case when Farsi is 
            // not supported).
            ScoreDoc[] result = search.Search(Csrq("content", "\u062F", "\u0698", T, T, c), null, 1000).scoreDocs;
            Assert.AreEqual(0, result.Length, "The index Term should not be included.");

            result = search.Search(Csrq("content", "\u0633", "\u0638", T, T, c), null, 1000).scoreDocs;
            Assert.AreEqual(1, result.Length, "The index Term should be included.");
            search.Close();
        }
    }
}