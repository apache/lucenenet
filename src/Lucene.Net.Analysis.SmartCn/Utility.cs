// lucene version compatibility level: 4.8.1
namespace Lucene.Net.Analysis.Cn.Smart
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
    /// <see cref="SmartChineseAnalyzer"/> utility constants and methods
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public static class Utility // LUCENENET specific: CA1052 Static holder types should be Static or NotInheritable
    {
        public static readonly char[] STRING_CHAR_ARRAY = "未##串".ToCharArray();

        public static readonly char[] NUMBER_CHAR_ARRAY = "未##数".ToCharArray();

        public static readonly char[] START_CHAR_ARRAY = "始##始".ToCharArray();

        public static readonly char[] END_CHAR_ARRAY = "末##末".ToCharArray();

        /// <summary>
        /// Delimiters will be filtered to this character by <see cref="Hhmm.SegTokenFilter"/>
        /// </summary>
        public static readonly char[] COMMON_DELIMITER = new char[] { ',' };

        /// <summary>
        /// Space-like characters that need to be skipped: such as space, tab, newline, carriage return.
        /// </summary>
        public static readonly string SPACES = " \u3000\t\r\n"; // LUCENENET specific - made the U+3000 character explicitly visible: https://sonarcloud.io/project/issues?resolved=false&rules=csharpsquid%3AS2479&id=nikcio_lucenenet

        /// <summary>
        /// Maximum bigram frequency (used in the smoothing function).
        /// </summary>
        public static readonly int MAX_FREQUENCE = 2079997 + 80000;

        /// <summary>
        /// Compare two arrays starting at the specified offsets.
        /// </summary>
        /// <param name="larray">left array</param>
        /// <param name="lstartIndex">start offset into <paramref name="larray"/></param>
        /// <param name="rarray">right array</param>
        /// <param name="rstartIndex">start offset into <paramref name="rarray"/></param>
        /// <returns>0 if the arrays are equal，1 if <paramref name="larray"/> &gt; 
        /// <paramref name="rarray"/>, -1 if <paramref name="larray"/> &lt; <paramref name="rarray"/></returns>
        public static int CompareArray(char[] larray, int lstartIndex, char[] rarray,
            int rstartIndex)
        {

            if (larray is null)
            {
                if (rarray is null || rstartIndex >= rarray.Length)
                    return 0;
                else
                    return -1;
            }
            else
            {
                // larray != null
                if (rarray is null)
                {
                    if (lstartIndex >= larray.Length)
                        return 0;
                    else
                        return 1;
                }
            }

            int li = lstartIndex, ri = rstartIndex;
            while (li < larray.Length && ri < rarray.Length && larray[li] == rarray[ri])
            {
                li++;
                ri++;
            }
            if (li == larray.Length)
            {
                if (ri == rarray.Length)
                {
                    // Both arrays are equivalent, return 0.
                    return 0;
                }
                else
                {
                    // larray < rarray because larray has ended first.
                    return -1;
                }
            }
            else
            {
                // differing lengths
                if (ri == rarray.Length)
                {
                    // larray > rarray because rarray has ended first.
                    return 1;
                }
                else
                {
                    // determine by comparison
                    if (larray[li] > rarray[ri])
                        return 1;
                    else
                        return -1;
                }
            }
        }

        /// <summary>
        /// Compare two arrays, starting at the specified offsets, but treating <paramref name="shortArray"/> as a prefix to <paramref name="longArray"/>.
        /// As long as <paramref name="shortArray"/> is a prefix of <paramref name="longArray"/>, return 0.
        /// Otherwise, behave as <see cref="CompareArray(char[], int, char[], int)"/>.
        /// </summary>
        /// <param name="shortArray">prefix array</param>
        /// <param name="shortIndex">offset into <paramref name="shortArray"/></param>
        /// <param name="longArray">long array (word)</param>
        /// <param name="longIndex">offset into <paramref name="longArray"/></param>
        /// <returns>0 if <paramref name="shortArray"/> is a prefix of <paramref name="longArray"/>, 
        /// otherwise act as <see cref="CompareArray(char[], int, char[], int)"/>.</returns>
        public static int CompareArrayByPrefix(char[] shortArray, int shortIndex,
            char[] longArray, int longIndex)
        {

            // a null prefix is a prefix of longArray
            if (shortArray is null)
                return 0;
            else if (longArray is null)
                return (shortIndex < shortArray.Length) ? 1 : 0;

            int si = shortIndex, li = longIndex;
            while (si < shortArray.Length && li < longArray.Length
                && shortArray[si] == longArray[li])
            {
                si++;
                li++;
            }
            if (si == shortArray.Length)
            {
                // shortArray is a prefix of longArray
                return 0;
            }
            else
            {
                // shortArray > longArray because longArray ended first.
                if (li == longArray.Length)
                    return 1;
                else
                    // determine by comparison
                    return (shortArray[si] > longArray[li]) ? 1 : -1;
            }
        }

        /// <summary>
        /// Return the internal <see cref="CharType"/> constant of a given character. 
        /// </summary>
        /// <param name="ch">input character</param>
        /// <returns>Constant from <see cref="CharType"/> describing the character type.</returns>
        /// <seealso cref="CharType"/>
        public static CharType GetCharType(char ch)
        {
            // Most (but not all!) of these are Han Ideographic Characters
            if (ch >= 0x4E00 && ch <= 0x9FA5)
                return CharType.HANZI;
            if ((ch >= 0x0041 && ch <= 0x005A) || (ch >= 0x0061 && ch <= 0x007A))
                return CharType.LETTER;
            if (ch >= 0x0030 && ch <= 0x0039)
                return CharType.DIGIT;
            if (ch == ' ' || ch == '\t' || ch == '\r' || ch == '\n' || ch == '　')
                return CharType.SPACE_LIKE;
            // Punctuation Marks
            if ((ch >= 0x0021 && ch <= 0x00BB) || (ch >= 0x2010 && ch <= 0x2642)
                || (ch >= 0x3001 && ch <= 0x301E))
                return CharType.DELIMITER;

            // Full-Width range
            if ((ch >= 0xFF21 && ch <= 0xFF3A) || (ch >= 0xFF41 && ch <= 0xFF5A))
                return CharType.FULLWIDTH_LETTER;
            if (ch >= 0xFF10 && ch <= 0xFF19)
                return CharType.FULLWIDTH_DIGIT;
            if (ch >= 0xFE30 && ch <= 0xFF63)
                return CharType.DELIMITER;
            return CharType.OTHER;
        }
    }
}
