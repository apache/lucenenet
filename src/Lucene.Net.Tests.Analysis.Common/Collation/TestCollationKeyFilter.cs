// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Globalization;
using System.IO;

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

    [TestFixture]
    [Obsolete("remove when CollationKeyFilter is removed.")]
    public class TestCollationKeyFilter : CollationTestBase
    {
        private readonly CompareInfo collator = CompareInfo.GetCompareInfo("fa");
        private readonly Analyzer analyzer;

        private readonly BytesRef firstRangeBeginning;
        private readonly BytesRef firstRangeEnd;
        private readonly BytesRef secondRangeBeginning;
        private readonly BytesRef secondRangeEnd;

        public TestCollationKeyFilter()
        {
            this.analyzer = new TestAnalyzer(this.collator);
            this.firstRangeBeginning = new BytesRef(this.EncodeCollationKey(this.collator.GetSortKey(m_firstRangeBeginningOriginal).KeyData));
            this.firstRangeEnd = new BytesRef(this.EncodeCollationKey(this.collator.GetSortKey(m_firstRangeEndOriginal).KeyData));
            this.secondRangeBeginning = new BytesRef(this.EncodeCollationKey(this.collator.GetSortKey(m_secondRangeBeginningOriginal).KeyData));
            this.secondRangeEnd = new BytesRef(this.EncodeCollationKey(this.collator.GetSortKey(m_secondRangeEndOriginal).KeyData));
        }

        // the sort order of Ø versus U depends on the version of the rules being used
        // for the inherited root locale: Ø's order isn't specified in Locale.US since
        // its not used in english.
        internal bool oStrokeFirst = CompareInfo.GetCompareInfo("").Compare("Ø", "U") < 0;

        public sealed class TestAnalyzer : Analyzer
        {
            internal CompareInfo _collator;

            internal TestAnalyzer(CompareInfo collator)
            {
                this._collator = collator;
            }

            protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                Tokenizer result = new KeywordTokenizer(reader);
                return new TokenStreamComponents(result, new CollationKeyFilter(result, this._collator));
            }
        }

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
            Analyzer usAnalyzer = new TestAnalyzer(CompareInfo.GetCompareInfo("en-US"));
            Analyzer franceAnalyzer = new TestAnalyzer(CompareInfo.GetCompareInfo("fr"));
            Analyzer swedenAnalyzer = new TestAnalyzer(CompareInfo.GetCompareInfo("sv-SE"));
            Analyzer denmarkAnalyzer = new TestAnalyzer(CompareInfo.GetCompareInfo("da-DK"));

            this.TestCollationKeySort(usAnalyzer, franceAnalyzer, swedenAnalyzer, denmarkAnalyzer,
                this.oStrokeFirst ? "BFJHD" : "BFJDH", FrenchResult, "BJDFH", "BJDHF");
        }
    }
}
