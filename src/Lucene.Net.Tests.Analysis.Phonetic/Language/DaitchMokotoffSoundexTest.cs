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
    /// Tests <see cref="DaitchMokotoffSoundex"/>.
    /// <para/>
    /// since 1.10
    /// </summary>
    public class DaitchMokotoffSoundexTest : StringEncoderAbstractTest<DaitchMokotoffSoundex>
    {
        protected override DaitchMokotoffSoundex CreateStringEncoder()
        {
            return new DaitchMokotoffSoundex();
        }

        private string GetSoundex(string source)
        {
            return StringEncoder.GetSoundex(source);
        }

        private string Encode(string source)
        {
            return StringEncoder.Encode(source);
        }

        [Test]
        public void TestAccentedCharacterFolding()
        {
            Assert.AreEqual("294795", GetSoundex("Straßburg"));
            Assert.AreEqual("294795", GetSoundex("Strasburg"));

            Assert.AreEqual("095600", GetSoundex("Éregon"));
            Assert.AreEqual("095600", GetSoundex("Eregon"));
        }

        [Test]
        public void TestAdjacentCodes()
        {
            // AKSSOL
            // A-KS-S-O-L
            // 0-54-4---8 -> wrong
            // 0-54-----8 -> correct
            Assert.AreEqual("054800", GetSoundex("AKSSOL"));

            // GERSCHFELD
            // G-E-RS-CH-F-E-L-D
            // 5--4/94-5/4-7-8-3 -> wrong
            // 5--4/94-5/--7-8-3 -> correct
            Assert.AreEqual("547830|545783|594783|594578", GetSoundex("GERSCHFELD"));
        }

        [Test]
        public void TestEncodeBasic()
        {
            // same as above, but without branching
            Assert.AreEqual("097400", Encode("AUERBACH"));
            Assert.AreEqual("097400", Encode("OHRBACH"));
            Assert.AreEqual("874400", Encode("LIPSHITZ"));
            Assert.AreEqual("874400", Encode("LIPPSZYC"));
            Assert.AreEqual("876450", Encode("LEWINSKY"));
            Assert.AreEqual("876450", Encode("LEVINSKI"));
            Assert.AreEqual("486740", Encode("SZLAMAWICZ"));
            Assert.AreEqual("486740", Encode("SHLAMOVITZ"));
        }

        [Test]
        public void TestEncodeIgnoreApostrophes()
        {
            this.CheckEncodingVariations("079600", new String[] { "OBrien", "'OBrien", "O'Brien", "OB'rien", "OBr'ien",
                "OBri'en", "OBrie'n", "OBrien'" });
        }

        /**
         * Test data from http://www.myatt.demon.co.uk/sxalg.htm
         *
         * @throws EncoderException
         */
        [Test]
        public void TestEncodeIgnoreHyphens()
        {
            this.CheckEncodingVariations("565463", new String[] { "KINGSMITH", "-KINGSMITH", "K-INGSMITH", "KI-NGSMITH",
                "KIN-GSMITH", "KING-SMITH", "KINGS-MITH", "KINGSM-ITH", "KINGSMI-TH", "KINGSMIT-H", "KINGSMITH-" });
        }

        [Test]
        public void TestEncodeIgnoreTrimmable()
        {
            Assert.AreEqual("746536", Encode(" \t\n\r Washington \t\n\r "));
            Assert.AreEqual("746536", Encode("Washington"));
        }

        /**
         * Examples from http://www.jewishgen.org/infofiles/soundex.html
         */
        [Test]
        public void TestSoundexBasic()
        {
            Assert.AreEqual("583600", GetSoundex("GOLDEN"));
            Assert.AreEqual("087930", GetSoundex("Alpert"));
            Assert.AreEqual("791900", GetSoundex("Breuer"));
            Assert.AreEqual("579000", GetSoundex("Haber"));
            Assert.AreEqual("665600", GetSoundex("Mannheim"));
            Assert.AreEqual("664000", GetSoundex("Mintz"));
            Assert.AreEqual("370000", GetSoundex("Topf"));
            Assert.AreEqual("586660", GetSoundex("Kleinmann"));
            Assert.AreEqual("769600", GetSoundex("Ben Aron"));

            Assert.AreEqual("097400|097500", GetSoundex("AUERBACH"));
            Assert.AreEqual("097400|097500", GetSoundex("OHRBACH"));
            Assert.AreEqual("874400", GetSoundex("LIPSHITZ"));
            Assert.AreEqual("874400|874500", GetSoundex("LIPPSZYC"));
            Assert.AreEqual("876450", GetSoundex("LEWINSKY"));
            Assert.AreEqual("876450", GetSoundex("LEVINSKI"));
            Assert.AreEqual("486740", GetSoundex("SZLAMAWICZ"));
            Assert.AreEqual("486740", GetSoundex("SHLAMOVITZ"));
        }

        /**
         * Examples from http://www.avotaynu.com/soundex.htm
         */
        [Test]
        public void TestSoundexBasic2()
        {
            Assert.AreEqual("467000|567000", GetSoundex("Ceniow"));
            Assert.AreEqual("467000", GetSoundex("Tsenyuv"));
            Assert.AreEqual("587400|587500", GetSoundex("Holubica"));
            Assert.AreEqual("587400", GetSoundex("Golubitsa"));
            Assert.AreEqual("746480|794648", GetSoundex("Przemysl"));
            Assert.AreEqual("746480", GetSoundex("Pshemeshil"));
            Assert.AreEqual("944744|944745|944754|944755|945744|945745|945754|945755", GetSoundex("Rosochowaciec"));
            Assert.AreEqual("945744", GetSoundex("Rosokhovatsets"));
        }

        /**
         * Examples from http://en.wikipedia.org/wiki/Daitch%E2%80%93Mokotoff_Soundex
         */
        [Test]
        public void TestSoundexBasic3()
        {
            Assert.AreEqual("734000|739400", GetSoundex("Peters"));
            Assert.AreEqual("734600|739460", GetSoundex("Peterson"));
            Assert.AreEqual("645740", GetSoundex("Moskowitz"));
            Assert.AreEqual("645740", GetSoundex("Moskovitz"));
            Assert.AreEqual("154600|145460|454600|445460", GetSoundex("Jackson"));
            Assert.AreEqual("154654|154645|154644|145465|145464|454654|454645|454644|445465|445464",
                    GetSoundex("Jackson-Jackson"));
        }

        [Test]
        public void TestSpecialRomanianCharacters()
        {
            Assert.AreEqual("364000|464000", GetSoundex("ţamas")); // t-cedilla
            Assert.AreEqual("364000|464000", GetSoundex("țamas")); // t-comma
        }
    }
}
