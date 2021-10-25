using J2N.Text;
using NUnit.Framework;
using System;
using System.Text.RegularExpressions;
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

    public class PhoneticEngineTest
    {
        private static readonly int TEN = 10;

        public static JCG.List<Object[]> Values = new JCG.List<object[]> { new Object[] { "Renault", "rinD|rinDlt|rina|rinalt|rino|rinolt|rinu|rinult", NameType.GENERIC, RuleType.APPROX, true, TEN },
                            new Object[] { "Renault", "rYnDlt|rYnalt|rYnult|rinDlt|rinalt|rinult", NameType.ASHKENAZI, RuleType.APPROX, true, TEN },
                            new Object[] { "Renault", "rYnDlt", NameType.ASHKENAZI, RuleType.APPROX, true, 1 },
                            new Object[] { "Renault", "rinDlt", NameType.SEPHARDIC, RuleType.APPROX, true, TEN },
                            new Object[] { "SntJohn-Smith", "sntjonsmit", NameType.GENERIC, RuleType.EXACT, true, TEN },
                            new Object[] { "d'ortley", "(ortlaj|ortlej)-(dortlaj|dortlej)", NameType.GENERIC, RuleType.EXACT, true, TEN },
                            new Object[] {
                                "van helsing",
                                "(elSink|elsink|helSink|helsink|helzink|xelsink)-(banhelsink|fanhelsink|fanhelzink|vanhelsink|vanhelzink|vanjelsink)",
                                NameType.GENERIC,
                                RuleType.EXACT,
                                false, TEN } };

        //    private readonly bool concat;
        //private readonly String name;
        //private readonly NameType nameType;
        //private readonly String phoneticExpected;
        //private readonly RuleType ruleType;
        //private readonly int maxPhonemes;

        //    public PhoneticEngineTest(String name, String phoneticExpected, NameType nameType,
        //                              RuleType ruleType, bool concat, int maxPhonemes)
        //    {
        //        this.name = name;
        //        this.phoneticExpected = phoneticExpected;
        //        this.nameType = nameType;
        //        this.ruleType = ruleType;
        //        this.concat = concat;
        //        this.maxPhonemes = maxPhonemes;
        //    }

        [Test]//@Test(timeout = 10000L)
        [TestCaseSource("Values")]
        public void TestEncode(String name, String phoneticExpected, NameType nameType,
                                      RuleType ruleType, bool concat, int maxPhonemes)
        {
            PhoneticEngine engine = new PhoneticEngine(nameType, ruleType, concat, maxPhonemes);

            String phoneticActual = engine.Encode(name);

            //System.err.println("expecting: " + this.phoneticExpected);
            //System.err.println("actual:    " + phoneticActual);
            Assert.AreEqual(phoneticExpected, phoneticActual, "phoneme incorrect");

            if (concat)
            {
                String[] split = new Regex("\\|").Split(phoneticActual).TrimEnd();
                Assert.True(split.Length <= maxPhonemes);
            }
            else
            {
                String[] words = phoneticActual.Split('-').TrimEnd();
                foreach (String word in words)
                {
                    String[] split = new Regex("\\|").Split(word).TrimEnd();
                    Assert.True(split.Length <= maxPhonemes);
                }
            }
        }
    }
}
