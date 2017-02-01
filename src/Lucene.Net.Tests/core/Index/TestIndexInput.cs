using System;

namespace Lucene.Net.Index
{
    using NUnit.Framework;
    using System.IO;
    using System.Reflection;
    using ByteArrayDataInput = Lucene.Net.Store.ByteArrayDataInput;
    using ByteArrayDataOutput = Lucene.Net.Store.ByteArrayDataOutput;
    using DataInput = Lucene.Net.Store.DataInput;
    using IndexInput = Lucene.Net.Store.IndexInput;
    using IndexOutput = Lucene.Net.Store.IndexOutput;

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
    using RAMDirectory = Lucene.Net.Store.RAMDirectory;
    using TestUtil = Lucene.Net.Util.TestUtil;

    [TestFixture]
    public class TestIndexInput : LuceneTestCase
    {
        internal static readonly byte[] READ_TEST_BYTES = new byte[] { unchecked((byte)(sbyte)0x80), 0x01, unchecked((byte)(sbyte)0xFF), 0x7F, unchecked((byte)(sbyte)0x80), unchecked((byte)(sbyte)0x80), 0x01, unchecked((byte)(sbyte)0x81), unchecked((byte)(sbyte)0x80), 0x01, unchecked((byte)(sbyte)0xFF), unchecked((byte)(sbyte)0xFF), unchecked((byte)(sbyte)0xFF), unchecked((byte)(sbyte)0xFF), 0x07, unchecked((byte)(sbyte)0xFF), unchecked((byte)(sbyte)0xFF), unchecked((byte)(sbyte)0xFF), unchecked((byte)(sbyte)0xFF), 0x0F, unchecked((byte)(sbyte)0xFF), unchecked((byte)(sbyte)0xFF), unchecked((byte)(sbyte)0xFF), unchecked((byte)(sbyte)0xFF), 0x07, unchecked((byte)(sbyte)0xFF), unchecked((byte)(sbyte)0xFF), unchecked((byte)(sbyte)0xFF), unchecked((byte)(sbyte)0xFF), unchecked((byte)(sbyte)0xFF), unchecked((byte)(sbyte)0xFF), unchecked((byte)(sbyte)0xFF), unchecked((byte)(sbyte)0xFF), (byte)0x7F, 0x06, (byte)'L', (byte)'u', (byte)'c', (byte)'e', (byte)'n', (byte)'e', 0x02, unchecked((byte)(sbyte)0xC2), unchecked((byte)(sbyte)0xBF), 0x0A, (byte)'L', (byte)'u', unchecked((byte)(sbyte)0xC2), unchecked((byte)(sbyte)0xBF), (byte)(sbyte)'c', (byte)'e', unchecked((byte)(sbyte)0xC2), unchecked((byte)(sbyte)0xBF), (byte)'n', (byte)'e', 0x03, unchecked((byte)(sbyte)0xE2), unchecked((byte)(sbyte)0x98), unchecked((byte)(sbyte)0xA0), 0x0C, (byte)'L', (byte)'u', unchecked((byte)(sbyte)0xE2), unchecked((byte)(sbyte)0x98), unchecked((byte)(sbyte)0xA0), (byte)'c', (byte)'e', unchecked((byte)(sbyte)0xE2), unchecked((byte)(sbyte)0x98), unchecked((byte)(sbyte)0xA0), (byte)'n', (byte)'e', 0x04, unchecked((byte)(sbyte)0xF0), unchecked((byte)(sbyte)0x9D), unchecked((byte)(sbyte)0x84), unchecked((byte)(sbyte)0x9E), 0x08, unchecked((byte)(sbyte)0xF0), unchecked((byte)(sbyte)0x9D), unchecked((byte)(sbyte)0x84), unchecked((byte)(sbyte)0x9E), unchecked((byte)(sbyte)0xF0), unchecked((byte)(sbyte)0x9D), unchecked((byte)(sbyte)0x85), unchecked((byte)(sbyte)0xA0), 0x0E, (byte)'L', (byte)'u', unchecked((byte)(sbyte)0xF0), unchecked((byte)(sbyte)0x9D), unchecked((byte)(sbyte)0x84), unchecked((byte)(sbyte)0x9E), (byte)'c', (byte)'e', unchecked((byte)(sbyte)0xF0), unchecked((byte)(sbyte)0x9D), unchecked((byte)(sbyte)0x85), unchecked((byte)(sbyte)0xA0), (byte)'n', (byte)'e', 0x01, 0x00, 0x08, (byte)'L', (byte)'u', 0x00, (byte)'c', (byte)'e', 0x00, (byte)'n', (byte)'e', unchecked((byte)0xFF), unchecked((byte)(sbyte)0xFF), unchecked((byte)(sbyte)0xFF), unchecked((byte)(sbyte)0xFF), (byte)0x17, (byte)0x01, unchecked((byte)(sbyte)0xFF), unchecked((byte)(sbyte)0xFF), unchecked((byte)(sbyte)0xFF), unchecked((byte)(sbyte)0xFF), unchecked((byte)(sbyte)0xFF), unchecked((byte)(sbyte)0xFF), unchecked((byte)(sbyte)0xFF), unchecked((byte)(sbyte)0xFF), unchecked((byte)(sbyte)0xFF), 0x01 };

        internal static readonly int COUNT = RANDOM_MULTIPLIER * 65536;
        internal static int[] INTS;
        internal static long[] LONGS;
        internal static byte[] RANDOM_TEST_BYTES;

