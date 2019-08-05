/*
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Search;
using Lucene.Net.Util;

namespace Lucene.Net.QueryParsers.Classic
{
    [TestFixture]
    public class TestMultiAnalyzer_ : BaseTokenStreamTestCase
    {

        private static int multiToken = 0;

        [Test]
        public virtual void TestMultiAnalyzer()
        {

            QueryParser qp = new QueryParser(TEST_VERSION_CURRENT, "", new MultiAnalyzer());

            // trivial, no multiple tokens:
            assertEquals("foo", qp.Parse("foo").toString());
            assertEquals("foo", qp.Parse("\"foo\"").toString());
            assertEquals("foo foobar", qp.Parse("foo foobar").toString());
            assertEquals("\"foo foobar\"", qp.Parse("\"foo foobar\"").toString());
            assertEquals("\"foo foobar blah\"", qp.Parse("\"foo foobar blah\"").toString());

            // two tokens at the same position:
            assertEquals("(multi multi2) foo", qp.Parse("multi foo").toString());
            assertEquals("foo (multi multi2)", qp.Parse("foo multi").toString());
            assertEquals("(multi multi2) (multi multi2)", qp.Parse("multi multi").toString());
            assertEquals("+(foo (multi multi2)) +(bar (multi multi2))",
                qp.Parse("+(foo multi) +(bar multi)").toString());
            assertEquals("+(foo (multi multi2)) field:\"bar (multi multi2)\"",
                qp.Parse("+(foo multi) field:\"bar multi\"").toString());

            // phrases:
            assertEquals("\"(multi multi2) foo\"", qp.Parse("\"multi foo\"").toString());
            assertEquals("\"foo (multi multi2)\"", qp.Parse("\"foo multi\"").toString());
            assertEquals("\"foo (multi multi2) foobar (multi multi2)\"",
                qp.Parse("\"foo multi foobar multi\"").toString());

            // fields:
            assertEquals("(field:multi field:multi2) field:foo", qp.Parse("field:multi field:foo").toString());
            assertEquals("field:\"(multi multi2) foo\"", qp.Parse("field:\"multi foo\"").toString());

            // three tokens at one position:
            assertEquals("triplemulti multi3 multi2", qp.Parse("triplemulti").toString());
            assertEquals("foo (triplemulti multi3 multi2) foobar",
                qp.Parse("foo triplemulti foobar").toString());

            // phrase with non-default slop:
            assertEquals("\"(multi multi2) foo\"~10", qp.Parse("\"multi foo\"~10").toString());

            // phrase with non-default boost:
            assertEquals("\"(multi multi2) foo\"^2.0", qp.Parse("\"multi foo\"^2").toString());

            // phrase after changing default slop
            qp.PhraseSlop=(99);
            assertEquals("\"(multi multi2) foo\"~99 bar",
                         qp.Parse("\"multi foo\" bar").toString());
            assertEquals("\"(multi multi2) foo\"~99 \"foo bar\"~2",
                         qp.Parse("\"multi foo\" \"foo bar\"~2").toString());
            qp.PhraseSlop=(0);

            // non-default operator:
            qp.DefaultOperator=(QueryParserBase.AND_OPERATOR);
            assertEquals("+(multi multi2) +foo", qp.Parse("multi foo").toString());

        }

        [Test]
        public virtual void TestMultiAnalyzerWithSubclassOfQueryParser()
        {

            DumbQueryParser qp = new DumbQueryParser("", new MultiAnalyzer());
            qp.PhraseSlop = (99); // modified default slop

            // direct call to (super's) getFieldQuery to demonstrate differnce
            // between phrase and multiphrase with modified default slop
            assertEquals("\"foo bar\"~99",
                         qp.GetSuperFieldQuery("", "foo bar", true).toString());
            assertEquals("\"(multi multi2) bar\"~99",
                         qp.GetSuperFieldQuery("", "multi bar", true).toString());


            // ask sublcass to parse phrase with modified default slop
            assertEquals("\"(multi multi2) foo\"~99 bar",
                         qp.Parse("\"multi foo\" bar").toString());

        }

        [Test]
        public virtual void TestPosIncrementAnalyzer()
        {
#pragma warning disable 612, 618
            QueryParser qp = new QueryParser(LuceneVersion.LUCENE_40, "", new PosIncrementAnalyzer());
#pragma warning restore 612, 618
            assertEquals("quick brown", qp.Parse("the quick brown").toString());
            assertEquals("quick brown fox", qp.Parse("the quick brown fox").toString());
        }

        /// <summary>
        /// Expands "multi" to "multi" and "multi2", both at the same position,
        /// and expands "triplemulti" to "triplemulti", "multi3", and "multi2".  
        /// </summary>
        private class MultiAnalyzer : Analyzer
        {
            protected internal override TokenStreamComponents CreateComponents(string fieldName, System.IO.TextReader reader)
            {
                Tokenizer result = new MockTokenizer(reader, MockTokenizer.WHITESPACE, true);
                return new TokenStreamComponents(result, new TestFilter(result));
            }
        }

        private sealed class TestFilter : TokenFilter
        {

            private string prevType;
            private int prevStartOffset;
            private int prevEndOffset;

            private readonly ICharTermAttribute termAtt;
            private readonly IPositionIncrementAttribute posIncrAtt;
            private readonly IOffsetAttribute offsetAtt;
            private readonly ITypeAttribute typeAtt;

            public TestFilter(TokenStream @in)
                : base(@in)
            {
                termAtt = AddAttribute<ICharTermAttribute>();
                posIncrAtt = AddAttribute<IPositionIncrementAttribute>();
                offsetAtt = AddAttribute<IOffsetAttribute>();
                typeAtt = AddAttribute<ITypeAttribute>();
            }

            public override sealed bool IncrementToken()
            {
                if (multiToken > 0)
                {
                    termAtt.SetEmpty().Append("multi" + (multiToken + 1));
                    offsetAtt.SetOffset(prevStartOffset, prevEndOffset);
                    typeAtt.Type = (prevType);
                    posIncrAtt.PositionIncrement = (0);
                    multiToken--;
                    return true;
                }
                else
                {
                    bool next = m_input.IncrementToken();
                    if (!next)
                    {
                        return false;
                    }
                    prevType = typeAtt.Type;
                    prevStartOffset = offsetAtt.StartOffset;
                    prevEndOffset = offsetAtt.EndOffset;
                    string text = termAtt.toString();
                    if (text.Equals("triplemulti", StringComparison.Ordinal))
                    {
                        multiToken = 2;
                        return true;
                    }
                    else if (text.Equals("multi", StringComparison.Ordinal))
                    {
                        multiToken = 1;
                        return true;
                    }
                    else
                    {
                        return true;
                    }
                }
            }

            public override void Reset()
            {
                base.Reset();
                this.prevType = null;
                this.prevStartOffset = 0;
                this.prevEndOffset = 0;
            }
        }

        /// <summary>
        /// Analyzes "the quick brown" as: quick(incr=2) brown(incr=1).
        /// Does not work correctly for input other than "the quick brown ...".
        /// </summary>
        private class PosIncrementAnalyzer : Analyzer
        {
            protected internal override TokenStreamComponents CreateComponents(string fieldName, System.IO.TextReader reader)
            {
                Tokenizer result = new MockTokenizer(reader, MockTokenizer.WHITESPACE, true);
                return new TokenStreamComponents(result, new TestPosIncrementFilter(result));
            }
        }

        private sealed class TestPosIncrementFilter : TokenFilter
        {
            ICharTermAttribute termAtt;
            IPositionIncrementAttribute posIncrAtt;

            public TestPosIncrementFilter(TokenStream @in)
                : base(@in)
            {
                termAtt = AddAttribute<ICharTermAttribute>();
                posIncrAtt = AddAttribute<IPositionIncrementAttribute>();
            }

            public override sealed bool IncrementToken()
            {
                while (m_input.IncrementToken())
                {
                    if (termAtt.toString().Equals("the", StringComparison.Ordinal))
                    {
                        // stopword, do nothing
                    }
                    else if (termAtt.toString().Equals("quick", StringComparison.Ordinal))
                    {
                        posIncrAtt.PositionIncrement = (2);
                        return true;
                    }
                    else
                    {
                        posIncrAtt.PositionIncrement = (1);
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// a very simple subclass of QueryParser
        /// </summary>
        private sealed class DumbQueryParser : QueryParser
        {
            public DumbQueryParser(string f, Analyzer a)
                : base(TEST_VERSION_CURRENT, f, a)
            {
            }

            // expose super's version 
            public Query GetSuperFieldQuery(string f, string t, bool quoted)
            {
                return base.GetFieldQuery(f, t, quoted);
            }

            // wrap super's version
            protected internal override Query GetFieldQuery(string field, string queryText, bool quoted)
            {
                return new DumbQueryWrapper(GetSuperFieldQuery(field, queryText, quoted));
            }
        }

        /// <summary>
        /// A very simple wrapper to prevent instanceof checks but uses
        /// the toString of the query it wraps.
        /// </summary>
        private sealed class DumbQueryWrapper : Query
        {
            private Query q;
            public DumbQueryWrapper(Query q)
            {
                this.q = q;
            }

            public override string ToString(string field)
            {
                return q.ToString(field);
            }
        }

    }
}
