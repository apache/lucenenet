using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;

namespace Lucene.Net.Analysis.Cn.Smart
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
    /// Tests for <see cref=""SmartChineseSentenceTokenizerFactory/> and 
    /// <see cref="SmartChineseWordTokenFilterFactory"/>
    /// </summary>
    [Obsolete]
    public class TestSmartChineseFactories : BaseTokenStreamTestCase
    {
        /// <summary>
        /// Test showing the behavior with whitespace
        /// </summary>
        [Test]
        public void TestSimple()
        {
            TextReader reader = new StringReader("我购买了道具和服装。");
            TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
            SmartChineseWordTokenFilterFactory factory = new SmartChineseWordTokenFilterFactory(new Dictionary<string, string>());
            stream = factory.Create(stream);
            // TODO: fix smart chinese to not emit punctuation tokens
            // at the moment: you have to clean up with WDF, or use the stoplist, etc
            AssertTokenStreamContents(stream,
               new String[] { "我", "购买", "了", "道具", "和", "服装", "," });
        }

        /// <summary>
        /// Test showing the behavior with whitespace
        /// </summary>
        [Test]
        public void TestTokenizer()
        {
            TextReader reader = new StringReader("我购买了道具和服装。我购买了道具和服装。");
            SmartChineseSentenceTokenizerFactory tokenizerFactory = new SmartChineseSentenceTokenizerFactory(new Dictionary<string, string>());
            TokenStream stream = tokenizerFactory.Create(reader);
            SmartChineseWordTokenFilterFactory factory = new SmartChineseWordTokenFilterFactory(new Dictionary<string, string>());
            stream = factory.Create(stream);
            // TODO: fix smart chinese to not emit punctuation tokens
            // at the moment: you have to clean up with WDF, or use the stoplist, etc
            AssertTokenStreamContents(stream,
               new String[] { "我", "购买", "了", "道具", "和", "服装", ",",
                    "我", "购买", "了", "道具", "和", "服装", ","
                });
        }

        /// <summary>
        /// Test that bogus arguments result in exception
        /// </summary>
        [Test]
        public void TestBogusArguments()
        {
            try
            {
                new SmartChineseSentenceTokenizerFactory(new Dictionary<string, string>() {
                    { "bogusArg", "bogusValue" }
                });
                fail();
            }
            catch (Exception expected) when (expected.IsIllegalArgumentException())
            {
                assertTrue(expected.Message.Contains("Unknown parameters"));
            }

            try
            {
                new SmartChineseWordTokenFilterFactory(new Dictionary<string, string>() {
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
