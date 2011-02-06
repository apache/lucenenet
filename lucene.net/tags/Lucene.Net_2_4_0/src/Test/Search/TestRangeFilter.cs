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

using IndexReader = Lucene.Net.Index.IndexReader;
using Term = Lucene.Net.Index.Term;
using IndexWriter = Lucene.Net.Index.IndexWriter;
using SimpleAnalyzer = Lucene.Net.Analysis.SimpleAnalyzer;
using Document = Lucene.Net.Documents.Document;
using Field = Lucene.Net.Documents.Field;
using RAMDirectory = Lucene.Net.Store.RAMDirectory;

namespace Lucene.Net.Search
{
    /// <summary> A basic 'positive' Unit test class for the RangeFilter class.
    /// 
    /// <p>
    /// NOTE: at the moment, this class only tests for 'positive' results,
    /// it does not verify the results to ensure there are no 'false positives',
    /// nor does it adequately test 'negative' results.  It also does not test
    /// that garbage in results in an Exception.
    /// </summary>
    [TestFixture]
    public class TestRangeFilter : BaseTestRangeFilter
    {

        [Test]
        public void TestRangeFilterId()
        {

            IndexReader reader = IndexReader.Open(signedIndex.index);
            IndexSearcher search = new IndexSearcher(reader);

            int medId = ((maxId - minId) / 2);

            string minIP = Pad(minId);
            string maxIP = Pad(maxId);
            string medIP = Pad(medId);

            int numDocs = reader.NumDocs();

            Assert.AreEqual(numDocs, 1 + maxId - minId, "num of docs");

            ScoreDoc[] result;
            Query q = new TermQuery(new Term("body", "body"));

            // test id, bounded on both ends

            result = search.Search(q, new RangeFilter("id", minIP, maxIP, T, T), numDocs).scoreDocs;
            Assert.AreEqual(numDocs, result.Length, "find all");

            result = search.Search(q, new RangeFilter("id", minIP, maxIP, T, F), numDocs).scoreDocs;
            Assert.AreEqual(numDocs - 1, result.Length, "all but last");

            result = search.Search(q, new RangeFilter("id", minIP, maxIP, F, T), numDocs).scoreDocs;
            Assert.AreEqual(numDocs - 1, result.Length, "all but first");

            result = search.Search(q, new RangeFilter("id", minIP, maxIP, F, F), numDocs).scoreDocs;
            Assert.AreEqual(numDocs - 2, result.Length, "all but ends");

            result = search.Search(q, new RangeFilter("id", medIP, maxIP, T, T), numDocs).scoreDocs;
            Assert.AreEqual(1 + maxId - medId, result.Length, "med and up");

            result = search.Search(q, new RangeFilter("id", minIP, medIP, T, T), numDocs).scoreDocs;
            Assert.AreEqual(1 + medId - minId, result.Length, "up to med");

            // unbounded id

            result = search.Search(q, new RangeFilter("id", minIP, null, T, F), numDocs).scoreDocs;
            Assert.AreEqual(numDocs, result.Length, "min and up");

            result = search.Search(q, new RangeFilter("id", null, maxIP, F, T), numDocs).scoreDocs;
            Assert.AreEqual(numDocs, result.Length, "max and down");

            result = search.Search(q, new RangeFilter("id", minIP, null, F, F), numDocs).scoreDocs;
            Assert.AreEqual(numDocs - 1, result.Length, "not min, but up");

            result = search.Search(q, new RangeFilter("id", null, maxIP, F, F), numDocs).scoreDocs;
            Assert.AreEqual(numDocs - 1, result.Length, "not max, but down");

            result = search.Search(q, new RangeFilter("id", medIP, maxIP, T, F), numDocs).scoreDocs;
            Assert.AreEqual(maxId - medId, result.Length, "med and up, not max");

            result = search.Search(q, new RangeFilter("id", minIP, medIP, F, T), numDocs).scoreDocs;
            Assert.AreEqual(medId - minId, result.Length, "not min, up to med");

            // very small sets

            result = search.Search(q, new RangeFilter("id", minIP, minIP, F, F), numDocs).scoreDocs;
            Assert.AreEqual(0, result.Length, "min,min,F,F");
            result = search.Search(q, new RangeFilter("id", medIP, medIP, F, F), numDocs).scoreDocs;
            Assert.AreEqual(0, result.Length, "med,med,F,F");
            result = search.Search(q, new RangeFilter("id", maxIP, maxIP, F, F), numDocs).scoreDocs;
            Assert.AreEqual(0, result.Length, "max,max,F,F");

            result = search.Search(q, new RangeFilter("id", minIP, minIP, T, T), numDocs).scoreDocs;
            Assert.AreEqual(1, result.Length, "min,min,T,T");
            result = search.Search(q, new RangeFilter("id", null, minIP, F, T), numDocs).scoreDocs;
            Assert.AreEqual(1, result.Length, "nul,min,F,T");

            result = search.Search(q, new RangeFilter("id", maxIP, maxIP, T, T), numDocs).scoreDocs;
            Assert.AreEqual(1, result.Length, "max,max,T,T");
            result = search.Search(q, new RangeFilter("id", maxIP, null, T, F), numDocs).scoreDocs;
            Assert.AreEqual(1, result.Length, "max,nul,T,T");

            result = search.Search(q, new RangeFilter("id", medIP, medIP, T, T), numDocs).scoreDocs;
            Assert.AreEqual(1, result.Length, "med,med,T,T");

        }

