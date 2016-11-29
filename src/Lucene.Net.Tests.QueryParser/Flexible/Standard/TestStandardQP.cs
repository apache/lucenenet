using Lucene.Net.Analysis;
using Lucene.Net.QueryParsers.Flexible.Core;
using Lucene.Net.QueryParsers.Flexible.Standard;
using Lucene.Net.QueryParsers.Util;
using Lucene.Net.Search;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Lucene.Net.Documents.DateTools;
using static Lucene.Net.QueryParsers.Classic.QueryParserBase;
using System.IO;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Flexible.Standard.Config;
using NUnit.Framework;

namespace Lucene.Net.QueryParsers.Flexible.Standard
{
    /// <summary>
    /// Tests QueryParser.
    /// </summary>
    public class TestStandardQP : QueryParserTestBase
    {
        public StandardQueryParser GetParser(Analyzer a)
        {
            if (a == null) a = new MockAnalyzer(Random(), MockTokenizer.SIMPLE, true);
            StandardQueryParser qp = new StandardQueryParser(a);
            qp.DefaultOperator = (Config.Operator.OR);

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
            qp.DefaultOperator = (Config.Operator.OR);
        }


        public override void SetDefaultOperatorAND(ICommonQueryParserConfiguration cqpC)
        {
            Debug.Assert(cqpC is StandardQueryParser);
            StandardQueryParser qp = (StandardQueryParser)cqpC;
            qp.DefaultOperator = (Config.Operator.AND);
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
            string field, Resolution value)
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
            //        Analyzer a = new Analyzer()
            //    {
            //        @Override
            //      public TokenStreamComponents createComponents(String fieldName,
            //          Reader reader)
            //    {
            //        return new TokenStreamComponents(new MockTokenizer(reader,
            //            MockTokenizer.WHITESPACE, false));
            //    }
            //};
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
            catch (NotSupportedException e)
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
            catch (NotSupportedException e)
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
            assertEquals(/*StandardQueryConfigHandler.*/Config.Operator.OR, qp.DefaultOperator);
            SetDefaultOperatorAND(qp);
            assertEquals(/*StandardQueryConfigHandler.*/Config.Operator.AND, qp.DefaultOperator);
            SetDefaultOperatorOR(qp);
            assertEquals(/*StandardQueryConfigHandler.*/Config.Operator.OR, qp.DefaultOperator);
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
