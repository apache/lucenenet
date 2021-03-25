// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.IO;

namespace Lucene.Net.Analysis.Core
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

    public class TestAnalyzers : BaseTokenStreamTestCase
    {

        [Test]
        public virtual void TestSimple()
        {
            Analyzer a = new SimpleAnalyzer(TEST_VERSION_CURRENT);
            AssertAnalyzesTo(a, "foo bar FOO BAR", new string[] { "foo", "bar", "foo", "bar" });
            AssertAnalyzesTo(a, "foo      bar .  FOO <> BAR", new string[] { "foo", "bar", "foo", "bar" });
            AssertAnalyzesTo(a, "foo.bar.FOO.BAR", new string[] { "foo", "bar", "foo", "bar" });
            AssertAnalyzesTo(a, "U.S.A.", new string[] { "u", "s", "a" });
            AssertAnalyzesTo(a, "C++", new string[] { "c" });
            AssertAnalyzesTo(a, "B2B", new string[] { "b", "b" });
            AssertAnalyzesTo(a, "2B", new string[] { "b" });
            AssertAnalyzesTo(a, "\"QUOTED\" word", new string[] { "quoted", "word" });
        }

        [Test]
        public virtual void TestNull()
        {
            Analyzer a = new WhitespaceAnalyzer(TEST_VERSION_CURRENT);
            AssertAnalyzesTo(a, "foo bar FOO BAR", new string[] { "foo", "bar", "FOO", "BAR" });
            AssertAnalyzesTo(a, "foo      bar .  FOO <> BAR", new string[] { "foo", "bar", ".", "FOO", "<>", "BAR" });
            AssertAnalyzesTo(a, "foo.bar.FOO.BAR", new string[] { "foo.bar.FOO.BAR" });
            AssertAnalyzesTo(a, "U.S.A.", new string[] { "U.S.A." });
            AssertAnalyzesTo(a, "C++", new string[] { "C++" });
            AssertAnalyzesTo(a, "B2B", new string[] { "B2B" });
            AssertAnalyzesTo(a, "2B", new string[] { "2B" });
            AssertAnalyzesTo(a, "\"QUOTED\" word", new string[] { "\"QUOTED\"", "word" });
        }

        [Test]
        public virtual void TestStop()
        {
            Analyzer a = new StopAnalyzer(TEST_VERSION_CURRENT);
            AssertAnalyzesTo(a, "foo bar FOO BAR", new string[] { "foo", "bar", "foo", "bar" });
            AssertAnalyzesTo(a, "foo a bar such FOO THESE BAR", new string[] { "foo", "bar", "foo", "bar" });
        }
        internal virtual void VerifyPayload(TokenStream ts)
        {
            IPayloadAttribute payloadAtt = ts.GetAttribute<IPayloadAttribute>();
            ts.Reset();
            for (sbyte b = 1; ; b++)
            {
                bool hasNext = ts.IncrementToken();
                if (!hasNext)
                {
                    break;
                }
                // System.out.println("id="+System.identityHashCode(nextToken) + " " + t);
                // System.out.println("payload=" + (int)nextToken.getPayload().toByteArray()[0]);
                assertEquals(b, payloadAtt.Payload.Bytes[0]);
            }
        }

        // Make sure old style next() calls result in a new copy of payloads
        [Test]
        public virtual void TestPayloadCopy()
        {
            string s = "how now brown cow";
            TokenStream ts;
            ts = new WhitespaceTokenizer(TEST_VERSION_CURRENT, new StringReader(s));
            ts = new PayloadSetter(ts);
            VerifyPayload(ts);

            ts = new WhitespaceTokenizer(TEST_VERSION_CURRENT, new StringReader(s));
            ts = new PayloadSetter(ts);
            VerifyPayload(ts);
        }

        // LUCENE-1150: Just a compile time test, to ensure the
        // StandardAnalyzer constants remain publicly accessible
        public virtual void _TestStandardConstants()
        {
#pragma warning disable 219, 612, 618
            int x = StandardTokenizer.ALPHANUM;
            x = StandardTokenizer.APOSTROPHE;
            x = StandardTokenizer.ACRONYM;
            x = StandardTokenizer.COMPANY;
            x = StandardTokenizer.EMAIL;
            x = StandardTokenizer.HOST;
            x = StandardTokenizer.NUM;
            x = StandardTokenizer.CJ;
            string[] y = StandardTokenizer.TOKEN_TYPES;
#pragma warning restore 219, 612, 618
        }

        private static readonly Analyzer LOWERCASE_WHITESPACE_ANALYZER = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
        {
            Tokenizer tokenizer = new WhitespaceTokenizer(TEST_VERSION_CURRENT, reader);
            return new TokenStreamComponents(tokenizer, new LowerCaseFilter(TEST_VERSION_CURRENT, tokenizer));
        });

        private static readonly Analyzer UPPERCASE_WHITESPACE_ANALYZER = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
        {
            Tokenizer tokenizer = new WhitespaceTokenizer(TEST_VERSION_CURRENT, reader);
            return new TokenStreamComponents(tokenizer, new UpperCaseFilter(TEST_VERSION_CURRENT, tokenizer));
        });


        /// <summary>
        /// Test that LowercaseFilter handles entire unicode range correctly
        /// </summary>
        [Test]
        public virtual void TestLowerCaseFilter()
        {
            Analyzer a = LOWERCASE_WHITESPACE_ANALYZER;
            // BMP
            AssertAnalyzesTo(a, "AbaCaDabA", new string[] { "abacadaba" });
            // supplementary
            AssertAnalyzesTo(a, "\ud801\udc16\ud801\udc16\ud801\udc16\ud801\udc16", new string[] { "\ud801\udc3e\ud801\udc3e\ud801\udc3e\ud801\udc3e" });
            AssertAnalyzesTo(a, "AbaCa\ud801\udc16DabA", new string[] { "abaca\ud801\udc3edaba" });
            // unpaired lead surrogate
            AssertAnalyzesTo(a, "AbaC\uD801AdaBa", new string[] { "abac\uD801adaba" });
            // unpaired trail surrogate
            AssertAnalyzesTo(a, "AbaC\uDC16AdaBa", new string[] { "abac\uDC16adaba" });
        }

        /// <summary>
        /// Test that LowercaseFilter handles entire unicode range correctly
        /// </summary>
        [Test]
        public virtual void TestUpperCaseFilter()
        {
            Analyzer a = UPPERCASE_WHITESPACE_ANALYZER;
            // BMP
            AssertAnalyzesTo(a, "AbaCaDabA", new string[] { "ABACADABA" });
            // supplementary
            AssertAnalyzesTo(a, "\ud801\udc3e\ud801\udc3e\ud801\udc3e\ud801\udc3e", new string[] { "\ud801\udc16\ud801\udc16\ud801\udc16\ud801\udc16" });
            AssertAnalyzesTo(a, "AbaCa\ud801\udc3eDabA", new string[] { "ABACA\ud801\udc16DABA" });
            // unpaired lead surrogate
            AssertAnalyzesTo(a, "AbaC\uD801AdaBa", new string[] { "ABAC\uD801ADABA" });
            // unpaired trail surrogate
            AssertAnalyzesTo(a, "AbaC\uDC16AdaBa", new string[] { "ABAC\uDC16ADABA" });
        }


        /// <summary>
        /// Test that LowercaseFilter handles the lowercasing correctly if the term
        /// buffer has a trailing surrogate character leftover and the current term in
        /// the buffer ends with a corresponding leading surrogate.
        /// </summary>
        [Test]
        public virtual void TestLowerCaseFilterLowSurrogateLeftover()
        {
            // test if the limit of the termbuffer is correctly used with supplementary
            // chars
            WhitespaceTokenizer tokenizer = new WhitespaceTokenizer(TEST_VERSION_CURRENT, new StringReader("BogustermBogusterm\udc16"));
            LowerCaseFilter filter = new LowerCaseFilter(TEST_VERSION_CURRENT, tokenizer);
            AssertTokenStreamContents(filter, new string[] { "bogustermbogusterm\udc16" });
            filter.Reset();
            string highSurEndingUpper = "BogustermBoguster\ud801";
            string highSurEndingLower = "bogustermboguster\ud801";
            tokenizer.SetReader(new StringReader(highSurEndingUpper));
            AssertTokenStreamContents(filter, new string[] { highSurEndingLower });
            assertTrue(filter.HasAttribute<ICharTermAttribute>());
            char[] termBuffer = filter.GetAttribute<ICharTermAttribute>().Buffer;
            int length = highSurEndingLower.Length;
            assertEquals('\ud801', termBuffer[length - 1]);
        }

        [Test]
        public virtual void TestLowerCaseTokenizer()
        {
            StringReader reader = new StringReader("Tokenizer \ud801\udc1ctest");
            LowerCaseTokenizer tokenizer = new LowerCaseTokenizer(TEST_VERSION_CURRENT, reader);
            AssertTokenStreamContents(tokenizer, new string[] { "tokenizer", "\ud801\udc44test" });
        }

        [Test]
        [Obsolete("deprecated (3.1)")]
        public virtual void TestLowerCaseTokenizerBWCompat()
        {
            StringReader reader = new StringReader("Tokenizer \ud801\udc1ctest");
            LowerCaseTokenizer tokenizer = new LowerCaseTokenizer(LuceneVersion.LUCENE_30, reader);
            AssertTokenStreamContents(tokenizer, new string[] { "tokenizer", "test" });
        }

        [Test]
        public virtual void TestWhitespaceTokenizer()
        {
            StringReader reader = new StringReader("Tokenizer \ud801\udc1ctest");
            WhitespaceTokenizer tokenizer = new WhitespaceTokenizer(TEST_VERSION_CURRENT, reader);
            AssertTokenStreamContents(tokenizer, new string[] { "Tokenizer", "\ud801\udc1ctest" });
        }

        [Test]
        [Obsolete("deprecated (3.1)")]
        public virtual void TestWhitespaceTokenizerBWCompat()
        {
            StringReader reader = new StringReader("Tokenizer \ud801\udc1ctest");
            WhitespaceTokenizer tokenizer = new WhitespaceTokenizer(LuceneVersion.LUCENE_30, reader);
            AssertTokenStreamContents(tokenizer, new string[] { "Tokenizer", "\ud801\udc1ctest" });
        }

        /// <summary>
        /// blast some random strings through the analyzer </summary>
        [Test]
        public virtual void TestRandomStrings()
        {
            CheckRandomData(Random, new WhitespaceAnalyzer(TEST_VERSION_CURRENT), 1000 * RandomMultiplier);
            CheckRandomData(Random, new SimpleAnalyzer(TEST_VERSION_CURRENT), 1000 * RandomMultiplier);
            CheckRandomData(Random, new StopAnalyzer(TEST_VERSION_CURRENT), 1000 * RandomMultiplier);
        }

        /// <summary>
        /// blast some random large strings through the analyzer </summary>
        [Test]
        [Slow]
        public virtual void TestRandomHugeStrings()
        {
            Random random = Random;
            CheckRandomData(random, new WhitespaceAnalyzer(TEST_VERSION_CURRENT), 100 * RandomMultiplier, 8192);
            CheckRandomData(random, new SimpleAnalyzer(TEST_VERSION_CURRENT), 100 * RandomMultiplier, 8192);
            CheckRandomData(random, new StopAnalyzer(TEST_VERSION_CURRENT), 100 * RandomMultiplier, 8192);
        }
    }

    internal sealed class PayloadSetter : TokenFilter
    {
        internal IPayloadAttribute payloadAtt;
        public PayloadSetter(TokenStream input) : base(input)
        {
            p = new BytesRef(data, 0, 1);
            payloadAtt = AddAttribute<IPayloadAttribute>();
        }

        internal byte[] data = new byte[1];
        internal BytesRef p;

        public override bool IncrementToken()
        {
            bool hasNext = m_input.IncrementToken();
            if (!hasNext)
            {
                return false;
            }
            payloadAtt.Payload = p; // reuse the payload / byte[]
            data[0]++;
            return true;
        }
    }
}