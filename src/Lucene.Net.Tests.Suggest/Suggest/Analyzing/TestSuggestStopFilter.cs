using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Util;
using NUnit.Framework;
using System;
using System.IO;

namespace Lucene.Net.Search.Suggest.Analyzing
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

    public class TestSuggestStopFilter : BaseTokenStreamTestCase
    {
        [Test]
        public void TestEndNotStopWord()
        {
            CharArraySet stopWords = StopFilter.MakeStopSet(TEST_VERSION_CURRENT, "to");
            TokenStream stream = new MockTokenizer(new StringReader("go to"));
            TokenStream filter = new SuggestStopFilter(stream, stopWords);
            AssertTokenStreamContents(filter,
                                      new string[] { "go", "to" },
                                      new int[] { 0, 3 },
                                      new int[] { 2, 5 },
                                      null,
                                      new int[] { 1, 1 },
                                      null,
                                      5,
                                      new bool[] { false, true },
                                      true);
        }

        [Test]
        public void TestEndIsStopWord()
        {

            CharArraySet stopWords = StopFilter.MakeStopSet(TEST_VERSION_CURRENT, "to");
            TokenStream stream = new MockTokenizer(new StringReader("go to "));
            TokenStream filter = new SuggestStopFilter(stream, stopWords);

            filter = new SuggestStopFilter(stream, stopWords);
            AssertTokenStreamContents(filter,
                                      new string[] { "go" },
                                      new int[] { 0 },
                                      new int[] { 2 },
                                      null,
                                      new int[] { 1 },
                                      null,
                                      6,
                                      new bool[] { false },
                                      true);
        }

        [Test]
        public void TestMidStopWord()
        {

            CharArraySet stopWords = StopFilter.MakeStopSet(TEST_VERSION_CURRENT, "to");
            TokenStream stream = new MockTokenizer(new StringReader("go to school"));
            TokenStream filter = new SuggestStopFilter(stream, stopWords);

            filter = new SuggestStopFilter(stream, stopWords);
            AssertTokenStreamContents(filter,
                                      new String[] { "go", "school" },
                                      new int[] { 0, 6 },
                                      new int[] { 2, 12 },
                                      null,
                                      new int[] { 1, 2 },
                                      null,
                                      12,
                                      new bool[] { false, false },
                                      true);
        }

        [Test]
        public void TestMultipleStopWords()
        {

            CharArraySet stopWords = StopFilter.MakeStopSet(TEST_VERSION_CURRENT, "to", "the", "a");
            TokenStream stream = new MockTokenizer(new StringReader("go to a the school"));
            TokenStream filter = new SuggestStopFilter(stream, stopWords);

            filter = new SuggestStopFilter(stream, stopWords);
            AssertTokenStreamContents(filter,
                                      new String[] { "go", "school" },
                                      new int[] { 0, 12 },
                                      new int[] { 2, 18 },
                                      null,
                                      new int[] { 1, 4 },
                                      null,
                                      18,
                                      new bool[] { false, false },
                                      true);
        }

        [Test]
        public void TestMultipleStopWordsEnd()
        {

            CharArraySet stopWords = StopFilter.MakeStopSet(TEST_VERSION_CURRENT, "to", "the", "a");
            TokenStream stream = new MockTokenizer(new StringReader("go to a the"));
            TokenStream filter = new SuggestStopFilter(stream, stopWords);

            filter = new SuggestStopFilter(stream, stopWords);
            AssertTokenStreamContents(filter,
                                      new String[] { "go", "the" },
                                      new int[] { 0, 8 },
                                      new int[] { 2, 11 },
                                      null,
                                      new int[] { 1, 3 },
                                      null,
                                      11,
                                      new bool[] { false, true },
                                      true);
        }

        [Test]
        public void TestMultipleStopWordsEnd2()
        {

            CharArraySet stopWords = StopFilter.MakeStopSet(TEST_VERSION_CURRENT, "to", "the", "a");
            TokenStream stream = new MockTokenizer(new StringReader("go to a the "));
            TokenStream filter = new SuggestStopFilter(stream, stopWords);

            filter = new SuggestStopFilter(stream, stopWords);
            AssertTokenStreamContents(filter,
                                      new String[] { "go" },
                                      new int[] { 0 },
                                      new int[] { 2 },
                                      null,
                                      new int[] { 1 },
                                      null,
                                      12,
                                      new bool[] { false },
                                      true);
        }
    }
}
