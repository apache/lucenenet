// LUCENENET TODO: Port issues - missing Normalizer2 dependency from icu.net

//using NUnit.Framework;
//using System;
//using System.Collections.Generic;
//using System.IO;

//namespace Lucene.Net.Analysis.ICU
//{
//    /// <summary>
//    /// basic tests for <see cref="ICUNormalizer2FilterFactory"/>
//    /// </summary>
//    public class TestICUNormalizer2FilterFactory : BaseTokenStreamTestCase
//    {
//        /** Test nfkc_cf defaults */
//        [Test]
//        public void TestDefaults()
//        {
//            TextReader reader = new StringReader("This is a Ｔｅｓｔ");
//            ICUNormalizer2FilterFactory factory = new ICUNormalizer2FilterFactory(new Dictionary<String, String>());
//            TokenStream stream = new MockTokenizer(reader, MockTokenizer.WHITESPACE, false);
//            stream = factory.Create(stream);
//            AssertTokenStreamContents(stream, new String[] { "this", "is", "a", "test" });
//        }

//        /** Test that bogus arguments result in exception */
//        [Test]
//        public void TestBogusArguments()
//        {
//            try
//            {
//                new ICUNormalizer2FilterFactory(new Dictionary<String, String>() {
//                    { "bogusArg", "bogusValue" }
//                });
//                fail();
//            }
//            catch (ArgumentException expected)
//            {
//                assertTrue(expected.Message.Contains("Unknown parameters"));
//            }
//        }

//        // TODO: add tests for different forms
//    }
//}
