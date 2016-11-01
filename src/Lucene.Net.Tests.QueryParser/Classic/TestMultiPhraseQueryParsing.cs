using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.QueryParsers.Classic
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

    [TestFixture]
    public class TestMultiPhraseQueryParsing_ : LuceneTestCase
    {
        private class TokenAndPos
        {
            public readonly string token;
            public readonly int pos;
            public TokenAndPos(string token, int pos)
            {
                this.token = token;
                this.pos = pos;
            }
        }

        private class CannedAnalyzer : Analyzer
        {
            private readonly TokenAndPos[] tokens;

            public CannedAnalyzer(TokenAndPos[] tokens)
            {
                this.tokens = tokens;
            }

            public override TokenStreamComponents CreateComponents(string fieldName, System.IO.TextReader reader)
            {
                return new TokenStreamComponents(new CannedTokenizer(reader, tokens));
            }
        }

        private class CannedTokenizer : Tokenizer
        {
            private readonly TokenAndPos[] tokens;
            private int upto = 0;
            private int lastPos = 0;
            private readonly ICharTermAttribute termAtt;
            private readonly IPositionIncrementAttribute posIncrAtt;

            public CannedTokenizer(System.IO.TextReader reader, TokenAndPos[] tokens)
                : base(reader)
            {
                this.tokens = tokens;
                this.termAtt = AddAttribute<ICharTermAttribute>();
                this.posIncrAtt = AddAttribute<IPositionIncrementAttribute>();
            }

            public override sealed bool IncrementToken()
            {
                ClearAttributes();
                if (upto < tokens.Length)
                {
                    TokenAndPos token = tokens[upto++];
                    termAtt.SetEmpty();
                    termAtt.Append(token.token);
                    posIncrAtt.PositionIncrement = (token.pos - lastPos);
                    lastPos = token.pos;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            public override void Reset()
            {
                base.Reset();
                this.upto = 0;
                this.lastPos = 0;
            }
        }

        [Test]
        public virtual void TestMultiPhraseQueryParsing()
        {
            TokenAndPos[] INCR_0_QUERY_TOKENS_AND = new TokenAndPos[]
            {
                new TokenAndPos("a", 0),
                new TokenAndPos("1", 0),
                new TokenAndPos("b", 1),
                new TokenAndPos("1", 1),
                new TokenAndPos("c", 2)
            };

            QueryParser qp = new QueryParser(TEST_VERSION_CURRENT, "field", new CannedAnalyzer(INCR_0_QUERY_TOKENS_AND));
            Query q = qp.Parse("\"this text is acually ignored\"");
            assertTrue("wrong query type!", q is MultiPhraseQuery);

            MultiPhraseQuery multiPhraseQuery = new MultiPhraseQuery();
            multiPhraseQuery.Add(new Term[] { new Term("field", "a"), new Term("field", "1") }, -1);
            multiPhraseQuery.Add(new Term[] { new Term("field", "b"), new Term("field", "1") }, 0);
            multiPhraseQuery.Add(new Term[] { new Term("field", "c") }, 1);

            assertEquals(multiPhraseQuery, q);
        }
    }
}
