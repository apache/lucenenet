using Lucene.Net.Analysis.Tokenattributes;
using System;
using System.Collections.Generic;
using System.Text;
using Lucene.Net.Documents;

namespace Lucene.Net.Analysis
{
    using Lucene.Net.Support;
    using NUnit.Framework;
    using System.IO;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using Directory = Lucene.Net.Store.Directory;
    using DirectoryReader = Lucene.Net.Index.DirectoryReader;

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
    using FieldType = FieldType;
    using IndexableBinaryStringTools = Lucene.Net.Util.IndexableBinaryStringTools;
    using IndexableField = Lucene.Net.Index.IndexableField;
    using IndexReader = Lucene.Net.Index.IndexReader;
    using IndexSearcher = Lucene.Net.Search.IndexSearcher;
    using IndexWriter = Lucene.Net.Index.IndexWriter;
    using IndexWriterConfig = Lucene.Net.Index.IndexWriterConfig;
    using IOUtils = Lucene.Net.Util.IOUtils;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using Query = Lucene.Net.Search.Query;
    using ScoreDoc = Lucene.Net.Search.ScoreDoc;
    using Sort = Lucene.Net.Search.Sort;
    using SortField = Lucene.Net.Search.SortField;
    using StringField = StringField;
    using Term = Lucene.Net.Index.Term;
    using TermQuery = Lucene.Net.Search.TermQuery;
    using TermRangeFilter = Lucene.Net.Search.TermRangeFilter;
    using TermRangeQuery = Lucene.Net.Search.TermRangeQuery;
    using TestUtil = Lucene.Net.Util.TestUtil;
    using TextField = TextField;

    /// <summary>
    /// base test class for testing Unicode collation.
    /// </summary>
    public abstract class CollationTestbase : LuceneTestCase
    {
        protected internal string FirstRangeBeginningOriginal = "\u062F";
        protected internal string FirstRangeEndOriginal = "\u0698";

        protected internal string SecondRangeBeginningOriginal = "\u0633";
        protected internal string SecondRangeEndOriginal = "\u0638";

        /// <summary>
        /// Convenience method to perform the same function as CollationKeyFilter.
        /// </summary>
        /// <param name="keyBits"> the result from
        ///  collator.getCollationKey(original).toByteArray() </param>
        /// <returns> The encoded collation key for the original String </returns>
        /// @deprecated only for testing deprecated filters
        [Obsolete("only for testing deprecated filters")]
        protected internal virtual string EncodeCollationKey(sbyte[] keyBits)
        {
            // Ensure that the backing char[] array is large enough to hold the encoded
            // Binary String
            int encodedLength = IndexableBinaryStringTools.GetEncodedLength(keyBits, 0, keyBits.Length);
            char[] encodedBegArray = new char[encodedLength];
            IndexableBinaryStringTools.Encode(keyBits, 0, keyBits.Length, encodedBegArray, 0, encodedLength);
            return new string(encodedBegArray);
        }

