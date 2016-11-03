using Lucene.Net.Support;
using System;
using System.Collections.Generic;

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
    ///
    /// @lucene.internal
    /// </summary>
    public abstract class StringHelper
    {
        /// <summary> Expert:
        /// The StringInterner implementation used by Lucene.
        /// This shouldn't be changed to an incompatible implementation after other Lucene APIs have been used.
        /// </summary>
        public static StringInterner interner = new SimpleStringInterner(1024, 8);

        /// <summary>Returns the same string object for all equal strings.</summary>
        public static System.String Intern(System.String s)
        {
            return interner.Intern(s);
        }

        /// <summary>
        /// Compares two <seealso cref="BytesRef"/>, element by element, and returns the
        /// number of elements common to both arrays.
        /// </summary>
        /// <param name="left"> The first <seealso cref="BytesRef"/> to compare </param>
        /// <param name="right"> The second <seealso cref="BytesRef"/> to compare </param>
        /// <returns> The number of common elements. </returns>
        public static int BytesDifference(BytesRef left, BytesRef right)
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

        private StringHelper()
        {
        }

        /// <returns> a Comparator over versioned strings such as X.YY.Z
        /// @lucene.internal </returns>
        public static IComparer<string> VersionComparator
        {
            get
            {
                return versionComparator;
            }
        }

        private static readonly IComparer<string> versionComparator = new ComparatorAnonymousInnerClassHelper();

        private sealed class ComparatorAnonymousInnerClassHelper : IComparer<string>
        {
            public ComparatorAnonymousInnerClassHelper()
            {
            }

            public int Compare(string a, string b)
            {
                var aTokens = new StringTokenizer(a, ".");
                var bTokens = new StringTokenizer(b, ".");

                while (aTokens.HasMoreTokens())
                {
                    int aToken = Convert.ToInt32(aTokens.NextToken());
                    if (bTokens.HasMoreTokens())
                    {
                        int bToken = Convert.ToInt32(bTokens.NextToken());
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
                while (bTokens.HasMoreTokens())
                {
                    if (Convert.ToInt32(bTokens.NextToken()) != 0)
                    {
                        return -1;
                    }
                }

                return 0;
            }
        }

        public static bool Equals(string s1, string s2)
        {
            if (s1 == null)
            {
                return s2 == null;
            }
            else
            {
                return s1.Equals(s2);
            }
        }

        /// <summary>
        /// Returns <code>true</code> iff the ref starts with the given prefix.
        /// Otherwise <code>false</code>.
        /// </summary>
        /// <param name="ref">
        ///          the <seealso cref="BytesRef"/> to test </param>
        /// <param name="prefix">
        ///          the expected prefix </param>
        /// <returns> Returns <code>true</code> iff the ref starts with the given prefix.
        ///         Otherwise <code>false</code>. </returns>
        public static bool StartsWith(BytesRef @ref, BytesRef prefix)
        {
            return SliceEquals(@ref, prefix, 0);
        }

        /// <summary>
        /// Returns <code>true</code> iff the ref ends with the given suffix. Otherwise
        /// <code>false</code>.
        /// </summary>
        /// <param name="ref">
        ///          the <seealso cref="BytesRef"/> to test </param>
        /// <param name="suffix">
        ///          the expected suffix </param>
        /// <returns> Returns <code>true</code> iff the ref ends with the given suffix.
        ///         Otherwise <code>false</code>. </returns>
        public static bool EndsWith(BytesRef @ref, BytesRef suffix)
        {
            return SliceEquals(@ref, suffix, @ref.Length - suffix.Length);
        }

        private static bool SliceEquals(BytesRef sliceToTest, BytesRef other, int pos)
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
        /// Pass this as the seed to <seealso cref="#murmurhash3_x86_32"/>. </summary>

        //Singleton-esque member. Only created once
        private static int good_fast_hash_seed;

        public static int GOOD_FAST_HASH_SEED
        {
            get
            {
                if (good_fast_hash_seed == 0)
                {
                    //LUCENE TO-DO No idea if this works
                    var prop = AppSettings.Get("tests.seed", null);
                    if (prop != null)
                    {
                        // So if there is a test failure that relied on hash
                        // order, we remain reproducible based on the test seed:
                        if (prop.Length > 8)
                        {
                            prop = prop.Substring(prop.Length - 8);
                        }
                        good_fast_hash_seed = (int)Convert.ToInt32(prop, 16);
                    }
                    else
                    {
                        good_fast_hash_seed = (int)DateTime.Now.Millisecond;
                    }
                }
                return good_fast_hash_seed;
            }
        }

        /// <summary>
        /// Returns the MurmurHash3_x86_32 hash.
        /// Original source/tests at https://github.com/yonik/java_util/
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
                k1 = Number.RotateLeft(k1, 15);
                k1 *= c2;

                h1 ^= k1;
                h1 = Number.RotateLeft(h1, 13);
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
                    k2 = Number.RotateLeft(k2, 15);
                    k2 *= c2;
                    h1 ^= k2;
                    break;
            }

            // finalization
            h1 ^= len;

            // fmix(h1);
            h1 ^= (int)((uint)h1 >> 16);
            h1 *= unchecked((int)0x85ebca6b);
            h1 ^= (int)((uint)h1 >> 13);
            h1 *= unchecked((int)0xc2b2ae35);
            h1 ^= (int)((uint)h1 >> 16);

            return h1;
        }

        public static int Murmurhash3_x86_32(BytesRef bytes, int seed)
        {
            return Murmurhash3_x86_32(bytes.Bytes, bytes.Offset, bytes.Length, seed);
        }
    }
}