// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Util;
using NUnit.Framework;

namespace Lucene.Net.Analysis.Ckb
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
    /// Test the Sorani analyzer
    /// </summary>
    public class TestSoraniAnalyzer : BaseTokenStreamTestCase
    {

        /// <summary>
        /// This test fails with NPE when the stopwords file is missing in classpath
        /// </summary>
        [Test]
        public virtual void TestResourcesAvailable()
        {
            new SoraniAnalyzer(TEST_VERSION_CURRENT);
        }

        [Test]
        public virtual void TestStopwords()
        {
            Analyzer a = new SoraniAnalyzer(TEST_VERSION_CURRENT);
            AssertAnalyzesTo(a, "ئەم پیاوە", new string[] { "پیاو" });
        }

        [Test]
        public virtual void TestCustomStopwords()
        {
            Analyzer a = new SoraniAnalyzer(TEST_VERSION_CURRENT, CharArraySet.Empty);
            AssertAnalyzesTo(a, "ئەم پیاوە", new string[] { "ئەم", "پیاو" });
        }

        [Test]
        public virtual void TestReusableTokenStream()
        {
            Analyzer a = new SoraniAnalyzer(TEST_VERSION_CURRENT);
            AssertAnalyzesTo(a, "پیاوە", new string[] { "پیاو" });
            AssertAnalyzesTo(a, "پیاو", new string[] { "پیاو" });
        }

        [Test]
        public virtual void TestWithStemExclusionSet()
        {
            CharArraySet set = new CharArraySet(TEST_VERSION_CURRENT, 1, true);
            set.add("پیاوە");
            Analyzer a = new SoraniAnalyzer(TEST_VERSION_CURRENT, CharArraySet.Empty, set);
            AssertAnalyzesTo(a, "پیاوە", new string[] { "پیاوە" });
        }

        /// <summary>
        /// blast some random strings through the analyzer </summary>
        [Test]
        public virtual void TestRandomStrings()
        {
            CheckRandomData(Random, new SoraniAnalyzer(TEST_VERSION_CURRENT), 1000 * RandomMultiplier);
        }
    }
}