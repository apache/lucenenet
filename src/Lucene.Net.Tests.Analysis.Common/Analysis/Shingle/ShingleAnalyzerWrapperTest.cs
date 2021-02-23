// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Analysis.Shingle
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
    /// A test class for ShingleAnalyzerWrapper as regards queries and scoring.
    /// </summary>
    public class ShingleAnalyzerWrapperTest : BaseTokenStreamTestCase
    {
        private Analyzer analyzer;
        private IndexSearcher searcher;
        private IndexReader reader;
        private Store.Directory directory;

        /// <summary>
        /// Set up a new index in RAM with three test phrases and the supplied Analyzer.
        /// </summary>
        /// <exception cref="Exception"> if an error occurs with index writer or searcher </exception>
        public override void SetUp()
        {
            base.SetUp();
            analyzer = new ShingleAnalyzerWrapper(new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false), 2);
            directory = NewDirectory();
            IndexWriter writer = new IndexWriter(directory, new IndexWriterConfig(TEST_VERSION_CURRENT, analyzer));

            Document doc;
            doc = new Document();
            doc.Add(new TextField("content", "please divide this sentence into shingles", Field.Store.YES));
            writer.AddDocument(doc);

            doc = new Document();
            doc.Add(new TextField("content", "just another test sentence", Field.Store.YES));
            writer.AddDocument(doc);

            doc = new Document();
            doc.Add(new TextField("content", "a sentence which contains no test", Field.Store.YES));
            writer.AddDocument(doc);

            writer.Dispose();

            reader = DirectoryReader.Open(directory);
            searcher = NewSearcher(reader);
        }

        public override void TearDown()
        {
            reader.Dispose();
            directory.Dispose();
            base.TearDown();
        }

        protected internal virtual void CompareRanks(ScoreDoc[] hits, int[] ranks)
        {
            assertEquals(ranks.Length, hits.Length);
            for (int i = 0; i < ranks.Length; i++)
            {
                assertEquals(ranks[i], hits[i].Doc);
            }
        }

        /*
         * This shows how to construct a phrase query containing shingles.
         */
        [Test]
        public virtual void TestShingleAnalyzerWrapperPhraseQuery()
        {
            PhraseQuery q = new PhraseQuery();

            TokenStream ts = analyzer.GetTokenStream("content", "this sentence");
            try
            {
                int j = -1;

                IPositionIncrementAttribute posIncrAtt = ts.AddAttribute<IPositionIncrementAttribute>();
                ICharTermAttribute termAtt = ts.AddAttribute<ICharTermAttribute>();

                ts.Reset();
                while (ts.IncrementToken())
                {
                    j += posIncrAtt.PositionIncrement;
                    string termText = termAtt.ToString();
                    q.Add(new Term("content", termText), j);
                }
                ts.End();
            }
            finally
            {
                IOUtils.DisposeWhileHandlingException(ts);
            }

            ScoreDoc[] hits = searcher.Search(q, null, 1000).ScoreDocs;
            int[] ranks = new int[] { 0 };
            CompareRanks(hits, ranks);
        }

        /*
         * How to construct a boolean query with shingles. A query like this will
         * implicitly score those documents higher that contain the words in the query
         * in the right order and adjacent to each other.
         */
        [Test]
        public virtual void TestShingleAnalyzerWrapperBooleanQuery()
        {
            BooleanQuery q = new BooleanQuery();

            TokenStream ts = analyzer.GetTokenStream("content", "test sentence");
            try
            {
                ICharTermAttribute termAtt = ts.AddAttribute<ICharTermAttribute>();

                ts.Reset();
                while (ts.IncrementToken())
                {
                    string termText = termAtt.ToString();
                    q.Add(new TermQuery(new Term("content", termText)), Occur.SHOULD);
                }
                ts.End();
            }
            finally
            {
                IOUtils.DisposeWhileHandlingException(ts);
            }

            ScoreDoc[] hits = searcher.Search(q, null, 1000).ScoreDocs;
            int[] ranks = new int[] { 1, 2, 0 };
            CompareRanks(hits, ranks);
        }

        [Test]
        public virtual void TestReusableTokenStream()
        {
            Analyzer a = new ShingleAnalyzerWrapper(new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false), 2);
            AssertAnalyzesTo(a, "please divide into shingles", new string[] { "please", "please divide", "divide", "divide into", "into", "into shingles", "shingles" }, new int[] { 0, 0, 7, 7, 14, 14, 19 }, new int[] { 6, 13, 13, 18, 18, 27, 27 }, new int[] { 1, 0, 1, 0, 1, 0, 1 });
            AssertAnalyzesTo(a, "divide me up again", new string[] { "divide", "divide me", "me", "me up", "up", "up again", "again" }, new int[] { 0, 0, 7, 7, 10, 10, 13 }, new int[] { 6, 9, 9, 12, 12, 18, 18 }, new int[] { 1, 0, 1, 0, 1, 0, 1 });
        }

        [Test]
        public virtual void TestNonDefaultMinShingleSize()
        {
            ShingleAnalyzerWrapper analyzer = new ShingleAnalyzerWrapper(new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false), 3, 4);
            AssertAnalyzesTo(analyzer, "please divide this sentence into shingles", new string[] { "please", "please divide this", "please divide this sentence", "divide", "divide this sentence", "divide this sentence into", "this", "this sentence into", "this sentence into shingles", "sentence", "sentence into shingles", "into", "shingles" }, new int[] { 0, 0, 0, 7, 7, 7, 14, 14, 14, 19, 19, 28, 33 }, new int[] { 6, 18, 27, 13, 27, 32, 18, 32, 41, 27, 41, 32, 41 }, new int[] { 1, 0, 0, 1, 0, 0, 1, 0, 0, 1, 0, 1, 1 });

            analyzer = new ShingleAnalyzerWrapper(new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false), 3, 4, ShingleFilter.DEFAULT_TOKEN_SEPARATOR, false, false, ShingleFilter.DEFAULT_FILLER_TOKEN);
            AssertAnalyzesTo(analyzer, "please divide this sentence into shingles", new string[] { "please divide this", "please divide this sentence", "divide this sentence", "divide this sentence into", "this sentence into", "this sentence into shingles", "sentence into shingles" }, new int[] { 0, 0, 7, 7, 14, 14, 19 }, new int[] { 18, 27, 27, 32, 32, 41, 41 }, new int[] { 1, 0, 1, 0, 1, 0, 1 });
        }

        [Test]
        public virtual void TestNonDefaultMinAndSameMaxShingleSize()
        {
            ShingleAnalyzerWrapper analyzer = new ShingleAnalyzerWrapper(new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false), 3, 3);
            AssertAnalyzesTo(analyzer, "please divide this sentence into shingles", new string[] { "please", "please divide this", "divide", "divide this sentence", "this", "this sentence into", "sentence", "sentence into shingles", "into", "shingles" }, new int[] { 0, 0, 7, 7, 14, 14, 19, 19, 28, 33 }, new int[] { 6, 18, 13, 27, 18, 32, 27, 41, 32, 41 }, new int[] { 1, 0, 1, 0, 1, 0, 1, 0, 1, 1 });

            analyzer = new ShingleAnalyzerWrapper(new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false), 3, 3, ShingleFilter.DEFAULT_TOKEN_SEPARATOR, false, false, ShingleFilter.DEFAULT_FILLER_TOKEN);
            AssertAnalyzesTo(analyzer, "please divide this sentence into shingles", new string[] { "please divide this", "divide this sentence", "this sentence into", "sentence into shingles" }, new int[] { 0, 7, 14, 19 }, new int[] { 18, 27, 32, 41 }, new int[] { 1, 1, 1, 1 });
        }

        [Test]
        public virtual void TestNoTokenSeparator()
        {
            ShingleAnalyzerWrapper analyzer = new ShingleAnalyzerWrapper(new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false), ShingleFilter.DEFAULT_MIN_SHINGLE_SIZE, ShingleFilter.DEFAULT_MAX_SHINGLE_SIZE, "", true, false, ShingleFilter.DEFAULT_FILLER_TOKEN);
            AssertAnalyzesTo(analyzer, "please divide into shingles", new string[] { "please", "pleasedivide", "divide", "divideinto", "into", "intoshingles", "shingles" }, new int[] { 0, 0, 7, 7, 14, 14, 19 }, new int[] { 6, 13, 13, 18, 18, 27, 27 }, new int[] { 1, 0, 1, 0, 1, 0, 1 });

            analyzer = new ShingleAnalyzerWrapper(new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false), ShingleFilter.DEFAULT_MIN_SHINGLE_SIZE, ShingleFilter.DEFAULT_MAX_SHINGLE_SIZE, "", false, false, ShingleFilter.DEFAULT_FILLER_TOKEN);
            AssertAnalyzesTo(analyzer, "please divide into shingles", new string[] { "pleasedivide", "divideinto", "intoshingles" }, new int[] { 0, 7, 14 }, new int[] { 13, 18, 27 }, new int[] { 1, 1, 1 });
        }

        [Test]
        public virtual void TestNullTokenSeparator()
        {
            ShingleAnalyzerWrapper analyzer = new ShingleAnalyzerWrapper(new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false), ShingleFilter.DEFAULT_MIN_SHINGLE_SIZE, ShingleFilter.DEFAULT_MAX_SHINGLE_SIZE, null, true, false, ShingleFilter.DEFAULT_FILLER_TOKEN);
            AssertAnalyzesTo(analyzer, "please divide into shingles", new string[] { "please", "pleasedivide", "divide", "divideinto", "into", "intoshingles", "shingles" }, new int[] { 0, 0, 7, 7, 14, 14, 19 }, new int[] { 6, 13, 13, 18, 18, 27, 27 }, new int[] { 1, 0, 1, 0, 1, 0, 1 });

            analyzer = new ShingleAnalyzerWrapper(new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false), ShingleFilter.DEFAULT_MIN_SHINGLE_SIZE, ShingleFilter.DEFAULT_MAX_SHINGLE_SIZE, "", false, false, ShingleFilter.DEFAULT_FILLER_TOKEN);
            AssertAnalyzesTo(analyzer, "please divide into shingles", new string[] { "pleasedivide", "divideinto", "intoshingles" }, new int[] { 0, 7, 14 }, new int[] { 13, 18, 27 }, new int[] { 1, 1, 1 });
        }

        [Test]
        public virtual void TestAltTokenSeparator()
        {
            ShingleAnalyzerWrapper analyzer = new ShingleAnalyzerWrapper(new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false), ShingleFilter.DEFAULT_MIN_SHINGLE_SIZE, ShingleFilter.DEFAULT_MAX_SHINGLE_SIZE, "<SEP>", true, false, ShingleFilter.DEFAULT_FILLER_TOKEN);
            AssertAnalyzesTo(analyzer, "please divide into shingles", new string[] { "please", "please<SEP>divide", "divide", "divide<SEP>into", "into", "into<SEP>shingles", "shingles" }, new int[] { 0, 0, 7, 7, 14, 14, 19 }, new int[] { 6, 13, 13, 18, 18, 27, 27 }, new int[] { 1, 0, 1, 0, 1, 0, 1 });

            analyzer = new ShingleAnalyzerWrapper(new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false), ShingleFilter.DEFAULT_MIN_SHINGLE_SIZE, ShingleFilter.DEFAULT_MAX_SHINGLE_SIZE, "<SEP>", false, false, ShingleFilter.DEFAULT_FILLER_TOKEN);
            AssertAnalyzesTo(analyzer, "please divide into shingles", new string[] { "please<SEP>divide", "divide<SEP>into", "into<SEP>shingles" }, new int[] { 0, 7, 14 }, new int[] { 13, 18, 27 }, new int[] { 1, 1, 1 });
        }

        [Test]
        public virtual void TestAltFillerToken()
        {
            Analyzer @delegate = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                CharArraySet stopSet = StopFilter.MakeStopSet(TEST_VERSION_CURRENT, "into");
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                TokenFilter filter = new StopFilter(TEST_VERSION_CURRENT, tokenizer, stopSet);
                return new TokenStreamComponents(tokenizer, filter);
            });

            ShingleAnalyzerWrapper analyzer = new ShingleAnalyzerWrapper(@delegate, ShingleFilter.DEFAULT_MIN_SHINGLE_SIZE, ShingleFilter.DEFAULT_MAX_SHINGLE_SIZE, ShingleFilter.DEFAULT_TOKEN_SEPARATOR, true, false, "--");
            AssertAnalyzesTo(analyzer, "please divide into shingles", new string[] { "please", "please divide", "divide", "divide --", "-- shingles", "shingles" }, new int[] { 0, 0, 7, 7, 19, 19 }, new int[] { 6, 13, 13, 19, 27, 27 }, new int[] { 1, 0, 1, 0, 1, 1 });

            analyzer = new ShingleAnalyzerWrapper(@delegate, ShingleFilter.DEFAULT_MIN_SHINGLE_SIZE, ShingleFilter.DEFAULT_MAX_SHINGLE_SIZE, ShingleFilter.DEFAULT_TOKEN_SEPARATOR, false, false, null);
            AssertAnalyzesTo(analyzer, "please divide into shingles", new string[] { "please divide", "divide ", " shingles" }, new int[] { 0, 7, 19 }, new int[] { 13, 19, 27 }, new int[] { 1, 1, 1 });

            analyzer = new ShingleAnalyzerWrapper(@delegate, ShingleFilter.DEFAULT_MIN_SHINGLE_SIZE, ShingleFilter.DEFAULT_MAX_SHINGLE_SIZE, ShingleFilter.DEFAULT_TOKEN_SEPARATOR, false, false, "");
            AssertAnalyzesTo(analyzer, "please divide into shingles", new string[] { "please divide", "divide ", " shingles" }, new int[] { 0, 7, 19 }, new int[] { 13, 19, 27 }, new int[] { 1, 1, 1 });
        }

        [Test]
        public virtual void TestOutputUnigramsIfNoShinglesSingleToken()
        {
            ShingleAnalyzerWrapper analyzer = new ShingleAnalyzerWrapper(new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false), ShingleFilter.DEFAULT_MIN_SHINGLE_SIZE, ShingleFilter.DEFAULT_MAX_SHINGLE_SIZE, "", false, true, ShingleFilter.DEFAULT_FILLER_TOKEN);
            AssertAnalyzesTo(analyzer, "please", new string[] { "please" }, new int[] { 0 }, new int[] { 6 }, new int[] { 1 });
        }
    }
}