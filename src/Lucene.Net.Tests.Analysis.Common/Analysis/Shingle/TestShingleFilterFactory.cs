// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Util;
using NUnit.Framework;
using System;
using System.IO;
using Reader = System.IO.TextReader;

namespace Lucene.Net.Analysis.Shingle
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
    /// Simple tests to ensure the Shingle filter factory works.
    /// </summary>
    public class TestShingleFilterFactory : BaseTokenStreamFactoryTestCase
    {
        /// <summary>
        /// Test the defaults
        /// </summary>
        [Test]
        public virtual void TestDefaults()
        {
            Reader reader = new StringReader("this is a test");
            TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            stream = TokenFilterFactory("Shingle").Create(stream);
            AssertTokenStreamContents(stream, new string[] { "this", "this is", "is", "is a", "a", "a test", "test" });
        }

        /// <summary>
        /// Test with unigrams disabled
        /// </summary>
        [Test]
        public virtual void TestNoUnigrams()
        {
            Reader reader = new StringReader("this is a test");
            TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            stream = TokenFilterFactory("Shingle", "outputUnigrams", "false").Create(stream);
            AssertTokenStreamContents(stream, new string[] { "this is", "is a", "a test" });
        }

        /// <summary>
        /// Test with a higher max shingle size
        /// </summary>
        [Test]
        public virtual void TestMaxShingleSize()
        {
            Reader reader = new StringReader("this is a test");
            TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            stream = TokenFilterFactory("Shingle", "maxShingleSize", "3").Create(stream);
            AssertTokenStreamContents(stream, new string[] { "this", "this is", "this is a", "is", "is a", "is a test", "a", "a test", "test" });
        }

        /// <summary>
        /// Test with higher min (and max) shingle size
        /// </summary>
        [Test]
        public virtual void TestMinShingleSize()
        {
            Reader reader = new StringReader("this is a test");
            TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            stream = TokenFilterFactory("Shingle", "minShingleSize", "3", "maxShingleSize", "4").Create(stream);
            AssertTokenStreamContents(stream, new string[] { "this", "this is a", "this is a test", "is", "is a test", "a", "test" });
        }

        /// <summary>
        /// Test with higher min (and max) shingle size and with unigrams disabled
        /// </summary>
        [Test]
        public virtual void TestMinShingleSizeNoUnigrams()
        {
            Reader reader = new StringReader("this is a test");
            TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            stream = TokenFilterFactory("Shingle", "minShingleSize", "3", "maxShingleSize", "4", "outputUnigrams", "false").Create(stream);
            AssertTokenStreamContents(stream, new string[] { "this is a", "this is a test", "is a test" });
        }

        /// <summary>
        /// Test with higher same min and max shingle size
        /// </summary>
        [Test]
        public virtual void TestEqualMinAndMaxShingleSize()
        {
            Reader reader = new StringReader("this is a test");
            TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            stream = TokenFilterFactory("Shingle", "minShingleSize", "3", "maxShingleSize", "3").Create(stream);
            AssertTokenStreamContents(stream, new string[] { "this", "this is a", "is", "is a test", "a", "test" });
        }

        /// <summary>
        /// Test with higher same min and max shingle size and with unigrams disabled
        /// </summary>
        [Test]
        public virtual void TestEqualMinAndMaxShingleSizeNoUnigrams()
        {
            Reader reader = new StringReader("this is a test");
            TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            stream = TokenFilterFactory("Shingle", "minShingleSize", "3", "maxShingleSize", "3", "outputUnigrams", "false").Create(stream);
            AssertTokenStreamContents(stream, new string[] { "this is a", "is a test" });
        }

        /// <summary>
        /// Test with a non-default token separator
        /// </summary>
        [Test]
        public virtual void TestTokenSeparator()
        {
            Reader reader = new StringReader("this is a test");
            TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            stream = TokenFilterFactory("Shingle", "tokenSeparator", "=BLAH=").Create(stream);
            AssertTokenStreamContents(stream, new string[] { "this", "this=BLAH=is", "is", "is=BLAH=a", "a", "a=BLAH=test", "test" });
        }

        /// <summary>
        /// Test with a non-default token separator and with unigrams disabled
        /// </summary>
        [Test]
        public virtual void TestTokenSeparatorNoUnigrams()
        {
            Reader reader = new StringReader("this is a test");
            TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            stream = TokenFilterFactory("Shingle", "tokenSeparator", "=BLAH=", "outputUnigrams", "false").Create(stream);
            AssertTokenStreamContents(stream, new string[] { "this=BLAH=is", "is=BLAH=a", "a=BLAH=test" });
        }

        /// <summary>
        /// Test with an empty token separator
        /// </summary>
        [Test]
        public virtual void TestEmptyTokenSeparator()
        {
            Reader reader = new StringReader("this is a test");
            TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            stream = TokenFilterFactory("Shingle", "tokenSeparator", "").Create(stream);
            AssertTokenStreamContents(stream, new string[] { "this", "thisis", "is", "isa", "a", "atest", "test" });
        }

        /// <summary>
        /// Test with higher min (and max) shingle size 
        /// and with a non-default token separator
        /// </summary>
        [Test]
        public virtual void TestMinShingleSizeAndTokenSeparator()
        {
            Reader reader = new StringReader("this is a test");
            TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            stream = TokenFilterFactory("Shingle", "minShingleSize", "3", "maxShingleSize", "4", "tokenSeparator", "=BLAH=").Create(stream);
            AssertTokenStreamContents(stream, new string[] { "this", "this=BLAH=is=BLAH=a", "this=BLAH=is=BLAH=a=BLAH=test", "is", "is=BLAH=a=BLAH=test", "a", "test" });
        }

        /// <summary>
        /// Test with higher min (and max) shingle size 
        /// and with a non-default token separator
        /// and with unigrams disabled
        /// </summary>
        [Test]
        public virtual void TestMinShingleSizeAndTokenSeparatorNoUnigrams()
        {
            Reader reader = new StringReader("this is a test");
            TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            stream = TokenFilterFactory("Shingle", "minShingleSize", "3", "maxShingleSize", "4", "tokenSeparator", "=BLAH=", "outputUnigrams", "false").Create(stream);
            AssertTokenStreamContents(stream, new string[] { "this=BLAH=is=BLAH=a", "this=BLAH=is=BLAH=a=BLAH=test", "is=BLAH=a=BLAH=test" });
        }

        /// <summary>
        /// Test with unigrams disabled except when there are no shingles, with
        /// a single input token. Using default min/max shingle sizes: 2/2.  No
        /// shingles will be created, since there are fewer input tokens than
        /// min shingle size.  However, because outputUnigramsIfNoShingles is
        /// set to true, even though outputUnigrams is set to false, one
        /// unigram should be output.
        /// </summary>
        [Test]
        public virtual void TestOutputUnigramsIfNoShingles()
        {
            Reader reader = new StringReader("test");
            TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            stream = TokenFilterFactory("Shingle", "outputUnigrams", "false", "outputUnigramsIfNoShingles", "true").Create(stream);
            AssertTokenStreamContents(stream, new string[] { "test" });
        }

        /// <summary>
        /// Test that bogus arguments result in exception </summary>
        [Test]
        public virtual void TestBogusArguments()
        {
            try
            {
                TokenFilterFactory("Shingle", "bogusArg", "bogusValue");
                fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                assertTrue(expected.Message.Contains("Unknown parameters"));
            }
        }
    }
}