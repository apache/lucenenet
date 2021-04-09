// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Util;
using NUnit.Framework;
using System;

namespace Lucene.Net.Analysis.Core
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

    public class TestStopFilterFactory : BaseTokenStreamFactoryTestCase
    {
        [Test]
        public virtual void TestInform()
        {
            IResourceLoader loader = new ClasspathResourceLoader(this.GetType());
            assertTrue("loader is null and it shouldn't be", loader != null);
            StopFilterFactory factory = (StopFilterFactory)TokenFilterFactory("Stop", "words", "stop-1.txt", "ignoreCase", "true");
            CharArraySet words = factory.StopWords;
            assertTrue("words is null and it shouldn't be", words != null);
            assertTrue("words Size: " + words.size() + " is not: " + 2, words.size() == 2);
            assertTrue(factory.IgnoreCase + " does not equal: " + true, factory.IgnoreCase == true);

            factory = (StopFilterFactory)TokenFilterFactory("Stop", "words", "stop-1.txt, stop-2.txt", "ignoreCase", "true");
            words = factory.StopWords;
            assertTrue("words is null and it shouldn't be", words != null);
            assertTrue("words Size: " + words.size() + " is not: " + 4, words.size() == 4);
            assertTrue(factory.IgnoreCase + " does not equal: " + true, factory.IgnoreCase == true);

            factory = (StopFilterFactory)TokenFilterFactory("Stop", "words", "stop-snowball.txt", "format", "snowball", "ignoreCase", "true");
            words = factory.StopWords;
            assertEquals(8, words.size());
            assertTrue(words.contains("he"));
            assertTrue(words.contains("him"));
            assertTrue(words.contains("his"));
            assertTrue(words.contains("himself"));
            assertTrue(words.contains("she"));
            assertTrue(words.contains("her"));
            assertTrue(words.contains("hers"));
            assertTrue(words.contains("herself"));

            // defaults
            factory = (StopFilterFactory)TokenFilterFactory("Stop");
            assertEquals(StopAnalyzer.ENGLISH_STOP_WORDS_SET, factory.StopWords, aggressive: false);
            assertEquals(false, factory.IgnoreCase);
        }

        /// <summary>
        /// Test that bogus arguments result in exception </summary>
        [Test]
        public virtual void TestBogusArguments()
        {
            try
            {
                TokenFilterFactory("Stop", "bogusArg", "bogusValue");
                fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                assertTrue(expected.Message.Contains("Unknown parameters"));
            }
        }

        /// <summary>
        /// Test that bogus arguments result in exception </summary>
        [Test]
        public virtual void TestBogusFormats()
        {
            try
            {
                TokenFilterFactory("Stop", "words", "stop-snowball.txt", "format", "bogus");
                fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                string msg = expected.Message;
                assertTrue(msg, msg.Contains("Unknown"));
                assertTrue(msg, msg.Contains("format"));
                assertTrue(msg, msg.Contains("bogus"));
            }
            try
            {
                TokenFilterFactory("Stop", "format", "bogus");
                // implicit default words file
                fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                string msg = expected.Message;
                assertTrue(msg, msg.Contains("can not be specified"));
                assertTrue(msg, msg.Contains("format"));
                assertTrue(msg, msg.Contains("bogus"));
            }
        }
    }
}