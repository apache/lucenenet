using Lucene.Net.Attributes;
using Lucene.Net.Store;
using NUnit.Framework;
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

    /// <summary>
    /// LUCENENET specific: <see cref="GrowableByteArrayDataOutput"/> had no direct test.
    /// It gained dedicated WriteInt16/WriteInt32/WriteInt64 overrides that grow the buffer
    /// once and write straight into it via BinaryPrimitives (#1279); these round-trip and
    /// big-endian-layout tests guard that, including a buffer growth across writes.
    /// </summary>
    [TestFixture]
    [LuceneNetSpecific]
    public class TestGrowableByteArrayDataOutput : LuceneTestCase
    {
        [Test]
        [LuceneNetSpecific]
        public virtual void TestWriteFixedWidth_RoundTripsAndGrows()
        {
            // Start tiny so the writes force ArrayUtil.Grow at least once.
            var @out = new GrowableByteArrayDataOutput(1);

            short[] i16 = { 0, 1, -1, short.MaxValue, short.MinValue };
            int[] i32 = { 0, 1, -1, int.MaxValue, int.MinValue, 0x01020304 };
            long[] i64 = { 0L, 1L, -1L, long.MaxValue, long.MinValue, 0x0102030405060708L };

            foreach (short v in i16) @out.WriteInt16(v);
            foreach (int v in i32) @out.WriteInt32(v);
            foreach (long v in i64) @out.WriteInt64(v);

            int expectedLen = i16.Length * sizeof(short) + i32.Length * sizeof(int) + i64.Length * sizeof(long);
            Assert.AreEqual(expectedLen, @out.Length);

            var @in = new ByteArrayDataInput(@out.Bytes, 0, @out.Length);
            foreach (short v in i16) Assert.AreEqual(v, @in.ReadInt16());
            foreach (int v in i32) Assert.AreEqual(v, @in.ReadInt32());
            foreach (long v in i64) Assert.AreEqual(v, @in.ReadInt64());
        }

        [Test]
        [LuceneNetSpecific]
        public virtual void TestWriteFixedWidth_IsBigEndian()
        {
            var @out = new GrowableByteArrayDataOutput(16);
            @out.WriteInt16(0x0102);
            @out.WriteInt32(0x03040506);
            byte[] b = @out.Bytes;
            // Int16 first (2 bytes), then Int32 (4 bytes), most-significant byte first.
            Assert.AreEqual((byte)0x01, b[0]);
            Assert.AreEqual((byte)0x02, b[1]);
            Assert.AreEqual((byte)0x03, b[2]);
            Assert.AreEqual((byte)0x04, b[3]);
            Assert.AreEqual((byte)0x05, b[4]);
            Assert.AreEqual((byte)0x06, b[5]);
            Assert.AreEqual(6, @out.Length);
        }
    }
}
