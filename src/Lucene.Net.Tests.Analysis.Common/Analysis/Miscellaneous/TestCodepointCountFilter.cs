using Lucene.Net.Analysis.Core;
using Lucene.Net.Support;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.IO;

namespace Lucene.Net.Analysis.Miscellaneous
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

    public class TestCodepointCountFilter : BaseTokenStreamTestCase
    {
        [Test]
        public virtual void TestFilterWithPosIncr()
        {
            TokenStream stream = new MockTokenizer(new StringReader("short toolong evenmuchlongertext a ab toolong foo"), MockTokenizer.WHITESPACE, false);
            CodepointCountFilter filter = new CodepointCountFilter(TEST_VERSION_CURRENT, stream, 2, 6);
            AssertTokenStreamContents(filter, new string[] { "short", "ab", "foo" }, new int[] { 1, 4, 2 });
        }

        [Test]
        public virtual void TestEmptyTerm()
        {
            Analyzer a = new AnalyzerAnonymousInnerClassHelper(this);
            CheckOneTerm(a, "", "");
        }

        private class AnalyzerAnonymousInnerClassHelper : Analyzer
        {
            private readonly TestCodepointCountFilter outerInstance;

            public AnalyzerAnonymousInnerClassHelper(TestCodepointCountFilter outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            public override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                Tokenizer tokenizer = new KeywordTokenizer(reader);
                return new TokenStreamComponents(tokenizer, new CodepointCountFilter(TEST_VERSION_CURRENT, tokenizer, 0, 5));
            }
        }

        [Test]
        public virtual void TestRandomStrings()
        {
            for (int i = 0; i < 10000; i++)
            {
                string text = TestUtil.RandomUnicodeString(Random(), 100);
                int min = TestUtil.NextInt(Random(), 0, 100);
                int max = TestUtil.NextInt(Random(), 0, 100);
                int count = Character.CodePointCount(text, 0, text.Length);// text.codePointCount(0, text.Length);
                if (min > max)
                {
                    int temp = min;
                    min = max;
                    max = temp;
                }
                bool expected = count >= min && count <= max;
                TokenStream stream = new KeywordTokenizer(new StringReader(text));
                stream = new CodepointCountFilter(TEST_VERSION_CURRENT, stream, min, max);
                stream.Reset();
                assertEquals(expected, stream.IncrementToken());
                stream.End();
                stream.Dispose();
            }
        }

        /// <summary>
        /// checking the validity of constructor arguments
        /// </summary>
        [Test]
        public virtual void TestIllegalArguments()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                new CodepointCountFilter(TEST_VERSION_CURRENT, new MockTokenizer(new StringReader("accept only valid arguments"), MockTokenizer.WHITESPACE, false), 4, 1));
        }
    }
}