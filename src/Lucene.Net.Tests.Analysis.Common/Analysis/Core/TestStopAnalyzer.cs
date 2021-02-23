// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using NUnit.Framework;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

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

    public class TestStopAnalyzer : BaseTokenStreamTestCase
    {

        private StopAnalyzer stop = new StopAnalyzer(TEST_VERSION_CURRENT);
        private ISet<object> inValidTokens = new JCG.HashSet<object>();

        public override void SetUp()
        {
            base.SetUp();

            var it = StopAnalyzer.ENGLISH_STOP_WORDS_SET.GetEnumerator();
            while (it.MoveNext())
            {
                inValidTokens.Add(it.Current);
            }
        }

        [Test]
        public virtual void TestDefaults()
        {
            assertTrue(stop != null);
            TokenStream stream = stop.GetTokenStream("test", "This is a test of the english stop analyzer");
            try
            {
                assertTrue(stream != null);
                ICharTermAttribute termAtt = stream.GetAttribute<ICharTermAttribute>();
                stream.Reset();

                while (stream.IncrementToken())
                {
                    assertFalse(inValidTokens.Contains(termAtt.ToString()));
                }
                stream.End();
            }
            finally
            {
                IOUtils.DisposeWhileHandlingException(stream);
            }
        }

        [Test]
        public virtual void TestStopList()
        {
            CharArraySet stopWordsSet = new CharArraySet(TEST_VERSION_CURRENT, new string[] { "good", "test", "analyzer" }, false);
            StopAnalyzer newStop = new StopAnalyzer(TEST_VERSION_CURRENT, stopWordsSet);
            TokenStream stream = newStop.GetTokenStream("test", "This is a good test of the english stop analyzer");
            try
            {
                assertNotNull(stream);
                ICharTermAttribute termAtt = stream.GetAttribute<ICharTermAttribute>();

                stream.Reset();
                while (stream.IncrementToken())
                {
                    string text = termAtt.ToString();
                    assertFalse(stopWordsSet.contains(text));
                }
                stream.End();
            }
            finally
            {
                IOUtils.DisposeWhileHandlingException(stream);
            }
        }

        [Test]
        public virtual void TestStopListPositions()
        {
            CharArraySet stopWordsSet = new CharArraySet(TEST_VERSION_CURRENT, new string[] { "good", "test", "analyzer" }, false);
            StopAnalyzer newStop = new StopAnalyzer(TEST_VERSION_CURRENT, stopWordsSet);
            string s = "This is a good test of the english stop analyzer with positions";
            int[] expectedIncr = new int[] { 1, 1, 1, 3, 1, 1, 1, 2, 1 };
            TokenStream stream = newStop.GetTokenStream("test", s);
            try
            {
                assertNotNull(stream);
                int i = 0;
                ICharTermAttribute termAtt = stream.GetAttribute<ICharTermAttribute>();
                IPositionIncrementAttribute posIncrAtt = stream.AddAttribute<IPositionIncrementAttribute>();

                stream.Reset();
                while (stream.IncrementToken())
                {
                    string text = termAtt.ToString();
                    assertFalse(stopWordsSet.contains(text));
                    assertEquals(expectedIncr[i++], posIncrAtt.PositionIncrement);
                }
                stream.End();
            }
            finally
            {
                IOUtils.DisposeWhileHandlingException(stream);
            }
        }
    }
}