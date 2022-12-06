// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Util;
using NUnit.Framework;

namespace Lucene.Net.Analysis.Ar
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
    /// Test the Arabic Analyzer
    /// 
    /// </summary>
    public class TestArabicAnalyzer : BaseTokenStreamTestCase
    {

        /// <summary>
        /// This test fails with NPE when the 
        /// stopwords file is missing in classpath 
        /// </summary>
        [Test]
        public virtual void TestResourcesAvailable()
        {
            new ArabicAnalyzer(TEST_VERSION_CURRENT);
        }

        /// <summary>
        /// Some simple tests showing some features of the analyzer, how some regular forms will conflate
        /// </summary>
        [Test]
        public virtual void TestBasicFeatures()
        {
            ArabicAnalyzer a = new ArabicAnalyzer(TEST_VERSION_CURRENT);
            AssertAnalyzesTo(a, "كبير", new string[] { "كبير" });
            AssertAnalyzesTo(a, "كبيرة", new string[] { "كبير" }); // feminine marker

            AssertAnalyzesTo(a, "مشروب", new string[] { "مشروب" });
            AssertAnalyzesTo(a, "مشروبات", new string[] { "مشروب" }); // plural -at

            AssertAnalyzesTo(a, "أمريكيين", new string[] { "امريك" }); // plural -in
            AssertAnalyzesTo(a, "امريكي", new string[] { "امريك" }); // singular with bare alif

            AssertAnalyzesTo(a, "كتاب", new string[] { "كتاب" });
            AssertAnalyzesTo(a, "الكتاب", new string[] { "كتاب" }); // definite article

            AssertAnalyzesTo(a, "ما ملكت أيمانكم", new string[] { "ملكت", "ايمانكم" });
            AssertAnalyzesTo(a, "الذين ملكت أيمانكم", new string[] { "ملكت", "ايمانكم" }); // stopwords
        }

        /// <summary>
        /// Simple tests to show things are getting reset correctly, etc.
        /// </summary>
        [Test]
        public virtual void TestReusableTokenStream()
        {
            ArabicAnalyzer a = new ArabicAnalyzer(TEST_VERSION_CURRENT);
            AssertAnalyzesTo(a, "كبير", new string[] { "كبير" });
            AssertAnalyzesTo(a, "كبيرة", new string[] { "كبير" }); // feminine marker
        }

        /// <summary>
        /// Non-arabic text gets treated in a similar way as SimpleAnalyzer.
        /// </summary>
        [Test]
        public virtual void TestEnglishInput()
        {
            AssertAnalyzesTo(new ArabicAnalyzer(TEST_VERSION_CURRENT), "English text.", new string[] { "english", "text" });
        }

        /// <summary>
        /// Test that custom stopwords work, and are not case-sensitive.
        /// </summary>
        [Test]
        public virtual void TestCustomStopwords()
        {
            CharArraySet set = new CharArraySet(TEST_VERSION_CURRENT, AsSet("the", "and", "a"), false);
            ArabicAnalyzer a = new ArabicAnalyzer(TEST_VERSION_CURRENT, set);
            AssertAnalyzesTo(a, "The quick brown fox.", new string[] { "quick", "brown", "fox" });
        }

        [Test]
        public virtual void TestWithStemExclusionSet()
        {
            CharArraySet set = new CharArraySet(TEST_VERSION_CURRENT, AsSet("ساهدهات"), false);
            ArabicAnalyzer a = new ArabicAnalyzer(TEST_VERSION_CURRENT, CharArraySet.Empty, set);
            AssertAnalyzesTo(a, "كبيرة the quick ساهدهات", new string[] { "كبير", "the", "quick", "ساهدهات" });
            AssertAnalyzesTo(a, "كبيرة the quick ساهدهات", new string[] { "كبير", "the", "quick", "ساهدهات" });


            a = new ArabicAnalyzer(TEST_VERSION_CURRENT, CharArraySet.Empty, CharArraySet.Empty);
            AssertAnalyzesTo(a, "كبيرة the quick ساهدهات", new string[] { "كبير", "the", "quick", "ساهد" });
            AssertAnalyzesTo(a, "كبيرة the quick ساهدهات", new string[] { "كبير", "the", "quick", "ساهد" });
        }

        /// <summary>
        /// blast some random strings through the analyzer </summary>
        [Test]
        public virtual void TestRandomStrings()
        {
            CheckRandomData(Random, new ArabicAnalyzer(TEST_VERSION_CURRENT), 1000 * RandomMultiplier);
        }
    }
}