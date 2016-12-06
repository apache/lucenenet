namespace Lucene.Net.Search
{
    using NUnit.Framework;

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

    using IndexReader = Lucene.Net.Index.IndexReader;
    using Term = Lucene.Net.Index.Term;

    /// <summary>
    /// A basic 'positive' Unit test class for the TermRangeFilter class.
    ///
    /// <p>
    /// NOTE: at the moment, this class only tests for 'positive' results, it does
    /// not verify the results to ensure there are no 'false positives', nor does it
    /// adequately test 'negative' results. It also does not test that garbage in
    /// results in an Exception.
    /// </summary>
    [TestFixture]
    public class TestTermRangeFilter : BaseTestRangeFilter
    {
        [Test]
        public virtual void TestRangeFilterId()
        {
            IndexReader reader = SignedIndexReader;
            IndexSearcher search = NewSearcher(reader);

            int medId = ((MaxId - MinId) / 2);

            string minIP = Pad(MinId);
            string maxIP = Pad(MaxId);
            string medIP = Pad(medId);

            int numDocs = reader.NumDocs;

            Assert.AreEqual(numDocs, 1 + MaxId - MinId, "num of docs");

            ScoreDoc[] result;
            Query q = new TermQuery(new Term("body", "body"));

            // test id, bounded on both ends

            result = search.Search(q, TermRangeFilter.NewStringRange("id", minIP, maxIP, T, T), numDocs).ScoreDocs;
            Assert.AreEqual(numDocs, result.Length, "find all");

            result = search.Search(q, TermRangeFilter.NewStringRange("id", minIP, maxIP, T, F), numDocs).ScoreDocs;
            Assert.AreEqual(numDocs - 1, result.Length, "all but last");

            result = search.Search(q, TermRangeFilter.NewStringRange("id", minIP, maxIP, F, T), numDocs).ScoreDocs;
            Assert.AreEqual(numDocs - 1, result.Length, "all but first");

            result = search.Search(q, TermRangeFilter.NewStringRange("id", minIP, maxIP, F, F), numDocs).ScoreDocs;
            Assert.AreEqual(numDocs - 2, result.Length, "all but ends");

            result = search.Search(q, TermRangeFilter.NewStringRange("id", medIP, maxIP, T, T), numDocs).ScoreDocs;
            Assert.AreEqual(1 + MaxId - medId, result.Length, "med and up");

            result = search.Search(q, TermRangeFilter.NewStringRange("id", minIP, medIP, T, T), numDocs).ScoreDocs;
            Assert.AreEqual(1 + medId - MinId, result.Length, "up to med");

            // unbounded id

            result = search.Search(q, TermRangeFilter.NewStringRange("id", minIP, null, T, F), numDocs).ScoreDocs;
            Assert.AreEqual(numDocs, result.Length, "min and up");

            result = search.Search(q, TermRangeFilter.NewStringRange("id", null, maxIP, F, T), numDocs).ScoreDocs;
            Assert.AreEqual(numDocs, result.Length, "max and down");

            result = search.Search(q, TermRangeFilter.NewStringRange("id", minIP, null, F, F), numDocs).ScoreDocs;
            Assert.AreEqual(numDocs - 1, result.Length, "not min, but up");

            result = search.Search(q, TermRangeFilter.NewStringRange("id", null, maxIP, F, F), numDocs).ScoreDocs;
            Assert.AreEqual(numDocs - 1, result.Length, "not max, but down");

            result = search.Search(q, TermRangeFilter.NewStringRange("id", medIP, maxIP, T, F), numDocs).ScoreDocs;
            Assert.AreEqual(MaxId - medId, result.Length, "med and up, not max");

            result = search.Search(q, TermRangeFilter.NewStringRange("id", minIP, medIP, F, T), numDocs).ScoreDocs;
            Assert.AreEqual(medId - MinId, result.Length, "not min, up to med");

            // very small sets

            result = search.Search(q, TermRangeFilter.NewStringRange("id", minIP, minIP, F, F), numDocs).ScoreDocs;
            Assert.AreEqual(0, result.Length, "min,min,F,F");
            result = search.Search(q, TermRangeFilter.NewStringRange("id", medIP, medIP, F, F), numDocs).ScoreDocs;
            Assert.AreEqual(0, result.Length, "med,med,F,F");
            result = search.Search(q, TermRangeFilter.NewStringRange("id", maxIP, maxIP, F, F), numDocs).ScoreDocs;
            Assert.AreEqual(0, result.Length, "max,max,F,F");

            result = search.Search(q, TermRangeFilter.NewStringRange("id", minIP, minIP, T, T), numDocs).ScoreDocs;
            Assert.AreEqual(1, result.Length, "min,min,T,T");
            result = search.Search(q, TermRangeFilter.NewStringRange("id", null, minIP, F, T), numDocs).ScoreDocs;
            Assert.AreEqual(1, result.Length, "nul,min,F,T");

            result = search.Search(q, TermRangeFilter.NewStringRange("id", maxIP, maxIP, T, T), numDocs).ScoreDocs;
            Assert.AreEqual(1, result.Length, "max,max,T,T");
            result = search.Search(q, TermRangeFilter.NewStringRange("id", maxIP, null, T, F), numDocs).ScoreDocs;
            Assert.AreEqual(1, result.Length, "max,nul,T,T");

            result = search.Search(q, TermRangeFilter.NewStringRange("id", medIP, medIP, T, T), numDocs).ScoreDocs;
            Assert.AreEqual(1, result.Length, "med,med,T,T");
        }

        [Test]
        public virtual void TestRangeFilterRand()
        {
            IndexReader reader = SignedIndexReader;
            IndexSearcher search = NewSearcher(reader);

            string minRP = Pad(SignedIndexDir.MinR);
            string maxRP = Pad(SignedIndexDir.MaxR);

            int numDocs = reader.NumDocs;

            Assert.AreEqual(numDocs, 1 + MaxId - MinId, "num of docs");

            ScoreDoc[] result;
            Query q = new TermQuery(new Term("body", "body"));

            // test extremes, bounded on both ends

            result = search.Search(q, TermRangeFilter.NewStringRange("rand", minRP, maxRP, T, T), numDocs).ScoreDocs;
            Assert.AreEqual(numDocs, result.Length, "find all");

            result = search.Search(q, TermRangeFilter.NewStringRange("rand", minRP, maxRP, T, F), numDocs).ScoreDocs;
            Assert.AreEqual(numDocs - 1, result.Length, "all but biggest");

            result = search.Search(q, TermRangeFilter.NewStringRange("rand", minRP, maxRP, F, T), numDocs).ScoreDocs;
            Assert.AreEqual(numDocs - 1, result.Length, "all but smallest");

            result = search.Search(q, TermRangeFilter.NewStringRange("rand", minRP, maxRP, F, F), numDocs).ScoreDocs;
            Assert.AreEqual(numDocs - 2, result.Length, "all but extremes");

            // unbounded

            result = search.Search(q, TermRangeFilter.NewStringRange("rand", minRP, null, T, F), numDocs).ScoreDocs;
            Assert.AreEqual(numDocs, result.Length, "smallest and up");

            result = search.Search(q, TermRangeFilter.NewStringRange("rand", null, maxRP, F, T), numDocs).ScoreDocs;
            Assert.AreEqual(numDocs, result.Length, "biggest and down");

            result = search.Search(q, TermRangeFilter.NewStringRange("rand", minRP, null, F, F), numDocs).ScoreDocs;
            Assert.AreEqual(numDocs - 1, result.Length, "not smallest, but up");

            result = search.Search(q, TermRangeFilter.NewStringRange("rand", null, maxRP, F, F), numDocs).ScoreDocs;
            Assert.AreEqual(numDocs - 1, result.Length, "not biggest, but down");

            // very small sets

            result = search.Search(q, TermRangeFilter.NewStringRange("rand", minRP, minRP, F, F), numDocs).ScoreDocs;
            Assert.AreEqual(0, result.Length, "min,min,F,F");
            result = search.Search(q, TermRangeFilter.NewStringRange("rand", maxRP, maxRP, F, F), numDocs).ScoreDocs;
            Assert.AreEqual(0, result.Length, "max,max,F,F");

            result = search.Search(q, TermRangeFilter.NewStringRange("rand", minRP, minRP, T, T), numDocs).ScoreDocs;
            Assert.AreEqual(1, result.Length, "min,min,T,T");
            result = search.Search(q, TermRangeFilter.NewStringRange("rand", null, minRP, F, T), numDocs).ScoreDocs;
            Assert.AreEqual(1, result.Length, "nul,min,F,T");

            result = search.Search(q, TermRangeFilter.NewStringRange("rand", maxRP, maxRP, T, T), numDocs).ScoreDocs;
            Assert.AreEqual(1, result.Length, "max,max,T,T");
            result = search.Search(q, TermRangeFilter.NewStringRange("rand", maxRP, null, T, F), numDocs).ScoreDocs;
            Assert.AreEqual(1, result.Length, "max,nul,T,T");
        }


        #region SorterTestBase
        // LUCENENET NOTE: Tests in a base class are not pulled into the correct
        // context in Visual Studio. This fixes that with the minimum amount of code necessary
        // to run them in the correct context without duplicating all of the tests.

        [Test]
        public override void TestPad()
        {
            base.TestPad();
        }

        #endregion
    }
}