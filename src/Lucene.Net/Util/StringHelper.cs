using J2N.Numerics;
using J2N.Text;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;

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
    /// Methods for manipulating strings.
    /// <para/>
    /// @lucene.internal
    /// </summary>
    public static class StringHelper // LUCENENET specific - marked static and removed private constructor
    {
        // Poached from Guava: set a different salt/seed
        // for each JVM instance, to frustrate hash key collision
        // denial of service attacks, and to catch any places that
        // somehow rely on hash function/order across JVM
        // instances:

        // LUCENENET specific - Since NUnit considers hash seed a per-test setting, we make the
        // backing field internal and writable so it can be set by the test framework.
        // The tests:seed system property is only applicable to the test environment, as it has no
        // useful purpose in production.
        internal static int goodFastHashSeed = (int)J2N.Time.CurrentTimeMilliseconds();

        /// <summary>
        /// Pass this as the seed to <see cref="Murmurhash3_x86_32(byte[], int, int, int)"/>. </summary>
        //Singleton-esque member. Only created once
        public static int GoodFastHashSeed => goodFastHashSeed;

        [Obsolete("Use GoodFastHashSeed instead. This field will be removed in 4.8.0 release candidate.")]
        public static int GOOD_FAST_HASH_SEED => goodFastHashSeed;

        /// <summary>
        /// Compares two <see cref="BytesRef"/>, element by element, and returns the
        /// number of elements common to both arrays.
        /// </summary>
        /// <param name="left"> The first <see cref="BytesRef"/> to compare. </param>
        /// <param name="right"> The second <see cref="BytesRef"/> to compare. </param>
        /// <returns> The number of common elements. </returns>
        public static int BytesDifference(this BytesRef left, BytesRef right) // LUCENENET specific - converted to extension method
        {
            int len = left.Length < right.Length ? left.Length : right.Length;
            var bytesLeft = left.Bytes;
            int offLeft = left.Offset;
            var bytesRight = right.Bytes;
            int offRight = right.Offset;
            for (int i = 0; i < len; i++)
            {
                if (bytesLeft[i + offLeft] != bytesRight[i + offRight])
                {
                    return i;
                }
            }
            return len;
        }

        /// <summary> Returns a <see cref="T:IComparer{string}"/> over versioned strings such as X.YY.Z
        /// <para/>
        /// @lucene.internal
        /// </summary>
        public static IComparer<string> VersionComparer { get; } = Comparer<string>.Create((a, b) =>
            {
                var aTokens = new StringTokenizer(a, ".");
                var bTokens = new StringTokenizer(b, ".");

                while (aTokens.MoveNext())
                {
                    int aToken = Convert.ToInt32(aTokens.Current, CultureInfo.InvariantCulture);
                    if (bTokens.MoveNext())
                    {
                        int bToken = Convert.ToInt32(bTokens.Current, CultureInfo.InvariantCulture);
                        if (aToken != bToken)
                        {
                            return aToken < bToken ? -1 : 1;
                        }
                    }
                    else
                    {
                        // a has some extra trailing tokens. if these are all zeroes, thats ok.
                        if (aToken != 0)
                        {
                            return 1;
                        }
                    }
                }

                // b has some extra trailing tokens. if these are all zeroes, thats ok.
                while (bTokens.MoveNext())
                {
                    if (Convert.ToInt32(bTokens.Current, CultureInfo.InvariantCulture) != 0)
                    {
                        return -1;
                    }
                }

                return 0;
            });

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Equals(string s1, string s2)
        {
            if (s1 is null)
            {
                return s2 is null;
            }
            else
            {
                return s1.Equals(s2, StringComparison.Ordinal);
            }
        }

        /// <summary>
        /// Returns <c>true</c> if the <paramref name="ref"/> starts with the given <paramref name="prefix"/>.
        /// Otherwise <c>false</c>.
        /// </summary>
        /// <param name="ref">
        ///          The <see cref="BytesRef"/> to test. </param>
        /// <param name="prefix">
        ///          The expected prefix </param>
        /// <returns> Returns <c>true</c> if the <paramref name="ref"/> starts with the given <paramref name="prefix"/>.
        ///         Otherwise <c>false</c>. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool StartsWith(this BytesRef @ref, BytesRef prefix) // LUCENENET specific - converted to extension method
        {
            return SliceEquals(@ref, prefix, 0);
        }

        /// <summary>
        /// Returns <c>true</c> if the <paramref name="ref"/> ends with the given <paramref name="suffix"/>. Otherwise
        /// <c>false</c>.
        /// </summary>
        /// <param name="ref">
        ///          The <see cref="BytesRef"/> to test. </param>
        /// <param name="suffix">
        ///          The expected suffix </param>
        /// <returns> Returns <c>true</c> if the <paramref name="ref"/> ends with the given <paramref name="suffix"/>.
        ///         Otherwise <c>false</c>. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool EndsWith(this BytesRef @ref, BytesRef suffix) // LUCENENET specific - converted to extension method
        {
            return SliceEquals(@ref, suffix, @ref.Length - suffix.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool SliceEquals(this BytesRef sliceToTest, BytesRef other, int pos) // LUCENENET specific - converted to extension method
        {
            if (pos < 0 || sliceToTest.Length - pos < other.Length)
            {
                return false;
            }
            int i = sliceToTest.Offset + pos;
            int j = other.Offset;
            int k = other.Offset + other.Length;

            while (j < k)
            {
                if (sliceToTest.Bytes[i++] != other.Bytes[j++])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Returns the MurmurHash3_x86_32 hash.
        /// Original source/tests at <a href="https://github.com/yonik/java_util/">https://github.com/yonik/java_util/</a>. 
        /// </summary>
        public static int Murmurhash3_x86_32(byte[] data, int offset, int len, int seed)
        {
            const int c1 = unchecked((int)0xcc9e2d51);
            const int c2 = 0x1b873593;

            int h1 = seed;
            int roundedEnd = offset + (len & unchecked((int)0xfffffffc)); // round down to 4 byte block

            for (int i = offset; i < roundedEnd; i += 4)
            {
                // little endian load order
                int k1 = (((sbyte)data[i]) & 0xff) | ((((sbyte)data[i + 1]) & 0xff) << 8) | ((((sbyte)data[i + 2]) & 0xff) << 16) | (((sbyte)data[i + 3]) << 24);
                k1 *= c1;
                k1 = BitOperation.RotateLeft(k1, 15);
                k1 *= c2;

                h1 ^= k1;
                h1 = BitOperation.RotateLeft(h1, 13);
                h1 = h1 * 5 + unchecked((int)0xe6546b64);
            }

            // tail
            int k2 = 0;

            switch (len & 0x03)
            {
                case 3:
                    k2 = (((sbyte)data[roundedEnd + 2]) & 0xff) << 16;
                    // fallthrough
                    goto case 2;
                case 2:
                    k2 |= (((sbyte)data[roundedEnd + 1]) & 0xff) << 8;
                    // fallthrough
                    goto case 1;
                case 1:
                    k2 |= (((sbyte)data[roundedEnd]) & 0xff);
                    k2 *= c1;
                    k2 = BitOperation.RotateLeft(k2, 15);
                    k2 *= c2;
                    h1 ^= k2;
                    break;
            }

            // finalization
            h1 ^= len;

            // fmix(h1);
            h1 ^= h1.TripleShift(16);
            h1 *= unchecked((int)0x85ebca6b);
            h1 ^= h1.TripleShift(13);
            h1 *= unchecked((int)0xc2b2ae35);
            h1 ^= h1.TripleShift(16);

            return h1;
        }

        /// <summary>
        /// Returns the MurmurHash3_x86_32 hash.
        /// Original source/tests at <a href="https://github.com/yonik/java_util/">https://github.com/yonik/java_util/</a>. 
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Murmurhash3_x86_32(BytesRef bytes, int seed)
        {
            return Murmurhash3_x86_32(bytes.Bytes, bytes.Offset, bytes.Length, seed);
        }
    }
}