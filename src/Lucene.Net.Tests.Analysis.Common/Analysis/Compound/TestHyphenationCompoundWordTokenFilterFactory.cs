using Lucene.Net.Analysis.Util;
using Lucene.Net.Attributes;
using NUnit.Framework;
using System.IO;

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
    /// Simple tests to ensure the Hyphenation compound filter factory is working.
    /// </summary>
    public class TestHyphenationCompoundWordTokenFilterFactory : BaseTokenStreamFactoryTestCase
    {
        /// <summary>
        /// Ensure the factory works with hyphenation grammar+dictionary: using default options.
        /// </summary>
        [Test]
        [DtdProcessingTest]
        public virtual void TestHyphenationWithDictionary()
        {
            TextReader reader = new StringReader("min veninde som er lidt af en læsehest");
            TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            stream = TokenFilterFactory("HyphenationCompoundWord", "hyphenator", "da_UTF8.xml", "dictionary", "da_compoundDictionary.txt").Create(stream);

            AssertTokenStreamContents(stream, new string[] { "min", "veninde", "som", "er", "lidt", "af", "en", "læsehest", "læse", "hest" }, new int[] { 1, 1, 1, 1, 1, 1, 1, 1, 0, 0 });
        }

        /// <summary>
        /// Ensure the factory works with no dictionary: using hyphenation grammar only.
        /// Also change the min/max subword sizes from the default. When using no dictionary,
        /// its generally necessary to tweak these, or you get lots of expansions.
        /// </summary>
        [Test]
        [DtdProcessingTest]
        public virtual void TestHyphenationOnly()
        {
            TextReader reader = new StringReader("basketballkurv");
            TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            stream = TokenFilterFactory("HyphenationCompoundWord", "hyphenator", "da_UTF8.xml", "minSubwordSize", "2", "maxSubwordSize", "4").Create(stream);

            AssertTokenStreamContents(stream, new string[] { "basketballkurv", "ba", "sket", "bal", "ball", "kurv" });
        }

        /// <summary>
        /// Test that bogus arguments result in exception </summary>
        [Test]
        public virtual void TestBogusArguments()
        {
            try
            {
                TokenFilterFactory("HyphenationCompoundWord", "hyphenator", "da_UTF8.xml", "bogusArg", "bogusValue");
                fail();
            }
            catch (System.ArgumentException expected)
            {
                assertTrue(expected.Message.Contains("Unknown parameters"));
            }
        }
    }
}