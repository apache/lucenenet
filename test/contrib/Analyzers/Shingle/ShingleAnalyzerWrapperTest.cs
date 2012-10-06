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

using System;
using System.IO;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Shingle;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Test.Analysis;
using NUnit.Framework;
using Directory = Lucene.Net.Store.Directory;

namespace Lucene.Net.Analyzers.Shingle
{
    /// <summary>
    /// A test class for ShingleAnalyzerWrapper as regards queries and scoring.
    /// </summary>
    public class ShingleAnalyzerWrapperTest : BaseTokenStreamTestCase
    {
        public IndexSearcher Searcher;

        /// <summary>
        /// Set up a new index in RAM with three test phrases and the supplied Analyzer.
        /// </summary>
        /// <param name="analyzer">the analyzer to use</param>
        /// <returns>an indexSearcher on the test index.</returns>
        public IndexSearcher SetUpSearcher(Analyzer analyzer)
        {
            Directory dir = new RAMDirectory();
            var writer = new IndexWriter(dir, analyzer, true, IndexWriter.MaxFieldLength.UNLIMITED);

            var doc = new Document();
            doc.Add(new Field("content", "please divide this sentence into shingles",
                              Field.Store.YES, Field.Index.ANALYZED));
            writer.AddDocument(doc);

            doc = new Document();
            doc.Add(new Field("content", "just another test sentence",
                              Field.Store.YES, Field.Index.ANALYZED));
            writer.AddDocument(doc);

            doc = new Document();
            doc.Add(new Field("content", "a sentence which contains no test",
                              Field.Store.YES, Field.Index.ANALYZED));
            writer.AddDocument(doc);

            writer.Close();

            return new IndexSearcher(dir, true);
        }

        protected ScoreDoc[] QueryParsingTest(Analyzer analyzer, String qs)
        {
            Searcher = SetUpSearcher(analyzer);

            var qp = new QueryParser(Util.Version.LUCENE_CURRENT, "content", analyzer);

            var q = qp.Parse(qs);

            return Searcher.Search(q, null, 1000).ScoreDocs;
        }

        protected void CompareRanks(ScoreDoc[] hits, int[] ranks)
        {
            Assert.AreEqual(ranks.Length, hits.Length);
            for (int i = 0; i < ranks.Length; i++)
            {
                Assert.AreEqual(ranks[i], hits[i].Doc);
            }
        }

        /// <summary>
        /// Will not work on an index without unigrams, since QueryParser automatically tokenizes on whitespace.
        /// </summary>
        [Test]
        public void TestShingleAnalyzerWrapperQueryParsing()
        {
            var hits = QueryParsingTest(new ShingleAnalyzerWrapper (new WhitespaceAnalyzer(), 2), "test sentence");
            var ranks = new[] {1, 2, 0};
            CompareRanks(hits, ranks);
        }

        /// <summary>
        /// This one fails with an exception.
        /// </summary>
        [Test]
        public void TestShingleAnalyzerWrapperPhraseQueryParsingFails()
        {
            var hits = QueryParsingTest(new ShingleAnalyzerWrapper (new WhitespaceAnalyzer(), 2), "\"this sentence\"");
            var ranks = new[] {0};
            CompareRanks(hits, ranks);
        }

        /// <summary>
        /// This one works, actually.
        /// </summary>
        [Test]
        public void TestShingleAnalyzerWrapperPhraseQueryParsing()
        {
            var hits = QueryParsingTest(new ShingleAnalyzerWrapper
                                             (new WhitespaceAnalyzer(), 2),
                                         "\"test sentence\"");
            var ranks = new[] {1};
            CompareRanks(hits, ranks);
        }

        /// <summary>
        /// Same as above, is tokenized without using the analyzer.
        /// </summary>
        [Test]
        public void TestShingleAnalyzerWrapperRequiredQueryParsing()
        {
            var hits = QueryParsingTest(new ShingleAnalyzerWrapper
                                             (new WhitespaceAnalyzer(), 2),
                                         "+test +sentence");
            var ranks = new[] {1, 2};
            CompareRanks(hits, ranks);
        }

        /// <summary>
        /// This shows how to construct a phrase query containing shingles.
        /// </summary>
        [Test]
        public void TestShingleAnalyzerWrapperPhraseQuery()
        {
            Analyzer analyzer = new ShingleAnalyzerWrapper(new WhitespaceAnalyzer(), 2);
            Searcher = SetUpSearcher(analyzer);

            var q = new PhraseQuery();

            var ts = analyzer.TokenStream("content", new StringReader("this sentence"));
            var j = -1;

            var posIncrAtt = ts.AddAttribute<IPositionIncrementAttribute>();
            var termAtt = ts.AddAttribute<ITermAttribute>();

            while (ts.IncrementToken())
            {
                j += posIncrAtt.PositionIncrement;
                var termText = termAtt.Term;
                q.Add(new Term("content", termText), j);
            }

            var hits = Searcher.Search(q, null, 1000).ScoreDocs;
            var ranks = new[] {0};
            CompareRanks(hits, ranks);
        }

