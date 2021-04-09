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
    /// basic tests for <see cref="ICUFoldingFilterFactory"/>
    /// </summary>
    public class TestICUFoldingFilterFactory : BaseTokenStreamTestCase
    {
        /** basic tests to ensure the folding is working */
        [Test]
        [AwaitsFix(BugUrl = "https://github.com/apache/lucenenet/issues/269")] // LUCENENET TODO: this test fails only on Linux on GitHub Actions
        public void Test()
        {
            TextReader reader = new StringReader("Résumé");
            ICUFoldingFilterFactory factory = new ICUFoldingFilterFactory(new Dictionary<string, string>());
            TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            stream = factory.Create(stream);
            AssertTokenStreamContents(stream, new string[] { "resume" });
        }

        /** Test that bogus arguments result in exception */
        [Test]
        public void TestBogusArguments()
        {
            try
            {
                new ICUFoldingFilterFactory(new Dictionary<string, string>() {
                            {"bogusArg", "bogusValue" }
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