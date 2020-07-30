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
    /// Tests Caverphone2.
    /// </summary>
    public class Caverphone2Test : StringEncoderAbstractTest<Caverphone2>
    {
        protected override Caverphone2 CreateStringEncoder()
        {
            return new Caverphone2();
        }

        /**
         * See http://caversham.otago.ac.nz/files/working/ctp150804.pdf
         *
         * AT11111111 words: add, aid, at, art, eat, earth, head, hit, hot, hold, hard, heart, it, out, old
         *
         * @throws EncoderException
         */
        [Test]
        public void TestCaverphoneRevisitedCommonCodeAT11111111()
        {
            this.CheckEncodingVariations("AT11111111", new String[]{
            "add",
            "aid",
            "at",
            "art",
            "eat",
            "earth",
            "head",
            "hit",
            "hot",
            "hold",
            "hard",
            "heart",
            "it",
            "out",
            "old"});
        }

        /**
         * See http://caversham.otago.ac.nz/files/working/ctp150804.pdf
         *
         * @throws EncoderException
         */
        [Test]
        public void TestCaverphoneRevisitedExamples()
        {
            String[]
            []
            data = { new string[] { "Stevenson", "STFNSN1111" }, new string[] { "Peter", "PTA1111111" } };
            this.CheckEncodings(data);
        }

        /**
         * See http://caversham.otago.ac.nz/files/working/ctp150804.pdf
         *
         * @throws EncoderException
         */
        [Test]
        public void TestCaverphoneRevisitedRandomNameKLN1111111()
        {
            this.CheckEncodingVariations("KLN1111111", new String[]{
            "Cailean",
            "Calan",
            "Calen",
            "Callahan",
            "Callan",
            "Callean",
            "Carleen",
            "Carlen",
            "Carlene",
            "Carlin",
            "Carline",
            "Carlyn",
            "Carlynn",
            "Carlynne",
            "Charlean",
            "Charleen",
            "Charlene",
            "Charline",
            "Cherlyn",
            "Chirlin",
            "Clein",
            "Cleon",
            "Cline",
            "Cohleen",
            "Colan",
            "Coleen",
            "Colene",
            "Colin",
            "Colleen",
            "Collen",
            "Collin",
            "Colline",
            "Colon",
            "Cullan",
            "Cullen",
            "Cullin",
            "Gaelan",
            "Galan",
            "Galen",
            "Garlan",
            "Garlen",
            "Gaulin",
            "Gayleen",
            "Gaylene",
            "Giliane",
            "Gillan",
            "Gillian",
            "Glen",
            "Glenn",
            "Glyn",
            "Glynn",
            "Gollin",
            "Gorlin",
            "Kalin",
            "Karlan",
            "Karleen",
            "Karlen",
            "Karlene",
            "Karlin",
            "Karlyn",
            "Kaylyn",
            "Keelin",
            "Kellen",
            "Kellene",
            "Kellyann",
            "Kellyn",
            "Khalin",
            "Kilan",
            "Kilian",
            "Killen",
            "Killian",
            "Killion",
            "Klein",
            "Kleon",
            "Kline",
            "Koerlin",
            "Kylen",
            "Kylynn",
            "Quillan",
            "Quillon",
            "Qulllon",
            "Xylon"});
        }

        /**
         * See http://caversham.otago.ac.nz/files/working/ctp150804.pdf
         *
         * @throws EncoderException
         */
        [Test]
        public void TestCaverphoneRevisitedRandomNameTN11111111()
        {
            this.CheckEncodingVariations("TN11111111", new String[]{
            "Dan",
            "Dane",
            "Dann",
            "Darn",
            "Daune",
            "Dawn",
            "Ddene",
            "Dean",
            "Deane",
            "Deanne",
            "DeeAnn",
            "Deeann",
            "Deeanne",
            "Deeyn",
            "Den",
            "Dene",
            "Denn",
            "Deonne",
            "Diahann",
            "Dian",
            "Diane",
            "Diann",
            "Dianne",
            "Diannne",
            "Dine",
            "Dion",
            "Dione",
            "Dionne",
            "Doane",
            "Doehne",
            "Don",
            "Donn",
            "Doone",
            "Dorn",
            "Down",
            "Downe",
            "Duane",
            "Dun",
            "Dunn",
            "Duyne",
            "Dyan",
            "Dyane",
            "Dyann",
            "Dyanne",
            "Dyun",
            "Tan",
            "Tann",
            "Teahan",
            "Ten",
            "Tenn",
            "Terhune",
            "Thain",
            "Thaine",
            "Thane",
            "Thanh",
            "Thayne",
            "Theone",
            "Thin",
            "Thorn",
            "Thorne",
            "Thun",
            "Thynne",
            "Tien",
            "Tine",
            "Tjon",
            "Town",
            "Towne",
            "Turne",
            "Tyne"});
        }

        /**
         * See http://caversham.otago.ac.nz/files/working/ctp150804.pdf
         *
         * @throws EncoderException
         */
        [Test]
        public void TestCaverphoneRevisitedRandomNameTTA1111111()
        {
            this.CheckEncodingVariations("TTA1111111", new String[]{
            "Darda",
            "Datha",
            "Dedie",
            "Deedee",
            "Deerdre",
            "Deidre",
            "Deirdre",
            "Detta",
            "Didi",
            "Didier",
            "Dido",
            "Dierdre",
            "Dieter",
            "Dita",
            "Ditter",
            "Dodi",
            "Dodie",
            "Dody",
            "Doherty",
            "Dorthea",
            "Dorthy",
            "Doti",
            "Dotti",
            "Dottie",
            "Dotty",
            "Doty",
            "Doughty",
            "Douty",
            "Dowdell",
            "Duthie",
            "Tada",
            "Taddeo",
            "Tadeo",
            "Tadio",
            "Tati",
            "Teador",
            "Tedda",
            "Tedder",
            "Teddi",
            "Teddie",
            "Teddy",
            "Tedi",
            "Tedie",
            "Teeter",
            "Teodoor",
            "Teodor",
            "Terti",
            "Theda",
            "Theodor",
            "Theodore",
            "Theta",
            "Thilda",
            "Thordia",
            "Tilda",
            "Tildi",
            "Tildie",
            "Tildy",
            "Tita",
            "Tito",
            "Tjader",
            "Toddie",
            "Toddy",
            "Torto",
            "Tuddor",
            "Tudor",
            "Turtle",
            "Tuttle",
            "Tutto"});
        }

        /**
         * See http://caversham.otago.ac.nz/files/working/ctp150804.pdf
         *
         * @throws EncoderException
         */
        [Test]
        public void TestCaverphoneRevisitedRandomWords()
        {
            this.CheckEncodingVariations("RTA1111111", new String[] { "rather", "ready", "writer" });
            this.CheckEncoding("SSA1111111", "social");
            this.CheckEncodingVariations("APA1111111", new String[] { "able", "appear" });
        }

        [Test]
        public void TestEndMb()
        {
            String[]
            []
            data = { new string[] { "mb", "M111111111" }, new string[] { "mbmb", "MPM1111111" } };
            this.CheckEncodings(data);
        }

        // Caverphone Revisited
        [Test]
        public void TestIsCaverphoneEquals()
        {
            Caverphone2 caverphone = new Caverphone2();
            Assert.False(caverphone.IsEncodeEqual("Peter", "Stevenson"), "Caverphone encodings should not be equal");
            Assert.True(caverphone.IsEncodeEqual("Peter", "Peady"), "Caverphone encodings should be equal");
        }

        [Test]
        public void TestSpecificationExamples()
        {
            String[]
            []
            data = {
                new string[] { "Peter", "PTA1111111"},
                new string[] { "ready", "RTA1111111"},
                new string[] { "social", "SSA1111111"},
                new string[] { "able", "APA1111111"},
                new string[] { "Tedder", "TTA1111111"},
                new string[] { "Karleen", "KLN1111111"},
                new string[] { "Dyun", "TN11111111"}
            };
            this.CheckEncodings(data);
        }
    }
}
