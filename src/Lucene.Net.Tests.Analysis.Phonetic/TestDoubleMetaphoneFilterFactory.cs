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

    public class TestDoubleMetaphoneFilterFactory : BaseTokenStreamTestCase
    {
        [Test]
        public void TestDefaults()
        {
            DoubleMetaphoneFilterFactory factory = new DoubleMetaphoneFilterFactory(new Dictionary<String, String>());
            TokenStream inputStream = new MockTokenizer(new StringReader("international"), MockTokenizer.WHITESPACE, false);

            TokenStream filteredStream = factory.Create(inputStream);
            assertEquals(typeof(DoubleMetaphoneFilter), filteredStream.GetType());
            AssertTokenStreamContents(filteredStream, new String[] { "international", "ANTR" });
        }

        [Test]
        public void TestSettingSizeAndInject()
        {
            IDictionary<string, string> parameters = new Dictionary<string, string>();
            parameters["inject"] = "false";
            parameters["maxCodeLength"] = "8";
            DoubleMetaphoneFilterFactory factory = new DoubleMetaphoneFilterFactory(parameters);

            TokenStream inputStream = new MockTokenizer(new StringReader("international"), MockTokenizer.WHITESPACE, false);

            TokenStream filteredStream = factory.Create(inputStream);
            assertEquals(typeof(DoubleMetaphoneFilter), filteredStream.GetType());
            AssertTokenStreamContents(filteredStream, new String[] { "ANTRNXNL" });
        }

        /** Test that bogus arguments result in exception */
        [Test]
        public void TestBogusArguments()
        {
            try
            {
                new DoubleMetaphoneFilterFactory(new Dictionary<String, String>() {
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
