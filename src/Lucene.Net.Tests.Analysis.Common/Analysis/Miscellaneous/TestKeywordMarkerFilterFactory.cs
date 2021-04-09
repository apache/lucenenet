// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Util;
using NUnit.Framework;
using System;
using System.IO;
using Reader = System.IO.TextReader;

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

    /// <summary>
    /// Simple tests to ensure the keyword marker filter factory is working.
    /// </summary>
    public class TestKeywordMarkerFilterFactory : BaseTokenStreamFactoryTestCase
    {

        [Test]
        public virtual void TestKeywords()
        {
            Reader reader = new StringReader("dogs cats");
            TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            stream = TokenFilterFactory("KeywordMarker", TEST_VERSION_CURRENT, new StringMockResourceLoader("cats"), "protected", "protwords.txt").Create(stream);
            stream = TokenFilterFactory("PorterStem").Create(stream);
            AssertTokenStreamContents(stream, new string[] { "dog", "cats" });
        }

        [Test]
        public virtual void TestKeywords2()
        {
            Reader reader = new StringReader("dogs cats");
            TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            stream = TokenFilterFactory("KeywordMarker", "pattern", "cats|Dogs").Create(stream);
            stream = TokenFilterFactory("PorterStem").Create(stream);
            AssertTokenStreamContents(stream, new string[] { "dog", "cats" });
        }

        [Test]
        public virtual void TestKeywordsMixed()
        {
            Reader reader = new StringReader("dogs cats birds");
            TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            stream = TokenFilterFactory("KeywordMarker", TEST_VERSION_CURRENT, new StringMockResourceLoader("cats"), "protected", "protwords.txt", "pattern", "birds|Dogs").Create(stream);
            stream = TokenFilterFactory("PorterStem").Create(stream);
            AssertTokenStreamContents(stream, new string[] { "dog", "cats", "birds" });
        }

        [Test]
        public virtual void TestKeywordsCaseInsensitive()
        {
            Reader reader = new StringReader("dogs cats Cats");
            TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            stream = TokenFilterFactory("KeywordMarker", TEST_VERSION_CURRENT, new StringMockResourceLoader("cats"), "protected", "protwords.txt", "ignoreCase", "true").Create(stream);
            stream = TokenFilterFactory("PorterStem").Create(stream);
            AssertTokenStreamContents(stream, new string[] { "dog", "cats", "Cats" });
        }

        [Test]
        public virtual void TestKeywordsCaseInsensitive2()
        {
            Reader reader = new StringReader("dogs cats Cats");
            TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            stream = TokenFilterFactory("KeywordMarker", "pattern", "Cats", "ignoreCase", "true").Create(stream);
            stream = TokenFilterFactory("PorterStem").Create(stream);
            AssertTokenStreamContents(stream, new string[] { "dog", "cats", "Cats" });
        }

        [Test]
        public virtual void TestKeywordsCaseInsensitiveMixed()
        {
            Reader reader = new StringReader("dogs cats Cats Birds birds");
            TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            stream = TokenFilterFactory("KeywordMarker", TEST_VERSION_CURRENT, new StringMockResourceLoader("cats"), "protected", "protwords.txt", "pattern", "birds", "ignoreCase", "true").Create(stream);
            stream = TokenFilterFactory("PorterStem").Create(stream);
            AssertTokenStreamContents(stream, new string[] { "dog", "cats", "Cats", "Birds", "birds" });
        }

        /// <summary>
        /// Test that bogus arguments result in exception </summary>
        [Test]
        public virtual void TestBogusArguments()
        {
            try
            {
                TokenFilterFactory("KeywordMarker", "bogusArg", "bogusValue");
                fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                assertTrue(expected.Message.Contains("Unknown parameters"));
            }
        }
    }
}