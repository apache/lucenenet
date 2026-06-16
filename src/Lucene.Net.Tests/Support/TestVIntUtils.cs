using Lucene.Net.Attributes;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using Assert = Lucene.Net.TestFramework.Assert;

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
    /// Tests for <see cref="VIntUtils"/>, the shared variable-length integer
    /// decoder used by the buffer-backed <see cref="DataInput"/> implementations.
    /// </summary>
    [TestFixture]
    [LuceneNetSpecific]
    public class TestVIntUtils : LuceneTestCase
    {
        // Encodes a VInt32 using the canonical DataOutput writer so the test
        // exercises the same format the readers consume in production.
        private static byte[] EncodeVInt32(int value)
        {
            byte[] backing = new byte[VIntUtils.MaxVInt32Length];
            var output = new ByteArrayDataOutput(backing);
            output.WriteVInt32(value);
            byte[] result = new byte[output.Position];
            Array.Copy(backing, result, result.Length);
            return result;
        }

        private static byte[] EncodeVInt64(long value)
        {
            byte[] backing = new byte[VIntUtils.MaxVInt64Length];
            var output = new ByteArrayDataOutput(backing);
            output.WriteVInt64(value);
            byte[] result = new byte[output.Position];
            Array.Copy(backing, result, result.Length);
            return result;
        }

        [Test]
        [LuceneNetSpecific]
        public void TestReadVInt32_RoundTripsValuesOfEveryByteLength()
        {
            // One representative value per encoded length (1..5 bytes).
            int[] values = { 0, 1, 127, 128, 16383, 16384, 2097151, 2097152, 268435455, int.MaxValue, -1 };

            // Pad to MaxVInt32Length so the decoder never reads past the span,
            // mirroring how the buffered readers guarantee 5 bytes are available.
            Span<byte> source = stackalloc byte[VIntUtils.MaxVInt32Length];

            foreach (int value in values)
            {
                byte[] encoded = EncodeVInt32(value);
                source.Clear();
                encoded.AsSpan().CopyTo(source);

                bool ok = VIntUtils.TryReadVInt32(source, out int result, out int count);

                assertTrue("value " + value + " should decode as a well-formed VInt32", ok);
                assertEquals("value " + value, value, result);
                assertEquals("byte count for value " + value, encoded.Length, count);
            }
        }

        [Test]
        [LuceneNetSpecific]
        public void TestReadVInt64_RoundTripsValuesOfEveryByteLength()
        {
            // One representative value per encoded length (1..9 bytes). Negative
            // values are disallowed on write (WriteVInt64 asserts i >= 0), so the
            // 9-byte case is covered here by long.MaxValue; the negative/too-long
            // read path is covered separately below.
            long[] values =
            {
                0L, 1L, 127L, 128L, 16383L, 16384L, 2097151L, 2097152L,
                268435455L, 268435456L, 34359738367L, 34359738368L,
                4398046511103L, 4398046511104L, 562949953421311L, 562949953421312L,
                72057594037927935L, 72057594037927936L, long.MaxValue
            };

            Span<byte> source = stackalloc byte[VIntUtils.MaxVInt64Length];

            foreach (long value in values)
            {
                byte[] encoded = EncodeVInt64(value);
                source.Clear();
                encoded.AsSpan().CopyTo(source);

                bool ok = VIntUtils.TryReadVInt64(source, out long result, out int count);

                assertTrue("value " + value + " should decode as a well-formed VInt64", ok);
                assertEquals("value " + value, value, result);
                assertEquals("byte count for value " + value, encoded.Length, count);
            }
        }

        [Test]
        [LuceneNetSpecific]
        public void TestReadVInt32_SingleByteFastPath()
        {
            // The all-positive single byte (high bit clear) is the most common case.
            Span<byte> source = stackalloc byte[VIntUtils.MaxVInt32Length];
            for (int b = 0; b <= sbyte.MaxValue; b++)
            {
                source.Clear();
                source[0] = (byte)b;

                bool ok = VIntUtils.TryReadVInt32(source, out int result, out int count);

                assertTrue(ok);
                assertEquals(b, result);
                assertEquals("single byte should consume exactly one byte", 1, count);
            }
        }

        [Test]
        [LuceneNetSpecific]
        public void TestReadVInt32_TooManyBitsReturnsFalseButAdvances()
        {
            // 5 continuation bytes with extra high bits set in the 5th byte: the
            // original byte-by-byte reader threw after consuming all 5 bytes, so
            // count must still report 5 so callers can advance past the bad VInt.
            Span<byte> source = stackalloc byte[] { 0xFF, 0xFF, 0xFF, 0xFF, 0xFF };

            bool ok = VIntUtils.TryReadVInt32(source, out int _, out int count);

            assertFalse("a 5th byte with high bits set is not a valid VInt32", ok);
            assertEquals("all 5 bytes must be consumed", 5, count);
        }

        [Test]
        [LuceneNetSpecific]
        public void TestReadVInt64_ContinuationPastNineBytesReturnsFalseButAdvances()
        {
            // 9th byte still has the continuation bit set, which would indicate a
            // value needing a 10th byte (disallowed). count must still report 9.
            Span<byte> source = stackalloc byte[]
            {
                0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF
            };

            bool ok = VIntUtils.TryReadVInt64(source, out long _, out int count);

            assertFalse("a 9th byte with the continuation bit set is not a valid VInt64", ok);
            assertEquals("all 9 bytes must be consumed", 9, count);
        }

        [Test]
        [LuceneNetSpecific]
        public void TestMaxLengths()
        {
            // A negative int encodes to the full 5 bytes; long.MaxValue (63 bits,
            // 7 bits per byte) encodes to the full 9 bytes.
            assertEquals(VIntUtils.MaxVInt32Length, EncodeVInt32(-1).Length);
            assertEquals(VIntUtils.MaxVInt64Length, EncodeVInt64(long.MaxValue).Length);
        }
    }
}
