// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using NUnit.Framework;
using System;

namespace Lucene.Net.Analysis.Ru
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
    /// Test case for RussianAnalyzer.
    /// </summary>

    public class TestRussianAnalyzer : BaseTokenStreamTestCase
    {

        /// <summary>
        /// Check that RussianAnalyzer doesnt discard any numbers </summary>
        [Test]
        public virtual void TestDigitsInRussianCharset()
        {
            RussianAnalyzer ra = new RussianAnalyzer(TEST_VERSION_CURRENT);
            AssertAnalyzesTo(ra, "text 1000", new string[] { "text", "1000" });
        }

        /// @deprecated (3.1) remove this test in Lucene 5.0: stopwords changed 
        [Test]
        [Obsolete("(3.1) remove this test in Lucene 5.0: stopwords changed")]
        public virtual void TestReusableTokenStream30()
        {
            Analyzer a = new RussianAnalyzer(LuceneVersion.LUCENE_30);
            AssertAnalyzesTo(a, "Вместе с тем о силе электромагнитной энергии имели представление еще", new string[] { "вмест", "сил", "электромагнитн", "энерг", "имел", "представлен" });
            AssertAnalyzesTo(a, "Но знание это хранилось в тайне", new string[] { "знан", "хран", "тайн" });
        }

        [Test]
        public virtual void TestReusableTokenStream()
        {
            Analyzer a = new RussianAnalyzer(TEST_VERSION_CURRENT);
            AssertAnalyzesTo(a, "Вместе с тем о силе электромагнитной энергии имели представление еще", new string[] { "вмест", "сил", "электромагнитн", "энерг", "имел", "представлен" });
            AssertAnalyzesTo(a, "Но знание это хранилось в тайне", new string[] { "знан", "эт", "хран", "тайн" });
        }


        [Test]
        public virtual void TestWithStemExclusionSet()
        {
            CharArraySet set = new CharArraySet(TEST_VERSION_CURRENT, 1, true);
            set.add("представление");
            Analyzer a = new RussianAnalyzer(TEST_VERSION_CURRENT, RussianAnalyzer.DefaultStopSet, set);
            AssertAnalyzesTo(a, "Вместе с тем о силе электромагнитной энергии имели представление еще", new string[] { "вмест", "сил", "электромагнитн", "энерг", "имел", "представление" });

        }

        /// <summary>
        /// blast some random strings through the analyzer </summary>
        [Test]
        public virtual void TestRandomStrings()
        {
            CheckRandomData(Random, new RussianAnalyzer(TEST_VERSION_CURRENT), 1000 * RandomMultiplier);
        }
    }
}