// Lucene version compatibility level 4.8.1
using J2N.Text;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Tartarus.Snowball.Ext;
using NUnit.Framework;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace Lucene.Net.Analysis.Snowball
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


    public class TestSnowballPorterFilterFactory : BaseTokenStreamFactoryTestCase
    {

        [Test]
        public virtual void Test()
        {
            string text = "The fledgling banks were counting on a big boom in banking";
            EnglishStemmer stemmer = new EnglishStemmer();
            string[] test = Regex.Split(text, "\\s").TrimEnd();
            string[] gold = new string[test.Length];
            for (int i = 0; i < test.Length; i++)
            {
                stemmer.SetCurrent(test[i]);
                stemmer.Stem();
                gold[i] = stemmer.Current;
            }

            TextReader reader = new StringReader(text);
            TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            stream = TokenFilterFactory("SnowballPorter", "language", "English").Create(stream);
            AssertTokenStreamContents(stream, gold);
        }

        /// <summary>
        /// Test the protected words mechanism of SnowballPorterFilterFactory
        /// </summary>
        [Test]
        public virtual void TestProtected()
        {
            TextReader reader = new StringReader("ridding of some stemming");
            TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            stream = TokenFilterFactory("SnowballPorter", TEST_VERSION_CURRENT, new StringMockResourceLoader("ridding"), "protected", "protwords.txt", "language", "English").Create(stream);

            AssertTokenStreamContents(stream, new string[] { "ridding", "of", "some", "stem" });
        }

        /// <summary>
        /// Test that bogus arguments result in exception </summary>
        [Test]
        public virtual void TestBogusArguments()
        {
            try
            {
                TokenFilterFactory("SnowballPorter", "bogusArg", "bogusValue");
                fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                assertTrue(expected.Message.Contains("Unknown parameters"));
            }
        }
    }
}