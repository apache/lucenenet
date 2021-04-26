// Lucene version compatibility level 4.8.1
using System.Text;
using Lucene.Net.Analysis.Util;
using NUnit.Framework;
using System;
using System.IO;
using Reader = System.IO.TextReader;

namespace Lucene.Net.Analysis.Standard
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
    /// Simple tests to ensure the standard lucene factories are working.
    /// </summary>
    public class TestStandardFactories : BaseTokenStreamFactoryTestCase
    {
        /// <summary>
        /// Test StandardTokenizerFactory
        /// </summary>
        [Test]
        public virtual void TestStandardTokenizer()
        {
            Reader reader = new StringReader("Wha\u0301t's this thing do?");
            TokenStream stream = TokenizerFactory("Standard").Create(reader);
            AssertTokenStreamContents(stream, new string[] { "Wha\u0301t's", "this", "thing", "do" });
        }

        [Test]
        public virtual void TestStandardTokenizerMaxTokenLength()
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < 100; ++i)
            {
                builder.Append("abcdefg"); // 7 * 100 = 700 char "word"
            }
            string longWord = builder.ToString();
            string content = "one two three " + longWord + " four five six";
            Reader reader = new StringReader(content);
            Tokenizer stream = TokenizerFactory("Standard", "maxTokenLength", "1000").Create(reader);
            AssertTokenStreamContents(stream, new string[] { "one", "two", "three", longWord, "four", "five", "six" });
        }

        /// <summary>
        /// Test ClassicTokenizerFactory
        /// </summary>
        [Test]
        public virtual void TestClassicTokenizer()
        {
            Reader reader = new StringReader("What's this thing do?");
            TokenStream stream = TokenizerFactory("Classic").Create(reader);
            AssertTokenStreamContents(stream, new string[] { "What's", "this", "thing", "do" });
        }

        [Test]
        public virtual void TestClassicTokenizerMaxTokenLength()
        {
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < 100; ++i)
            {
                builder.Append("abcdefg"); // 7 * 100 = 700 char "word"
            }
            string longWord = builder.ToString();
            string content = "one two three " + longWord + " four five six";
            Reader reader = new StringReader(content);
            Tokenizer stream = TokenizerFactory("Classic", "maxTokenLength", "1000").Create(reader);
            AssertTokenStreamContents(stream, new string[] { "one", "two", "three", longWord, "four", "five", "six" });
        }

        /// <summary>
        /// Test ClassicFilterFactory
        /// </summary>
        [Test]
        public virtual void TestStandardFilter()
        {
            Reader reader = new StringReader("What's this thing do?");
            TokenStream stream = TokenizerFactory("Classic").Create(reader);
            stream = TokenFilterFactory("Classic").Create(stream);
            AssertTokenStreamContents(stream, new string[] { "What", "this", "thing", "do" });
        }

        /// <summary>
        /// Test KeywordTokenizerFactory
        /// </summary>
        [Test]
        public virtual void TestKeywordTokenizer()
        {
            Reader reader = new StringReader("What's this thing do?");
            TokenStream stream = TokenizerFactory("Keyword").Create(reader);
            AssertTokenStreamContents(stream, new string[] { "What's this thing do?" });
        }

        /// <summary>
        /// Test WhitespaceTokenizerFactory
        /// </summary>
        [Test]
        public virtual void TestWhitespaceTokenizer()
        {
            Reader reader = new StringReader("What's this thing do?");
            TokenStream stream = TokenizerFactory("Whitespace").Create(reader);
            AssertTokenStreamContents(stream, new string[] { "What's", "this", "thing", "do?" });
        }

        /// <summary>
        /// Test LetterTokenizerFactory
        /// </summary>
        [Test]
        public virtual void TestLetterTokenizer()
        {
            Reader reader = new StringReader("What's this thing do?");
            TokenStream stream = TokenizerFactory("Letter").Create(reader);
            AssertTokenStreamContents(stream, new string[] { "What", "s", "this", "thing", "do" });
        }

        /// <summary>
        /// Test LowerCaseTokenizerFactory
        /// </summary>
        [Test]
        public virtual void TestLowerCaseTokenizer()
        {
            Reader reader = new StringReader("What's this thing do?");
            TokenStream stream = TokenizerFactory("LowerCase").Create(reader);
            AssertTokenStreamContents(stream, new string[] { "what", "s", "this", "thing", "do" });
        }

        /// <summary>
        /// Ensure the ASCIIFoldingFilterFactory works
        /// </summary>
        [Test]
        public virtual void TestASCIIFolding()
        {
            Reader reader = new StringReader("Česká");
            TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            stream = TokenFilterFactory("ASCIIFolding").Create(stream);
            AssertTokenStreamContents(stream, new string[] { "Ceska" });
        }

        /// <summary>
        /// Test that bogus arguments result in exception </summary>
        [Test]
        public virtual void TestBogusArguments()
        {
            try
            {
                TokenizerFactory("Standard", "bogusArg", "bogusValue");
                fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                assertTrue(expected.Message.Contains("Unknown parameters"));
            }

            try
            {
                TokenizerFactory("Classic", "bogusArg", "bogusValue");
                fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                assertTrue(expected.Message.Contains("Unknown parameters"));
            }

            try
            {
                TokenizerFactory("Whitespace", "bogusArg", "bogusValue");
                fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                assertTrue(expected.Message.Contains("Unknown parameters"));
            }

            try
            {
                TokenizerFactory("Letter", "bogusArg", "bogusValue");
                fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                assertTrue(expected.Message.Contains("Unknown parameters"));
            }

            try
            {
                TokenizerFactory("LowerCase", "bogusArg", "bogusValue");
                fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                assertTrue(expected.Message.Contains("Unknown parameters"));
            }

            try
            {
                TokenFilterFactory("ASCIIFolding", "bogusArg", "bogusValue");
                fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                assertTrue(expected.Message.Contains("Unknown parameters"));
            }

            try
            {
                TokenFilterFactory("Standard", "bogusArg", "bogusValue");
                fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                assertTrue(expected.Message.Contains("Unknown parameters"));
            }

            try
            {
                TokenFilterFactory("Classic", "bogusArg", "bogusValue");
                fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                assertTrue(expected.Message.Contains("Unknown parameters"));
            }
        }
    }
}