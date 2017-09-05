using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace Lucene.Net.Analysis.Ja.Util
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

    public class TestToStringUtil : LuceneTestCase
    {
        [Test]
        public void TestPOS()
        {
            assertEquals("noun-suffix-verbal", ToStringUtil.GetPOSTranslation("名詞-接尾-サ変接続"));
        }

        [Test]
        public void TestHepburn()
        {
            assertEquals("majan", ToStringUtil.GetRomanization("マージャン"));
            assertEquals("uroncha", ToStringUtil.GetRomanization("ウーロンチャ"));
            assertEquals("chahan", ToStringUtil.GetRomanization("チャーハン"));
            assertEquals("chashu", ToStringUtil.GetRomanization("チャーシュー"));
            assertEquals("shumai", ToStringUtil.GetRomanization("シューマイ"));
        }

        // see http://en.wikipedia.org/wiki/Hepburn_romanization,
        // but this isnt even thorough or really probably what we want!
        [Test]
        public void TestHepburnTable()
        {
            IDictionary<String, String> table = new Dictionary<String, String>() {
                { "ア", "a" }, { "イ", "i" }, { "ウ", "u" }, { "エ", "e" }, { "オ", "o" },
                { "カ", "ka" }, { "キ", "ki" }, { "ク", "ku" }, { "ケ", "ke" }, { "コ", "ko" },
                { "サ", "sa" }, { "シ", "shi" }, { "ス", "su" }, { "セ", "se" }, { "ソ", "so" },
                { "タ", "ta" }, { "チ", "chi" }, { "ツ", "tsu" }, { "テ", "te" }, { "ト", "to" },
                { "ナ", "na" }, { "ニ", "ni" }, { "ヌ", "nu" }, { "ネ", "ne" }, { "ノ", "no" },
                { "ハ", "ha" }, { "ヒ", "hi" }, { "フ", "fu" }, { "ヘ", "he" }, { "ホ", "ho" },
                { "マ", "ma" }, { "ミ", "mi" }, { "ム", "mu" }, { "メ", "me" }, { "モ", "mo" },
                { "ヤ", "ya" }, { "ユ", "yu" }, { "ヨ", "yo" },
                { "ラ", "ra" }, { "リ", "ri" }, { "ル", "ru" }, { "レ", "re" }, { "ロ", "ro" },
                { "ワ", "wa" }, { "ヰ", "i" }, { "ヱ", "e" }, { "ヲ", "o" },
                { "ン", "n" },
                { "ガ", "ga" }, { "ギ", "gi" }, { "グ", "gu" }, { "ゲ", "ge" }, { "ゴ", "go" },
                { "ザ", "za" }, { "ジ", "ji" }, { "ズ", "zu" }, { "ゼ", "ze" }, { "ゾ", "zo" },
                { "ダ", "da" }, { "ヂ", "ji" }, { "ヅ", "zu" }, { "デ", "de" }, { "ド", "do" },
                { "バ", "ba" }, { "ビ", "bi" }, { "ブ", "bu" }, { "ベ", "be" }, { "ボ", "bo" },
                { "パ", "pa" }, { "ピ", "pi" }, { "プ", "pu" }, { "ペ", "pe" }, { "ポ", "po" },

                { "キャ", "kya" }, { "キュ", "kyu" }, { "キョ", "kyo" },
                { "シャ", "sha" }, { "シュ", "shu" }, { "ショ", "sho" },
                { "チャ", "cha" }, { "チュ", "chu" }, { "チョ", "cho" },
                { "ニャ", "nya" }, { "ニュ", "nyu" }, { "ニョ", "nyo" },
                { "ヒャ", "hya" }, { "ヒュ", "hyu" }, { "ヒョ", "hyo" },
                { "ミャ", "mya" }, { "ミュ", "myu" }, { "ミョ", "myo" },
                { "リャ", "rya" }, { "リュ", "ryu" }, { "リョ", "ryo" },
                { "ギャ", "gya" }, { "ギュ", "gyu" }, { "ギョ", "gyo" },
                { "ジャ", "ja" }, { "ジュ", "ju" }, { "ジョ", "jo" },
                { "ヂャ", "ja" }, { "ヂュ", "ju" }, { "ヂョ", "jo" },
                { "ビャ", "bya" }, { "ビュ", "byu" }, { "ビョ", "byo" },
                { "ピャ", "pya" }, { "ピュ", "pyu" }, { "ピョ", "pyo" },

                { "イィ", "yi" }, { "イェ", "ye" },
                { "ウァ", "wa" }, { "ウィ", "wi" }, { "ウゥ", "wu" }, { "ウェ", "we" }, { "ウォ", "wo" },
                { "ウュ", "wyu" },
                // TODO: really should be vu
                { "ヴァ", "va" }, { "ヴィ", "vi" }, { "ヴ", "v" }, { "ヴェ", "ve" }, { "ヴォ", "vo" },
                { "ヴャ", "vya" }, { "ヴュ", "vyu" }, { "ヴィェ", "vye" }, { "ヴョ", "vyo" },
                { "キェ", "kye" },
                { "ギェ", "gye" },
                { "クァ", "kwa" }, { "クィ", "kwi" }, { "クェ", "kwe" }, { "クォ", "kwo" },
                { "クヮ", "kwa" },
                { "グァ", "gwa" }, { "グィ", "gwi" }, { "グェ", "gwe" }, { "グォ", "gwo" },
                { "グヮ", "gwa" },
                { "シェ", "she" },
                { "ジェ", "je" },
                { "スィ", "si" },
                { "ズィ", "zi" },
                { "チェ", "che" },
                { "ツァ", "tsa" }, { "ツィ", "tsi" }, { "ツェ", "tse" }, { "ツォ", "tso" },
                { "ツュ", "tsyu" },
                { "ティ", "ti" }, { "トゥ", "tu" },
                { "テュ", "tyu" },
                { "ディ", "di" }, { "ドゥ", "du" },
                { "デュ", "dyu" },
                { "ニェ", "nye" },
                { "ヒェ", "hye" },
                { "ビェ", "bye" },
                { "ピェ", "pye" },
                { "ファ", "fa" }, { "フィ", "fi" }, { "フェ", "fe" }, { "フォ", "fo" },
                { "フャ", "fya" }, { "フュ", "fyu" }, { "フィェ", "fye" }, { "フョ", "fyo" },
                { "ホゥ", "hu" },
                { "ミェ", "mye" },
                { "リェ", "rye" },
                { "ラ゜", "la" }, { "リ゜", "li" }, { "ル゜", "lu" }, { "レ゜", "le" }, { "ロ゜", "lo" },
                { "ヷ", "va" }, { "ヸ", "vi" }, { "ヹ", "ve" }, { "ヺ", "vo" },
            };

            foreach (String s in table.Keys)
            {
                assertEquals(s, table[s], ToStringUtil.GetRomanization(s));
            }
        }
    }
}
