// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Util;
using System;
using System.IO;
using NUnit.Framework;

namespace Lucene.Net.Analysis.Compound
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
    /// Simple tests to ensure the Dictionary compound filter factory is working.
    /// </summary>
    public class TestDictionaryCompoundWordTokenFilterFactory : BaseTokenStreamFactoryTestCase
    {
        /// <summary>
        /// Ensure the filter actually decompounds text.
        /// </summary>
        [Test]
        public virtual void TestDecompounding()
        {
            TextReader reader = new StringReader("I like to play softball");
            TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            stream = TokenFilterFactory("DictionaryCompoundWord", "dictionary", "compoundDictionary.txt").Create(stream);
            AssertTokenStreamContents(stream, new string[] { "I", "like", "to", "play", "softball", "soft", "ball" });
        }

        /// <summary>
        /// Test that bogus arguments result in exception </summary>
        [Test]
        public virtual void TestBogusArguments()
        {
            try
            {
                TokenFilterFactory("DictionaryCompoundWord", "dictionary", "compoundDictionary.txt", "bogusArg", "bogusValue");
                fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                assertTrue(expected.Message.Contains("Unknown parameters"));
            }
        }
    }
}