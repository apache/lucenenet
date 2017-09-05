using Icu.Collation;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Globalization;
using System.IO;

namespace Lucene.Net.Collation
{
    [Obsolete("remove this when ICUCollationKeyFilter is removed")]
    public class TestICUCollationKeyFilter : CollationTestBase
    {
        private Collator collator = Collator.Create(new CultureInfo("fa"));
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
                (collator.GetSortKey(FirstRangeBeginningOriginal).KeyData));
            this.firstRangeEnd = new BytesRef(EncodeCollationKey
                (collator.GetSortKey(FirstRangeEndOriginal).KeyData));
            this.secondRangeBeginning = new BytesRef(EncodeCollationKey
                (collator.GetSortKey(SecondRangeBeginningOriginal).KeyData));
            this.secondRangeEnd = new BytesRef(EncodeCollationKey
                (collator.GetSortKey(SecondRangeEndOriginal).KeyData));
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
            Analyzer usAnalyzer = new TestAnalyzer(Collator.Create(new CultureInfo("en-us"), Collator.Fallback.FallbackAllowed));
            Analyzer franceAnalyzer
              = new TestAnalyzer(Collator.Create(new CultureInfo("fr")));
            Analyzer swedenAnalyzer
              = new TestAnalyzer(Collator.Create(new CultureInfo("sv-se"), Collator.Fallback.FallbackAllowed));
            Analyzer denmarkAnalyzer
              = new TestAnalyzer(Collator.Create(new CultureInfo("da-dk"), Collator.Fallback.FallbackAllowed));

            // The ICU Collator and java.text.Collator implementations differ in their
            // orderings - "BFJHD" is the ordering for the ICU Collator for Locale.US.
            TestCollationKeySort
                (usAnalyzer, franceAnalyzer, swedenAnalyzer, denmarkAnalyzer,
                "BFJHD", "ECAGI", "BJDFH", "BJDHF");
        }
    }
}
