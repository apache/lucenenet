using ICU4N.Text;
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

    [Obsolete("remove this when ICUCollationKeyFilter is removed")]
    public class TestICUCollationKeyFilter : CollationTestBase
    {
        private Collator collator = Collator.GetInstance(new CultureInfo("fa"));
        private Analyzer analyzer;

        private BytesRef firstRangeBeginning;
        private BytesRef firstRangeEnd;
        private BytesRef secondRangeBeginning;
        private BytesRef secondRangeEnd;


        public override void SetUp()
        {
            base.SetUp();

            this.analyzer = new TestAnalyzer(collator);
            this.firstRangeBeginning = new BytesRef(EncodeCollationKey
                (collator.GetCollationKey(m_firstRangeBeginningOriginal).ToByteArray()));
            this.firstRangeEnd = new BytesRef(EncodeCollationKey
                (collator.GetCollationKey(m_firstRangeEndOriginal).ToByteArray()));
            this.secondRangeBeginning = new BytesRef(EncodeCollationKey
                (collator.GetCollationKey(m_secondRangeBeginningOriginal).ToByteArray()));
            this.secondRangeEnd = new BytesRef(EncodeCollationKey
                (collator.GetCollationKey(m_secondRangeEndOriginal).ToByteArray()));
        }

        public sealed class TestAnalyzer : Analyzer
        {
            private Collator _collator;

            internal TestAnalyzer(Collator collator)
            {
                _collator = collator;
            }

            protected internal override TokenStreamComponents CreateComponents(string fieldName, TextReader reader)
            {
                Tokenizer result = new KeywordTokenizer(reader);
                return new TokenStreamComponents(result, new ICUCollationKeyFilter(result, _collator));
            }
        }

        [Test]
        public void TestFarsiRangeFilterCollating()
        {
            TestFarsiRangeFilterCollating(analyzer, firstRangeBeginning, firstRangeEnd,
                                      secondRangeBeginning, secondRangeEnd);
        }

        [Test]
        public void TestFarsiRangeQueryCollating()
        {
            TestFarsiRangeQueryCollating(analyzer, firstRangeBeginning, firstRangeEnd,
                                     secondRangeBeginning, secondRangeEnd);
        }

        [Test]
        public void TestFarsiTermRangeQuery()
        {
            TestFarsiTermRangeQuery
                (analyzer, firstRangeBeginning, firstRangeEnd,
                secondRangeBeginning, secondRangeEnd);
        }

        // Test using various international locales with accented characters (which
        // sort differently depending on locale)
        //
        // Copied (and slightly modified) from 
        // org.apache.lucene.search.TestSort.testInternationalSort()
        //  
        [Test]
        public void TestCollationKeySort()
        {
            Analyzer usAnalyzer = new TestAnalyzer(Collator.GetInstance(new CultureInfo("en-us")));
            Analyzer franceAnalyzer
              = new TestAnalyzer(Collator.GetInstance(new CultureInfo("fr")));
            Analyzer swedenAnalyzer
              = new TestAnalyzer(Collator.GetInstance(new CultureInfo("sv-se")));
            Analyzer denmarkAnalyzer
              = new TestAnalyzer(Collator.GetInstance(new CultureInfo("da-dk")));

            // The ICU Collator and java.text.Collator implementations differ in their
            // orderings - "BFJHD" is the ordering for the ICU Collator for Locale.US.
            TestCollationKeySort
                (usAnalyzer, franceAnalyzer, swedenAnalyzer, denmarkAnalyzer,
                "BFJHD", "ECAGI", "BJDFH", "BJDHF");
        }
    }
}
