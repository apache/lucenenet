using J2N.Text;
using Lucene.Net.Support;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using Assert = Lucene.Net.TestFramework.Assert;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Analysis.Phonetic.Language.Bm
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
    /// Tests <see cref="PhoneticEngine"/> and <see cref="LanguageSet"/> in ways very similar to code found in solr-3.6.0.
    /// <para/>
    /// since 1.7
    /// </summary>
    public class PhoneticEngineRegressionTest
    {
        [Test]
        public void TestSolrGENERIC()
        {
            IDictionary<String, String> args;

            // concat is true, ruleType is EXACT
            args = new JCG.SortedDictionary<String, String>();
            args["nameType"] = "GENERIC";
            Assert.AreEqual(Encode(args, true, "Angelo"), "agilo|angilo|aniilo|anilo|anxilo|anzilo|ogilo|ongilo|oniilo|onilo|onxilo|onzilo");
            args["ruleType"] = "EXACT";
            Assert.AreEqual(Encode(args, true, "Angelo"), "anZelo|andZelo|angelo|anhelo|anjelo|anxelo");
            Assert.AreEqual(Encode(args, true, "D'Angelo"), "(anZelo|andZelo|angelo|anhelo|anjelo|anxelo)-(danZelo|dandZelo|dangelo|danhelo|danjelo|danxelo)");
            args["languageSet"] = "italian,greek,spanish";
            Assert.AreEqual(Encode(args, true, "Angelo"), "andZelo|angelo|anxelo");
            Assert.AreEqual(Encode(args, true, "1234"), "");

            // concat is false, ruleType is EXACT
            args = new JCG.SortedDictionary<String, String>();
            Assert.AreEqual(Encode(args, false, "Angelo"), "agilo|angilo|aniilo|anilo|anxilo|anzilo|ogilo|ongilo|oniilo|onilo|onxilo|onzilo");
            args["ruleType"] = "EXACT";
            Assert.AreEqual(Encode(args, false, "Angelo"), "anZelo|andZelo|angelo|anhelo|anjelo|anxelo");
            Assert.AreEqual(Encode(args, false, "D'Angelo"), "(anZelo|andZelo|angelo|anhelo|anjelo|anxelo)-(danZelo|dandZelo|dangelo|danhelo|danjelo|danxelo)");
            args["languageSet"] = "italian,greek,spanish";
            Assert.AreEqual(Encode(args, false, "Angelo"), "andZelo|angelo|anxelo");
            Assert.AreEqual(Encode(args, false, "1234"), "");

            // concat is true, ruleType is APPROX
            args = new JCG.SortedDictionary<String, String>();
            Assert.AreEqual(Encode(args, true, "Angelo"), "agilo|angilo|aniilo|anilo|anxilo|anzilo|ogilo|ongilo|oniilo|onilo|onxilo|onzilo");
            args["ruleType"] = "APPROX";
            Assert.AreEqual(Encode(args, true, "Angelo"), "agilo|angilo|aniilo|anilo|anxilo|anzilo|ogilo|ongilo|oniilo|onilo|onxilo|onzilo");
            Assert.AreEqual(Encode(args, true, "D'Angelo"), "(agilo|angilo|aniilo|anilo|anxilo|anzilo|ogilo|ongilo|oniilo|onilo|onxilo|onzilo)-(dagilo|dangilo|daniilo|danilo|danxilo|danzilo|dogilo|dongilo|doniilo|donilo|donxilo|donzilo)");
            args["languageSet"] = "italian,greek,spanish";
            Assert.AreEqual(Encode(args, true, "Angelo"), "angilo|anxilo|anzilo|ongilo|onxilo|onzilo");
            Assert.AreEqual(Encode(args, true, "1234"), "");

            // concat is false, ruleType is APPROX
            args = new JCG.SortedDictionary<String, String>();
            Assert.AreEqual(Encode(args, false, "Angelo"), "agilo|angilo|aniilo|anilo|anxilo|anzilo|ogilo|ongilo|oniilo|onilo|onxilo|onzilo");
            args["ruleType"] = "APPROX";
            Assert.AreEqual(Encode(args, false, "Angelo"), "agilo|angilo|aniilo|anilo|anxilo|anzilo|ogilo|ongilo|oniilo|onilo|onxilo|onzilo");
            Assert.AreEqual(Encode(args, false, "D'Angelo"), "(agilo|angilo|aniilo|anilo|anxilo|anzilo|ogilo|ongilo|oniilo|onilo|onxilo|onzilo)-(dagilo|dangilo|daniilo|danilo|danxilo|danzilo|dogilo|dongilo|doniilo|donilo|donxilo|donzilo)");
            args["languageSet"] = "italian,greek,spanish";
            Assert.AreEqual(Encode(args, false, "Angelo"), "angilo|anxilo|anzilo|ongilo|onxilo|onzilo");
            Assert.AreEqual(Encode(args, false, "1234"), "");
        }

        [Test]
        public void TestSolrASHKENAZI()
        {
            IDictionary<String, String> args;

            // concat is true, ruleType is EXACT
            args = new JCG.SortedDictionary<String, String>();
            args["nameType"] = "ASHKENAZI";
            Assert.AreEqual(Encode(args, true, "Angelo"), "AnElO|AnSelO|AngElO|AngzelO|AnkselO|AnzelO");
            args["ruleType"] = "EXACT";
            Assert.AreEqual(Encode(args, true, "Angelo"), "andZelo|angelo|anhelo|anxelo");
            Assert.AreEqual(Encode(args, true, "D'Angelo"), "dandZelo|dangelo|danhelo|danxelo");
            args["languageSet"] = "italian,greek,spanish";
            Assert.AreEqual(Encode(args, true, "Angelo"), "angelo|anxelo");
            Assert.AreEqual(Encode(args, true, "1234"), "");

            // concat is false, ruleType is EXACT
            args = new JCG.SortedDictionary<String, String>();
            args["nameType"] = "ASHKENAZI";
            Assert.AreEqual(Encode(args, false, "Angelo"), "AnElO|AnSelO|AngElO|AngzelO|AnkselO|AnzelO");
            args["ruleType"] = "EXACT";
            Assert.AreEqual(Encode(args, false, "Angelo"), "andZelo|angelo|anhelo|anxelo");
            Assert.AreEqual(Encode(args, false, "D'Angelo"), "dandZelo|dangelo|danhelo|danxelo");
            args["languageSet"] = "italian,greek,spanish";
            Assert.AreEqual(Encode(args, false, "Angelo"), "angelo|anxelo");
            Assert.AreEqual(Encode(args, false, "1234"), "");

            // concat is true, ruleType is APPROX
            args = new JCG.SortedDictionary<String, String>();
            args["nameType"] = "ASHKENAZI";
            Assert.AreEqual(Encode(args, true, "Angelo"), "AnElO|AnSelO|AngElO|AngzelO|AnkselO|AnzelO");
            args["ruleType"] = "APPROX";
            Assert.AreEqual(Encode(args, true, "Angelo"), "AnElO|AnSelO|AngElO|AngzelO|AnkselO|AnzelO");
            Assert.AreEqual(Encode(args, true, "D'Angelo"), "dAnElO|dAnSelO|dAngElO|dAngzelO|dAnkselO|dAnzelO");
            args["languageSet"] = "italian,greek,spanish";
            Assert.AreEqual(Encode(args, true, "Angelo"), "AnSelO|AngElO|AngzelO|AnkselO");
            Assert.AreEqual(Encode(args, true, "1234"), "");

            // concat is false, ruleType is APPROX
            args = new JCG.SortedDictionary<String, String>();
            args["nameType"] = "ASHKENAZI";
            Assert.AreEqual(Encode(args, false, "Angelo"), "AnElO|AnSelO|AngElO|AngzelO|AnkselO|AnzelO");
            args["ruleType"] = "APPROX";
            Assert.AreEqual(Encode(args, false, "Angelo"), "AnElO|AnSelO|AngElO|AngzelO|AnkselO|AnzelO");
            Assert.AreEqual(Encode(args, false, "D'Angelo"), "dAnElO|dAnSelO|dAngElO|dAngzelO|dAnkselO|dAnzelO");
            args["languageSet"] = "italian,greek,spanish";
            Assert.AreEqual(Encode(args, false, "Angelo"), "AnSelO|AngElO|AngzelO|AnkselO");
            Assert.AreEqual(Encode(args, false, "1234"), "");
        }

        [Test]
        public void TestSolrSEPHARDIC()
        {
            IDictionary<String, String> args;

            // concat is true, ruleType is EXACT
            args = new JCG.SortedDictionary<String, String>();
            args["nameType"] = "SEPHARDIC";
            Assert.AreEqual(Encode(args, true, "Angelo"), "anhila|anhilu|anzila|anzilu|nhila|nhilu|nzila|nzilu");
            args["ruleType"] = "EXACT";
            Assert.AreEqual(Encode(args, true, "Angelo"), "anZelo|andZelo|anxelo");
            Assert.AreEqual(Encode(args, true, "D'Angelo"), "anZelo|andZelo|anxelo");
            args["languageSet"] = "italian,greek,spanish";
            Assert.AreEqual(Encode(args, true, "Angelo"), "andZelo|anxelo");
            Assert.AreEqual(Encode(args, true, "1234"), "");

            // concat is false, ruleType is EXACT
            args = new JCG.SortedDictionary<String, String>();
            args["nameType"] = "SEPHARDIC";
            Assert.AreEqual(Encode(args, false, "Angelo"), "anhila|anhilu|anzila|anzilu|nhila|nhilu|nzila|nzilu");
            args["ruleType"] = "EXACT";
            Assert.AreEqual(Encode(args, false, "Angelo"), "anZelo|andZelo|anxelo");
            Assert.AreEqual(Encode(args, false, "D'Angelo"), "danZelo|dandZelo|danxelo");
            args["languageSet"] = "italian,greek,spanish";
            Assert.AreEqual(Encode(args, false, "Angelo"), "andZelo|anxelo");
            Assert.AreEqual(Encode(args, false, "1234"), "");

            // concat is true, ruleType is APPROX
            args = new JCG.SortedDictionary<String, String>();
            args["nameType"] = "SEPHARDIC";
            Assert.AreEqual(Encode(args, true, "Angelo"), "anhila|anhilu|anzila|anzilu|nhila|nhilu|nzila|nzilu");
            args["ruleType"] = "APPROX";
            Assert.AreEqual(Encode(args, true, "Angelo"), "anhila|anhilu|anzila|anzilu|nhila|nhilu|nzila|nzilu");
            Assert.AreEqual(Encode(args, true, "D'Angelo"), "anhila|anhilu|anzila|anzilu|nhila|nhilu|nzila|nzilu");
            args["languageSet"] = "italian,greek,spanish";
            Assert.AreEqual(Encode(args, true, "Angelo"), "anhila|anhilu|anzila|anzilu|nhila|nhilu|nzila|nzilu");
            Assert.AreEqual(Encode(args, true, "1234"), "");

            // concat is false, ruleType is APPROX
            args = new JCG.SortedDictionary<String, String>();
            args["nameType"] = "SEPHARDIC";
            Assert.AreEqual(Encode(args, false, "Angelo"), "anhila|anhilu|anzila|anzilu|nhila|nhilu|nzila|nzilu");
            args["ruleType"] = "APPROX";
            Assert.AreEqual(Encode(args, false, "Angelo"), "anhila|anhilu|anzila|anzilu|nhila|nhilu|nzila|nzilu");
            Assert.AreEqual(Encode(args, false, "D'Angelo"), "danhila|danhilu|danzila|danzilu|nhila|nhilu|nzila|nzilu");
            args["languageSet"] = "italian,greek,spanish";
            Assert.AreEqual(Encode(args, false, "Angelo"), "anhila|anhilu|anzila|anzilu|nhila|nhilu|nzila|nzilu");
            Assert.AreEqual(Encode(args, false, "1234"), "");
        }

        /**
         * This code is similar in style to code found in Solr:
         * solr/core/src/java/org/apache/solr/analysis/BeiderMorseFilterFactory.java
         *
         * Making a JUnit test out of it to protect Solr from possible future
         * regressions in Commons-Codec.
         */
        private static string Encode(IDictionary<string, string> args, bool concat, string input)
        {
            LanguageSet languageSet;
            PhoneticEngine engine;

            // PhoneticEngine = NameType + RuleType + concat
            // we use common-codec's defaults: GENERIC + APPROX + true
            args.TryGetValue("nameType", out string nameTypeArg);
            NameType nameType = (nameTypeArg is null) ? NameType.GENERIC : (NameType)Enum.Parse(typeof(NameType), nameTypeArg, true);

            args.TryGetValue("ruleType", out string ruleTypeArg);
            RuleType ruleType = (ruleTypeArg is null) ? RuleType.APPROX : (RuleType)Enum.Parse(typeof(RuleType), ruleTypeArg, true);

            engine = new PhoneticEngine(nameType, ruleType, concat);

            // LanguageSet: defaults to automagic, otherwise a comma-separated list.
            args.TryGetValue("languageSet", out string languageSetArg);
            if (languageSetArg is null || languageSetArg.Equals("auto", StringComparison.Ordinal))
            {
                languageSet = null;
            }
            else
            {
                languageSet = LanguageSet.From(new JCG.HashSet<string>(languageSetArg.Split(',').TrimEnd()));
            }

            /*
                org/apache/lucene/analysis/phonetic/BeiderMorseFilter.java (lines 96-98) does this:

                encoded = (languages is null)
                    ? engine.encode(termAtt.toString())
                    : engine.encode(termAtt.toString(), languages);

                Hence our approach, below:
            */
            if (languageSet is null)
            {
                return engine.Encode(input);
            }
            else
            {
                return engine.Encode(input, languageSet);
            }
        }
    }
}
