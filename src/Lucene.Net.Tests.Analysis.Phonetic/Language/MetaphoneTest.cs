using NUnit.Framework;
using System;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Analysis.Phonetic.Language
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

    public class MetaphoneTest : StringEncoderAbstractTest<Metaphone>
    {
        public void AssertIsMetaphoneEqual(string source, string[] matches)
        {
            // match source to all matches
            foreach (string matche in matches)
            {
                Assert.True(this.StringEncoder.IsMetaphoneEqual(source, matche),
                    "Source: " + source + ", should have same Metaphone as: " + matche);
            }
            // match to each other
            foreach (string matche in matches)
            {
                foreach (string matche2 in matches)
                {
                    Assert.True(this.StringEncoder.IsMetaphoneEqual(matche, matche2));
                }
            }
        }

        public void AssertMetaphoneEqual(String[][] pairs)
        {
            this.ValidateFixture(pairs);
            foreach (String[] pair in pairs)
            {
                String name0 = pair[0];
                String name1 = pair[1];
                String failMsg = "Expected match between " + name0 + " and " + name1;
                Assert.True(this.StringEncoder.IsMetaphoneEqual(name0, name1), failMsg);
                Assert.True(this.StringEncoder.IsMetaphoneEqual(name1, name0), failMsg);
            }
        }

        
    protected override Metaphone CreateStringEncoder()
        {
            return new Metaphone();
        }

        [Test]
    public void TestIsMetaphoneEqual1()
        {
            this.AssertMetaphoneEqual(new String[][] { new string[] {
                "Case", "case" }, new string[] {
                "CASE", "Case" }, new string[] {
                "caSe", "cAsE" }, new string[] {
                "quick", "cookie" }
        });
        }

        /**
         * Matches computed from http://www.lanw.com/java/phonetic/default.htm
         */
        [Test]
        public void TestIsMetaphoneEqual2()
        {
            this.AssertMetaphoneEqual(new String[][] { new string[] { "Lawrence", "Lorenza" }, new string[] {
                "Gary", "Cahra" }, });
        }

        /**
         * Initial AE case.
         *
         * Match data computed from http://www.lanw.com/java/phonetic/default.htm
         */
        [Test]
        public void TestIsMetaphoneEqualAero()
        {
            this.AssertIsMetaphoneEqual("Aero", new String[] { "Eure" });
        }

        /**
         * Initial WH case.
         *
         * Match data computed from http://www.lanw.com/java/phonetic/default.htm
         */
        [Test]
        public void TestIsMetaphoneEqualWhite()
        {
            this.AssertIsMetaphoneEqual(
                "White",
                new String[] { "Wade", "Wait", "Waite", "Wat", "Whit", "Wiatt", "Wit", "Wittie", "Witty", "Wood", "Woodie", "Woody" });
        }

        /**
         * Initial A, not followed by an E case.
         *
         * Match data computed from http://www.lanw.com/java/phonetic/default.htm
         */
        [Test]
        public void TestIsMetaphoneEqualAlbert()
        {
            this.AssertIsMetaphoneEqual("Albert", new String[] { "Ailbert", "Alberik", "Albert", "Alberto", "Albrecht" });
        }

        /**
         * Match data computed from http://www.lanw.com/java/phonetic/default.htm
         */
        [Test]
        public void TestIsMetaphoneEqualGary()
        {
            this.AssertIsMetaphoneEqual(
                "Gary",
                new String[] {
                "Cahra",
                "Cara",
                "Carey",
                "Cari",
                "Caria",
                "Carie",
                "Caro",
                "Carree",
                "Carri",
                "Carrie",
                "Carry",
                "Cary",
                "Cora",
                "Corey",
                "Cori",
                "Corie",
                "Correy",
                "Corri",
                "Corrie",
                "Corry",
                "Cory",
                "Gray",
                "Kara",
                "Kare",
                "Karee",
                "Kari",
                "Karia",
                "Karie",
                "Karrah",
                "Karrie",
                "Karry",
                "Kary",
                "Keri",
                "Kerri",
                "Kerrie",
                "Kerry",
                "Kira",
                "Kiri",
                "Kora",
                "Kore",
                "Kori",
                "Korie",
                "Korrie",
                "Korry" });
        }

        /**
         * Match data computed from http://www.lanw.com/java/phonetic/default.htm
         */
        [Test]
        public void TestIsMetaphoneEqualJohn()
        {
            this.AssertIsMetaphoneEqual(
                "John",
                new String[] {
                "Gena",
                "Gene",
                "Genia",
                "Genna",
                "Genni",
                "Gennie",
                "Genny",
                "Giana",
                "Gianna",
                "Gina",
                "Ginni",
                "Ginnie",
                "Ginny",
                "Jaine",
                "Jan",
                "Jana",
                "Jane",
                "Janey",
                "Jania",
                "Janie",
                "Janna",
                "Jany",
                "Jayne",
                "Jean",
                "Jeana",
                "Jeane",
                "Jeanie",
                "Jeanna",
                "Jeanne",
                "Jeannie",
                "Jen",
                "Jena",
                "Jeni",
                "Jenn",
                "Jenna",
                "Jennee",
                "Jenni",
                "Jennie",
                "Jenny",
                "Jinny",
                "Jo Ann",
                "Jo-Ann",
                "Jo-Anne",
                "Joan",
                "Joana",
                "Joane",
                "Joanie",
                "Joann",
                "Joanna",
                "Joanne",
                "Joeann",
                "Johna",
                "Johnna",
                "Joni",
                "Jonie",
                "Juana",
                "June",
                "Junia",
                "Junie" });
        }

        /**
         * Initial KN case.
         *
         * Match data computed from http://www.lanw.com/java/phonetic/default.htm
         */
        [Test]
        public void TestIsMetaphoneEqualKnight()
        {
            this.AssertIsMetaphoneEqual(
                "Knight",
                new String[] {
                "Hynda",
                "Nada",
                "Nadia",
                "Nady",
                "Nat",
                "Nata",
                "Natty",
                "Neda",
                "Nedda",
                "Nedi",
                "Netta",
                "Netti",
                "Nettie",
                "Netty",
                "Nita",
                "Nydia" });
        }
        /**
         * Match data computed from http://www.lanw.com/java/phonetic/default.htm
         */
        [Test]
        public void TestIsMetaphoneEqualMary()
        {
            this.AssertIsMetaphoneEqual(
                "Mary",
                new String[] {
                "Mair",
                "Maire",
                "Mara",
                "Mareah",
                "Mari",
                "Maria",
                "Marie",
                "Mary",
                "Maura",
                "Maure",
                "Meara",
                "Merrie",
                "Merry",
                "Mira",
                "Moira",
                "Mora",
                "Moria",
                "Moyra",
                "Muire",
                "Myra",
                "Myrah" });
        }

        /**
         * Match data computed from http://www.lanw.com/java/phonetic/default.htm
         */
        [Test]
        public void TestIsMetaphoneEqualParis()
        {
            this.AssertIsMetaphoneEqual("Paris", new String[] { "Pearcy", "Perris", "Piercy", "Pierz", "Pryse" });
        }

        /**
         * Match data computed from http://www.lanw.com/java/phonetic/default.htm
         */
        [Test]
        public void TestIsMetaphoneEqualPeter()
        {
            this.AssertIsMetaphoneEqual(
                "Peter",
                new String[] { "Peadar", "Peder", "Pedro", "Peter", "Petr", "Peyter", "Pieter", "Pietro", "Piotr" });
        }

        /**
         * Match data computed from http://www.lanw.com/java/phonetic/default.htm
         */
        [Test]
        public void TestIsMetaphoneEqualRay()
        {
            this.AssertIsMetaphoneEqual("Ray", new String[] { "Ray", "Rey", "Roi", "Roy", "Ruy" });
        }

        /**
         * Match data computed from http://www.lanw.com/java/phonetic/default.htm
         */
        [Test]
        public void TestIsMetaphoneEqualSusan()
        {
            this.AssertIsMetaphoneEqual(
                "Susan",
                new String[] {
                "Siusan",
                "Sosanna",
                "Susan",
                "Susana",
                "Susann",
                "Susanna",
                "Susannah",
                "Susanne",
                "Suzann",
                "Suzanna",
                "Suzanne",
                "Zuzana" });
        }

        /**
         * Initial WR case.
         *
         * Match data computed from http://www.lanw.com/java/phonetic/default.htm
         */
        [Test]
        public void TestIsMetaphoneEqualWright()
        {
            this.AssertIsMetaphoneEqual("Wright", new String[] { "Rota", "Rudd", "Ryde" });
        }

        /**
         * Match data computed from http://www.lanw.com/java/phonetic/default.htm
         */
        [Test]
        public void TestIsMetaphoneEqualXalan()
        {
            this.AssertIsMetaphoneEqual(
                "Xalan",
                new String[] { "Celene", "Celina", "Celine", "Selena", "Selene", "Selina", "Seline", "Suellen", "Xylina" });
        }

        [Test]
        public void TestMetaphone()
        {
            Assert.AreEqual("HL", this.StringEncoder.GetMetaphone("howl"));
            Assert.AreEqual("TSTN", this.StringEncoder.GetMetaphone("testing"));
            Assert.AreEqual("0", this.StringEncoder.GetMetaphone("The"));
            Assert.AreEqual("KK", this.StringEncoder.GetMetaphone("quick"));
            Assert.AreEqual("BRN", this.StringEncoder.GetMetaphone("brown"));
            Assert.AreEqual("FKS", this.StringEncoder.GetMetaphone("fox"));
            Assert.AreEqual("JMPT", this.StringEncoder.GetMetaphone("jumped"));
            Assert.AreEqual("OFR", this.StringEncoder.GetMetaphone("over"));
            Assert.AreEqual("0", this.StringEncoder.GetMetaphone("the"));
            Assert.AreEqual("LS", this.StringEncoder.GetMetaphone("lazy"));
            Assert.AreEqual("TKS", this.StringEncoder.GetMetaphone("dogs"));
        }

        [Test]
        public void TestWordEndingInMB()
        {
            Assert.AreEqual("KM", this.StringEncoder.GetMetaphone("COMB"));
            Assert.AreEqual("TM", this.StringEncoder.GetMetaphone("TOMB"));
            Assert.AreEqual("WM", this.StringEncoder.GetMetaphone("WOMB"));
        }

        [Test]
        public void TestDiscardOfSCEOrSCIOrSCY()
        {
            Assert.AreEqual("SNS", this.StringEncoder.GetMetaphone("SCIENCE"));
            Assert.AreEqual("SN", this.StringEncoder.GetMetaphone("SCENE"));
            Assert.AreEqual("S", this.StringEncoder.GetMetaphone("SCY"));
        }

        /**
         * Tests (CODEC-57) Metaphone.metaphone(String) returns an empty string when passed the word "why"
         */
        [Test]
        public void TestWhy()
        {
            // PHP returns "H". The original metaphone returns an empty string.
            Assert.AreEqual("", this.StringEncoder.GetMetaphone("WHY"));
        }

        [Test]
        public void TestWordsWithCIA()
        {
            Assert.AreEqual("XP", this.StringEncoder.GetMetaphone("CIAPO"));
        }

        [Test]
        public void TestTranslateOfSCHAndCH()
        {
            Assert.AreEqual("SKTL", this.StringEncoder.GetMetaphone("SCHEDULE"));
            Assert.AreEqual("SKMT", this.StringEncoder.GetMetaphone("SCHEMATIC"));

            Assert.AreEqual("KRKT", this.StringEncoder.GetMetaphone("CHARACTER"));
            Assert.AreEqual("TX", this.StringEncoder.GetMetaphone("TEACH"));
        }

        [Test]
        public void TestTranslateToJOfDGEOrDGIOrDGY()
        {
            Assert.AreEqual("TJ", this.StringEncoder.GetMetaphone("DODGY"));
            Assert.AreEqual("TJ", this.StringEncoder.GetMetaphone("DODGE"));
            Assert.AreEqual("AJMT", this.StringEncoder.GetMetaphone("ADGIEMTI"));
        }

        [Test]
        public void TestDiscardOfSilentHAfterG()
        {
            Assert.AreEqual("KNT", this.StringEncoder.GetMetaphone("GHENT"));
            Assert.AreEqual("B", this.StringEncoder.GetMetaphone("BAUGH"));
        }

        [Test]
        public void TestDiscardOfSilentGN()
        {
            // NOTE: This does not test for silent GN, but for starting with GN
            Assert.AreEqual("N", this.StringEncoder.GetMetaphone("GNU"));

            // NOTE: Trying to test for GNED, but expected code does not appear to execute
            Assert.AreEqual("SNT", this.StringEncoder.GetMetaphone("SIGNED"));
        }

        [Test]
        public void TestPHTOF()
        {
            Assert.AreEqual("FX", this.StringEncoder.GetMetaphone("PHISH"));
        }

        [Test]
        public void TestSHAndSIOAndSIAToX()
        {
            Assert.AreEqual("XT", this.StringEncoder.GetMetaphone("SHOT"));
            Assert.AreEqual("OTXN", this.StringEncoder.GetMetaphone("ODSIAN"));
            Assert.AreEqual("PLXN", this.StringEncoder.GetMetaphone("PULSION"));
        }

        [Test]
        public void TestTIOAndTIAToX()
        {
            Assert.AreEqual("OX", this.StringEncoder.GetMetaphone("OTIA"));
            Assert.AreEqual("PRXN", this.StringEncoder.GetMetaphone("PORTION"));
        }

        [Test]
        public void TestTCH()
        {
            Assert.AreEqual("RX", this.StringEncoder.GetMetaphone("RETCH"));
            Assert.AreEqual("WX", this.StringEncoder.GetMetaphone("WATCH"));
        }

        [Test]
        public void TestExceedLength()
        {
            // should be AKSKS, but istruncated by Max Code Length
            Assert.AreEqual("AKSK", this.StringEncoder.GetMetaphone("AXEAXE"));
        }

        [Test]
        public void TestSetMaxLengthWithTruncation()
        {
            // should be AKSKS, but istruncated by Max Code Length
            this.StringEncoder.MaxCodeLen=(6);
            Assert.AreEqual("AKSKSK", this.StringEncoder.GetMetaphone("AXEAXEAXE"));
        }

        public void ValidateFixture(String[][] pairs)
        {
            if (pairs.Length == 0)
            {
                Assert.Fail("Test fixture is empty");
            }
            for (int i = 0; i < pairs.Length; i++)
            {
                if (pairs[i].Length != 2)
                {
                    Assert.Fail("Error in test fixture in the data array at index " + i);
                }
            }
        }
    }
}