        [Test]
        public void TestRangeFilterIdCollating()
        {

            IndexReader reader = IndexReader.Open(signedIndex.index);
            IndexSearcher search = new IndexSearcher(reader);

            System.Globalization.CompareInfo c = System.Globalization.CultureInfo.GetCultureInfo("en-us").CompareInfo;

            int medId = ((maxId - minId) / 2);

            string minIP = Pad(minId);
            string maxIP = Pad(maxId);
            string medIP = Pad(medId);

            int numDocs = reader.NumDocs();

            Assert.AreEqual(numDocs, 1 + maxId - minId, "num of docs");

            Hits result;
            Query q = new TermQuery(new Term("body", "body"));

            // test id, bounded on both ends

            result = search.Search(q, new RangeFilter("id", minIP, maxIP, T, T, c));
            Assert.AreEqual(numDocs, result.Length(), "find all");

            result = search.Search(q, new RangeFilter("id", minIP, maxIP, T, F, c));
            Assert.AreEqual(numDocs - 1, result.Length(), "all but last");

            result = search.Search(q, new RangeFilter("id", minIP, maxIP, F, T, c));
            Assert.AreEqual(numDocs - 1, result.Length(), "all but first");

            result = search.Search(q, new RangeFilter("id", minIP, maxIP, F, F, c));
            Assert.AreEqual(numDocs - 2, result.Length(), "all but ends");

            result = search.Search(q, new RangeFilter("id", medIP, maxIP, T, T, c));
            Assert.AreEqual(1 + maxId - medId, result.Length(), "med and up");

            result = search.Search(q, new RangeFilter("id", minIP, medIP, T, T, c));
            Assert.AreEqual(1 + medId - minId, result.Length(), "up to med");

            // unbounded id

            result = search.Search(q, new RangeFilter("id", minIP, null, T, F, c));
            Assert.AreEqual(numDocs, result.Length(), "min and up");

            result = search.Search(q, new RangeFilter("id", null, maxIP, F, T, c));
            Assert.AreEqual(numDocs, result.Length(), "max and down");

            result = search.Search(q, new RangeFilter("id", minIP, null, F, F, c));
            Assert.AreEqual(numDocs - 1, result.Length(), "not min, but up");

            result = search.Search(q, new RangeFilter("id", null, maxIP, F, F, c));
            Assert.AreEqual(numDocs - 1, result.Length(), "not max, but down");

            result = search.Search(q, new RangeFilter("id", medIP, maxIP, T, F, c));
            Assert.AreEqual(maxId - medId, result.Length(), "med and up, not max");

            result = search.Search(q, new RangeFilter("id", minIP, medIP, F, T, c));
            Assert.AreEqual(medId - minId, result.Length(), "not min, up to med");

            // very small sets

            result = search.Search(q, new RangeFilter("id", minIP, minIP, F, F, c));
            Assert.AreEqual(0, result.Length(), "min,min,F,F");
            result = search.Search(q, new RangeFilter("id", medIP, medIP, F, F, c));
            Assert.AreEqual(0, result.Length(), "med,med,F,F");
            result = search.Search(q, new RangeFilter("id", maxIP, maxIP, F, F, c));
            Assert.AreEqual(0, result.Length(), "max,max,F,F");

            result = search.Search(q, new RangeFilter("id", minIP, minIP, T, T, c));
            Assert.AreEqual(1, result.Length(), "min,min,T,T");
            result = search.Search(q, new RangeFilter("id", null, minIP, F, T, c));
            Assert.AreEqual(1, result.Length(), "nul,min,F,T");

            result = search.Search(q, new RangeFilter("id", maxIP, maxIP, T, T, c));
            Assert.AreEqual(1, result.Length(), "max,max,T,T");
            result = search.Search(q, new RangeFilter("id", maxIP, null, T, F, c));
            Assert.AreEqual(1, result.Length(), "max,nul,T,T");

            result = search.Search(q, new RangeFilter("id", medIP, medIP, T, T, c));
            Assert.AreEqual(1, result.Length(), "med,med,T,T");
        }

