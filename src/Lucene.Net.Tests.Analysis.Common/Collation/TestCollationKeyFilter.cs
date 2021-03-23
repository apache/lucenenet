// Lucene version compatibility level 4.8.1
#if FEATURE_COLLATION
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using Icu;
using Icu.Collation;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Util;
using NUnit.Framework;

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
        public TestCollationKeyFilter()
        {
            this.analyzer = new TestAnalyzer(this, this.collator);
            this.firstRangeBeginning = new BytesRef(this.EncodeCollationKey(this.collator.GetSortKey(this.FirstRangeBeginningOriginal).KeyData.ToSByteArray()));
            this.firstRangeEnd = new BytesRef(this.EncodeCollationKey(this.collator.GetSortKey(this.FirstRangeEndOriginal).KeyData.ToSByteArray()));
            this.secondRangeBeginning = new BytesRef(this.EncodeCollationKey(this.collator.GetSortKey(this.SecondRangeBeginningOriginal).KeyData.ToSByteArray()));
            this.secondRangeEnd = new BytesRef(this.EncodeCollationKey(this.collator.GetSortKey(this.SecondRangeEndOriginal).KeyData.ToSByteArray()));
        }

        // the sort order of Ø versus U depends on the version of the rules being used
        // for the inherited root locale: Ø's order isnt specified in Locale.US since 
        // its not used in english.
        internal bool oStrokeFirst = Collator.Create(new CultureInfo("")).Compare("Ø", "U") < 0;

        // Neither Java 1.4.2 nor 1.5.0 has Farsi Locale collation available in
        // RuleBasedCollator.  However, the Arabic Locale seems to order the Farsi
        // characters properly.
        private readonly Collator collator = Collator.Create(new CultureInfo("ar"));
        private Analyzer analyzer;

        private BytesRef firstRangeBeginning;
        private BytesRef firstRangeEnd;
        private BytesRef secondRangeBeginning;
        private BytesRef secondRangeEnd;
        
        public sealed class TestAnalyzer : Analyzer
        {
            private readonly TestCollationKeyFilter outerInstance;

            internal Collator _collator;

            internal TestAnalyzer(TestCollationKeyFilter outerInstance, Collator collator)
            {
                this.outerInstance = outerInstance;
                this._collator = collator;
            }

            protected internal override TokenStreamComponents CreateComponents(String fieldName, TextReader reader)
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
            Analyzer usAnalyzer = new TestAnalyzer(this, GetCollator("en-US"));
            Collator franceCollator = GetCollator("fr");
            franceCollator.FrenchCollation = FrenchCollation.On;
            Analyzer franceAnalyzer = new TestAnalyzer(this, franceCollator);

            // `useFallback: true` on both Swedish and Danish collators in
            // case the region specific collator is not found.
            Analyzer swedenAnalyzer = new TestAnalyzer(this, GetCollator("sv-SE", "sv"));
            Analyzer denmarkAnalyzer = new TestAnalyzer(this, GetCollator("da-DK", "da"));

            // The ICU Collator and Sun java.text.Collator implementations differ in their
            // orderings - "BFJDH" is the ordering for java.text.Collator for Locale.US.
            this.TestCollationKeySort(usAnalyzer, franceAnalyzer, swedenAnalyzer, denmarkAnalyzer, 
                this.oStrokeFirst ? "BFJHD" : "BFJDH", "EACGI", "BJDFH", "BJDHF");
        }

        private Collator GetCollator(params string[] localeNames)
        {
            var firstMatchingLocale = localeNames
                .Select(x => new Locale(x))
                .FirstOrDefault(x => availableCollationLocales.Contains(x.Id));

            if (firstMatchingLocale == default)
            {
                throw new ArgumentException($"Could not find a collator locale matching any of the following: {string.Join(", ", localeNames)}");
            }

            Collator collator = RuleBasedCollator.Create(firstMatchingLocale.Id);

            return collator;
        }

        // Original Java Code:
        //public void testCollationKeySort() throws Exception {
        //  Analyzer usAnalyzer = new TestAnalyzer(Collator.getInstance(Locale.US));
        //  Analyzer franceAnalyzer 
        //    = new TestAnalyzer(Collator.getInstance(Locale.FRANCE));
        //  Analyzer swedenAnalyzer 
        //    = new TestAnalyzer(Collator.getInstance(new Locale("sv", "se")));
        //  Analyzer denmarkAnalyzer 
        //    = new TestAnalyzer(Collator.getInstance(new Locale("da", "dk")));

        //  // The ICU Collator and Sun java.text.Collator implementations differ in their
        //  // orderings - "BFJDH" is the ordering for java.text.Collator for Locale.US.
        //  testCollationKeySort
        //  (usAnalyzer, franceAnalyzer, swedenAnalyzer, denmarkAnalyzer, 
        //   oStrokeFirst ? "BFJHD" : "BFJDH", "EACGI", "BJDFH", "BJDHF");
        //}
    }
}
#endif