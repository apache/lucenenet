// commons-codec version compatibility level: 1.10
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

    /// <summary>
    /// Tests <see cref="Soundex"/>
    /// </summary>
    public class SoundexTest : StringEncoderAbstractTest<Soundex>
    {
        protected override Soundex CreateStringEncoder()
        {
            return new Soundex();
        }

        [Test]
        public void TestB650()
        {
            this.CheckEncodingVariations("B650", new string[]{
            "BARHAM",
            "BARONE",
            "BARRON",
            "BERNA",
            "BIRNEY",
            "BIRNIE",
            "BOOROM",
            "BOREN",
            "BORN",
            "BOURN",
            "BOURNE",
            "BOWRON",
            "BRAIN",
            "BRAME",
            "BRANN",
            "BRAUN",
            "BREEN",
            "BRIEN",
            "BRIM",
            "BRIMM",
            "BRINN",
            "BRION",
            "BROOM",
            "BROOME",
            "BROWN",
            "BROWNE",
            "BRUEN",
            "BRUHN",
            "BRUIN",
            "BRUMM",
            "BRUN",
            "BRUNO",
            "BRYAN",
            "BURIAN",
            "BURN",
            "BURNEY",
            "BYRAM",
            "BYRNE",
            "BYRON",
            "BYRUM"});
        }

        [Test]
        public void TestBadCharacters()
        {
            Assert.AreEqual("H452", this.StringEncoder.Encode("HOL>MES"));

        }

        [Test]
        public void TestDifference()
        {
            // Edge cases
            Assert.AreEqual(0, this.StringEncoder.Difference(null, null));
            Assert.AreEqual(0, this.StringEncoder.Difference("", ""));
            Assert.AreEqual(0, this.StringEncoder.Difference(" ", " "));
            // Normal cases
            Assert.AreEqual(4, this.StringEncoder.Difference("Smith", "Smythe"));
            Assert.AreEqual(2, this.StringEncoder.Difference("Ann", "Andrew"));
            Assert.AreEqual(1, this.StringEncoder.Difference("Margaret", "Andrew"));
            Assert.AreEqual(0, this.StringEncoder.Difference("Janet", "Margaret"));
            // Examples from http://msdn.microsoft.com/library/default.asp?url=/library/en-us/tsqlref/ts_de-dz_8co5.asp
            Assert.AreEqual(4, this.StringEncoder.Difference("Green", "Greene"));
            Assert.AreEqual(0, this.StringEncoder.Difference("Blotchet-Halls", "Greene"));
            // Examples from http://msdn.microsoft.com/library/default.asp?url=/library/en-us/tsqlref/ts_setu-sus_3o6w.asp
            Assert.AreEqual(4, this.StringEncoder.Difference("Smith", "Smythe"));
            Assert.AreEqual(4, this.StringEncoder.Difference("Smithers", "Smythers"));
            Assert.AreEqual(2, this.StringEncoder.Difference("Anothers", "Brothers"));
        }

        [Test]
        public void TestEncodeBasic()
        {
            Assert.AreEqual("T235", this.StringEncoder.Encode("testing"));
            Assert.AreEqual("T000", this.StringEncoder.Encode("The"));
            Assert.AreEqual("Q200", this.StringEncoder.Encode("quick"));
            Assert.AreEqual("B650", this.StringEncoder.Encode("brown"));
            Assert.AreEqual("F200", this.StringEncoder.Encode("fox"));
            Assert.AreEqual("J513", this.StringEncoder.Encode("jumped"));
            Assert.AreEqual("O160", this.StringEncoder.Encode("over"));
            Assert.AreEqual("T000", this.StringEncoder.Encode("the"));
            Assert.AreEqual("L200", this.StringEncoder.Encode("lazy"));
            Assert.AreEqual("D200", this.StringEncoder.Encode("dogs"));
        }

        /**
         * Examples from http://www.bradandkathy.com/genealogy/overviewofsoundex.html
         */
        [Test]
        public void RestEncodeBatch2()
        {
            Assert.AreEqual("A462", this.StringEncoder.Encode("Allricht"));
            Assert.AreEqual("E166", this.StringEncoder.Encode("Eberhard"));
            Assert.AreEqual("E521", this.StringEncoder.Encode("Engebrethson"));
            Assert.AreEqual("H512", this.StringEncoder.Encode("Heimbach"));
            Assert.AreEqual("H524", this.StringEncoder.Encode("Hanselmann"));
            Assert.AreEqual("H431", this.StringEncoder.Encode("Hildebrand"));
            Assert.AreEqual("K152", this.StringEncoder.Encode("Kavanagh"));
            Assert.AreEqual("L530", this.StringEncoder.Encode("Lind"));
            Assert.AreEqual("L222", this.StringEncoder.Encode("Lukaschowsky"));
            Assert.AreEqual("M235", this.StringEncoder.Encode("McDonnell"));
            Assert.AreEqual("M200", this.StringEncoder.Encode("McGee"));
            Assert.AreEqual("O155", this.StringEncoder.Encode("Opnian"));
            Assert.AreEqual("O155", this.StringEncoder.Encode("Oppenheimer"));
            Assert.AreEqual("R355", this.StringEncoder.Encode("Riedemanas"));
            Assert.AreEqual("Z300", this.StringEncoder.Encode("Zita"));
            Assert.AreEqual("Z325", this.StringEncoder.Encode("Zitzmeinn"));
        }

        /**
         * Examples from http://www.archives.gov/research_room/genealogy/census/soundex.html
         */
        [Test]
        public void TestEncodeBatch3()
        {
            Assert.AreEqual("W252", this.StringEncoder.Encode("Washington"));
            Assert.AreEqual("L000", this.StringEncoder.Encode("Lee"));
            Assert.AreEqual("G362", this.StringEncoder.Encode("Gutierrez"));
            Assert.AreEqual("P236", this.StringEncoder.Encode("Pfister"));
            Assert.AreEqual("J250", this.StringEncoder.Encode("Jackson"));
            Assert.AreEqual("T522", this.StringEncoder.Encode("Tymczak"));
            // For VanDeusen: D-250 (D, 2 for the S, 5 for the N, 0 added) is also
            // possible.
            Assert.AreEqual("V532", this.StringEncoder.Encode("VanDeusen"));
        }

        /**
         * Examples from: http://www.myatt.demon.co.uk/sxalg.htm
         */
        [Test]
        public void TestEncodeBatch4()
        {
            Assert.AreEqual("H452", this.StringEncoder.Encode("HOLMES"));
            Assert.AreEqual("A355", this.StringEncoder.Encode("ADOMOMI"));
            Assert.AreEqual("V536", this.StringEncoder.Encode("VONDERLEHR"));
            Assert.AreEqual("B400", this.StringEncoder.Encode("BALL"));
            Assert.AreEqual("S000", this.StringEncoder.Encode("SHAW"));
            Assert.AreEqual("J250", this.StringEncoder.Encode("JACKSON"));
            Assert.AreEqual("S545", this.StringEncoder.Encode("SCANLON"));
            Assert.AreEqual("S532", this.StringEncoder.Encode("SAINTJOHN"));

        }

        [Test]
        public void TestEncodeIgnoreApostrophes()
        {
            this.CheckEncodingVariations("O165", new string[]{
            "OBrien",
            "'OBrien",
            "O'Brien",
            "OB'rien",
            "OBr'ien",
            "OBri'en",
            "OBrie'n",
            "OBrien'"});
        }

        /**
         * Test data from http://www.myatt.demon.co.uk/sxalg.htm
         *
         * @throws EncoderException
         */
        [Test]
        public void TestEncodeIgnoreHyphens()
        {
            this.CheckEncodingVariations("K525", new String[]{
            "KINGSMITH",
            "-KINGSMITH",
            "K-INGSMITH",
            "KI-NGSMITH",
            "KIN-GSMITH",
            "KING-SMITH",
            "KINGS-MITH",
            "KINGSM-ITH",
            "KINGSMI-TH",
            "KINGSMIT-H",
            "KINGSMITH-"});
        }

        [Test]
        public void TestEncodeIgnoreTrimmable()
        {
            Assert.AreEqual("W252", this.StringEncoder.Encode(" \t\n\r Washington \t\n\r "));
        }

        /**
         * Consonants from the same code group separated by W or H are treated as one.
         */
        [Test]
        public void TestHWRuleEx1()
        {
            // From
            // http://www.archives.gov/research_room/genealogy/census/soundex.html:
            // Ashcraft is coded A-261 (A, 2 for the S, C ignored, 6 for the R, 1
            // for the F). It is not coded A-226.
            Assert.AreEqual("A261", this.StringEncoder.Encode("Ashcraft"));
        }

        /**
         * Consonants from the same code group separated by W or H are treated as one.
         *
         * Test data from http://www.myatt.demon.co.uk/sxalg.htm
         */
        [Test]
        public void TestHWRuleEx2()
        {
            Assert.AreEqual("B312", this.StringEncoder.Encode("BOOTHDAVIS"));
            Assert.AreEqual("B312", this.StringEncoder.Encode("BOOTH-DAVIS"));
        }

        /**
         * Consonants from the same code group separated by W or H are treated as one.
         *
         * @throws EncoderException
         */
        [Test]
        public void TestHWRuleEx3()
        {
            Assert.AreEqual("S460", this.StringEncoder.Encode("Sgler"));
            Assert.AreEqual("S460", this.StringEncoder.Encode("Swhgler"));
            // Also S460:
            this.CheckEncodingVariations("S460", new String[]{
            "SAILOR",
            "SALYER",
            "SAYLOR",
            "SCHALLER",
            "SCHELLER",
            "SCHILLER",
            "SCHOOLER",
            "SCHULER",
            "SCHUYLER",
            "SEILER",
            "SEYLER",
            "SHOLAR",
            "SHULER",
            "SILAR",
            "SILER",
            "SILLER"});
        }

        /**
         * Examples for MS SQLServer from
         * http://msdn.microsoft.com/library/default.asp?url=/library/en-us/tsqlref/ts_setu-sus_3o6w.asp
         */
        [Test]
        public void TestMsSqlServer1()
        {
            Assert.AreEqual("S530", this.StringEncoder.Encode("Smith"));
            Assert.AreEqual("S530", this.StringEncoder.Encode("Smythe"));
        }

        /**
         * Examples for MS SQLServer from
         * http://support.microsoft.com/default.aspx?scid=http://support.microsoft.com:80/support
         * /kb/articles/Q100/3/65.asp&NoWebContent=1
         *
         * @throws EncoderException
         */
        [Test]
        public void TestMsSqlServer2()
        {
            this.CheckEncodingVariations("E625", new String[] { "Erickson", "Erickson", "Erikson", "Ericson", "Ericksen", "Ericsen" });
        }

        /**
         * Examples for MS SQLServer from http://databases.about.com/library/weekly/aa042901a.htm
         */
        [Test]
        public void TestMsSqlServer3()
        {
            Assert.AreEqual("A500", this.StringEncoder.Encode("Ann"));
            Assert.AreEqual("A536", this.StringEncoder.Encode("Andrew"));
            Assert.AreEqual("J530", this.StringEncoder.Encode("Janet"));
            Assert.AreEqual("M626", this.StringEncoder.Encode("Margaret"));
            Assert.AreEqual("S315", this.StringEncoder.Encode("Steven"));
            Assert.AreEqual("M240", this.StringEncoder.Encode("Michael"));
            Assert.AreEqual("R163", this.StringEncoder.Encode("Robert"));
            Assert.AreEqual("L600", this.StringEncoder.Encode("Laura"));
            Assert.AreEqual("A500", this.StringEncoder.Encode("Anne"));
        }

        /**
         * https://issues.apache.org/jira/browse/CODEC-54 https://issues.apache.org/jira/browse/CODEC-56
         */
        [Test]
        public void TestNewInstance()
        {
            Assert.AreEqual("W452", new Soundex().GetSoundex("Williams"));
        }

        [Test]
        public void TestNewInstance2()
        {
            Assert.AreEqual("W452", new Soundex(Soundex.US_ENGLISH_MAPPING_STRING.toCharArray()).GetSoundex("Williams"));
        }

        [Test]
        public void TestNewInstance3()
        {
            Assert.AreEqual("W452", new Soundex(Soundex.US_ENGLISH_MAPPING_STRING).GetSoundex("Williams"));
        }

        [Test]
        public void TestSoundexUtilsConstructable()
        {
            new SoundexUtils();
        }

        [Test]
        public void TestSoundexUtilsNullBehaviour()
        {
            Assert.AreEqual(null, SoundexUtils.Clean(null));
            Assert.AreEqual("", SoundexUtils.Clean(""));
            Assert.AreEqual(0, SoundexUtils.DifferenceEncoded(null, ""));
            Assert.AreEqual(0, SoundexUtils.DifferenceEncoded("", null));
        }

        /**
         * https://issues.apache.org/jira/browse/CODEC-54 https://issues.apache.org/jira/browse/CODEC-56
         */
        [Test]
        public void TestUsEnglishStatic()
        {
            Assert.AreEqual("W452", Soundex.US_ENGLISH.GetSoundex("Williams"));
        }

        /**
         * Fancy characters are not mapped by the default US mapping.
         *
         * http://issues.apache.org/bugzilla/show_bug.cgi?id=29080
         */
        [Test]
        public void TestUsMappingEWithAcute()
        {
            Assert.AreEqual("E000", this.StringEncoder.Encode("e"));
            if (char.IsLetter('\u00e9'))
            { // e-acute
                try
                {
                    //         uppercase E-acute
                    Assert.AreEqual("\u00c9000", this.StringEncoder.Encode("\u00e9"));
                    Assert.Fail("Expected IllegalArgumentException not thrown");
                }
                catch (Exception e) when (e.IsIllegalArgumentException())
                {
                    // expected
                }
            }
            else
            {
                Assert.AreEqual("", this.StringEncoder.Encode("\u00e9"));
            }
        }

        /**
         * Fancy characters are not mapped by the default US mapping.
         *
         * http://issues.apache.org/bugzilla/show_bug.cgi?id=29080
         */
        [Test]
        public void TestUsMappingOWithDiaeresis()
        {
            Assert.AreEqual("O000", this.StringEncoder.Encode("o"));
            if (char.IsLetter('\u00f6'))
            { // o-umlaut
                try
                {
                    //         uppercase O-umlaut
                    Assert.AreEqual("\u00d6000", this.StringEncoder.Encode("\u00f6"));
                    Assert.Fail("Expected IllegalArgumentException not thrown");
                }
                catch (Exception e) when (e.IsIllegalArgumentException())
                {
                    // expected
                }
            }
            else
            {
                Assert.AreEqual("", this.StringEncoder.Encode("\u00f6"));
            }
        }
    }
}
