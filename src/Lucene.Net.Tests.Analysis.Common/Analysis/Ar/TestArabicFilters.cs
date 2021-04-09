// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Util;
using NUnit.Framework;
using System;
using System.IO;

namespace Lucene.Net.Analysis.Ar
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
    /// Simple tests to ensure the Arabic filter Factories are working.
    /// </summary>
    public class TestArabicFilters : BaseTokenStreamFactoryTestCase
    {
        /// <summary>
        /// Test ArabicLetterTokenizerFactory </summary>
        /// @deprecated (3.1) Remove in Lucene 5.0 
        [Test]
        [Obsolete("(3.1) Remove in Lucene 5.0")]
        public virtual void TestTokenizer()
        {
            TextReader reader = new StringReader("الذين مَلكت أيمانكم");
            TokenStream stream = TokenizerFactory("ArabicLetter").Create(reader);
            AssertTokenStreamContents(stream, new string[] { "الذين", "مَلكت", "أيمانكم" });
        }

        /// <summary>
        /// Test ArabicNormalizationFilterFactory
        /// </summary>
        [Test]
        public virtual void TestNormalizer()
        {
            TextReader reader = new StringReader("الذين مَلكت أيمانكم");
            Tokenizer tokenizer = TokenizerFactory("Standard").Create(reader);
            TokenStream stream = TokenFilterFactory("ArabicNormalization").Create(tokenizer);
            AssertTokenStreamContents(stream, new string[] { "الذين", "ملكت", "ايمانكم" });
        }

        /// <summary>
        /// Test ArabicStemFilterFactory
        /// </summary>
        [Test]
        public virtual void TestStemmer()
        {
            TextReader reader = new StringReader("الذين مَلكت أيمانكم");
            Tokenizer tokenizer = TokenizerFactory("Standard").Create(reader);
            TokenStream stream = TokenFilterFactory("ArabicNormalization").Create(tokenizer);
            stream = TokenFilterFactory("ArabicStem").Create(stream);
            AssertTokenStreamContents(stream, new string[] { "ذين", "ملكت", "ايمانكم" });
        }

        /// <summary>
        /// Test PersianCharFilterFactory
        /// </summary>
        [Test]
        public virtual void TestPersianCharFilter()
        {
            TextReader reader = CharFilterFactory("Persian").Create(new StringReader("می‌خورد"));
            Tokenizer tokenizer = TokenizerFactory("Standard").Create(reader);
            AssertTokenStreamContents(tokenizer, new string[] { "می", "خورد" });
        }

        /// <summary>
        /// Test that bogus arguments result in exception </summary>
        [Test]
        public virtual void TestBogusArguments()
        {
            try
            {
                TokenFilterFactory("ArabicNormalization", "bogusArg", "bogusValue");
                fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                assertTrue(expected.Message.Contains("Unknown parameters"));
            }

            try
            {
                TokenFilterFactory("Arabicstem", "bogusArg", "bogusValue");
                fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                assertTrue(expected.Message.Contains("Unknown parameters"));
            }

            try
            {
                CharFilterFactory("Persian", "bogusArg", "bogusValue");
                fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                assertTrue(expected.Message.Contains("Unknown parameters"));
            }

            try
            {
                TokenizerFactory("ArabicLetter", "bogusArg", "bogusValue");
                fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                assertTrue(expected.Message.Contains("Unknown parameters"));
            }
        }
    }
}