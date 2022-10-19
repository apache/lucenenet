using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

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

    /// <summary>
    /// Utility class for english translations of morphological data,
    /// used only for debugging.
    /// </summary>
    public static class ToStringUtil
    {
        // a translation map for parts of speech, only used for reflectWith
        private static readonly IDictionary<string, string> posTranslations = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "名詞", "noun"},
            { "名詞-一般", "noun-common" },
            { "名詞-固有名詞", "noun-proper" },
            { "名詞-固有名詞-一般", "noun-proper-misc" },
            { "名詞-固有名詞-人名", "noun-proper-person" },
            { "名詞-固有名詞-人名-一般", "noun-proper-person-misc" },
            { "名詞-固有名詞-人名-姓", "noun-proper-person-surname" },
            { "名詞-固有名詞-人名-名", "noun-proper-person-given_name" },
            { "名詞-固有名詞-組織", "noun-proper-organization" },
            { "名詞-固有名詞-地域", "noun-proper-place" },
            { "名詞-固有名詞-地域-一般", "noun-proper-place-misc" },
            { "名詞-固有名詞-地域-国", "noun-proper-place-country" },
            { "名詞-代名詞", "noun-pronoun" },
            { "名詞-代名詞-一般", "noun-pronoun-misc" },
            { "名詞-代名詞-縮約", "noun-pronoun-contraction" },
            { "名詞-副詞可能", "noun-adverbial" },
            { "名詞-サ変接続", "noun-verbal" },
            { "名詞-形容動詞語幹", "noun-adjective-base" },
            { "名詞-数", "noun-numeric" },
            { "名詞-非自立", "noun-affix" },
            { "名詞-非自立-一般", "noun-affix-misc" },
            { "名詞-非自立-副詞可能", "noun-affix-adverbial" },
            { "名詞-非自立-助動詞語幹", "noun-affix-aux" },
            { "名詞-非自立-形容動詞語幹", "noun-affix-adjective-base" },
            { "名詞-特殊", "noun-special" },
            { "名詞-特殊-助動詞語幹", "noun-special-aux" },
            { "名詞-接尾", "noun-suffix" },
            { "名詞-接尾-一般", "noun-suffix-misc" },
            { "名詞-接尾-人名", "noun-suffix-person" },
            { "名詞-接尾-地域", "noun-suffix-place" },
            { "名詞-接尾-サ変接続", "noun-suffix-verbal" },
            { "名詞-接尾-助動詞語幹", "noun-suffix-aux" },
            { "名詞-接尾-形容動詞語幹", "noun-suffix-adjective-base" },
            { "名詞-接尾-副詞可能", "noun-suffix-adverbial" },
            { "名詞-接尾-助数詞", "noun-suffix-classifier" },
            { "名詞-接尾-特殊", "noun-suffix-special" },
            { "名詞-接続詞的", "noun-suffix-conjunctive" },
            { "名詞-動詞非自立的", "noun-verbal_aux" },
            { "名詞-引用文字列", "noun-quotation" },
            { "名詞-ナイ形容詞語幹", "noun-nai_adjective" },
            { "接頭詞", "prefix" },
            { "接頭詞-名詞接続", "prefix-nominal" },
            { "接頭詞-動詞接続", "prefix-verbal" },
            { "接頭詞-形容詞接続", "prefix-adjectival" },
            { "接頭詞-数接続", "prefix-numerical" },
            { "動詞", "verb" },
            { "動詞-自立", "verb-main" },
            { "動詞-非自立", "verb-auxiliary" },
            { "動詞-接尾", "verb-suffix" },
            { "形容詞", "adjective" },
            { "形容詞-自立", "adjective-main" },
            { "形容詞-非自立", "adjective-auxiliary" },
            { "形容詞-接尾", "adjective-suffix" },
            { "副詞", "adverb" },
            { "副詞-一般", "adverb-misc" },
            { "副詞-助詞類接続", "adverb-particle_conjunction" },
            { "連体詞", "adnominal" },
            { "接続詞", "conjunction" },
            { "助詞", "particle" },
            { "助詞-格助詞", "particle-case" },
            { "助詞-格助詞-一般", "particle-case-misc" },
            { "助詞-格助詞-引用", "particle-case-quote" },
            { "助詞-格助詞-連語", "particle-case-compound" },
            { "助詞-接続助詞", "particle-conjunctive" },
            { "助詞-係助詞", "particle-dependency" },
            { "助詞-副助詞", "particle-adverbial" },
            { "助詞-間投助詞", "particle-interjective" },
            { "助詞-並立助詞", "particle-coordinate" },
            { "助詞-終助詞", "particle-final" },
            { "助詞-副助詞／並立助詞／終助詞", "particle-adverbial/conjunctive/final" },
            { "助詞-連体化", "particle-adnominalizer" },
            { "助詞-副詞化", "particle-adnominalizer" },
            { "助詞-特殊", "particle-special" },
            { "助動詞", "auxiliary-verb" },
            { "感動詞", "interjection" },
            { "記号", "symbol" },
            { "記号-一般", "symbol-misc" },
            { "記号-句点", "symbol-period" },
            { "記号-読点", "symbol-comma" },
            { "記号-空白", "symbol-space" },
            { "記号-括弧開", "symbol-open_bracket" },
            { "記号-括弧閉", "symbol-close_bracket" },
            { "記号-アルファベット", "symbol-alphabetic" },
            { "その他", "other" },
            { "その他-間投", "other-interjection" },
            { "フィラー", "filler" },
            { "非言語音", "non-verbal" },
            { "語断片", "fragment" },
            { "未知語", "unknown" }
        };


        /// <summary>
        /// Get the english form of a POS tag
        /// </summary>
        public static string GetPOSTranslation(string s)
        {
            posTranslations.TryGetValue(s, out string result);
            return result;
        }

        // a translation map for inflection types, only used for reflectWith
        private static readonly IDictionary<string, string> inflTypeTranslations = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "*", "*" },
            { "形容詞・アウオ段", "adj-group-a-o-u" },
            { "形容詞・イ段", "adj-group-i" },
            { "形容詞・イイ",  "adj-group-ii" },
            { "不変化型", "non-inflectional" },
            { "特殊・タ", "special-da" },
            { "特殊・ダ", "special-ta" },
            { "文語・ゴトシ", "classical-gotoshi" },
            { "特殊・ジャ", "special-ja" },
            { "特殊・ナイ", "special-nai" },
            { "五段・ラ行特殊", "5-row-cons-r-special" },
            { "特殊・ヌ", "special-nu" },
            { "文語・キ", "classical-ki" },
            { "特殊・タイ", "special-tai" },
            { "文語・ベシ", "classical-beshi" },
            { "特殊・ヤ", "special-ya" },
            { "文語・マジ", "classical-maji" },
            { "下二・タ行", "2-row-lower-cons-t" },
            { "特殊・デス", "special-desu" },
            { "特殊・マス", "special-masu" },
            { "五段・ラ行アル", "5-row-aru" },
            { "文語・ナリ", "classical-nari" },
            { "文語・リ", "classical-ri" },
            { "文語・ケリ", "classical-keri" },
            { "文語・ル", "classical-ru" },
            { "五段・カ行イ音便", "5-row-cons-k-i-onbin" },
            { "五段・サ行", "5-row-cons-s" },
            { "一段", "1-row" },
            { "五段・ワ行促音便", "5-row-cons-w-cons-onbin" },
            { "五段・マ行", "5-row-cons-m" },
            { "五段・タ行", "5-row-cons-t" },
            { "五段・ラ行", "5-row-cons-r" },
            { "サ変・−スル", "irregular-suffix-suru" },
            { "五段・ガ行", "5-row-cons-g" },
            { "サ変・−ズル", "irregular-suffix-zuru" },
            { "五段・バ行", "5-row-cons-b" },
            { "五段・ワ行ウ音便", "5-row-cons-w-u-onbin" },
            { "下二・ダ行", "2-row-lower-cons-d" },
            { "五段・カ行促音便ユク", "5-row-cons-k-cons-onbin-yuku" },
            { "上二・ダ行", "2-row-upper-cons-d" },
            { "五段・カ行促音便", "5-row-cons-k-cons-onbin" },
            { "一段・得ル", "1-row-eru" },
            { "四段・タ行", "4-row-cons-t" },
            { "五段・ナ行", "5-row-cons-n" },
            { "下二・ハ行", "2-row-lower-cons-h" },
            { "四段・ハ行", "4-row-cons-h" },
            { "四段・バ行", "4-row-cons-b" },
            { "サ変・スル", "irregular-suru" },
            { "上二・ハ行", "2-row-upper-cons-h" },
            { "下二・マ行", "2-row-lower-cons-m" },
            { "四段・サ行", "4-row-cons-s" },
            { "下二・ガ行", "2-row-lower-cons-g" },
            { "カ変・来ル", "kuru-kanji" },
            { "一段・クレル", "1-row-kureru" },
            { "下二・得", "2-row-lower-u" },
            { "カ変・クル", "kuru-kana" },
            { "ラ変", "irregular-cons-r" },
            { "下二・カ行", "2-row-lower-cons-k" },
        };


        /// <summary>
        /// Get the english form of inflection type
        /// </summary>
        public static string GetInflectionTypeTranslation(string s)
        {
            inflTypeTranslations.TryGetValue(s, out string result);
            return result;
        }

        // a translation map for inflection forms, only used for reflectWith
        private static readonly IDictionary<string, string> inflFormTranslations = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "*", "*" },
            { "基本形", "base" },
            { "文語基本形", "classical-base" },
            { "未然ヌ接続", "imperfective-nu-connection" },
            { "未然ウ接続", "imperfective-u-connection" },
            { "連用タ接続", "conjunctive-ta-connection" },
            { "連用テ接続", "conjunctive-te-connection" },
            { "連用ゴザイ接続", "conjunctive-gozai-connection" },
            { "体言接続", "uninflected-connection" },
            { "仮定形", "subjunctive" },
            { "命令ｅ", "imperative-e" },
            { "仮定縮約１", "conditional-contracted-1" },
            { "仮定縮約２", "conditional-contracted-2" },
            { "ガル接続", "garu-connection" },
            { "未然形", "imperfective" },
            { "連用形", "conjunctive" },
            { "音便基本形", "onbin-base" },
            { "連用デ接続", "conjunctive-de-connection" },
            { "未然特殊", "imperfective-special" },
            { "命令ｉ", "imperative-i" },
            { "連用ニ接続", "conjunctive-ni-connection" },
            { "命令ｙｏ", "imperative-yo" },
            { "体言接続特殊", "adnominal-special" },
            { "命令ｒｏ", "imperative-ro" },
            { "体言接続特殊２", "uninflected-special-connection-2" },
            { "未然レル接続", "imperfective-reru-connection" },
            { "現代基本形", "modern-base" },
            { "基本形-促音便", "base-onbin" }, // not sure about this
        };


        /// <summary>
        /// Get the english form of inflected form
        /// </summary>
        public static string GetInflectedFormTranslation(string s)
        {
            inflFormTranslations.TryGetValue(s, out string result);
            return result;
        }

        /// <summary>
        /// Romanize katakana with modified hepburn
        /// </summary>
        public static string GetRomanization(string s)
        {
            StringBuilder result = new StringBuilder();
            try
            {
                GetRomanization(result, s);
            }
            catch (Exception bogus) when (bogus.IsIOException())
            {
                throw RuntimeException.Create(bogus);
            }
            return result.ToString();
        }

        /// <summary>
        /// Romanize katakana with modified hepburn
        /// </summary>
        // TODO: now that this is used by readingsfilter and not just for
        // debugging, fix this to really be a scheme that works best with IMEs
        public static void GetRomanization(StringBuilder builder, string s)
        {
            int len = s.Length;
            for (int i = 0; i < len; i++)
            {
                // maximum lookahead: 3
                char ch = s[i];
                char ch2 = (i < len - 1) ? s[i + 1] : (char)0;
                char ch3 = (i < len - 2) ? s[i + 2] : (char)0;

                //main:
                switch (ch)
                {

                    case 'ッ':
                        switch (ch2)
                        {
                            case 'カ':
                            case 'キ':
                            case 'ク':
                            case 'ケ':
                            case 'コ':
                                builder.Append('k');
                                goto break_main;
                            case 'サ':
                            case 'シ':
                            case 'ス':
                            case 'セ':
                            case 'ソ':
                                builder.Append('s');
                                goto break_main;
                            case 'タ':
                            case 'チ':
                            case 'ツ':
                            case 'テ':
                            case 'ト':
                                builder.Append('t');
                                goto break_main;
                            case 'パ':
                            case 'ピ':
                            case 'プ':
                            case 'ペ':
                            case 'ポ':
                                builder.Append('p');
                                goto break_main;
                        }
                        break;
                    case 'ア':
                        builder.Append('a');
                        break;
                    case 'イ':
                        if (ch2 == 'ィ')
                        {
                            builder.Append("yi");
                            i++;
                        }
                        else if (ch2 == 'ェ')
                        {
                            builder.Append("ye");
                            i++;
                        }
                        else
                        {
                            builder.Append('i');
                        }
                        break;
                    case 'ウ':
                        switch (ch2)
                        {
                            case 'ァ':
                                builder.Append("wa");
                                i++;
                                break;
                            case 'ィ':
                                builder.Append("wi");
                                i++;
                                break;
                            case 'ゥ':
                                builder.Append("wu");
                                i++;
                                break;
                            case 'ェ':
                                builder.Append("we");
                                i++;
                                break;
                            case 'ォ':
                                builder.Append("wo");
                                i++;
                                break;
                            case 'ュ':
                                builder.Append("wyu");
                                i++;
                                break;
                            default:
                                builder.Append('u');
                                break;
                        }
                        break;
                    case 'エ':
                        builder.Append('e');
                        break;
                    case 'オ':
                        if (ch2 == 'ウ')
                        {
                            builder.Append('ō');
                            i++;
                        }
                        else
                        {
                            builder.Append('o');
                        }
                        break;
                    case 'カ':
                        builder.Append("ka");
                        break;
                    case 'キ':
                        if (ch2 == 'ョ' && ch3 == 'ウ')
                        {
                            builder.Append("kyō");
                            i += 2;
                        }
                        else if (ch2 == 'ュ' && ch3 == 'ウ')
                        {
                            builder.Append("kyū");
                            i += 2;
                        }
                        else if (ch2 == 'ャ')
                        {
                            builder.Append("kya");
                            i++;
                        }
                        else if (ch2 == 'ョ')
                        {
                            builder.Append("kyo");
                            i++;
                        }
                        else if (ch2 == 'ュ')
                        {
                            builder.Append("kyu");
                            i++;
                        }
                        else if (ch2 == 'ェ')
                        {
                            builder.Append("kye");
                            i++;
                        }
                        else
                        {
                            builder.Append("ki");
                        }
                        break;
                    case 'ク':
                        switch (ch2)
                        {
                            case 'ァ':
                                builder.Append("kwa");
                                i++;
                                break;
                            case 'ィ':
                                builder.Append("kwi");
                                i++;
                                break;
                            case 'ェ':
                                builder.Append("kwe");
                                i++;
                                break;
                            case 'ォ':
                                builder.Append("kwo");
                                i++;
                                break;
                            case 'ヮ':
                                builder.Append("kwa");
                                i++;
                                break;
                            default:
                                builder.Append("ku");
                                break;
                        }
                        break;
                    case 'ケ':
                        builder.Append("ke");
                        break;
                    case 'コ':
                        if (ch2 == 'ウ')
                        {
                            builder.Append("kō");
                            i++;
                        }
                        else
                        {
                            builder.Append("ko");
                        }
                        break;
                    case 'サ':
                        builder.Append("sa");
                        break;
                    case 'シ':
                        if (ch2 == 'ョ' && ch3 == 'ウ')
                        {
                            builder.Append("shō");
                            i += 2;
                        }
                        else if (ch2 == 'ュ' && ch3 == 'ウ')
                        {
                            builder.Append("shū");
                            i += 2;
                        }
                        else if (ch2 == 'ャ')
                        {
                            builder.Append("sha");
                            i++;
                        }
                        else if (ch2 == 'ョ')
                        {
                            builder.Append("sho");
                            i++;
                        }
                        else if (ch2 == 'ュ')
                        {
                            builder.Append("shu");
                            i++;
                        }
                        else if (ch2 == 'ェ')
                        {
                            builder.Append("she");
                            i++;
                        }
                        else
                        {
                            builder.Append("shi");
                        }
                        break;
                    case 'ス':
                        if (ch2 == 'ィ')
                        {
                            builder.Append("si");
                            i++;
                        }
                        else
                        {
                            builder.Append("su");
                        }
                        break;
                    case 'セ':
                        builder.Append("se");
                        break;
                    case 'ソ':
                        if (ch2 == 'ウ')
                        {
                            builder.Append("sō");
                            i++;
                        }
                        else
                        {
                            builder.Append("so");
                        }
                        break;
                    case 'タ':
                        builder.Append("ta");
                        break;
                    case 'チ':
                        if (ch2 == 'ョ' && ch3 == 'ウ')
                        {
                            builder.Append("chō");
                            i += 2;
                        }
                        else if (ch2 == 'ュ' && ch3 == 'ウ')
                        {
                            builder.Append("chū");
                            i += 2;
                        }
                        else if (ch2 == 'ャ')
                        {
                            builder.Append("cha");
                            i++;
                        }
                        else if (ch2 == 'ョ')
                        {
                            builder.Append("cho");
                            i++;
                        }
                        else if (ch2 == 'ュ')
                        {
                            builder.Append("chu");
                            i++;
                        }
                        else if (ch2 == 'ェ')
                        {
                            builder.Append("che");
                            i++;
                        }
                        else
                        {
                            builder.Append("chi");
                        }
                        break;
                    case 'ツ':
                        if (ch2 == 'ァ')
                        {
                            builder.Append("tsa");
                            i++;
                        }
                        else if (ch2 == 'ィ')
                        {
                            builder.Append("tsi");
                            i++;
                        }
                        else if (ch2 == 'ェ')
                        {
                            builder.Append("tse");
                            i++;
                        }
                        else if (ch2 == 'ォ')
                        {
                            builder.Append("tso");
                            i++;
                        }
                        else if (ch2 == 'ュ')
                        {
                            builder.Append("tsyu");
                            i++;
                        }
                        else
                        {
                            builder.Append("tsu");
                        }
                        break;
                    case 'テ':
                        if (ch2 == 'ィ')
                        {
                            builder.Append("ti");
                            i++;
                        }
                        else if (ch2 == 'ゥ')
                        {
                            builder.Append("tu");
                            i++;
                        }
                        else if (ch2 == 'ュ')
                        {
                            builder.Append("tyu");
                            i++;
                        }
                        else
                        {
                            builder.Append("te");
                        }
                        break;
                    case 'ト':
                        if (ch2 == 'ウ')
                        {
                            builder.Append("tō");
                            i++;
                        }
                        else if (ch2 == 'ゥ')
                        {
                            builder.Append("tu");
                            i++;
                        }
                        else
                        {
                            builder.Append("to");
                        }
                        break;
                    case 'ナ':
                        builder.Append("na");
                        break;
                    case 'ニ':
                        if (ch2 == 'ョ' && ch3 == 'ウ')
                        {
                            builder.Append("nyō");
                            i += 2;
                        }
                        else if (ch2 == 'ュ' && ch3 == 'ウ')
                        {
                            builder.Append("nyū");
                            i += 2;
                        }
                        else if (ch2 == 'ャ')
                        {
                            builder.Append("nya");
                            i++;
                        }
                        else if (ch2 == 'ョ')
                        {
                            builder.Append("nyo");
                            i++;
                        }
                        else if (ch2 == 'ュ')
                        {
                            builder.Append("nyu");
                            i++;
                        }
                        else if (ch2 == 'ェ')
                        {
                            builder.Append("nye");
                            i++;
                        }
                        else
                        {
                            builder.Append("ni");
                        }
                        break;
                    case 'ヌ':
                        builder.Append("nu");
                        break;
                    case 'ネ':
                        builder.Append("ne");
                        break;
                    case 'ノ':
                        if (ch2 == 'ウ')
                        {
                            builder.Append("nō");
                            i++;
                        }
                        else
                        {
                            builder.Append("no");
                        }
                        break;
                    case 'ハ':
                        builder.Append("ha");
                        break;
                    case 'ヒ':
                        if (ch2 == 'ョ' && ch3 == 'ウ')
                        {
                            builder.Append("hyō");
                            i += 2;
                        }
                        else if (ch2 == 'ュ' && ch3 == 'ウ')
                        {
                            builder.Append("hyū");
                            i += 2;
                        }
                        else if (ch2 == 'ャ')
                        {
                            builder.Append("hya");
                            i++;
                        }
                        else if (ch2 == 'ョ')
                        {
                            builder.Append("hyo");
                            i++;
                        }
                        else if (ch2 == 'ュ')
                        {
                            builder.Append("hyu");
                            i++;
                        }
                        else if (ch2 == 'ェ')
                        {
                            builder.Append("hye");
                            i++;
                        }
                        else
                        {
                            builder.Append("hi");
                        }
                        break;
                    case 'フ':
                        if (ch2 == 'ャ')
                        {
                            builder.Append("fya");
                            i++;
                        }
                        else if (ch2 == 'ュ')
                        {
                            builder.Append("fyu");
                            i++;
                        }
                        else if (ch2 == 'ィ' && ch3 == 'ェ')
                        {
                            builder.Append("fye");
                            i += 2;
                        }
                        else if (ch2 == 'ョ')
                        {
                            builder.Append("fyo");
                            i++;
                        }
                        else if (ch2 == 'ァ')
                        {
                            builder.Append("fa");
                            i++;
                        }
                        else if (ch2 == 'ィ')
                        {
                            builder.Append("fi");
                            i++;
                        }
                        else if (ch2 == 'ェ')
                        {
                            builder.Append("fe");
                            i++;
                        }
                        else if (ch2 == 'ォ')
                        {
                            builder.Append("fo");
                            i++;
                        }
                        else
                        {
                            builder.Append("fu");
                        }
                        break;
                    case 'ヘ':
                        builder.Append("he");
                        break;
                    case 'ホ':
                        if (ch2 == 'ウ')
                        {
                            builder.Append("hō");
                            i++;
                        }
                        else if (ch2 == 'ゥ')
                        {
                            builder.Append("hu");
                            i++;
                        }
                        else
                        {
                            builder.Append("ho");
                        }
                        break;
                    case 'マ':
                        builder.Append("ma");
                        break;
                    case 'ミ':
                        if (ch2 == 'ョ' && ch3 == 'ウ')
                        {
                            builder.Append("myō");
                            i += 2;
                        }
                        else if (ch2 == 'ュ' && ch3 == 'ウ')
                        {
                            builder.Append("myū");
                            i += 2;
                        }
                        else if (ch2 == 'ャ')
                        {
                            builder.Append("mya");
                            i++;
                        }
                        else if (ch2 == 'ョ')
                        {
                            builder.Append("myo");
                            i++;
                        }
                        else if (ch2 == 'ュ')
                        {
                            builder.Append("myu");
                            i++;
                        }
                        else if (ch2 == 'ェ')
                        {
                            builder.Append("mye");
                            i++;
                        }
                        else
                        {
                            builder.Append("mi");
                        }
                        break;
                    case 'ム':
                        builder.Append("mu");
                        break;
                    case 'メ':
                        builder.Append("me");
                        break;
                    case 'モ':
                        if (ch2 == 'ウ')
                        {
                            builder.Append("mō");
                            i++;
                        }
                        else
                        {
                            builder.Append("mo");
                        }
                        break;
                    case 'ヤ':
                        builder.Append("ya");
                        break;
                    case 'ユ':
                        builder.Append("yu");
                        break;
                    case 'ヨ':
                        if (ch2 == 'ウ')
                        {
                            builder.Append("yō");
                            i++;
                        }
                        else
                        {
                            builder.Append("yo");
                        }
                        break;
                    case 'ラ':
                        if (ch2 == '゜')
                        {
                            builder.Append("la");
                            i++;
                        }
                        else
                        {
                            builder.Append("ra");
                        }
                        break;
                    case 'リ':
                        if (ch2 == 'ョ' && ch3 == 'ウ')
                        {
                            builder.Append("ryō");
                            i += 2;
                        }
                        else if (ch2 == 'ュ' && ch3 == 'ウ')
                        {
                            builder.Append("ryū");
                            i += 2;
                        }
                        else if (ch2 == 'ャ')
                        {
                            builder.Append("rya");
                            i++;
                        }
                        else if (ch2 == 'ョ')
                        {
                            builder.Append("ryo");
                            i++;
                        }
                        else if (ch2 == 'ュ')
                        {
                            builder.Append("ryu");
                            i++;
                        }
                        else if (ch2 == 'ェ')
                        {
                            builder.Append("rye");
                            i++;
                        }
                        else if (ch2 == '゜')
                        {
                            builder.Append("li");
                            i++;
                        }
                        else
                        {
                            builder.Append("ri");
                        }
                        break;
                    case 'ル':
                        if (ch2 == '゜')
                        {
                            builder.Append("lu");
                            i++;
                        }
                        else
                        {
                            builder.Append("ru");
                        }
                        break;
                    case 'レ':
                        if (ch2 == '゜')
                        {
                            builder.Append("le");
                            i++;
                        }
                        else
                        {
                            builder.Append("re");
                        }
                        break;
                    case 'ロ':
                        if (ch2 == 'ウ')
                        {
                            builder.Append("rō");
                            i++;
                        }
                        else if (ch2 == '゜')
                        {
                            builder.Append("lo");
                            i++;
                        }
                        else
                        {
                            builder.Append("ro");
                        }
                        break;
                    case 'ワ':
                        builder.Append("wa");
                        break;
                    case 'ヰ':
                        builder.Append('i');
                        break;
                    case 'ヱ':
                        builder.Append('e');
                        break;
                    case 'ヲ':
                        builder.Append('o');
                        break;
                    case 'ン':
                        switch (ch2)
                        {
                            case 'バ':
                            case 'ビ':
                            case 'ブ':
                            case 'ベ':
                            case 'ボ':
                            case 'パ':
                            case 'ピ':
                            case 'プ':
                            case 'ペ':
                            case 'ポ':
                            case 'マ':
                            case 'ミ':
                            case 'ム':
                            case 'メ':
                            case 'モ':
                                builder.Append('m');
                                goto break_main;
                            case 'ヤ':
                            case 'ユ':
                            case 'ヨ':
                            case 'ア':
                            case 'イ':
                            case 'ウ':
                            case 'エ':
                            case 'オ':
                                builder.Append("n'");
                                goto break_main;
                            default:
                                builder.Append('n');
                                goto break_main;
                        }
                    case 'ガ':
                        builder.Append("ga");
                        break;
                    case 'ギ':
                        if (ch2 == 'ョ' && ch3 == 'ウ')
                        {
                            builder.Append("gyō");
                            i += 2;
                        }
                        else if (ch2 == 'ュ' && ch3 == 'ウ')
                        {
                            builder.Append("gyū");
                            i += 2;
                        }
                        else if (ch2 == 'ャ')
                        {
                            builder.Append("gya");
                            i++;
                        }
                        else if (ch2 == 'ョ')
                        {
                            builder.Append("gyo");
                            i++;
                        }
                        else if (ch2 == 'ュ')
                        {
                            builder.Append("gyu");
                            i++;
                        }
                        else if (ch2 == 'ェ')
                        {
                            builder.Append("gye");
                            i++;
                        }
                        else
                        {
                            builder.Append("gi");
                        }
                        break;
                    case 'グ':
                        switch (ch2)
                        {
                            case 'ァ':
                                builder.Append("gwa");
                                i++;
                                break;
                            case 'ィ':
                                builder.Append("gwi");
                                i++;
                                break;
                            case 'ェ':
                                builder.Append("gwe");
                                i++;
                                break;
                            case 'ォ':
                                builder.Append("gwo");
                                i++;
                                break;
                            case 'ヮ':
                                builder.Append("gwa");
                                i++;
                                break;
                            default:
                                builder.Append("gu");
                                break;
                        }
                        break;
                    case 'ゲ':
                        builder.Append("ge");
                        break;
                    case 'ゴ':
                        if (ch2 == 'ウ')
                        {
                            builder.Append("gō");
                            i++;
                        }
                        else
                        {
                            builder.Append("go");
                        }
                        break;
                    case 'ザ':
                        builder.Append("za");
                        break;
                    case 'ジ':
                        if (ch2 == 'ョ' && ch3 == 'ウ')
                        {
                            builder.Append("jō");
                            i += 2;
                        }
                        else if (ch2 == 'ュ' && ch3 == 'ウ')
                        {
                            builder.Append("jū");
                            i += 2;
                        }
                        else if (ch2 == 'ャ')
                        {
                            builder.Append("ja");
                            i++;
                        }
                        else if (ch2 == 'ョ')
                        {
                            builder.Append("jo");
                            i++;
                        }
                        else if (ch2 == 'ュ')
                        {
                            builder.Append("ju");
                            i++;
                        }
                        else if (ch2 == 'ェ')
                        {
                            builder.Append("je");
                            i++;
                        }
                        else
                        {
                            builder.Append("ji");
                        }
                        break;
                    case 'ズ':
                        if (ch2 == 'ィ')
                        {
                            builder.Append("zi");
                            i++;
                        }
                        else
                        {
                            builder.Append("zu");
                        }
                        break;
                    case 'ゼ':
                        builder.Append("ze");
                        break;
                    case 'ゾ':
                        if (ch2 == 'ウ')
                        {
                            builder.Append("zō");
                            i++;
                        }
                        else
                        {
                            builder.Append("zo");
                        }
                        break;
                    case 'ダ':
                        builder.Append("da");
                        break;
                    case 'ヂ':
                        // TODO: investigate all this
                        if (ch2 == 'ョ' && ch3 == 'ウ')
                        {
                            builder.Append("jō");
                            i += 2;
                        }
                        else if (ch2 == 'ュ' && ch3 == 'ウ')
                        {
                            builder.Append("jū");
                            i += 2;
                        }
                        else if (ch2 == 'ャ')
                        {
                            builder.Append("ja");
                            i++;
                        }
                        else if (ch2 == 'ョ')
                        {
                            builder.Append("jo");
                            i++;
                        }
                        else if (ch2 == 'ュ')
                        {
                            builder.Append("ju");
                            i++;
                        }
                        else if (ch2 == 'ェ')
                        {
                            builder.Append("je");
                            i++;
                        }
                        else
                        {
                            builder.Append("ji");
                        }
                        break;
                    case 'ヅ':
                        builder.Append("zu");
                        break;
                    case 'デ':
                        if (ch2 == 'ィ')
                        {
                            builder.Append("di");
                            i++;
                        }
                        else if (ch2 == 'ュ')
                        {
                            builder.Append("dyu");
                            i++;
                        }
                        else
                        {
                            builder.Append("de");
                        }
                        break;
                    case 'ド':
                        if (ch2 == 'ウ')
                        {
                            builder.Append("dō");
                            i++;
                        }
                        else if (ch2 == 'ゥ')
                        {
                            builder.Append("du");
                            i++;
                        }
                        else
                        {
                            builder.Append("do");
                        }
                        break;
                    case 'バ':
                        builder.Append("ba");
                        break;
                    case 'ビ':
                        if (ch2 == 'ョ' && ch3 == 'ウ')
                        {
                            builder.Append("byō");
                            i += 2;
                        }
                        else if (ch2 == 'ュ' && ch3 == 'ウ')
                        {
                            builder.Append("byū");
                            i += 2;
                        }
                        else if (ch2 == 'ャ')
                        {
                            builder.Append("bya");
                            i++;
                        }
                        else if (ch2 == 'ョ')
                        {
                            builder.Append("byo");
                            i++;
                        }
                        else if (ch2 == 'ュ')
                        {
                            builder.Append("byu");
                            i++;
                        }
                        else if (ch2 == 'ェ')
                        {
                            builder.Append("bye");
                            i++;
                        }
                        else
                        {
                            builder.Append("bi");
                        }
                        break;
                    case 'ブ':
                        builder.Append("bu");
                        break;
                    case 'ベ':
                        builder.Append("be");
                        break;
                    case 'ボ':
                        if (ch2 == 'ウ')
                        {
                            builder.Append("bō");
                            i++;
                        }
                        else
                        {
                            builder.Append("bo");
                        }
                        break;
                    case 'パ':
                        builder.Append("pa");
                        break;
                    case 'ピ':
                        if (ch2 == 'ョ' && ch3 == 'ウ')
                        {
                            builder.Append("pyō");
                            i += 2;
                        }
                        else if (ch2 == 'ュ' && ch3 == 'ウ')
                        {
                            builder.Append("pyū");
                            i += 2;
                        }
                        else if (ch2 == 'ャ')
                        {
                            builder.Append("pya");
                            i++;
                        }
                        else if (ch2 == 'ョ')
                        {
                            builder.Append("pyo");
                            i++;
                        }
                        else if (ch2 == 'ュ')
                        {
                            builder.Append("pyu");
                            i++;
                        }
                        else if (ch2 == 'ェ')
                        {
                            builder.Append("pye");
                            i++;
                        }
                        else
                        {
                            builder.Append("pi");
                        }
                        break;
                    case 'プ':
                        builder.Append("pu");
                        break;
                    case 'ペ':
                        builder.Append("pe");
                        break;
                    case 'ポ':
                        if (ch2 == 'ウ')
                        {
                            builder.Append("pō");
                            i++;
                        }
                        else
                        {
                            builder.Append("po");
                        }
                        break;
                    case 'ヷ':
                        builder.Append("va");
                        break;
                    case 'ヸ':
                        builder.Append("vi");
                        break;
                    case 'ヹ':
                        builder.Append("ve");
                        break;
                    case 'ヺ':
                        builder.Append("vo");
                        break;
                    case 'ヴ':
                        if (ch2 == 'ィ' && ch3 == 'ェ')
                        {
                            builder.Append("vye");
                            i += 2;
                        }
                        else
                        {
                            builder.Append('v');
                        }
                        break;
                    case 'ァ':
                        builder.Append('a');
                        break;
                    case 'ィ':
                        builder.Append('i');
                        break;
                    case 'ゥ':
                        builder.Append('u');
                        break;
                    case 'ェ':
                        builder.Append('e');
                        break;
                    case 'ォ':
                        builder.Append('o');
                        break;
                    case 'ヮ':
                        builder.Append("wa");
                        break;
                    case 'ャ':
                        builder.Append("ya");
                        break;
                    case 'ュ':
                        builder.Append("yu");
                        break;
                    case 'ョ':
                        builder.Append("yo");
                        break;
                    case 'ー':
                        break;
                    default:
                        builder.Append(ch);
                        break;
                }
                break_main: { }
            }
        }
    }
}
