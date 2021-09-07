// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Util;
using NUnit.Framework;
using System;

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

    public class TestKeepFilterFactory : BaseTokenStreamFactoryTestCase
    {

        [Test]
        public virtual void TestInform()
        {
            IResourceLoader loader = new ClasspathResourceLoader(this.GetType());
            assertTrue("loader is null and it shouldn't be", loader != null);
            KeepWordFilterFactory factory = (KeepWordFilterFactory)TokenFilterFactory("KeepWord", "words", "keep-1.txt", "ignoreCase", "true");
            CharArraySet words = factory.Words;
            assertTrue("words is null and it shouldn't be", words != null);
            assertTrue("words Size: " + words.size() + " is not: " + 2, words.size() == 2);

            factory = (KeepWordFilterFactory)TokenFilterFactory("KeepWord", "words", "keep-1.txt, keep-2.txt", "ignoreCase", "true");
            words = factory.Words;
            assertTrue("words is null and it shouldn't be", words != null);
            assertTrue("words Size: " + words.size() + " is not: " + 4, words.size() == 4);
        }

        /// <summary>
        /// Test that bogus arguments result in exception </summary>
        [Test]
        public virtual void TestBogusArguments()
        {
            try
            {
                TokenFilterFactory("KeepWord", "bogusArg", "bogusValue");
                fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                assertTrue(expected.Message.Contains("Unknown parameters"));
            }
        }
    }
}