using Icu.Collation;
using Lucene.Net.Analysis;
using Lucene.Net.Util;
using NUnit.Framework;
using System.Globalization;

namespace Lucene.Net.Collation
{
    [SuppressCodecs("Lucene3x")]
    public class TestICUCollationKeyAnalyzer : CollationTestBase
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

            this.analyzer = new ICUCollationKeyAnalyzer(TEST_VERSION_CURRENT, collator);
            this.firstRangeBeginning = new BytesRef
          (collator.GetSortKey(FirstRangeBeginningOriginal).KeyData);
            this.firstRangeEnd = new BytesRef
          (collator.GetSortKey(FirstRangeEndOriginal).KeyData);
            this.secondRangeBeginning = new BytesRef
          (collator.GetSortKey(SecondRangeBeginningOriginal).KeyData);
            this.secondRangeEnd = new BytesRef
          (collator.GetSortKey(SecondRangeEndOriginal).KeyData);
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
            Analyzer usAnalyzer = new ICUCollationKeyAnalyzer
              (TEST_VERSION_CURRENT, Collator.Create(new CultureInfo("en-us"), Collator.Fallback.FallbackAllowed));

            Analyzer franceAnalyzer = new ICUCollationKeyAnalyzer
              (TEST_VERSION_CURRENT, Collator.Create(new CultureInfo("fr")));

            Analyzer swedenAnalyzer = new ICUCollationKeyAnalyzer
              (TEST_VERSION_CURRENT, Collator.Create(new CultureInfo("sv-se"), Collator.Fallback.FallbackAllowed));

            Analyzer denmarkAnalyzer = new ICUCollationKeyAnalyzer
              (TEST_VERSION_CURRENT, Collator.Create(new CultureInfo("da-dk"), Collator.Fallback.FallbackAllowed));

            // The ICU Collator and java.text.Collator implementations differ in their
            // orderings - "BFJHD" is the ordering for the ICU Collator for Locale.ROOT.
            TestCollationKeySort
                (usAnalyzer, franceAnalyzer, swedenAnalyzer, denmarkAnalyzer,
                "BFJHD", "ECAGI", "BJDFH", "BJDHF");
        }

        [Test]
        public void TestThreadSafe()
        {
            int iters = 20 * RANDOM_MULTIPLIER;
            for (int i = 0; i < iters; i++)
            {
                CultureInfo locale = new CultureInfo("de");
                Collator collator = Collator.Create(locale);
                collator.Strength = CollationStrength.Identical;
                AssertThreadSafe(new ICUCollationKeyAnalyzer(TEST_VERSION_CURRENT, collator));
            }
        }
    }
}
