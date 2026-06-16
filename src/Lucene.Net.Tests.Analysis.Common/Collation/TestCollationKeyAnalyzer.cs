// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis;
using Lucene.Net.Util;
using NUnit.Framework;
using System.Globalization;

namespace Lucene.Net.Collation
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

    [SuppressCodecs("Lucene3x")]
    [TestFixture]
    public class TestCollationKeyAnalyzer : CollationTestBase
    {
        private readonly CompareInfo collator = CompareInfo.GetCompareInfo("fa");
        private readonly Analyzer analyzer;

        private readonly BytesRef firstRangeBeginning;
        private readonly BytesRef firstRangeEnd;
        private readonly BytesRef secondRangeBeginning;
        private readonly BytesRef secondRangeEnd;

        public TestCollationKeyAnalyzer()
        {
            this.analyzer = new CollationKeyAnalyzer(LuceneTestCase.TEST_VERSION_CURRENT, this.collator);
            this.firstRangeBeginning = new BytesRef(this.collator.GetSortKey(m_firstRangeBeginningOriginal).KeyData);
            this.firstRangeEnd = new BytesRef(this.collator.GetSortKey(m_firstRangeEndOriginal).KeyData);
            this.secondRangeBeginning = new BytesRef(this.collator.GetSortKey(m_secondRangeBeginningOriginal).KeyData);
            this.secondRangeEnd = new BytesRef(this.collator.GetSortKey(m_secondRangeEndOriginal).KeyData);
        }

        /// <summary>
        /// the sort order of Ø versus U depends on the version of the rules being used
        /// for the inherited root locale: Ø's order isn't specified in Locale.US since
        /// its not used in english.
        /// </summary>
        private readonly bool oStrokeFirst = CompareInfo.GetCompareInfo("en-US").Compare("Ø", "U") < 0;

        [Test]
        public virtual void TestFarsiRangeFilterCollating()
        {
            this.TestFarsiRangeFilterCollating(this.analyzer, this.firstRangeBeginning, this.firstRangeEnd, this.secondRangeBeginning, this.secondRangeEnd);
        }

        [Test]
        public virtual void TestFarsiRangeQueryCollating()
        {
            this.TestFarsiRangeQueryCollating(this.analyzer, this.firstRangeBeginning, this.firstRangeEnd, this.secondRangeBeginning, this.secondRangeEnd);
        }

        [Test]
        public virtual void TestFarsiTermRangeQuery()
        {
            this.TestFarsiTermRangeQuery(this.analyzer, this.firstRangeBeginning, this.firstRangeEnd, this.secondRangeBeginning, this.secondRangeEnd);
        }

        [Test]
        public virtual void TestCollationKeySort()
        {
            Analyzer usAnalyzer = new CollationKeyAnalyzer(LuceneTestCase.TEST_VERSION_CURRENT, CompareInfo.GetCompareInfo("en-US"));
            Analyzer franceAnalyzer = new CollationKeyAnalyzer(LuceneTestCase.TEST_VERSION_CURRENT, CompareInfo.GetCompareInfo("fr"));
            Analyzer swedenAnalyzer = new CollationKeyAnalyzer(LuceneTestCase.TEST_VERSION_CURRENT, CompareInfo.GetCompareInfo("sv-SE"));
            Analyzer denmarkAnalyzer = new CollationKeyAnalyzer(LuceneTestCase.TEST_VERSION_CURRENT, CompareInfo.GetCompareInfo("da-DK"));

            var expectedUSKeySort = this.oStrokeFirst ? "BFJHD" : "BFJDH";

            this.TestCollationKeySort(usAnalyzer, franceAnalyzer, swedenAnalyzer, denmarkAnalyzer,
                expectedUSKeySort, FrenchResult, "BJDFH", "BJDHF");
        }

        [Test]
        public virtual void TestThreadSafe()
        {
            var iters = 20 * LuceneTestCase.RandomMultiplier;
            for (var i = 0; i < iters; i++)
            {
                CompareInfo collator = CompareInfo.GetCompareInfo("de");
                this.AssertThreadSafe(new CollationKeyAnalyzer(LuceneTestCase.TEST_VERSION_CURRENT, collator,
                    CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace));
            }
        }
    }
}
