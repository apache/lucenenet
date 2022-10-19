using Lucene.Net.Analysis;
using Lucene.Net.Analysis.TokenAttributes;
using NUnit.Framework;
using System;
using System.IO;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Util
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

    using Analyzer = Lucene.Net.Analysis.Analyzer;
    using BooleanQuery = Lucene.Net.Search.BooleanQuery;
    using CharacterRunAutomaton = Lucene.Net.Util.Automaton.CharacterRunAutomaton;
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using MockTokenizer = Lucene.Net.Analysis.MockTokenizer;
    using MultiPhraseQuery = Lucene.Net.Search.MultiPhraseQuery;
    using Occur = Lucene.Net.Search.Occur;
    using PhraseQuery = Lucene.Net.Search.PhraseQuery;
    using RegExp = Lucene.Net.Util.Automaton.RegExp;
    using Term = Lucene.Net.Index.Term;
    using TermQuery = Lucene.Net.Search.TermQuery;
    using TokenFilter = Lucene.Net.Analysis.TokenFilter;
    using Tokenizer = Lucene.Net.Analysis.Tokenizer;
    using TokenStream = Lucene.Net.Analysis.TokenStream;

    [TestFixture]
    public class TestQueryBuilder : LuceneTestCase
    {
        [Test]
        public virtual void TestTerm()
        {
            TermQuery expected = new TermQuery(new Term("field", "test"));
            QueryBuilder builder = new QueryBuilder(new MockAnalyzer(Random));
            Assert.AreEqual(expected, builder.CreateBooleanQuery("field", "test"));
        }

        [Test]
        public virtual void TestBoolean()
        {
            BooleanQuery expected = new BooleanQuery();
            expected.Add(new TermQuery(new Term("field", "foo")), Occur.SHOULD);
            expected.Add(new TermQuery(new Term("field", "bar")), Occur.SHOULD);
            QueryBuilder builder = new QueryBuilder(new MockAnalyzer(Random));
            Assert.IsTrue(expected.Equals(builder.CreateBooleanQuery("field", "foo bar")));
            //Assert.AreEqual(expected, builder.CreateBooleanQuery("field", "foo bar"));
        }

        [Test]
        public virtual void TestBooleanMust()
        {
            BooleanQuery expected = new BooleanQuery();
            expected.Add(new TermQuery(new Term("field", "foo")), Occur.MUST);
            expected.Add(new TermQuery(new Term("field", "bar")), Occur.MUST);
            QueryBuilder builder = new QueryBuilder(new MockAnalyzer(Random));
            Assert.AreEqual(expected, builder.CreateBooleanQuery("field", "foo bar", Occur.MUST));
        }

        [Test]
        public virtual void TestMinShouldMatchNone()
        {
            QueryBuilder builder = new QueryBuilder(new MockAnalyzer(Random));
            Assert.AreEqual(builder.CreateBooleanQuery("field", "one two three four"), builder.CreateMinShouldMatchQuery("field", "one two three four", 0f));
        }

        [Test]
        public virtual void TestMinShouldMatchAll()
        {
            QueryBuilder builder = new QueryBuilder(new MockAnalyzer(Random));
            Assert.AreEqual(builder.CreateBooleanQuery("field", "one two three four", Occur.MUST), builder.CreateMinShouldMatchQuery("field", "one two three four", 1f));
        }

        [Test]
        public virtual void TestMinShouldMatch()
        {
            BooleanQuery expected = new BooleanQuery();
            expected.Add(new TermQuery(new Term("field", "one")), Occur.SHOULD);
            expected.Add(new TermQuery(new Term("field", "two")), Occur.SHOULD);
            expected.Add(new TermQuery(new Term("field", "three")), Occur.SHOULD);
            expected.Add(new TermQuery(new Term("field", "four")), Occur.SHOULD);
            expected.MinimumNumberShouldMatch = 0;

            QueryBuilder builder = new QueryBuilder(new MockAnalyzer(Random));
            Assert.AreEqual(expected, builder.CreateMinShouldMatchQuery("field", "one two three four", 0.1f));
            Assert.AreEqual(expected, builder.CreateMinShouldMatchQuery("field", "one two three four", 0.24f));

            expected.MinimumNumberShouldMatch = 1;
            Assert.AreEqual(expected, builder.CreateMinShouldMatchQuery("field", "one two three four", 0.25f));
            Assert.AreEqual(expected, builder.CreateMinShouldMatchQuery("field", "one two three four", 0.49f));

            expected.MinimumNumberShouldMatch = 2;
            Assert.AreEqual(expected, builder.CreateMinShouldMatchQuery("field", "one two three four", 0.5f));
            Assert.AreEqual(expected, builder.CreateMinShouldMatchQuery("field", "one two three four", 0.74f));

            expected.MinimumNumberShouldMatch = 3;
            Assert.AreEqual(expected, builder.CreateMinShouldMatchQuery("field", "one two three four", 0.75f));
            Assert.AreEqual(expected, builder.CreateMinShouldMatchQuery("field", "one two three four", 0.99f));
        }

        [Test]
        public virtual void TestPhraseQueryPositionIncrements()
        {
            PhraseQuery expected = new PhraseQuery();
            expected.Add(new Term("field", "1"));
            expected.Add(new Term("field", "2"), 2);

            CharacterRunAutomaton stopList = new CharacterRunAutomaton((new RegExp("[sS][tT][oO][pP]")).ToAutomaton());

            Analyzer analyzer = new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false, stopList);

            QueryBuilder builder = new QueryBuilder(analyzer);
            Assert.AreEqual(expected, builder.CreatePhraseQuery("field", "1 stop 2"));
        }

        [Test]
        public virtual void TestEmpty()
        {
            QueryBuilder builder = new QueryBuilder(new MockAnalyzer(Random));
            Assert.IsNull(builder.CreateBooleanQuery("field", ""));
        }

        /// <summary>
        /// adds synonym of "dog" for "dogs". </summary>
        internal class MockSynonymAnalyzer : Analyzer
        {
            protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                MockTokenizer tokenizer = new MockTokenizer(reader);
                return new TokenStreamComponents(tokenizer, new MockSynonymFilter(tokenizer));
            }
        }

        /// <summary>
        /// adds synonym of "dog" for "dogs".
        /// </summary>
        protected internal class MockSynonymFilter : TokenFilter
        {
            private readonly ICharTermAttribute termAtt;
            private readonly IPositionIncrementAttribute posIncAtt;
            private bool addSynonym = false;

            public MockSynonymFilter(TokenStream input)
                : base(input)
            {
                termAtt = AddAttribute<ICharTermAttribute>();
                posIncAtt = AddAttribute<IPositionIncrementAttribute>();
            }

            public sealed override bool IncrementToken()
            {
                if (addSynonym) // inject our synonym
                {
                    ClearAttributes();
                    termAtt.SetEmpty().Append("dog");
                    posIncAtt.PositionIncrement = 0;
                    addSynonym = false;
                    return true;
                }

                if (m_input.IncrementToken())
                {
                    addSynonym = termAtt.ToString().Equals("dogs", StringComparison.Ordinal);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// simple synonyms test </summary>
        [Test]
        public virtual void TestSynonyms()
        {
            BooleanQuery expected = new BooleanQuery(true);
            expected.Add(new TermQuery(new Term("field", "dogs")), Occur.SHOULD);
            expected.Add(new TermQuery(new Term("field", "dog")), Occur.SHOULD);
            QueryBuilder builder = new QueryBuilder(new MockSynonymAnalyzer());
            Assert.AreEqual(expected, builder.CreateBooleanQuery("field", "dogs"));
            Assert.AreEqual(expected, builder.CreatePhraseQuery("field", "dogs"));
            Assert.AreEqual(expected, builder.CreateBooleanQuery("field", "dogs", Occur.MUST));
            Assert.AreEqual(expected, builder.CreatePhraseQuery("field", "dogs"));
        }

        /// <summary>
        /// forms multiphrase query </summary>
        [Test]
        public virtual void TestSynonymsPhrase()
        {
            MultiPhraseQuery expected = new MultiPhraseQuery();
            expected.Add(new Term("field", "old"));
            expected.Add(new Term[] { new Term("field", "dogs"), new Term("field", "dog") });
            QueryBuilder builder = new QueryBuilder(new MockSynonymAnalyzer());
            Assert.AreEqual(expected, builder.CreatePhraseQuery("field", "old dogs"));
        }

        protected internal class SimpleCJKTokenizer : Tokenizer
        {
            private readonly ICharTermAttribute termAtt;

            public SimpleCJKTokenizer(TextReader input)
                : base(input)
            {
                termAtt = AddAttribute<ICharTermAttribute>();
            }

            public sealed override bool IncrementToken()
            {
                int ch = m_input.Read();
                if (ch < 0)
                {
                    return false;
                }
                ClearAttributes();
                termAtt.SetEmpty().Append((char)ch);
                return true;
            }
        }

        private class SimpleCJKAnalyzer : Analyzer
        {
            protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                return new TokenStreamComponents(new SimpleCJKTokenizer(reader));
            }
        }

        [Test]
        public virtual void TestCJKTerm()
        {
            // individual CJK chars as terms
            SimpleCJKAnalyzer analyzer = new SimpleCJKAnalyzer();

            BooleanQuery expected = new BooleanQuery();
            expected.Add(new TermQuery(new Term("field", "中")), Occur.SHOULD);
            expected.Add(new TermQuery(new Term("field", "国")), Occur.SHOULD);

            QueryBuilder builder = new QueryBuilder(analyzer);
            Assert.AreEqual(expected, builder.CreateBooleanQuery("field", "中国"));
        }

        [Test]
        public virtual void TestCJKPhrase()
        {
            // individual CJK chars as terms
            SimpleCJKAnalyzer analyzer = new SimpleCJKAnalyzer();

            PhraseQuery expected = new PhraseQuery();
            expected.Add(new Term("field", "中"));
            expected.Add(new Term("field", "国"));

            QueryBuilder builder = new QueryBuilder(analyzer);
            Assert.AreEqual(expected, builder.CreatePhraseQuery("field", "中国"));
        }

        [Test]
        public virtual void TestCJKSloppyPhrase()
        {
            // individual CJK chars as terms
            SimpleCJKAnalyzer analyzer = new SimpleCJKAnalyzer();

            PhraseQuery expected = new PhraseQuery();
            expected.Slop = 3;
            expected.Add(new Term("field", "中"));
            expected.Add(new Term("field", "国"));

            QueryBuilder builder = new QueryBuilder(analyzer);
            Assert.AreEqual(expected, builder.CreatePhraseQuery("field", "中国", 3));
        }

        /// <summary>
        /// adds synonym of "國" for "国".
        /// </summary>
        protected internal class MockCJKSynonymFilter : TokenFilter
        {
            private readonly ICharTermAttribute termAtt;
            private readonly IPositionIncrementAttribute posIncAtt;
            private bool addSynonym = false;

            public MockCJKSynonymFilter(TokenStream input)
                : base(input)
            {
                termAtt = AddAttribute<ICharTermAttribute>();
                posIncAtt = AddAttribute<IPositionIncrementAttribute>();
            }

            public sealed override bool IncrementToken()
            {
                if (addSynonym) // inject our synonym
                {
                    ClearAttributes();
                    termAtt.SetEmpty().Append('國');
                    posIncAtt.PositionIncrement = 0;
                    addSynonym = false;
                    return true;
                }

                if (m_input.IncrementToken())
                {
                    addSynonym = termAtt.ToString().Equals("国", StringComparison.Ordinal);
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        internal class MockCJKSynonymAnalyzer : Analyzer
        {
            protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                Tokenizer tokenizer = new SimpleCJKTokenizer(reader);
                return new TokenStreamComponents(tokenizer, new MockCJKSynonymFilter(tokenizer));
            }
        }

        /// <summary>
        /// simple CJK synonym test </summary>
        [Test]
        public virtual void TestCJKSynonym()
        {
            BooleanQuery expected = new BooleanQuery(true);
            expected.Add(new TermQuery(new Term("field", "国")), Occur.SHOULD);
            expected.Add(new TermQuery(new Term("field", "國")), Occur.SHOULD);
            QueryBuilder builder = new QueryBuilder(new MockCJKSynonymAnalyzer());
            Assert.AreEqual(expected, builder.CreateBooleanQuery("field", "国"));
            Assert.AreEqual(expected, builder.CreatePhraseQuery("field", "国"));
            Assert.AreEqual(expected, builder.CreateBooleanQuery("field", "国", Occur.MUST));
        }

        /// <summary>
        /// synonyms with default OR operator </summary>
        [Test]
        public virtual void TestCJKSynonymsOR()
        {
            BooleanQuery expected = new BooleanQuery();
            expected.Add(new TermQuery(new Term("field", "中")), Occur.SHOULD);
            BooleanQuery inner = new BooleanQuery(true);
            inner.Add(new TermQuery(new Term("field", "国")), Occur.SHOULD);
            inner.Add(new TermQuery(new Term("field", "國")), Occur.SHOULD);
            expected.Add(inner, Occur.SHOULD);
            QueryBuilder builder = new QueryBuilder(new MockCJKSynonymAnalyzer());
            Assert.AreEqual(expected, builder.CreateBooleanQuery("field", "中国"));
        }

        /// <summary>
        /// more complex synonyms with default OR operator </summary>
        [Test]
        public virtual void TestCJKSynonymsOR2()
        {
            BooleanQuery expected = new BooleanQuery();
            expected.Add(new TermQuery(new Term("field", "中")), Occur.SHOULD);
            BooleanQuery inner = new BooleanQuery(true);
            inner.Add(new TermQuery(new Term("field", "国")), Occur.SHOULD);
            inner.Add(new TermQuery(new Term("field", "國")), Occur.SHOULD);
            expected.Add(inner, Occur.SHOULD);
            BooleanQuery inner2 = new BooleanQuery(true);
            inner2.Add(new TermQuery(new Term("field", "国")), Occur.SHOULD);
            inner2.Add(new TermQuery(new Term("field", "國")), Occur.SHOULD);
            expected.Add(inner2, Occur.SHOULD);
            QueryBuilder builder = new QueryBuilder(new MockCJKSynonymAnalyzer());
            Assert.AreEqual(expected, builder.CreateBooleanQuery("field", "中国国"));
        }

        /// <summary>
        /// synonyms with default AND operator </summary>
        [Test]
        public virtual void TestCJKSynonymsAND()
        {
            BooleanQuery expected = new BooleanQuery();
            expected.Add(new TermQuery(new Term("field", "中")), Occur.MUST);
            BooleanQuery inner = new BooleanQuery(true);
            inner.Add(new TermQuery(new Term("field", "国")), Occur.SHOULD);
            inner.Add(new TermQuery(new Term("field", "國")), Occur.SHOULD);
            expected.Add(inner, Occur.MUST);
            QueryBuilder builder = new QueryBuilder(new MockCJKSynonymAnalyzer());
            Assert.AreEqual(expected, builder.CreateBooleanQuery("field", "中国", Occur.MUST));
        }

        /// <summary>
        /// more complex synonyms with default AND operator </summary>
        [Test]
        public virtual void TestCJKSynonymsAND2()
        {
            BooleanQuery expected = new BooleanQuery();
            expected.Add(new TermQuery(new Term("field", "中")), Occur.MUST);
            BooleanQuery inner = new BooleanQuery(true);
            inner.Add(new TermQuery(new Term("field", "国")), Occur.SHOULD);
            inner.Add(new TermQuery(new Term("field", "國")), Occur.SHOULD);
            expected.Add(inner, Occur.MUST);
            BooleanQuery inner2 = new BooleanQuery(true);
            inner2.Add(new TermQuery(new Term("field", "国")), Occur.SHOULD);
            inner2.Add(new TermQuery(new Term("field", "國")), Occur.SHOULD);
            expected.Add(inner2, Occur.MUST);
            QueryBuilder builder = new QueryBuilder(new MockCJKSynonymAnalyzer());
            Assert.AreEqual(expected, builder.CreateBooleanQuery("field", "中国国", Occur.MUST));
        }

        /// <summary>
        /// forms multiphrase query </summary>
        [Test]
        public virtual void TestCJKSynonymsPhrase()
        {
            MultiPhraseQuery expected = new MultiPhraseQuery();
            expected.Add(new Term("field", "中"));
            expected.Add(new Term[] { new Term("field", "国"), new Term("field", "國") });
            QueryBuilder builder = new QueryBuilder(new MockCJKSynonymAnalyzer());
            Assert.AreEqual(expected, builder.CreatePhraseQuery("field", "中国"));
            expected.Slop = 3;
            Assert.AreEqual(expected, builder.CreatePhraseQuery("field", "中国", 3));
        }
    }
}