        public virtual void TestFarsiRangeFilterCollating(Analyzer analyzer, BytesRef firstBeg, BytesRef firstEnd, BytesRef secondBeg, BytesRef secondEnd)
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, analyzer));
            Document doc = new Document();
            doc.Add(new TextField("content", "\u0633\u0627\u0628", Field.Store.YES));
            doc.Add(new StringField("body", "body", Field.Store.YES));
            writer.AddDocument(doc);
            writer.Dispose();
            IndexReader reader = DirectoryReader.Open(dir);
            IndexSearcher searcher = new IndexSearcher(reader);
            Search.Query query = new TermQuery(new Term("body", "body"));

            // Unicode order would include U+0633 in [ U+062F - U+0698 ], but Farsi
            // orders the U+0698 character before the U+0633 character, so the single
            // index Term below should NOT be returned by a TermRangeFilter with a Farsi
            // Collator (or an Arabic one for the case when Farsi searcher not
            // supported).
            ScoreDoc[] result = searcher.Search(query, new TermRangeFilter("content", firstBeg, firstEnd, true, true), 1).ScoreDocs;
            Assert.AreEqual(0, result.Length, "The index Term should not be included.");

            result = searcher.Search(query, new TermRangeFilter("content", secondBeg, secondEnd, true, true), 1).ScoreDocs;
            Assert.AreEqual(1, result.Length, "The index Term should be included.");

            reader.Dispose();
            dir.Dispose();
        }

        public virtual void TestFarsiRangeQueryCollating(Analyzer analyzer, BytesRef firstBeg, BytesRef firstEnd, BytesRef secondBeg, BytesRef secondEnd)
        {
            Directory dir = NewDirectory();
            IndexWriter writer = new IndexWriter(dir, new IndexWriterConfig(TEST_VERSION_CURRENT, analyzer));
            Document doc = new Document();

            // Unicode order would include U+0633 in [ U+062F - U+0698 ], but Farsi
            // orders the U+0698 character before the U+0633 character, so the single
            // index Term below should NOT be returned by a TermRangeQuery with a Farsi
            // Collator (or an Arabic one for the case when Farsi is not supported).
            doc.Add(new TextField("content", "\u0633\u0627\u0628", Field.Store.YES));
            writer.AddDocument(doc);
            writer.Dispose();
            IndexReader reader = DirectoryReader.Open(dir);
            IndexSearcher searcher = new IndexSearcher(reader);

            Search.Query query = new TermRangeQuery("content", firstBeg, firstEnd, true, true);
            ScoreDoc[] hits = searcher.Search(query, null, 1000).ScoreDocs;
            Assert.AreEqual(0, hits.Length, "The index Term should not be included.");

            query = new TermRangeQuery("content", secondBeg, secondEnd, true, true);
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            Assert.AreEqual(1, hits.Length, "The index Term should be included.");
            reader.Dispose();
            dir.Dispose();
        }

        public virtual void TestFarsiTermRangeQuery(Analyzer analyzer, BytesRef firstBeg, BytesRef firstEnd, BytesRef secondBeg, BytesRef secondEnd)
        {
            Directory farsiIndex = NewDirectory();
            IndexWriter writer = new IndexWriter(farsiIndex, new IndexWriterConfig(TEST_VERSION_CURRENT, analyzer));
            Document doc = new Document();
            doc.Add(new TextField("content", "\u0633\u0627\u0628", Field.Store.YES));
            doc.Add(new StringField("body", "body", Field.Store.YES));
            writer.AddDocument(doc);
            writer.Dispose();

            IndexReader reader = DirectoryReader.Open(farsiIndex);
            IndexSearcher search = NewSearcher(reader);

            // Unicode order would include U+0633 in [ U+062F - U+0698 ], but Farsi
            // orders the U+0698 character before the U+0633 character, so the single
            // index Term below should NOT be returned by a TermRangeQuery
            // with a Farsi Collator (or an Arabic one for the case when Farsi is
            // not supported).
            Search.Query csrq = new TermRangeQuery("content", firstBeg, firstEnd, true, true);
            ScoreDoc[] result = search.Search(csrq, null, 1000).ScoreDocs;
            Assert.AreEqual(0, result.Length, "The index Term should not be included.");

            csrq = new TermRangeQuery("content", secondBeg, secondEnd, true, true);
            result = search.Search(csrq, null, 1000).ScoreDocs;
            Assert.AreEqual(1, result.Length, "The index Term should be included.");
            reader.Dispose();
            farsiIndex.Dispose();
        }

        // Test using various international locales with accented characters (which
        // sort differently depending on locale)
        //
        // Copied (and slightly modified) from
        // Lucene.Net.Search.TestSort.testInternationalSort()
        //
        // TODO: this test is really fragile. there are already 3 different cases,
        // depending upon unicode version.
        public virtual void TestCollationKeySort(Analyzer usAnalyzer, Analyzer franceAnalyzer, Analyzer swedenAnalyzer, Analyzer denmarkAnalyzer, string usResult, string frResult, string svResult, string dkResult)
        {
            Directory indexStore = NewDirectory();
            IndexWriter writer = new IndexWriter(indexStore, new IndexWriterConfig(TEST_VERSION_CURRENT, new MockAnalyzer(Random(), MockTokenizer.WHITESPACE, false)));

            // document data:
            // the tracer field is used to determine which document was hit
            string[][] sortData = new string[][] { new string[] { "A", "x", "p\u00EAche", "p\u00EAche", "p\u00EAche", "p\u00EAche" }, new string[] { "B", "y", "HAT", "HAT", "HAT", "HAT" }, new string[] { "C", "x", "p\u00E9ch\u00E9", "p\u00E9ch\u00E9", "p\u00E9ch\u00E9", "p\u00E9ch\u00E9" }, new string[] { "D", "y", "HUT", "HUT", "HUT", "HUT" }, new string[] { "E", "x", "peach", "peach", "peach", "peach" }, new string[] { "F", "y", "H\u00C5T", "H\u00C5T", "H\u00C5T", "H\u00C5T" }, new string[] { "G", "x", "sin", "sin", "sin", "sin" }, new string[] { "H", "y", "H\u00D8T", "H\u00D8T", "H\u00D8T", "H\u00D8T" }, new string[] { "I", "x", "s\u00EDn", "s\u00EDn", "s\u00EDn", "s\u00EDn" }, new string[] { "J", "y", "HOT", "HOT", "HOT", "HOT" } };

            FieldType customType = new FieldType();
            customType.Stored = true;

            for (int i = 0; i < sortData.Length; ++i)
            {
                Document doc = new Document();
                doc.Add(new Field("tracer", sortData[i][0], customType));
                doc.Add(new TextField("contents", sortData[i][1], Field.Store.NO));
                if (sortData[i][2] != null)
                {
                    doc.Add(new TextField("US", usAnalyzer.TokenStream("US", new StringReader(sortData[i][2]))));
                }
                if (sortData[i][3] != null)
                {
                    doc.Add(new TextField("France", franceAnalyzer.TokenStream("France", new StringReader(sortData[i][3]))));
                }
                if (sortData[i][4] != null)
                {
                    doc.Add(new TextField("Sweden", swedenAnalyzer.TokenStream("Sweden", new StringReader(sortData[i][4]))));
                }
                if (sortData[i][5] != null)
                {
                    doc.Add(new TextField("Denmark", denmarkAnalyzer.TokenStream("Denmark", new StringReader(sortData[i][5]))));
                }
                writer.AddDocument(doc);
            }
            writer.ForceMerge(1);
            writer.Dispose();
            IndexReader reader = DirectoryReader.Open(indexStore);
            IndexSearcher searcher = new IndexSearcher(reader);

            Sort sort = new Sort();
            Search.Query queryX = new TermQuery(new Term("contents", "x"));
            Search.Query queryY = new TermQuery(new Term("contents", "y"));

            sort.SetSort(new SortField("US", SortField.Type_e.STRING));
            AssertMatches(searcher, queryY, sort, usResult);

            sort.SetSort(new SortField("France", SortField.Type_e.STRING));
            AssertMatches(searcher, queryX, sort, frResult);

            sort.SetSort(new SortField("Sweden", SortField.Type_e.STRING));
            AssertMatches(searcher, queryY, sort, svResult);

            sort.SetSort(new SortField("Denmark", SortField.Type_e.STRING));
            AssertMatches(searcher, queryY, sort, dkResult);
            reader.Dispose();
            indexStore.Dispose();
        }

        // Make sure the documents returned by the search match the expected list
        // Copied from TestSort.java
        private void AssertMatches(IndexSearcher searcher, Search.Query query, Sort sort, string expectedResult)
        {
            ScoreDoc[] result = searcher.Search(query, null, 1000, sort).ScoreDocs;
            StringBuilder buff = new StringBuilder(10);
            int n = result.Length;
            for (int i = 0; i < n; ++i)
            {
                Document doc = searcher.Doc(result[i].Doc);
                IndexableField[] v = doc.GetFields("tracer");
                for (int j = 0; j < v.Length; ++j)
                {
                    buff.Append(v[j].StringValue);
                }
            }
            Assert.AreEqual(expectedResult, buff.ToString());
        }

        public virtual void AssertThreadSafe(Analyzer analyzer)
        {
            int numTestPoints = 100;
            int numThreads = TestUtil.NextInt(Random(), 3, 5);
            Dictionary<string, BytesRef> map = new Dictionary<string, BytesRef>();

            // create a map<String,SortKey> up front.
            // then with multiple threads, generate sort keys for all the keys in the map
            // and ensure they are the same as the ones we produced in serial fashion.

            for (int i = 0; i < numTestPoints; i++)
            {
                string term = TestUtil.RandomSimpleString(Random());
                IOException priorException = null;
                TokenStream ts = analyzer.TokenStream("fake", new StringReader(term));
                try
                {
                    ITermToBytesRefAttribute termAtt = ts.AddAttribute<ITermToBytesRefAttribute>();
                    BytesRef bytes = termAtt.BytesRef;
                    ts.Reset();
                    Assert.IsTrue(ts.IncrementToken());
                    termAtt.FillBytesRef();
                    // ensure we make a copy of the actual bytes too
                    map[term] = BytesRef.DeepCopyOf(bytes);
                    Assert.IsFalse(ts.IncrementToken());
                    ts.End();
                }
                catch (IOException e)
                {
                    priorException = e;
                }
                finally
                {
                    IOUtils.CloseWhileHandlingException(priorException, ts);
                }
            }

            ThreadClass[] threads = new ThreadClass[numThreads];
            for (int i = 0; i < numThreads; i++)
            {
                threads[i] = new ThreadAnonymousInnerClassHelper(this, analyzer, map);
            }
            for (int i = 0; i < numThreads; i++)
            {
                threads[i].Start();
            }
            for (int i = 0; i < numThreads; i++)
            {
                threads[i].Join();
            }
        }

        private class ThreadAnonymousInnerClassHelper : ThreadClass
        {
            private readonly CollationTestbase OuterInstance;

            private Analyzer Analyzer;
            private Dictionary<string, BytesRef> Map;

            public ThreadAnonymousInnerClassHelper(CollationTestbase outerInstance, Analyzer analyzer, Dictionary<string, BytesRef> map)
            {
                this.OuterInstance = outerInstance;
                this.Analyzer = analyzer;
                this.Map = map;
            }

            public override void Run()
            {
                try
                {
                    foreach (KeyValuePair<string, BytesRef> mapping in Map)
                    {
                        string term = mapping.Key;
                        BytesRef expected = mapping.Value;
                        IOException priorException = null;
                        TokenStream ts = Analyzer.TokenStream("fake", new StringReader(term));
                        try
                        {
                            ITermToBytesRefAttribute termAtt = ts.AddAttribute<ITermToBytesRefAttribute>();
                            BytesRef bytes = termAtt.BytesRef;
                            ts.Reset();
                            Assert.IsTrue(ts.IncrementToken());
                            termAtt.FillBytesRef();
                            Assert.AreEqual(expected, bytes);
                            Assert.IsFalse(ts.IncrementToken());
                            ts.End();
                        }
                        catch (IOException e)
                        {
                            priorException = e;
                        }
                        finally
                        {
                            IOUtils.CloseWhileHandlingException(priorException, ts);
                        }
                    }
                }
                catch (IOException e)
                {
                    throw (Exception)e;
                }
            }
        }
    }
}