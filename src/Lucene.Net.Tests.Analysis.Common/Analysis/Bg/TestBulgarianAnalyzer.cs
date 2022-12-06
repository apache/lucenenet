// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Util;
using NUnit.Framework;

namespace Lucene.Net.Analysis.Bg
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
    /// Test the Bulgarian analyzer
    /// </summary>
    public class TestBulgarianAnalyzer : BaseTokenStreamTestCase
    {

        /// <summary>
        /// This test fails with NPE when the stopwords file is missing in classpath
        /// </summary>
        [Test]
        public virtual void TestResourcesAvailable()
        {
            new BulgarianAnalyzer(TEST_VERSION_CURRENT);
        }

        [Test]
        public virtual void TestStopwords()
        {
            Analyzer a = new BulgarianAnalyzer(TEST_VERSION_CURRENT);
            AssertAnalyzesTo(a, "Как се казваш?", new string[] { "казваш" });
        }

        [Test]
        public virtual void TestCustomStopwords()
        {
            Analyzer a = new BulgarianAnalyzer(TEST_VERSION_CURRENT, CharArraySet.Empty);
            AssertAnalyzesTo(a, "Как се казваш?", new string[] { "как", "се", "казваш" });
        }

        [Test]
        public virtual void TestReusableTokenStream()
        {
            Analyzer a = new BulgarianAnalyzer(TEST_VERSION_CURRENT);
            AssertAnalyzesTo(a, "документи", new string[] { "документ" });
            AssertAnalyzesTo(a, "документ", new string[] { "документ" });
        }

        /// <summary>
        /// Test some examples from the paper
        /// </summary>
        [Test]
        public virtual void TestBasicExamples()
        {
            Analyzer a = new BulgarianAnalyzer(TEST_VERSION_CURRENT);
            AssertAnalyzesTo(a, "енергийни кризи", new string[] { "енергийн", "криз" });
            AssertAnalyzesTo(a, "Атомната енергия", new string[] { "атомн", "енерг" });

            AssertAnalyzesTo(a, "компютри", new string[] { "компютр" });
            AssertAnalyzesTo(a, "компютър", new string[] { "компютр" });

            AssertAnalyzesTo(a, "градове", new string[] { "град" });
        }

        [Test]
        public virtual void TestWithStemExclusionSet()
        {
            CharArraySet set = new CharArraySet(TEST_VERSION_CURRENT, 1, true);
            set.add("строеве");
            Analyzer a = new BulgarianAnalyzer(TEST_VERSION_CURRENT, CharArraySet.Empty, set);
            AssertAnalyzesTo(a, "строевете строеве", new string[] { "строй", "строеве" });
        }

        /// <summary>
        /// blast some random strings through the analyzer </summary>
        [Test]
        public virtual void TestRandomStrings()
        {
            CheckRandomData(Random, new BulgarianAnalyzer(TEST_VERSION_CURRENT), 1000 * RandomMultiplier);
        }
    }
}