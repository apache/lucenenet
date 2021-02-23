// Lucene version compatibility level 4.8.1
#if FEATURE_COLLATION
using Icu;
using Icu.Collation;
using Lucene.Net.Analysis;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Globalization;
using System.Linq;

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
        public TestCollationKeyAnalyzer()
        {
            this.analyzer = new CollationKeyAnalyzer(LuceneTestCase.TEST_VERSION_CURRENT, this.collator);
            this.firstRangeBeginning = new BytesRef(this.collator.GetSortKey(this.FirstRangeBeginningOriginal).KeyData);
            this.firstRangeEnd = new BytesRef(this.collator.GetSortKey(this.FirstRangeEndOriginal).KeyData);
            this.secondRangeBeginning = new BytesRef(this.collator.GetSortKey(this.SecondRangeBeginningOriginal).KeyData);
            this.secondRangeEnd = new BytesRef(this.collator.GetSortKey(this.SecondRangeEndOriginal).KeyData);
        }

        /// <summary>
        /// the sort order of Ø versus U depends on the version of the rules being used
        /// for the inherited root locale: Ø's order isnt specified in Locale.US since 
        /// its not used in english.
        /// </summary>
        private readonly bool oStrokeFirst = Collator.Create("en-US").Compare("Ø", "U") < 0;

        /// <summary>
        /// Neither Java 1.4.2 nor 1.5.0 has Farsi Locale collation available in
        /// RuleBasedCollator.  However, the Arabic Locale seems to order the Farsi
        /// characters properly.
        /// </summary>
        private readonly Collator collator = Collator.Create(new CultureInfo("ar"));
        private Analyzer analyzer;

        private BytesRef firstRangeBeginning;
        private BytesRef firstRangeEnd;
        private BytesRef secondRangeBeginning;
        private BytesRef secondRangeEnd;

        [Test]
        public virtual void TestInitVars()
        {
            var sortKey = this.collator.GetSortKey(this.FirstRangeBeginningOriginal);
            
            var r = new BytesRef(sortKey.KeyData);
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
            Analyzer usAnalyzer = new CollationKeyAnalyzer(LuceneTestCase.TEST_VERSION_CURRENT, GetCollator("en-US"));
            Collator franceCollator = GetCollator("fr");
            franceCollator.FrenchCollation = FrenchCollation.On;

            // `useFallback: true` on both Swedish and Danish collators in
            // case the region specific collator is not found.
            Analyzer franceAnalyzer = new CollationKeyAnalyzer(LuceneTestCase.TEST_VERSION_CURRENT, franceCollator);
            Analyzer swedenAnalyzer = new CollationKeyAnalyzer(LuceneTestCase.TEST_VERSION_CURRENT, GetCollator("sv-SE", "sv"));
            Analyzer denmarkAnalyzer = new CollationKeyAnalyzer(LuceneTestCase.TEST_VERSION_CURRENT, GetCollator("da-DK", "da"));

            var expectedUSKeySort = this.oStrokeFirst ? "BFJHD" : "BFJDH";

            // The ICU Collator and Sun java.text.Collator implementations differ in their
            // orderings - "BFJDH" is the ordering for java.text.Collator for Locale.US.
            this.TestCollationKeySort(usAnalyzer, franceAnalyzer, swedenAnalyzer, denmarkAnalyzer, 
                expectedUSKeySort, "EACGI", "BJDFH", "BJDHF");
        }

        // Original Java Code:
        //public void testCollationKeySort() throws Exception {
        //  Analyzer usAnalyzer 
        //    = new CollationKeyAnalyzer(TEST_VERSION_CURRENT, Collator.getInstance(Locale.US));
        //  Analyzer franceAnalyzer 
        //    = new CollationKeyAnalyzer(TEST_VERSION_CURRENT, Collator.getInstance(Locale.FRANCE));
        //  Analyzer swedenAnalyzer 
        //    = new CollationKeyAnalyzer(TEST_VERSION_CURRENT, Collator.getInstance(new Locale("sv", "se")));
        //  Analyzer denmarkAnalyzer 
        //    = new CollationKeyAnalyzer(TEST_VERSION_CURRENT, Collator.getInstance(new Locale("da", "dk")));
    
        //  // The ICU Collator and Sun java.text.Collator implementations differ in their
        //  // orderings - "BFJDH" is the ordering for java.text.Collator for Locale.US.
        //  testCollationKeySort
        //  (usAnalyzer, franceAnalyzer, swedenAnalyzer, denmarkAnalyzer, 
        //   oStrokeFirst ? "BFJHD" : "BFJDH", "EACGI", "BJDFH", "BJDHF");
        //}

        [Test]
        public virtual void TestThreadSafe()
        {
            var iters = 20 * LuceneTestCase.RANDOM_MULTIPLIER;
            for (var i = 0; i < iters; i++)
            {
                var collator = Collator.Create(new CultureInfo("de"));
                collator.Strength = CollationStrength.Primary;
                this.AssertThreadSafe(new CollationKeyAnalyzer(LuceneTestCase.TEST_VERSION_CURRENT, collator));
            }
        }

        /// <summary>
        /// LUCENENET
        /// Get the first available collator based on the given localeNames.
        /// icu.net may not have all the collation rules.
        /// </summary>
        private Collator GetCollator(params string[] localeNames)
        {
            var firstAvailableLocale = localeNames
                .Select(x => new Locale(x))
                .FirstOrDefault(x => availableCollationLocales.Contains(x.Id));

            if (firstAvailableLocale == default)
                throw new ArgumentException($"None of the locales are available: {string.Join(", ", localeNames)}");

            Collator collator = Collator.Create(firstAvailableLocale.Id);

            return collator;
        }
    }
}
#endif