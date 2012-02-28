using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Index;
using NUnit.Framework;
using Version=Lucene.Net.Util.Version;

namespace Lucene.Net.Analyzers.Fr
{
    /**
     * 
     */
    [TestFixture]
    public class TestElision : BaseTokenStreamTestCase
    {
        [Test]
        public void TestElision2()
        {
            String test = "Plop, juste pour voir l'embrouille avec O'brian. M'enfin.";
            Tokenizer tokenizer = new StandardTokenizer(Version.LUCENE_CURRENT, new StringReader(test));
            HashSet<String> articles = new HashSet<String>();
            articles.Add("l");
            articles.Add("M");
            TokenFilter filter = new ElisionFilter(tokenizer, articles);
            List<string> tas = Filtre(filter);
            Assert.AreEqual("embrouille", tas[4]);
            Assert.AreEqual("O'brian", tas[6]);
            Assert.AreEqual("enfin", tas[7]);
        }

        private List<string> Filtre(TokenFilter filter)
        {
            List<string> tas = new List<string>();
            TermAttribute termAtt = filter.GetAttribute<TermAttribute>();
            while (filter.IncrementToken())
            {
                tas.Add(termAtt.Term());
            }
            return tas;
        }
    }
}