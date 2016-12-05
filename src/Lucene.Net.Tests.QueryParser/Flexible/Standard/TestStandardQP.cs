using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Flexible.Core;
using Lucene.Net.QueryParsers.Flexible.Standard.Config;
using Lucene.Net.QueryParsers.Util;
using Lucene.Net.Search;
using Lucene.Net.Support;
using NUnit.Framework;
using System;
using System.Diagnostics;
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
            if (a == null) a = new MockAnalyzer(Random(), MockTokenizer.SIMPLE, true);
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
            Debug.Assert(cqpC != null, "Parameter must not be null");
            Debug.Assert((cqpC is StandardQueryParser), "Parameter must be instance of StandardQueryParser");
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
            Debug.Assert(cqpC is StandardQueryParser);
            StandardQueryParser qp = (StandardQueryParser)cqpC;
            qp.DefaultOperator = (Operator.OR);
        }


        public override void SetDefaultOperatorAND(ICommonQueryParserConfiguration cqpC)
        {
            Debug.Assert(cqpC is StandardQueryParser);
            StandardQueryParser qp = (StandardQueryParser)cqpC;
            qp.DefaultOperator = (Operator.AND);
        }


        public override void SetAnalyzeRangeTerms(ICommonQueryParserConfiguration cqpC,
            bool value)
        {
            throw new NotSupportedException();
        }


        public override void SetAutoGeneratePhraseQueries(ICommonQueryParserConfiguration cqpC,
            bool value)
        {
            throw new NotSupportedException();
        }


        public override void SetDateResolution(ICommonQueryParserConfiguration cqpC,
            string field, DateTools.Resolution value)
        {
            Debug.Assert(cqpC is StandardQueryParser);
            StandardQueryParser qp = (StandardQueryParser)cqpC;
            qp.DateResolutionMap.Put(field, value);
        }


        internal class TestOperatorVsWhiteSpaceAnalyzer : Analyzer
        {
            public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
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
            Analyzer a = new MockAnalyzer(Random(), MockTokenizer.WHITESPACE, false);
            AssertQueryEquals("a:b\\-?c", a, "a:b-?c");
            AssertQueryEquals("a:b\\+?c", a, "a:b+?c");
            AssertQueryEquals("a:b\\:?c", a, "a:b:?c");


            AssertQueryEquals("a:b\\\\?c", a, "a:b\\?c");
        }

        [Test]
        public override void TestEscapedWildcard()
        {
            ICommonQueryParserConfiguration qp = GetParserConfig(new MockAnalyzer(Random(), MockTokenizer.WHITESPACE, false));
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
#pragma warning disable 168
            catch (NotSupportedException e)
#pragma warning restore 168
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
#pragma warning disable 168
            catch (NotSupportedException e)
#pragma warning restore 168
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
            StandardQueryParser qp = GetParser(new MockAnalyzer(Random()));
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
                    BooleanClause.Occur.SHOULD);
            expanded.Add(new TermQuery(new Term("field", "dog")),
                BooleanClause.Occur.SHOULD);
            assertEquals(expanded, dumb.Parse("\"dogs\"", "field"));
            /** even with the phrase operator the behavior is the same */
            assertEquals(expanded, dumb.Parse("dogs", "field"));

            /**
             * custom behavior, the synonyms are expanded, unless you use quote operator
             */
            //TODO test something like "SmartQueryParser()"
        }
    }
}
