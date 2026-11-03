using J2N.Collections.Generic;
using Lucene.Net.Util;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
#nullable enable

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
    /// A case-insensitive equality comparer for the keys of <see cref="CharArraySet"/> and
    /// <see cref="CharArrayDictionary{TValue}"/> that supports lookups by <see cref="ReadOnlySpan{T}"/>
    /// of <see cref="char"/> without allocating.
    /// <para/>
    /// Keys are folded by lowercasing each code point in the invariant culture, which matches
    /// Java Lucene's <c>Character.toLowerCase()</c> based behavior. This is intentionally different
    /// from <see cref="StringComparer.OrdinalIgnoreCase"/>, which folds to uppercase and therefore
    /// matches a different set of strings (for example, the Kelvin sign and dotless i).
    /// <para/>
    /// Keys that are stored in a collection using this comparer are expected to already be folded
    /// (see <see cref="Fold(string)"/>), which the collections guarantee. Probes may be in any case.
    /// </summary>
    // LUCENENET specific
    internal sealed class CharArrayComparer : IEqualityComparer<string>, ISpanAlternateEqualityComparer<char, string>
    {
        /// <summary>
        /// Comparer for <see cref="LuceneVersion"/> 3.1 and later, which properly lowercases supplementary characters.
        /// </summary>
        public static readonly CharArrayComparer Default = new CharArrayComparer(CharacterUtils.GetInstance(LuceneVersion.LUCENE_48));

        /// <summary>
        /// Comparer for <see cref="LuceneVersion"/> before 3.1, which lowercases each <see cref="char"/>
        /// on its own and therefore does not lowercase supplementary characters.
        /// </summary>
#pragma warning disable 612, 618
        public static readonly CharArrayComparer Lucene30 = new CharArrayComparer(CharacterUtils.GetInstance(LuceneVersion.LUCENE_30));
#pragma warning restore 612, 618

        private readonly CharacterUtils charUtils;

        private CharArrayComparer(CharacterUtils charUtils)
        {
            this.charUtils = charUtils;
        }

        /// <summary>
        /// Gets the case-insensitive comparer for the given <paramref name="matchVersion"/>.
        /// </summary>
        public static CharArrayComparer GetInstance(LuceneVersion matchVersion)
        {
#pragma warning disable 612, 618
            return matchVersion.OnOrAfter(LuceneVersion.LUCENE_31) ? Default : Lucene30;
#pragma warning restore 612, 618
        }

        /// <summary>
        /// Returns the folded (lowercased) form of <paramref name="text"/>. If <paramref name="text"/>
        /// is already folded, the same instance is returned.
        /// </summary>
        public string Fold(string text)
        {
            if (IsAsciiFolded(text.AsSpan()))
                return text;

            int length = text.Length;
            char[]? rented = null;
            Span<char> buffer = length * sizeof(char) <= Constants.MaxStackByteLimit
                ? stackalloc char[length]
                : (rented = ArrayPool<char>.Shared.Rent(length));
            try
            {
                Span<char> folded = buffer.Slice(0, length);
                charUtils.ToLower(text.AsSpan(), folded);
                return folded.SequenceEqual(text.AsSpan()) ? text : folded.ToString();
            }
            finally
            {
                if (rented is not null)
                    ArrayPool<char>.Shared.Return(rented);
            }
        }

        /// <summary>
        /// Returns a new <see cref="string"/> containing the folded (lowercased) form of <paramref name="text"/>.
        /// </summary>
        public string Create(ReadOnlySpan<char> text)
        {
            if (IsAsciiFolded(text))
                return text.ToString();

            int length = text.Length;
            char[]? rented = null;
            Span<char> buffer = length * sizeof(char) <= Constants.MaxStackByteLimit
                ? stackalloc char[length]
                : (rented = ArrayPool<char>.Shared.Rent(length));
            try
            {
                Span<char> folded = buffer.Slice(0, length);
                charUtils.ToLower(text, folded);
                return folded.ToString();
            }
            finally
            {
                if (rented is not null)
                    ArrayPool<char>.Shared.Return(rented);
            }
        }

        public bool Equals(string? x, string? y)
        {
            if (ReferenceEquals(x, y))
                return true;
            if (x is null || y is null)
                return false;
            return Equals(x.AsSpan(), y.AsSpan());
        }

        public bool Equals(ReadOnlySpan<char> alternate, string other)
        {
            if (other is null)
                return false;
            return Equals(alternate, other.AsSpan());
        }

        private bool Equals(ReadOnlySpan<char> x, ReadOnlySpan<char> y)
        {
            // Lowercasing never changes the number of UTF-16 code units
            if (x.Length != y.Length)
                return false;

            // Fast path: probes are typically already lowercased (for example, by LowerCaseFilter)
            // and stored keys always are.
            if (x.SequenceEqual(y))
                return true;

            int length = x.Length;
            char[]? rented = null;
            Span<char> buffer = length * 2 * sizeof(char) <= Constants.MaxStackByteLimit
                ? stackalloc char[length * 2]
                : (rented = ArrayPool<char>.Shared.Rent(length * 2));
            try
            {
                Span<char> foldedX = buffer.Slice(0, length);
                Span<char> foldedY = buffer.Slice(length, length);
                charUtils.ToLower(x, foldedX);
                charUtils.ToLower(y, foldedY);
                return foldedX.SequenceEqual(foldedY);
            }
            finally
            {
                if (rented is not null)
                    ArrayPool<char>.Shared.Return(rented);
            }
        }

        public int GetHashCode(string obj)
        {
            if (obj is null)
                return 0;
            return GetHashCode(obj.AsSpan());
        }

        public int GetHashCode(ReadOnlySpan<char> alternate)
        {
            // Fast path for ASCII, which can be folded inline. This produces the same result
            // as folding the whole span first, because ASCII lowercasing is the same for all versions.
            int code = 0;
            int i = 0;
            for (; i < alternate.Length; i++)
            {
                char c = alternate[i];
                if (c > 0x7F)
                    break;
                code = code * 31 + ToLowerAscii(c);
            }
            if (i == alternate.Length)
                return code;

            // Slow path: fold the rest. Splitting here is safe because the previous char
            // (if any) is ASCII and therefore cannot be the high half of a surrogate pair.
            ReadOnlySpan<char> remaining = alternate.Slice(i);
            int length = remaining.Length;
            char[]? rented = null;
            Span<char> buffer = length * sizeof(char) <= Constants.MaxStackByteLimit
                ? stackalloc char[length]
                : (rented = ArrayPool<char>.Shared.Rent(length));
            try
            {
                Span<char> folded = buffer.Slice(0, length);
                charUtils.ToLower(remaining, folded);
                foreach (char c in folded)
                {
                    code = code * 31 + c;
                }
                return code;
            }
            finally
            {
                if (rented is not null)
                    ArrayPool<char>.Shared.Return(rented);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static char ToLowerAscii(char c)
            => (uint)(c - 'A') <= 'Z' - 'A' ? (char)(c | 0x20) : c;

        private static bool IsAsciiFolded(ReadOnlySpan<char> text)
        {
            foreach (char c in text)
            {
                if (c > 0x7F || (uint)(c - 'A') <= 'Z' - 'A')
                    return false;
            }
            return true;
        }
    }
}
