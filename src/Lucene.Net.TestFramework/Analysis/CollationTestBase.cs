using J2N.Threading;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.TestFramework;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Assert = Lucene.Net.TestFramework.Assert;
using Directory = Lucene.Net.Store.Directory;

namespace Lucene.Net.Analysis
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
    /// Base test class for testing Unicode collation.
    /// </summary>
    public abstract class CollationTestBase : LuceneTestCase
    {
        protected string m_firstRangeBeginningOriginal = "\u062F";
        protected string m_firstRangeEndOriginal = "\u0698";

        protected string m_secondRangeBeginningOriginal = "\u0633";
        protected string m_secondRangeEndOriginal = "\u0638";

        // LUCENENET: The all locales may are not available for collation.
        // LUCENENET: Removed this (only) reference to the ICU library, since it has a lot of data and we don't
        // want to unnecessarily reference it in all test projects. 
        //protected readonly string[] availableCollationLocales = RuleBasedCollator.GetAvailableCollationLocales().ToArray();

        /// <summary>
        /// Convenience method to perform the same function as CollationKeyFilter.
        /// </summary>
        /// <param name="keyBits"> the result from
        /// <c>collator.GetCollationKey(original).ToByteArray()</c></param>
        /// <returns> The encoded collation key for the original string.</returns>
        [Obsolete("only for testing deprecated filters")]
        protected virtual string EncodeCollationKey(byte[] keyBits)
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
            using Directory dir = NewDirectory();
            using (IndexWriter writer = new IndexWriter(dir, new IndexWriterConfig(LuceneTestCase.TEST_VERSION_CURRENT, analyzer)))
            {
                Document doc = new Document();
                doc.Add(new TextField("content", "\u0633\u0627\u0628", Field.Store.YES));
                doc.Add(new StringField("body", "body", Field.Store.YES));
                writer.AddDocument(doc);
            } // writer.Dispose();
            using IndexReader reader = DirectoryReader.Open(dir);
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
        }

        public virtual void TestFarsiRangeQueryCollating(Analyzer analyzer, BytesRef firstBeg, BytesRef firstEnd, BytesRef secondBeg, BytesRef secondEnd)
        {
            using Directory dir = NewDirectory();
            using (IndexWriter writer = new IndexWriter(dir, new IndexWriterConfig(LuceneTestCase.TEST_VERSION_CURRENT, analyzer)))
            {
                Document doc = new Document();

                // Unicode order would include U+0633 in [ U+062F - U+0698 ], but Farsi
                // orders the U+0698 character before the U+0633 character, so the single
                // index Term below should NOT be returned by a TermRangeQuery with a Farsi
                // Collator (or an Arabic one for the case when Farsi is not supported).
                doc.Add(new TextField("content", "\u0633\u0627\u0628", Field.Store.YES));
                writer.AddDocument(doc);
            } // writer.Dispose();
            using IndexReader reader = DirectoryReader.Open(dir);
            IndexSearcher searcher = new IndexSearcher(reader);

            Search.Query query = new TermRangeQuery("content", firstBeg, firstEnd, true, true);
            ScoreDoc[] hits = searcher.Search(query, null, 1000).ScoreDocs;
            Assert.AreEqual(0, hits.Length, "The index Term should not be included.");

            query = new TermRangeQuery("content", secondBeg, secondEnd, true, true);
            hits = searcher.Search(query, null, 1000).ScoreDocs;
            Assert.AreEqual(1, hits.Length, "The index Term should be included.");
        }

        public virtual void TestFarsiTermRangeQuery(Analyzer analyzer, BytesRef firstBeg, BytesRef firstEnd, BytesRef secondBeg, BytesRef secondEnd)
        {
            using Directory farsiIndex = NewDirectory();
            using (IndexWriter writer = new IndexWriter(farsiIndex, new IndexWriterConfig(LuceneTestCase.TEST_VERSION_CURRENT, analyzer)))
            {
                Document doc = new Document();
                doc.Add(new TextField("content", "\u0633\u0627\u0628", Field.Store.YES));
                doc.Add(new StringField("body", "body", Field.Store.YES));
                writer.AddDocument(doc);
            } // writer.Dispose();

            using IndexReader reader = DirectoryReader.Open(farsiIndex);
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
        }

        /// <summary>
        /// Test using various international locales with accented characters (which
        /// sort differently depending on locale).
        /// </summary>

        // Copied (and slightly modified) from
        // Lucene.Net.Search.TestSort.TestInternationalSort()
        //
        // TODO: this test is really fragile. there are already 3 different cases,
        // depending upon unicode version.
        public virtual void TestCollationKeySort(Analyzer usAnalyzer, 
                                                Analyzer franceAnalyzer, 
                                                Analyzer swedenAnalyzer, 
                                                Analyzer denmarkAnalyzer, 
                                                string usResult, 
                                                string frResult, 
                                                string svResult, 
                                                string dkResult)
        {
            using Directory indexStore = NewDirectory();
            using (IndexWriter writer = new IndexWriter(indexStore, new IndexWriterConfig(LuceneTestCase.TEST_VERSION_CURRENT, new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false))))
            {
                // document data:
                // the tracer field is used to determine which document was hit
                string[][] sortData = new string[][] { new string[] { "A", "x", "p\u00EAche", "p\u00EAche", "p\u00EAche", "p\u00EAche" }, new string[] { "B", "y", "HAT", "HAT", "HAT", "HAT" }, new string[] { "C", "x", "p\u00E9ch\u00E9", "p\u00E9ch\u00E9", "p\u00E9ch\u00E9", "p\u00E9ch\u00E9" }, new string[] { "D", "y", "HUT", "HUT", "HUT", "HUT" }, new string[] { "E", "x", "peach", "peach", "peach", "peach" }, new string[] { "F", "y", "H\u00C5T", "H\u00C5T", "H\u00C5T", "H\u00C5T" }, new string[] { "G", "x", "sin", "sin", "sin", "sin" }, new string[] { "H", "y", "H\u00D8T", "H\u00D8T", "H\u00D8T", "H\u00D8T" }, new string[] { "I", "x", "s\u00EDn", "s\u00EDn", "s\u00EDn", "s\u00EDn" }, new string[] { "J", "y", "HOT", "HOT", "HOT", "HOT" } };

                FieldType customType = new FieldType();
                customType.IsStored = true;

                for (int i = 0; i < sortData.Length; ++i)
                {
                    Document doc = new Document();
                    doc.Add(new Field("tracer", sortData[i][0], customType));
                    doc.Add(new TextField("contents", sortData[i][1], Field.Store.NO));
                    if (sortData[i][2] != null)
                    {
                        doc.Add(new TextField("US", usAnalyzer.GetTokenStream("US", new StringReader(sortData[i][2]))));
                    }
                    if (sortData[i][3] != null)
                    {
                        doc.Add(new TextField("France", franceAnalyzer.GetTokenStream("France", new StringReader(sortData[i][3]))));
                    }
                    if (sortData[i][4] != null)
                    {
                        doc.Add(new TextField("Sweden", swedenAnalyzer.GetTokenStream("Sweden", new StringReader(sortData[i][4]))));
                    }
                    if (sortData[i][5] != null)
                    {
                        doc.Add(new TextField("Denmark", denmarkAnalyzer.GetTokenStream("Denmark", new StringReader(sortData[i][5]))));
                    }
                    writer.AddDocument(doc);
                }
                writer.ForceMerge(1);
            } // writer.Dispose();
            using IndexReader reader = DirectoryReader.Open(indexStore);
            IndexSearcher searcher = new IndexSearcher(reader);

            Sort sort = new Sort();
            Search.Query queryX = new TermQuery(new Term("contents", "x"));
            Search.Query queryY = new TermQuery(new Term("contents", "y"));

            sort.SetSort(new SortField("US", SortFieldType.STRING));
            AssertMatches(searcher, queryY, sort, usResult);

            sort.SetSort(new SortField("France", SortFieldType.STRING));
            AssertMatches(searcher, queryX, sort, frResult);

            sort.SetSort(new SortField("Sweden", SortFieldType.STRING));
            AssertMatches(searcher, queryY, sort, svResult);

            sort.SetSort(new SortField("Denmark", SortFieldType.STRING));
            AssertMatches(searcher, queryY, sort, dkResult);
        }

        /// <summary>
        /// Make sure the documents returned by the search match the expected list
        /// </summary>
        // Copied from TestSort.java
        private static void AssertMatches(IndexSearcher searcher, Search.Query query, Sort sort, string expectedResult) // LUCENENET: CA1822: Mark members as static
        {
            ScoreDoc[] result = searcher.Search(query, null, 1000, sort).ScoreDocs;
            StringBuilder buff = new StringBuilder(10);
            int n = result.Length;
            for (int i = 0; i < n; ++i)
            {
                Document doc = searcher.Doc(result[i].Doc);
                IIndexableField[] v = doc.GetFields("tracer");
                for (var j = 0; j < v.Length; ++j)
                {
                    buff.Append(v[j].GetStringValue());
                }
            }
            Assert.AreEqual(expectedResult, buff.ToString());
        }

        public virtual void AssertThreadSafe(Analyzer analyzer)
        {
            int numTestPoints = 100;
            int numThreads = TestUtil.NextInt32(Random, 3, 5);
            Dictionary<string, BytesRef> map = new Dictionary<string, BytesRef>();

            // create a map<String,SortKey> up front.
            // then with multiple threads, generate sort keys for all the keys in the map
            // and ensure they are the same as the ones we produced in serial fashion.

            for (int i = 0; i < numTestPoints; i++)
            {
                string term = TestUtil.RandomSimpleString(Random);
                Exception priorException = null; // LUCENENET: No need to cast to IOExcpetion
                TokenStream ts = analyzer.GetTokenStream("fake", new StringReader(term));
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
                catch (Exception e) when (e.IsIOException())
                {
                    priorException = e;
                }
                finally
                {
                    IOUtils.DisposeWhileHandlingException(priorException, ts);
                }
            }

            ThreadJob[] threads = new ThreadJob[numThreads];
            for (int i = 0; i < numThreads; i++)
            {
                threads[i] = new ThreadAnonymousClass(analyzer, map);
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

        private sealed class ThreadAnonymousClass : ThreadJob
        {
            private readonly Analyzer analyzer;
            private readonly IDictionary<string, BytesRef> map;

            public ThreadAnonymousClass(Analyzer analyzer, IDictionary<string, BytesRef> map)
            {
                this.analyzer = analyzer;
                this.map = map;
            }

            public override void Run()
            {
                try
                {
                    foreach (var mapping in this.map)
                    {
                        string term = mapping.Key;
                        BytesRef expected = mapping.Value;
                        Exception priorException = null; // LUCENENET: No need to cast to IOExcpetion
                        TokenStream ts = this.analyzer.GetTokenStream("fake", new StringReader(term));
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
                        catch (Exception e) when (e.IsIOException())
                        {
                            priorException = e;
                        }
                        finally
                        {
                            IOUtils.DisposeWhileHandlingException(priorException, ts);
                        }
                    }
                }
                catch (Exception e) when (e.IsIOException())
                {
                    throw RuntimeException.Create(e);
                }
            }
        }
    }
}