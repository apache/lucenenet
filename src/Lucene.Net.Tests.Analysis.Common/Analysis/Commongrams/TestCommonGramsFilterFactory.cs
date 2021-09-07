// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Util;
using NUnit.Framework;
using System;
using System.IO;

namespace Lucene.Net.Analysis.CommonGrams
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
    /// Tests pretty much copied from StopFilterFactoryTest We use the test files
    /// used by the StopFilterFactoryTest TODO: consider creating separate test files
    /// so this won't break if stop filter test files change
    /// 
    /// </summary>
    public class TestCommonGramsFilterFactory : BaseTokenStreamFactoryTestCase
    {

        [Test]
        public virtual void TestInform()
        {
            //IResourceLoader loader = new ClasspathResourceLoader(typeof(TestStopFilter));
            IResourceLoader loader = new ClasspathResourceLoader(typeof(TestAnalyzers)); // LUCENENET: Need to set to a type that is in the same path as the files
            assertTrue("loader is null and it shouldn't be", loader != null);
            CommonGramsFilterFactory factory = (CommonGramsFilterFactory)TokenFilterFactory("CommonGrams", TEST_VERSION_CURRENT, loader, "words", "stop-1.txt", "ignoreCase", "true");
            CharArraySet words = factory.CommonWords;
            assertTrue("words is null and it shouldn't be", words != null);
            assertTrue("words Size: " + words.size() + " is not: " + 2, words.size() == 2);
            assertTrue(factory.IgnoreCase + " does not equal: " + true, factory.IgnoreCase == true);

            factory = (CommonGramsFilterFactory)TokenFilterFactory("CommonGrams", TEST_VERSION_CURRENT, loader, "words", "stop-1.txt, stop-2.txt", "ignoreCase", "true");
            words = factory.CommonWords;
            assertTrue("words is null and it shouldn't be", words != null);
            assertTrue("words Size: " + words.size() + " is not: " + 4, words.size() == 4);
            assertTrue(factory.IgnoreCase + " does not equal: " + true, factory.IgnoreCase == true);

            factory = (CommonGramsFilterFactory)TokenFilterFactory("CommonGrams", TEST_VERSION_CURRENT, loader, "words", "stop-snowball.txt", "format", "snowball", "ignoreCase", "true");
            words = factory.CommonWords;
            assertEquals(8, words.size());
            assertTrue(words.contains("he"));
            assertTrue(words.contains("him"));
            assertTrue(words.contains("his"));
            assertTrue(words.contains("himself"));
            assertTrue(words.contains("she"));
            assertTrue(words.contains("her"));
            assertTrue(words.contains("hers"));
            assertTrue(words.contains("herself"));
        }

        /// <summary>
        /// If no words are provided, then a set of english default stopwords is used.
        /// </summary>
        [Test]
        public virtual void TestDefaults()
        {
            CommonGramsFilterFactory factory = (CommonGramsFilterFactory)TokenFilterFactory("CommonGrams");
            CharArraySet words = factory.CommonWords;
            assertTrue("words is null and it shouldn't be", words != null);
            assertTrue(words.contains("the"));
            Tokenizer tokenizer = new MockTokenizer(new StringReader("testing the factory"), MockTokenizer.WHITESPACE, false);
            TokenStream stream = factory.Create(tokenizer);
            AssertTokenStreamContents(stream, new string[] { "testing", "testing_the", "the", "the_factory", "factory" });
        }

        /// <summary>
        /// Test that bogus arguments result in exception </summary>
        [Test]
        public virtual void TestBogusArguments()
        {
            try
            {
                TokenFilterFactory("CommonGrams", "bogusArg", "bogusValue");
                fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                assertTrue(expected.Message.Contains("Unknown parameters"));
            }
        }
    }
}