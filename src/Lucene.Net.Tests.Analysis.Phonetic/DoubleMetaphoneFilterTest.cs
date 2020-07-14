using Lucene.Net.Analysis.Core;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.IO;

namespace Lucene.Net.Analysis.Phonetic
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

    public class DoubleMetaphoneFilterTest : BaseTokenStreamTestCase
    {
        [Test]
        public void TestSize4FalseInject()
        {
            TokenStream stream = new WhitespaceTokenizer(TEST_VERSION_CURRENT, new StringReader("international"));
            TokenStream filter = new DoubleMetaphoneFilter(stream, 4, false);
            AssertTokenStreamContents(filter, new String[] { "ANTR" });
        }

        [Test]
        public void TestSize4TrueInject()
        {
            TokenStream stream = new WhitespaceTokenizer(TEST_VERSION_CURRENT, new StringReader("international"));
            TokenStream filter = new DoubleMetaphoneFilter(stream, 4, true);
            AssertTokenStreamContents(filter, new String[] { "international", "ANTR" });
        }
        [Test]
        public void TestAlternateInjectFalse()
        {
            TokenStream stream = new WhitespaceTokenizer(TEST_VERSION_CURRENT, new StringReader("Kuczewski"));
            TokenStream filter = new DoubleMetaphoneFilter(stream, 4, false);
            AssertTokenStreamContents(filter, new String[] { "KSSK", "KXFS" });
        }
        [Test]
        public void TestSize8FalseInject()
        {
            TokenStream stream = new WhitespaceTokenizer(TEST_VERSION_CURRENT, new StringReader("international"));
            TokenStream filter = new DoubleMetaphoneFilter(stream, 8, false);
            AssertTokenStreamContents(filter, new String[] { "ANTRNXNL" });
        }
        [Test]
        public void TestNonConvertableStringsWithInject()
        {
            TokenStream stream = new WhitespaceTokenizer(TEST_VERSION_CURRENT, new StringReader("12345 #$%@#^%&"));
            TokenStream filter = new DoubleMetaphoneFilter(stream, 8, true);
            AssertTokenStreamContents(filter, new String[] { "12345", "#$%@#^%&" });
        }

        [Test]
        public void TestNonConvertableStringsWithoutInject()
        {
            TokenStream stream = new WhitespaceTokenizer(TEST_VERSION_CURRENT, new StringReader("12345 #$%@#^%&"));
            TokenStream filter = new DoubleMetaphoneFilter(stream, 8, false);
            AssertTokenStreamContents(filter, new String[] { "12345", "#$%@#^%&" });

            // should have something after the stream
            stream = new WhitespaceTokenizer(TEST_VERSION_CURRENT, new StringReader("12345 #$%@#^%& hello"));
            filter = new DoubleMetaphoneFilter(stream, 8, false);
            AssertTokenStreamContents(filter, new String[] { "12345", "#$%@#^%&", "HL" });
        }

        [Test]
        public void TestRandom()
        {
            int codeLen = TestUtil.NextInt32(Random, 1, 8);
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                return new TokenStreamComponents(tokenizer, new DoubleMetaphoneFilter(tokenizer, codeLen, false));
            });

            CheckRandomData(Random, a, 1000 * RandomMultiplier);

            Analyzer b = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                return new TokenStreamComponents(tokenizer, new DoubleMetaphoneFilter(tokenizer, codeLen, true));
            });

            CheckRandomData(Random, b, 1000 * RandomMultiplier);
        }

        [Test]
        public void TestEmptyTerm()
        {
            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
            {
                Tokenizer tokenizer = new KeywordTokenizer(reader);
                return new TokenStreamComponents(tokenizer, new DoubleMetaphoneFilter(tokenizer, 8, Random.nextBoolean()));
            });

            CheckOneTerm(a, "", "");
        }
    }
}
