/**
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

using System;
using System.IO;
using System.Collections;

using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Analysis.AR
{


    /**
     * Test the Arabic Analyzer
     *
     */
    [TestFixture]
    public class TestArabicAnalyzer : BaseTokenStreamTestCase
    {

        /** This test fails with NPE when the 
         * stopwords file is missing in classpath */
        [Test]
        public void TestResourcesAvailable()
        {
            new ArabicAnalyzer();
        }

        /**
         * Some simple tests showing some features of the analyzer, how some regular forms will conflate
         */
        [Test]
        public void TestBasicFeatures()
        {
            ArabicAnalyzer a = new ArabicAnalyzer();
            AssertAnalyzesTo(a, "كبير", new String[] { "كبير" });
            AssertAnalyzesTo(a, "كبيرة", new String[] { "كبير" }); // feminine marker

            AssertAnalyzesTo(a, "مشروب", new String[] { "مشروب" });
            AssertAnalyzesTo(a, "مشروبات", new String[] { "مشروب" }); // plural -at

            AssertAnalyzesTo(a, "أمريكيين", new String[] { "امريك" }); // plural -in
            AssertAnalyzesTo(a, "امريكي", new String[] { "امريك" }); // singular with bare alif

            AssertAnalyzesTo(a, "كتاب", new String[] { "كتاب" });
            AssertAnalyzesTo(a, "الكتاب", new String[] { "كتاب" }); // definite article

            AssertAnalyzesTo(a, "ما ملكت أيمانكم", new String[] { "ملكت", "ايمانكم" });
            AssertAnalyzesTo(a, "الذين ملكت أيمانكم", new String[] { "ملكت", "ايمانكم" }); // stopwords
        }

        /**
         * Simple tests to show things are getting reset correctly, etc.
         */
        [Test]
        public void TestReusableTokenStream()
        {
            ArabicAnalyzer a = new ArabicAnalyzer();
            AssertAnalyzesToReuse(a, "كبير", new String[] { "كبير" });
            AssertAnalyzesToReuse(a, "كبيرة", new String[] { "كبير" }); // feminine marker
        }

        /**
         * Non-arabic text gets treated in a similar way as SimpleAnalyzer.
         */
        [Test]
        public void TestEnglishInput()
        {
            AssertAnalyzesTo(new ArabicAnalyzer(), "English text.", new String[] {
        "english", "text" });
        }

        /**
         * Test that custom stopwords work, and are not case-sensitive.
         */
        [Test]
        public void TestCustomStopwords()
        {
            ArabicAnalyzer a = new ArabicAnalyzer(new String[] { "the", "and", "a" });
            AssertAnalyzesTo(a, "The quick brown fox.", new String[] { "quick", "brown", "fox" });
        }
    }
}