// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Util;
using NUnit.Framework;

namespace Lucene.Net.Analysis.Hi
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
    /// Tests the HindiAnalyzer
    /// </summary>
    public class TestHindiAnalyzer : BaseTokenStreamTestCase
    {
        /// <summary>
        /// This test fails with NPE when the 
        /// stopwords file is missing in classpath 
        /// </summary>
        public virtual void TestResourcesAvailable()
        {
            new HindiAnalyzer(TEST_VERSION_CURRENT);
        }

        [Test]
        public virtual void TestBasics()
        {
            Analyzer a = new HindiAnalyzer(TEST_VERSION_CURRENT);
            // two ways to write 'hindi' itself.
            CheckOneTerm(a, "हिन्दी", "हिंद");
            CheckOneTerm(a, "हिंदी", "हिंद");
        }

        [Test]
        public virtual void TestExclusionSet()
        {
            CharArraySet exclusionSet = new CharArraySet(TEST_VERSION_CURRENT, AsSet("हिंदी"), false);
            Analyzer a = new HindiAnalyzer(TEST_VERSION_CURRENT, HindiAnalyzer.DefaultStopSet, exclusionSet);
            CheckOneTerm(a, "हिंदी", "हिंदी");
        }

        /// <summary>
        /// blast some random strings through the analyzer </summary>
        [Test]
        public virtual void TestRandomStrings()
        {
            CheckRandomData(Random, new HindiAnalyzer(TEST_VERSION_CURRENT), 1000 * RandomMultiplier);
        }
    }
}