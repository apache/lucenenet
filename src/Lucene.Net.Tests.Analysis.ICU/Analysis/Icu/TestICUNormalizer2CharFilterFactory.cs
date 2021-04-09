// Lucene version compatibility level 4.8.1
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;

namespace Lucene.Net.Analysis.Icu
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
    /// basic tests for <see cref="ICUNormalizer2CharFilterFactory"/>
    /// </summary>
    public class TestICUNormalizer2CharFilterFactory : BaseTokenStreamTestCase
    {
        /** Test nfkc_cf defaults */
        [Test]
        public void TestDefaults()
        {
            TextReader reader = new StringReader("This is a Ｔｅｓｔ");
            ICUNormalizer2CharFilterFactory factory = new ICUNormalizer2CharFilterFactory(new Dictionary<String, String>());
            reader = factory.Create(reader);
            TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            AssertTokenStreamContents(stream, new String[] { "this", "is", "a", "test" });
        }

        /** Test that bogus arguments result in exception */
        [Test]
        public void TestBogusArguments()
        {
            try
            {
                new ICUNormalizer2CharFilterFactory(new Dictionary<String, String>() {
                    { "bogusArg", "bogusValue" }
                });
                fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                assertTrue(expected.Message.Contains("Unknown parameters"));
            }
        }

        // TODO: add tests for different forms
    }
}
