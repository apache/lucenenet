using Lucene.Net.Documents;
using NUnit.Framework;
using System;
using System.Globalization;
using System.IO;
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

    using Directory = Lucene.Net.Store.Directory;
    using DirectoryReader = Lucene.Net.Index.DirectoryReader;
    using Document = Documents.Document;
    using Field = Field;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using IndexWriter = Lucene.Net.Index.IndexWriter;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using Term = Lucene.Net.Index.Term;

    /// <summary>
    /// A basic 'positive' Unit test class for the FieldCacheRangeFilter class.
    ///
    /// <p>
    /// NOTE: at the moment, this class only tests for 'positive' results,
    /// it does not verify the results to ensure there are no 'false positives',
    /// nor does it adequately test 'negative' results.  It also does not test
    /// that garbage in results in an Exception.
    /// </summary>
    [TestFixture]
    public class TestFieldCacheRangeFilter : BaseTestRangeFilter
    {
        /// <summary>
        /// LUCENENET specific. Ensure we have an infostream attached to the default FieldCache
        /// when running the tests. In Java, this was done in the Core.Search.TestFieldCache.TestInfoStream() 
        /// method (which polluted the state of these tests), but we need to make the tests self-contained 
        /// so they can be run correctly regardless of order. Not setting the InfoStream skips an execution
        /// path within these tests, so we should do it to make sure we test all of the code.
        /// </summary>
        public override void SetUp()
        {
            base.SetUp();
            FieldCache.DEFAULT.InfoStream = new StringWriter();
        }

        /// <summary>
        /// LUCENENET specific. See <see cref="SetUp()"/>. Dispose our InfoStream and set it to null
        /// to avoid polluting the state of other tests.
        /// </summary>
        public override void TearDown()
        {
            FieldCache.DEFAULT.InfoStream.Dispose();
            FieldCache.DEFAULT.InfoStream = null;
            base.TearDown();
        }

        [Test]
        public virtual void TestRangeFilterId()
        {
            IndexReader reader = signedIndexReader;
            IndexSearcher search = NewSearcher(reader);

            int medId = ((maxId - minId) / 2);

            string minIP = Pad(minId);
            string maxIP = Pad(maxId);
            string medIP = Pad(medId);

            int numDocs = reader.NumDocs;

            Assert.AreEqual(numDocs, 1 + maxId - minId, "num of docs");

            ScoreDoc[] result;
            Query q = new TermQuery(new Term("body", "body"));

            // test id, bounded on both ends
            result = search.Search(q, FieldCacheRangeFilter.NewStringRange("id", minIP, maxIP, T, T), numDocs).ScoreDocs;
            Assert.AreEqual(numDocs, result.Length, "find all");

            result = search.Search(q, FieldCacheRangeFilter.NewStringRange("id", minIP, maxIP, T, F), numDocs).ScoreDocs;
            Assert.AreEqual(numDocs - 1, result.Length, "all but last");

            result = search.Search(q, FieldCacheRangeFilter.NewStringRange("id", minIP, maxIP, F, T), numDocs).ScoreDocs;
            Assert.AreEqual(numDocs - 1, result.Length, "all but first");

            result = search.Search(q, FieldCacheRangeFilter.NewStringRange("id", minIP, maxIP, F, F), numDocs).ScoreDocs;
            Assert.AreEqual(numDocs - 2, result.Length, "all but ends");

            result = search.Search(q, FieldCacheRangeFilter.NewStringRange("id", medIP, maxIP, T, T), numDocs).ScoreDocs;
            Assert.AreEqual(1 + maxId - medId, result.Length, "med and up");

            result = search.Search(q, FieldCacheRangeFilter.NewStringRange("id", minIP, medIP, T, T), numDocs).ScoreDocs;
            Assert.AreEqual(1 + medId - minId, result.Length, "up to med");

            // unbounded id

            result = search.Search(q, FieldCacheRangeFilter.NewStringRange("id", null, null, T, T), numDocs).ScoreDocs;
            Assert.AreEqual(numDocs, result.Length, "find all");

            result = search.Search(q, FieldCacheRangeFilter.NewStringRange("id", minIP, null, T, F), numDocs).ScoreDocs;
            Assert.AreEqual(numDocs, result.Length, "min and up");

            result = search.Search(q, FieldCacheRangeFilter.NewStringRange("id", null, maxIP, F, T), numDocs).ScoreDocs;
            Assert.AreEqual(numDocs, result.Length, "max and down");

            result = search.Search(q, FieldCacheRangeFilter.NewStringRange("id", minIP, null, F, F), numDocs).ScoreDocs;
            Assert.AreEqual(numDocs - 1, result.Length, "not min, but up");

            result = search.Search(q, FieldCacheRangeFilter.NewStringRange("id", null, maxIP, F, F), numDocs).ScoreDocs;
            Assert.AreEqual(numDocs - 1, result.Length, "not max, but down");

            result = search.Search(q, FieldCacheRangeFilter.NewStringRange("id", medIP, maxIP, T, F), numDocs).ScoreDocs;
            Assert.AreEqual(maxId - medId, result.Length, "med and up, not max");

            result = search.Search(q, FieldCacheRangeFilter.NewStringRange("id", minIP, medIP, F, T), numDocs).ScoreDocs;
            Assert.AreEqual(medId - minId, result.Length, "not min, up to med");

            // very small sets

            result = search.Search(q, FieldCacheRangeFilter.NewStringRange("id", minIP, minIP, F, F), numDocs).ScoreDocs;
            Assert.AreEqual(0, result.Length, "min,min,F,F");
            result = search.Search(q, FieldCacheRangeFilter.NewStringRange("id", medIP, medIP, F, F), numDocs).ScoreDocs;
            Assert.AreEqual(0, result.Length, "med,med,F,F");
            result = search.Search(q, FieldCacheRangeFilter.NewStringRange("id", maxIP, maxIP, F, F), numDocs).ScoreDocs;
            Assert.AreEqual(0, result.Length, "max,max,F,F");

            result = search.Search(q, FieldCacheRangeFilter.NewStringRange("id", minIP, minIP, T, T), numDocs).ScoreDocs;
            Assert.AreEqual(1, result.Length, "min,min,T,T");
            result = search.Search(q, FieldCacheRangeFilter.NewStringRange("id", null, minIP, F, T), numDocs).ScoreDocs;
            Assert.AreEqual(1, result.Length, "nul,min,F,T");

            result = search.Search(q, FieldCacheRangeFilter.NewStringRange("id", maxIP, maxIP, T, T), numDocs).ScoreDocs;
            Assert.AreEqual(1, result.Length, "max,max,T,T");
            result = search.Search(q, FieldCacheRangeFilter.NewStringRange("id", maxIP, null, T, F), numDocs).ScoreDocs;
            Assert.AreEqual(1, result.Length, "max,nul,T,T");

            result = search.Search(q, FieldCacheRangeFilter.NewStringRange("id", medIP, medIP, T, T), numDocs).ScoreDocs;
            Assert.AreEqual(1, result.Length, "med,med,T,T");
        }

        [Test]
        public virtual void TestFieldCacheRangeFilterRand()
        {
            IndexReader reader = signedIndexReader;
            IndexSearcher search = NewSearcher(reader);

            string minRP = Pad(signedIndexDir.minR);
            string maxRP = Pad(signedIndexDir.maxR);

            int numDocs = reader.NumDocs;

            Assert.AreEqual(numDocs, 1 + maxId - minId, "num of docs");

            ScoreDoc[] result;
            Query q = new TermQuery(new Term("body", "body"));

            // test extremes, bounded on both ends

            result = search.Search(q, FieldCacheRangeFilter.NewStringRange("rand", minRP, maxRP, T, T), numDocs).ScoreDocs;
            Assert.AreEqual(numDocs, result.Length, "find all");

            result = search.Search(q, FieldCacheRangeFilter.NewStringRange("rand", minRP, maxRP, T, F), numDocs).ScoreDocs;
            Assert.AreEqual(numDocs - 1, result.Length, "all but biggest");

            result = search.Search(q, FieldCacheRangeFilter.NewStringRange("rand", minRP, maxRP, F, T), numDocs).ScoreDocs;
            Assert.AreEqual(numDocs - 1, result.Length, "all but smallest");

            result = search.Search(q, FieldCacheRangeFilter.NewStringRange("rand", minRP, maxRP, F, F), numDocs).ScoreDocs;
            Assert.AreEqual(numDocs - 2, result.Length, "all but extremes");

            // unbounded

            result = search.Search(q, FieldCacheRangeFilter.NewStringRange("rand", minRP, null, T, F), numDocs).ScoreDocs;
            Assert.AreEqual(numDocs, result.Length, "smallest and up");

            result = search.Search(q, FieldCacheRangeFilter.NewStringRange("rand", null, maxRP, F, T), numDocs).ScoreDocs;
            Assert.AreEqual(numDocs, result.Length, "biggest and down");

            result = search.Search(q, FieldCacheRangeFilter.NewStringRange("rand", minRP, null, F, F), numDocs).ScoreDocs;
            Assert.AreEqual(numDocs - 1, result.Length, "not smallest, but up");

            result = search.Search(q, FieldCacheRangeFilter.NewStringRange("rand", null, maxRP, F, F), numDocs).ScoreDocs;
            Assert.AreEqual(numDocs - 1, result.Length, "not biggest, but down");

            // very small sets

            result = search.Search(q, FieldCacheRangeFilter.NewStringRange("rand", minRP, minRP, F, F), numDocs).ScoreDocs;
            Assert.AreEqual(0, result.Length, "min,min,F,F");
            result = search.Search(q, FieldCacheRangeFilter.NewStringRange("rand", maxRP, maxRP, F, F), numDocs).ScoreDocs;
            Assert.AreEqual(0, result.Length, "max,max,F,F");

            result = search.Search(q, FieldCacheRangeFilter.NewStringRange("rand", minRP, minRP, T, T), numDocs).ScoreDocs;
            Assert.AreEqual(1, result.Length, "min,min,T,T");
            result = search.Search(q, FieldCacheRangeFilter.NewStringRange("rand", null, minRP, F, T), numDocs).ScoreDocs;
            Assert.AreEqual(1, result.Length, "nul,min,F,T");

            result = search.Search(q, FieldCacheRangeFilter.NewStringRange("rand", maxRP, maxRP, T, T), numDocs).ScoreDocs;
            Assert.AreEqual(1, result.Length, "max,max,T,T");
            result = search.Search(q, FieldCacheRangeFilter.NewStringRange("rand", maxRP, null, T, F), numDocs).ScoreDocs;
            Assert.AreEqual(1, result.Length, "max,nul,T,T");
        }

        // byte-ranges cannot be tested, because all ranges are too big for bytes, need an extra range for that

        [Test]
        public virtual void TestFieldCacheRangeFilterShorts()
        {
            IndexReader reader = signedIndexReader;
            IndexSearcher search = NewSearcher(reader);

            int numDocs = reader.NumDocs;
            int medId = ((maxId - minId) / 2);
            short minIdO = Convert.ToInt16((short)minId);
            short maxIdO = Convert.ToInt16((short)maxId);
            short medIdO = Convert.ToInt16((short)medId);

            Assert.AreEqual(numDocs, 1 + maxId - minId, "num of docs");

            ScoreDoc[] result;
            Query q = new TermQuery(new Term("body", "body"));

#pragma warning disable 612, 618
            // test id, bounded on both ends
            result = search.Search(q, FieldCacheRangeFilter.NewInt16Range("id", minIdO, maxIdO, T, T), numDocs).ScoreDocs;
            Assert.AreEqual(numDocs, result.Length, "find all");

            result = search.Search(q, FieldCacheRangeFilter.NewInt16Range("id", minIdO, maxIdO, T, F), numDocs).ScoreDocs;
            Assert.AreEqual(numDocs - 1, result.Length, "all but last");

            result = search.Search(q, FieldCacheRangeFilter.NewInt16Range("id", minIdO, maxIdO, F, T), numDocs).ScoreDocs;
            Assert.AreEqual(numDocs - 1, result.Length, "all but first");

            result = search.Search(q, FieldCacheRangeFilter.NewInt16Range("id", minIdO, maxIdO, F, F), numDocs).ScoreDocs;
            Assert.AreEqual(numDocs - 2, result.Length, "all but ends");

            result = search.Search(q, FieldCacheRangeFilter.NewInt16Range("id", medIdO, maxIdO, T, T), numDocs).ScoreDocs;
            Assert.AreEqual(1 + maxId - medId, result.Length, "med and up");

            result = search.Search(q, FieldCacheRangeFilter.NewInt16Range("id", minIdO, medIdO, T, T), numDocs).ScoreDocs;
            Assert.AreEqual(1 + medId - minId, result.Length, "up to med");

            // unbounded id

            result = search.Search(q, FieldCacheRangeFilter.NewInt16Range("id", null, null, T, T), numDocs).ScoreDocs;
            Assert.AreEqual(numDocs, result.Length, "find all");

            result = search.Search(q, FieldCacheRangeFilter.NewInt16Range("id", minIdO, null, T, F), numDocs).ScoreDocs;
            Assert.AreEqual(numDocs, result.Length, "min and up");

            result = search.Search(q, FieldCacheRangeFilter.NewInt16Range("id", null, maxIdO, F, T), numDocs).ScoreDocs;
            Assert.AreEqual(numDocs, result.Length, "max and down");

            result = search.Search(q, FieldCacheRangeFilter.NewInt16Range("id", minIdO, null, F, F), numDocs).ScoreDocs;
            Assert.AreEqual(numDocs - 1, result.Length, "not min, but up");

            result = search.Search(q, FieldCacheRangeFilter.NewInt16Range("id", null, maxIdO, F, F), numDocs).ScoreDocs;
            Assert.AreEqual(numDocs - 1, result.Length, "not max, but down");

            result = search.Search(q, FieldCacheRangeFilter.NewInt16Range("id", medIdO, maxIdO, T, F), numDocs).ScoreDocs;
            Assert.AreEqual(maxId - medId, result.Length, "med and up, not max");

            result = search.Search(q, FieldCacheRangeFilter.NewInt16Range("id", minIdO, medIdO, F, T), numDocs).ScoreDocs;
            Assert.AreEqual(medId - minId, result.Length, "not min, up to med");

            // very small sets

            result = search.Search(q, FieldCacheRangeFilter.NewInt16Range("id", minIdO, minIdO, F, F), numDocs).ScoreDocs;
            Assert.AreEqual(0, result.Length, "min,min,F,F");
            result = search.Search(q, FieldCacheRangeFilter.NewInt16Range("id", medIdO, medIdO, F, F), numDocs).ScoreDocs;
            Assert.AreEqual(0, result.Length, "med,med,F,F");
            result = search.Search(q, FieldCacheRangeFilter.NewInt16Range("id", maxIdO, maxIdO, F, F), numDocs).ScoreDocs;
            Assert.AreEqual(0, result.Length, "max,max,F,F");

            result = search.Search(q, FieldCacheRangeFilter.NewInt16Range("id", minIdO, minIdO, T, T), numDocs).ScoreDocs;
            Assert.AreEqual(1, result.Length, "min,min,T,T");
            result = search.Search(q, FieldCacheRangeFilter.NewInt16Range("id", null, minIdO, F, T), numDocs).ScoreDocs;
            Assert.AreEqual(1, result.Length, "nul,min,F,T");

            result = search.Search(q, FieldCacheRangeFilter.NewInt16Range("id", maxIdO, maxIdO, T, T), numDocs).ScoreDocs;
            Assert.AreEqual(1, result.Length, "max,max,T,T");
            result = search.Search(q, FieldCacheRangeFilter.NewInt16Range("id", maxIdO, null, T, F), numDocs).ScoreDocs;
            Assert.AreEqual(1, result.Length, "max,nul,T,T");

            result = search.Search(q, FieldCacheRangeFilter.NewInt16Range("id", medIdO, medIdO, T, T), numDocs).ScoreDocs;
            Assert.AreEqual(1, result.Length, "med,med,T,T");

            // special cases
            result = search.Search(q, FieldCacheRangeFilter.NewInt16Range("id", Convert.ToInt16(short.MaxValue), null, F, F), numDocs).ScoreDocs;
            Assert.AreEqual(0, result.Length, "overflow special case");
            result = search.Search(q, FieldCacheRangeFilter.NewInt16Range("id", null, Convert.ToInt16(short.MinValue), F, F), numDocs).ScoreDocs;
            Assert.AreEqual(0, result.Length, "overflow special case");
            result = search.Search(q, FieldCacheRangeFilter.NewInt16Range("id", maxIdO, minIdO, T, T), numDocs).ScoreDocs;
            Assert.AreEqual(0, result.Length, "inverse range");
#pragma warning restore 612, 618
        }

        [Test]
        public virtual void TestFieldCacheRangeFilterInts()
        {
            IndexReader reader = signedIndexReader;
            IndexSearcher search = NewSearcher(reader);

            int numDocs = reader.NumDocs;
            int medId = ((maxId - minId) / 2);
            int minIdO = Convert.ToInt32(minId);
            int maxIdO = Convert.ToInt32(maxId);
            int medIdO = Convert.ToInt32(medId);

            Assert.AreEqual(numDocs, 1 + maxId - minId, "num of docs");

            ScoreDoc[] result;
            Query q = new TermQuery(new Term("body", "body"));

            // test id, bounded on both ends

            result = search.Search(q, FieldCacheRangeFilter.NewInt32Range("id", minIdO, maxIdO, T, T), numDocs).ScoreDocs;
            Assert.AreEqual(numDocs, result.Length, "find all");

            result = search.Search(q, FieldCacheRangeFilter.NewInt32Range("id", minIdO, maxIdO, T, F), numDocs).ScoreDocs;
            Assert.AreEqual(numDocs - 1, result.Length, "all but last");

            result = search.Search(q, FieldCacheRangeFilter.NewInt32Range("id", minIdO, maxIdO, F, T), numDocs).ScoreDocs;
            Assert.AreEqual(numDocs - 1, result.Length, "all but first");

            result = search.Search(q, FieldCacheRangeFilter.NewInt32Range("id", minIdO, maxIdO, F, F), numDocs).ScoreDocs;
            Assert.AreEqual(numDocs - 2, result.Length, "all but ends");

            result = search.Search(q, FieldCacheRangeFilter.NewInt32Range("id", medIdO, maxIdO, T, T), numDocs).ScoreDocs;
            Assert.AreEqual(1 + maxId - medId, result.Length, "med and up");

            result = search.Search(q, FieldCacheRangeFilter.NewInt32Range("id", minIdO, medIdO, T, T), numDocs).ScoreDocs;
            Assert.AreEqual(1 + medId - minId, result.Length, "up to med");

            // unbounded id

            result = search.Search(q, FieldCacheRangeFilter.NewInt32Range("id", null, null, T, T), numDocs).ScoreDocs;
            Assert.AreEqual(numDocs, result.Length, "find all");

            result = search.Search(q, FieldCacheRangeFilter.NewInt32Range("id", minIdO, null, T, F), numDocs).ScoreDocs;
            Assert.AreEqual(numDocs, result.Length, "min and up");

            result = search.Search(q, FieldCacheRangeFilter.NewInt32Range("id", null, maxIdO, F, T), numDocs).ScoreDocs;
            Assert.AreEqual(numDocs, result.Length, "max and down");

            result = search.Search(q, FieldCacheRangeFilter.NewInt32Range("id", minIdO, null, F, F), numDocs).ScoreDocs;
            Assert.AreEqual(numDocs - 1, result.Length, "not min, but up");

            result = search.Search(q, FieldCacheRangeFilter.NewInt32Range("id", null, maxIdO, F, F), numDocs).ScoreDocs;
            Assert.AreEqual(numDocs - 1, result.Length, "not max, but down");

            result = search.Search(q, FieldCacheRangeFilter.NewInt32Range("id", medIdO, maxIdO, T, F), numDocs).ScoreDocs;
            Assert.AreEqual(maxId - medId, result.Length, "med and up, not max");

            result = search.Search(q, FieldCacheRangeFilter.NewInt32Range("id", minIdO, medIdO, F, T), numDocs).ScoreDocs;
            Assert.AreEqual(medId - minId, result.Length, "not min, up to med");

            // very small sets

            result = search.Search(q, FieldCacheRangeFilter.NewInt32Range("id", minIdO, minIdO, F, F), numDocs).ScoreDocs;
            Assert.AreEqual(0, result.Length, "min,min,F,F");
            result = search.Search(q, FieldCacheRangeFilter.NewInt32Range("id", medIdO, medIdO, F, F), numDocs).ScoreDocs;
            Assert.AreEqual(0, result.Length, "med,med,F,F");
            result = search.Search(q, FieldCacheRangeFilter.NewInt32Range("id", maxIdO, maxIdO, F, F), numDocs).ScoreDocs;
            Assert.AreEqual(0, result.Length, "max,max,F,F");

            result = search.Search(q, FieldCacheRangeFilter.NewInt32Range("id", minIdO, minIdO, T, T), numDocs).ScoreDocs;
            Assert.AreEqual(1, result.Length, "min,min,T,T");
            result = search.Search(q, FieldCacheRangeFilter.NewInt32Range("id", null, minIdO, F, T), numDocs).ScoreDocs;
            Assert.AreEqual(1, result.Length, "nul,min,F,T");

            result = search.Search(q, FieldCacheRangeFilter.NewInt32Range("id", maxIdO, maxIdO, T, T), numDocs).ScoreDocs;
            Assert.AreEqual(1, result.Length, "max,max,T,T");
            result = search.Search(q, FieldCacheRangeFilter.NewInt32Range("id", maxIdO, null, T, F), numDocs).ScoreDocs;
            Assert.AreEqual(1, result.Length, "max,nul,T,T");

            result = search.Search(q, FieldCacheRangeFilter.NewInt32Range("id", medIdO, medIdO, T, T), numDocs).ScoreDocs;
            Assert.AreEqual(1, result.Length, "med,med,T,T");

            // special cases
            result = search.Search(q, FieldCacheRangeFilter.NewInt32Range("id", Convert.ToInt32(int.MaxValue), null, F, F), numDocs).ScoreDocs;
            Assert.AreEqual(0, result.Length, "overflow special case");
            result = search.Search(q, FieldCacheRangeFilter.NewInt32Range("id", null, Convert.ToInt32(int.MinValue), F, F), numDocs).ScoreDocs;
            Assert.AreEqual(0, result.Length, "overflow special case");
            result = search.Search(q, FieldCacheRangeFilter.NewInt32Range("id", maxIdO, minIdO, T, T), numDocs).ScoreDocs;
            Assert.AreEqual(0, result.Length, "inverse range");
        }

        [Test]
        public virtual void TestFieldCacheRangeFilterLongs()
        {
            IndexReader reader = signedIndexReader;
            IndexSearcher search = NewSearcher(reader);

            int numDocs = reader.NumDocs;
            int medId = ((maxId - minId) / 2);
            long minIdO = Convert.ToInt64(minId);
            long maxIdO = Convert.ToInt64(maxId);
            long medIdO = Convert.ToInt64(medId);

            Assert.AreEqual(numDocs, 1 + maxId - minId, "num of docs");

            ScoreDoc[] result;
            Query q = new TermQuery(new Term("body", "body"));

            // test id, bounded on both ends

            result = search.Search(q, FieldCacheRangeFilter.NewInt64Range("id", minIdO, maxIdO, T, T), numDocs).ScoreDocs;
            Assert.AreEqual(numDocs, result.Length, "find all");

            result = search.Search(q, FieldCacheRangeFilter.NewInt64Range("id", minIdO, maxIdO, T, F), numDocs).ScoreDocs;
            Assert.AreEqual(numDocs - 1, result.Length, "all but last");

            result = search.Search(q, FieldCacheRangeFilter.NewInt64Range("id", minIdO, maxIdO, F, T), numDocs).ScoreDocs;
            Assert.AreEqual(numDocs - 1, result.Length, "all but first");

            result = search.Search(q, FieldCacheRangeFilter.NewInt64Range("id", minIdO, maxIdO, F, F), numDocs).ScoreDocs;
            Assert.AreEqual(numDocs - 2, result.Length, "all but ends");

            result = search.Search(q, FieldCacheRangeFilter.NewInt64Range("id", medIdO, maxIdO, T, T), numDocs).ScoreDocs;
            Assert.AreEqual(1 + maxId - medId, result.Length, "med and up");

            result = search.Search(q, FieldCacheRangeFilter.NewInt64Range("id", minIdO, medIdO, T, T), numDocs).ScoreDocs;
            Assert.AreEqual(1 + medId - minId, result.Length, "up to med");

            // unbounded id

            result = search.Search(q, FieldCacheRangeFilter.NewInt64Range("id", null, null, T, T), numDocs).ScoreDocs;
            Assert.AreEqual(numDocs, result.Length, "find all");

            result = search.Search(q, FieldCacheRangeFilter.NewInt64Range("id", minIdO, null, T, F), numDocs).ScoreDocs;
            Assert.AreEqual(numDocs, result.Length, "min and up");

            result = search.Search(q, FieldCacheRangeFilter.NewInt64Range("id", null, maxIdO, F, T), numDocs).ScoreDocs;
            Assert.AreEqual(numDocs, result.Length, "max and down");

            result = search.Search(q, FieldCacheRangeFilter.NewInt64Range("id", minIdO, null, F, F), numDocs).ScoreDocs;
            Assert.AreEqual(numDocs - 1, result.Length, "not min, but up");

            result = search.Search(q, FieldCacheRangeFilter.NewInt64Range("id", null, maxIdO, F, F), numDocs).ScoreDocs;
            Assert.AreEqual(numDocs - 1, result.Length, "not max, but down");

            result = search.Search(q, FieldCacheRangeFilter.NewInt64Range("id", medIdO, maxIdO, T, F), numDocs).ScoreDocs;
            Assert.AreEqual(maxId - medId, result.Length, "med and up, not max");

            result = search.Search(q, FieldCacheRangeFilter.NewInt64Range("id", minIdO, medIdO, F, T), numDocs).ScoreDocs;
            Assert.AreEqual(medId - minId, result.Length, "not min, up to med");

            // very small sets

            result = search.Search(q, FieldCacheRangeFilter.NewInt64Range("id", minIdO, minIdO, F, F), numDocs).ScoreDocs;
            Assert.AreEqual(0, result.Length, "min,min,F,F");
            result = search.Search(q, FieldCacheRangeFilter.NewInt64Range("id", medIdO, medIdO, F, F), numDocs).ScoreDocs;
            Assert.AreEqual(0, result.Length, "med,med,F,F");
            result = search.Search(q, FieldCacheRangeFilter.NewInt64Range("id", maxIdO, maxIdO, F, F), numDocs).ScoreDocs;
            Assert.AreEqual(0, result.Length, "max,max,F,F");

            result = search.Search(q, FieldCacheRangeFilter.NewInt64Range("id", minIdO, minIdO, T, T), numDocs).ScoreDocs;
            Assert.AreEqual(1, result.Length, "min,min,T,T");
            result = search.Search(q, FieldCacheRangeFilter.NewInt64Range("id", null, minIdO, F, T), numDocs).ScoreDocs;
            Assert.AreEqual(1, result.Length, "nul,min,F,T");

            result = search.Search(q, FieldCacheRangeFilter.NewInt64Range("id", maxIdO, maxIdO, T, T), numDocs).ScoreDocs;
            Assert.AreEqual(1, result.Length, "max,max,T,T");
            result = search.Search(q, FieldCacheRangeFilter.NewInt64Range("id", maxIdO, null, T, F), numDocs).ScoreDocs;
            Assert.AreEqual(1, result.Length, "max,nul,T,T");

            result = search.Search(q, FieldCacheRangeFilter.NewInt64Range("id", medIdO, medIdO, T, T), numDocs).ScoreDocs;
            Assert.AreEqual(1, result.Length, "med,med,T,T");

            // special cases
            result = search.Search(q, FieldCacheRangeFilter.NewInt64Range("id", Convert.ToInt64(long.MaxValue), null, F, F), numDocs).ScoreDocs;
            Assert.AreEqual(0, result.Length, "overflow special case");
            result = search.Search(q, FieldCacheRangeFilter.NewInt64Range("id", null, Convert.ToInt64(long.MinValue), F, F), numDocs).ScoreDocs;
            Assert.AreEqual(0, result.Length, "overflow special case");
            result = search.Search(q, FieldCacheRangeFilter.NewInt64Range("id", maxIdO, minIdO, T, T), numDocs).ScoreDocs;
            Assert.AreEqual(0, result.Length, "inverse range");
        }

        // float and double tests are a bit minimalistic, but its complicated, because missing precision

        [Test]
        public virtual void TestFieldCacheRangeFilterFloats()
        {
            IndexReader reader = signedIndexReader;
            IndexSearcher search = NewSearcher(reader);

            int numDocs = reader.NumDocs;
            float minIdO = Convert.ToSingle(minId + .5f);
            float medIdO = Convert.ToSingle((float)minIdO + ((maxId - minId)) / 2.0f);

            ScoreDoc[] result;
            Query q = new TermQuery(new Term("body", "body"));

            result = search.Search(q, FieldCacheRangeFilter.NewSingleRange("id", minIdO, medIdO, T, T), numDocs).ScoreDocs;
            Assert.AreEqual(numDocs / 2, result.Length, "find all");
            int count = 0;
            result = search.Search(q, FieldCacheRangeFilter.NewSingleRange("id", null, medIdO, F, T), numDocs).ScoreDocs;
            count += result.Length;
            result = search.Search(q, FieldCacheRangeFilter.NewSingleRange("id", medIdO, null, F, F), numDocs).ScoreDocs;
            count += result.Length;
            Assert.AreEqual(numDocs, count, "sum of two concenatted ranges");
            result = search.Search(q, FieldCacheRangeFilter.NewSingleRange("id", null, null, T, T), numDocs).ScoreDocs;
            Assert.AreEqual(numDocs, result.Length, "find all");
            result = search.Search(q, FieldCacheRangeFilter.NewSingleRange("id", Convert.ToSingle(float.PositiveInfinity), null, F, F), numDocs).ScoreDocs;
            Assert.AreEqual(0, result.Length, "infinity special case");
            result = search.Search(q, FieldCacheRangeFilter.NewSingleRange("id", null, Convert.ToSingle(float.NegativeInfinity), F, F), numDocs).ScoreDocs;
            Assert.AreEqual(0, result.Length, "infinity special case");
        }

        [Test]
        public virtual void TestFieldCacheRangeFilterDoubles()
        {
            IndexReader reader = signedIndexReader;
            IndexSearcher search = NewSearcher(reader);

            int numDocs = reader.NumDocs;
            double minIdO = Convert.ToDouble(minId + .5);
            double medIdO = Convert.ToDouble((float)minIdO + ((maxId - minId)) / 2.0);

            ScoreDoc[] result;
            Query q = new TermQuery(new Term("body", "body"));

            result = search.Search(q, FieldCacheRangeFilter.NewDoubleRange("id", minIdO, medIdO, T, T), numDocs).ScoreDocs;
            Assert.AreEqual(numDocs / 2, result.Length, "find all");
            int count = 0;
            result = search.Search(q, FieldCacheRangeFilter.NewDoubleRange("id", null, medIdO, F, T), numDocs).ScoreDocs;
            count += result.Length;
            result = search.Search(q, FieldCacheRangeFilter.NewDoubleRange("id", medIdO, null, F, F), numDocs).ScoreDocs;
            count += result.Length;
            Assert.AreEqual(numDocs, count, "sum of two concenatted ranges");
            result = search.Search(q, FieldCacheRangeFilter.NewDoubleRange("id", null, null, T, T), numDocs).ScoreDocs;
            Assert.AreEqual(numDocs, result.Length, "find all");
            result = search.Search(q, FieldCacheRangeFilter.NewDoubleRange("id", Convert.ToDouble(double.PositiveInfinity), null, F, F), numDocs).ScoreDocs;
            Assert.AreEqual(0, result.Length, "infinity special case");
            result = search.Search(q, FieldCacheRangeFilter.NewDoubleRange("id", null, Convert.ToDouble(double.NegativeInfinity), F, F), numDocs).ScoreDocs;
            Assert.AreEqual(0, result.Length, "infinity special case");
        }

        // test using a sparse index (with deleted docs).
        [Test]
        public virtual void TestSparseIndex()
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, NewIndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random)));

            for (int d = -20; d <= 20; d++)
            {
                Document doc = new Document();
                doc.Add(NewStringField("id", Convert.ToString(d, CultureInfo.InvariantCulture), Field.Store.NO));
                doc.Add(NewStringField("body", "body", Field.Store.NO));
                writer.AddDocument(doc);
            }

            writer.ForceMerge(1);
            writer.DeleteDocuments(new Term("id", "0"));
            writer.Dispose();

            IndexReader reader = DirectoryReader.Open(dir);
            IndexSearcher search = NewSearcher(reader);
            Assert.IsTrue(reader.HasDeletions);

            ScoreDoc[] result;
            Query q = new TermQuery(new Term("body", "body"));

#pragma warning disable 612, 618
            result = search.Search(q, FieldCacheRangeFilter.NewByteRange("id", (sbyte?)-20, (sbyte?)20, T, T), 100).ScoreDocs;
            Assert.AreEqual(40, result.Length, "find all");

            result = search.Search(q, FieldCacheRangeFilter.NewByteRange("id", (sbyte?)0, (sbyte?)20, T, T), 100).ScoreDocs;
            Assert.AreEqual(20, result.Length, "find all");

            result = search.Search(q, FieldCacheRangeFilter.NewByteRange("id", (sbyte?)-20, (sbyte?)0, T, T), 100).ScoreDocs;
            Assert.AreEqual(20, result.Length, "find all");

            result = search.Search(q, FieldCacheRangeFilter.NewByteRange("id", (sbyte?)10, (sbyte?)20, T, T), 100).ScoreDocs;
            Assert.AreEqual(11, result.Length, "find all");

            result = search.Search(q, FieldCacheRangeFilter.NewByteRange("id", (sbyte?)-20, (sbyte?)-10, T, T), 100).ScoreDocs;
            Assert.AreEqual(11, result.Length, "find all");
#pragma warning restore 612, 618

            reader.Dispose();
            dir.Dispose();
        }
    }
}