// Lucene version compatibility level 4.8.1
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;
using NUnit.Framework;
using System;

namespace Lucene.Net.Analysis.Fr
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

    /// <summary>
    /// Test case for FrenchAnalyzer.
    /// 
    /// </summary>

    public class TestFrenchAnalyzer : BaseTokenStreamTestCase
    {

        [Test]
        public virtual void TestAnalyzer()
        {
            FrenchAnalyzer fa = new FrenchAnalyzer(TEST_VERSION_CURRENT);

            AssertAnalyzesTo(fa, "", new string[] { });

            AssertAnalyzesTo(fa, "chien chat cheval", new string[] { "chien", "chat", "cheval" });

            AssertAnalyzesTo(fa, "chien CHAT CHEVAL", new string[] { "chien", "chat", "cheval" });

            AssertAnalyzesTo(fa, "  chien  ,? + = -  CHAT /: > CHEVAL", new string[] { "chien", "chat", "cheval" });

            AssertAnalyzesTo(fa, "chien++", new string[] { "chien" });

            AssertAnalyzesTo(fa, "mot \"entreguillemet\"", new string[] { "mot", "entreguilemet" });

            // let's do some french specific tests now

            /* 1. couldn't resist
             I would expect this to stay one term as in French the minus
            sign is often used for composing words */
            AssertAnalyzesTo(fa, "Jean-François", new string[] { "jean", "francoi" });

            // 2. stopwords
            AssertAnalyzesTo(fa, "le la chien les aux chat du des à cheval", new string[] { "chien", "chat", "cheval" });

            // some nouns and adjectives
            AssertAnalyzesTo(fa, "lances chismes habitable chiste éléments captifs", new string[] { "lanc", "chism", "habitabl", "chist", "element", "captif" });

            // some verbs
            AssertAnalyzesTo(fa, "finissions souffrirent rugissante", new string[] { "finision", "soufrirent", "rugisant" });

            // some everything else
            // aujourd'hui stays one term which is OK
            AssertAnalyzesTo(fa, "C3PO aujourd'hui oeuf ïâöûàä anticonstitutionnellement Java++ ", new string[] { "c3po", "aujourd'hui", "oeuf", "ïaöuaä", "anticonstitutionel", "java" });

            // some more everything else
            // here 1940-1945 stays as one term, 1940:1945 not ?
            AssertAnalyzesTo(fa, "33Bis 1940-1945 1940:1945 (---i+++)*", new string[] { "33bi", "1940", "1945", "1940", "1945", "i" });

        }

        /// @deprecated (3.1) remove this test for Lucene 5.0 
        [Test]
        [Obsolete("(3.1) remove this test for Lucene 5.0")]
        public virtual void TestAnalyzer30()
        {
            FrenchAnalyzer fa = new FrenchAnalyzer(LuceneVersion.LUCENE_30);

            AssertAnalyzesTo(fa, "", new string[] { });

            AssertAnalyzesTo(fa, "chien chat cheval", new string[] { "chien", "chat", "cheval" });

            AssertAnalyzesTo(fa, "chien CHAT CHEVAL", new string[] { "chien", "chat", "cheval" });

            AssertAnalyzesTo(fa, "  chien  ,? + = -  CHAT /: > CHEVAL", new string[] { "chien", "chat", "cheval" });

            AssertAnalyzesTo(fa, "chien++", new string[] { "chien" });

            AssertAnalyzesTo(fa, "mot \"entreguillemet\"", new string[] { "mot", "entreguillemet" });

            // let's do some french specific tests now

            /* 1. couldn't resist
             I would expect this to stay one term as in French the minus
            sign is often used for composing words */
            AssertAnalyzesTo(fa, "Jean-François", new string[] { "jean", "françois" });

            // 2. stopwords
            AssertAnalyzesTo(fa, "le la chien les aux chat du des à cheval", new string[] { "chien", "chat", "cheval" });

            // some nouns and adjectives
            AssertAnalyzesTo(fa, "lances chismes habitable chiste éléments captifs", new string[] { "lanc", "chism", "habit", "chist", "élément", "captif" });

            // some verbs
            AssertAnalyzesTo(fa, "finissions souffrirent rugissante", new string[] { "fin", "souffr", "rug" });

            // some everything else
            // aujourd'hui stays one term which is OK
            AssertAnalyzesTo(fa, "C3PO aujourd'hui oeuf ïâöûàä anticonstitutionnellement Java++ ", new string[] { "c3po", "aujourd'hui", "oeuf", "ïâöûàä", "anticonstitutionnel", "jav" });

            // some more everything else
            // here 1940-1945 stays as one term, 1940:1945 not ?
            AssertAnalyzesTo(fa, "33Bis 1940-1945 1940:1945 (---i+++)*", new string[] { "33bis", "1940-1945", "1940", "1945", "i" });

        }

        [Test]
        public virtual void TestReusableTokenStream()
        {
            FrenchAnalyzer fa = new FrenchAnalyzer(TEST_VERSION_CURRENT);
            // stopwords
            AssertAnalyzesTo(fa, "le la chien les aux chat du des à cheval", new string[] { "chien", "chat", "cheval" });

            // some nouns and adjectives
            AssertAnalyzesTo(fa, "lances chismes habitable chiste éléments captifs", new string[] { "lanc", "chism", "habitabl", "chist", "element", "captif" });
        }

        [Test]
        public virtual void TestExclusionTableViaCtor()
        {
            CharArraySet set = new CharArraySet(TEST_VERSION_CURRENT, 1, true);
            set.add("habitable");
            FrenchAnalyzer fa = new FrenchAnalyzer(TEST_VERSION_CURRENT, CharArraySet.Empty, set);
            AssertAnalyzesTo(fa, "habitable chiste", new string[] { "habitable", "chist" });

            fa = new FrenchAnalyzer(TEST_VERSION_CURRENT, CharArraySet.Empty, set);
            AssertAnalyzesTo(fa, "habitable chiste", new string[] { "habitable", "chist" });
        }

        [Test]
        public virtual void TestElision()
        {
            FrenchAnalyzer fa = new FrenchAnalyzer(TEST_VERSION_CURRENT);
            AssertAnalyzesTo(fa, "voir l'embrouille", new string[] { "voir", "embrouil" });
        }

        /// <summary>
        /// Prior to 3.1, this analyzer had no lowercase filter.
        /// stopwords were case sensitive. Preserve this for back compat. </summary>
        /// @deprecated (3.1) Remove this test in Lucene 5.0 
        [Test]
        [Obsolete("(3.1) Remove this test in Lucene 5.0")]
        public virtual void TestBuggyStopwordsCasing()
        {
            FrenchAnalyzer a = new FrenchAnalyzer(LuceneVersion.LUCENE_30);
            AssertAnalyzesTo(a, "Votre", new string[] { "votr" });
        }

        /// <summary>
        /// Test that stopwords are not case sensitive
        /// </summary>
        [Test]
        public virtual void TestStopwordsCasing()
        {
#pragma warning disable 612, 618
            FrenchAnalyzer a = new FrenchAnalyzer(LuceneVersion.LUCENE_31);
#pragma warning restore 612, 618
            AssertAnalyzesTo(a, "Votre", new string[] { });
        }

        /// <summary>
        /// blast some random strings through the analyzer </summary>
        [Test]
        public virtual void TestRandomStrings()
        {
            CheckRandomData(Random, new FrenchAnalyzer(TEST_VERSION_CURRENT), 1000 * RandomMultiplier);
        }

        /// <summary>
        /// test accent-insensitive </summary>
        [Test]
        public virtual void TestAccentInsensitive()
        {
            Analyzer a = new FrenchAnalyzer(TEST_VERSION_CURRENT);
            CheckOneTerm(a, "sécuritaires", "securitair");
            CheckOneTerm(a, "securitaires", "securitair");
        }
    }
}