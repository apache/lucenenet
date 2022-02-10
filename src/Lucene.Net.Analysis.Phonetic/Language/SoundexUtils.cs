// commons-codec version compatibility level: 1.9
using System;
using System.Globalization;

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
    /// Utility methods for <see cref="Soundex"/> and <see cref="RefinedSoundex"/> classes.
    /// <para/>
    /// This class is immutable and thread-safe.
    /// <para/>
    /// @since 1.3
    /// </summary>
    internal sealed class SoundexUtils
    {
        private static readonly CultureInfo LOCALE_ENGLISH = new CultureInfo("en");

        /// <summary>
        /// Cleans up the input string before Soundex processing by only returning
        /// upper case letters.
        /// </summary>
        /// <param name="str">The string to clean.</param>
        /// <returns>A clean string.</returns>
        public static string Clean(string str)
        {
            if (str is null || str.Length == 0)
            {
                return str;
            }
            int len = str.Length;
            char[] chars = new char[len];
            int count = 0;
            for (int i = 0; i < len; i++)
            {
                if (char.IsLetter(str[i]))
                {
                    chars[count++] = str[i];
                }
            }
            if (count == len)
            {
                return LOCALE_ENGLISH.TextInfo.ToUpper(str);
            }
            return LOCALE_ENGLISH.TextInfo.ToUpper(new string(chars, 0, count));
        }

        /// <summary>
        /// Encodes the Strings and returns the number of characters in the two
        /// encoded Strings that are the same.
        /// <list type="bullet">
        ///     <item><description>
        ///         For Soundex, this return value ranges from 0 through 4: 0 indicates
        ///         little or no similarity, and 4 indicates strong similarity or identical
        ///         values.
        ///     </description></item>
        ///     <item><description>For refined Soundex, the return value can be greater than 4.</description></item>
        /// </list>
        /// <para/>
        /// See: <a href="http://msdn.microsoft.com/library/default.asp?url=/library/en-us/tsqlref/ts_de-dz_8co5.asp">
        /// MS T-SQL DIFFERENCE</a>
        /// </summary>
        /// <param name="encoder">The encoder to use to encode the strings.</param>
        /// <param name="s1">A string that will be encoded and compared.</param>
        /// <param name="s2">A string that will be encoded and compared.</param>
        /// <returns>The number of characters in the two Soundex encoded strings that are the same.</returns>
        /// <seealso cref="DifferenceEncoded(string, string)"/>
        public static int Difference(IStringEncoder encoder, string s1, string s2)
        {
            return DifferenceEncoded(encoder.Encode(s1), encoder.Encode(s2));
        }

        /// <summary>
        /// Returns the number of characters in the two Soundex encoded strings that
        /// are the same.
        /// <list type="bullet">
        ///     <item><description>
        ///         For Soundex, this return value ranges from 0 through 4: 0 indicates
        ///         little or no similarity, and 4 indicates strong similarity or identical
        ///         values.
        ///     </description></item>
        ///     <item><description>For refined Soundex, the return value can be greater than 4.</description></item>
        /// </list>
        /// <para/>
        /// See: <a href="http://msdn.microsoft.com/library/default.asp?url=/library/en-us/tsqlref/ts_de-dz_8co5.asp">
        /// MS T-SQL DIFFERENCE</a>
        /// </summary>
        /// <param name="es1">An encoded string.</param>
        /// <param name="es2">An encoded string.</param>
        /// <returns>The number of characters in the two Soundex encoded strings that are the same.</returns>
        public static int DifferenceEncoded(string es1, string es2)
        {
            if (es1 is null || es2 is null)
            {
                return 0;
            }
            int lengthToMatch = Math.Min(es1.Length, es2.Length);
            int diff = 0;
            for (int i = 0; i < lengthToMatch; i++)
            {
                if (es1[i] == es2[i])
                {
                    diff++;
                }
            }
            return diff;
        }
    }
}
