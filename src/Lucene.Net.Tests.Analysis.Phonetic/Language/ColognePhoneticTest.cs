using NUnit.Framework;
using System;

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
    /// Tests the <see cref="ColognePhonetic"/> class.
    /// </summary>
    public class ColognePhoneticTest : StringEncoderAbstractTest<ColognePhonetic>
    {
        protected override ColognePhonetic CreateStringEncoder()
        {
            return new ColognePhonetic();
        }

        [Test]
        public void TestAabjoe()
        {
            this.CheckEncoding("01", "Aabjoe");
        }

        [Test]
        public void TestAaclan()
        {
            this.CheckEncoding("0856", "Aaclan");
        }

        /**
         * Tests [CODEC-122]
         *
         * @throws EncoderException
         */
        [Test]
        public void TestAychlmajrForCodec122()
        {
            this.CheckEncoding("04567", "Aychlmajr");
        }

        [Test]
        public void TestEdgeCases()
        {
            String[][] data = {
            new string[] { "a", "0"},
            new string[] { "e", "0"},
            new string[] { "i", "0"},
            new string[] { "o", "0"},
            new string[] { "u", "0"},
            new string[] { "\u00E4", "0"}, // a-umlaut
            new string[] { "\u00F6", "0"}, // o-umlaut
            new string[] { "\u00FC", "0"}, // u-umlaut
            new string[] { "aa", "0"},
            new string[] { "ha", "0"},
            new string[] { "h", ""},
            new string[] { "aha", "0"},
            new string[] { "b", "1"},
            new string[] { "p", "1"},
            new string[] { "ph", "3"},
            new string[] { "f", "3"},
            new string[] { "v", "3"},
            new string[] { "w", "3"},
            new string[] { "g", "4"},
            new string[] { "k", "4"},
            new string[] { "q", "4"},
            new string[] { "x", "48"},
            new string[] { "ax", "048"},
            new string[] { "cx", "48"},
            new string[] { "l", "5"},
            new string[] { "cl", "45"},
            new string[] { "acl", "085"},
            new string[] { "mn", "6"},
            new string[] { "r", "7"}
            };
            this.CheckEncodings(data);
        }

        [Test]
        public void TestExamples()
        {
            String[][] data = {
            new string[] { "m\u00DCller", "657"}, // mÜller - why upper case U-umlaut?
            new string[] { "schmidt", "862"},
            new string[] { "schneider", "8627"},
            new string[] { "fischer", "387"},
            new string[] { "weber", "317"},
            new string[] { "wagner", "3467"},
            new string[] { "becker", "147"},
            new string[] { "hoffmann", "0366"},
            new string[] { "sch\u00C4fer", "837"}, // schÄfer - why upper case A-umlaut ?
            new string[] { "Breschnew", "17863"},
            new string[] { "Wikipedia", "3412"},
            new string[] { "peter", "127"},
            new string[] { "pharma", "376"},
            new string[] { "m\u00f6nchengladbach", "664645214"}, // mönchengladbach
            new string[] { "deutsch", "28"},
            new string[] { "deutz", "28"},
            new string[] { "hamburg", "06174"},
            new string[] { "hannover", "0637"},
            new string[] { "christstollen", "478256"},
            new string[] { "Xanthippe", "48621"},
            new string[] { "Zacharias", "8478"},
            new string[] { "Holzbau", "0581"},
            new string[] { "matsch", "68"},
            new string[] { "matz", "68"},
            new string[] { "Arbeitsamt", "071862"},
            new string[] { "Eberhard", "01772"},
            new string[] { "Eberhardt", "01772"},
            new string[] { "heithabu", "021"}
            };
            this.CheckEncodings(data);
        }

        [Test]
        public void TestHyphen()
        {
            String[][] data = {
                new string[] { "bergisch-gladbach", "174845214"},
                new string[] { "M\u00fcller-L\u00fcdenscheidt", "65752682"}
            }; // Müller-Lüdenscheidt
            this.CheckEncodings(data);
        }

        [Test]
        public void TestIsEncodeEquals()
        {
            String[][] data = {
            new string[] {"Meyer", "M\u00fcller"}, // Müller
            new string[] {"Meyer", "Mayr"},
            new string[] {"house", "house"},
            new string[] {"House", "house"},
            new string[] {"Haus", "house"},
            new string[] {"ganz", "Gans"},
            new string[] {"ganz", "G\u00e4nse"}, // Gänse
            new string[] {"Miyagi", "Miyako"}};
            foreach (String[] element in data)
            {
                this.StringEncoder.IsEncodeEqual(element[1], element[0]);
            }
        }

        [Test]
        public void TestVariationsMella()
        {
            String[] data = { "mella", "milah", "moulla", "mellah", "muehle", "mule" };
            this.CheckEncodingVariations("65", data);
        }

        [Test]
        public void TestVariationsMeyer()
        {
            String[] data = { "Meier", "Maier", "Mair", "Meyer", "Meyr", "Mejer", "Major" };
            this.CheckEncodingVariations("67", data);
        }
    }
}
