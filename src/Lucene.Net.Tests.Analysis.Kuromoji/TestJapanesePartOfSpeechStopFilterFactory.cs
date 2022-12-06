﻿using Lucene.Net.Support;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;

namespace Lucene.Net.Analysis.Ja
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
    /// Simple tests for <see cref="JapanesePartOfSpeechStopFilterFactory"/>
    /// </summary>
    public class TestJapanesePartOfSpeechStopFilterFactory : BaseTokenStreamTestCase
    {
        [Test]
        public void TestBasics()
        {
            String tags =
                "#  verb-main:\n" +
                "動詞-自立\n";

            JapaneseTokenizerFactory tokenizerFactory = new JapaneseTokenizerFactory(new Dictionary<String, String>());
            tokenizerFactory.Inform(new StringMockResourceLoader(""));
            TokenStream ts = tokenizerFactory.Create(new StringReader("私は制限スピードを超える。"));
            IDictionary<String, String> args = new Dictionary<String, String>();
            args["luceneMatchVersion"] = TEST_VERSION_CURRENT.ToString();
            args["tags"] = "stoptags.txt";
            JapanesePartOfSpeechStopFilterFactory factory = new JapanesePartOfSpeechStopFilterFactory(args);
            factory.Inform(new StringMockResourceLoader(tags));
            ts = factory.Create(ts);
            AssertTokenStreamContents(ts,
                new String[] { "私", "は", "制限", "スピード", "を" }
            );
        }

        /** Test that bogus arguments result in exception */
        [Test]
        public void TestBogusArguments()
        {
            try
            {
                new JapanesePartOfSpeechStopFilterFactory(new Dictionary<String, String>() {
                    { "luceneMatchVersion", TEST_VERSION_CURRENT.toString() },
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