        [OneTimeSetUp]
        public static void BeforeClass()
        {
            Random random = Random();
            INTS = new int[COUNT];
            LONGS = new long[COUNT];
            RANDOM_TEST_BYTES = new byte[COUNT * (5 + 4 + 9 + 8)];
            ByteArrayDataOutput bdo = new ByteArrayDataOutput(RANDOM_TEST_BYTES);
            for (int i = 0; i < COUNT; i++)
            {
                int i1 = INTS[i] = random.Next();
                bdo.WriteVInt(i1);
                bdo.WriteInt(i1);

                long l1;
                if (Rarely())
                {
                    // a long with lots of zeroes at the end
                    l1 = LONGS[i] = TestUtil.NextLong(random, 0, int.MaxValue) << 32;
                }
                else
                {
                    l1 = LONGS[i] = TestUtil.NextLong(random, 0, long.MaxValue);
                }
                bdo.WriteVLong(l1);
                bdo.WriteLong(l1);
            }
        }

        [OneTimeTearDown]
        public static void AfterClass()
        {
            INTS = null;
            LONGS = null;
            RANDOM_TEST_BYTES = null;
        }

        private void CheckReads(DataInput @is, Type expectedEx)
        {
            Assert.AreEqual(128, @is.ReadVInt());
            Assert.AreEqual(16383, @is.ReadVInt());
            Assert.AreEqual(16384, @is.ReadVInt());
            Assert.AreEqual(16385, @is.ReadVInt());
            Assert.AreEqual(int.MaxValue, @is.ReadVInt());
            Assert.AreEqual(-1, @is.ReadVInt());
            Assert.AreEqual((long)int.MaxValue, @is.ReadVLong());
            Assert.AreEqual(long.MaxValue, @is.ReadVLong());
            Assert.AreEqual("Lucene", @is.ReadString());

            Assert.AreEqual("\u00BF", @is.ReadString());
            Assert.AreEqual("Lu\u00BFce\u00BFne", @is.ReadString());

            Assert.AreEqual("\u2620", @is.ReadString());
            Assert.AreEqual("Lu\u2620ce\u2620ne", @is.ReadString());

            Assert.AreEqual("\uD834\uDD1E", @is.ReadString());
            Assert.AreEqual("\uD834\uDD1E\uD834\uDD60", @is.ReadString());
            Assert.AreEqual("Lu\uD834\uDD1Ece\uD834\uDD60ne", @is.ReadString());

            Assert.AreEqual("\u0000", @is.ReadString());
            Assert.AreEqual("Lu\u0000ce\u0000ne", @is.ReadString());

            try
            {
                @is.ReadVInt();
                Assert.Fail("Should throw " + expectedEx.Name);
            }
            catch (Exception e)
            {
                Assert.IsTrue(e.Message.StartsWith("Invalid vInt"));
                Assert.IsTrue(expectedEx.IsInstanceOfType(e));
            }
            Assert.AreEqual(1, @is.ReadVInt()); // guard value

            try
            {
                @is.ReadVLong();
                Assert.Fail("Should throw " + expectedEx.Name);
            }
            catch (Exception e)
            {
                Assert.IsTrue(e.Message.StartsWith("Invalid vLong"));
                Assert.IsTrue(expectedEx.IsInstanceOfType(e));
            }
            Assert.AreEqual(1L, @is.ReadVLong()); // guard value
        }

        private void CheckRandomReads(DataInput @is)
        {
            for (int i = 0; i < COUNT; i++)
            {
                Assert.AreEqual(INTS[i], @is.ReadVInt());
                Assert.AreEqual(INTS[i], @is.ReadInt());
                Assert.AreEqual(LONGS[i], @is.ReadVLong());
                Assert.AreEqual(LONGS[i], @is.ReadLong());
            }
        }

        // this test only checks BufferedIndexInput because MockIndexInput extends BufferedIndexInput
        [Test]
        public virtual void TestBufferedIndexInputRead()
        {
            IndexInput @is = new MockIndexInput(READ_TEST_BYTES);
            CheckReads(@is, typeof(IOException));
            @is.Dispose();
            @is = new MockIndexInput(RANDOM_TEST_BYTES);
            CheckRandomReads(@is);
            @is.Dispose();
        }

        // this test checks the raw IndexInput methods as it uses RAMIndexInput which extends IndexInput directly
        [Test]
        public virtual void TestRawIndexInputRead()
        {
            Random random = Random();
            RAMDirectory dir = new RAMDirectory();
            IndexOutput os = dir.CreateOutput("foo", NewIOContext(random));
            os.WriteBytes(READ_TEST_BYTES, READ_TEST_BYTES.Length);
            os.Dispose();
            IndexInput @is = dir.OpenInput("foo", NewIOContext(random));
            CheckReads(@is, typeof(IOException));
            @is.Dispose();

            os = dir.CreateOutput("bar", NewIOContext(random));
            os.WriteBytes(RANDOM_TEST_BYTES, RANDOM_TEST_BYTES.Length);
            os.Dispose();
            @is = dir.OpenInput("bar", NewIOContext(random));
            CheckRandomReads(@is);
            @is.Dispose();
            dir.Dispose();
        }

        [Test]
        public virtual void TestByteArrayDataInput()
        {
            ByteArrayDataInput @is = new ByteArrayDataInput((byte[])(Array)READ_TEST_BYTES);
            CheckReads(@is, typeof(Exception));
            @is = new ByteArrayDataInput(RANDOM_TEST_BYTES);
            CheckRandomReads(@is);
        }
    }
}