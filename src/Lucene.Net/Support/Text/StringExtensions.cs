using J2N.Text;
using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace Lucene.Net.Support.Text
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
    /// Extensions to <see cref="string"/>.
    /// </summary>
    internal static class StringExtensions
    {
        /// <summary>
        /// Returns <c>true</c> if <paramref name="input"/> contains any character from <paramref name="charsToCompare"/>.
        /// </summary>
        /// <param name="input">The string in which to seek characters from <paramref name="charsToCompare"/>.</param>
        /// <param name="charsToCompare">An array of characters to check.</param>
        /// <returns><c>true</c> if any <paramref name="charsToCompare"/> are found; otherwise, <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ContainsAny(this string input, char[] charsToCompare)
        {
            if (input is null)
                throw new ArgumentNullException(nameof(input));
            if (charsToCompare is null)
                throw new ArgumentNullException(nameof(charsToCompare));

            // Ensure the strings passed don't contain invalid characters
            for (int i = 0; i < charsToCompare.Length; i++)
            {
                if (input.Contains(charsToCompare[i]))
                    return true;
            }
            return false;
        }

#if !FEATURE_STRING_CONTAINS_STRINGCOMPARISON
        /// <summary>
        /// Returns <c>true</c> if <paramref name="input"/> contains <paramref name="value"/>
        /// using the specified <paramref name="comparison"/>.
        /// </summary>
        /// <param name="input">The string in which to seek <paramref name="value"/>.</param>
        /// <param name="value">The string to check for.</param>
        /// <param name="comparison">The <see cref="StringComparison"/> to use.</param>
        /// <returns><c>true</c> if <paramref name="value"/> is found; otherwise, <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Contains(this string input, string value, StringComparison comparison)
        {
            if (input is null)
                throw new ArgumentNullException(nameof(input));
            if (value is null)
                throw new ArgumentNullException(nameof(value));

            return input.AsSpan().Contains(value.AsSpan(), comparison);
        }
#endif

#nullable enable
        /// <summary>
        /// Returns <c>true</c> if <paramref name="path"/> is a valid, single path component for use in index
        /// file system access. A valid value is either a file name or a directory name, without any directory
        /// or volume separators.
        /// </summary>
        /// <param name="path">The file name to check.</param>
        /// <returns><c>true</c> if <paramref name="path"/> is a valid path component;
        /// otherwise, <c>false</c>.</returns>
        public static bool IsValidSinglePathComponent(this string path)
        {
            // NOTE: `path == Path.GetFileName(path)` ensures there are no directory components, such as
            // directory separators or volume names. This works for both file and folder names, despite the use of
            // Path.GetFileName() for the latter. If `path` ends with a directory separator, it's invalid anyway.
            // Check IndexOfAny before Path.GetFileName: on .NET Framework, Path.GetFileName throws
            // ArgumentException for strings containing NUL or other characters that are illegal in paths.
            return !string.IsNullOrEmpty(path)
                   && path != "."
                   && path != ".."
                   && path.IndexOfAny(Path.GetInvalidFileNameChars()) < 0
                   && path.IndexOf('\\') < 0 // backslash is not an invalid character on Linux/macOS but is invalid for this purpose
                   && path == Path.GetFileName(path); // see NOTE above
        }
#nullable restore
    }
}
