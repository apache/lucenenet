using Lucene.Net.Analysis;
using Lucene.Net.Diagnostics;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Flexible.Core;
using Lucene.Net.QueryParsers.Flexible.Standard.Config;
using Lucene.Net.QueryParsers.Util;
using Lucene.Net.Search;
using Lucene.Net.Support;
using NUnit.Framework;
using System;
using System.IO;
using Operator = Lucene.Net.QueryParsers.Flexible.Standard.Config.StandardQueryConfigHandler.Operator;

namespace Lucene.Net.QueryParsers.Flexible.Standard
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
    /// Tests QueryParser.
    /// </summary>
    public class TestStandardQP : QueryParserTestBase
    {
        public StandardQueryParser GetParser(Analyzer a)
        {
            if (a is null) a = new MockAnalyzer(Random, MockTokenizer.SIMPLE, true);
            StandardQueryParser qp = new StandardQueryParser(a);
            qp.DefaultOperator = (Operator.OR);

            return qp;
        }

        public Query Parse(String query, StandardQueryParser qp)
        {
            return qp.Parse(query, DefaultField);
        }


        public override ICommonQueryParserConfiguration GetParserConfig(Analyzer a)
        {
            return GetParser(a);
        }


        public override Query GetQuery(String query, ICommonQueryParserConfiguration cqpC)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(cqpC != null, "Parameter must not be null");
            if (Debugging.AssertsEnabled) Debugging.Assert((cqpC is StandardQueryParser), "Parameter must be instance of StandardQueryParser");
            StandardQueryParser qp = (StandardQueryParser)cqpC;
            return Parse(query, qp);
        }


        public override Query GetQuery(String query, Analyzer a)
        {
            return Parse(query, GetParser(a));
        }


        public override bool IsQueryParserException(Exception exception)
        {
            return exception is QueryNodeException;
        }


        public override void SetDefaultOperatorOR(ICommonQueryParserConfiguration cqpC)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(cqpC is StandardQueryParser);
            StandardQueryParser qp = (StandardQueryParser)cqpC;
            qp.DefaultOperator = (Operator.OR);
        }


        public override void SetDefaultOperatorAND(ICommonQueryParserConfiguration cqpC)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(cqpC is StandardQueryParser);
            StandardQueryParser qp = (StandardQueryParser)cqpC;
            qp.DefaultOperator = (Operator.AND);
        }


        public override void SetAnalyzeRangeTerms(ICommonQueryParserConfiguration cqpC,
            bool value)
        {
            throw UnsupportedOperationException.Create();
        }


        public override void SetAutoGeneratePhraseQueries(ICommonQueryParserConfiguration cqpC,
            bool value)
        {
            throw UnsupportedOperationException.Create();
        }


        public override void SetDateResolution(ICommonQueryParserConfiguration cqpC,
            string field, DateResolution value)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(cqpC is StandardQueryParser);
            StandardQueryParser qp = (StandardQueryParser)cqpC;
            qp.DateResolutionMap[field] = value;
        }


        internal class TestOperatorVsWhiteSpaceAnalyzer : Analyzer
        {
            protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                return new TokenStreamComponents(new MockTokenizer(reader,
                    MockTokenizer.WHITESPACE, false));
            }
        }
        [Test]
        public override void TestOperatorVsWhitespace()
        {
            // LUCENE-2566 is not implemented for StandardQueryParser
            // TODO implement LUCENE-2566 and remove this (override)method
            Analyzer a = new TestOperatorVsWhiteSpaceAnalyzer();
            AssertQueryEquals("a - b", a, "a -b");
            AssertQueryEquals("a + b", a, "a +b");
            AssertQueryEquals("a ! b", a, "a -b");
        }

        [Test]
        public override void TestRangeWithPhrase()
        {
            // StandardSyntaxParser does not differentiate between a term and a
            // one-term-phrase in a range query.
            // Is this an issue? Should StandardSyntaxParser mark the text as
            // wasEscaped=true ?
            AssertQueryEquals("[\\* TO \"*\"]", null, "[\\* TO *]");
        }

        [Test]
        public override void TestEscapedVsQuestionMarkAsWildcard()
        {
            Analyzer a = new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false);
            AssertQueryEquals("a:b\\-?c", a, "a:b-?c");
            AssertQueryEquals("a:b\\+?c", a, "a:b+?c");
            AssertQueryEquals("a:b\\:?c", a, "a:b:?c");


            AssertQueryEquals("a:b\\\\?c", a, "a:b\\?c");
        }

        [Test]
        public override void TestEscapedWildcard()
        {
            ICommonQueryParserConfiguration qp = GetParserConfig(new MockAnalyzer(Random, MockTokenizer.WHITESPACE, false));
            WildcardQuery q = new WildcardQuery(new Term("field", "foo?ba?r"));//TODO not correct!!
            assertEquals(q, GetQuery("foo\\?ba?r", qp));
        }


        [Test]
        public override void TestCollatedRange()
        {
            try
            {
                SetAnalyzeRangeTerms(GetParser(null), true);
                base.TestCollatedRange();
            }
            catch (Exception e) when (e.IsUnsupportedOperationException())
            {
                // expected
            }
        }

        [Test]
        public override void TestAutoGeneratePhraseQueriesOn()
        {
            try
            {
                SetAutoGeneratePhraseQueries(GetParser(null), true);
                base.TestAutoGeneratePhraseQueriesOn();
            }
            catch (Exception e) when (e.IsUnsupportedOperationException())
            {
                // expected
            }
        }

        [Test]
        public override void TestStarParsing()
        {
        }

        [Test]
        public override void TestDefaultOperator()
        {
            StandardQueryParser qp = GetParser(new MockAnalyzer(Random));
            // make sure OR is the default:
            assertEquals(StandardQueryConfigHandler.Operator.OR, qp.DefaultOperator);
            SetDefaultOperatorAND(qp);
            assertEquals(StandardQueryConfigHandler.Operator.AND, qp.DefaultOperator);
            SetDefaultOperatorOR(qp);
            assertEquals(StandardQueryConfigHandler.Operator.OR, qp.DefaultOperator);
        }


        [Test]
        public override void TestNewFieldQuery()
        {
            /** ordinary behavior, synonyms form uncoordinated boolean query */
            StandardQueryParser dumb = GetParser(new Analyzer1());
            BooleanQuery expanded = new BooleanQuery(true);
            expanded.Add(new TermQuery(new Term("field", "dogs")),
                    Occur.SHOULD);
            expanded.Add(new TermQuery(new Term("field", "dog")),
                Occur.SHOULD);
            assertEquals(expanded, dumb.Parse("\"dogs\"", "field"));
            /** even with the phrase operator the behavior is the same */
            assertEquals(expanded, dumb.Parse("dogs", "field"));

            /**
             * custom behavior, the synonyms are expanded, unless you use quote operator
             */
            //TODO test something like "SmartQueryParser()"
        }


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
        public override void TestSimple()
        {
            base.TestSimple();
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
