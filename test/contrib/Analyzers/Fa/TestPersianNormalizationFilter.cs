using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.AR;
using NUnit.Framework;

namespace Lucene.Net.Analyzers.Fa
{
    /**
     * Test the Arabic Normalization Filter
     * 
     */
    [TestFixture]
    public class TestPersianNormalizationFilter : BaseTokenStreamTestCase
    {
        [Test]
        public void TestFarsiYeh()
        {
            Check("های", "هاي");
        }

        [Test]
        public void TestYehBarree()
        {
            Check("هاے", "هاي");
        }

        [Test]
        public void TestKeheh()
        {
            Check("کشاندن", "كشاندن");
        }

        [Test]
        public void TestHehYeh()
        {
            Check("كتابۀ", "كتابه");
        }

        [Test]
        public void TestHehHamzaAbove()
        {
            Check("كتابهٔ", "كتابه");
        }

        [Test]
        public void TestHehGoal()
        {
            Check("زادہ", "زاده");
        }

        private void Check(String input, String expected)
        {
            ArabicLetterTokenizer tokenStream = new ArabicLetterTokenizer(
                new StringReader(input));
            PersianNormalizationFilter filter = new PersianNormalizationFilter(
                tokenStream);
            AssertTokenStreamContents(filter, new String[] { expected });
        }
    }
}
