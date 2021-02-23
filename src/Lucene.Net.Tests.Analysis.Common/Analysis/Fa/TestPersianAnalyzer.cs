// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Util;
using NUnit.Framework;

namespace Lucene.Net.Analysis.Fa
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
    /// Test the Persian Analyzer
    /// 
    /// </summary>
    public class TestPersianAnalyzer : BaseTokenStreamTestCase
    {

        /// <summary>
        /// This test fails with NPE when the stopwords file is missing in classpath
        /// </summary>
        public virtual void TestResourcesAvailable()
        {
            new PersianAnalyzer(TEST_VERSION_CURRENT);
        }

        /// <summary>
        /// This test shows how the combination of tokenization (breaking on zero-width
        /// non-joiner), normalization (such as treating arabic YEH and farsi YEH the
        /// same), and stopwords creates a light-stemming effect for verbs.
        /// 
        /// These verb forms are from http://en.wikipedia.org/wiki/Persian_grammar
        /// </summary>
        [Test]
        public virtual void TestBehaviorVerbs()
        {
            Analyzer a = new PersianAnalyzer(TEST_VERSION_CURRENT);
            // active present indicative
            AssertAnalyzesTo(a, "می‌خورد", new string[] { "خورد" });
            // active preterite indicative
            AssertAnalyzesTo(a, "خورد", new string[] { "خورد" });
            // active imperfective preterite indicative
            AssertAnalyzesTo(a, "می‌خورد", new string[] { "خورد" });
            // active future indicative
            AssertAnalyzesTo(a, "خواهد خورد", new string[] { "خورد" });
            // active present progressive indicative
            AssertAnalyzesTo(a, "دارد می‌خورد", new string[] { "خورد" });
            // active preterite progressive indicative
            AssertAnalyzesTo(a, "داشت می‌خورد", new string[] { "خورد" });

            // active perfect indicative
            AssertAnalyzesTo(a, "خورده‌است", new string[] { "خورده" });
            // active imperfective perfect indicative
            AssertAnalyzesTo(a, "می‌خورده‌است", new string[] { "خورده" });
            // active pluperfect indicative
            AssertAnalyzesTo(a, "خورده بود", new string[] { "خورده" });
            // active imperfective pluperfect indicative
            AssertAnalyzesTo(a, "می‌خورده بود", new string[] { "خورده" });
            // active preterite subjunctive
            AssertAnalyzesTo(a, "خورده باشد", new string[] { "خورده" });
            // active imperfective preterite subjunctive
            AssertAnalyzesTo(a, "می‌خورده باشد", new string[] { "خورده" });
            // active pluperfect subjunctive
            AssertAnalyzesTo(a, "خورده بوده باشد", new string[] { "خورده" });
            // active imperfective pluperfect subjunctive
            AssertAnalyzesTo(a, "می‌خورده بوده باشد", new string[] { "خورده" });
            // passive present indicative
            AssertAnalyzesTo(a, "خورده می‌شود", new string[] { "خورده" });
            // passive preterite indicative
            AssertAnalyzesTo(a, "خورده شد", new string[] { "خورده" });
            // passive imperfective preterite indicative
            AssertAnalyzesTo(a, "خورده می‌شد", new string[] { "خورده" });
            // passive perfect indicative
            AssertAnalyzesTo(a, "خورده شده‌است", new string[] { "خورده" });
            // passive imperfective perfect indicative
            AssertAnalyzesTo(a, "خورده می‌شده‌است", new string[] { "خورده" });
            // passive pluperfect indicative
            AssertAnalyzesTo(a, "خورده شده بود", new string[] { "خورده" });
            // passive imperfective pluperfect indicative
            AssertAnalyzesTo(a, "خورده می‌شده بود", new string[] { "خورده" });
            // passive future indicative
            AssertAnalyzesTo(a, "خورده خواهد شد", new string[] { "خورده" });
            // passive present progressive indicative
            AssertAnalyzesTo(a, "دارد خورده می‌شود", new string[] { "خورده" });
            // passive preterite progressive indicative
            AssertAnalyzesTo(a, "داشت خورده می‌شد", new string[] { "خورده" });
            // passive present subjunctive
            AssertAnalyzesTo(a, "خورده شود", new string[] { "خورده" });
            // passive preterite subjunctive
            AssertAnalyzesTo(a, "خورده شده باشد", new string[] { "خورده" });
            // passive imperfective preterite subjunctive
            AssertAnalyzesTo(a, "خورده می‌شده باشد", new string[] { "خورده" });
            // passive pluperfect subjunctive
            AssertAnalyzesTo(a, "خورده شده بوده باشد", new string[] { "خورده" });
            // passive imperfective pluperfect subjunctive
            AssertAnalyzesTo(a, "خورده می‌شده بوده باشد", new string[] { "خورده" });

            // active present subjunctive
            AssertAnalyzesTo(a, "بخورد", new string[] { "بخورد" });
        }

        /// <summary>
        /// This test shows how the combination of tokenization and stopwords creates a
        /// light-stemming effect for verbs.
        /// 
        /// In this case, these forms are presented with alternative orthography, using
        /// arabic yeh and whitespace. This yeh phenomenon is common for legacy text
        /// due to some previous bugs in Microsoft Windows.
        /// 
        /// These verb forms are from http://en.wikipedia.org/wiki/Persian_grammar
        /// </summary>
        [Test]
        public virtual void TestBehaviorVerbsDefective()
        {
            Analyzer a = new PersianAnalyzer(TEST_VERSION_CURRENT);
            // active present indicative
            AssertAnalyzesTo(a, "مي خورد", new string[] { "خورد" });
            // active preterite indicative
            AssertAnalyzesTo(a, "خورد", new string[] { "خورد" });
            // active imperfective preterite indicative
            AssertAnalyzesTo(a, "مي خورد", new string[] { "خورد" });
            // active future indicative
            AssertAnalyzesTo(a, "خواهد خورد", new string[] { "خورد" });
            // active present progressive indicative
            AssertAnalyzesTo(a, "دارد مي خورد", new string[] { "خورد" });
            // active preterite progressive indicative
            AssertAnalyzesTo(a, "داشت مي خورد", new string[] { "خورد" });

            // active perfect indicative
            AssertAnalyzesTo(a, "خورده است", new string[] { "خورده" });
            // active imperfective perfect indicative
            AssertAnalyzesTo(a, "مي خورده است", new string[] { "خورده" });
            // active pluperfect indicative
            AssertAnalyzesTo(a, "خورده بود", new string[] { "خورده" });
            // active imperfective pluperfect indicative
            AssertAnalyzesTo(a, "مي خورده بود", new string[] { "خورده" });
            // active preterite subjunctive
            AssertAnalyzesTo(a, "خورده باشد", new string[] { "خورده" });
            // active imperfective preterite subjunctive
            AssertAnalyzesTo(a, "مي خورده باشد", new string[] { "خورده" });
            // active pluperfect subjunctive
            AssertAnalyzesTo(a, "خورده بوده باشد", new string[] { "خورده" });
            // active imperfective pluperfect subjunctive
            AssertAnalyzesTo(a, "مي خورده بوده باشد", new string[] { "خورده" });
            // passive present indicative
            AssertAnalyzesTo(a, "خورده مي شود", new string[] { "خورده" });
            // passive preterite indicative
            AssertAnalyzesTo(a, "خورده شد", new string[] { "خورده" });
            // passive imperfective preterite indicative
            AssertAnalyzesTo(a, "خورده مي شد", new string[] { "خورده" });
            // passive perfect indicative
            AssertAnalyzesTo(a, "خورده شده است", new string[] { "خورده" });
            // passive imperfective perfect indicative
            AssertAnalyzesTo(a, "خورده مي شده است", new string[] { "خورده" });
            // passive pluperfect indicative
            AssertAnalyzesTo(a, "خورده شده بود", new string[] { "خورده" });
            // passive imperfective pluperfect indicative
            AssertAnalyzesTo(a, "خورده مي شده بود", new string[] { "خورده" });
            // passive future indicative
            AssertAnalyzesTo(a, "خورده خواهد شد", new string[] { "خورده" });
            // passive present progressive indicative
            AssertAnalyzesTo(a, "دارد خورده مي شود", new string[] { "خورده" });
            // passive preterite progressive indicative
            AssertAnalyzesTo(a, "داشت خورده مي شد", new string[] { "خورده" });
            // passive present subjunctive
            AssertAnalyzesTo(a, "خورده شود", new string[] { "خورده" });
            // passive preterite subjunctive
            AssertAnalyzesTo(a, "خورده شده باشد", new string[] { "خورده" });
            // passive imperfective preterite subjunctive
            AssertAnalyzesTo(a, "خورده مي شده باشد", new string[] { "خورده" });
            // passive pluperfect subjunctive
            AssertAnalyzesTo(a, "خورده شده بوده باشد", new string[] { "خورده" });
            // passive imperfective pluperfect subjunctive
            AssertAnalyzesTo(a, "خورده مي شده بوده باشد", new string[] { "خورده" });

            // active present subjunctive
            AssertAnalyzesTo(a, "بخورد", new string[] { "بخورد" });
        }

        /// <summary>
        /// This test shows how the combination of tokenization (breaking on zero-width
        /// non-joiner or space) and stopwords creates a light-stemming effect for
        /// nouns, removing the plural -ha.
        /// </summary>
        [Test]
        public virtual void TestBehaviorNouns()
        {
            Analyzer a = new PersianAnalyzer(TEST_VERSION_CURRENT);
            AssertAnalyzesTo(a, "برگ ها", new string[] { "برگ" });
            AssertAnalyzesTo(a, "برگ‌ها", new string[] { "برگ" });
        }

        /// <summary>
        /// Test showing that non-persian text is treated very much like SimpleAnalyzer
        /// (lowercased, etc)
        /// </summary>
        [Test]
        public virtual void TestBehaviorNonPersian()
        {
            Analyzer a = new PersianAnalyzer(TEST_VERSION_CURRENT);
            AssertAnalyzesTo(a, "English test.", new string[] { "english", "test" });
        }

        /// <summary>
        /// Basic test ensuring that tokenStream works correctly.
        /// </summary>
        [Test]
        public virtual void TestReusableTokenStream()
        {
            Analyzer a = new PersianAnalyzer(TEST_VERSION_CURRENT);
            AssertAnalyzesTo(a, "خورده مي شده بوده باشد", new string[] { "خورده" });
            AssertAnalyzesTo(a, "برگ‌ها", new string[] { "برگ" });
        }

        /// <summary>
        /// Test that custom stopwords work, and are not case-sensitive.
        /// </summary>
        [Test]
        public virtual void TestCustomStopwords()
        {
            PersianAnalyzer a = new PersianAnalyzer(TEST_VERSION_CURRENT, new CharArraySet(TEST_VERSION_CURRENT, AsSet("the", "and", "a"), false));
            AssertAnalyzesTo(a, "The quick brown fox.", new string[] { "quick", "brown", "fox" });
        }

        /// <summary>
        /// blast some random strings through the analyzer </summary>
        [Test]
        public virtual void TestRandomStrings()
        {
            CheckRandomData(Random, new PersianAnalyzer(TEST_VERSION_CURRENT), 1000 * RandomMultiplier);
        }
    }
}