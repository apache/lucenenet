using Lucene.Net.Attributes;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Analysis.Payloads
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
    /// Direct round-trip and big-endian-layout tests for <see cref="PayloadHelper"/>.
    /// <para/>
    /// LUCENENET specific: the encode/decode methods were rewritten to use
    /// <see cref="System.Buffers.Binary.BinaryPrimitives"/>; these tests guard the
    /// byte order and offset handling against regressions (negative values, the
    /// int boundaries, and a non-zero <c>offset</c> into a shared buffer).
    /// </summary>
    [TestFixture]
    [LuceneNetSpecific]
    public class TestPayloadHelper : LuceneTestCase
    {
        private static readonly int[] s_int32Values =
        {
            0, 1, -1, 2, -2, 127, 128, 255, 256, 65535, 65536,
            int.MaxValue, int.MinValue, unchecked((int)0xDEADBEEF), 0x12345678
        };

        private static readonly float[] s_singleValues =
        {
            0f, 1f, -1f, 0.5f, -0.5f, 3.5f, 99.3f, float.MaxValue,
            float.MinValue, float.Epsilon, float.PositiveInfinity, float.NegativeInfinity
        };

        [Test]
        [LuceneNetSpecific]
        public void TestEncodeDecodeInt32_RoundTrips()
        {
            foreach (int value in s_int32Values)
            {
                byte[] data = PayloadHelper.EncodeInt32(value);
                assertEquals(4, data.Length);
                assertEquals(value, PayloadHelper.DecodeInt32(data, 0));
            }
        }

        [Test]
        [LuceneNetSpecific]
        public void TestEncodeInt32_IsBigEndian()
        {
            // 0x01020304 must be stored most-significant-byte first.
            byte[] data = PayloadHelper.EncodeInt32(0x01020304);
            assertEquals((byte)0x01, data[0]);
            assertEquals((byte)0x02, data[1]);
            assertEquals((byte)0x03, data[2]);
            assertEquals((byte)0x04, data[3]);
        }

        [Test]
        [LuceneNetSpecific]
        public void TestEncodeDecodeInt32_AtOffsetIntoSharedBuffer()
        {
            // Encode several values back-to-back into one buffer at varying offsets,
            // then decode each independently. Catches offset/length mistakes.
            int[] values = { -2, 256, int.MaxValue, int.MinValue };
            byte[] buffer = new byte[values.Length * 4];
            for (int i = 0; i < values.Length; i++)
            {
                PayloadHelper.EncodeInt32(values[i], buffer, i * 4);
            }
            for (int i = 0; i < values.Length; i++)
            {
                assertEquals(values[i], PayloadHelper.DecodeInt32(buffer, i * 4));
            }
        }

        [Test]
        [LuceneNetSpecific]
        public void TestEncodeDecodeSingle_RoundTrips()
        {
            foreach (float value in s_singleValues)
            {
                byte[] data = PayloadHelper.EncodeSingle(value);
                assertEquals(4, data.Length);
                // Exact bit round-trip (Assert.AreEqual on float compares bit patterns
                // for the special values too).
                assertEquals(value, PayloadHelper.DecodeSingle(data, 0), 0f);
            }
        }

        [Test]
        [LuceneNetSpecific]
        public void TestEncodeDecodeSingle_AtOffset()
        {
            byte[] buffer = new byte[8];
            PayloadHelper.EncodeSingle(3.5f, buffer, 0);
            PayloadHelper.EncodeSingle(-0.5f, buffer, 4);
            assertEquals(3.5f, PayloadHelper.DecodeSingle(buffer, 0), 0f);
            assertEquals(-0.5f, PayloadHelper.DecodeSingle(buffer, 4), 0f);
        }

        [Test]
        [LuceneNetSpecific]
        public void TestDecodeSingle_MatchesEncodeInt32Layout()
        {
            // EncodeSingle is defined as EncodeInt32(SingleToInt32Bits(x)); confirm the
            // span-based DecodeSingle reads back the same float regardless of entry point.
            byte[] data = PayloadHelper.EncodeSingle(99.3f);
            assertEquals(99.3f, PayloadHelper.DecodeSingle(data), 0f);
            int bits = PayloadHelper.DecodeInt32(data, 0);
            assertEquals(J2N.BitConversion.SingleToInt32Bits(99.3f), bits);
        }
    }
}
