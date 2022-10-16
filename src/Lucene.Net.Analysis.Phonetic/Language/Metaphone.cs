// commons-codec version compatibility level: 1.9
using System;
using System.Globalization;
using System.Text;

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
    /// Encodes a string into a Metaphone value.
    /// <para/>
    /// Initial Java implementation by <c>William B. Brogden. December, 1997</c>.
    /// Permission given by <c>wbrogden</c> for code to be used anywhere.
    /// <para/>
    /// <c>Hanging on the Metaphone</c> by <c>Lawrence Philips</c> in <c>Computer Language of Dec. 1990,
    /// p 39.</c>
    /// <para/>
    /// Note, that this does not match the algorithm that ships with PHP, or the algorithm found in the Perl implementations:
    /// <para/>
    /// <list type="bullet">
    ///     <item><description><a href="http://search.cpan.org/~mschwern/Text-Metaphone-1.96/Metaphone.pm">Text:Metaphone-1.96</a> (broken link 4/30/2013) </description></item>
    ///     <item><description><a href="https://metacpan.org/source/MSCHWERN/Text-Metaphone-1.96//Metaphone.pm">Text:Metaphone-1.96</a> (link checked 4/30/2013) </description></item>
    /// </list>
    /// <para/>
    /// They have had undocumented changes from the originally published algorithm.
    /// For more information, see <a href="https://issues.apache.org/jira/browse/CODEC-57">CODEC-57</a>.
    /// <para/>
    /// This class is conditionally thread-safe.
    /// The instance field <see cref="maxCodeLen"/> is mutable <see cref="MaxCodeLen"/>
    /// but is not volatile, and accesses are not synchronized.
    /// If an instance of the class is shared between threads, the caller needs to ensure that suitable synchronization
    /// is used to ensure safe publication of the value between threads, and must not set <see cref="MaxCodeLen"/>
    /// after initial setup.
    /// </summary>
    public class Metaphone : IStringEncoder
    {
        /// <summary>
        /// Five values in the English language
        /// </summary>
        private const string VOWELS = "AEIOU";

        /// <summary>
        /// Variable used in Metaphone algorithm
        /// </summary>
        private const string FRONTV = "EIY";

        /// <summary>
        /// Variable used in Metaphone algorithm
        /// </summary>
        private const string VARSON = "CSPTG";

        /// <summary>
        /// The max code length for metaphone is 4
        /// </summary>
        private int maxCodeLen = 4;

        /// <summary>
        /// Creates an instance of the <see cref="Metaphone"/> encoder
        /// </summary>
        public Metaphone()
            : base()
        {
        }

        private static readonly CultureInfo LOCALE_ENGLISH = new CultureInfo("en");

        /// <summary>
        /// Find the metaphone value of a string. This is similar to the
        /// soundex algorithm, but better at finding similar sounding words.
        /// All input is converted to upper case.
        /// Limitations: Input format is expected to be a single ASCII word
        /// with only characters in the A - Z range, no punctuation or numbers.
        /// </summary>
        /// <param name="txt">String to find the metaphone code for.</param>
        /// <returns>A metaphone code corresponding to the string supplied.</returns>
        public virtual string GetMetaphone(string txt)
        {
            bool hard; // LUCENENET: IDE0059: Remove unnecessary value assignment
            if (txt is null || txt.Length == 0)
            {
                return "";
            }
            // single character is itself
            if (txt.Length == 1)
            {
                return LOCALE_ENGLISH.TextInfo.ToUpper(txt);
            }

            char[] inwd = LOCALE_ENGLISH.TextInfo.ToUpper(txt).ToCharArray();

            StringBuilder local = new StringBuilder(40); // manipulate
            StringBuilder code = new StringBuilder(10); //   output
                                                        // handle initial 2 characters exceptions
            switch (inwd[0])
            {
                case 'K':
                case 'G':
                case 'P': /* looking for KN, etc*/
                    if (inwd[1] == 'N')
                    {
                        local.Append(inwd, 1, inwd.Length - 1);
                    }
                    else
                    {
                        local.Append(inwd);
                    }
                    break;
                case 'A': /* looking for AE */
                    if (inwd[1] == 'E')
                    {
                        local.Append(inwd, 1, inwd.Length - 1);
                    }
                    else
                    {
                        local.Append(inwd);
                    }
                    break;
                case 'W': /* looking for WR or WH */
                    if (inwd[1] == 'R')
                    {   // WR -> R
                        local.Append(inwd, 1, inwd.Length - 1);
                        break;
                    }
                    if (inwd[1] == 'H')
                    {
                        local.Append(inwd, 1, inwd.Length - 1);
                        local[0] = 'W'; // WH -> W
                    }
                    else
                    {
                        local.Append(inwd);
                    }
                    break;
                case 'X': /* initial X becomes S */
                    inwd[0] = 'S';
                    local.Append(inwd);
                    break;
                default:
                    local.Append(inwd);
                    break;
            } // now local has working string with initials fixed

            int wdsz = local.Length;
            int n = 0;

            while (code.Length < this.MaxCodeLen &&
                   n < wdsz)
            { // max code size of 4 works well
                char symb = local[n];
                // remove duplicate letters except C
                if (symb != 'C' && IsPreviousChar(local, n, symb))
                {
                    n++;
                }
                else
                { // not dup
                    switch (symb)
                    {
                        case 'A':
                        case 'E':
                        case 'I':
                        case 'O':
                        case 'U':
                            if (n == 0)
                            {
                                code.Append(symb);
                            }
                            break; // only use vowel if leading char
                        case 'B':
                            if (IsPreviousChar(local, n, 'M') &&
                                 IsLastChar(wdsz, n))
                            { // B is silent if word ends in MB
                                break;
                            }
                            code.Append(symb);
                            break;
                        case 'C': // lots of C special cases
                                  /* discard if SCI, SCE or SCY */
                            if (IsPreviousChar(local, n, 'S') &&
                                 !IsLastChar(wdsz, n) &&
                                 FRONTV.IndexOf(local[n + 1]) >= 0)
                            {
                                break;
                            }
                            if (RegionMatch(local, n, "CIA"))
                            { // "CIA" -> X
                                code.Append('X');
                                break;
                            }
                            if (!IsLastChar(wdsz, n) &&
                                FRONTV.IndexOf(local[n + 1]) >= 0)
                            {
                                code.Append('S');
                                break; // CI,CE,CY -> S
                            }
                            if (IsPreviousChar(local, n, 'S') &&
                                IsNextChar(local, n, 'H'))
                            { // SCH->sk
                                code.Append('K');
                                break;
                            }
                            if (IsNextChar(local, n, 'H'))
                            { // detect CH
                                if (n == 0 &&
                                    wdsz >= 3 &&
                                    IsVowel(local, 2))
                                { // CH consonant -> K consonant
                                    code.Append('K');
                                }
                                else
                                {
                                    code.Append('X'); // CHvowel -> X
                                }
                            }
                            else
                            {
                                code.Append('K');
                            }
                            break;
                        case 'D':
                            if (!IsLastChar(wdsz, n + 1) &&
                                IsNextChar(local, n, 'G') &&
                                FRONTV.IndexOf(local[n + 2]) >= 0)
                            { // DGE DGI DGY -> J
                                code.Append('J'); n += 2;
                            }
                            else
                            {
                                code.Append('T');
                            }
                            break;
                        case 'G': // GH silent at end or before consonant
                            if (IsLastChar(wdsz, n + 1) &&
                                IsNextChar(local, n, 'H'))
                            {
                                break;
                            }
                            if (!IsLastChar(wdsz, n + 1) &&
                                IsNextChar(local, n, 'H') &&
                                !IsVowel(local, n + 2))
                            {
                                break;
                            }
                            if (n > 0 &&
                                (RegionMatch(local, n, "GN") ||
                                  RegionMatch(local, n, "GNED")))
                            {
                                break; // silent G
                            }
                            if (IsPreviousChar(local, n, 'G'))
                            {
                                // NOTE: Given that duplicated chars are removed, I don't see how this can ever be true
                                hard = true;
                            }
                            else
                            {
                                hard = false;
                            }
                            if (!IsLastChar(wdsz, n) &&
                                FRONTV.IndexOf(local[n + 1]) >= 0 &&
                                !hard)
                            {
                                code.Append('J');
                            }
                            else
                            {
                                code.Append('K');
                            }
                            break;
                        case 'H':
                            if (IsLastChar(wdsz, n))
                            {
                                break; // terminal H
                            }
                            if (n > 0 &&
                                VARSON.IndexOf(local[n - 1]) >= 0)
                            {
                                break;
                            }
                            if (IsVowel(local, n + 1))
                            {
                                code.Append('H'); // Hvowel
                            }
                            break;
                        case 'F':
                        case 'J':
                        case 'L':
                        case 'M':
                        case 'N':
                        case 'R':
                            code.Append(symb);
                            break;
                        case 'K':
                            if (n > 0)
                            { // not initial
                                if (!IsPreviousChar(local, n, 'C'))
                                {
                                    code.Append(symb);
                                }
                            }
                            else
                            {
                                code.Append(symb); // initial K
                            }
                            break;
                        case 'P':
                            if (IsNextChar(local, n, 'H'))
                            {
                                // PH -> F
                                code.Append('F');
                            }
                            else
                            {
                                code.Append(symb);
                            }
                            break;
                        case 'Q':
                            code.Append('K');
                            break;
                        case 'S':
                            if (RegionMatch(local, n, "SH") ||
                                RegionMatch(local, n, "SIO") ||
                                RegionMatch(local, n, "SIA"))
                            {
                                code.Append('X');
                            }
                            else
                            {
                                code.Append('S');
                            }
                            break;
                        case 'T':
                            if (RegionMatch(local, n, "TIA") ||
                                RegionMatch(local, n, "TIO"))
                            {
                                code.Append('X');
                                break;
                            }
                            if (RegionMatch(local, n, "TCH"))
                            {
                                // Silent if in "TCH"
                                break;
                            }
                            // substitute numeral 0 for TH (resembles theta after all)
                            if (RegionMatch(local, n, "TH"))
                            {
                                code.Append('0');
                            }
                            else
                            {
                                code.Append('T');
                            }
                            break;
                        case 'V':
                            code.Append('F'); break;
                        case 'W':
                        case 'Y': // silent if not followed by vowel
                            if (!IsLastChar(wdsz, n) &&
                                IsVowel(local, n + 1))
                            {
                                code.Append(symb);
                            }
                            break;
                        case 'X':
                            code.Append('K');
                            code.Append('S');
                            break;
                        case 'Z':
                            code.Append('S');
                            break;
                        default:
                            // do nothing
                            break;
                    } // end switch
                    n++;
                } // end else from symb != 'C'
                if (code.Length > this.MaxCodeLen)
                {
                    code.Length = this.MaxCodeLen;
                }
            }
            return code.ToString();
        }

        private static bool IsVowel(StringBuilder sb, int index) // LUCENENET: CA1822: Mark members as static
        {
            return VOWELS.IndexOf(sb[index]) >= 0;
        }

        private static bool IsPreviousChar(StringBuilder sb, int index, char c) // LUCENENET: CA1822: Mark members as static
        {
            bool matches = false;
            if (index > 0 &&
                index < sb.Length)
            {
                matches = sb[index - 1] == c;
            }
            return matches;
        }

        private static bool IsNextChar(StringBuilder sb, int index, char c) // LUCENENET: CA1822: Mark members as static
        {
            bool matches = false;
            if (index >= 0 &&
                index < sb.Length - 1)
            {
                matches = sb[index + 1] == c;
            }
            return matches;
        }

        private static bool RegionMatch(StringBuilder sb, int index, string test) // LUCENENET: CA1822: Mark members as static
        {
            bool matches = false;
            if (index >= 0 &&
                index + test.Length - 1 < sb.Length)
            {
                string substring = sb.ToString(index, test.Length);
                matches = substring.Equals(test, StringComparison.Ordinal);
            }
            return matches;
        }

        private static bool IsLastChar(int wdsz, int n) // LUCENENET: CA1822: Mark members as static
        {
            return n + 1 == wdsz;
        }

        // LUCENENET specific - in .NET we don't need an object overload of Encode(), since strings are sealed anyway.

        /// <summary>
        /// Encodes a string using the <see cref="Metaphone"/> algorithm.
        /// </summary>
        /// <param name="str">String to encode.</param>
        /// <returns>The metaphone code corresponding to the string supplied.</returns>
        public virtual string Encode(string str)
        {
            return GetMetaphone(str);
        }

        /// <summary>
        /// Tests is the metaphones of two strings are identical.
        /// </summary>
        /// <param name="str1">First of two strings to compare.</param>
        /// <param name="str2">Second of two strings to compare.</param>
        /// <returns><c>true</c> if the metaphones of these strings are identical, <c>false</c> otherwise.</returns>
        public virtual bool IsMetaphoneEqual(string str1, string str2)
        {
            return GetMetaphone(str1).Equals(GetMetaphone(str2), StringComparison.Ordinal);
        }

        /// <summary>
        /// Gets or Sets <see cref="maxCodeLen"/>.
        /// </summary>
        public virtual int MaxCodeLen
        {
            get => this.maxCodeLen;
            set => this.maxCodeLen = value;
        }
    }
}
