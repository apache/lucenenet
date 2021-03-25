// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Analysis.It
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

    public class TestItalianAnalyzer : BaseTokenStreamTestCase
    {
        /// <summary>
        /// This test fails with NPE when the 
        /// stopwords file is missing in classpath 
        /// </summary>
        [Test]
        public virtual void TestResourcesAvailable()
        {
            new ItalianAnalyzer(TEST_VERSION_CURRENT);
        }

        /// <summary>
        /// test stopwords and stemming </summary>
        [Test]
        public virtual void TestBasics()
        {
            Analyzer a = new ItalianAnalyzer(TEST_VERSION_CURRENT);
            // stemming
            CheckOneTerm(a, "abbandonata", "abbandonat");
            CheckOneTerm(a, "abbandonati", "abbandonat");
            // stopword
            AssertAnalyzesTo(a, "dallo", new string[] { });
        }

        /// <summary>
        /// test use of exclusion set </summary>
        [Test]
        public virtual void TestExclude()
        {
            CharArraySet exclusionSet = new CharArraySet(TEST_VERSION_CURRENT, AsSet("abbandonata"), false);
            Analyzer a = new ItalianAnalyzer(TEST_VERSION_CURRENT, ItalianAnalyzer.DefaultStopSet, exclusionSet);
            CheckOneTerm(a, "abbandonata", "abbandonata");
            CheckOneTerm(a, "abbandonati", "abbandonat");
        }

        /// <summary>
        /// blast some random strings through the analyzer </summary>
        [Test]
        public virtual void TestRandomStrings()
        {
            CheckRandomData(Random, new ItalianAnalyzer(TEST_VERSION_CURRENT), 1000 * RandomMultiplier);
        }

        /// <summary>
        /// test that the elisionfilter is working </summary>
        [Test]
        public virtual void TestContractions()
        {
            Analyzer a = new ItalianAnalyzer(TEST_VERSION_CURRENT);
            AssertAnalyzesTo(a, "dell'Italia", new string[] { "ital" });
            AssertAnalyzesTo(a, "l'Italiano", new string[] { "italian" });
        }

        /// <summary>
        /// test that we don't enable this before 3.2 </summary>
        [Test]
        public virtual void TestContractionsBackwards()
        {
#pragma warning disable 612, 618
            Analyzer a = new ItalianAnalyzer(LuceneVersion.LUCENE_31);
#pragma warning restore 612, 618
            AssertAnalyzesTo(a, "dell'Italia", new string[] { "dell'ital" });
            AssertAnalyzesTo(a, "l'Italiano", new string[] { "l'ital" });
        }
    }
}