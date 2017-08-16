// LUCENENET TODO: Port issues - missing Normalizer2 dependency from icu.net

//using Lucene.Net.Analysis.Core;
//using Lucene.Net.Support;
//using NUnit.Framework;
//using System;

//namespace Lucene.Net.Analysis.ICU
//{
//    /// <summary>
//    /// Tests the ICUNormalizer2Filter
//    /// </summary>
//    public class TestICUNormalizer2Filter : BaseTokenStreamTestCase
//    {
//        private readonly Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
//        {
//            Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
//            return new TokenStreamComponents(tokenizer, new ICUNormalizer2Filter(tokenizer));
//        });

//        [Test]
//        public void TestDefaults()
//        {
//            // case folding
//            AssertAnalyzesTo(a, "This is a test", new String[] { "this", "is", "a", "test" });

//            // case folding
//            AssertAnalyzesTo(a, "Ruß", new String[] { "russ" });

//            // case folding
//            AssertAnalyzesTo(a, "ΜΆΪΟΣ", new String[] { "μάϊοσ" });
//            AssertAnalyzesTo(a, "Μάϊος", new String[] { "μάϊοσ" });

//            // supplementary case folding
//            AssertAnalyzesTo(a, "𐐖", new String[] { "𐐾" });

//            // normalization
//            AssertAnalyzesTo(a, "ﴳﴺﰧ", new String[] { "طمطمطم" });

//            // removal of default ignorables
//            AssertAnalyzesTo(a, "क्‍ष", new String[] { "क्ष" });
//        }

//        [Test]
//        public void TestAlternate()
//        {
//            //    Analyzer a = new Analyzer()
//            //{
//            //    @Override
//            //      public TokenStreamComponents createComponents(String fieldName, Reader reader)
//            //{
//            //    Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
//            //    return new TokenStreamComponents(tokenizer, new ICUNormalizer2Filter(
//            //        tokenizer,
//            //        /* specify nfc with decompose to get nfd */
//            //        Normalizer2.getInstance(null, "nfc", Normalizer2.Mode.DECOMPOSE)));
//            //}
//            //    };

//            Analyzer a = Analysis.Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
//            {
//                Tokenizer tokenizer = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
//                return new TokenStreamComponents(tokenizer, new ICUNormalizer2Filter(
//                    tokenizer,
//                    /* specify nfc with decompose to get nfd */
//                    //Normalizer2.getInstance(null, "nfc", Normalizer2.Mode.DECOMPOSE)));
//                    new Normalizer2(global::Icu.Normalizer.UNormalizationMode.UNORM_NFD))); // LUCENENET NOTE: "nfc" + "DECOMPOSE" = "UNORM_NFD"
//            });

//            // decompose EAcute into E + combining Acute
//            AssertAnalyzesTo(a, "\u00E9", new String[] { "\u0065\u0301" });
//        }

//        /** blast some random strings through the analyzer */
//        [Test]
//        public void TestRandomStrings()
//        {
//            CheckRandomData(Random(), a, 1000 * RANDOM_MULTIPLIER);
//        }

//        [Test]
//        public void TestEmptyTerm()
//        {
//            Analyzer a = Analyzer.NewAnonymous(createComponents: (fieldName, reader) =>
//            {
//                Tokenizer tokenizer = new KeywordTokenizer(reader);
//                return new TokenStreamComponents(tokenizer, new ICUNormalizer2Filter(tokenizer));
//            });
//            CheckOneTerm(a, "", "");
//        }
//    }
//}
