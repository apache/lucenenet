using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
#nullable enable

namespace Lucene.Net.Support
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
    /// Helper methods for working with variable-length integers (VInts).
    /// </summary>
    /// <seealso cref="Lucene.Net.Store.DataInput"/>
    [ExceptionToNetNumericConvention] // "VInt" follows the existing Read/WriteVInt32/64 naming
    internal static class VIntUtils
    {
        /// <summary>
        /// Gets the max bytes that a 32-bit VInt can consume.
        /// </summary>
        public const int MaxVInt32Length = 5;

        /// <summary>
        /// Gets the max bytes that a 64-bit VInt can consume.
        /// </summary>
        public const int MaxVInt64Length = 9;

        /// <summary>
        /// Tries to read a 32-bit VInt from the given <paramref name="source"/>.
        /// <paramref name="source"/> must be at least <see cref="MaxVInt32Length"/> bytes
        /// in length to avoid possible out-of-range exceptions.
        /// </summary>
        /// <param name="source">The source bytes to read from.</param>
        /// <param name="value">The decoded value (undefined if the method returns <c>false</c>).</param>
        /// <param name="count">The number of bytes consumed. Always set, regardless of return value,
        /// so that callers can advance their position even on a malformed VInt — matching the
        /// behavior of the original byte-by-byte readers (which advanced before throwing).</param>
        /// <returns><c>true</c> if the VInt was well-formed; <c>false</c> if the 5th byte had
        /// extra high bits set.</returns>
        public static bool TryReadVInt32(ReadOnlySpan<byte> source, out int value, out int count)
        {
            Debug.Assert(source.Length >= MaxVInt32Length);
            byte b = source[0];
            if (b <= sbyte.MaxValue) // LUCENENET: Optimized equivalent of "if ((sbyte)b >= 0)"
            {
                count = 1;
                value = b;
                return true;
            }
            int i = b & 0x7F;
            b = source[1];
            i |= (b & 0x7F) << 7;
            if (b <= sbyte.MaxValue)
            {
                count = 2;
                value = i;
                return true;
            }
            b = source[2];
            i |= (b & 0x7F) << 14;
            if (b <= sbyte.MaxValue)
            {
                count = 3;
                value = i;
                return true;
            }
            b = source[3];
            i |= (b & 0x7F) << 21;
            if (b <= sbyte.MaxValue)
            {
                count = 4;
                value = i;
                return true;
            }
            b = source[4];
            // Warning: the next ands use 0x0F / 0xF0 - beware copy/paste errors:
            i |= (b & 0x0F) << 28;
            count = 5;
            value = i;
            return (b & 0xF0) == 0;
        }

        /// <summary>
        /// Tries to read a 64-bit VInt from the given <paramref name="source"/>.
        /// <paramref name="source"/> must be at least <see cref="MaxVInt64Length"/> bytes
        /// in length to avoid possible out-of-range exceptions.
        /// </summary>
        /// <param name="source">The source bytes to read from.</param>
        /// <param name="value">The decoded value (undefined if the method returns <c>false</c>).</param>
        /// <param name="count">The number of bytes consumed. Always set, regardless of return value
        /// — see <see cref="TryReadVInt32"/>.</param>
        /// <returns><c>true</c> if the VInt was well-formed; <c>false</c> if the 9th byte had
        /// the continuation bit set (which would indicate a negative value, disallowed).</returns>
        public static bool TryReadVInt64(ReadOnlySpan<byte> source, out long value, out int count)
        {
            Debug.Assert(source.Length >= MaxVInt64Length);
            byte b = source[0];
            if (b <= sbyte.MaxValue)
            {
                count = 1;
                value = b;
                return true;
            }
            long i = b & 0x7FL;
            b = source[1];
            i |= (b & 0x7FL) << 7;
            if (b <= sbyte.MaxValue)
            {
                count = 2;
                value = i;
                return true;
            }
            b = source[2];
            i |= (b & 0x7FL) << 14;
            if (b <= sbyte.MaxValue)
            {
                count = 3;
                value = i;
                return true;
            }
            b = source[3];
            i |= (b & 0x7FL) << 21;
            if (b <= sbyte.MaxValue)
            {
                count = 4;
                value = i;
                return true;
            }
            b = source[4];
            i |= (b & 0x7FL) << 28;
            if (b <= sbyte.MaxValue)
            {
                count = 5;
                value = i;
                return true;
            }
            b = source[5];
            i |= (b & 0x7FL) << 35;
            if (b <= sbyte.MaxValue)
            {
                count = 6;
                value = i;
                return true;
            }
            b = source[6];
            i |= (b & 0x7FL) << 42;
            if (b <= sbyte.MaxValue)
            {
                count = 7;
                value = i;
                return true;
            }
            b = source[7];
            i |= (b & 0x7FL) << 49;
            if (b <= sbyte.MaxValue)
            {
                count = 8;
                value = i;
                return true;
            }
            b = source[8];
            i |= (b & 0x7FL) << 56;
            count = 9;
            value = i;
            return b <= sbyte.MaxValue;
        }

        // LUCENENET: The throw sites below are factored into dedicated, non-inlined helpers so the
        // hot Read*VInt* overrides that call them stay small enough for the JIT to inline. Keeping
        // the `throw` + message string out of the caller body avoids bloating its IL (the inliner
        // counts the throw path's size even though it is never taken on the happy path). The messages
        // live in a nested SR class aligned with the BCL convention so they can be moved to an
        // SR.resx later without touching the call sites. See dotnet/runtime's ThrowHelper for the
        // pattern this mirrors: https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/ThrowHelper.cs

        /// <summary>
        /// Throws an <see cref="IOException"/> indicating a malformed 32-bit VInt. Used by the
        /// buffered/stream-backed readers, which surface decode failures as I/O errors.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        [DoesNotReturn]
        public static void ThrowInvalidVInt32()
            => throw new IOException(SR.Invalid_VInt32);

        /// <summary>
        /// Throws an <see cref="IOException"/> indicating a malformed 64-bit VInt. Used by the
        /// buffered/stream-backed readers, which surface decode failures as I/O errors.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        [DoesNotReturn]
        public static void ThrowInvalidVInt64()
            => throw new IOException(SR.Invalid_VInt64);

        /// <summary>
        /// Throws a <see cref="RuntimeException"/> indicating a malformed 32-bit VInt. Used by
        /// <see cref="Lucene.Net.Store.ByteArrayDataInput"/>, which (matching upstream) throws an
        /// unchecked exception rather than an <see cref="IOException"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        [DoesNotReturn]
        public static void ThrowInvalidVInt32Runtime()
            => throw RuntimeException.Create(SR.Invalid_VInt32);

        /// <summary>
        /// Throws a <see cref="RuntimeException"/> indicating a malformed 64-bit VInt. Used by
        /// <see cref="Lucene.Net.Store.ByteArrayDataInput"/>, which (matching upstream) throws an
        /// unchecked exception rather than an <see cref="IOException"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        [DoesNotReturn]
        public static void ThrowInvalidVInt64Runtime()
            => throw RuntimeException.Create(SR.Invalid_VInt64);

        private static class SR
        {
            public const string Invalid_VInt32 = "Invalid VInt32 detected (too many bits)";
            public const string Invalid_VInt64 = "Invalid VInt64 detected (negative values disallowed)";
        }
    }
}
