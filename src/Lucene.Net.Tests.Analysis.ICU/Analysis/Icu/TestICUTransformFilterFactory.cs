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
    /// basic tests for <see cref="ICUTransformFilterFactory"/>
    /// </summary>
    public class TestICUTransformFilterFactory : BaseTokenStreamTestCase
    {
        /** ensure the transform is working */
        [Test]
        public void Test()
        {
            TextReader reader = new StringReader("簡化字");
            IDictionary<string, string> args = new Dictionary<string, string>
            {
                { "id", "Traditional-Simplified" }
            };
            ICUTransformFilterFactory factory = new ICUTransformFilterFactory(args);
            TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            stream = factory.Create(stream);
            AssertTokenStreamContents(stream, new string[] { "简化字" });
        }

        /** test forward and reverse direction */
        [Test]
        public void TestForwardDirection()
        {
            // forward
            TextReader reader = new StringReader("Российская Федерация");
            IDictionary<string, string> args = new Dictionary<string, string>
            {
                { "id", "Cyrillic-Latin" }
            };
            ICUTransformFilterFactory factory = new ICUTransformFilterFactory(args);
            TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            stream = factory.Create(stream);
            AssertTokenStreamContents(stream, new string[] { "Rossijskaâ", "Federaciâ" });
        }

        [Test]
        public void TestReverseDirection()
        {
            // backward (invokes Latin-Cyrillic)
            TextReader reader = new StringReader("Rossijskaâ Federaciâ");
            IDictionary<string, string> args = new Dictionary<string, string>
            {
                {"id", "Cyrillic-Latin" },
                { "direction", "reverse"}
            };

            ICUTransformFilterFactory factory = new ICUTransformFilterFactory(args);
            TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            stream = factory.Create(stream);
            AssertTokenStreamContents(stream, new string[] { "Российская", "Федерация" });
        }

        /** Test that bogus arguments result in exception */
        [Test]
        public void TestBogusArguments()
        {
            try
            {
                new ICUTransformFilterFactory(new Dictionary<string, string> {
                        {"id", "Null" },
                        { "bogusArg", "bogusValue"}
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