        /// <summary>
        /// How to construct a boolean query with shingles. A query like this will
        /// implicitly score those documents higher that contain the words in the query
        /// in the right order and adjacent to each other. 
        /// </summary>
        [Test]
        public void TestShingleAnalyzerWrapperBooleanQuery()
        {
            Analyzer analyzer = new ShingleAnalyzerWrapper(new WhitespaceAnalyzer(), 2);
            Searcher = SetUpSearcher(analyzer);

            var q = new BooleanQuery();

            var ts = analyzer.TokenStream("content", new StringReader("test sentence"));

            var termAtt = ts.AddAttribute<ITermAttribute>();

            while (ts.IncrementToken())
            {
                var termText = termAtt.Term;
                q.Add(new TermQuery(new Term("content", termText)),
                      Occur.SHOULD);
            }

            var hits = Searcher.Search(q, null, 1000).ScoreDocs;
            var ranks = new[] {1, 2, 0};
            CompareRanks(hits, ranks);
        }

        [Test]
        public void TestReusableTokenStream()
        {
            Analyzer a = new ShingleAnalyzerWrapper(new WhitespaceAnalyzer(), 2);
            AssertAnalyzesToReuse(a, "please divide into shingles",
                                  new[]
                                      {
                                          "please", "please divide", "divide", "divide into", "into", "into shingles",
                                          "shingles"
                                      },
                                  new[] {0, 0, 7, 7, 14, 14, 19},
                                  new[] {6, 13, 13, 18, 18, 27, 27},
                                  new[] {1, 0, 1, 0, 1, 0, 1});
            AssertAnalyzesToReuse(a, "divide me up again",
                                  new[] {"divide", "divide me", "me", "me up", "up", "up again", "again"},
                                  new[] {0, 0, 7, 7, 10, 10, 13},
                                  new[] {6, 9, 9, 12, 12, 18, 18},
                                  new[] {1, 0, 1, 0, 1, 0, 1});
        }

        /// <summary>
        /// subclass that acts just like whitespace analyzer for testing
        /// </summary>
        [Test]
        public void TestLucene1678BwComp()
        {
            Analyzer a = new ShingleWrapperSubclassAnalyzer();
            AssertAnalyzesToReuse(a, "this is a test",
                                  new[] { "this", "is", "a", "test" },
                                  new[] { 0, 5, 8, 10 },
                                  new[] { 4, 7, 9, 14 });
        }

        #region Nested type: NonreusableAnalyzer

        private class NonreusableAnalyzer : Analyzer
        {
            private int _invocationCount;

            public override TokenStream TokenStream(String fieldName, TextReader reader)
            {
                if (++_invocationCount%2 == 0)
                    return new WhitespaceTokenizer(reader);

                return new LetterTokenizer(reader);
            }
        }

        #endregion

        #region Nested type: ShingleWrapperSubclassAnalyzer

        private class ShingleWrapperSubclassAnalyzer : ShingleAnalyzerWrapper
        {
            public ShingleWrapperSubclassAnalyzer()
                : base(Util.Version.LUCENE_CURRENT)
            {
                
            }

            public override TokenStream TokenStream(String fieldName, TextReader reader)
            {
                return new WhitespaceTokenizer(reader);
            }
        } ;

        #endregion

        /// <summary>
        /// analyzer that does not support reuse it is LetterTokenizer on odd invocations, WhitespaceTokenizer on even.
        /// </summary>
        [Test]
        public void TestWrappedAnalyzerDoesNotReuse()
        {
            Analyzer a = new ShingleAnalyzerWrapper(new NonreusableAnalyzer());
            AssertAnalyzesToReuse(a, "please divide into shingles.",
                                  new[]
                                      {
                                          "please", "please divide", "divide", "divide into", "into", "into shingles",
                                          "shingles"
                                      },
                                  new[] { 0, 0, 7, 7, 14, 14, 19 },
                                  new[] { 6, 13, 13, 18, 18, 27, 27 },
                                  new[] { 1, 0, 1, 0, 1, 0, 1 });
            AssertAnalyzesToReuse(a, "please divide into shingles.",
                                  new[]
                                      {
                                          "please", "please divide", "divide", "divide into", "into", "into shingles.",
                                          "shingles."
                                      },
                                  new[] { 0, 0, 7, 7, 14, 14, 19 },
                                  new[] { 6, 13, 13, 18, 18, 28, 28 },
                                  new[] { 1, 0, 1, 0, 1, 0, 1 });
            AssertAnalyzesToReuse(a, "please divide into shingles.",
                                  new[]
                                      {
                                          "please", "please divide", "divide", "divide into", "into", "into shingles",
                                          "shingles"
                                      },
                                  new[] { 0, 0, 7, 7, 14, 14, 19 },
                                  new[] { 6, 13, 13, 18, 18, 27, 27 },
                                  new[] { 1, 0, 1, 0, 1, 0, 1 });
        }
    }
}