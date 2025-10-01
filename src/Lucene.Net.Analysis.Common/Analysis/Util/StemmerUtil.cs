// Lucene version compatibility level 4.8.1
using Lucene.Net.Diagnostics;
using System;

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
        /// <param name="prefix"> Prefix string to test </param>
        /// <returns> <c>true</c> if <paramref name="s"/> starts with <paramref name="prefix"/> </returns>
        /// <remarks>
        /// LUCENENET NOTE: This method has been converted to use <see cref="ReadOnlySpan{T}"/>.
        /// </remarks>
        public static bool StartsWith(ReadOnlySpan<char> s, string prefix)
        {
            return StartsWith(s, prefix.AsSpan());
        }

        /// <summary>
        /// Returns true if the character array starts with the prefix.
        /// </summary>
        /// <param name="s"> Input Buffer </param>
        /// <param name="len"> length of input buffer </param>
        /// <param name="prefix"> Prefix string to test </param>
        /// <returns> <c>true</c> if <paramref name="s"/> starts with <paramref name="prefix"/> </returns>
        /// <remarks>
        /// LUCENENET NOTE: This method has been converted to use <see cref="ReadOnlySpan{T}"/>.
        /// Callers should prefer the overload without the <paramref name="len"/> parameter and use a slice instead,
        /// but this overload is provided for compatibility with existing code.
        /// </remarks>
        internal static bool StartsWith(ReadOnlySpan<char> s, int len, string prefix)
        {
            return StartsWith(s.Slice(0, len), prefix.AsSpan());
        }

        /// <summary>
        /// Returns true if the character array starts with the prefix.
        /// </summary>
        /// <param name="s"> Input Buffer </param>
        /// <param name="prefix"> Prefix string to test </param>
        /// <returns> <c>true</c> if <paramref name="s"/> starts with <paramref name="prefix"/> </returns>
        /// <remarks>
        /// LUCENENET NOTE: This method has been converted to use <see cref="ReadOnlySpan{T}"/>.
        /// </remarks>
        public static bool StartsWith(ReadOnlySpan<char> s, ReadOnlySpan<char> prefix)
        {
            int prefixLen = prefix.Length;
            if (prefixLen > s.Length)
            {
                return false;
            }

            // LUCENENET: use more efficient implementation in MemoryExtensions
            return s.StartsWith(prefix, StringComparison.Ordinal);
        }

        /// <summary>
        /// Returns true if the character array starts with the prefix.
        /// </summary>
        /// <param name="s"> Input Buffer </param>
        /// <param name="len"> length of input buffer </param>
        /// <param name="prefix"> Prefix string to test </param>
        /// <returns> <c>true</c> if <paramref name="s"/> starts with <paramref name="prefix"/> </returns>
        /// <remarks>
        /// LUCENENET NOTE: This method has been converted to use <see cref="ReadOnlySpan{T}"/>.
        /// Callers should prefer the overload without the <paramref name="len"/> parameter and use a slice instead,
        /// but this overload is provided for compatibility with existing code.
        /// </remarks>
        internal static bool StartsWith(ReadOnlySpan<char> s, int len, ReadOnlySpan<char> prefix)
        {
            return StartsWith(s.Slice(0, len), prefix);
        }

        /// <summary>
        /// Returns true if the character array ends with the suffix.
        /// </summary>
        /// <param name="s"> Input Buffer </param>
        /// <param name="suffix"> Suffix string to test </param>
        /// <returns> <c>true</c> if <paramref name="s"/> ends with <paramref name="suffix"/> </returns>
        /// <remarks>
        /// LUCENENET NOTE: This method has been converted to use <see cref="ReadOnlySpan{T}"/>.
        /// </remarks>
        public static bool EndsWith(ReadOnlySpan<char> s, string suffix)
        {
            return EndsWith(s, suffix.AsSpan());
        }

        /// <summary>
        /// Returns true if the character array ends with the suffix.
        /// </summary>
        /// <param name="s"> Input Buffer </param>
        /// <param name="len"> length of input buffer </param>
        /// <param name="suffix"> Suffix string to test </param>
        /// <returns> <c>true</c> if <paramref name="s"/> ends with <paramref name="suffix"/> </returns>
        /// <remarks>
        /// LUCENENET NOTE: This method has been converted to use <see cref="ReadOnlySpan{T}"/>.
        /// Callers should prefer the overload without the <paramref name="len"/> parameter and use a slice instead,
        /// but this overload is provided for compatibility with existing code.
        /// </remarks>
        internal static bool EndsWith(ReadOnlySpan<char> s, int len, string suffix)
        {
            return EndsWith(s.Slice(0, len), suffix.AsSpan());
        }

        /// <summary>
        /// Returns true if the character array ends with the suffix.
        /// </summary>
        /// <param name="s"> Input Buffer </param>
        /// <param name="suffix"> Suffix string to test </param>
        /// <returns> <c>true</c> if <paramref name="s"/> ends with <paramref name="suffix"/> </returns>
        /// <remarks>
        /// LUCENENET NOTE: This method has been converted to use <see cref="ReadOnlySpan{T}"/>.
        /// </remarks>
        public static bool EndsWith(ReadOnlySpan<char> s, ReadOnlySpan<char> suffix)
        {
            int suffixLen = suffix.Length;
            if (suffixLen > s.Length)
            {
                return false;
            }

            // LUCENENET: use more efficient implementation in MemoryExtensions
            return s.EndsWith(suffix, StringComparison.Ordinal);
        }

        /// <summary>
        /// Returns true if the character array ends with the suffix.
        /// </summary>
        /// <param name="s"> Input Buffer </param>
        /// <param name="len"> length of input buffer </param>
        /// <param name="suffix"> Suffix string to test </param>
        /// <returns> <c>true</c> if <paramref name="s"/> ends with <paramref name="suffix"/> </returns>
        /// <remarks>
        /// LUCENENET NOTE: This method has been converted to use <see cref="ReadOnlySpan{T}"/>.
        /// Callers should prefer the overload without the <paramref name="len"/> parameter and use a slice instead,
        /// but this overload is provided for compatibility with existing code.
        /// </remarks>
        internal static bool EndsWith(ReadOnlySpan<char> s, int len, ReadOnlySpan<char> suffix)
        {
            return EndsWith(s.Slice(0, len), suffix);
        }

        // LUCENENET NOTE: char[] overload of EndsWith removed because the ReadOnlySpan<char> overload can be used instead

        /// <summary>
        /// Delete a character in-place
        /// </summary>
        /// <param name="s"> Input Buffer </param>
        /// <param name="pos"> Position of character to delete </param>
        /// <param name="len"> length of input buffer </param>
        /// <returns> length of input buffer after deletion </returns>
        /// <remarks>
        /// LUCENENET NOTE: This method has been converted to use <see cref="Span{T}"/>.
        /// </remarks>
        public static int Delete(Span<char> s, int pos)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(pos < s.Length);
            if (pos < s.Length - 1) // don't arraycopy if asked to delete last character
            {
                // Arrays.Copy(s, pos + 1, s, pos, len - pos - 1);
                s.Slice(pos + 1, s.Length - pos - 1).CopyTo(s.Slice(pos, s.Length - pos - 1));
            }
            return s.Length - 1;
        }

        /// <summary>
        /// Delete a character in-place
        /// </summary>
        /// <param name="s"> Input Buffer </param>
        /// <param name="pos"> Position of character to delete </param>
        /// <param name="len"> length of input buffer </param>
        /// <returns> length of input buffer after deletion </returns>
        /// <remarks>
        /// LUCENENET NOTE: This method has been converted to use <see cref="Span{T}"/>.
        /// Callers should prefer the overload without the <paramref name="len"/> parameter and use a slice instead,
        /// but this overload is provided for compatibility with existing code.
        /// </remarks>
        internal static int Delete(Span<char> s, int pos, int len)
        {
            return Delete(s.Slice(0, len), pos);
        }

        /// <summary>
        /// Delete n characters in-place
        /// </summary>
        /// <param name="s"> Input Buffer </param>
        /// <param name="pos"> Position of character to delete </param>
        /// <param name="len"> Length of input buffer </param>
        /// <param name="nChars"> number of characters to delete </param>
        /// <returns> length of input buffer after deletion </returns>
        /// <remarks>
        /// LUCENENET NOTE: This method has been converted to use <see cref="Span{T}"/>.
        /// </remarks>
        public static int DeleteN(Span<char> s, int pos, int nChars)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(pos + nChars <= s.Length);
            if (pos + nChars < s.Length) // don't arraycopy if asked to delete the last characters
            {
                // Arrays.Copy(s, pos + nChars, s, pos, len - pos - nChars);
                s.Slice(pos + nChars, s.Length - pos - nChars).CopyTo(s.Slice(pos, s.Length - pos - nChars));
            }
            return s.Length - nChars;
        }

        /// <summary>
        /// Delete n characters in-place
        /// </summary>
        /// <param name="s"> Input Buffer </param>
        /// <param name="pos"> Position of character to delete </param>
        /// <param name="len"> Length of input buffer </param>
        /// <param name="nChars"> number of characters to delete </param>
        /// <returns> length of input buffer after deletion </returns>
        /// <remarks>
        /// LUCENENET NOTE: This method has been converted to use <see cref="Span{T}"/>.
        /// </remarks>
        internal static int DeleteN(Span<char> s, int pos, int len, int nChars)
        {
            return DeleteN(s.Slice(0, len), pos, nChars);
        }
    }
}
