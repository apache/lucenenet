using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Lucene.Net.Analysis.Ru;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Analyzers.Ru
{
    [TestFixture]
    public class TestRussianStem : LuceneTestCase
    {
        private List<string> words = new List<string>();
        private List<string> stems = new List<string>();

        /**
         * @see TestCase#setUp()
         */
        public override void SetUp()
        {
            base.SetUp();
            //System.out.println(new java.util.Date());
            String str;

            // open and read words into an array list
            StreamReader inWords = new StreamReader(@"ru\wordsUTF8.txt", Encoding.UTF8);
            while ((str = inWords.ReadLine()) != null)
            {
                words.Add(str);
            }
            inWords.Close();

            // open and read stems into an array list
            StreamReader inStems = new StreamReader(@"ru\stemsUTF8.txt", Encoding.UTF8);
            while ((str = inStems.ReadLine()) != null)
            {
                stems.Add(str);
            }
            inStems.Close();
        }

        [Test]
        public void TestStem()
        {
            for (int i = 0; i < words.Count; i++)
            {
                //if ( (i % 100) == 0 ) System.err.println(i);
                String realStem =
                    RussianStemmer.StemWord(words[i]);
                Assert.AreEqual(stems[i], realStem, "unicode");
            }
        }
    }
}