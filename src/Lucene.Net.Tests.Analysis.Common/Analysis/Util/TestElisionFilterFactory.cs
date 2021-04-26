// Lucene version compatibility level 4.8.1
using NUnit.Framework;
using System;
using System.IO;

namespace Lucene.Net.Analysis.Util
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
    /// Simple tests to ensure the French elision filter factory is working.
    /// </summary>
    public class TestElisionFilterFactory : BaseTokenStreamFactoryTestCase
    {
        /// <summary>
        /// Ensure the filter actually normalizes text.
        /// </summary>
        [Test]
        public virtual void TestElision()
        {
            TextReader reader = new StringReader("l'avion");
            TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            stream = TokenFilterFactory("Elision", "articles", "frenchArticles.txt").Create(stream);
            AssertTokenStreamContents(stream, new string[] { "avion" });
        }

        /// <summary>
        /// Test creating an elision filter without specifying any articles
        /// </summary>
        [Test]
        public virtual void TestDefaultArticles()
        {
            TextReader reader = new StringReader("l'avion");
            TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            stream = TokenFilterFactory("Elision").Create(stream);
            AssertTokenStreamContents(stream, new string[] { "avion" });
        }

        /// <summary>
        /// Test setting ignoreCase=true
        /// </summary>
        [Test]
        public virtual void TestCaseInsensitive()
        {
            TextReader reader = new StringReader("L'avion");
            TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            stream = TokenFilterFactory("Elision", "articles", "frenchArticles.txt", "ignoreCase", "true").Create(stream);
            AssertTokenStreamContents(stream, new string[] { "avion" });
        }

        /// <summary>
        /// Test that bogus arguments result in exception </summary>
        [Test]
        public virtual void TestBogusArguments()
        {
            try
            {
                TokenFilterFactory("Elision", "bogusArg", "bogusValue");
                fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                assertTrue(expected.Message.Contains("Unknown parameters"));
            }
        }
    }
}