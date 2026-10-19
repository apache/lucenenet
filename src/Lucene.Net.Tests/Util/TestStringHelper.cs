using Lucene.Net.Attributes;
using NUnit.Framework;
using System;
using Assert = Lucene.Net.TestFramework.Assert;

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

    [TestFixture]
    public class TestStringHelper : LuceneTestCase
    {
        [Test]
        public virtual void TestMurmurHash3()
        {
            // Hashes computed using murmur3_32 from https://code.google.com/p/pyfasthash
            Assert.AreEqual(0xf6a5c420, (uint)StringHelper.Murmurhash3_x86_32(new BytesRef("foo"), 0));
            Assert.AreEqual(0xcd018ef6, (uint)StringHelper.Murmurhash3_x86_32(new BytesRef("foo"), 16));
            Assert.AreEqual(0x111e7435, (uint)StringHelper.Murmurhash3_x86_32(new BytesRef("You want weapons? We're in a library! Books! The best weapons in the world!"), 0));
            Assert.AreEqual(0x2c628cd0, (uint)StringHelper.Murmurhash3_x86_32(new BytesRef("You want weapons? We're in a library! Books! The best weapons in the world!"), 3476));
        }

        #region Span-based comparison (#1423)

        /// <summary>
        /// Builds <c>a</c> and <c>b</c> at distinct non-zero offsets, with <c>b</c> equal to <c>a</c>
        /// over the common length except where <paramref name="mutate"/> changes it. Both backing
        /// arrays carry random bytes outside the ref bounds.
        /// </summary>
        private (BytesRef a, BytesRef b) MakePair(int aLength, int bLength, Action<byte[], int> mutate)
        {
            const int aOffset = 3, bOffset = 7;
            byte[] aBytes = new byte[aOffset + aLength + 5];
            byte[] bBytes = new byte[bOffset + bLength + 5];
            Random.NextBytes(aBytes);
            Random.NextBytes(bBytes);
            Array.Copy(aBytes, aOffset, bBytes, bOffset, Math.Min(aLength, bLength));
            mutate?.Invoke(bBytes, bOffset);
            return (new BytesRef(aBytes, aOffset, aLength), new BytesRef(bBytes, bOffset, bLength));
        }

        [Test, LuceneNetSpecific]
        public void BytesDifference_ReturnsFirstMismatchIndex_AcrossVectorWidths()
        {
            for (int length = 0; length <= 70; length++)
            {
                for (int mismatchAt = 0; mismatchAt < length; mismatchAt++)
                {
                    int at = mismatchAt;
                    var (a, b) = MakePair(length, length, (bytes, off) => bytes[off + at] ^= 0x01);
                    Assert.AreEqual(at, StringHelper.BytesDifference(a, b), "mismatch at {0}, length={1}", at, length);
                    Assert.AreEqual(at, StringHelper.BytesDifference(b, a), "mismatch at {0} (reversed), length={1}", at, length);
                }
            }
        }

        [Test, LuceneNetSpecific]
        public void BytesDifference_ReturnsShorterLength_WhenOneIsPrefixOfOther()
        {
            for (int shorter = 0; shorter <= 70; shorter++)
            {
                var (equalA, equalB) = MakePair(shorter, shorter, mutate: null);
                Assert.AreEqual(shorter, StringHelper.BytesDifference(equalA, equalB), "equal, length={0}", shorter);

                var (a, b) = MakePair(shorter, shorter + 3, mutate: null);
                Assert.AreEqual(shorter, StringHelper.BytesDifference(a, b), "prefix, shorter={0}", shorter);
                Assert.AreEqual(shorter, StringHelper.BytesDifference(b, a), "prefix (reversed), shorter={0}", shorter);
            }
        }

        [Test, LuceneNetSpecific]
        public void StartsWith_And_EndsWith_MatchSliceContents_AcrossVectorWidths()
        {
            for (int length = 0; length <= 70; length++)
            {
                for (int affixLength = 0; affixLength <= length; affixLength++)
                {
                    // Build the full ref at offset 3 with random surrounding bytes.
                    byte[] bytes = new byte[3 + length + 5];
                    Random.NextBytes(bytes);
                    var full = new BytesRef(bytes, 3, length);

                    // A prefix/suffix copied into its own array at a different offset.
                    byte[] prefixBytes = new byte[7 + affixLength + 2];
                    byte[] suffixBytes = new byte[5 + affixLength + 2];
                    Random.NextBytes(prefixBytes);
                    Random.NextBytes(suffixBytes);
                    Array.Copy(bytes, 3, prefixBytes, 7, affixLength);
                    Array.Copy(bytes, 3 + length - affixLength, suffixBytes, 5, affixLength);
                    var prefix = new BytesRef(prefixBytes, 7, affixLength);
                    var suffix = new BytesRef(suffixBytes, 5, affixLength);

                    Assert.IsTrue(StringHelper.StartsWith(full, prefix), "StartsWith length={0}, affix={1}", length, affixLength);
                    Assert.IsTrue(StringHelper.EndsWith(full, suffix), "EndsWith length={0}, affix={1}", length, affixLength);

                    if (affixLength > 0)
                    {
                        // Flip the last byte of the prefix and the first byte of the suffix.
                        prefixBytes[7 + affixLength - 1] ^= 0x01;
                        suffixBytes[5] ^= 0x01;
                        Assert.IsFalse(StringHelper.StartsWith(full, prefix), "StartsWith mismatch length={0}, affix={1}", length, affixLength);
                        Assert.IsFalse(StringHelper.EndsWith(full, suffix), "EndsWith mismatch length={0}, affix={1}", length, affixLength);
                    }
                }

                // An affix longer than the ref can never match.
                byte[] longer = new byte[length + 1];
                Random.NextBytes(longer);
                var fullRef = new BytesRef(new byte[length], 0, length);
                Assert.IsFalse(StringHelper.StartsWith(fullRef, new BytesRef(longer)), "StartsWith too long, length={0}", length);
                Assert.IsFalse(StringHelper.EndsWith(fullRef, new BytesRef(longer)), "EndsWith too long, length={0}", length);
            }
        }

        #endregion
    }
}
