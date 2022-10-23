// Lucene version compatibility level 4.8.1
using System;
using NUnit.Framework;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Util;

namespace Lucene.Net.Analysis.Nl
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
    /// Test the Dutch Stem Filter, which only modifies the term text.
    /// 
    /// The code states that it uses the snowball algorithm, but tests reveal some differences.
    /// 
    /// </summary>
    public class TestDutchStemmer : BaseTokenStreamTestCase
    {

        [Test]
        public virtual void TestWithSnowballExamples()
        {
            check("lichaamsziek", "lichaamsziek");
            check("lichamelijk", "licham");
            check("lichamelijke", "licham");
            check("lichamelijkheden", "licham");
            check("lichamen", "licham");
            check("lichere", "licher");
            check("licht", "licht");
            check("lichtbeeld", "lichtbeeld");
            check("lichtbruin", "lichtbruin");
            check("lichtdoorlatende", "lichtdoorlat");
            check("lichte", "licht");
            check("lichten", "licht");
            check("lichtende", "lichtend");
            check("lichtenvoorde", "lichtenvoord");
            check("lichter", "lichter");
            check("lichtere", "lichter");
            check("lichters", "lichter");
            check("lichtgevoeligheid", "lichtgevoel");
            check("lichtgewicht", "lichtgewicht");
            check("lichtgrijs", "lichtgrijs");
            check("lichthoeveelheid", "lichthoevel");
            check("lichtintensiteit", "lichtintensiteit");
            check("lichtje", "lichtj");
            check("lichtjes", "lichtjes");
            check("lichtkranten", "lichtkrant");
            check("lichtkring", "lichtkring");
            check("lichtkringen", "lichtkring");
            check("lichtregelsystemen", "lichtregelsystem");
            check("lichtste", "lichtst");
            check("lichtstromende", "lichtstrom");
            check("lichtte", "licht");
            check("lichtten", "licht");
            check("lichttoetreding", "lichttoetred");
            check("lichtverontreinigde", "lichtverontreinigd");
            check("lichtzinnige", "lichtzinn");
            check("lid", "lid");
            check("lidia", "lidia");
            check("lidmaatschap", "lidmaatschap");
            check("lidstaten", "lidstat");
            check("lidvereniging", "lidveren");
            check("opgingen", "opging");
            check("opglanzing", "opglanz");
            check("opglanzingen", "opglanz");
            check("opglimlachten", "opglimlacht");
            check("opglimpen", "opglimp");
            check("opglimpende", "opglimp");
            check("opglimping", "opglimp");
            check("opglimpingen", "opglimp");
            check("opgraven", "opgrav");
            check("opgrijnzen", "opgrijnz");
            check("opgrijzende", "opgrijz");
            check("opgroeien", "opgroei");
            check("opgroeiende", "opgroei");
            check("opgroeiplaats", "opgroeiplat");
            check("ophaal", "ophal");
            check("ophaaldienst", "ophaaldienst");
            check("ophaalkosten", "ophaalkost");
            check("ophaalsystemen", "ophaalsystem");
            check("ophaalt", "ophaalt");
            check("ophaaltruck", "ophaaltruck");
            check("ophalen", "ophal");
            check("ophalend", "ophal");
            check("ophalers", "ophaler");
            check("ophef", "ophef");
            check("opheldering", "ophelder");
            check("ophemelde", "ophemeld");
            check("ophemelen", "ophemel");
            check("opheusden", "opheusd");
            check("ophief", "ophief");
            check("ophield", "ophield");
            check("ophieven", "ophiev");
            check("ophoepelt", "ophoepelt");
            check("ophoog", "ophog");
            check("ophoogzand", "ophoogzand");
            check("ophopen", "ophop");
            check("ophoping", "ophop");
            check("ophouden", "ophoud");
        }

        /// @deprecated (3.1) remove this test in Lucene 5.0 
        [Test]
        [Obsolete("(3.1) remove this test in Lucene 5.0")]
        public virtual void TestOldBuggyStemmer()
        {
            Analyzer a = new DutchAnalyzer(LuceneVersion.LUCENE_30);
            CheckOneTerm(a, "opheffen", "ophef"); // versus snowball 'opheff'
            CheckOneTerm(a, "opheffende", "ophef"); // versus snowball 'opheff'
            CheckOneTerm(a, "opheffing", "ophef"); // versus snowball 'opheff'
        }

        [Test]
        public virtual void TestSnowballCorrectness()
        {
            Analyzer a = new DutchAnalyzer(TEST_VERSION_CURRENT);
            CheckOneTerm(a, "opheffen", "opheff");
            CheckOneTerm(a, "opheffende", "opheff");
            CheckOneTerm(a, "opheffing", "opheff");
        }

        [Test]
        public virtual void TestReusableTokenStream()
        {
            Analyzer a = new DutchAnalyzer(TEST_VERSION_CURRENT);
            CheckOneTerm(a, "lichaamsziek", "lichaamsziek");
            CheckOneTerm(a, "lichamelijk", "licham");
            CheckOneTerm(a, "lichamelijke", "licham");
            CheckOneTerm(a, "lichamelijkheden", "licham");
        }

        [Test]
        public virtual void TestExclusionTableViaCtor()
        {
#pragma warning disable 612, 618
            CharArraySet set = new CharArraySet(LuceneVersion.LUCENE_30, 1, true);
#pragma warning restore 612, 618
            set.add("lichamelijk");
            DutchAnalyzer a = new DutchAnalyzer(TEST_VERSION_CURRENT, CharArraySet.Empty, set);
            AssertAnalyzesTo(a, "lichamelijk lichamelijke", new string[] { "lichamelijk", "licham" });

            a = new DutchAnalyzer(TEST_VERSION_CURRENT, CharArraySet.Empty, set);
            AssertAnalyzesTo(a, "lichamelijk lichamelijke", new string[] { "lichamelijk", "licham" });

        }

        /// <summary>
        /// check that the default stem overrides are used
        /// even if you use a non-default ctor.
        /// </summary>
        [Test]
        public virtual void TestStemOverrides()
        {
            DutchAnalyzer a = new DutchAnalyzer(TEST_VERSION_CURRENT, CharArraySet.Empty);
            CheckOneTerm(a, "fiets", "fiets");
        }
        /// <summary>
        /// 3.0 still uses the chararraymap internally check if that works as well </summary>
        /// @deprecated (4.3) remove this test in Lucene 5.0 
        [Test]
        [Obsolete("(4.3) remove this test in Lucene 5.0")]
        public virtual void Test30StemOverrides()
        {
            DutchAnalyzer a = new DutchAnalyzer(LuceneVersion.LUCENE_30);
            CheckOneTerm(a, "fiets", "fiets");
            a = new DutchAnalyzer(LuceneVersion.LUCENE_30, CharArraySet.Empty);
            CheckOneTerm(a, "fiets", "fiet"); // only the default ctor populates the dict
        }

        [Test]
        public virtual void TestEmptyStemDictionary()
        {
            DutchAnalyzer a = new DutchAnalyzer(TEST_VERSION_CURRENT, CharArraySet.Empty, CharArraySet.Empty, CharArrayDictionary<string>.Empty);
            CheckOneTerm(a, "fiets", "fiet");
        }

        /// <summary>
        /// prior to 3.6, this confusingly did not happen if 
        /// you specified your own stoplist!!!! </summary>
        /// @deprecated (3.6) Remove this test in Lucene 5.0 
        [Test]
        [Obsolete("(3.6) Remove this test in Lucene 5.0")]
        public virtual void TestBuggyStemOverrides()
        {
            DutchAnalyzer a = new DutchAnalyzer(LuceneVersion.LUCENE_35, CharArraySet.Empty);
            CheckOneTerm(a, "fiets", "fiet");
        }

        /// <summary>
        /// Prior to 3.1, this analyzer had no lowercase filter.
        /// stopwords were case sensitive. Preserve this for back compat. </summary>
        /// @deprecated (3.1) Remove this test in Lucene 5.0 
        [Test]
        [Obsolete("(3.1) Remove this test in Lucene 5.0")]
        public virtual void TestBuggyStopwordsCasing()
        {
            DutchAnalyzer a = new DutchAnalyzer(LuceneVersion.LUCENE_30);
            AssertAnalyzesTo(a, "Zelf", new string[] { "zelf" });
        }

        /// <summary>
        /// Test that stopwords are not case sensitive
        /// </summary>
        [Test]
        public virtual void TestStopwordsCasing()
        {
#pragma warning disable 612, 618
            DutchAnalyzer a = new DutchAnalyzer(LuceneVersion.LUCENE_31);
#pragma warning restore 612, 618
            AssertAnalyzesTo(a, "Zelf", new string[] { });
        }

        private void check(string input, string expected)
        {
            CheckOneTerm(new DutchAnalyzer(TEST_VERSION_CURRENT), input, expected);
        }

        /// <summary>
        /// blast some random strings through the analyzer </summary>
        public virtual void TestRandomStrings()
        {
            CheckRandomData(Random, new DutchAnalyzer(TEST_VERSION_CURRENT), 1000 * RandomMultiplier);
        }
    }
}