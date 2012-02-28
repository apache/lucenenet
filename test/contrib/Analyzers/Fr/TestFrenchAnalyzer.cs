using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Fr;
using NUnit.Framework;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.Analyzers.Fr
{
    /**
     * Test case for FrenchAnalyzer.
     *
     * @version   $version$
     */
    [TestFixture]
    public class TestFrenchAnalyzer : BaseTokenStreamTestCase
    {
        [Test]
        public void TestAnalyzer()
        {
            FrenchAnalyzer fa = new FrenchAnalyzer(Version.LUCENE_CURRENT);

            AssertAnalyzesTo(fa, "", new String[0]);

            AssertAnalyzesTo(
                fa,
                "chien chat cheval",
                new String[] {"chien", "chat", "cheval"});

            AssertAnalyzesTo(
                fa,
                "chien CHAT CHEVAL",
                new String[] {"chien", "chat", "cheval"});

            AssertAnalyzesTo(
                fa,
                "  chien  ,? + = -  CHAT /: > CHEVAL",
                new String[] {"chien", "chat", "cheval"});

            AssertAnalyzesTo(fa, "chien++", new String[] {"chien"});

            AssertAnalyzesTo(
                fa,
                "mot \"entreguillemet\"",
                new String[] {"mot", "entreguillemet"});

            // let's do some french specific tests now	

            /* 1. couldn't resist
             I would expect this to stay one term as in French the minus 
            sign is often used for composing words */
            AssertAnalyzesTo(
                fa,
                "Jean-François",
                new String[] {"jean", "françois"});

            // 2. stopwords
            AssertAnalyzesTo(
                fa,
                "le la chien les aux chat du des à cheval",
                new String[] {"chien", "chat", "cheval"});

            // some nouns and adjectives
            AssertAnalyzesTo(
                fa,
                "lances chismes habitable chiste éléments captifs",
                new String[]
                    {
                        "lanc",
                        "chism",
                        "habit",
                        "chist",
                        "élément",
                        "captif"
                    });

            // some verbs
            AssertAnalyzesTo(
                fa,
                "finissions souffrirent rugissante",
                new String[] {"fin", "souffr", "rug"});

            // some everything else
            // aujourd'hui stays one term which is OK
            AssertAnalyzesTo(
                fa,
                "C3PO aujourd'hui oeuf ïâöûàä anticonstitutionnellement Java++ ",
                new String[]
                    {
                        "c3po",
                        "aujourd'hui",
                        "oeuf",
                        "ïâöûàä",
                        "anticonstitutionnel",
                        "jav"
                    });

            // some more everything else
            // here 1940-1945 stays as one term, 1940:1945 not ?
            AssertAnalyzesTo(
                fa,
                "33Bis 1940-1945 1940:1945 (---i+++)*",
                new String[] {"33bis", "1940-1945", "1940", "1945", "i"});

        }

        [Test]
        public void TestReusableTokenStream()
        {
            FrenchAnalyzer fa = new FrenchAnalyzer(Version.LUCENE_CURRENT);
            // stopwords
            AssertAnalyzesToReuse(
                fa,
                "le la chien les aux chat du des à cheval",
                new String[] {"chien", "chat", "cheval"});

            // some nouns and adjectives
            AssertAnalyzesToReuse(
                fa,
                "lances chismes habitable chiste éléments captifs",
                new String[]
                    {
                        "lanc",
                        "chism",
                        "habit",
                        "chist",
                        "élément",
                        "captif"
                    });
        }

        /* 
         * Test that changes to the exclusion table are applied immediately
         * when using reusable token streams.
         */
        [Test]
        public void TestExclusionTableReuse()
        {
            FrenchAnalyzer fa = new FrenchAnalyzer(Version.LUCENE_CURRENT);
            AssertAnalyzesToReuse(fa, "habitable", new String[] { "habit" });
            fa.SetStemExclusionTable(new String[] { "habitable" });
            AssertAnalyzesToReuse(fa, "habitable", new String[] { "habitable" });
        }
    }
}