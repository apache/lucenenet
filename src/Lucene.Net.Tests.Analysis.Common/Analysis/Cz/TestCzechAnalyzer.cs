// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using NUnit.Framework;
using System;

namespace Lucene.Net.Analysis.Cz
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
    /// Test the CzechAnalyzer
    /// 
    /// Before Lucene 3.1, CzechAnalyzer was a StandardAnalyzer with a custom 
    /// stopword list. As of 3.1 it also includes a stemmer.
    /// 
    /// </summary>
    public class TestCzechAnalyzer : BaseTokenStreamTestCase
    {
        /// @deprecated (3.1) Remove this test when support for 3.0 indexes is no longer needed. 
        [Test]
        [Obsolete("(3.1) Remove this test when support for 3.0 indexes is no longer needed.")]
        public virtual void TestStopWordLegacy()
        {
            AssertAnalyzesTo(new CzechAnalyzer(LuceneVersion.LUCENE_30), "Pokud mluvime o volnem", new string[] { "mluvime", "volnem" });
        }

        [Test]
        public virtual void TestStopWord()
        {
            AssertAnalyzesTo(new CzechAnalyzer(TEST_VERSION_CURRENT), "Pokud mluvime o volnem", new string[] { "mluvim", "voln" });
        }

        /// @deprecated (3.1) Remove this test when support for 3.0 indexes is no longer needed. 
        [Test]
        [Obsolete("(3.1) Remove this test when support for 3.0 indexes is no longer needed.")]
        public virtual void TestReusableTokenStreamLegacy()
        {
            Analyzer analyzer = new CzechAnalyzer(LuceneVersion.LUCENE_30);
            AssertAnalyzesTo(analyzer, "Pokud mluvime o volnem", new string[] { "mluvime", "volnem" });
            AssertAnalyzesTo(analyzer, "Česká Republika", new string[] { "česká", "republika" });
        }

        [Test]
        public virtual void TestReusableTokenStream()
        {
            Analyzer analyzer = new CzechAnalyzer(TEST_VERSION_CURRENT);
            AssertAnalyzesTo(analyzer, "Pokud mluvime o volnem", new string[] { "mluvim", "voln" });
            AssertAnalyzesTo(analyzer, "Česká Republika", new string[] { "česk", "republik" });
        }

        [Test]
        public virtual void TestWithStemExclusionSet()
        {
            CharArraySet set = new CharArraySet(TEST_VERSION_CURRENT, 1, true);
            set.add("hole");
            CzechAnalyzer cz = new CzechAnalyzer(TEST_VERSION_CURRENT, CharArraySet.Empty, set);
            AssertAnalyzesTo(cz, "hole desek", new string[] { "hole", "desk" });
        }

        /// <summary>
        /// blast some random strings through the analyzer </summary>
        [Test]
        public virtual void TestRandomStrings()
        {
            CheckRandomData(Random, new CzechAnalyzer(TEST_VERSION_CURRENT), 1000 * RandomMultiplier);
        }
    }
}