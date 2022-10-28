// Lucene version compatibility level 4.8.1
using System;
using System.Globalization;

namespace Lucene.Net.Analysis.Br
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
    /// A stemmer for Brazilian Portuguese words.
    /// </summary>
    public class BrazilianStemmer
    {
        private static readonly CultureInfo locale = new CultureInfo("pt-BR");

        /// <summary>
        /// Changed term
        /// </summary>
        private string TERM;
        private string CT;
        private string R1;
        private string R2;
        private string RV;


        public BrazilianStemmer()
        {
        }

        /// <summary>
        /// Stems the given term to an unique <c>discriminator</c>.
        /// </summary>
        /// <param name="term">  The term that should be stemmed. </param>
        /// <returns>Discriminator for <paramref name="term"/></returns>
        protected internal virtual string Stem(string term)
        {
            bool altered = false; // altered the term

            // creates CT
            CreateCT(term);

            if (!IsIndexable(CT))
            {
                return null;
            }
            if (!IsStemmable(CT))
            {
                return CT;
            }

            R1 = GetR1(CT);
            R2 = GetR1(R1);
            RV = GetRV(CT);
            TERM = term + ";" + CT;

            altered = Step1();
            if (!altered)
            {
                altered = Step2();
            }

            if (altered)
            {
                Step3();
            }
            else
            {
                Step4();
            }

            Step5();

            return CT;
        }

        /// <summary>
        /// Checks a term if it can be processed correctly.
        /// </summary>
        /// <returns>  true if, and only if, the given term consists in letters. </returns>
        private static bool IsStemmable(string term) // LUCENENET: CA1822: Mark members as static
        {
            for (int c = 0; c < term.Length; c++)
            {
                // Discard terms that contain non-letter characters.
                if (!char.IsLetter(term[c]))
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Checks a term if it can be processed indexed.
        /// </summary>
        /// <returns> true if it can be indexed </returns>
        private static bool IsIndexable(string term) // LUCENENET: CA1822: Mark members as static
        {
            return (term.Length < 30) && (term.Length > 2);
        }

        /// <summary>
        /// See if string is 'a','e','i','o','u'
        /// </summary>
        /// <returns> true if is vowel </returns>
        private static bool IsVowel(char value) // LUCENENET: CA1822: Mark members as static
        {
            return (value == 'a') || (value == 'e') || (value == 'i') || (value == 'o') || (value == 'u');
        }

        /// <summary>
        /// Gets R1
        /// 
        /// R1 - is the region after the first non-vowel following a vowel,
        ///      or is the null region at the end of the word if there is
        ///      no such non-vowel.
        /// </summary>
        /// <returns> null or a string representing R1 </returns>
        private static string GetR1(string value) // LUCENENET: CA1822: Mark members as static
        {
            int i;
            int j;

            // be-safe !!!
            if (value is null)
            {
                return null;
            }

            // find 1st vowel
            i = value.Length - 1;
            for (j = 0; j < i; j++)
            {
                if (IsVowel(value[j]))
                {
                    break;
                }
            }

            if (!(j < i))
            {
                return null;
            }

            // find 1st non-vowel
            for (; j < i; j++)
            {
                if (!(IsVowel(value[j])))
                {
                    break;
                }
            }

            if (!(j < i))
            {
                return null;
            }

            return value.Substring(j + 1);
        }

        /// <summary>
        /// Gets RV
        /// 
        /// RV - IF the second letter is a consonant, RV is the region after
        ///      the next following vowel,
        /// 
        ///      OR if the first two letters are vowels, RV is the region
        ///      after the next consonant,
        /// 
        ///      AND otherwise (consonant-vowel case) RV is the region after
        ///      the third letter.
        /// 
        ///      BUT RV is the end of the word if this positions cannot be
        ///      found.
        /// </summary>
        /// <returns> null or a string representing RV </returns>
        private static string GetRV(string value) // LUCENENET: CA1822: Mark members as static
        {
            int i;
            int j;

            // be-safe !!!
            if (value is null)
            {
                return null;
            }

            i = value.Length - 1;

            // RV - IF the second letter is a consonant, RV is the region after
            //      the next following vowel,
            if ((i > 0) && !IsVowel(value[1]))
            {
                // find 1st vowel
                for (j = 2; j < i; j++)
                {
                    if (IsVowel(value[j]))
                    {
                        break;
                    }
                }

                if (j < i)
                {
                    return value.Substring(j + 1);
                }
            }


            // RV - OR if the first two letters are vowels, RV is the region
            //      after the next consonant,
            if ((i > 1) && IsVowel(value[0]) && IsVowel(value[1]))
            {
                // find 1st consoant
                for (j = 2; j < i; j++)
                {
                    if (!IsVowel(value[j]))
                    {
                        break;
                    }
                }

                if (j < i)
                {
                    return value.Substring(j + 1);
                }
            }

            // RV - AND otherwise (consonant-vowel case) RV is the region after
            //      the third letter.
            if (i > 2)
            {
                return value.Substring(3);
            }

            return null;
        }

        /// <summary>
        /// 1) Turn to lowercase
        /// 2) Remove accents
        /// 3) ã -> a ; õ -> o
        /// 4) ç -> c
        /// </summary>
        /// <returns> null or a string transformed </returns>
        private static string ChangeTerm(string value) // LUCENENET: CA1822: Mark members as static
        {
            int j;
            string r = "";

            // be-safe !!!
            if (value is null)
            {
                return null;
            }

            value = locale.TextInfo.ToLower(value);
            for (j = 0; j < value.Length; j++)
            {
                if ((value[j] == 'á') || (value[j] == 'â') || (value[j] == 'ã'))
                {
                    r = r + "a";
                    continue;
                }
                if ((value[j] == 'é') || (value[j] == 'ê'))
                {
                    r = r + "e";
                    continue;
                }
                if (value[j] == 'í')
                {
                    r = r + "i";
                    continue;
                }
                if ((value[j] == 'ó') || (value[j] == 'ô') || (value[j] == 'õ'))
                {
                    r = r + "o";
                    continue;
                }
                if ((value[j] == 'ú') || (value[j] == 'ü'))
                {
                    r = r + "u";
                    continue;
                }
                if (value[j] == 'ç')
                {
                    r = r + "c";
                    continue;
                }
                if (value[j] == 'ñ')
                {
                    r = r + "n";
                    continue;
                }

                r = r + value[j];
            }

            return r;
        }

        /// <summary>
        /// Check if a string ends with a suffix
        /// </summary>
        /// <returns> true if the string ends with the specified suffix </returns>
        private static bool Suffix(string value, string suffix) // LUCENENET: CA1822: Mark members as static
        {

            // be-safe !!!
            if ((value is null) || (suffix is null))
            {
                return false;
            }

            if (suffix.Length > value.Length)
            {
                return false;
            }

            return value.Substring(value.Length - suffix.Length).Equals(suffix, StringComparison.Ordinal);
        }

        /// <summary>
        /// Replace a <see cref="string"/> suffix by another
        /// </summary>
        /// <returns> the replaced <see cref="string"/> </returns>
        private static string ReplaceSuffix(string value, string toReplace, string changeTo) // LUCENENET: CA1822: Mark members as static
        {
            string vvalue;

            // be-safe !!!
            if ((value is null) || (toReplace is null) || (changeTo is null))
            {
                return value;
            }

            vvalue = RemoveSuffix(value, toReplace);

            if (value.Equals(vvalue, StringComparison.Ordinal))
            {
                return value;
            }
            else
            {
                return vvalue + changeTo;
            }
        }

        /// <summary>
        /// Remove a <see cref="string"/> suffix
        /// </summary>
        /// <returns> the <see cref="string"/> without the suffix </returns>
        private static string RemoveSuffix(string value, string toRemove) // LUCENENET: CA1822: Mark members as static
        {
            // be-safe !!!
            if ((value is null) || (toRemove is null) || !Suffix(value, toRemove))
            {
                return value;
            }

            return value.Substring(0, value.Length - toRemove.Length);
        }

        /// <summary>
        /// See if a suffix is preceded by a <see cref="string"/>
        /// </summary>
        /// <returns> true if the suffix is preceded </returns>
        private static bool SuffixPreceded(string value, string suffix, string preceded) // LUCENENET: CA1822: Mark members as static
        {
            // be-safe !!!
            if ((value is null) || (suffix is null) || (preceded is null) || !Suffix(value, suffix))
            {
                return false;
            }

            return Suffix(RemoveSuffix(value, suffix), preceded);
        }

        /// <summary>
        /// Creates CT (changed term) , substituting * 'ã' and 'õ' for 'a~' and 'o~'.
        /// </summary>
        private void CreateCT(string term)
        {
            CT = ChangeTerm(term);

            if (CT.Length < 2)
            {
                return;
            }

            // if the first character is ... , remove it
            if ((CT[0] == '"') || (CT[0] == '\'') || (CT[0] == '-') || (CT[0] == ',') || (CT[0] == ';') || (CT[0] == '.') || (CT[0] == '?') || (CT[0] == '!'))
            {
                CT = CT.Substring(1);
            }

            if (CT.Length < 2)
            {
                return;
            }

            // if the last character is ... , remove it
            if ((CT[CT.Length - 1] == '-') || (CT[CT.Length - 1] == ',') || (CT[CT.Length - 1] == ';') || (CT[CT.Length - 1] == '.') || (CT[CT.Length - 1] == '?') || (CT[CT.Length - 1] == '!') || (CT[CT.Length - 1] == '\'') || (CT[CT.Length - 1] == '"'))
            {
                CT = CT.Substring(0, CT.Length - 1);
            }
        }


        /// <summary>
        /// Standard suffix removal.
        /// Search for the longest among the following suffixes, and perform
        /// the following actions:
        /// </summary>
        /// <returns> false if no ending was removed </returns>
        private bool Step1()
        {
            if (CT is null)
            {
                return false;
            }

            // suffix length = 7
            if (Suffix(CT, "uciones") && Suffix(R2, "uciones"))
            {
                CT = ReplaceSuffix(CT, "uciones", "u");
                return true;
            }

            // suffix length = 6
            if (CT.Length >= 6)
            {
                if (Suffix(CT, "imentos") && Suffix(R2, "imentos"))
                {
                    CT = RemoveSuffix(CT, "imentos");
                    return true;
                }
                if (Suffix(CT, "amentos") && Suffix(R2, "amentos"))
                {
                    CT = RemoveSuffix(CT, "amentos");
                    return true;
                }
                if (Suffix(CT, "adores") && Suffix(R2, "adores"))
                {
                    CT = RemoveSuffix(CT, "adores");
                    return true;
                }
                if (Suffix(CT, "adoras") && Suffix(R2, "adoras"))
                {
                    CT = RemoveSuffix(CT, "adoras");
                    return true;
                }
                if (Suffix(CT, "logias") && Suffix(R2, "logias"))
                {
                    ReplaceSuffix(CT, "logias", "log");
                    return true;
                }
                if (Suffix(CT, "encias") && Suffix(R2, "encias"))
                {
                    CT = ReplaceSuffix(CT, "encias", "ente");
                    return true;
                }
                if (Suffix(CT, "amente") && Suffix(R1, "amente"))
                {
                    CT = RemoveSuffix(CT, "amente");
                    return true;
                }
                if (Suffix(CT, "idades") && Suffix(R2, "idades"))
                {
                    CT = RemoveSuffix(CT, "idades");
                    return true;
                }
            }

            // suffix length = 5
            if (CT.Length >= 5)
            {
                if (Suffix(CT, "acoes") && Suffix(R2, "acoes"))
                {
                    CT = RemoveSuffix(CT, "acoes");
                    return true;
                }
                if (Suffix(CT, "imento") && Suffix(R2, "imento"))
                {
                    CT = RemoveSuffix(CT, "imento");
                    return true;
                }
                if (Suffix(CT, "amento") && Suffix(R2, "amento"))
                {
                    CT = RemoveSuffix(CT, "amento");
                    return true;
                }
                if (Suffix(CT, "adora") && Suffix(R2, "adora"))
                {
                    CT = RemoveSuffix(CT, "adora");
                    return true;
                }
                if (Suffix(CT, "ismos") && Suffix(R2, "ismos"))
                {
                    CT = RemoveSuffix(CT, "ismos");
                    return true;
                }
                if (Suffix(CT, "istas") && Suffix(R2, "istas"))
                {
                    CT = RemoveSuffix(CT, "istas");
                    return true;
                }
                if (Suffix(CT, "logia") && Suffix(R2, "logia"))
                {
                    CT = ReplaceSuffix(CT, "logia", "log");
                    return true;
                }
                if (Suffix(CT, "ucion") && Suffix(R2, "ucion"))
                {
                    CT = ReplaceSuffix(CT, "ucion", "u");
                    return true;
                }
                if (Suffix(CT, "encia") && Suffix(R2, "encia"))
                {
                    CT = ReplaceSuffix(CT, "encia", "ente");
                    return true;
                }
                if (Suffix(CT, "mente") && Suffix(R2, "mente"))
                {
                    CT = RemoveSuffix(CT, "mente");
                    return true;
                }
                if (Suffix(CT, "idade") && Suffix(R2, "idade"))
                {
                    CT = RemoveSuffix(CT, "idade");
                    return true;
                }
            }

            // suffix length = 4
            if (CT.Length >= 4)
            {
                if (Suffix(CT, "acao") && Suffix(R2, "acao"))
                {
                    CT = RemoveSuffix(CT, "acao");
                    return true;
                }
                if (Suffix(CT, "ezas") && Suffix(R2, "ezas"))
                {
                    CT = RemoveSuffix(CT, "ezas");
                    return true;
                }
                if (Suffix(CT, "icos") && Suffix(R2, "icos"))
                {
                    CT = RemoveSuffix(CT, "icos");
                    return true;
                }
                if (Suffix(CT, "icas") && Suffix(R2, "icas"))
                {
                    CT = RemoveSuffix(CT, "icas");
                    return true;
                }
                if (Suffix(CT, "ismo") && Suffix(R2, "ismo"))
                {
                    CT = RemoveSuffix(CT, "ismo");
                    return true;
                }
                if (Suffix(CT, "avel") && Suffix(R2, "avel"))
                {
                    CT = RemoveSuffix(CT, "avel");
                    return true;
                }
                if (Suffix(CT, "ivel") && Suffix(R2, "ivel"))
                {
                    CT = RemoveSuffix(CT, "ivel");
                    return true;
                }
                if (Suffix(CT, "ista") && Suffix(R2, "ista"))
                {
                    CT = RemoveSuffix(CT, "ista");
                    return true;
                }
                if (Suffix(CT, "osos") && Suffix(R2, "osos"))
                {
                    CT = RemoveSuffix(CT, "osos");
                    return true;
                }
                if (Suffix(CT, "osas") && Suffix(R2, "osas"))
                {
                    CT = RemoveSuffix(CT, "osas");
                    return true;
                }
                if (Suffix(CT, "ador") && Suffix(R2, "ador"))
                {
                    CT = RemoveSuffix(CT, "ador");
                    return true;
                }
                if (Suffix(CT, "ivas") && Suffix(R2, "ivas"))
                {
                    CT = RemoveSuffix(CT, "ivas");
                    return true;
                }
                if (Suffix(CT, "ivos") && Suffix(R2, "ivos"))
                {
                    CT = RemoveSuffix(CT, "ivos");
                    return true;
                }
                if (Suffix(CT, "iras") && Suffix(RV, "iras") && SuffixPreceded(CT, "iras", "e"))
                {
                    CT = ReplaceSuffix(CT, "iras", "ir");
                    return true;
                }
            }

            // suffix length = 3
            if (CT.Length >= 3)
            {
                if (Suffix(CT, "eza") && Suffix(R2, "eza"))
                {
                    CT = RemoveSuffix(CT, "eza");
                    return true;
                }
                if (Suffix(CT, "ico") && Suffix(R2, "ico"))
                {
                    CT = RemoveSuffix(CT, "ico");
                    return true;
                }
                if (Suffix(CT, "ica") && Suffix(R2, "ica"))
                {
                    CT = RemoveSuffix(CT, "ica");
                    return true;
                }
                if (Suffix(CT, "oso") && Suffix(R2, "oso"))
                {
                    CT = RemoveSuffix(CT, "oso");
                    return true;
                }
                if (Suffix(CT, "osa") && Suffix(R2, "osa"))
                {
                    CT = RemoveSuffix(CT, "osa");
                    return true;
                }
                if (Suffix(CT, "iva") && Suffix(R2, "iva"))
                {
                    CT = RemoveSuffix(CT, "iva");
                    return true;
                }
                if (Suffix(CT, "ivo") && Suffix(R2, "ivo"))
                {
                    CT = RemoveSuffix(CT, "ivo");
                    return true;
                }
                if (Suffix(CT, "ira") && Suffix(RV, "ira") && SuffixPreceded(CT, "ira", "e"))
                {
                    CT = ReplaceSuffix(CT, "ira", "ir");
                    return true;
                }
            }

            // no ending was removed by step1
            return false;
        }


        /// <summary>
        /// Verb suffixes.
        /// 
        /// Search for the longest among the following suffixes in RV,
        /// and if found, delete.
        /// </summary>
        /// <returns> false if no ending was removed </returns>
        private bool Step2()
        {
            if (RV is null)
            {
                return false;
            }

            // suffix lenght = 7
            if (RV.Length >= 7)
            {
                if (Suffix(RV, "issemos"))
                {
                    CT = RemoveSuffix(CT, "issemos");
                    return true;
                }
                if (Suffix(RV, "essemos"))
                {
                    CT = RemoveSuffix(CT, "essemos");
                    return true;
                }
                if (Suffix(RV, "assemos"))
                {
                    CT = RemoveSuffix(CT, "assemos");
                    return true;
                }
                if (Suffix(RV, "ariamos"))
                {
                    CT = RemoveSuffix(CT, "ariamos");
                    return true;
                }
                if (Suffix(RV, "eriamos"))
                {
                    CT = RemoveSuffix(CT, "eriamos");
                    return true;
                }
                if (Suffix(RV, "iriamos"))
                {
                    CT = RemoveSuffix(CT, "iriamos");
                    return true;
                }
            }

            // suffix length = 6
            if (RV.Length >= 6)
            {
                if (Suffix(RV, "iremos"))
                {
                    CT = RemoveSuffix(CT, "iremos");
                    return true;
                }
                if (Suffix(RV, "eremos"))
                {
                    CT = RemoveSuffix(CT, "eremos");
                    return true;
                }
                if (Suffix(RV, "aremos"))
                {
                    CT = RemoveSuffix(CT, "aremos");
                    return true;
                }
                if (Suffix(RV, "avamos"))
                {
                    CT = RemoveSuffix(CT, "avamos");
                    return true;
                }
                if (Suffix(RV, "iramos"))
                {
                    CT = RemoveSuffix(CT, "iramos");
                    return true;
                }
                if (Suffix(RV, "eramos"))
                {
                    CT = RemoveSuffix(CT, "eramos");
                    return true;
                }
                if (Suffix(RV, "aramos"))
                {
                    CT = RemoveSuffix(CT, "aramos");
                    return true;
                }
                if (Suffix(RV, "asseis"))
                {
                    CT = RemoveSuffix(CT, "asseis");
                    return true;
                }
                if (Suffix(RV, "esseis"))
                {
                    CT = RemoveSuffix(CT, "esseis");
                    return true;
                }
                if (Suffix(RV, "isseis"))
                {
                    CT = RemoveSuffix(CT, "isseis");
                    return true;
                }
                if (Suffix(RV, "arieis"))
                {
                    CT = RemoveSuffix(CT, "arieis");
                    return true;
                }
                if (Suffix(RV, "erieis"))
                {
                    CT = RemoveSuffix(CT, "erieis");
                    return true;
                }
                if (Suffix(RV, "irieis"))
                {
                    CT = RemoveSuffix(CT, "irieis");
                    return true;
                }
            }


            // suffix length = 5
            if (RV.Length >= 5)
            {
                if (Suffix(RV, "irmos"))
                {
                    CT = RemoveSuffix(CT, "irmos");
                    return true;
                }
                if (Suffix(RV, "iamos"))
                {
                    CT = RemoveSuffix(CT, "iamos");
                    return true;
                }
                if (Suffix(RV, "armos"))
                {
                    CT = RemoveSuffix(CT, "armos");
                    return true;
                }
                if (Suffix(RV, "ermos"))
                {
                    CT = RemoveSuffix(CT, "ermos");
                    return true;
                }
                if (Suffix(RV, "areis"))
                {
                    CT = RemoveSuffix(CT, "areis");
                    return true;
                }
                if (Suffix(RV, "ereis"))
                {
                    CT = RemoveSuffix(CT, "ereis");
                    return true;
                }
                if (Suffix(RV, "ireis"))
                {
                    CT = RemoveSuffix(CT, "ireis");
                    return true;
                }
                if (Suffix(RV, "asses"))
                {
                    CT = RemoveSuffix(CT, "asses");
                    return true;
                }
                if (Suffix(RV, "esses"))
                {
                    CT = RemoveSuffix(CT, "esses");
                    return true;
                }
                if (Suffix(RV, "isses"))
                {
                    CT = RemoveSuffix(CT, "isses");
                    return true;
                }
                if (Suffix(RV, "astes"))
                {
                    CT = RemoveSuffix(CT, "astes");
                    return true;
                }
                if (Suffix(RV, "assem"))
                {
                    CT = RemoveSuffix(CT, "assem");
                    return true;
                }
                if (Suffix(RV, "essem"))
                {
                    CT = RemoveSuffix(CT, "essem");
                    return true;
                }
                if (Suffix(RV, "issem"))
                {
                    CT = RemoveSuffix(CT, "issem");
                    return true;
                }
                if (Suffix(RV, "ardes"))
                {
                    CT = RemoveSuffix(CT, "ardes");
                    return true;
                }
                if (Suffix(RV, "erdes"))
                {
                    CT = RemoveSuffix(CT, "erdes");
                    return true;
                }
                if (Suffix(RV, "irdes"))
                {
                    CT = RemoveSuffix(CT, "irdes");
                    return true;
                }
                if (Suffix(RV, "ariam"))
                {
                    CT = RemoveSuffix(CT, "ariam");
                    return true;
                }
                if (Suffix(RV, "eriam"))
                {
                    CT = RemoveSuffix(CT, "eriam");
                    return true;
                }
                if (Suffix(RV, "iriam"))
                {
                    CT = RemoveSuffix(CT, "iriam");
                    return true;
                }
                if (Suffix(RV, "arias"))
                {
                    CT = RemoveSuffix(CT, "arias");
                    return true;
                }
                if (Suffix(RV, "erias"))
                {
                    CT = RemoveSuffix(CT, "erias");
                    return true;
                }
                if (Suffix(RV, "irias"))
                {
                    CT = RemoveSuffix(CT, "irias");
                    return true;
                }
                if (Suffix(RV, "estes"))
                {
                    CT = RemoveSuffix(CT, "estes");
                    return true;
                }
                if (Suffix(RV, "istes"))
                {
                    CT = RemoveSuffix(CT, "istes");
                    return true;
                }
                if (Suffix(RV, "areis"))
                {
                    CT = RemoveSuffix(CT, "areis");
                    return true;
                }
                if (Suffix(RV, "aveis"))
                {
                    CT = RemoveSuffix(CT, "aveis");
                    return true;
                }
            }

            // suffix length = 4
            if (RV.Length >= 4)
            {
                if (Suffix(RV, "aria"))
                {
                    CT = RemoveSuffix(CT, "aria");
                    return true;
                }
                if (Suffix(RV, "eria"))
                {
                    CT = RemoveSuffix(CT, "eria");
                    return true;
                }
                if (Suffix(RV, "iria"))
                {
                    CT = RemoveSuffix(CT, "iria");
                    return true;
                }
                if (Suffix(RV, "asse"))
                {
                    CT = RemoveSuffix(CT, "asse");
                    return true;
                }
                if (Suffix(RV, "esse"))
                {
                    CT = RemoveSuffix(CT, "esse");
                    return true;
                }
                if (Suffix(RV, "isse"))
                {
                    CT = RemoveSuffix(CT, "isse");
                    return true;
                }
                if (Suffix(RV, "aste"))
                {
                    CT = RemoveSuffix(CT, "aste");
                    return true;
                }
                if (Suffix(RV, "este"))
                {
                    CT = RemoveSuffix(CT, "este");
                    return true;
                }
                if (Suffix(RV, "iste"))
                {
                    CT = RemoveSuffix(CT, "iste");
                    return true;
                }
                if (Suffix(RV, "arei"))
                {
                    CT = RemoveSuffix(CT, "arei");
                    return true;
                }
                if (Suffix(RV, "erei"))
                {
                    CT = RemoveSuffix(CT, "erei");
                    return true;
                }
                if (Suffix(RV, "irei"))
                {
                    CT = RemoveSuffix(CT, "irei");
                    return true;
                }
                if (Suffix(RV, "aram"))
                {
                    CT = RemoveSuffix(CT, "aram");
                    return true;
                }
                if (Suffix(RV, "eram"))
                {
                    CT = RemoveSuffix(CT, "eram");
                    return true;
                }
                if (Suffix(RV, "iram"))
                {
                    CT = RemoveSuffix(CT, "iram");
                    return true;
                }
                if (Suffix(RV, "avam"))
                {
                    CT = RemoveSuffix(CT, "avam");
                    return true;
                }
                if (Suffix(RV, "arem"))
                {
                    CT = RemoveSuffix(CT, "arem");
                    return true;
                }
                if (Suffix(RV, "erem"))
                {
                    CT = RemoveSuffix(CT, "erem");
                    return true;
                }
                if (Suffix(RV, "irem"))
                {
                    CT = RemoveSuffix(CT, "irem");
                    return true;
                }
                if (Suffix(RV, "ando"))
                {
                    CT = RemoveSuffix(CT, "ando");
                    return true;
                }
                if (Suffix(RV, "endo"))
                {
                    CT = RemoveSuffix(CT, "endo");
                    return true;
                }
                if (Suffix(RV, "indo"))
                {
                    CT = RemoveSuffix(CT, "indo");
                    return true;
                }
                if (Suffix(RV, "arao"))
                {
                    CT = RemoveSuffix(CT, "arao");
                    return true;
                }
                if (Suffix(RV, "erao"))
                {
                    CT = RemoveSuffix(CT, "erao");
                    return true;
                }
                if (Suffix(RV, "irao"))
                {
                    CT = RemoveSuffix(CT, "irao");
                    return true;
                }
                if (Suffix(RV, "adas"))
                {
                    CT = RemoveSuffix(CT, "adas");
                    return true;
                }
                if (Suffix(RV, "idas"))
                {
                    CT = RemoveSuffix(CT, "idas");
                    return true;
                }
                if (Suffix(RV, "aras"))
                {
                    CT = RemoveSuffix(CT, "aras");
                    return true;
                }
                if (Suffix(RV, "eras"))
                {
                    CT = RemoveSuffix(CT, "eras");
                    return true;
                }
                if (Suffix(RV, "iras"))
                {
                    CT = RemoveSuffix(CT, "iras");
                    return true;
                }
                if (Suffix(RV, "avas"))
                {
                    CT = RemoveSuffix(CT, "avas");
                    return true;
                }
                if (Suffix(RV, "ares"))
                {
                    CT = RemoveSuffix(CT, "ares");
                    return true;
                }
                if (Suffix(RV, "eres"))
                {
                    CT = RemoveSuffix(CT, "eres");
                    return true;
                }
                if (Suffix(RV, "ires"))
                {
                    CT = RemoveSuffix(CT, "ires");
                    return true;
                }
                if (Suffix(RV, "ados"))
                {
                    CT = RemoveSuffix(CT, "ados");
                    return true;
                }
                if (Suffix(RV, "idos"))
                {
                    CT = RemoveSuffix(CT, "idos");
                    return true;
                }
                if (Suffix(RV, "amos"))
                {
                    CT = RemoveSuffix(CT, "amos");
                    return true;
                }
                if (Suffix(RV, "emos"))
                {
                    CT = RemoveSuffix(CT, "emos");
                    return true;
                }
                if (Suffix(RV, "imos"))
                {
                    CT = RemoveSuffix(CT, "imos");
                    return true;
                }
                if (Suffix(RV, "iras"))
                {
                    CT = RemoveSuffix(CT, "iras");
                    return true;
                }
                if (Suffix(RV, "ieis"))
                {
                    CT = RemoveSuffix(CT, "ieis");
                    return true;
                }
            }

            // suffix length = 3
            if (RV.Length >= 3)
            {
                if (Suffix(RV, "ada"))
                {
                    CT = RemoveSuffix(CT, "ada");
                    return true;
                }
                if (Suffix(RV, "ida"))
                {
                    CT = RemoveSuffix(CT, "ida");
                    return true;
                }
                if (Suffix(RV, "ara"))
                {
                    CT = RemoveSuffix(CT, "ara");
                    return true;
                }
                if (Suffix(RV, "era"))
                {
                    CT = RemoveSuffix(CT, "era");
                    return true;
                }
                if (Suffix(RV, "ira"))
                {
                    CT = RemoveSuffix(CT, "ava");
                    return true;
                }
                if (Suffix(RV, "iam"))
                {
                    CT = RemoveSuffix(CT, "iam");
                    return true;
                }
                if (Suffix(RV, "ado"))
                {
                    CT = RemoveSuffix(CT, "ado");
                    return true;
                }
                if (Suffix(RV, "ido"))
                {
                    CT = RemoveSuffix(CT, "ido");
                    return true;
                }
                if (Suffix(RV, "ias"))
                {
                    CT = RemoveSuffix(CT, "ias");
                    return true;
                }
                if (Suffix(RV, "ais"))
                {
                    CT = RemoveSuffix(CT, "ais");
                    return true;
                }
                if (Suffix(RV, "eis"))
                {
                    CT = RemoveSuffix(CT, "eis");
                    return true;
                }
                if (Suffix(RV, "ira"))
                {
                    CT = RemoveSuffix(CT, "ira");
                    return true;
                }
                if (Suffix(RV, "ear"))
                {
                    CT = RemoveSuffix(CT, "ear");
                    return true;
                }
            }

            // suffix length = 2
            if (RV.Length >= 2)
            {
                if (Suffix(RV, "ia"))
                {
                    CT = RemoveSuffix(CT, "ia");
                    return true;
                }
                if (Suffix(RV, "ei"))
                {
                    CT = RemoveSuffix(CT, "ei");
                    return true;
                }
                if (Suffix(RV, "am"))
                {
                    CT = RemoveSuffix(CT, "am");
                    return true;
                }
                if (Suffix(RV, "em"))
                {
                    CT = RemoveSuffix(CT, "em");
                    return true;
                }
                if (Suffix(RV, "ar"))
                {
                    CT = RemoveSuffix(CT, "ar");
                    return true;
                }
                if (Suffix(RV, "er"))
                {
                    CT = RemoveSuffix(CT, "er");
                    return true;
                }
                if (Suffix(RV, "ir"))
                {
                    CT = RemoveSuffix(CT, "ir");
                    return true;
                }
                if (Suffix(RV, "as"))
                {
                    CT = RemoveSuffix(CT, "as");
                    return true;
                }
                if (Suffix(RV, "es"))
                {
                    CT = RemoveSuffix(CT, "es");
                    return true;
                }
                if (Suffix(RV, "is"))
                {
                    CT = RemoveSuffix(CT, "is");
                    return true;
                }
                if (Suffix(RV, "eu"))
                {
                    CT = RemoveSuffix(CT, "eu");
                    return true;
                }
                if (Suffix(RV, "iu"))
                {
                    CT = RemoveSuffix(CT, "iu");
                    return true;
                }
                if (Suffix(RV, "iu"))
                {
                    CT = RemoveSuffix(CT, "iu");
                    return true;
                }
                if (Suffix(RV, "ou"))
                {
                    CT = RemoveSuffix(CT, "ou");
                    return true;
                }
            }

            // no ending was removed by step2
            return false;
        }

        /// <summary>
        /// Delete suffix 'i' if in RV and preceded by 'c'
        /// </summary>
        private void Step3()
        {
            if (RV is null)
            {
                return;
            }

            if (Suffix(RV, "i") && SuffixPreceded(RV, "i", "c"))
            {
                CT = RemoveSuffix(CT, "i");
            }

        }

        /// <summary>
        /// Residual suffix
        /// 
        /// If the word ends with one of the suffixes (os a i o á í ó)
        /// in RV, delete it
        /// </summary>
        private void Step4()
        {
            if (RV is null)
            {
                return;
            }

            if (Suffix(RV, "os"))
            {
                CT = RemoveSuffix(CT, "os");
                return;
            }
            if (Suffix(RV, "a"))
            {
                CT = RemoveSuffix(CT, "a");
                return;
            }
            if (Suffix(RV, "i"))
            {
                CT = RemoveSuffix(CT, "i");
                return;
            }
            if (Suffix(RV, "o"))
            {
                CT = RemoveSuffix(CT, "o");
                //return; // LUCENENET: Removed redundant jump statements. https://rules.sonarsource.com/csharp/RSPEC-3626
            }

        }

        /// <summary>
        /// If the word ends with one of ( e é ê) in RV,delete it,
        /// and if preceded by 'gu' (or 'ci') with the 'u' (or 'i') in RV,
        /// delete the 'u' (or 'i')
        /// 
        /// Or if the word ends ç remove the cedilha
        /// </summary>
        private void Step5()
        {
            if (RV is null)
            {
                return;
            }

            if (Suffix(RV, "e"))
            {
                if (SuffixPreceded(RV, "e", "gu"))
                {
                    CT = RemoveSuffix(CT, "e");
                    CT = RemoveSuffix(CT, "u");
                    return;
                }

                if (SuffixPreceded(RV, "e", "ci"))
                {
                    CT = RemoveSuffix(CT, "e");
                    CT = RemoveSuffix(CT, "i");
                    return;
                }

                CT = RemoveSuffix(CT, "e");
                //return; // LUCENENET: Removed redundant jump statements. https://rules.sonarsource.com/csharp/RSPEC-3626
            }
        }

        /// <summary>
        /// For log and debug purpose
        /// </summary>
        /// <returns> TERM, CT, RV, R1 and R2 </returns>
        public virtual string Log()
        {
            return " (TERM = " + TERM + ")" + " (CT = " + CT + ")" + " (RV = " + RV + ")" + " (R1 = " + R1 + ")" + " (R2 = " + R2 + ")";
        }
    }
}