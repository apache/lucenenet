// commons-codec version compatibility level: 1.9
using System;
using System.Globalization;
using System.Runtime.CompilerServices;
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
    /// Encodes a string into a double metaphone value. This Implementation is based on the algorithm by <c>Lawrence
    /// Philips</c>.
    /// <para/>
    /// This class is conditionally thread-safe. The instance field <see cref="maxCodeLen"/> is mutable
    /// <see cref="MaxCodeLen"/> but is not volatile, and accesses are not synchronized. If an instance of the class is
    /// shared between threads, the caller needs to ensure that suitable synchronization is used to ensure safe publication
    /// of the value between threads, and must not set <see cref="MaxCodeLen"/> after initial setup.
    /// <para/>
    /// See <a href="http://drdobbs.com/184401251?pgno=2">Original Article</a>
    /// <para/>
    /// See <a href="http://en.wikipedia.org/wiki/Metaphone">http://en.wikipedia.org/wiki/Metaphone</a>
    /// </summary>
    public class DoubleMetaphone : IStringEncoder
    {
        /// <summary>
        /// "Vowels" to test for
        /// </summary>
        private const string VOWELS = "AEIOUY";

        /// <summary>
        /// Prefixes when present which are not pronounced
        /// </summary>
        private static readonly string[] SILENT_START =
            { "GN", "KN", "PN", "WR", "PS" };
        private static readonly string[] L_R_N_M_B_H_F_V_W_SPACE =
            { "L", "R", "N", "M", "B", "H", "F", "V", "W", " " };
        private static readonly string[] ES_EP_EB_EL_EY_IB_IL_IN_IE_EI_ER =
            { "ES", "EP", "EB", "EL", "EY", "IB", "IL", "IN", "IE", "EI", "ER" };
        private static readonly string[] L_T_K_S_N_M_B_Z =
            { "L", "T", "K", "S", "N", "M", "B", "Z" };

        /// <summary>
        /// Maximum length of an encoding, default is 4
        /// </summary>
        private int maxCodeLen = 4;

        /// <summary>
        /// Creates an instance of this <see cref="DoubleMetaphone"/> encoder
        /// </summary>
        public DoubleMetaphone()
            : base()
        {
        }

        /// <summary>
        /// Encode a value with Double Metaphone.
        /// </summary>
        /// <param name="value">String to encode.</param>
        /// <returns>An encoded string.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual string GetDoubleMetaphone(string value)
        {
            return GetDoubleMetaphone(value, false);
        }

        /// <summary>
        /// Encode a value with Double Metaphone, optionally using the alternate encoding.
        /// </summary>
        /// <param name="value">String to encode.</param>
        /// <param name="alternate">Use alternate encode.</param>
        /// <returns>An encoded string.</returns>
        public virtual string GetDoubleMetaphone(string value, bool alternate)
        {
            value = CleanInput(value);
            if (value is null)
            {
                return null;
            }

            bool slavoGermanic = IsSlavoGermanic(value);
            int index = IsSilentStart(value) ? 1 : 0;

            DoubleMetaphoneResult result = new DoubleMetaphoneResult(this.MaxCodeLen);

            while (!result.IsComplete && index <= value.Length - 1)
            {
                switch (value[index])
                {
                    case 'A':
                    case 'E':
                    case 'I':
                    case 'O':
                    case 'U':
                    case 'Y':
                        index = HandleAEIOUY(result, index);
                        break;
                    case 'B':
                        result.Append('P');
                        index = CharAt(value, index + 1) == 'B' ? index + 2 : index + 1;
                        break;
                    case '\u00C7':
                        // A C with a Cedilla
                        result.Append('S');
                        index++;
                        break;
                    case 'C':
                        index = HandleC(value, result, index);
                        break;
                    case 'D':
                        index = HandleD(value, result, index);
                        break;
                    case 'F':
                        result.Append('F');
                        index = CharAt(value, index + 1) == 'F' ? index + 2 : index + 1;
                        break;
                    case 'G':
                        index = HandleG(value, result, index, slavoGermanic);
                        break;
                    case 'H':
                        index = HandleH(value, result, index);
                        break;
                    case 'J':
                        index = HandleJ(value, result, index, slavoGermanic);
                        break;
                    case 'K':
                        result.Append('K');
                        index = CharAt(value, index + 1) == 'K' ? index + 2 : index + 1;
                        break;
                    case 'L':
                        index = HandleL(value, result, index);
                        break;
                    case 'M':
                        result.Append('M');
                        index = ConditionM0(value, index) ? index + 2 : index + 1;
                        break;
                    case 'N':
                        result.Append('N');
                        index = CharAt(value, index + 1) == 'N' ? index + 2 : index + 1;
                        break;
                    case '\u00D1':
                        // N with a tilde (spanish ene)
                        result.Append('N');
                        index++;
                        break;
                    case 'P':
                        index = HandleP(value, result, index);
                        break;
                    case 'Q':
                        result.Append('K');
                        index = CharAt(value, index + 1) == 'Q' ? index + 2 : index + 1;
                        break;
                    case 'R':
                        index = HandleR(value, result, index, slavoGermanic);
                        break;
                    case 'S':
                        index = HandleS(value, result, index, slavoGermanic);
                        break;
                    case 'T':
                        index = HandleT(value, result, index);
                        break;
                    case 'V':
                        result.Append('F');
                        index = CharAt(value, index + 1) == 'V' ? index + 2 : index + 1;
                        break;
                    case 'W':
                        index = HandleW(value, result, index);
                        break;
                    case 'X':
                        index = HandleX(value, result, index);
                        break;
                    case 'Z':
                        index = HandleZ(value, result, index, slavoGermanic);
                        break;
                    default:
                        index++;
                        break;
                }
            }

            return alternate ? result.Alternate : result.Primary;
        }

        // LUCENENET specific - in .NET we don't need an object overload of Encode(), since strings are sealed anyway.

        /// <summary>
        /// Encode the value using DoubleMetaphone.
        /// </summary>
        /// <param name="value">String to encode.</param>
        /// <returns>An encoded string.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual string Encode(string value)
        {
            return GetDoubleMetaphone(value);
        }

        /// <summary>
        /// Check if the Double Metaphone values of two <see cref="string"/> values
        /// are equal.
        /// </summary>
        /// <param name="value1">The left-hand side of the encoded <see cref="string.Equals(object)"/>.</param>
        /// <param name="value2">The right-hand side of the encoded <see cref="string.Equals(object)"/>.</param>
        /// <returns><c>true</c> if the encoded <see cref="string"/>s are equal; <c>false</c> otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual bool IsDoubleMetaphoneEqual(string value1, string value2)
        {
            return IsDoubleMetaphoneEqual(value1, value2, false);
        }

        /// <summary>
        /// Check if the Double Metaphone values of two <see cref="string"/> values
        /// are equal, optionally using the alternate value.
        /// </summary>
        /// <param name="value1">The left-hand side of the encoded <see cref="string.Equals(object)"/>.</param>
        /// <param name="value2">The right-hand side of the encoded <see cref="string.Equals(object)"/>.</param>
        /// <param name="alternate">Use the alternate value if <c>true</c>.</param>
        /// <returns><c>true</c> if the encoded <see cref="string"/>s are equal; <c>false</c> otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual bool IsDoubleMetaphoneEqual(string value1, string value2, bool alternate)
        {
            return GetDoubleMetaphone(value1, alternate).Equals(GetDoubleMetaphone(value2, alternate), StringComparison.Ordinal);
        }

        /// <summary>
        /// Gets or Sets the maxCodeLen.
        /// </summary>
        public virtual int MaxCodeLen
        {
            get => this.maxCodeLen;
            set => this.maxCodeLen = value;
        }

        //-- BEGIN HANDLERS --//

        /// <summary>
        /// Handles 'A', 'E', 'I', 'O', 'U', and 'Y' cases.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int HandleAEIOUY(DoubleMetaphoneResult result, int index) // LUCENENET: CA1822: Mark members as static
        {
            if (index == 0)
            {
                result.Append('A');
            }
            return index + 1;
        }

        /// <summary>
        /// Handles 'C' cases.
        /// </summary>
        private int HandleC(string value, DoubleMetaphoneResult result, int index)
        {
            if (ConditionC0(value, index))
            {  // very confusing, moved out
                result.Append('K');
                index += 2;
            }
            else if (index == 0 && Contains(value, index, 6, "CAESAR"))
            {
                result.Append('S');
                index += 2;
            }
            else if (Contains(value, index, 2, "CH"))
            {
                index = HandleCH(value, result, index);
            }
            else if (Contains(value, index, 2, "CZ") &&
                     !Contains(value, index - 2, 4, "WICZ"))
            {
                //-- "Czerny" --//
                result.Append('S', 'X');
                index += 2;
            }
            else if (Contains(value, index + 1, 3, "CIA"))
            {
                //-- "focaccia" --//
                result.Append('X');
                index += 3;
            }
            else if (Contains(value, index, 2, "CC") &&
                     !(index == 1 && CharAt(value, 0) == 'M'))
            {
                //-- double "cc" but not "McClelland" --//
                return HandleCC(value, result, index);
            }
            else if (Contains(value, index, 2, "CK", "CG", "CQ"))
            {
                result.Append('K');
                index += 2;
            }
            else if (Contains(value, index, 2, "CI", "CE", "CY"))
            {
                //-- Italian vs. English --//
                if (Contains(value, index, 3, "CIO", "CIE", "CIA"))
                {
                    result.Append('S', 'X');
                }
                else
                {
                    result.Append('S');
                }
                index += 2;
            }
            else
            {
                result.Append('K');
                if (Contains(value, index + 1, 2, " C", " Q", " G"))
                {
                    //-- Mac Caffrey, Mac Gregor --//
                    index += 3;
                }
                else if (Contains(value, index + 1, 1, "C", "K", "Q") &&
                         !Contains(value, index + 1, 2, "CE", "CI"))
                {
                    index += 2;
                }
                else
                {
                    index++;
                }
            }

            return index;
        }

        /// <summary>
        /// Handles 'CC' cases.
        /// </summary>
        private int HandleCC(string value, DoubleMetaphoneResult result, int index)
        {
            if (Contains(value, index + 2, 1, "I", "E", "H") &&
                !Contains(value, index + 2, 2, "HU"))
            {
                //-- "bellocchio" but not "bacchus" --//
                if ((index == 1 && CharAt(value, index - 1) == 'A') ||
                    Contains(value, index - 1, 5, "UCCEE", "UCCES"))
                {
                    //-- "accident", "accede", "succeed" --//
                    result.Append("KS");
                }
                else
                {
                    //-- "bacci", "bertucci", other Italian --//
                    result.Append('X');
                }
                index += 3;
            }
            else
            {    // Pierce's rule
                result.Append('K');
                index += 2;
            }

            return index;
        }

        /// <summary>
        /// Handles 'CH' cases.
        /// </summary>
        private static int HandleCH(string value, DoubleMetaphoneResult result, int index) // LUCENENET: CA1822: Mark members as static
        {
            if (index > 0 && Contains(value, index, 4, "CHAE"))
            {   // Michael
                result.Append('K', 'X');
                return index + 2;
            }
            else if (ConditionCH0(value, index))
            {
                //-- Greek roots ("chemistry", "chorus", etc.) --//
                result.Append('K');
                return index + 2;
            }
            else if (ConditionCH1(value, index))
            {
                //-- Germanic, Greek, or otherwise 'ch' for 'kh' sound --//
                result.Append('K');
                return index + 2;
            }
            else
            {
                if (index > 0)
                {
                    if (Contains(value, 0, 2, "MC"))
                    {
                        result.Append('K');
                    }
                    else
                    {
                        result.Append('X', 'K');
                    }
                }
                else
                {
                    result.Append('X');
                }
                return index + 2;
            }
        }

        /// <summary>
        /// Handles 'D' cases.
        /// </summary>
        private static int HandleD(string value, DoubleMetaphoneResult result, int index) // LUCENENET: CA1822: Mark members as static
        {
            if (Contains(value, index, 2, "DG"))
            {
                //-- "Edge" --//
                if (Contains(value, index + 2, 1, "I", "E", "Y"))
                {
                    result.Append('J');
                    index += 3;
                    //-- "Edgar" --//
                }
                else
                {
                    result.Append("TK");
                    index += 2;
                }
            }
            else if (Contains(value, index, 2, "DT", "DD"))
            {
                result.Append('T');
                index += 2;
            }
            else
            {
                result.Append('T');
                index++;
            }
            return index;
        }

        /// <summary>
        /// Handles 'G' cases.
        /// </summary>
        private int HandleG(string value, DoubleMetaphoneResult result, int index,
                            bool slavoGermanic)
        {
            if (CharAt(value, index + 1) == 'H')
            {
                index = HandleGH(value, result, index);
            }
            else if (CharAt(value, index + 1) == 'N')
            {
                if (index == 1 && IsVowel(CharAt(value, 0)) && !slavoGermanic)
                {
                    result.Append("KN", "N");
                }
                else if (!Contains(value, index + 2, 2, "EY") &&
                         CharAt(value, index + 1) != 'Y' && !slavoGermanic)
                {
                    result.Append("N", "KN");
                }
                else
                {
                    result.Append("KN");
                }
                index = index + 2;
            }
            else if (Contains(value, index + 1, 2, "LI") && !slavoGermanic)
            {
                result.Append("KL", "L");
                index += 2;
            }
            else if (index == 0 &&
                     (CharAt(value, index + 1) == 'Y' ||
                      Contains(value, index + 1, 2, ES_EP_EB_EL_EY_IB_IL_IN_IE_EI_ER)))
            {
                //-- -ges-, -gep-, -gel-, -gie- at beginning --//
                result.Append('K', 'J');
                index += 2;
            }
            else if ((Contains(value, index + 1, 2, "ER") ||
                      CharAt(value, index + 1) == 'Y') &&
                     !Contains(value, 0, 6, "DANGER", "RANGER", "MANGER") &&
                     !Contains(value, index - 1, 1, "E", "I") &&
                     !Contains(value, index - 1, 3, "RGY", "OGY"))
            {
                //-- -ger-, -gy- --//
                result.Append('K', 'J');
                index += 2;
            }
            else if (Contains(value, index + 1, 1, "E", "I", "Y") ||
                     Contains(value, index - 1, 4, "AGGI", "OGGI"))
            {
                //-- Italian "biaggi" --//
                if (Contains(value, 0, 4, "VAN ", "VON ") ||
                    Contains(value, 0, 3, "SCH") ||
                    Contains(value, index + 1, 2, "ET"))
                {
                    //-- obvious germanic --//
                    result.Append('K');
                }
                else if (Contains(value, index + 1, 3, "IER"))
                {
                    result.Append('J');
                }
                else
                {
                    result.Append('J', 'K');
                }
                index += 2;
            }
            else if (CharAt(value, index + 1) == 'G')
            {
                index += 2;
                result.Append('K');
            }
            else
            {
                index++;
                result.Append('K');
            }
            return index;
        }

        /// <summary>
        /// Handles 'GH' cases.
        /// </summary>
        private int HandleGH(string value, DoubleMetaphoneResult result, int index)
        {
            if (index > 0 && !IsVowel(CharAt(value, index - 1)))
            {
                result.Append('K');
                index += 2;
            }
            else if (index == 0)
            {
                if (CharAt(value, index + 2) == 'I')
                {
                    result.Append('J');
                }
                else
                {
                    result.Append('K');
                }
                index += 2;
            }
            else if ((index > 1 && Contains(value, index - 2, 1, "B", "H", "D")) ||
                     (index > 2 && Contains(value, index - 3, 1, "B", "H", "D")) ||
                     (index > 3 && Contains(value, index - 4, 1, "B", "H")))
            {
                //-- Parker's rule (with some further refinements) - "hugh"
                index += 2;
            }
            else
            {
                if (index > 2 && CharAt(value, index - 1) == 'U' &&
                    Contains(value, index - 3, 1, "C", "G", "L", "R", "T"))
                {
                    //-- "laugh", "McLaughlin", "cough", "gough", "rough", "tough"
                    result.Append('F');
                }
                else if (index > 0 && CharAt(value, index - 1) != 'I')
                {
                    result.Append('K');
                }
                index += 2;
            }
            return index;
        }

        /// <summary>
        /// Handles 'H' cases.
        /// </summary>
        private int HandleH(string value, DoubleMetaphoneResult result, int index)
        {
            //-- only keep if first & before vowel or between 2 vowels --//
            if ((index == 0 || IsVowel(CharAt(value, index - 1))) &&
                IsVowel(CharAt(value, index + 1)))
            {
                result.Append('H');
                index += 2;
                //-- also takes car of "HH" --//
            }
            else
            {
                index++;
            }
            return index;
        }

        /// <summary>
        /// Handles 'J' cases.
        /// </summary>
        private int HandleJ(string value, DoubleMetaphoneResult result, int index,
                            bool slavoGermanic)
        {
            if (Contains(value, index, 4, "JOSE") || Contains(value, 0, 4, "SAN "))
            {
                //-- obvious Spanish, "Jose", "San Jacinto" --//
                if ((index == 0 && (CharAt(value, index + 4) == ' ') ||
                     value.Length == 4) || Contains(value, 0, 4, "SAN "))
                {
                    result.Append('H');
                }
                else
                {
                    result.Append('J', 'H');
                }
                index++;
            }
            else
            {
                if (index == 0 && !Contains(value, index, 4, "JOSE"))
                {
                    result.Append('J', 'A');
                }
                else if (IsVowel(CharAt(value, index - 1)) && !slavoGermanic &&
                         (CharAt(value, index + 1) == 'A' || CharAt(value, index + 1) == 'O'))
                {
                    result.Append('J', 'H');
                }
                else if (index == value.Length - 1)
                {
                    result.Append('J', ' ');
                }
                else if (!Contains(value, index + 1, 1, L_T_K_S_N_M_B_Z) &&
                         !Contains(value, index - 1, 1, "S", "K", "L"))
                {
                    result.Append('J');
                }

                if (CharAt(value, index + 1) == 'J')
                {
                    index += 2;
                }
                else
                {
                    index++;
                }
            }
            return index;
        }

        /// <summary>
        /// Handles 'L' cases.
        /// </summary>
        private int HandleL(string value, DoubleMetaphoneResult result, int index)
        {
            if (CharAt(value, index + 1) == 'L')
            {
                if (ConditionL0(value, index))
                {
                    result.AppendPrimary('L');
                }
                else
                {
                    result.Append('L');
                }
                index += 2;
            }
            else
            {
                index++;
                result.Append('L');
            }
            return index;
        }

        /// <summary>
        /// Handles 'P' cases.
        /// </summary>
        private int HandleP(string value, DoubleMetaphoneResult result, int index)
        {
            if (CharAt(value, index + 1) == 'H')
            {
                result.Append('F');
                index += 2;
            }
            else
            {
                result.Append('P');
                index = Contains(value, index + 1, 1, "P", "B") ? index + 2 : index + 1;
            }
            return index;
        }

        /// <summary>
        /// Handles 'R' cases.
        /// </summary>
        private int HandleR(string value, DoubleMetaphoneResult result, int index,
                            bool slavoGermanic)
        {
            if (index == value.Length - 1 && !slavoGermanic &&
                Contains(value, index - 2, 2, "IE") &&
                !Contains(value, index - 4, 2, "ME", "MA"))
            {
                result.AppendAlternate('R');
            }
            else
            {
                result.Append('R');
            }
            return CharAt(value, index + 1) == 'R' ? index + 2 : index + 1;
        }

        /// <summary>
        /// Handles 'S' cases.
        /// </summary>
        private int HandleS(string value, DoubleMetaphoneResult result, int index,
                            bool slavoGermanic)
        {
            if (Contains(value, index - 1, 3, "ISL", "YSL"))
            {
                //-- special cases "island", "isle", "carlisle", "carlysle" --//
                index++;
            }
            else if (index == 0 && Contains(value, index, 5, "SUGAR"))
            {
                //-- special case "sugar-" --//
                result.Append('X', 'S');
                index++;
            }
            else if (Contains(value, index, 2, "SH"))
            {
                if (Contains(value, index + 1, 4, "HEIM", "HOEK", "HOLM", "HOLZ"))
                {
                    //-- germanic --//
                    result.Append('S');
                }
                else
                {
                    result.Append('X');
                }
                index += 2;
            }
            else if (Contains(value, index, 3, "SIO", "SIA") || Contains(value, index, 4, "SIAN"))
            {
                //-- Italian and Armenian --//
                if (slavoGermanic)
                {
                    result.Append('S');
                }
                else
                {
                    result.Append('S', 'X');
                }
                index += 3;
            }
            else if ((index == 0 && Contains(value, index + 1, 1, "M", "N", "L", "W")) ||
                     Contains(value, index + 1, 1, "Z"))
            {
                //-- german & anglicisations, e.g. "smith" match "schmidt" //
                // "snider" match "schneider" --//
                //-- also, -sz- in slavic language although in hungarian it //
                //   is pronounced "s" --//
                result.Append('S', 'X');
                index = Contains(value, index + 1, 1, "Z") ? index + 2 : index + 1;
            }
            else if (Contains(value, index, 2, "SC"))
            {
                index = HandleSC(value, result, index);
            }
            else
            {
                if (index == value.Length - 1 && Contains(value, index - 2, 2, "AI", "OI"))
                {
                    //-- french e.g. "resnais", "artois" --//
                    result.AppendAlternate('S');
                }
                else
                {
                    result.Append('S');
                }
                index = Contains(value, index + 1, 1, "S", "Z") ? index + 2 : index + 1;
            }
            return index;
        }

        /// <summary>
        /// Handles 'SC' cases.
        /// </summary>
        private int HandleSC(string value, DoubleMetaphoneResult result, int index)
        {
            if (CharAt(value, index + 2) == 'H')
            {
                //-- Schlesinger's rule --//
                if (Contains(value, index + 3, 2, "OO", "ER", "EN", "UY", "ED", "EM"))
                {
                    //-- Dutch origin, e.g. "school", "schooner" --//
                    if (Contains(value, index + 3, 2, "ER", "EN"))
                    {
                        //-- "schermerhorn", "schenker" --//
                        result.Append("X", "SK");
                    }
                    else
                    {
                        result.Append("SK");
                    }
                }
                else
                {
                    if (index == 0 && !IsVowel(CharAt(value, 3)) && CharAt(value, 3) != 'W')
                    {
                        result.Append('X', 'S');
                    }
                    else
                    {
                        result.Append('X');
                    }
                }
            }
            else if (Contains(value, index + 2, 1, "I", "E", "Y"))
            {
                result.Append('S');
            }
            else
            {
                result.Append("SK");
            }
            return index + 3;
        }

        /// <summary>
        /// Handles 'T' cases.
        /// </summary>
        private static int HandleT(string value, DoubleMetaphoneResult result, int index) // LUCENENET: CA1822: Mark members as static
        {
            if (Contains(value, index, 4, "TION"))
            {
                result.Append('X');
                index += 3;
            }
            else if (Contains(value, index, 3, "TIA", "TCH"))
            {
                result.Append('X');
                index += 3;
            }
            else if (Contains(value, index, 2, "TH") || Contains(value, index, 3, "TTH"))
            {
                if (Contains(value, index + 2, 2, "OM", "AM") ||
                    //-- special case "thomas", "thames" or germanic --//
                    Contains(value, 0, 4, "VAN ", "VON ") ||
                    Contains(value, 0, 3, "SCH"))
                {
                    result.Append('T');
                }
                else
                {
                    result.Append('0', 'T');
                }
                index += 2;
            }
            else
            {
                result.Append('T');
                index = Contains(value, index + 1, 1, "T", "D") ? index + 2 : index + 1;
            }
            return index;
        }

        /// <summary>
        /// Handles 'W' cases.
        /// </summary>
        private int HandleW(string value, DoubleMetaphoneResult result, int index)
        {
            if (Contains(value, index, 2, "WR"))
            {
                //-- can also be in middle of word --//
                result.Append('R');
                index += 2;
            }
            else
            {
                if (index == 0 && (IsVowel(CharAt(value, index + 1)) ||
                                   Contains(value, index, 2, "WH")))
                {
                    if (IsVowel(CharAt(value, index + 1)))
                    {
                        //-- Wasserman should match Vasserman --//
                        result.Append('A', 'F');
                    }
                    else
                    {
                        //-- need Uomo to match Womo --//
                        result.Append('A');
                    }
                    index++;
                }
                else if ((index == value.Length - 1 && IsVowel(CharAt(value, index - 1))) ||
                         Contains(value, index - 1, 5, "EWSKI", "EWSKY", "OWSKI", "OWSKY") ||
                         Contains(value, 0, 3, "SCH"))
                {
                    //-- Arnow should match Arnoff --//
                    result.AppendAlternate('F');
                    index++;
                }
                else if (Contains(value, index, 4, "WICZ", "WITZ"))
                {
                    //-- Polish e.g. "filipowicz" --//
                    result.Append("TS", "FX");
                    index += 4;
                }
                else
                {
                    index++;
                }
            }
            return index;
        }

        /// <summary>
        /// Handles 'X' cases.
        /// </summary>
        private static int HandleX(string value, DoubleMetaphoneResult result, int index) // LUCENENET: CA1822: Mark members as static
        {
            if (index == 0)
            {
                result.Append('S');
                index++;
            }
            else
            {
                if (!((index == value.Length - 1) &&
                      (Contains(value, index - 3, 3, "IAU", "EAU") ||
                       Contains(value, index - 2, 2, "AU", "OU"))))
                {
                    //-- French e.g. breaux --//
                    result.Append("KS");
                }
                index = Contains(value, index + 1, 1, "C", "X") ? index + 2 : index + 1;
            }
            return index;
        }

        /// <summary>
        /// Handles 'Z' cases.
        /// </summary>
        private int HandleZ(string value, DoubleMetaphoneResult result, int index,
                            bool slavoGermanic)
        {
            if (CharAt(value, index + 1) == 'H')
            {
                //-- Chinese pinyin e.g. "zhao" or Angelina "Zhang" --//
                result.Append('J');
                index += 2;
            }
            else
            {
                if (Contains(value, index + 1, 2, "ZO", "ZI", "ZA") ||
                    (slavoGermanic && (index > 0 && CharAt(value, index - 1) != 'T')))
                {
                    result.Append("S", "TS");
                }
                else
                {
                    result.Append('S');
                }
                index = CharAt(value, index + 1) == 'Z' ? index + 2 : index + 1;
            }
            return index;
        }

        //-- BEGIN CONDITIONS --//

        /// <summary>
        /// Complex condition 0 for 'C'.
        /// </summary>
        private bool ConditionC0(string value, int index)
        {
            if (Contains(value, index, 4, "CHIA"))
            {
                return true;
            }
            else if (index <= 1)
            {
                return false;
            }
            else if (IsVowel(CharAt(value, index - 2)))
            {
                return false;
            }
            else if (!Contains(value, index - 1, 3, "ACH"))
            {
                return false;
            }
            else
            {
                char c = CharAt(value, index + 2);
                return (c != 'I' && c != 'E') ||
                        Contains(value, index - 2, 6, "BACHER", "MACHER");
            }
        }

        /// <summary>
        /// Complex condition 0 for 'CH'.
        /// </summary>
        private static bool ConditionCH0(string value, int index) // LUCENENET: CA1822: Mark members as static
        {
            if (index != 0)
            {
                return false;
            }
            else if (!Contains(value, index + 1, 5, "HARAC", "HARIS") &&
                     !Contains(value, index + 1, 3, "HOR", "HYM", "HIA", "HEM"))
            {
                return false;
            }
            else if (Contains(value, 0, 5, "CHORE"))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        /// <summary>
        /// Complex condition 1 for 'CH'.
        /// </summary>
        private static bool ConditionCH1(string value, int index) // LUCENENET: CA1822: Mark members as static
        {
            return ((Contains(value, 0, 4, "VAN ", "VON ") || Contains(value, 0, 3, "SCH")) ||
                    Contains(value, index - 2, 6, "ORCHES", "ARCHIT", "ORCHID") ||
                    Contains(value, index + 2, 1, "T", "S") ||
                    ((Contains(value, index - 1, 1, "A", "O", "U", "E") || index == 0) &&
                     (Contains(value, index + 2, 1, L_R_N_M_B_H_F_V_W_SPACE) || index + 1 == value.Length - 1)));
        }

        /// <summary>
        /// Complex condition 0 for 'L'.
        /// </summary>
        private static bool ConditionL0(string value, int index) // LUCENENET: CA1822: Mark members as static
        {
            if (index == value.Length - 3 &&
                Contains(value, index - 1, 4, "ILLO", "ILLA", "ALLE"))
            {
                return true;
            }
            else if ((Contains(value, value.Length - 2, 2, "AS", "OS") ||
                      Contains(value, value.Length - 1, 1, "A", "O")) &&
                     Contains(value, index - 1, 4, "ALLE"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Complex condition 0 for 'M'.
        /// </summary>
        private bool ConditionM0(string value, int index)
        {
            if (CharAt(value, index + 1) == 'M')
            {
                return true;
            }
            return Contains(value, index - 1, 3, "UMB") &&
                   ((index + 1) == value.Length - 1 || Contains(value, index + 2, 2, "ER"));
        }

        //-- BEGIN HELPER FUNCTIONS --//

        /// <summary>
        /// Determines whether or not a value is of slavo-germanic origin. A value is
        /// of slavo-germanic origin if it contians any of 'W', 'K', 'CZ', or 'WITZ'.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsSlavoGermanic(string value) // LUCENENET: CA1822: Mark members as static
        {
            return value.IndexOf('W') > -1 || value.IndexOf('K') > -1 ||
                value.IndexOf("CZ", StringComparison.Ordinal) > -1 || value.IndexOf("WITZ", StringComparison.Ordinal) > -1;
        }

        /// <summary>
        /// Determines whether or not a character is a vowel or not
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsVowel(char ch) // LUCENENET: CA1822: Mark members as static
        {
            return VOWELS.IndexOf(ch) != -1;
        }

        /// <summary>
        /// Determines whether or not the value starts with a silent letter.  It will
        /// return <c>true</c> if the value starts with any of 'GN', 'KN',
        /// 'PN', 'WR' or 'PS'.
        /// </summary>
        private static bool IsSilentStart(string value) // LUCENENET: CA1822: Mark members as static
        {
            bool result = false;
            foreach (string element in SILENT_START)
            {
                if (value.StartsWith(element, StringComparison.Ordinal))
                {
                    result = true;
                    break;
                }
            }
            return result;
        }

        private static readonly CultureInfo LOCALE_ENGLISH = new CultureInfo("en");

        /// <summary>
        /// Cleans the input.
        /// </summary>
        private static string CleanInput(string input) // LUCENENET: CA1822: Mark members as static
        {
            if (input is null)
            {
                return null;
            }
            input = input.Trim();
            if (input.Length == 0)
            {
                return null;
            }
            return LOCALE_ENGLISH.TextInfo.ToUpper(input);
        }

        /// <summary>
        /// Gets the character at index <paramref name="index"/> if available, otherwise
        /// it returns <see cref="char.MinValue"/> so that there is some sort
        /// of a default.
        /// </summary>
        protected virtual char CharAt(string value, int index)
        {
            if (index < 0 || index >= value.Length)
            {
                return char.MinValue;
            }
            return value[index];
        }

        /// <summary>
        /// Determines whether <paramref name="value"/> contains any of the criteria starting at index <paramref name="start"/> and
        /// matching up to length <paramref name="length"/>.
        /// </summary>
        protected static bool Contains(string value, int start, int length,
                                          params string[] criteria)
        {
            bool result = false;
            if (start >= 0 && start + length <= value.Length)
            {
                string target = value.Substring(start, length);

                foreach (string element in criteria)
                {
                    if (target.Equals(element, StringComparison.Ordinal))
                    {
                        result = true;
                        break;
                    }
                }
            }
            return result;
        }

        //-- BEGIN INNER CLASSES --//

        /// <summary>
        /// Inner class for storing results, since there is the optional alternate encoding.
        /// </summary>
        public class DoubleMetaphoneResult
        {
            private readonly StringBuilder primary;
            private readonly StringBuilder alternate;
            private readonly int maxLength;

            public DoubleMetaphoneResult(int maxLength)
            {
                this.maxLength = maxLength;
                this.primary = new StringBuilder(maxLength);
                this.alternate = new StringBuilder(maxLength);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public virtual void Append(char value)
            {
                AppendPrimary(value);
                AppendAlternate(value);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public virtual void Append(char primary, char alternate)
            {
                AppendPrimary(primary);
                AppendAlternate(alternate);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public virtual void AppendPrimary(char value)
            {
                if (this.primary.Length < this.maxLength)
                {
                    this.primary.Append(value);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public virtual void AppendAlternate(char value)
            {
                if (this.alternate.Length < this.maxLength)
                {
                    this.alternate.Append(value);
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public virtual void Append(string value)
            {
                AppendPrimary(value);
                AppendAlternate(value);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public virtual void Append(string primary, string alternate)
            {
                AppendPrimary(primary);
                AppendAlternate(alternate);
            }

            public virtual void AppendPrimary(string value)
            {
                int addChars = this.maxLength - this.primary.Length;
                if (value.Length <= addChars)
                {
                    this.primary.Append(value);
                }
                else
                {
#if FEATURE_STRINGBUILDER_APPEND_READONLYSPAN
                    this.primary.Append(value.AsSpan(0, addChars - 0));
#else
                    this.primary.Append(value.Substring(0, addChars - 0));
#endif
                }
            }

            public virtual void AppendAlternate(string value)
            {
                int addChars = this.maxLength - this.alternate.Length;
                if (value.Length <= addChars)
                {
                    this.alternate.Append(value);
                }
                else
                {
#if FEATURE_STRINGBUILDER_APPEND_READONLYSPAN
                    this.alternate.Append(value.AsSpan(0, addChars - 0));
#else
                    this.alternate.Append(value.Substring(0, addChars - 0));
#endif
                }
            }

            public virtual string Primary => this.primary.ToString();

            public virtual string Alternate => this.alternate.ToString();

            public virtual bool IsComplete =>
                this.primary.Length >= this.maxLength &&
                this.alternate.Length >= this.maxLength;
        }
    }
}
