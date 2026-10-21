using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Lucene.Net.Util
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
    /// LUCENENET specific: in-place bulk bitwise operations over <see cref="long"/> word arrays,
    /// shared by <see cref="FixedBitSet"/>, <see cref="OpenBitSet"/> and <see cref="Int64BitSet"/>.
    /// <para/>
    /// The Java JIT auto-vectorizes the simple word loops in these classes, but the .NET JIT does not,
    /// so these helpers use <see cref="Vector{T}"/> explicitly when the hardware supports it and fall
    /// back to the original scalar loop otherwise (for example, on 32-bit .NET Framework).
    /// <para/>
    /// Each method processes exactly the first <c>numWords</c> words of both arrays and leaves the
    /// rest of <c>dst</c> untouched. Callers are responsible for bounds (as in the original loops,
    /// an out-of-range <c>numWords</c> throws).
    /// </summary>
    internal static class BitArrayOperations
    {
        /// <summary>
        /// <c>dst[i] |= src[i]</c> for <c>i</c> in <c>[0, numWords)</c>.
        /// </summary>
        public static void Or(long[] dst, long[] src, int numWords)
        {
            Span<long> d = dst.AsSpan(0, numWords);
            ReadOnlySpan<long> s = src.AsSpan(0, numWords);
            int i = 0;
            if (Vector.IsHardwareAccelerated && numWords >= Vector<long>.Count)
            {
                Span<Vector<long>> vd = MemoryMarshal.Cast<long, Vector<long>>(d);
                ReadOnlySpan<Vector<long>> vs = MemoryMarshal.Cast<long, Vector<long>>(s);
                for (int v = 0; v < vd.Length; v++)
                {
                    vd[v] |= vs[v];
                }
                i = vd.Length * Vector<long>.Count;
            }
            for (; i < d.Length; i++)
            {
                d[i] |= s[i];
            }
        }

        /// <summary>
        /// <c>dst[i] &amp;= src[i]</c> for <c>i</c> in <c>[0, numWords)</c>.
        /// </summary>
        public static void And(long[] dst, long[] src, int numWords)
        {
            Span<long> d = dst.AsSpan(0, numWords);
            ReadOnlySpan<long> s = src.AsSpan(0, numWords);
            int i = 0;
            if (Vector.IsHardwareAccelerated && numWords >= Vector<long>.Count)
            {
                Span<Vector<long>> vd = MemoryMarshal.Cast<long, Vector<long>>(d);
                ReadOnlySpan<Vector<long>> vs = MemoryMarshal.Cast<long, Vector<long>>(s);
                for (int v = 0; v < vd.Length; v++)
                {
                    vd[v] &= vs[v];
                }
                i = vd.Length * Vector<long>.Count;
            }
            for (; i < d.Length; i++)
            {
                d[i] &= s[i];
            }
        }

        /// <summary>
        /// <c>dst[i] &amp;= ~src[i]</c> for <c>i</c> in <c>[0, numWords)</c>.
        /// </summary>
        public static void AndNot(long[] dst, long[] src, int numWords)
        {
            Span<long> d = dst.AsSpan(0, numWords);
            ReadOnlySpan<long> s = src.AsSpan(0, numWords);
            int i = 0;
            if (Vector.IsHardwareAccelerated && numWords >= Vector<long>.Count)
            {
                Span<Vector<long>> vd = MemoryMarshal.Cast<long, Vector<long>>(d);
                ReadOnlySpan<Vector<long>> vs = MemoryMarshal.Cast<long, Vector<long>>(s);
                for (int v = 0; v < vd.Length; v++)
                {
                    vd[v] = Vector.AndNot(vd[v], vs[v]);
                }
                i = vd.Length * Vector<long>.Count;
            }
            for (; i < d.Length; i++)
            {
                d[i] &= ~s[i];
            }
        }

        /// <summary>
        /// <c>dst[i] ^= src[i]</c> for <c>i</c> in <c>[0, numWords)</c>.
        /// </summary>
        public static void Xor(long[] dst, long[] src, int numWords)
        {
            Span<long> d = dst.AsSpan(0, numWords);
            ReadOnlySpan<long> s = src.AsSpan(0, numWords);
            int i = 0;
            if (Vector.IsHardwareAccelerated && numWords >= Vector<long>.Count)
            {
                Span<Vector<long>> vd = MemoryMarshal.Cast<long, Vector<long>>(d);
                ReadOnlySpan<Vector<long>> vs = MemoryMarshal.Cast<long, Vector<long>>(s);
                for (int v = 0; v < vd.Length; v++)
                {
                    vd[v] ^= vs[v];
                }
                i = vd.Length * Vector<long>.Count;
            }
            for (; i < d.Length; i++)
            {
                d[i] ^= s[i];
            }
        }
    }
}
