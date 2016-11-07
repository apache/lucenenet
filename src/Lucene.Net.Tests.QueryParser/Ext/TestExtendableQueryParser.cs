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
            if (a == null)
                a = new MockAnalyzer(Random(), MockTokenizer.SIMPLE, true);
            Classic.QueryParser qp = extensions == null ? new ExtendableQueryParser(
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
            catch (ParseException /*e*/)
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
                BooleanClause[] clauses = bquery.Clauses;
                assertEquals(2, clauses.Length);
                BooleanClause booleanClause = clauses[0];
                query = booleanClause.Query;
                assertTrue("expected instance of TermQuery but was " + query.GetType(),
                    query is TermQuery);
                TermQuery tquery = (TermQuery)query;
                assertEquals("aField", tquery.Term
                    .Field);
                assertEquals("foo", tquery.Term.Text());

                booleanClause = clauses[1];
                query = booleanClause.Query;
                assertTrue("expected instance of TermQuery but was " + query.GetType(),
                    query is TermQuery);
                tquery = (TermQuery)query;
                assertEquals(DefaultField, tquery.Term.Field);
                assertEquals("bar", tquery.Term.Text());
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
                assertEquals("foo & bar", tquery.Term.Text());
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
                assertEquals("foo & bar", tquery.Term.Text());
            }
        }
    }
}
