// LUCENENET specific: Albanian is not supported by Apache Lucene 4.8; this analyzer is a .NET-only addition.
using Lucene.Net.Analysis.Util;
using NUnit.Framework;
using System;
using System.IO;

namespace Lucene.Net.Analysis.Sq
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
    /// Simple tests to ensure the Albanian filter factories are working.
    /// </summary>
    public class TestAlbanianFilterFactories : BaseTokenStreamFactoryTestCase
    {
        /// <summary>
        /// Test AlbanianNormalizationFilterFactory
        /// </summary>
        [Test]
        public virtual void TestNormalization()
        {
            TextReader reader = new StringReader("Kur'ani është");
            TokenStream stream = TokenizerFactory("Standard").Create(reader);
            stream = TokenFilterFactory("AlbanianNormalization").Create(stream);
            AssertTokenStreamContents(stream, new string[] { "Kurani", "eshte" });
        }

        /// <summary>
        /// Test AlbanianLightStemFilterFactory
        /// </summary>
        [Test]
        public virtual void TestStemming()
        {
            TextReader reader = new StringReader("durimtaret");
            Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            TokenStream stream = TokenFilterFactory("AlbanianLightStem").Create(tokenizer);
            AssertTokenStreamContents(stream, new string[] { "dur" });
        }

        /// <summary>
        /// Test that bogus arguments result in exception </summary>
        [Test]
        public virtual void TestBogusArguments()
        {
            try
            {
                TokenFilterFactory("AlbanianNormalization", "bogusArg", "bogusValue");
                fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                assertTrue(expected.Message.Contains("Unknown parameters"));
            }

            try
            {
                TokenFilterFactory("AlbanianLightStem", "bogusArg", "bogusValue");
                fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                assertTrue(expected.Message.Contains("Unknown parameters"));
            }
        }
    }
}
