using System;
using Lucene.Net.Attributes;
using NUnit.Framework;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Store
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

    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;

    [TestFixture]
    [LuceneNetSpecific]
    public class TestByteArrayDataOutput : LuceneTestCase
    {
        [Test]
        public virtual void TestWriteString()
        {
            byte[] bytes = new byte[10];
            ByteArrayDataOutput @out = new ByteArrayDataOutput(bytes);
            @out.WriteString("ABC");

            ByteArrayDataInput @in = new ByteArrayDataInput(bytes);
            Assert.AreEqual("ABC", @in.ReadString());
        }

        [Test]
        public virtual void TestWriteChars()
        {
            byte[] bytes = new byte[10];
            ByteArrayDataOutput @out = new ByteArrayDataOutput(bytes);
            @out.WriteChars("ABC".AsSpan());

            ByteArrayDataInput @in = new ByteArrayDataInput(bytes);
            Assert.AreEqual("ABC", @in.ReadString());
        }

        // LUCENENET specific: ByteArrayDataOutput.WriteInt64 has a dedicated override that
        // writes directly into the backing array via BinaryPrimitives (#1279). Round-trip
        // edge values through ByteArrayDataInput.ReadInt64 and verify the big-endian layout.
        [Test]
        [LuceneNetSpecific]
        public virtual void TestWriteInt64_RoundTripsAndIsBigEndian()
        {
            long[] values = { 0L, 1L, -1L, long.MaxValue, long.MinValue, 0x0102030405060708L,
                unchecked((long)0xFFEEDDCCBBAA9988L) };

            // Round-trip several values back-to-back through one buffer.
            byte[] buffer = new byte[values.Length * sizeof(long)];
            ByteArrayDataOutput @out = new ByteArrayDataOutput(buffer);
            foreach (long v in values)
            {
                @out.WriteInt64(v);
            }
            ByteArrayDataInput @in = new ByteArrayDataInput(buffer);
            foreach (long v in values)
            {
                Assert.AreEqual(v, @in.ReadInt64());
            }

            // Most-significant byte first.
            byte[] one = new byte[sizeof(long)];
            new ByteArrayDataOutput(one).WriteInt64(0x0102030405060708L);
            Assert.AreEqual((byte)0x01, one[0]);
            Assert.AreEqual((byte)0x02, one[1]);
            Assert.AreEqual((byte)0x03, one[2]);
            Assert.AreEqual((byte)0x04, one[3]);
            Assert.AreEqual((byte)0x05, one[4]);
            Assert.AreEqual((byte)0x06, one[5]);
            Assert.AreEqual((byte)0x07, one[6]);
            Assert.AreEqual((byte)0x08, one[7]);
        }

        // LUCENENET specific: WriteInt16/WriteInt32 also have direct-array BinaryPrimitives
        // overrides (#1279). Round-trip edge values and verify big-endian layout.
        [Test]
        [LuceneNetSpecific]
        public virtual void TestWriteInt16_RoundTripsAndIsBigEndian()
        {
            short[] values = { 0, 1, -1, short.MaxValue, short.MinValue, 0x0102, unchecked((short)0xFFEE) };
            byte[] buffer = new byte[values.Length * sizeof(short)];
            ByteArrayDataOutput @out = new ByteArrayDataOutput(buffer);
            foreach (short v in values)
            {
                @out.WriteInt16(v);
            }
            ByteArrayDataInput @in = new ByteArrayDataInput(buffer);
            foreach (short v in values)
            {
                Assert.AreEqual(v, @in.ReadInt16());
            }

            byte[] one = new byte[sizeof(short)];
            new ByteArrayDataOutput(one).WriteInt16(0x0102);
            Assert.AreEqual((byte)0x01, one[0]);
            Assert.AreEqual((byte)0x02, one[1]);
        }

        [Test]
        [LuceneNetSpecific]
        public virtual void TestWriteInt32_RoundTripsAndIsBigEndian()
        {
            int[] values = { 0, 1, -1, int.MaxValue, int.MinValue, 0x01020304, unchecked((int)0xFFEEDDCC) };
            byte[] buffer = new byte[values.Length * sizeof(int)];
            ByteArrayDataOutput @out = new ByteArrayDataOutput(buffer);
            foreach (int v in values)
            {
                @out.WriteInt32(v);
            }
            ByteArrayDataInput @in = new ByteArrayDataInput(buffer);
            foreach (int v in values)
            {
                Assert.AreEqual(v, @in.ReadInt32());
            }

            byte[] one = new byte[sizeof(int)];
            new ByteArrayDataOutput(one).WriteInt32(0x01020304);
            Assert.AreEqual((byte)0x01, one[0]);
            Assert.AreEqual((byte)0x02, one[1]);
            Assert.AreEqual((byte)0x03, one[2]);
            Assert.AreEqual((byte)0x04, one[3]);
        }
    }
}
