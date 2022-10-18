// commons-codec version compatibility level: 1.9
using System;
using System.Diagnostics.CodeAnalysis;
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
    /// Match Rating Approach Phonetic Algorithm Developed by <c>Western Airlines</c> in 1977.
    /// <para/>
    /// This class is immutable and thread-safe.
    /// <para/>
    /// See: <a href="http://en.wikipedia.org/wiki/Match_rating_approach">Wikipedia - Match Rating Approach</a>
    /// <para/>
    /// since 1.8
    /// </summary>
    public class MatchRatingApproachEncoder : IStringEncoder
    {
        private const string SPACE = " ";

        private const string EMPTY = "";

        /// <summary>
        /// Constants used mainly for the min rating value.
        /// </summary>
        private const int ONE = 1, TWO = 2, THREE = 3, FOUR = 4, FIVE = 5, SIX = 6, SEVEN = 7, EIGHT = 8,
                                 ELEVEN = 11, TWELVE = 12;

        /// <summary>
        /// The plain letter equivalent of the accented letters.
        /// </summary>
        private const string PLAIN_ASCII = "AaEeIiOoUu" + // grave
            "AaEeIiOoUuYy" + // acute
            "AaEeIiOoUuYy" + // circumflex
            "AaOoNn" + // tilde
            "AaEeIiOoUuYy" + // umlaut
            "Aa" + // ring
            "Cc" + // cedilla
            "OoUu"; // double acute

        /// <summary>
        /// Unicode characters corresponding to various accented letters. For example: \u00DA is U acute etc...
        /// </summary>
        private const string UNICODE = "\u00C0\u00E0\u00C8\u00E8\u00CC\u00EC\u00D2\u00F2\u00D9\u00F9" +
                "\u00C1\u00E1\u00C9\u00E9\u00CD\u00ED\u00D3\u00F3\u00DA\u00FA\u00DD\u00FD" +
                "\u00C2\u00E2\u00CA\u00EA\u00CE\u00EE\u00D4\u00F4\u00DB\u00FB\u0176\u0177" +
                "\u00C3\u00E3\u00D5\u00F5\u00D1\u00F1" +
                "\u00C4\u00E4\u00CB\u00EB\u00CF\u00EF\u00D6\u00F6\u00DC\u00FC\u0178\u00FF" +
                "\u00C5\u00E5" + "\u00C7\u00E7" + "\u0150\u0151\u0170\u0171";

        private static readonly string[] DOUBLE_CONSONANT =
                new string[] { "BB", "CC", "DD", "FF", "GG", "HH", "JJ", "KK", "LL", "MM", "NN", "PP", "QQ", "RR", "SS",
                           "TT", "VV", "WW", "XX", "YY", "ZZ" };

        private static readonly CultureInfo LOCALE_ENGLISH = new CultureInfo("en");

        // LUCENENET: Use compiled invariant regexes and rollup all multi-char replacements for better performance
        // on short strings
        private static readonly Replacement WHITESPACE_REPLACEMENT = new Replacement("\\s+", EMPTY);
        private static readonly Replacement NAME_CHARS_REPLACEMENT = new Replacement("\\-|[&]|\\'|\\.|[\\,]", EMPTY);
        private static readonly Replacement VOWEL_REPLACEMENT = new Replacement("A|E|I|O|U", EMPTY);
        private static readonly Replacement VOWEL_WHITESPACE_REPLACEMENT = new Replacement("\\s{2,}\\b", SPACE);

        /// <summary>
        /// Cleans up a name: 1. Upper-cases everything 2. Removes some common punctuation 3. Removes accents 4. Removes any
        /// spaces.
        /// </summary>
        /// <param name="name">The name to be cleaned.</param>
        /// <returns>The cleaned name.</returns>
        [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "By design")]
        internal string CleanName(string name)
        {
            string upperName = LOCALE_ENGLISH.TextInfo.ToUpper(name);

            // LUCENENET: Optimized chars to trim for short names by putting them into a single
            // compiled regex that is statically cached
            upperName = NAME_CHARS_REPLACEMENT.Replace(upperName);

            upperName = RemoveAccents(upperName);
            upperName = WHITESPACE_REPLACEMENT.Replace(upperName);

            return upperName;
        }

        // LUCENENET specific - in .NET we don't need an object overload of Encode(), since strings are sealed anyway.

        /// <summary>
        /// Encodes a string using the Match Rating Approach (MRA) algorithm.
        /// </summary>
        /// <param name="name">String to encode.</param>
        /// <returns>The MRA code corresponding to the string supplied.</returns>
        public string Encode(string name)
        {
            // Bulletproof for trivial input - NINO
            if (name is null || EMPTY.Equals(name, StringComparison.OrdinalIgnoreCase) ||
                SPACE.Equals(name, StringComparison.OrdinalIgnoreCase) || name.Length == 1)
            {
                return EMPTY;
            }

            // Preprocessing
            name = CleanName(name);

            // BEGIN: Actual encoding part of the algorithm...
            // 1. Delete all vowels unless the vowel begins the word
            name = RemoveVowels(name);

            // 2. Remove second consonant from any double consonant
            name = RemoveDoubleConsonants(name);

            // 3. Reduce codex to 6 letters by joining the first 3 and last 3 letters
            name = GetFirst3Last3(name);

            return name;
        }

        /// <summary>
        /// Gets the first &amp; last 3 letters of a name (if &gt; 6 characters) Else just returns the name.
        /// </summary>
        /// <param name="name">The string to get the substrings from.</param>
        /// <returns>Annexed first &amp; last 3 letters of input word.</returns>
        [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "By design")]
        internal string GetFirst3Last3(string name)
        {
            int nameLength = name.Length;

            if (nameLength > SIX)
            {
                string firstThree = name.Substring(0, THREE - 0);
                string lastThree = name.Substring(nameLength - THREE, nameLength - (nameLength - THREE));
                return firstThree + lastThree;
            }
            else
            {
                return name;
            }
        }

        /// <summary>
        /// Obtains the min rating of the length sum of the 2 names. In essence the larger the sum length the smaller the
        /// min rating. Values strictly from documentation.
        /// </summary>
        /// <param name="sumLength">The length of 2 strings sent down.</param>
        /// <returns>The min rating value.</returns>
        [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "By design")]
        internal int GetMinRating(int sumLength)
        {
            int minRating; // LUCENENET: IDE0059: Remove unnecessary value assignment

            if (sumLength <= FOUR)
            {
                minRating = FIVE;
            }
            else if (sumLength >= FIVE && sumLength <= SEVEN)
            {
                minRating = FOUR;
            }
            else if (sumLength >= EIGHT && sumLength <= ELEVEN)
            {
                minRating = THREE;
            }
            else if (sumLength == TWELVE)
            {
                minRating = TWO;
            }
            else
            {
                minRating = ONE; // docs said little here.
            }

            return minRating;
        }

        /// <summary>
        /// Determines if two names are homophonous via Match Rating Approach (MRA) algorithm. It should be noted that the
        /// strings are cleaned in the same way as <see cref="Encode(string)"/>.
        /// </summary>
        /// <param name="name1">First of the 2 strings (names) to compare.</param>
        /// <param name="name2">Second of the 2 names to compare.</param>
        /// <returns><c>true</c> if the encodings are identical <c>false</c> otherwise.</returns>
        public virtual bool IsEncodeEquals(string name1, string name2)
        {
            // Bulletproof for trivial input - NINO
            if (name1 is null || EMPTY.Equals(name1, StringComparison.OrdinalIgnoreCase) || SPACE.Equals(name1, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            else if (name2 is null || EMPTY.Equals(name2, StringComparison.OrdinalIgnoreCase) || SPACE.Equals(name2, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            else if (name1.Length == 1 || name2.Length == 1)
            {
                return false;
            }
            else if (name1.Equals(name2, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            // Preprocessing
            name1 = CleanName(name1);
            name2 = CleanName(name2);

            // Actual MRA Algorithm

            // 1. Remove vowels
            name1 = RemoveVowels(name1);
            name2 = RemoveVowels(name2);

            // 2. Remove double consonants
            name1 = RemoveDoubleConsonants(name1);
            name2 = RemoveDoubleConsonants(name2);

            // 3. Reduce down to 3 letters
            name1 = GetFirst3Last3(name1);
            name2 = GetFirst3Last3(name2);

            // 4. Check for length difference - if 3 or greater then no similarity
            // comparison is done
            if (Math.Abs(name1.Length - name2.Length) >= THREE)
            {
                return false;
            }

            // 5. Obtain the minimum rating value by calculating the length sum of the
            // encoded strings and sending it down.
            int sumLength = Math.Abs(name1.Length + name2.Length);
            int minRating; // LUCENENET: IDE0059: Remove unnecessary value assignment
            minRating = GetMinRating(sumLength);

            // 6. Process the encoded strings from left to right and remove any
            // identical characters found from both strings respectively.
            int count = LeftToRightThenRightToLeftProcessing(name1, name2);

            // 7. Each PNI item that has a similarity rating equal to or greater than
            // the min is considered to be a good candidate match
            return count >= minRating;

        }

        /// <summary>
        /// Determines if a letter is a vowel.
        /// </summary>
        /// <param name="letter">The letter under investiagtion.</param>
        /// <returns><c>true</c> if a vowel, else <c>false</c>.</returns>
        internal static bool IsVowel(string letter) // LUCENENET: CA1822: Mark members as static
        {
            return letter.Equals("E", StringComparison.OrdinalIgnoreCase) || letter.Equals("A", StringComparison.OrdinalIgnoreCase) || letter.Equals("O", StringComparison.OrdinalIgnoreCase) ||
                   letter.Equals("I", StringComparison.OrdinalIgnoreCase) || letter.Equals("U", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Processes the names from left to right (first) then right to left removing identical letters in same positions.
        /// Then subtracts the longer string that remains from 6 and returns this.
        /// </summary>
        /// <param name="name1"></param>
        /// <param name="name2"></param>
        /// <returns></returns>
        [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "By design")]
        internal int LeftToRightThenRightToLeftProcessing(string name1, string name2)
        {
            char[] name1Char = name1.ToCharArray();
            char[] name2Char = name2.ToCharArray();

            int name1Size = name1.Length - 1;
            int name2Size = name2.Length - 1;

            string name1LtRStart/* = EMPTY*/; // LUCENENET: IDE0059: Remove unnecessary value assignment
            string name1LtREnd/* = EMPTY*/; // LUCENENET: IDE0059: Remove unnecessary value assignment

            string name2RtLStart/* = EMPTY*/; // LUCENENET: IDE0059: Remove unnecessary value assignment
            string name2RtLEnd/* = EMPTY*/; // LUCENENET: IDE0059: Remove unnecessary value assignment

            for (int i = 0; i < name1Char.Length; i++)
            {
                if (i > name2Size)
                {
                    break;
                }

                name1LtRStart = name1.Substring(i, 1);
                name1LtREnd = name1.Substring(name1Size - i, 1);

                name2RtLStart = name2.Substring(i, 1);
                name2RtLEnd = name2.Substring(name2Size - i, 1);

                // Left to right...
                if (name1LtRStart.Equals(name2RtLStart, StringComparison.Ordinal))
                {
                    name1Char[i] = ' ';
                    name2Char[i] = ' ';
                }

                // Right to left...
                if (name1LtREnd.Equals(name2RtLEnd, StringComparison.Ordinal))
                {
                    name1Char[name1Size - i] = ' ';
                    name2Char[name2Size - i] = ' ';
                }
            }

            // Char arrays -> string & remove extraneous space
            string strA = WHITESPACE_REPLACEMENT.Replace(new string(name1Char));
            string strB = WHITESPACE_REPLACEMENT.Replace(new string(name2Char));

            // Final bit - subtract longest string from 6 and return this int value
            if (strA.Length > strB.Length)
            {
                return Math.Abs(SIX - strA.Length);
            }
            else
            {
                return Math.Abs(SIX - strB.Length);
            }
        }

        /// <summary>
        /// Removes accented letters and replaces with non-accented ascii equivalent Case is preserved.
        /// http://www.codecodex.com/wiki/Remove_accent_from_letters_%28ex_.%C3%A9_to_e%29
        /// </summary>
        /// <param name="accentedWord">The word that may have accents in it.</param>
        /// <returns>De-accented word.</returns>
        internal static string RemoveAccents(string accentedWord) // LUCENENET: CA1822: Mark members as static
        {
            if (accentedWord is null)
            {
                return null;
            }

            StringBuilder sb = new StringBuilder();
            int n = accentedWord.Length;

            for (int i = 0; i < n; i++)
            {
                char c = accentedWord[i];
                int pos = UNICODE.IndexOf(c);
                if (pos > -1)
                {
                    sb.Append(PLAIN_ASCII[pos]);
                }
                else
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Replaces any double consonant pair with the single letter equivalent.
        /// </summary>
        /// <param name="name">String to have double consonants removed.</param>
        /// <returns>Single consonant word.</returns>
        [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "By design")]
        internal string RemoveDoubleConsonants(string name)
        {
            string replacedName = name.ToUpperInvariant();
            foreach (string dc in DOUBLE_CONSONANT)
            {
                if (replacedName.Contains(dc))
                {
                    string singleLetter = dc.Substring(0, 1 - 0);
                    replacedName = replacedName.Replace(dc, singleLetter);
                }
            }
            return replacedName;
        }

        /// <summary>
        /// Deletes all vowels unless the vowel begins the word.
        /// </summary>
        /// <param name="name">The name to have vowels removed.</param>
        /// <returns>De-voweled word.</returns>
        [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "By design")]
        internal string RemoveVowels(string name)
        {
            // Extract first letter
            string firstLetter = name.Substring(0, 1 - 0);

            // LUCENENET specific - Optimized for short names by doing
            // alteration in a single compiled statically cached regex
            name = VOWEL_REPLACEMENT.Replace(name);
            name = VOWEL_WHITESPACE_REPLACEMENT.Replace(name);

            // return isVowel(firstLetter) ? (firstLetter + name) : name;
            if (IsVowel(firstLetter))
            {
                return firstLetter + name;
            }
            else
            {
                return name;
            }
        }
    }
}
