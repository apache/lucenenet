using Lucene.Net.Support;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;

namespace Lucene.Net.Analysis.Phonetic
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
    /// Simple tests for <see cref="BeiderMorseFilterFactory"/>
    /// </summary>
    public class TestBeiderMorseFilterFactory : BaseTokenStreamTestCase
    {
        [Test]
        public void TestBasics()
        {
            BeiderMorseFilterFactory factory = new BeiderMorseFilterFactory(new Dictionary<String, String>());
            TokenStream ts = factory.Create(new MockTokenizer(new StringReader("Weinberg"), MockTokenizer.WHITESPACE, false));
            AssertTokenStreamContents(ts,
                new String[] { "vDnbirk", "vanbirk", "vinbirk", "wDnbirk", "wanbirk", "winbirk" },
                new int[] { 0, 0, 0, 0, 0, 0 },
                new int[] { 8, 8, 8, 8, 8, 8 },
                new int[] { 1, 0, 0, 0, 0, 0 });
        }

        [Test]
        public void TestLanguageSet()
        {
            IDictionary<String, String> args = new Dictionary<string, string>();
            args["languageSet"] = "polish";
            BeiderMorseFilterFactory factory = new BeiderMorseFilterFactory(args);
            TokenStream ts = factory.Create(new MockTokenizer(new StringReader("Weinberg"), MockTokenizer.WHITESPACE, false));
            AssertTokenStreamContents(ts,
                new String[] { "vDmbYrk", "vDmbirk", "vambYrk", "vambirk", "vimbYrk", "vimbirk" },
                new int[] { 0, 0, 0, 0, 0, 0 },
                new int[] { 8, 8, 8, 8, 8, 8 },
                new int[] { 1, 0, 0, 0, 0, 0 });
        }

        [Test]
        public void TestOptions()
        {
            IDictionary<String, String> args = new Dictionary<string, string>();
            args["nameType"] = "ASHKENAZI";
            args["ruleType"] = "EXACT";
            BeiderMorseFilterFactory factory = new BeiderMorseFilterFactory(args);
            TokenStream ts = factory.Create(new MockTokenizer(new StringReader("Weinberg"), MockTokenizer.WHITESPACE, false));
            AssertTokenStreamContents(ts,
                new String[] { "vajnberk" },
                new int[] { 0 },
                new int[] { 8 },
                new int[] { 1 });
        }

        /** Test that bogus arguments result in exception */
        [Test]
        public void TestBogusArguments()
        {
            try
            {
                new BeiderMorseFilterFactory(new Dictionary<String, String>() {
                    { "bogusArg", "bogusValue" }
                });
                fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                assertTrue(expected.Message.Contains("Unknown parameters"));
            }
        }
    }
}
