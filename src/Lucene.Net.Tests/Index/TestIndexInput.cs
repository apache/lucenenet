using NUnit.Framework;
using System;
using System.IO;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Index
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

    using ByteArrayDataInput = Lucene.Net.Store.ByteArrayDataInput;
    using ByteArrayDataOutput = Lucene.Net.Store.ByteArrayDataOutput;
    using DataInput = Lucene.Net.Store.DataInput;
    using IndexInput = Lucene.Net.Store.IndexInput;
    using IndexOutput = Lucene.Net.Store.IndexOutput;
    using LuceneTestCase = Lucene.Net.Util.LuceneTestCase;
    using RAMDirectory = Lucene.Net.Store.RAMDirectory;
    using TestUtil = Lucene.Net.Util.TestUtil;

    [TestFixture]
    public class TestIndexInput : LuceneTestCase
    {
        internal static readonly byte[] READ_TEST_BYTES = new byte[] {
            (byte) 0x80, 0x01,
            (byte) 0xFF, 0x7F,
            (byte) 0x80, (byte) 0x80, 0x01,
            (byte) 0x81, (byte) 0x80, 0x01,
            (byte) 0xFF, (byte) 0xFF, (byte) 0xFF, (byte) 0xFF, (byte) 0x07,
            (byte) 0xFF, (byte) 0xFF, (byte) 0xFF, (byte) 0xFF, (byte) 0x0F,
            (byte) 0xFF, (byte) 0xFF, (byte) 0xFF, (byte) 0xFF, (byte) 0x07,
            (byte) 0xFF, (byte) 0xFF, (byte) 0xFF, (byte) 0xFF, (byte) 0xFF, (byte) 0xFF, (byte) 0xFF, (byte) 0xFF, (byte) 0x7F,
            0x06, (byte) 'L', (byte) 'u', (byte) 'c', (byte) 'e', (byte) 'n', (byte) 'e',

            // 2-byte UTF-8 (U+00BF "INVERTED QUESTION MARK") 
            0x02, (byte) 0xC2, (byte) 0xBF,
            0x0A, (byte) 'L', (byte) 'u', (byte) 0xC2, (byte) 0xBF,
                  (byte) 'c', (byte) 'e', (byte) 0xC2, (byte) 0xBF,
                  (byte) 'n', (byte) 'e',

            // 3-byte UTF-8 (U+2620 "SKULL AND CROSSBONES") 
            0x03, (byte) 0xE2, (byte) 0x98, (byte) 0xA0,
            0x0C, (byte) 'L', (byte) 'u', (byte) 0xE2, (byte) 0x98, (byte) 0xA0,
                  (byte) 'c', (byte) 'e', (byte) 0xE2, (byte) 0x98, (byte) 0xA0,
                  (byte) 'n', (byte) 'e',

            // surrogate pairs
            // (U+1D11E "MUSICAL SYMBOL G CLEF")
            // (U+1D160 "MUSICAL SYMBOL EIGHTH NOTE")
            0x04, (byte) 0xF0, (byte) 0x9D, (byte) 0x84, (byte) 0x9E,
            0x08, (byte) 0xF0, (byte) 0x9D, (byte) 0x84, (byte) 0x9E,
                  (byte) 0xF0, (byte) 0x9D, (byte) 0x85, (byte) 0xA0,
            0x0E, (byte) 'L', (byte) 'u',
                  (byte) 0xF0, (byte) 0x9D, (byte) 0x84, (byte) 0x9E,
                  (byte) 'c', (byte) 'e',
                  (byte) 0xF0, (byte) 0x9D, (byte) 0x85, (byte) 0xA0,
                  (byte) 'n', (byte) 'e',  

            // null bytes
            0x01, 0x00,
            0x08, (byte) 'L', (byte) 'u', 0x00, (byte) 'c', (byte) 'e', 0x00, (byte) 'n', (byte) 'e',
    
            // tests for Exceptions on invalid values
            (byte) 0xFF, (byte) 0xFF, (byte) 0xFF, (byte) 0xFF, (byte) 0x17,
            (byte) 0x01, // guard value
            (byte) 0xFF, (byte) 0xFF, (byte) 0xFF, (byte) 0xFF, (byte) 0xFF, (byte) 0xFF, (byte) 0xFF, (byte) 0xFF, (byte) 0xFF,
            (byte) 0x01, // guard value
        };

        internal static readonly int COUNT = RandomMultiplier * 65536;
        internal static int[] INTS;
        internal static long[] LONGS;
        internal static byte[] RANDOM_TEST_BYTES;

        [OneTimeSetUp]
        public override void BeforeClass()
        {
            base.BeforeClass();

            Random random = Random;
            INTS = new int[COUNT];
            LONGS = new long[COUNT];
            RANDOM_TEST_BYTES = new byte[COUNT * (5 + 4 + 9 + 8)];
            ByteArrayDataOutput bdo = new ByteArrayDataOutput(RANDOM_TEST_BYTES);
            for (int i = 0; i < COUNT; i++)
            {
                int i1 = INTS[i] = random.Next();
                bdo.WriteVInt32(i1);
                bdo.WriteInt32(i1);

                long l1;
                if (Rarely())
                {
                    // a long with lots of zeroes at the end
                    l1 = LONGS[i] = TestUtil.NextInt64(random, 0, int.MaxValue) << 32;
                }
                else
                {
                    l1 = LONGS[i] = TestUtil.NextInt64(random, 0, long.MaxValue);
                }
                bdo.WriteVInt64(l1);
                bdo.WriteInt64(l1);
            }
        }

        [OneTimeTearDown]
        public override void AfterClass()
        {
            INTS = null;
            LONGS = null;
            RANDOM_TEST_BYTES = null;
            base.AfterClass();
        }

        private void CheckReads(DataInput @is, Type expectedEx)
        {
            Assert.AreEqual(128, @is.ReadVInt32());
            Assert.AreEqual(16383, @is.ReadVInt32());
            Assert.AreEqual(16384, @is.ReadVInt32());
            Assert.AreEqual(16385, @is.ReadVInt32());
            Assert.AreEqual(int.MaxValue, @is.ReadVInt32());
            Assert.AreEqual(-1, @is.ReadVInt32());
            Assert.AreEqual((long)int.MaxValue, @is.ReadVInt64());
            Assert.AreEqual(long.MaxValue, @is.ReadVInt64());
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
                @is.ReadVInt32();
                Assert.Fail("Should throw " + expectedEx.Name);
            }
            catch (Exception e) when (e.IsException())
            {
                Assert.IsTrue(e.Message.StartsWith("Invalid VInt32", StringComparison.Ordinal));
                Assert.IsTrue(expectedEx.IsInstanceOfType(e));
            }
            Assert.AreEqual(1, @is.ReadVInt32()); // guard value

            try
            {
                @is.ReadVInt64();
                Assert.Fail("Should throw " + expectedEx.Name);
            }
            catch (Exception e) when (e.IsException())
            {
                Assert.IsTrue(e.Message.StartsWith("Invalid VInt64", StringComparison.Ordinal));
                Assert.IsTrue(expectedEx.IsInstanceOfType(e));
            }
            Assert.AreEqual(1L, @is.ReadVInt64()); // guard value
        }

        private void CheckRandomReads(DataInput @is)
        {
            for (int i = 0; i < COUNT; i++)
            {
                Assert.AreEqual(INTS[i], @is.ReadVInt32());
                Assert.AreEqual(INTS[i], @is.ReadInt32());
                Assert.AreEqual(LONGS[i], @is.ReadVInt64());
                Assert.AreEqual(LONGS[i], @is.ReadInt64());
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
            Random random = Random;
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
            ByteArrayDataInput @is = new ByteArrayDataInput(READ_TEST_BYTES);
            CheckReads(@is, typeof(Exception));
            @is = new ByteArrayDataInput(RANDOM_TEST_BYTES);
            CheckRandomReads(@is);
        }
    }
}