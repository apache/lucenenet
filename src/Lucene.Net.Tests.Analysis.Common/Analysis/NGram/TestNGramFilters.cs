// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.IO;
using Reader = System.IO.TextReader;

namespace Lucene.Net.Analysis.NGram
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
    /// Simple tests to ensure the NGram filter factories are working.
    /// </summary>
    public class TestNGramFilters : BaseTokenStreamFactoryTestCase
    {
        /// <summary>
        /// Test NGramTokenizerFactory
        /// </summary>
        [Test]
        public virtual void TestNGramTokenizer()
        {
            Reader reader = new StringReader("test");
            TokenStream stream = TokenizerFactory("NGram").Create(reader);
            AssertTokenStreamContents(stream, new string[] { "t", "te", "e", "es", "s", "st", "t" });
        }

        /// <summary>
        /// Test NGramTokenizerFactory with min and max gram options
        /// </summary>
        [Test]
        public virtual void TestNGramTokenizer2()
        {
            Reader reader = new StringReader("test");
            TokenStream stream = TokenizerFactory("NGram", "minGramSize", "2", "maxGramSize", "3").Create(reader);
            AssertTokenStreamContents(stream, new string[] { "te", "tes", "es", "est", "st" });
        }

        /// <summary>
        /// Test the NGramFilterFactory
        /// </summary>
        [Test]
        public virtual void TestNGramFilter()
        {
            Reader reader = new StringReader("test");
            TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            stream = TokenFilterFactory("NGram").Create(stream);
            AssertTokenStreamContents(stream, new string[] { "t", "te", "e", "es", "s", "st", "t" });
        }

        /// <summary>
        /// Test the NGramFilterFactory with min and max gram options
        /// </summary>
        [Test]
        public virtual void TestNGramFilter2()
        {
            Reader reader = new StringReader("test");
            TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            stream = TokenFilterFactory("NGram", "minGramSize", "2", "maxGramSize", "3").Create(stream);
            AssertTokenStreamContents(stream, new string[] { "te", "tes", "es", "est", "st" });
        }

        /// <summary>
        /// Test EdgeNGramTokenizerFactory
        /// </summary>
        [Test]
        public virtual void TestEdgeNGramTokenizer()
        {
            Reader reader = new StringReader("test");
            TokenStream stream = TokenizerFactory("EdgeNGram").Create(reader);
            AssertTokenStreamContents(stream, new string[] { "t" });
        }

        /// <summary>
        /// Test EdgeNGramTokenizerFactory with min and max gram size
        /// </summary>
        [Test]
        public virtual void TestEdgeNGramTokenizer2()
        {
            Reader reader = new StringReader("test");
            TokenStream stream = TokenizerFactory("EdgeNGram", "minGramSize", "1", "maxGramSize", "2").Create(reader);
            AssertTokenStreamContents(stream, new string[] { "t", "te" });
        }

        /// <summary>
        /// Test EdgeNGramTokenizerFactory with side option
        /// </summary>
        [Test]
        public virtual void TestEdgeNGramTokenizer3()
        {
            Reader reader = new StringReader("ready");
#pragma warning disable 612, 618
            TokenStream stream = TokenizerFactory("EdgeNGram", LuceneVersion.LUCENE_43, "side", "back").Create(reader);
#pragma warning restore 612, 618
            AssertTokenStreamContents(stream, new string[] { "y" });
        }

        /// <summary>
        /// Test EdgeNGramFilterFactory
        /// </summary>
        [Test]
        public virtual void TestEdgeNGramFilter()
        {
            Reader reader = new StringReader("test");
            TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            stream = TokenFilterFactory("EdgeNGram").Create(stream);
            AssertTokenStreamContents(stream, new string[] { "t" });
        }

        /// <summary>
        /// Test EdgeNGramFilterFactory with min and max gram size
        /// </summary>
        [Test]
        public virtual void TestEdgeNGramFilter2()
        {
            Reader reader = new StringReader("test");
            TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            stream = TokenFilterFactory("EdgeNGram", "minGramSize", "1", "maxGramSize", "2").Create(stream);
            AssertTokenStreamContents(stream, new string[] { "t", "te" });
        }

        /// <summary>
        /// Test EdgeNGramFilterFactory with side option
        /// </summary>
        [Test]
        public virtual void TestEdgeNGramFilter3()
        {
            Reader reader = new StringReader("ready");
            TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
#pragma warning disable 612, 618
            stream = TokenFilterFactory("EdgeNGram", LuceneVersion.LUCENE_43, "side", "back").Create(stream);
#pragma warning restore 612, 618
            AssertTokenStreamContents(stream, new string[] { "y" });
        }

        /// <summary>
        /// Test that bogus arguments result in exception </summary>
        [Test]
        public virtual void TestBogusArguments()
        {
            try
            {
                TokenizerFactory("NGram", "bogusArg", "bogusValue");
                fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                assertTrue(expected.Message.Contains("Unknown parameters"));
            }

            try
            {
                TokenizerFactory("EdgeNGram", "bogusArg", "bogusValue");
                fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                assertTrue(expected.Message.Contains("Unknown parameters"));
            }

            try
            {
                TokenFilterFactory("NGram", "bogusArg", "bogusValue");
                fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                assertTrue(expected.Message.Contains("Unknown parameters"));
            }

            try
            {
                TokenFilterFactory("EdgeNGram", "bogusArg", "bogusValue");
                fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                assertTrue(expected.Message.Contains("Unknown parameters"));
            }
        }
    }
}