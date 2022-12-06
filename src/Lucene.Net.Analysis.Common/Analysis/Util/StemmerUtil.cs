// Lucene version compatibility level 4.8.1
using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using System;
using System.Diagnostics;

namespace Lucene.Net.Analysis.Util
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
    /// Some commonly-used stemming functions
    /// 
    /// @lucene.internal
    /// </summary>
    public static class StemmerUtil // LUCENENET specific: CA1052 Static holder types should be Static or NotInheritable
    {
        /// <summary>
        /// Returns true if the character array starts with the prefix.
        /// </summary>
        /// <param name="s"> Input Buffer </param>
        /// <param name="len"> length of input buffer </param>
        /// <param name="prefix"> Prefix string to test </param>
        /// <returns> <c>true</c> if <paramref name="s"/> starts with <paramref name="prefix"/> </returns>
        public static bool StartsWith(char[] s, int len, string prefix)
        {
            int prefixLen = prefix.Length;
            if (prefixLen > len)
            {
                return false;
            }
            for (int i = 0; i < prefixLen; i++)
            {
                if (s[i] != prefix[i])
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Returns true if the character array ends with the suffix.
        /// </summary>
        /// <param name="s"> Input Buffer </param>
        /// <param name="len"> length of input buffer </param>
        /// <param name="suffix"> Suffix string to test </param>
        /// <returns> <c>true</c> if <paramref name="s"/> ends with <paramref name="suffix"/> </returns>
        public static bool EndsWith(char[] s, int len, string suffix)
        {
            int suffixLen = suffix.Length;
            if (suffixLen > len)
            {
                return false;
            }
            for (int i = suffixLen - 1; i >= 0; i--)
            {
                if (s[len - (suffixLen - i)] != suffix[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Returns true if the character array ends with the suffix.
        /// </summary>
        /// <param name="s"> Input Buffer </param>
        /// <param name="len"> length of input buffer </param>
        /// <param name="suffix"> Suffix string to test </param>
        /// <returns> <c>true</c> if <paramref name="s"/> ends with <paramref name="suffix"/> </returns>
        public static bool EndsWith(char[] s, int len, char[] suffix)
        {
            int suffixLen = suffix.Length;
            if (suffixLen > len)
            {
                return false;
            }
            for (int i = suffixLen - 1; i >= 0; i--)
            {
                if (s[len - (suffixLen - i)] != suffix[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Delete a character in-place
        /// </summary>
        /// <param name="s"> Input Buffer </param>
        /// <param name="pos"> Position of character to delete </param>
        /// <param name="len"> length of input buffer </param>
        /// <returns> length of input buffer after deletion </returns>
        public static int Delete(char[] s, int pos, int len)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(pos < len);
            if (pos < len - 1) // don't arraycopy if asked to delete last character
            {
                Arrays.Copy(s, pos + 1, s, pos, len - pos - 1);
            }
            return len - 1;
        }

        /// <summary>
        /// Delete n characters in-place
        /// </summary>
        /// <param name="s"> Input Buffer </param>
        /// <param name="pos"> Position of character to delete </param>
        /// <param name="len"> Length of input buffer </param>
        /// <param name="nChars"> number of characters to delete </param>
        /// <returns> length of input buffer after deletion </returns>
        public static int DeleteN(char[] s, int pos, int len, int nChars)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(pos + nChars <= len);
            if (pos + nChars < len) // don't arraycopy if asked to delete the last characters
            {
                Arrays.Copy(s, pos + nChars, s, pos, len - pos - nChars);
            }
            return len - nChars;
        }
    }
}