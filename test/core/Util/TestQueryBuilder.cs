using System.IO;
using NUnit.Framework;

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
    using MockAnalyzer = Lucene.Net.Analysis.MockAnalyzer;
    using MockTokenizer = Lucene.Net.Analysis.MockTokenizer;
    using TokenFilter = Lucene.Net.Analysis.TokenFilter;
    using TokenStream = Lucene.Net.Analysis.TokenStream;
    using Tokenizer = Lucene.Net.Analysis.Tokenizer;
    using CharTermAttribute = Lucene.Net.Analysis.Tokenattributes.CharTermAttribute;
    using PositionIncrementAttribute = Lucene.Net.Analysis.Tokenattributes.PositionIncrementAttribute;
    using Term = Lucene.Net.Index.Term;
    using BooleanClause = Lucene.Net.Search.BooleanClause;
    using BooleanQuery = Lucene.Net.Search.BooleanQuery;
    using MultiPhraseQuery = Lucene.Net.Search.MultiPhraseQuery;
    using PhraseQuery = Lucene.Net.Search.PhraseQuery;
    using TermQuery = Lucene.Net.Search.TermQuery;
    using CharacterRunAutomaton = Lucene.Net.Util.Automaton.CharacterRunAutomaton;
    using RegExp = Lucene.Net.Util.Automaton.RegExp;

    [TestFixture]
    public class TestQueryBuilder : LuceneTestCase
    {
        [Test]
        public virtual void TestTerm()
        {
            TermQuery expected = new TermQuery(new Term("field", "test"));
            QueryBuilder builder = new QueryBuilder(new MockAnalyzer(Random()));
            Assert.AreEqual(expected, builder.CreateBooleanQuery("field", "test"));
        }

        [Test]
        public virtual void TestBoolean()
        {
            BooleanQuery expected = new BooleanQuery();
            expected.Add(new TermQuery(new Term("field", "foo")), BooleanClause.Occur_e.SHOULD);
            expected.Add(new TermQuery(new Term("field", "bar")), BooleanClause.Occur_e.SHOULD);
            QueryBuilder builder = new QueryBuilder(new MockAnalyzer(Random()));
            Assert.AreEqual(expected, builder.CreateBooleanQuery("field", "foo bar"));
        }

        [Test]
        public virtual void TestBooleanMust()
        {
            BooleanQuery expected = new BooleanQuery();
            expected.Add(new TermQuery(new Term("field", "foo")), BooleanClause.Occur_e.MUST);
            expected.Add(new TermQuery(new Term("field", "bar")), BooleanClause.Occur_e.MUST);
            QueryBuilder builder = new QueryBuilder(new MockAnalyzer(Random()));
            Assert.AreEqual(expected, builder.CreateBooleanQuery("field", "foo bar", BooleanClause.Occur_e.MUST));
        }

        [Test]
        public virtual void TestMinShouldMatchNone()
        {
            QueryBuilder builder = new QueryBuilder(new MockAnalyzer(Random()));
            Assert.AreEqual(builder.CreateBooleanQuery("field", "one two three four"), builder.CreateMinShouldMatchQuery("field", "one two three four", 0f));
        }

        [Test]
        public virtual void TestMinShouldMatchAll()
        {
            QueryBuilder builder = new QueryBuilder(new MockAnalyzer(Random()));
            Assert.AreEqual(builder.CreateBooleanQuery("field", "one two three four", BooleanClause.Occur_e.MUST), builder.CreateMinShouldMatchQuery("field", "one two three four", 1f));
        }

        [Test]
        public virtual void TestMinShouldMatch()
        {
            BooleanQuery expected = new BooleanQuery();
            expected.Add(new TermQuery(new Term("field", "one")), BooleanClause.Occur_e.SHOULD);
            expected.Add(new TermQuery(new Term("field", "two")), BooleanClause.Occur_e.SHOULD);
            expected.Add(new TermQuery(new Term("field", "three")), BooleanClause.Occur_e.SHOULD);
            expected.Add(new TermQuery(new Term("field", "four")), BooleanClause.Occur_e.SHOULD);
            expected.MinimumNumberShouldMatch = 0;

            QueryBuilder builder = new QueryBuilder(new MockAnalyzer(Random()));
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

            Analyzer analyzer = new MockAnalyzer(Random(), MockTokenizer.WHITESPACE, false, stopList);

            QueryBuilder builder = new QueryBuilder(analyzer);
            Assert.AreEqual(expected, builder.CreatePhraseQuery("field", "1 stop 2"));
        }

        [Test]
        public virtual void TestEmpty()
        {
            QueryBuilder builder = new QueryBuilder(new MockAnalyzer(Random()));
            Assert.IsNull(builder.CreateBooleanQuery("field", ""));
        }

        /// <summary>
        /// adds synonym of "dog" for "dogs". </summary>
        internal class MockSynonymAnalyzer : Analyzer
        {
            protected override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
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
            internal CharTermAttribute TermAtt;// = AddAttribute(typeof(CharTermAttribute));
            internal PositionIncrementAttribute PosIncAtt;// = AddAttribute(typeof(PositionIncrementAttribute));
            internal bool AddSynonym = false;

            public MockSynonymFilter(TokenStream input)
                : base(input)
            {
            }

            public override bool IncrementToken()
            {
                if (AddSynonym) // inject our synonym
                {
                    ClearAttributes();
                    TermAtt.SetEmpty().Append("dog");
                    PosIncAtt.PositionIncrement = 0;
                    AddSynonym = false;
                    return true;
                }

                if (Input.IncrementToken())
                {
                    AddSynonym = TermAtt.ToString().Equals("dogs");
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
            expected.Add(new TermQuery(new Term("field", "dogs")), BooleanClause.Occur_e.SHOULD);
            expected.Add(new TermQuery(new Term("field", "dog")), BooleanClause.Occur_e.SHOULD);
            QueryBuilder builder = new QueryBuilder(new MockSynonymAnalyzer());
            Assert.AreEqual(expected, builder.CreateBooleanQuery("field", "dogs"));
            Assert.AreEqual(expected, builder.CreatePhraseQuery("field", "dogs"));
            Assert.AreEqual(expected, builder.CreateBooleanQuery("field", "dogs", BooleanClause.Occur_e.MUST));
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
            internal CharTermAttribute TermAtt;// = AddAttribute(typeof(CharTermAttribute));

            public SimpleCJKTokenizer(TextReader input)
                : base(input)
            {
            }

            public override bool IncrementToken()
            {
                int ch = Input.Read();
                if (ch < 0)
                {
                    return false;
                }
                ClearAttributes();
                TermAtt.SetEmpty().Append((char)ch);
                return true;
            }
        }

        private class SimpleCJKAnalyzer : Analyzer
        {
            private readonly TestQueryBuilder OuterInstance;

            public SimpleCJKAnalyzer(TestQueryBuilder outerInstance)
            {
                this.OuterInstance = outerInstance;
            }

            protected override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                return new TokenStreamComponents(new SimpleCJKTokenizer(reader));
            }
        }

        [Test]
        public virtual void TestCJKTerm()
        {
            // individual CJK chars as terms
            SimpleCJKAnalyzer analyzer = new SimpleCJKAnalyzer(this);

            BooleanQuery expected = new BooleanQuery();
            expected.Add(new TermQuery(new Term("field", "中")), BooleanClause.Occur_e.SHOULD);
            expected.Add(new TermQuery(new Term("field", "国")), BooleanClause.Occur_e.SHOULD);

            QueryBuilder builder = new QueryBuilder(analyzer);
            Assert.AreEqual(expected, builder.CreateBooleanQuery("field", "中国"));
        }

        [Test]
        public virtual void TestCJKPhrase()
        {
            // individual CJK chars as terms
            SimpleCJKAnalyzer analyzer = new SimpleCJKAnalyzer(this);

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
            SimpleCJKAnalyzer analyzer = new SimpleCJKAnalyzer(this);

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
            internal CharTermAttribute TermAtt;// = AddAttribute<CharTermAttribute>();
            internal PositionIncrementAttribute PosIncAtt;// = AddAttribute<PositionIncrementAttribute>();
            internal bool AddSynonym = false;

            public MockCJKSynonymFilter(TokenStream input)
                : base(input)
            {
            }

            public override bool IncrementToken()
            {
                if (AddSynonym) // inject our synonym
                {
                    ClearAttributes();
                    TermAtt.SetEmpty().Append("國");
                    PosIncAtt.PositionIncrement = 0;
                    AddSynonym = false;
                    return true;
                }

                if (Input.IncrementToken())
                {
                    AddSynonym = TermAtt.ToString().Equals("国");
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
            protected override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
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
            expected.Add(new TermQuery(new Term("field", "国")), BooleanClause.Occur_e.SHOULD);
            expected.Add(new TermQuery(new Term("field", "國")), BooleanClause.Occur_e.SHOULD);
            QueryBuilder builder = new QueryBuilder(new MockCJKSynonymAnalyzer());
            Assert.AreEqual(expected, builder.CreateBooleanQuery("field", "国"));
            Assert.AreEqual(expected, builder.CreatePhraseQuery("field", "国"));
            Assert.AreEqual(expected, builder.CreateBooleanQuery("field", "国", BooleanClause.Occur_e.MUST));
        }

        /// <summary>
        /// synonyms with default OR operator </summary>
        [Test]
        public virtual void TestCJKSynonymsOR()
        {
            BooleanQuery expected = new BooleanQuery();
            expected.Add(new TermQuery(new Term("field", "中")), BooleanClause.Occur_e.SHOULD);
            BooleanQuery inner = new BooleanQuery(true);
            inner.Add(new TermQuery(new Term("field", "国")), BooleanClause.Occur_e.SHOULD);
            inner.Add(new TermQuery(new Term("field", "國")), BooleanClause.Occur_e.SHOULD);
            expected.Add(inner, BooleanClause.Occur_e.SHOULD);
            QueryBuilder builder = new QueryBuilder(new MockCJKSynonymAnalyzer());
            Assert.AreEqual(expected, builder.CreateBooleanQuery("field", "中国"));
        }

        /// <summary>
        /// more complex synonyms with default OR operator </summary>
        [Test]
        public virtual void TestCJKSynonymsOR2()
        {
            BooleanQuery expected = new BooleanQuery();
            expected.Add(new TermQuery(new Term("field", "中")), BooleanClause.Occur_e.SHOULD);
            BooleanQuery inner = new BooleanQuery(true);
            inner.Add(new TermQuery(new Term("field", "国")), BooleanClause.Occur_e.SHOULD);
            inner.Add(new TermQuery(new Term("field", "國")), BooleanClause.Occur_e.SHOULD);
            expected.Add(inner, BooleanClause.Occur_e.SHOULD);
            BooleanQuery inner2 = new BooleanQuery(true);
            inner2.Add(new TermQuery(new Term("field", "国")), BooleanClause.Occur_e.SHOULD);
            inner2.Add(new TermQuery(new Term("field", "國")), BooleanClause.Occur_e.SHOULD);
            expected.Add(inner2, BooleanClause.Occur_e.SHOULD);
            QueryBuilder builder = new QueryBuilder(new MockCJKSynonymAnalyzer());
            Assert.AreEqual(expected, builder.CreateBooleanQuery("field", "中国国"));
        }

        /// <summary>
        /// synonyms with default AND operator </summary>
        [Test]
        public virtual void TestCJKSynonymsAND()
        {
            BooleanQuery expected = new BooleanQuery();
            expected.Add(new TermQuery(new Term("field", "中")), BooleanClause.Occur_e.MUST);
            BooleanQuery inner = new BooleanQuery(true);
            inner.Add(new TermQuery(new Term("field", "国")), BooleanClause.Occur_e.SHOULD);
            inner.Add(new TermQuery(new Term("field", "國")), BooleanClause.Occur_e.SHOULD);
            expected.Add(inner, BooleanClause.Occur_e.MUST);
            QueryBuilder builder = new QueryBuilder(new MockCJKSynonymAnalyzer());
            Assert.AreEqual(expected, builder.CreateBooleanQuery("field", "中国", BooleanClause.Occur_e.MUST));
        }

        /// <summary>
        /// more complex synonyms with default AND operator </summary>
        [Test]
        public virtual void TestCJKSynonymsAND2()
        {
            BooleanQuery expected = new BooleanQuery();
            expected.Add(new TermQuery(new Term("field", "中")), BooleanClause.Occur_e.MUST);
            BooleanQuery inner = new BooleanQuery(true);
            inner.Add(new TermQuery(new Term("field", "国")), BooleanClause.Occur_e.SHOULD);
            inner.Add(new TermQuery(new Term("field", "國")), BooleanClause.Occur_e.SHOULD);
            expected.Add(inner, BooleanClause.Occur_e.MUST);
            BooleanQuery inner2 = new BooleanQuery(true);
            inner2.Add(new TermQuery(new Term("field", "国")), BooleanClause.Occur_e.SHOULD);
            inner2.Add(new TermQuery(new Term("field", "國")), BooleanClause.Occur_e.SHOULD);
            expected.Add(inner2, BooleanClause.Occur_e.MUST);
            QueryBuilder builder = new QueryBuilder(new MockCJKSynonymAnalyzer());
            Assert.AreEqual(expected, builder.CreateBooleanQuery("field", "中国国", BooleanClause.Occur_e.MUST));
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