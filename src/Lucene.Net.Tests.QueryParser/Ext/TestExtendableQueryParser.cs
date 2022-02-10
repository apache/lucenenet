using Lucene.Net.Analysis;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using NUnit.Framework;
using System.Globalization;

namespace Lucene.Net.QueryParsers.Ext
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
    /// Testcase for the class <see cref="ExtendableQueryParser"/>
    /// </summary>
    [TestFixture]
    public class TestExtendableQueryParser : TestQueryParser
    {
        private static char[] DELIMITERS = new char[] {
            Extensions.DEFAULT_EXTENSION_FIELD_DELIMITER, '-', '|' };

        public override Classic.QueryParser GetParser(Analyzer a)
        {
            return GetParser(a, null);
        }

        public Classic.QueryParser GetParser(Analyzer a, Extensions extensions)
        {
            if (a is null)
                a = new MockAnalyzer(Random, MockTokenizer.SIMPLE, true);
            Classic.QueryParser qp = extensions is null ? new ExtendableQueryParser(
                TEST_VERSION_CURRENT, DefaultField, a) : new ExtendableQueryParser(
                TEST_VERSION_CURRENT, DefaultField, a, extensions);
            qp.DefaultOperator = QueryParserBase.OR_OPERATOR;
            return qp;
        }

        [Test]
        public virtual void TestUnescapedExtDelimiter()
        {
            Extensions ext = NewExtensions(':');
            ext.Add("testExt", new ExtensionStub());
            ExtendableQueryParser parser = (ExtendableQueryParser)GetParser(null, ext);
            try
            {
                parser.Parse("aField:testExt:\"foo \\& bar\"");
                fail("extension field delimiter is not escaped");
            }
            catch (Lucene.Net.QueryParsers.Classic.ParseException /*e*/)
            {
            }
        }

        [Test]
        public virtual void TestExtFieldUnqoted()
        {
            for (int i = 0; i < DELIMITERS.Length; i++)
            {
                Extensions ext = NewExtensions(DELIMITERS[i]);
                ext.Add("testExt", new ExtensionStub());
                ExtendableQueryParser parser = (ExtendableQueryParser)GetParser(null,
                    ext);
                string field = ext.BuildExtensionField("testExt", "aField");
                Query query = parser.Parse(string.Format(CultureInfo.InvariantCulture, "{0}:foo bar", field));
                assertTrue("expected instance of BooleanQuery but was "
                    + query.GetType(), query is BooleanQuery);
                BooleanQuery bquery = (BooleanQuery)query;
                BooleanClause[] clauses = bquery.GetClauses();
                assertEquals(2, clauses.Length);
                BooleanClause booleanClause = clauses[0];
                query = booleanClause.Query;
                assertTrue("expected instance of TermQuery but was " + query.GetType(),
                    query is TermQuery);
                TermQuery tquery = (TermQuery)query;
                assertEquals("aField", tquery.Term
                    .Field);
                assertEquals("foo", tquery.Term.Text);

                booleanClause = clauses[1];
                query = booleanClause.Query;
                assertTrue("expected instance of TermQuery but was " + query.GetType(),
                    query is TermQuery);
                tquery = (TermQuery)query;
                assertEquals(DefaultField, tquery.Term.Field);
                assertEquals("bar", tquery.Term.Text);
            }
        }

        [Test]
        public virtual void TestExtDefaultField()
        {
            for (int i = 0; i < DELIMITERS.Length; i++)
            {
                Extensions ext = NewExtensions(DELIMITERS[i]);
                ext.Add("testExt", new ExtensionStub());
                ExtendableQueryParser parser = (ExtendableQueryParser)GetParser(null,
                    ext);
                string field = ext.BuildExtensionField("testExt");
                Query parse = parser.Parse(string.Format(CultureInfo.InvariantCulture, "{0}:\"foo \\& bar\"", field));
                assertTrue("expected instance of TermQuery but was " + parse.GetType(),
                    parse is TermQuery);
                TermQuery tquery = (TermQuery)parse;
                assertEquals(DefaultField, tquery.Term.Field);
                assertEquals("foo & bar", tquery.Term.Text);
            }
        }

        public Extensions NewExtensions(char delimiter)
        {
            return new Extensions(delimiter);
        }

        [Test]
        public virtual void TestExtField()
        {
            for (int i = 0; i < DELIMITERS.Length; i++)
            {
                Extensions ext = NewExtensions(DELIMITERS[i]);
                ext.Add("testExt", new ExtensionStub());
                ExtendableQueryParser parser = (ExtendableQueryParser)GetParser(null,
                    ext);
                string field = ext.BuildExtensionField("testExt", "afield");
                Query parse = parser.Parse(string.Format(CultureInfo.InvariantCulture, "{0}:\"foo \\& bar\"", field));
                assertTrue("expected instance of TermQuery but was " + parse.GetType(),
                    parse is TermQuery);
                TermQuery tquery = (TermQuery)parse;
                assertEquals("afield", tquery.Term.Field);
                assertEquals("foo & bar", tquery.Term.Text);
            }
        }


        #region TestQueryParser
        // LUCENENET NOTE: Tests in a base class are not pulled into the correct
        // context in Visual Studio. This fixes that with the minimum amount of code necessary
        // to run them in the correct context without duplicating all of the tests.

        [Test]
        public override void TestDefaultOperator()
        {
            base.TestDefaultOperator();
        }

        [Test]
        public override void TestProtectedCtors()
        {
            base.TestProtectedCtors();
        }

        [Test]
        public override void TestFuzzySlopeExtendability()
        {
            base.TestFuzzySlopeExtendability();
        }

        [Test]
        public override void TestStarParsing()
        {
            base.TestStarParsing();
        }

        [Test]
        public override void TestCustomQueryParserWildcard()
        {
            base.TestCustomQueryParserWildcard();
        }

        [Test]
        public override void TestCustomQueryParserFuzzy()
        {
            base.TestCustomQueryParserFuzzy();
        }

        [Test]
        public override void TestNewFieldQuery()
        {
            base.TestNewFieldQuery();
        }

        /// <summary>
        /// simple synonyms test
        /// </summary>
        [Test]
        public override void TestSynonyms()
        {
            base.TestSynonyms();
        }

        /// <summary>
        /// forms multiphrase query
        /// </summary>
        [Test]
        public override void TestSynonymsPhrase()
        {
            base.TestSynonymsPhrase();
        }

        /// <summary>
        /// simple CJK synonym test
        /// </summary>
        [Test]
        public override void TestCJKSynonym()
        {
            base.TestCJKSynonym();
        }

        /// <summary>
        /// synonyms with default OR operator 
        /// </summary>
        [Test]
        public override void TestCJKSynonymsOR()
        {
            base.TestCJKSynonymsOR();
        }

        /// <summary>
        /// more complex synonyms with default OR operator
        /// </summary>
        [Test]
        public override void TestCJKSynonymsOR2()
        {
            base.TestCJKSynonymsOR2();
        }

        /// <summary>
        /// synonyms with default AND operator
        /// </summary>
        [Test]
        public override void TestCJKSynonymsAND()
        {
            base.TestCJKSynonymsAND();
        }

        /// <summary>
        /// more complex synonyms with default AND operator
        /// </summary>
        [Test]
        public override void TestCJKSynonymsAND2()
        {
            base.TestCJKSynonymsAND2();
        }

        [Test]
        public override void TestCJKSynonymsPhrase()
        {
            base.TestCJKSynonymsPhrase();
        }

        #endregion

        #region QueryParserTestBase
        // LUCENENET NOTE: Tests in a base class are not pulled into the correct
        // context in Visual Studio. This fixes that with the minimum amount of code necessary
        // to run them in the correct context without duplicating all of the tests.

        [Test]
        public override void TestCJK()
        {
            base.TestCJK();
        }

        [Test]
        public override void TestCJKTerm()
        {
            base.TestCJKTerm();
        }

        [Test]
        public override void TestCJKBoostedTerm()
        {
            base.TestCJKBoostedTerm();
        }

        [Test]
        public override void TestCJKPhrase()
        {
            base.TestCJKPhrase();
        }

        [Test]
        public override void TestCJKBoostedPhrase()
        {
            base.TestCJKBoostedPhrase();
        }

        [Test]
        public override void TestCJKSloppyPhrase()
        {
            base.TestCJKSloppyPhrase();
        }

        [Test]
        public override void TestAutoGeneratePhraseQueriesOn()
        {
            base.TestAutoGeneratePhraseQueriesOn();
        }

        [Test]
        public override void TestSimple()
        {
            base.TestSimple();
        }

        [Test]
        public override void TestOperatorVsWhitespace()
        {
            base.TestOperatorVsWhitespace();
        }

        [Test]
        public override void TestPunct()
        {
            base.TestPunct();
        }

        [Test]
        public override void TestSlop()
        {
            base.TestSlop();
        }

        [Test]
        public override void TestNumber()
        {
            base.TestNumber();
        }

        [Test]
        public override void TestWildcard()
        {
            base.TestWildcard();
        }

        [Test]
        public override void TestLeadingWildcardType()
        {
            base.TestLeadingWildcardType();
        }

        [Test]
        public override void TestQPA()
        {
            base.TestQPA();
        }

        [Test]
        public override void TestRange()
        {
            base.TestRange();
        }

        [Test]
        public override void TestRangeWithPhrase()
        {
            base.TestRangeWithPhrase();
        }

        [Test]
        public override void TestDateRange()
        {
            base.TestDateRange();
        }

        [Test]
        public override void TestEscaped()
        {
            base.TestEscaped();
        }

        [Test]
        public override void TestEscapedVsQuestionMarkAsWildcard()
        {
            base.TestEscapedVsQuestionMarkAsWildcard();
        }

        [Test]
        public override void TestQueryStringEscaping()
        {
            base.TestQueryStringEscaping();
        }

        [Test]
        public override void TestTabNewlineCarriageReturn()
        {
            base.TestTabNewlineCarriageReturn();
        }

        [Test]
        public override void TestSimpleDAO()
        {
            base.TestSimpleDAO();
        }

        [Test]
        public override void TestBoost()
        {
            base.TestBoost();
        }

        [Test]
        public override void TestException()
        {
            base.TestException();
        }

        [Test]
        public override void TestBooleanQuery()
        {
            base.TestBooleanQuery();
        }

        [Test]
        public override void TestPrecedence()
        {
            base.TestPrecedence();
        }

        [Test]
        public override void TestEscapedWildcard()
        {
            base.TestEscapedWildcard();
        }

        [Test]
        public override void TestRegexps()
        {
            base.TestRegexps();
        }

        [Test]
        public override void TestStopwords()
        {
            base.TestStopwords();
        }

        [Test]
        public override void TestPositionIncrement()
        {
            base.TestPositionIncrement();
        }

        [Test]
        public override void TestMatchAllDocs()
        {
            base.TestMatchAllDocs();
        }

        // LUCENE-2002: make sure defaults for StandardAnalyzer's
        // enableStopPositionIncr & QueryParser's enablePosIncr
        // "match"
        [Test]
        public override void TestPositionIncrements()
        {
            base.TestPositionIncrements();
        }

        [Test]
        public override void TestCollatedRange()
        {
            base.TestCollatedRange();
        }

        [Test]
        public override void TestDistanceAsEditsParsing()
        {
            base.TestDistanceAsEditsParsing();
        }

        [Test]
        public override void TestPhraseQueryToString()
        {
            base.TestPhraseQueryToString();
        }

        [Test]
        public override void TestParseWildcardAndPhraseQueries()
        {
            base.TestParseWildcardAndPhraseQueries();
        }

        [Test]
        public override void TestPhraseQueryPositionIncrements()
        {
            base.TestPhraseQueryPositionIncrements();
        }

        [Test]
        public override void TestMatchAllQueryParsing()
        {
            base.TestMatchAllQueryParsing();
        }

        [Test]
        public override void TestNestedAndClausesFoo()
        {
            base.TestNestedAndClausesFoo();
        }

        #endregion
    }
}
