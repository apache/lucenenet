using System;
using System.IO;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Support;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Analysis.Util
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
    /// Testcase for <seealso cref="CharTokenizer"/> subclasses
    /// </summary>
    [TestFixture]
    public class TestCharTokenizers : BaseTokenStreamTestCase
    {

        /*
         * test to read surrogate pairs without loosing the pairing 
         * if the surrogate pair is at the border of the internal IO buffer
         */
        [Test]
        public virtual void TestReadSupplementaryChars()
        {
            var builder = new StringBuilder();
            // create random input
            var num = 1024 + Random().Next(1024);
            num *= RANDOM_MULTIPLIER;
            for (var i = 1; i < num; i++)
            {
                builder.Append("\ud801\udc1cabc");
                if ((i % 10) == 0)
                {
                    builder.Append(" ");
                }
            }
            // internal buffer size is 1024 make sure we have a surrogate pair right at the border
            builder.Insert(1023, "\ud801\udc1c");
            var tokenizer = new LowerCaseTokenizer(TEST_VERSION_CURRENT, new StringReader(builder.ToString()));
            AssertTokenStreamContents(tokenizer, builder.ToString().ToLowerInvariant().Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
        }

        /*
       * test to extend the buffer TermAttribute buffer internally. If the internal
       * alg that extends the size of the char array only extends by 1 char and the
       * next char to be filled in is a supplementary codepoint (using 2 chars) an
       * index out of bound exception is triggered.
       */
        [Test]
        public virtual void TestExtendCharBuffer()
        {
            for (var i = 0; i < 40; i++)
            {
                var builder = new StringBuilder();
                for (int j = 0; j < 1 + i; j++)
                {
                    builder.Append("a");
                }
                builder.Append("\ud801\udc1cabc");
                var tokenizer = new LowerCaseTokenizer(TEST_VERSION_CURRENT, new StringReader(builder.ToString()));
                AssertTokenStreamContents(tokenizer, new[] { builder.ToString().ToLowerInvariant() });
            }
        }

        /*
         * tests the max word length of 255 - tokenizer will split at the 255 char no matter what happens
         */
        [Test]
        public virtual void TestMaxWordLength()
        {
            var builder = new StringBuilder();

            for (var i = 0; i < 255; i++)
            {
                builder.Append("A");
            }
            var tokenizer = new LowerCaseTokenizer(TEST_VERSION_CURRENT, new StringReader(builder.ToString() + builder.ToString()));
            AssertTokenStreamContents(tokenizer, new[] { builder.ToString().ToLowerInvariant(), builder.ToString().ToLowerInvariant() });
        }

        /*
         * tests the max word length of 255 with a surrogate pair at position 255
         */
        [Test]
        public virtual void TestMaxWordLengthWithSupplementary()
        {
            var builder = new StringBuilder();

            for (var i = 0; i < 254; i++)
            {
                builder.Append("A");
            }
            builder.Append("\ud801\udc1c");
            var tokenizer = new LowerCaseTokenizer(TEST_VERSION_CURRENT, new StringReader(builder.ToString() + builder.ToString()));
            AssertTokenStreamContents(tokenizer, new[] { builder.ToString().ToLowerInvariant(), builder.ToString().ToLowerInvariant() });
        }

        // LUCENE-3642: normalize SMP->BMP and check that offsets are correct
        [Test]
        public virtual void TestCrossPlaneNormalization()
        {
            var analyzer = new AnalyzerAnonymousInnerClassHelper();
            var num = 1000 * RANDOM_MULTIPLIER;
            for (var i = 0; i < num; i++)
            {
                var s = TestUtil.RandomUnicodeString(Random());
                var ts = analyzer.TokenStream("foo", s);
                try
                {
                    ts.Reset();
                    var offsetAtt = ts.AddAttribute<IOffsetAttribute>();
                    while (ts.IncrementToken())
                    {
                        var highlightedText = s.Substring(offsetAtt.StartOffset, offsetAtt.EndOffset - offsetAtt.StartOffset);
                        for (int j = 0, cp = 0; j < highlightedText.Length; j += Character.CharCount(cp))
                        {
                            cp = char.ConvertToUtf32(highlightedText, j);
                            assertTrue("non-letter:" + cp.ToString("x"), Character.IsLetter(cp));
                        }
                    }
                    ts.End();
                }
                finally
                {
                    IOUtils.CloseWhileHandlingException(ts);
                }
            }
            // just for fun
            CheckRandomData(Random(), analyzer, num);
        }

        private sealed class AnalyzerAnonymousInnerClassHelper : Analyzer
        {
            protected override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                Tokenizer tokenizer = new LetterTokenizerAnonymousInnerClassHelper(TEST_VERSION_CURRENT, reader);
                return new TokenStreamComponents(tokenizer, tokenizer);
            }

            private sealed class LetterTokenizerAnonymousInnerClassHelper : LetterTokenizer
            {
                public LetterTokenizerAnonymousInnerClassHelper(LuceneVersion TEST_VERSION_CURRENT, TextReader reader)
                    : base(TEST_VERSION_CURRENT, reader)
                {
                }

                protected override int Normalize(int c)
                {
                    if (c > 0xffff)
                    {
                        return 'δ';
                    }
                    else
                    {
                        return c;
                    }
                }
            }
        }

        // LUCENE-3642: normalize BMP->SMP and check that offsets are correct
        [Test]
        public virtual void TestCrossPlaneNormalization2()
        {
            var analyzer = new AnalyzerAnonymousInnerClassHelper2();
            var num = 1000 * RANDOM_MULTIPLIER;
            for (var i = 0; i < num; i++)
            {
                var s = TestUtil.RandomUnicodeString(Random());
                var ts = analyzer.TokenStream("foo", s);
                try
                {
                    ts.Reset();
                    var offsetAtt = ts.AddAttribute<IOffsetAttribute>();
                    while (ts.IncrementToken())
                    {
                        string highlightedText = s.Substring(offsetAtt.StartOffset, offsetAtt.EndOffset - offsetAtt.StartOffset);
                        for (int j = 0, cp = 0; j < highlightedText.Length; j += Character.CharCount(cp))
                        {
                            cp = char.ConvertToUtf32(highlightedText, j);
                            assertTrue("non-letter:" + cp.ToString("x"), Character.IsLetter(cp));
                        }
                    }
                    ts.End();
                }
                finally
                {
                    IOUtils.CloseWhileHandlingException(ts);
                }
            }
            // just for fun
            CheckRandomData(Random(), analyzer, num);
        }

        private sealed class AnalyzerAnonymousInnerClassHelper2 : Analyzer
        {
            protected override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                Tokenizer tokenizer = new LetterTokenizerAnonymousInnerClassHelper2(TEST_VERSION_CURRENT, reader);
                return new TokenStreamComponents(tokenizer, tokenizer);
            }

            private sealed class LetterTokenizerAnonymousInnerClassHelper2 : LetterTokenizer
            {
                public LetterTokenizerAnonymousInnerClassHelper2(LuceneVersion TEST_VERSION_CURRENT, TextReader reader)
                    : base(TEST_VERSION_CURRENT, reader)
                {
                }

                protected override int Normalize(int c)
                {
                    if (c <= 0xffff)
                    {
                        return 0x1043C;
                    }
                    else
                    {
                        return c;
                    }
                }
            }
        }

        /// <summary>
        /// LUCENENET: Added this test as proof that making the IsTokenChar parameter a char
        /// is not going to work 100% of the time because of surrogate pairs.
        /// </summary>

        [Test]
        public virtual void TestSurrogates()
        {
            var analyzer = new AnalyzerAnonymousInnerClassHelper3();

            AssertAnalyzesTo(analyzer, "bar 123" + (char)55404 + (char)56321 + "34 5te 987", new string[] { "123𫀁34", "5", "987" });
            AssertAnalyzesTo(analyzer, "787 " + (char)55297 + (char)56388 + "6" + (char)55404 + (char)56321 + " art true 734", new string[] { "787", "𐑄6𫀁", "734" });
        }

        private sealed class AnalyzerAnonymousInnerClassHelper3 : Analyzer
        {
            public AnalyzerAnonymousInnerClassHelper3()
            { }

            protected override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                Tokenizer tokenizer = new NumberAndSurrogatePairTokenizer(TEST_VERSION_CURRENT, reader);
                return new TokenStreamComponents(tokenizer, tokenizer);
            }

            private sealed class NumberAndSurrogatePairTokenizer : CharTokenizer
            {
                public NumberAndSurrogatePairTokenizer(LuceneVersion matchVersion, TextReader reader)
                    : base(matchVersion, reader)
                {
                }

                protected override bool IsTokenChar(int c)
                {
                    if (char.IsNumber((char)c))
                    {
                        return true;
                    }

                    string character = char.ConvertFromUtf32(c);
                    return char.IsSurrogatePair(character, 0);
                }
            }
        }
    }
}