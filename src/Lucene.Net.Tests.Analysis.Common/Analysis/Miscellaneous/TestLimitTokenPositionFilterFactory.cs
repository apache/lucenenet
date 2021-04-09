// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Util;
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

    public class TestLimitTokenPositionFilterFactory : BaseTokenStreamFactoryTestCase
    {

        [Test]
        public virtual void TestMaxPosition1()
        {
            foreach (bool consumeAll in new bool[] { true, false })
            {
                TextReader reader = new StringReader("A1 B2 C3 D4 E5 F6");
                MockTokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                // if we are consuming all tokens, we can use the checks, otherwise we can't
                tokenizer.EnableChecks = consumeAll;
                TokenStream stream = tokenizer;
                stream = TokenFilterFactory("LimitTokenPosition", LimitTokenPositionFilterFactory.MAX_TOKEN_POSITION_KEY, "1", LimitTokenPositionFilterFactory.CONSUME_ALL_TOKENS_KEY, Convert.ToString(consumeAll)).Create(stream);
                AssertTokenStreamContents(stream, new string[] { "A1" });
            }
        }

        [Test]
        public virtual void TestMissingParam()
        {
            try
            {
                TokenFilterFactory("LimitTokenPosition");
                fail();
            }
            catch (Exception e) when (e.IsIllegalArgumentException())
            {
                assertTrue("exception doesn't mention param: " + e.Message, 0 < e.Message.IndexOf(LimitTokenPositionFilterFactory.MAX_TOKEN_POSITION_KEY, StringComparison.Ordinal));
            }
        }

        [Test]
        public virtual void TestMaxPosition1WithShingles()
        {
            foreach (bool consumeAll in new bool[] { true, false })
            {
                TextReader reader = new StringReader("one two three four five");
                MockTokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
                // if we are consuming all tokens, we can use the checks, otherwise we can't
                tokenizer.EnableChecks = consumeAll;
                TokenStream stream = tokenizer;
                stream = TokenFilterFactory("Shingle", "minShingleSize", "2", "maxShingleSize", "3", "outputUnigrams", "true").Create(stream);
                stream = TokenFilterFactory("LimitTokenPosition", LimitTokenPositionFilterFactory.MAX_TOKEN_POSITION_KEY, "1", LimitTokenPositionFilterFactory.CONSUME_ALL_TOKENS_KEY, Convert.ToString(consumeAll)).Create(stream);
                AssertTokenStreamContents(stream, new string[] { "one", "one two", "one two three" });
            }
        }

        [Test]
        public virtual void TestConsumeAllTokens()
        {
            TextReader reader = new StringReader("A1 B2 C3 D4 E5 F6");
            TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            stream = TokenFilterFactory("LimitTokenPosition", "maxTokenPosition", "3", "consumeAllTokens", "true").Create(stream);
            AssertTokenStreamContents(stream, new string[] { "A1", "B2", "C3" });
        }

        /// <summary>
        /// Test that bogus arguments result in exception
        /// </summary>
        [Test]
        public virtual void TestBogusArguments()
        {
            try
            {
                TokenFilterFactory("LimitTokenPosition", "maxTokenPosition", "3", "bogusArg", "bogusValue");
                fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                assertTrue(expected.Message.Contains("Unknown parameters"));
            }
        }
    }
}