        [Test]
        public void TestRangeFilterRand()
        {

            IndexReader reader = IndexReader.Open(signedIndex.index);
            IndexSearcher search = new IndexSearcher(reader);

            string minRP = Pad(signedIndex.minR);
            string maxRP = Pad(signedIndex.maxR);

            int numDocs = reader.NumDocs();

            Assert.AreEqual(numDocs, 1 + maxId - minId, "num of docs");

            ScoreDoc[] result;
            Query q = new TermQuery(new Term("body", "body"));

            // test extremes, bounded on both ends

            result = search.Search(q, new RangeFilter("rand", minRP, maxRP, T, T), numDocs).scoreDocs;
            Assert.AreEqual(numDocs, result.Length, "find all");

            result = search.Search(q, new RangeFilter("rand", minRP, maxRP, T, F), numDocs).scoreDocs;
            Assert.AreEqual(numDocs - 1, result.Length, "all but biggest");

            result = search.Search(q, new RangeFilter("rand", minRP, maxRP, F, T), numDocs).scoreDocs;
            Assert.AreEqual(numDocs - 1, result.Length, "all but smallest");

            result = search.Search(q, new RangeFilter("rand", minRP, maxRP, F, F), numDocs).scoreDocs;
            Assert.AreEqual(numDocs - 2, result.Length, "all but extremes");

            // unbounded

            result = search.Search(q, new RangeFilter("rand", minRP, null, T, F), numDocs).scoreDocs;
            Assert.AreEqual(numDocs, result.Length, "smallest and up");

            result = search.Search(q, new RangeFilter("rand", null, maxRP, F, T), numDocs).scoreDocs;
            Assert.AreEqual(numDocs, result.Length, "biggest and down");

            result = search.Search(q, new RangeFilter("rand", minRP, null, F, F), numDocs).scoreDocs;
            Assert.AreEqual(numDocs - 1, result.Length, "not smallest, but up");

            result = search.Search(q, new RangeFilter("rand", null, maxRP, F, F), numDocs).scoreDocs;
            Assert.AreEqual(numDocs - 1, result.Length, "not biggest, but down");

            // very small sets

            result = search.Search(q, new RangeFilter("rand", minRP, minRP, F, F), numDocs).scoreDocs;
            Assert.AreEqual(0, result.Length, "min,min,F,F");
            result = search.Search(q, new RangeFilter("rand", maxRP, maxRP, F, F), numDocs).scoreDocs;
            Assert.AreEqual(0, result.Length, "max,max,F,F");

            result = search.Search(q, new RangeFilter("rand", minRP, minRP, T, T), numDocs).scoreDocs;
            Assert.AreEqual(1, result.Length, "min,min,T,T");
            result = search.Search(q, new RangeFilter("rand", null, minRP, F, T), numDocs).scoreDocs;
            Assert.AreEqual(1, result.Length, "nul,min,F,T");

            result = search.Search(q, new RangeFilter("rand", maxRP, maxRP, T, T), numDocs).scoreDocs;
            Assert.AreEqual(1, result.Length, "max,max,T,T");
            result = search.Search(q, new RangeFilter("rand", maxRP, null, T, F), numDocs).scoreDocs;
            Assert.AreEqual(1, result.Length, "max,nul,T,T");

        }

