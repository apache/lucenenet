// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Util;
using NUnit.Framework;
using System;
using System.IO;

namespace Lucene.Net.Analysis.Hi
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
    /// Simple tests to ensure the Hindi filter Factories are working.
    /// </summary>
    public class TestHindiFilters : BaseTokenStreamFactoryTestCase
    {
        /// <summary>
        /// Test IndicNormalizationFilterFactory
        /// </summary>
        [Test]
        public virtual void TestIndicNormalizer()
        {
            TextReader reader = new StringReader("ত্‍ अाैर");
            TokenStream stream = TokenizerFactory("Standard").Create(reader);
            stream = TokenFilterFactory("IndicNormalization").Create(stream);
            AssertTokenStreamContents(stream, new string[] { "ৎ", "और" });
        }

        /// <summary>
        /// Test HindiNormalizationFilterFactory
        /// </summary>
        [Test]
        public virtual void TestHindiNormalizer()
        {
            TextReader reader = new StringReader("क़िताब");
            TokenStream stream = TokenizerFactory("Standard").Create(reader);
            stream = TokenFilterFactory("IndicNormalization").Create(stream);
            stream = TokenFilterFactory("HindiNormalization").Create(stream);
            AssertTokenStreamContents(stream, new string[] { "किताब" });
        }

        /// <summary>
        /// Test HindiStemFilterFactory
        /// </summary>
        [Test]
        public virtual void TestStemmer()
        {
            TextReader reader = new StringReader("किताबें");
            TokenStream stream = TokenizerFactory("Standard").Create(reader);
            stream = TokenFilterFactory("IndicNormalization").Create(stream);
            stream = TokenFilterFactory("HindiNormalization").Create(stream);
            stream = TokenFilterFactory("HindiStem").Create(stream);
            AssertTokenStreamContents(stream, new string[] { "किताब" });
        }

        /// <summary>
        /// Test that bogus arguments result in exception </summary>
        [Test]
        public virtual void TestBogusArguments()
        {
            try
            {
                TokenFilterFactory("IndicNormalization", "bogusArg", "bogusValue");
                fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                assertTrue(expected.Message.Contains("Unknown parameters"));
            }

            try
            {
                TokenFilterFactory("HindiNormalization", "bogusArg", "bogusValue");
                fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                assertTrue(expected.Message.Contains("Unknown parameters"));
            }

            try
            {
                TokenFilterFactory("HindiStem", "bogusArg", "bogusValue");
                fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                assertTrue(expected.Message.Contains("Unknown parameters"));
            }
        }
    }
}