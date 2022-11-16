// commons-codec version compatibility level: 1.9
using Lucene.Net.Support;
using System;
using System.Text;
using System.Text.RegularExpressions;

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
    /// Encodes a string into a NYSIIS value. NYSIIS is an encoding used to relate similar names, but can also be used as a
    /// general purpose scheme to find word with similar phonemes.
    /// </summary>
    /// <remarks>
    /// NYSIIS features an accuracy increase of 2.7% over the traditional Soundex algorithm.
    /// <para/>
    /// Algorithm description:
    /// <list type="number">
    ///     <item>
    ///         <term>Transcode first characters of name</term>
    ///         <description>
    ///             <list type="number">
    ///                 <item><description>MAC ->   MCC</description></item>
    ///                 <item><description>KN  ->   NN</description></item>
    ///                 <item><description>K   ->   C</description></item>
    ///                 <item><description>PH  ->   FF</description></item>
    ///                 <item><description>PF  ->   FF</description></item>
    ///                 <item><description>SCH ->   SSS</description></item>
    ///             </list>
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <term>Transcode last characters of name</term>
    ///         <description>
    ///             <list type="number">
    ///                 <item><description>EE, IE          ->   Y</description></item>
    ///                 <item><description>DT,RT,RD,NT,ND  ->   D</description></item>
    ///             </list>
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <term>First character of key = first character of name</term>
    ///     </item>
    ///     <item>
    ///         <term>Transcode remaining characters by following these rules, incrementing by one character each time</term>
    ///         <description>
    ///             <list type="number">
    ///                 <item><description>EV  ->   AF  else A,E,I,O,U -> A</description></item>
    ///                 <item><description>Q   ->   G</description></item>
    ///                 <item><description>Z   ->   S</description></item>
    ///                 <item><description>M   ->   N</description></item>
    ///                 <item><description>KN  ->   N   else K -> C</description></item>
    ///                 <item><description>SCH ->   SSS</description></item>
    ///                 <item><description>PH  ->   FF</description></item>
    ///                 <item><description>H   ->   If previous or next is nonvowel, previous</description></item>
    ///                 <item><description>W   ->   If previous is vowel, previous</description></item>
    ///                 <item><description>Add current to key if current != last key character</description></item>
    ///             </list>
    ///         </description>
    ///     </item>
    ///     <item>
    ///         <term>If last character is S, remove it</term>
    ///     </item>
    ///     <item>
    ///         <term>If last characters are AY, replace with Y</term>
    ///     </item>
    ///     <item>
    ///         <term>If last character is A, remove it</term>
    ///     </item>
    ///     <item>
    ///         <term>Collapse all strings of repeated characters</term>
    ///     </item>
    ///     <item>
    ///         <term>Add original first character of name as first character of key</term>
    ///     </item>
    /// </list>
    /// <para/>
    /// This class is immutable and thread-safe.
    /// <para/>
    /// See: <a href="http://en.wikipedia.org/wiki/NYSIIS">NYSIIS on Wikipedia</a>
    /// <para/>
    /// See: <a href="http://www.dropby.com/NYSIIS.html">NYSIIS on dropby.com</a>
    /// <para/>
    /// since 1.7
    /// </remarks>
    /// <seealso cref="Soundex"/>
    public class Nysiis : IStringEncoder
    {
        private static readonly char[] CHARS_A = new char[] { 'A' };
        private static readonly char[] CHARS_AF = new char[] { 'A', 'F' };
        private static readonly char[] CHARS_C = new char[] { 'C' };
        private static readonly char[] CHARS_FF = new char[] { 'F', 'F' };
        private static readonly char[] CHARS_G = new char[] { 'G' };
        private static readonly char[] CHARS_N = new char[] { 'N' };
        private static readonly char[] CHARS_NN = new char[] { 'N', 'N' };
        private static readonly char[] CHARS_S = new char[] { 'S' };
        private static readonly char[] CHARS_SSS = new char[] { 'S', 'S', 'S' };

        private static readonly Regex PAT_MAC = new Regex("^MAC", RegexOptions.Compiled);
        private static readonly Regex PAT_KN = new Regex("^KN", RegexOptions.Compiled);
        private static readonly Regex PAT_K = new Regex("^K", RegexOptions.Compiled);
        private static readonly Regex PAT_PH_PF = new Regex("^(PH|PF)", RegexOptions.Compiled);
        private static readonly Regex PAT_SCH = new Regex("^SCH", RegexOptions.Compiled);
        private static readonly Regex PAT_EE_IE = new Regex("(EE|IE)$", RegexOptions.Compiled);
        private static readonly Regex PAT_DT_ETC = new Regex("(DT|RT|RD|NT|ND)$", RegexOptions.Compiled);

        private const char SPACE = ' ';
        private const int TRUE_LENGTH = 6;

        /// <summary>
        /// Tests if the given character is a vowel.
        /// </summary>
        /// <param name="c">The character to test.</param>
        /// <returns><c>true</c> if the character is a vowel, <c>false</c> otherwise.</returns>
        private static bool IsVowel(char c)
        {
            return c == 'A' || c == 'E' || c == 'I' || c == 'O' || c == 'U';
        }

        /// <summary>
        /// Transcodes the remaining parts of the string. The method operates on a sliding window, looking at 4 characters at
        /// a time: [i-1, i, i+1, i+2].
        /// </summary>
        /// <param name="prev">The previous character.</param>
        /// <param name="curr">The current character.</param>
        /// <param name="next">The next character.</param>
        /// <param name="aNext">The after next character</param>
        /// <returns>A transcoded array of characters, starting from the current position.</returns>
        private static char[] TranscodeRemaining(char prev, char curr, char next, char aNext)
        {
            // 1. EV -> AF
            if (curr == 'E' && next == 'V')
            {
                return CHARS_AF;
            }

            // A, E, I, O, U -> A
            if (IsVowel(curr))
            {
                return CHARS_A;
            }

            // 2. Q -> G, Z -> S, M -> N
            if (curr == 'Q')
            {
                return CHARS_G;
            }
            else if (curr == 'Z')
            {
                return CHARS_S;
            }
            else if (curr == 'M')
            {
                return CHARS_N;
            }

            // 3. KN -> NN else K -> C
            if (curr == 'K')
            {
                if (next == 'N')
                {
                    return CHARS_NN;
                }
                else
                {
                    return CHARS_C;
                }
            }

            // 4. SCH -> SSS
            if (curr == 'S' && next == 'C' && aNext == 'H')
            {
                return CHARS_SSS;
            }

            // PH -> FF
            if (curr == 'P' && next == 'H')
            {
                return CHARS_FF;
            }

            // 5. H -> If previous or next is a non vowel, previous.
            if (curr == 'H' && (!IsVowel(prev) || !IsVowel(next)))
            {
                return new char[] { prev };
            }

            // 6. W -> If previous is vowel, previous.
            if (curr == 'W' && IsVowel(prev))
            {
                return new char[] { prev };
            }

            return new char[] { curr };
        }

        /// <summary>Indicates the strict mode.</summary>
        private readonly bool strict;

        /// <summary>
        /// Creates an instance of the <see cref="Nysiis"/> encoder with strict mode (original form),
        /// i.e. encoded strings have a maximum length of 6.
        /// </summary>
        public Nysiis()
            : this(true)
        {
        }

        /// <summary>
        /// Create an instance of the <see cref="Nysiis"/> encoder with the specified strict mode:
        /// <list type="bullet">
        ///     <item><term><c>true</c>:</term><description>encoded strings have a maximum length of 6</description></item>
        ///     <item><term><c>false</c>:</term><description>encoded strings may have arbitrary length</description></item>
        /// </list>
        /// </summary>
        /// <param name="strict">The strict mode.</param>
        public Nysiis(bool strict)
        {
            this.strict = strict;
        }

        // LUCENENET specific - in .NET we don't need an object overload of Encode(), since strings are sealed anyway.

        /// <summary>
        /// Encodes a string using the NYSIIS algorithm.
        /// </summary>
        /// <param name="str">A string object to encode.</param>
        /// <returns>A <see cref="Nysiis"/> code corresponding to the string supplied.</returns>
        /// <exception cref="ArgumentException">If a character is not mapped.</exception>
        public virtual string Encode(string str)
        {
            return this.GetNysiis(str);
        }

        /// <summary>
        /// Indicates the strict mode for this <see cref="Nysiis"/> encoder.
        /// <c>true</c> if the encoder is configured for strict mode, <c>false</c> otherwise.
        /// </summary>
        public virtual bool IsStrict => this.strict;

        /// <summary>
        /// Retrieves the NYSIIS code for a given string.
        /// </summary>
        /// <param name="str">String to encode using the NYSIIS algorithm.</param>
        /// <returns>A NYSIIS code for the string supplied.</returns>
        public virtual string GetNysiis(string str)
        {
            if (str is null)
            {
                return null;
            }

            // Use the same clean rules as Soundex
            str = SoundexUtils.Clean(str);

            if (str.Length == 0)
            {
                return str;
            }

            // Translate first characters of name:
            // MAC -> MCC, KN -> NN, K -> C, PH | PF -> FF, SCH -> SSS
            str = PAT_MAC.Replace(str, "MCC", 1);
            str = PAT_KN.Replace(str, "NN", 1);
            str = PAT_K.Replace(str, "C", 1);
            str = PAT_PH_PF.Replace(str, "FF", 1);
            str = PAT_SCH.Replace(str, "SSS", 1);

            // Translate last characters of name:
            // EE -> Y, IE -> Y, DT | RT | RD | NT | ND -> D
            str = PAT_EE_IE.Replace(str, "Y", 1);
            str = PAT_DT_ETC.Replace(str, "D", 1);

            // First character of key = first character of name.
            StringBuilder key = new StringBuilder(str.Length);
            key.Append(str[0]);

            // Transcode remaining characters, incrementing by one character each time
            char[] chars = str.ToCharArray();
            int len = chars.Length;

            for (int i = 1; i < len; i++)
            {
                char next = i < len - 1 ? chars[i + 1] : SPACE;
                char aNext = i < len - 2 ? chars[i + 2] : SPACE;
                char[] transcoded = TranscodeRemaining(chars[i - 1], chars[i], next, aNext);
                Arrays.Copy(transcoded, 0, chars, i, transcoded.Length);

                // only append the current char to the key if it is different from the last one
                if (chars[i] != chars[i - 1])
                {
                    key.Append(chars[i]);
                }
            }

            if (key.Length > 1)
            {
                char lastChar = key[key.Length - 1];

                // If last character is S, remove it.
                if (lastChar == 'S')
                {
                    //key.deleteCharAt(key.length() - 1);
                    key.Remove(key.Length - 1, 1);
                    lastChar = key[key.Length - 1];
                }

                if (key.Length > 2)
                {
                    char last2Char = key[key.Length - 2];
                    // If last characters are AY, replace with Y.
                    if (last2Char == 'A' && lastChar == 'Y')
                    {
                        //.key.deleteCharAt(key.length() - 2);
                        key.Remove(key.Length - 2, 1);
                    }
                }

                // If last character is A, remove it.
                if (lastChar == 'A')
                {
                    //key.deleteCharAt(key.length() - 1);
                    key.Remove(key.Length - 1, 1);
                }
            }

            string result = key.ToString();
            return this.IsStrict ? result.Substring(0, Math.Min(TRUE_LENGTH, result.Length) - 0) : result;
        }
    }
}