        [Test]
        public void TestRangeFilterRandCollating()
        {

            // using the unsigned index because collation seems to ignore hyphens
            IndexReader reader = IndexReader.Open(unsignedIndex.index);
            IndexSearcher search = new IndexSearcher(reader);

            System.Globalization.CompareInfo c = System.Globalization.CultureInfo.GetCultureInfo("en-us").CompareInfo;

            string minRP = Pad(unsignedIndex.minR);
            string maxRP = Pad(unsignedIndex.maxR);

            int numDocs = reader.NumDocs();

            Assert.AreEqual(numDocs, 1 + maxId - minId, "num of docs");

            Hits result;
            Query q = new TermQuery(new Term("body", "body"));

            // test extremes, bounded on both ends

            result = search.Search(q, new RangeFilter("rand", minRP, maxRP, T, T, c));
            Assert.AreEqual(numDocs, result.Length(), "find all");

            result = search.Search(q, new RangeFilter("rand", minRP, maxRP, T, F, c));
            Assert.AreEqual(numDocs - 1, result.Length(), "all but biggest");

            result = search.Search(q, new RangeFilter("rand", minRP, maxRP, F, T, c));
            Assert.AreEqual(numDocs - 1, result.Length(), "all but smallest");

            result = search.Search(q, new RangeFilter("rand", minRP, maxRP, F, F, c));
            Assert.AreEqual(numDocs - 2, result.Length(), "all but extremes");

            // unbounded

            result = search.Search(q, new RangeFilter("rand", minRP, null, T, F, c));
            Assert.AreEqual(numDocs, result.Length(), "smallest and up");

            result = search.Search(q, new RangeFilter("rand", null, maxRP, F, T, c));
            Assert.AreEqual(numDocs, result.Length(), "biggest and down");

            result = search.Search(q, new RangeFilter("rand", minRP, null, F, F, c));
            Assert.AreEqual(numDocs - 1, result.Length(), "not smallest, but up");

            result = search.Search(q, new RangeFilter("rand", null, maxRP, F, F, c));
            Assert.AreEqual(numDocs - 1, result.Length(), "not biggest, but down");

            // very small sets

            result = search.Search(q, new RangeFilter("rand", minRP, minRP, F, F, c));
            Assert.AreEqual(0, result.Length(), "min,min,F,F");
            result = search.Search(q, new RangeFilter("rand", maxRP, maxRP, F, F, c));
            Assert.AreEqual(0, result.Length(), "max,max,F,F");

            result = search.Search(q, new RangeFilter("rand", minRP, minRP, T, T, c));
            Assert.AreEqual(1, result.Length(), "min,min,T,T");
            result = search.Search(q, new RangeFilter("rand", null, minRP, F, T, c));
            Assert.AreEqual(1, result.Length(), "nul,min,F,T");

            result = search.Search(q, new RangeFilter("rand", maxRP, maxRP, T, T, c));
            Assert.AreEqual(1, result.Length(), "max,max,T,T");
            result = search.Search(q, new RangeFilter("rand", maxRP, null, T, F, c));
            Assert.AreEqual(1, result.Length(), "max,nul,T,T");
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
                              Field.Store.YES, Field.Index.UN_TOKENIZED));
            doc.Add(new Field("body", "body",
                              Field.Store.YES, Field.Index.UN_TOKENIZED));
            writer.AddDocument(doc);

            writer.Optimize();
            writer.Close();

            IndexReader reader = IndexReader.Open(farsiIndex);
            IndexSearcher search = new IndexSearcher(reader);
            Query q = new TermQuery(new Term("body", "body"));

            // Neither Java 1.4.2 nor 1.5.0 has Farsi Locale collation available in
            // RuleBasedCollator.  However, the Arabic Locale seems to order the Farsi
            // characters properly.
            System.Globalization.CompareInfo collator = System.Globalization.CultureInfo.GetCultureInfo("ar").CompareInfo;

            // Unicode order would include U+0633 in [ U+062F - U+0698 ], but Farsi
            // orders the U+0698 character before the U+0633 character, so the single
            // index Term below should NOT be returned by a RangeFilter with a Farsi
            // Collator (or an Arabic one for the case when Farsi is not supported).
            Hits result = search.Search
                (q, new RangeFilter("content", "\u062F", "\u0698", T, T, collator));
            Assert.AreEqual(0, result.Length(), "The index Term should not be included.");

            result = search.Search
                (q, new RangeFilter("content", "\u0633", "\u0638", T, T, collator));
            Assert.AreEqual(1, result.Length(), "The index Term should be included.");
            search.Close();
        }
    }
}