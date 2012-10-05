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
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.CJK;
using Lucene.Net.Test.Analysis;
using NUnit.Framework;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.Analyzers.Cjk
{
    [TestFixture]
    public class TestCJKTokenizer : BaseTokenStreamTestCase
    {

        public class TestToken
        {
            protected internal String termText;
            protected internal int start;
            protected internal int end;
            protected internal String type;
        }

        public TestToken NewToken(String termText, int start, int end, int type)
        {
            TestToken token = new TestToken();
            token.termText = termText;
            token.type = CJKTokenizer.TOKEN_TYPE_NAMES[type];
            token.start = start;
            token.end = end;
            return token;
        }

        public void CheckCjkToken(String str, TestToken[] out_tokens)
        {
            Analyzer analyzer = new CJKAnalyzer(Version.LUCENE_CURRENT);
            String[] terms = new String[out_tokens.Length];
            int[] startOffsets = new int[out_tokens.Length];
            int[] endOffsets = new int[out_tokens.Length];
            String[] types = new String[out_tokens.Length];
            for (int i = 0; i < out_tokens.Length; i++)
            {
                terms[i] = out_tokens[i].termText;
                startOffsets[i] = out_tokens[i].start;
                endOffsets[i] = out_tokens[i].end;
                types[i] = out_tokens[i].type;
            }
            AssertAnalyzesTo(analyzer, str, terms, startOffsets, endOffsets, types, null);
        }

        public void CheckCjkTokenReusable(Analyzer a, String str, TestToken[] out_tokens)
        {
            Analyzer analyzer = new CJKAnalyzer(Version.LUCENE_CURRENT);
            String[] terms = new String[out_tokens.Length];
            int[] startOffsets = new int[out_tokens.Length];
            int[] endOffsets = new int[out_tokens.Length];
            String[] types = new String[out_tokens.Length];
            for (int i = 0; i < out_tokens.Length; i++)
            {
                terms[i] = out_tokens[i].termText;
                startOffsets[i] = out_tokens[i].start;
                endOffsets[i] = out_tokens[i].end;
                types[i] = out_tokens[i].type;
            }
            AssertAnalyzesToReuse(analyzer, str, terms, startOffsets, endOffsets, types, null);
        }

        [Test]
        public void TestJa1()
        {
            String str = "\u4e00\u4e8c\u4e09\u56db\u4e94\u516d\u4e03\u516b\u4e5d\u5341";

            TestToken[] out_tokens = {
                                         NewToken("\u4e00\u4e8c", 0, 2, CJKTokenizer.DOUBLE_TOKEN_TYPE),
                                         NewToken("\u4e8c\u4e09", 1, 3, CJKTokenizer.DOUBLE_TOKEN_TYPE),
                                         NewToken("\u4e09\u56db", 2, 4, CJKTokenizer.DOUBLE_TOKEN_TYPE),
                                         NewToken("\u56db\u4e94", 3, 5, CJKTokenizer.DOUBLE_TOKEN_TYPE),
                                         NewToken("\u4e94\u516d", 4, 6, CJKTokenizer.DOUBLE_TOKEN_TYPE),
                                         NewToken("\u516d\u4e03", 5, 7, CJKTokenizer.DOUBLE_TOKEN_TYPE),
                                         NewToken("\u4e03\u516b", 6, 8, CJKTokenizer.DOUBLE_TOKEN_TYPE),
                                         NewToken("\u516b\u4e5d", 7, 9, CJKTokenizer.DOUBLE_TOKEN_TYPE),
                                         NewToken("\u4e5d\u5341", 8, 10, CJKTokenizer.DOUBLE_TOKEN_TYPE)
                                     };
            CheckCjkToken(str, out_tokens);
        }

        [Test]
        public void TestJa2()
        {
            String str = "\u4e00 \u4e8c\u4e09\u56db \u4e94\u516d\u4e03\u516b\u4e5d \u5341";

            TestToken[] out_tokens = {
                                         NewToken("\u4e00", 0, 1, CJKTokenizer.DOUBLE_TOKEN_TYPE),
                                         NewToken("\u4e8c\u4e09", 2, 4, CJKTokenizer.DOUBLE_TOKEN_TYPE),
                                         NewToken("\u4e09\u56db", 3, 5, CJKTokenizer.DOUBLE_TOKEN_TYPE),
                                         NewToken("\u4e94\u516d", 6, 8, CJKTokenizer.DOUBLE_TOKEN_TYPE),
                                         NewToken("\u516d\u4e03", 7, 9, CJKTokenizer.DOUBLE_TOKEN_TYPE),
                                         NewToken("\u4e03\u516b", 8, 10, CJKTokenizer.DOUBLE_TOKEN_TYPE),
                                         NewToken("\u516b\u4e5d", 9, 11, CJKTokenizer.DOUBLE_TOKEN_TYPE),
                                         NewToken("\u5341", 12, 13, CJKTokenizer.DOUBLE_TOKEN_TYPE)
                                     };
            CheckCjkToken(str, out_tokens);
        }

        [Test]
        public void TestC()
        {
            String str = "abc defgh ijklmn opqrstu vwxy z";

            TestToken[] out_tokens = {
                                         NewToken("abc", 0, 3, CJKTokenizer.SINGLE_TOKEN_TYPE),
                                         NewToken("defgh", 4, 9, CJKTokenizer.SINGLE_TOKEN_TYPE),
                                         NewToken("ijklmn", 10, 16, CJKTokenizer.SINGLE_TOKEN_TYPE),
                                         NewToken("opqrstu", 17, 24, CJKTokenizer.SINGLE_TOKEN_TYPE),
                                         NewToken("vwxy", 25, 29, CJKTokenizer.SINGLE_TOKEN_TYPE),
                                         NewToken("z", 30, 31, CJKTokenizer.SINGLE_TOKEN_TYPE),
                                     };
            CheckCjkToken(str, out_tokens);
        }

        [Test]
        public void TestMix()
        {
            String str = "\u3042\u3044\u3046\u3048\u304aabc\u304b\u304d\u304f\u3051\u3053";

            TestToken[] out_tokens = {
                                         NewToken("\u3042\u3044", 0, 2, CJKTokenizer.DOUBLE_TOKEN_TYPE),
                                         NewToken("\u3044\u3046", 1, 3, CJKTokenizer.DOUBLE_TOKEN_TYPE),
                                         NewToken("\u3046\u3048", 2, 4, CJKTokenizer.DOUBLE_TOKEN_TYPE),
                                         NewToken("\u3048\u304a", 3, 5, CJKTokenizer.DOUBLE_TOKEN_TYPE),
                                         NewToken("abc", 5, 8, CJKTokenizer.SINGLE_TOKEN_TYPE),
                                         NewToken("\u304b\u304d", 8, 10, CJKTokenizer.DOUBLE_TOKEN_TYPE),
                                         NewToken("\u304d\u304f", 9, 11, CJKTokenizer.DOUBLE_TOKEN_TYPE),
                                         NewToken("\u304f\u3051", 10, 12, CJKTokenizer.DOUBLE_TOKEN_TYPE),
                                         NewToken("\u3051\u3053", 11, 13, CJKTokenizer.DOUBLE_TOKEN_TYPE)
                                     };
            CheckCjkToken(str, out_tokens);
        }

        [Test]
        public void TestMix2()
        {
            String str = "\u3042\u3044\u3046\u3048\u304aab\u3093c\u304b\u304d\u304f\u3051 \u3053";

            TestToken[] out_tokens = {
                                         NewToken("\u3042\u3044", 0, 2, CJKTokenizer.DOUBLE_TOKEN_TYPE),
                                         NewToken("\u3044\u3046", 1, 3, CJKTokenizer.DOUBLE_TOKEN_TYPE),
                                         NewToken("\u3046\u3048", 2, 4, CJKTokenizer.DOUBLE_TOKEN_TYPE),
                                         NewToken("\u3048\u304a", 3, 5, CJKTokenizer.DOUBLE_TOKEN_TYPE),
                                         NewToken("ab", 5, 7, CJKTokenizer.SINGLE_TOKEN_TYPE),
                                         NewToken("\u3093", 7, 8, CJKTokenizer.DOUBLE_TOKEN_TYPE),
                                         NewToken("c", 8, 9, CJKTokenizer.SINGLE_TOKEN_TYPE),
                                         NewToken("\u304b\u304d", 9, 11, CJKTokenizer.DOUBLE_TOKEN_TYPE),
                                         NewToken("\u304d\u304f", 10, 12, CJKTokenizer.DOUBLE_TOKEN_TYPE),
                                         NewToken("\u304f\u3051", 11, 13, CJKTokenizer.DOUBLE_TOKEN_TYPE),
                                         NewToken("\u3053", 14, 15, CJKTokenizer.DOUBLE_TOKEN_TYPE)
                                     };
            CheckCjkToken(str, out_tokens);
        }

        [Test]
        public void TestSingleChar()
        {
            String str = "\u4e00";

            TestToken[] out_tokens = {
                                         NewToken("\u4e00", 0, 1, CJKTokenizer.DOUBLE_TOKEN_TYPE),
                                     };
            CheckCjkToken(str, out_tokens);
        }

        /*
         * Full-width text is normalized to half-width 
         */
        [Test]
        public void TestFullWidth()
        {
            String str = "Test 1234";
            TestToken[] out_tokens = {
                                         NewToken("test", 0, 4, CJKTokenizer.SINGLE_TOKEN_TYPE),
                                         NewToken("1234", 5, 9, CJKTokenizer.SINGLE_TOKEN_TYPE)
                                     };
            CheckCjkToken(str, out_tokens);
        }

        /*
         * Non-english text (not just CJK) is treated the same as CJK: C1C2 C2C3 
         */
        [Test]
        public void TestNonIdeographic()
        {
            String str = "\u4e00 روبرت موير";
            TestToken[] out_tokens = {
                                         NewToken("\u4e00", 0, 1, CJKTokenizer.DOUBLE_TOKEN_TYPE),
                                         NewToken("رو", 2, 4, CJKTokenizer.DOUBLE_TOKEN_TYPE),
                                         NewToken("وب", 3, 5, CJKTokenizer.DOUBLE_TOKEN_TYPE),
                                         NewToken("بر", 4, 6, CJKTokenizer.DOUBLE_TOKEN_TYPE),
                                         NewToken("رت", 5, 7, CJKTokenizer.DOUBLE_TOKEN_TYPE),
                                         NewToken("مو", 8, 10, CJKTokenizer.DOUBLE_TOKEN_TYPE),
                                         NewToken("وي", 9, 11, CJKTokenizer.DOUBLE_TOKEN_TYPE),
                                         NewToken("ير", 10, 12, CJKTokenizer.DOUBLE_TOKEN_TYPE)
                                     };
            CheckCjkToken(str, out_tokens);
        }

        /*
         * Non-english text with nonletters (non-spacing marks,etc) is treated as C1C2 C2C3,
         * except for words are split around non-letters.
         */
        [Test]
        public void TestNonIdeographicNonLetter()
        {
            String str = "\u4e00 رُوبرت موير";
            TestToken[] out_tokens = {
                                         NewToken("\u4e00", 0, 1, CJKTokenizer.DOUBLE_TOKEN_TYPE),
                                         NewToken("ر", 2, 3, CJKTokenizer.DOUBLE_TOKEN_TYPE),
                                         NewToken("وب", 4, 6, CJKTokenizer.DOUBLE_TOKEN_TYPE),
                                         NewToken("بر", 5, 7, CJKTokenizer.DOUBLE_TOKEN_TYPE),
                                         NewToken("رت", 6, 8, CJKTokenizer.DOUBLE_TOKEN_TYPE),
                                         NewToken("مو", 9, 11, CJKTokenizer.DOUBLE_TOKEN_TYPE),
                                         NewToken("وي", 10, 12, CJKTokenizer.DOUBLE_TOKEN_TYPE),
                                         NewToken("ير", 11, 13, CJKTokenizer.DOUBLE_TOKEN_TYPE)
                                     };
            CheckCjkToken(str, out_tokens);
        }

        [Test]
        public void TestTokenStream()
        {
            Analyzer analyzer = new CJKAnalyzer(Version.LUCENE_CURRENT);
            AssertAnalyzesTo(analyzer, "\u4e00\u4e01\u4e02",
                             new String[] {"\u4e00\u4e01", "\u4e01\u4e02"});
        }

        [Test]
        public void TestReusableTokenStream()
        {
            Analyzer analyzer = new CJKAnalyzer(Version.LUCENE_CURRENT);
            String str = "\u3042\u3044\u3046\u3048\u304aabc\u304b\u304d\u304f\u3051\u3053";

            TestToken[] out_tokens = {
                                         NewToken("\u3042\u3044", 0, 2, CJKTokenizer.DOUBLE_TOKEN_TYPE),
                                         NewToken("\u3044\u3046", 1, 3, CJKTokenizer.DOUBLE_TOKEN_TYPE),
                                         NewToken("\u3046\u3048", 2, 4, CJKTokenizer.DOUBLE_TOKEN_TYPE),
                                         NewToken("\u3048\u304a", 3, 5, CJKTokenizer.DOUBLE_TOKEN_TYPE),
                                         NewToken("abc", 5, 8, CJKTokenizer.SINGLE_TOKEN_TYPE),
                                         NewToken("\u304b\u304d", 8, 10, CJKTokenizer.DOUBLE_TOKEN_TYPE),
                                         NewToken("\u304d\u304f", 9, 11, CJKTokenizer.DOUBLE_TOKEN_TYPE),
                                         NewToken("\u304f\u3051", 10, 12, CJKTokenizer.DOUBLE_TOKEN_TYPE),
                                         NewToken("\u3051\u3053", 11, 13, CJKTokenizer.DOUBLE_TOKEN_TYPE)
                                     };
            CheckCjkTokenReusable(analyzer, str, out_tokens);

            str = "\u3042\u3044\u3046\u3048\u304aab\u3093c\u304b\u304d\u304f\u3051 \u3053";
            TestToken[] out_tokens2 = {
                                          NewToken("\u3042\u3044", 0, 2, CJKTokenizer.DOUBLE_TOKEN_TYPE),
                                          NewToken("\u3044\u3046", 1, 3, CJKTokenizer.DOUBLE_TOKEN_TYPE),
                                          NewToken("\u3046\u3048", 2, 4, CJKTokenizer.DOUBLE_TOKEN_TYPE),
                                          NewToken("\u3048\u304a", 3, 5, CJKTokenizer.DOUBLE_TOKEN_TYPE),
                                          NewToken("ab", 5, 7, CJKTokenizer.SINGLE_TOKEN_TYPE),
                                          NewToken("\u3093", 7, 8, CJKTokenizer.DOUBLE_TOKEN_TYPE),
                                          NewToken("c", 8, 9, CJKTokenizer.SINGLE_TOKEN_TYPE),
                                          NewToken("\u304b\u304d", 9, 11, CJKTokenizer.DOUBLE_TOKEN_TYPE),
                                          NewToken("\u304d\u304f", 10, 12, CJKTokenizer.DOUBLE_TOKEN_TYPE),
                                          NewToken("\u304f\u3051", 11, 13, CJKTokenizer.DOUBLE_TOKEN_TYPE),
                                          NewToken("\u3053", 14, 15, CJKTokenizer.DOUBLE_TOKEN_TYPE)
                                      };
            CheckCjkTokenReusable(analyzer, str, out_tokens2);
        }

        /*
         * LUCENE-2207: wrong offset calculated by end() 
         */
        [Test]
        public void TestFinalOffset()
        {
            CheckCjkToken("あい", new TestToken[]
                                    {
                                        NewToken("あい", 0, 2, CJKTokenizer.DOUBLE_TOKEN_TYPE)
                                    });
            CheckCjkToken("あい   ", new TestToken[]
                                       {
                                           NewToken("あい", 0, 2, CJKTokenizer.DOUBLE_TOKEN_TYPE)
                                       });
            CheckCjkToken("test", new TestToken[]
                                      {
                                          NewToken("test", 0, 4, CJKTokenizer.SINGLE_TOKEN_TYPE)
                                      });
            CheckCjkToken("test   ", new TestToken[]
                                         {
                                             NewToken("test", 0, 4, CJKTokenizer.SINGLE_TOKEN_TYPE)
                                         });
            CheckCjkToken("あいtest", new TestToken[]
                                        {
                                            NewToken("あい", 0, 2, CJKTokenizer.DOUBLE_TOKEN_TYPE),
                                            NewToken("test", 2, 6, CJKTokenizer.SINGLE_TOKEN_TYPE)
                                        });
            CheckCjkToken("testあい    ", new TestToken[]
                                            {
                                                NewToken("test", 0, 4, CJKTokenizer.SINGLE_TOKEN_TYPE),
                                                NewToken("あい", 4, 6, CJKTokenizer.DOUBLE_TOKEN_TYPE)
                                            });
        }
    }
}
