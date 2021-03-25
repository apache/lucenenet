// Lucene version compatibility level 4.8.1
using Lucene.Net.Util;
using NUnit.Framework;
using System;

namespace Lucene.Net.Analysis.Cjk
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

    /// @deprecated Remove when CJKTokenizer is removed (5.0) 
    [Obsolete("Remove when CJKTokenizer is removed (5.0)")]
    public class TestCJKTokenizer : BaseTokenStreamTestCase
    {

        internal sealed class TestToken
        {
            internal string termText;
            internal int start;
            internal int end;
            internal string type;
        }

        internal virtual TestToken newToken(string termText, int start, int end, int type)
        {
            TestToken token = new TestToken();
            token.termText = termText;
            token.type = CJKTokenizer.TOKEN_TYPE_NAMES[type];
            token.start = start;
            token.end = end;
            return token;
        }

        internal virtual void checkCJKToken(string str, TestToken[] out_tokens)
        {
            Analyzer analyzer = new CJKAnalyzer(LuceneVersion.LUCENE_30);
            string[] terms = new string[out_tokens.Length];
            int[] startOffsets = new int[out_tokens.Length];
            int[] endOffsets = new int[out_tokens.Length];
            string[] types = new string[out_tokens.Length];
            for (int i = 0; i < out_tokens.Length; i++)
            {
                terms[i] = out_tokens[i].termText;
                startOffsets[i] = out_tokens[i].start;
                endOffsets[i] = out_tokens[i].end;
                types[i] = out_tokens[i].type;
            }
            AssertAnalyzesTo(analyzer, str, terms, startOffsets, endOffsets, types, null);
        }

        internal virtual void checkCJKTokenReusable(Analyzer a, string str, TestToken[] out_tokens)
        {
            Analyzer analyzer = new CJKAnalyzer(LuceneVersion.LUCENE_30);
            string[] terms = new string[out_tokens.Length];
            int[] startOffsets = new int[out_tokens.Length];
            int[] endOffsets = new int[out_tokens.Length];
            string[] types = new string[out_tokens.Length];
            for (int i = 0; i < out_tokens.Length; i++)
            {
                terms[i] = out_tokens[i].termText;
                startOffsets[i] = out_tokens[i].start;
                endOffsets[i] = out_tokens[i].end;
                types[i] = out_tokens[i].type;
            }
            AssertAnalyzesTo(analyzer, str, terms, startOffsets, endOffsets, types, null);
        }

        [Test]
        public virtual void TestJa1()
        {
            string str = "\u4e00\u4e8c\u4e09\u56db\u4e94\u516d\u4e03\u516b\u4e5d\u5341";

            TestToken[] out_tokens = new TestToken[] { newToken("\u4e00\u4e8c", 0, 2, CJKTokenizer.DOUBLE_TOKEN_TYPE), newToken("\u4e8c\u4e09", 1, 3, CJKTokenizer.DOUBLE_TOKEN_TYPE), newToken("\u4e09\u56db", 2, 4, CJKTokenizer.DOUBLE_TOKEN_TYPE), newToken("\u56db\u4e94", 3, 5, CJKTokenizer.DOUBLE_TOKEN_TYPE), newToken("\u4e94\u516d", 4, 6, CJKTokenizer.DOUBLE_TOKEN_TYPE), newToken("\u516d\u4e03", 5, 7, CJKTokenizer.DOUBLE_TOKEN_TYPE), newToken("\u4e03\u516b", 6, 8, CJKTokenizer.DOUBLE_TOKEN_TYPE), newToken("\u516b\u4e5d", 7, 9, CJKTokenizer.DOUBLE_TOKEN_TYPE), newToken("\u4e5d\u5341", 8, 10, CJKTokenizer.DOUBLE_TOKEN_TYPE) };
            checkCJKToken(str, out_tokens);
        }

        [Test]
        public virtual void TestJa2()
        {
            string str = "\u4e00 \u4e8c\u4e09\u56db \u4e94\u516d\u4e03\u516b\u4e5d \u5341";

            TestToken[] out_tokens = new TestToken[] { newToken("\u4e00", 0, 1, CJKTokenizer.DOUBLE_TOKEN_TYPE), newToken("\u4e8c\u4e09", 2, 4, CJKTokenizer.DOUBLE_TOKEN_TYPE), newToken("\u4e09\u56db", 3, 5, CJKTokenizer.DOUBLE_TOKEN_TYPE), newToken("\u4e94\u516d", 6, 8, CJKTokenizer.DOUBLE_TOKEN_TYPE), newToken("\u516d\u4e03", 7, 9, CJKTokenizer.DOUBLE_TOKEN_TYPE), newToken("\u4e03\u516b", 8, 10, CJKTokenizer.DOUBLE_TOKEN_TYPE), newToken("\u516b\u4e5d", 9, 11, CJKTokenizer.DOUBLE_TOKEN_TYPE), newToken("\u5341", 12, 13, CJKTokenizer.DOUBLE_TOKEN_TYPE) };
            checkCJKToken(str, out_tokens);
        }

        [Test]
        public virtual void TestC()
        {
            string str = "abc defgh ijklmn opqrstu vwxy z";

            TestToken[] out_tokens = new TestToken[] { newToken("abc", 0, 3, CJKTokenizer.SINGLE_TOKEN_TYPE), newToken("defgh", 4, 9, CJKTokenizer.SINGLE_TOKEN_TYPE), newToken("ijklmn", 10, 16, CJKTokenizer.SINGLE_TOKEN_TYPE), newToken("opqrstu", 17, 24, CJKTokenizer.SINGLE_TOKEN_TYPE), newToken("vwxy", 25, 29, CJKTokenizer.SINGLE_TOKEN_TYPE), newToken("z", 30, 31, CJKTokenizer.SINGLE_TOKEN_TYPE) };
            checkCJKToken(str, out_tokens);
        }

        [Test]
        public virtual void TestMix()
        {
            string str = "\u3042\u3044\u3046\u3048\u304aabc\u304b\u304d\u304f\u3051\u3053";

            TestToken[] out_tokens = new TestToken[] { newToken("\u3042\u3044", 0, 2, CJKTokenizer.DOUBLE_TOKEN_TYPE), newToken("\u3044\u3046", 1, 3, CJKTokenizer.DOUBLE_TOKEN_TYPE), newToken("\u3046\u3048", 2, 4, CJKTokenizer.DOUBLE_TOKEN_TYPE), newToken("\u3048\u304a", 3, 5, CJKTokenizer.DOUBLE_TOKEN_TYPE), newToken("abc", 5, 8, CJKTokenizer.SINGLE_TOKEN_TYPE), newToken("\u304b\u304d", 8, 10, CJKTokenizer.DOUBLE_TOKEN_TYPE), newToken("\u304d\u304f", 9, 11, CJKTokenizer.DOUBLE_TOKEN_TYPE), newToken("\u304f\u3051", 10, 12, CJKTokenizer.DOUBLE_TOKEN_TYPE), newToken("\u3051\u3053", 11, 13, CJKTokenizer.DOUBLE_TOKEN_TYPE) };
            checkCJKToken(str, out_tokens);
        }

        [Test]
        public virtual void TestMix2()
        {
            string str = "\u3042\u3044\u3046\u3048\u304aab\u3093c\u304b\u304d\u304f\u3051 \u3053";

            TestToken[] out_tokens = new TestToken[] { newToken("\u3042\u3044", 0, 2, CJKTokenizer.DOUBLE_TOKEN_TYPE), newToken("\u3044\u3046", 1, 3, CJKTokenizer.DOUBLE_TOKEN_TYPE), newToken("\u3046\u3048", 2, 4, CJKTokenizer.DOUBLE_TOKEN_TYPE), newToken("\u3048\u304a", 3, 5, CJKTokenizer.DOUBLE_TOKEN_TYPE), newToken("ab", 5, 7, CJKTokenizer.SINGLE_TOKEN_TYPE), newToken("\u3093", 7, 8, CJKTokenizer.DOUBLE_TOKEN_TYPE), newToken("c", 8, 9, CJKTokenizer.SINGLE_TOKEN_TYPE), newToken("\u304b\u304d", 9, 11, CJKTokenizer.DOUBLE_TOKEN_TYPE), newToken("\u304d\u304f", 10, 12, CJKTokenizer.DOUBLE_TOKEN_TYPE), newToken("\u304f\u3051", 11, 13, CJKTokenizer.DOUBLE_TOKEN_TYPE), newToken("\u3053", 14, 15, CJKTokenizer.DOUBLE_TOKEN_TYPE) };
            checkCJKToken(str, out_tokens);
        }

        [Test]
        public virtual void TestSingleChar()
        {
            string str = "\u4e00";

            TestToken[] out_tokens = new TestToken[] { newToken("\u4e00", 0, 1, CJKTokenizer.DOUBLE_TOKEN_TYPE) };
            checkCJKToken(str, out_tokens);
        }

        /*
         * Full-width text is normalized to half-width 
         */
        [Test]
        public virtual void TestFullWidth()
        {
            string str = "Ｔｅｓｔ １２３４";
            TestToken[] out_tokens = new TestToken[] { newToken("test", 0, 4, CJKTokenizer.SINGLE_TOKEN_TYPE), newToken("1234", 5, 9, CJKTokenizer.SINGLE_TOKEN_TYPE) };
            checkCJKToken(str, out_tokens);
        }

        /*
         * Non-english text (not just CJK) is treated the same as CJK: C1C2 C2C3 
         */
        [Test]
        public virtual void TestNonIdeographic()
        {
            string str = "\u4e00 روبرت موير";
            TestToken[] out_tokens = new TestToken[] { newToken("\u4e00", 0, 1, CJKTokenizer.DOUBLE_TOKEN_TYPE), newToken("رو", 2, 4, CJKTokenizer.DOUBLE_TOKEN_TYPE), newToken("وب", 3, 5, CJKTokenizer.DOUBLE_TOKEN_TYPE), newToken("بر", 4, 6, CJKTokenizer.DOUBLE_TOKEN_TYPE), newToken("رت", 5, 7, CJKTokenizer.DOUBLE_TOKEN_TYPE), newToken("مو", 8, 10, CJKTokenizer.DOUBLE_TOKEN_TYPE), newToken("وي", 9, 11, CJKTokenizer.DOUBLE_TOKEN_TYPE), newToken("ير", 10, 12, CJKTokenizer.DOUBLE_TOKEN_TYPE) };
            checkCJKToken(str, out_tokens);
        }

        /*
         * Non-english text with nonletters (non-spacing marks,etc) is treated as C1C2 C2C3,
         * except for words are split around non-letters.
         */
        [Test]
        public virtual void TestNonIdeographicNonLetter()
        {
            string str = "\u4e00 رُوبرت موير";
            TestToken[] out_tokens = new TestToken[] { newToken("\u4e00", 0, 1, CJKTokenizer.DOUBLE_TOKEN_TYPE), newToken("ر", 2, 3, CJKTokenizer.DOUBLE_TOKEN_TYPE), newToken("وب", 4, 6, CJKTokenizer.DOUBLE_TOKEN_TYPE), newToken("بر", 5, 7, CJKTokenizer.DOUBLE_TOKEN_TYPE), newToken("رت", 6, 8, CJKTokenizer.DOUBLE_TOKEN_TYPE), newToken("مو", 9, 11, CJKTokenizer.DOUBLE_TOKEN_TYPE), newToken("وي", 10, 12, CJKTokenizer.DOUBLE_TOKEN_TYPE), newToken("ير", 11, 13, CJKTokenizer.DOUBLE_TOKEN_TYPE) };
            checkCJKToken(str, out_tokens);
        }

        [Test]
        public virtual void TestTokenStream()
        {
            Analyzer analyzer = new CJKAnalyzer(LuceneVersion.LUCENE_30);
            AssertAnalyzesTo(analyzer, "\u4e00\u4e01\u4e02", new string[] { "\u4e00\u4e01", "\u4e01\u4e02" });
        }

        [Test]
        public virtual void TestReusableTokenStream()
        {
            Analyzer analyzer = new CJKAnalyzer(LuceneVersion.LUCENE_30);
            string str = "\u3042\u3044\u3046\u3048\u304aabc\u304b\u304d\u304f\u3051\u3053";

            TestToken[] out_tokens = new TestToken[] { newToken("\u3042\u3044", 0, 2, CJKTokenizer.DOUBLE_TOKEN_TYPE), newToken("\u3044\u3046", 1, 3, CJKTokenizer.DOUBLE_TOKEN_TYPE), newToken("\u3046\u3048", 2, 4, CJKTokenizer.DOUBLE_TOKEN_TYPE), newToken("\u3048\u304a", 3, 5, CJKTokenizer.DOUBLE_TOKEN_TYPE), newToken("abc", 5, 8, CJKTokenizer.SINGLE_TOKEN_TYPE), newToken("\u304b\u304d", 8, 10, CJKTokenizer.DOUBLE_TOKEN_TYPE), newToken("\u304d\u304f", 9, 11, CJKTokenizer.DOUBLE_TOKEN_TYPE), newToken("\u304f\u3051", 10, 12, CJKTokenizer.DOUBLE_TOKEN_TYPE), newToken("\u3051\u3053", 11, 13, CJKTokenizer.DOUBLE_TOKEN_TYPE) };
            checkCJKTokenReusable(analyzer, str, out_tokens);

            str = "\u3042\u3044\u3046\u3048\u304aab\u3093c\u304b\u304d\u304f\u3051 \u3053";
            TestToken[] out_tokens2 = new TestToken[] { newToken("\u3042\u3044", 0, 2, CJKTokenizer.DOUBLE_TOKEN_TYPE), newToken("\u3044\u3046", 1, 3, CJKTokenizer.DOUBLE_TOKEN_TYPE), newToken("\u3046\u3048", 2, 4, CJKTokenizer.DOUBLE_TOKEN_TYPE), newToken("\u3048\u304a", 3, 5, CJKTokenizer.DOUBLE_TOKEN_TYPE), newToken("ab", 5, 7, CJKTokenizer.SINGLE_TOKEN_TYPE), newToken("\u3093", 7, 8, CJKTokenizer.DOUBLE_TOKEN_TYPE), newToken("c", 8, 9, CJKTokenizer.SINGLE_TOKEN_TYPE), newToken("\u304b\u304d", 9, 11, CJKTokenizer.DOUBLE_TOKEN_TYPE), newToken("\u304d\u304f", 10, 12, CJKTokenizer.DOUBLE_TOKEN_TYPE), newToken("\u304f\u3051", 11, 13, CJKTokenizer.DOUBLE_TOKEN_TYPE), newToken("\u3053", 14, 15, CJKTokenizer.DOUBLE_TOKEN_TYPE) };
            checkCJKTokenReusable(analyzer, str, out_tokens2);
        }

        /// <summary>
        /// LUCENE-2207: wrong offset calculated by end() 
        /// </summary>
        [Test]
        public virtual void TestFinalOffset()
        {
            checkCJKToken("あい", new TestToken[] { newToken("あい", 0, 2, CJKTokenizer.DOUBLE_TOKEN_TYPE) });
            checkCJKToken("あい   ", new TestToken[] { newToken("あい", 0, 2, CJKTokenizer.DOUBLE_TOKEN_TYPE) });
            checkCJKToken("test", new TestToken[] { newToken("test", 0, 4, CJKTokenizer.SINGLE_TOKEN_TYPE) });
            checkCJKToken("test   ", new TestToken[] { newToken("test", 0, 4, CJKTokenizer.SINGLE_TOKEN_TYPE) });
            checkCJKToken("あいtest", new TestToken[] { newToken("あい", 0, 2, CJKTokenizer.DOUBLE_TOKEN_TYPE), newToken("test", 2, 6, CJKTokenizer.SINGLE_TOKEN_TYPE) });
            checkCJKToken("testあい    ", new TestToken[] { newToken("test", 0, 4, CJKTokenizer.SINGLE_TOKEN_TYPE), newToken("あい", 4, 6, CJKTokenizer.DOUBLE_TOKEN_TYPE) });
        }

        /// <summary>
        /// blast some random strings through the analyzer </summary>
        [Test]
        public virtual void TestRandomStrings()
        {
            CheckRandomData(Random, new CJKAnalyzer(LuceneVersion.LUCENE_30), 10000 * RandomMultiplier);
        }
    }
}