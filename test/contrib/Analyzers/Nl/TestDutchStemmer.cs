/*
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Nl;
using Lucene.Net.Test.Analysis;
using NUnit.Framework;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.Analyzers.Nl
{
    /*
     * Test the Dutch Stem Filter, which only modifies the term text.
     * 
     * The code states that it uses the snowball algorithm, but tests reveal some differences.
     * 
     */
    [TestFixture]
    public class TestDutchStemmer : BaseTokenStreamTestCase
    {
        FileInfo customDictFile = new FileInfo(@"nl\customStemDict.txt");

        [Test]
        public void TestWithSnowballExamples()
        {
            Check("lichaamsziek", "lichaamsziek");
            Check("lichamelijk", "licham");
            Check("lichamelijke", "licham");
            Check("lichamelijkheden", "licham");
            Check("lichamen", "licham");
            Check("lichere", "licher");
            Check("licht", "licht");
            Check("lichtbeeld", "lichtbeeld");
            Check("lichtbruin", "lichtbruin");
            Check("lichtdoorlatende", "lichtdoorlat");
            Check("lichte", "licht");
            Check("lichten", "licht");
            Check("lichtende", "lichtend");
            Check("lichtenvoorde", "lichtenvoord");
            Check("lichter", "lichter");
            Check("lichtere", "lichter");
            Check("lichters", "lichter");
            Check("lichtgevoeligheid", "lichtgevoel");
            Check("lichtgewicht", "lichtgewicht");
            Check("lichtgrijs", "lichtgrijs");
            Check("lichthoeveelheid", "lichthoevel");
            Check("lichtintensiteit", "lichtintensiteit");
            Check("lichtje", "lichtj");
            Check("lichtjes", "lichtjes");
            Check("lichtkranten", "lichtkrant");
            Check("lichtkring", "lichtkring");
            Check("lichtkringen", "lichtkring");
            Check("lichtregelsystemen", "lichtregelsystem");
            Check("lichtste", "lichtst");
            Check("lichtstromende", "lichtstrom");
            Check("lichtte", "licht");
            Check("lichtten", "licht");
            Check("lichttoetreding", "lichttoetred");
            Check("lichtverontreinigde", "lichtverontreinigd");
            Check("lichtzinnige", "lichtzinn");
            Check("lid", "lid");
            Check("lidia", "lidia");
            Check("lidmaatschap", "lidmaatschap");
            Check("lidstaten", "lidstat");
            Check("lidvereniging", "lidveren");
            Check("opgingen", "opging");
            Check("opglanzing", "opglanz");
            Check("opglanzingen", "opglanz");
            Check("opglimlachten", "opglimlacht");
            Check("opglimpen", "opglimp");
            Check("opglimpende", "opglimp");
            Check("opglimping", "opglimp");
            Check("opglimpingen", "opglimp");
            Check("opgraven", "opgrav");
            Check("opgrijnzen", "opgrijnz");
            Check("opgrijzende", "opgrijz");
            Check("opgroeien", "opgroei");
            Check("opgroeiende", "opgroei");
            Check("opgroeiplaats", "opgroeiplat");
            Check("ophaal", "ophal");
            Check("ophaaldienst", "ophaaldienst");
            Check("ophaalkosten", "ophaalkost");
            Check("ophaalsystemen", "ophaalsystem");
            Check("ophaalt", "ophaalt");
            Check("ophaaltruck", "ophaaltruck");
            Check("ophalen", "ophal");
            Check("ophalend", "ophal");
            Check("ophalers", "ophaler");
            Check("ophef", "ophef");
            Check("opheffen", "ophef"); // versus snowball 'opheff'
            Check("opheffende", "ophef"); // versus snowball 'opheff'
            Check("opheffing", "ophef"); // versus snowball 'opheff'
            Check("opheldering", "ophelder");
            Check("ophemelde", "ophemeld");
            Check("ophemelen", "ophemel");
            Check("opheusden", "opheusd");
            Check("ophief", "ophief");
            Check("ophield", "ophield");
            Check("ophieven", "ophiev");
            Check("ophoepelt", "ophoepelt");
            Check("ophoog", "ophog");
            Check("ophoogzand", "ophoogzand");
            Check("ophopen", "ophop");
            Check("ophoping", "ophop");
            Check("ophouden", "ophoud");
        }

        [Test]
        public void TestReusableTokenStream()
        {
            Analyzer a = new DutchAnalyzer(Version.LUCENE_CURRENT);
            CheckOneTermReuse(a, "lichaamsziek", "lichaamsziek");
            CheckOneTermReuse(a, "lichamelijk", "licham");
            CheckOneTermReuse(a, "lichamelijke", "licham");
            CheckOneTermReuse(a, "lichamelijkheden", "licham");
        }

        /*
         * subclass that acts just like whitespace analyzer for testing
         */
        private class DutchSubclassAnalyzer : DutchAnalyzer
        {
            public DutchSubclassAnalyzer(Version matchVersion)
                : base(matchVersion)
            {

            }
            public override TokenStream TokenStream(String fieldName, TextReader reader)
            {
                return new WhitespaceTokenizer(reader);
            }
        }

        [Test]
        public void TestLucene1678BwComp()
        {
            Analyzer a = new DutchSubclassAnalyzer(Version.LUCENE_CURRENT);
            CheckOneTermReuse(a, "lichaamsziek", "lichaamsziek");
            CheckOneTermReuse(a, "lichamelijk", "lichamelijk");
            CheckOneTermReuse(a, "lichamelijke", "lichamelijke");
            CheckOneTermReuse(a, "lichamelijkheden", "lichamelijkheden");
        }

        /* 
         * Test that changes to the exclusion table are applied immediately
         * when using reusable token streams.
         */
        [Test]
        public void TestExclusionTableReuse()
        {
            DutchAnalyzer a = new DutchAnalyzer(Version.LUCENE_CURRENT);
            CheckOneTermReuse(a, "lichamelijk", "licham");
            a.SetStemExclusionTable(new String[] { "lichamelijk" });
            CheckOneTermReuse(a, "lichamelijk", "lichamelijk");
        }

        /* 
         * Test that changes to the dictionary stemming table are applied immediately
         * when using reusable token streams.
         */
        [Test]
        public void TestStemDictionaryReuse()
        {
            DutchAnalyzer a = new DutchAnalyzer(Version.LUCENE_CURRENT);
            CheckOneTermReuse(a, "lichamelijk", "licham");
            a.SetStemDictionary(customDictFile);
            CheckOneTermReuse(a, "lichamelijk", "somethingentirelydifferent");
        }

        private void Check(String input, String expected)
        {
            CheckOneTerm(new DutchAnalyzer(Version.LUCENE_CURRENT), input, expected);
        }

    }
}
