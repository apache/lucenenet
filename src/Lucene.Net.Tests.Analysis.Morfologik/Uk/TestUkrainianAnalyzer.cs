// Lucene version compatibility level 8.2.0
using NUnit.Framework;
using System;

namespace Lucene.Net.Analysis.Uk
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
    /// Test case for <see cref="UkrainianAnalyzer"/>.
    /// </summary>
    public class TestUkrainianAnalyzer : BaseTokenStreamTestCase
    {
        /** Check that UkrainianAnalyzer doesn't discard any numbers */
        [Test]
        public void TestDigitsInUkrainianCharset()
        {
            UkrainianMorfologikAnalyzer ra = new UkrainianMorfologikAnalyzer(TEST_VERSION_CURRENT);
            AssertAnalyzesTo(ra, "text 1000", new String[] { "text", "1000" });
            ra.Dispose();
        }

        [Test]
        public void TestReusableTokenStream()
        {
            Analyzer a = new UkrainianMorfologikAnalyzer(TEST_VERSION_CURRENT);
            AssertAnalyzesTo(a, "Ця п'єса, у свою чергу, рухається по емоційно-напруженому колу за ритм-енд-блюзом.",
                                 new String[] { "п'єса", "черга", "рухатися", "емоційно", "напружений", "кола", "коло", "кіл", "ритм", "енд", "блюз" });
            a.Dispose();
        }

        [Test]
        public void TestSpecialCharsTokenStream()
        {
            Analyzer a = new UkrainianMorfologikAnalyzer(TEST_VERSION_CURRENT);
            AssertAnalyzesTo(a, "м'яса м'я\u0301са м\u02BCяса м\u2019яса м\u2018яса м`яса",
                     new String[] { "м'ясо", "м'ясо", "м'ясо", "м'ясо", "м'ясо", "м'ясо" });
            a.Dispose();
        }

        [Test]
        public void TestCapsTokenStream()
        {
            Analyzer a = new UkrainianMorfologikAnalyzer(TEST_VERSION_CURRENT);
            AssertAnalyzesTo(a, "Цих Чайковського і Ґете.",
                     new String[] { "Чайковське", "Чайковський", "Гете" });
            a.Dispose();
        }

        [Test]
        public void TestCharNormalization()
        {
            Analyzer a = new UkrainianMorfologikAnalyzer(TEST_VERSION_CURRENT);
            AssertAnalyzesTo(a, "Ґюмрі та Гюмрі.",
                     new String[] { "Гюмрі", "Гюмрі" });
            a.Dispose();
        }

        [Test]
        public void TestSampleSentence()
        {
            Analyzer a = new UkrainianMorfologikAnalyzer(TEST_VERSION_CURRENT);
            AssertAnalyzesTo(a, "Це — проект генерування словника з тегами частин мови для української мови.",
                     new String[] { "проект", "генерування", "словник", "тег", "частина", "мова", "українська", "український", "Українська", "мова" });
            a.Dispose();
        }

        /** blast some random strings through the analyzer */
        [Test]
        public void TestRandomStrings()
        {
            Analyzer analyzer = new UkrainianMorfologikAnalyzer(TEST_VERSION_CURRENT);
            CheckRandomData(Random, analyzer, 1000 * RandomMultiplier);
            analyzer.Dispose();
        }
    }
}
