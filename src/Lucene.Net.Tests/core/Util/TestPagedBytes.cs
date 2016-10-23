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

using Lucene.Net.Attributes;
using Lucene.Net.Randomized.Generators;
using Lucene.Net.Support;
using NUnit.Framework;
using System;

namespace Lucene.Net.Util
{
    using BaseDirectoryWrapper = Lucene.Net.Store.BaseDirectoryWrapper;
    using DataInput = Lucene.Net.Store.DataInput;
    using DataOutput = Lucene.Net.Store.DataOutput;
    using IndexInput = Lucene.Net.Store.IndexInput;
    using IndexOutput = Lucene.Net.Store.IndexOutput;
    using IOContext = Lucene.Net.Store.IOContext;
    using MockDirectoryWrapper = Lucene.Net.Store.MockDirectoryWrapper;

    [TestFixture]
    public class TestPagedBytes : LuceneTestCase
    {
        // Writes random byte/s to "normal" file in dir, then
        // copies into PagedBytes and verifies with
        // PagedBytes.Reader:
        [Test, LongRunningTest]
        public virtual void TestDataInputOutput()
        {
            Random random = Random();
            for (int iter = 0; iter < 5 * RANDOM_MULTIPLIER; iter++)
            {
                BaseDirectoryWrapper dir = NewFSDirectory(CreateTempDir("testOverflow"));
                if (dir is MockDirectoryWrapper)
                {
                    ((MockDirectoryWrapper)dir).Throttling = MockDirectoryWrapper.Throttling_e.NEVER;
                }
                int blockBits = TestUtil.NextInt(random, 1, 20);
                int blockSize = 1 << blockBits;
                PagedBytes p = new PagedBytes(blockBits);
                IndexOutput @out = dir.CreateOutput("foo", IOContext.DEFAULT);
                int numBytes = TestUtil.NextInt(Random(), 2, 10000000);

                byte[] answer = new byte[numBytes];
                Random().NextBytes(answer);
                int written = 0;
                while (written < numBytes)
                {
                    if (Random().Next(10) == 7)
                    {
                        @out.WriteByte(answer[written++]);
                    }
                    else
                    {
                        int chunk = Math.Min(Random().Next(1000), numBytes - written);
                        @out.WriteBytes(answer, written, chunk);
                        written += chunk;
                    }
                }

                @out.Dispose();
                IndexInput input = dir.OpenInput("foo", IOContext.DEFAULT);
                DataInput @in = (DataInput)input.Clone();

                p.Copy(input, input.Length());
                PagedBytes.Reader reader = p.Freeze(random.NextBoolean());

                byte[] verify = new byte[numBytes];
                int read = 0;
                while (read < numBytes)
                {
                    if (Random().Next(10) == 7)
                    {
                        verify[read++] = @in.ReadByte();
                    }
                    else
                    {
                        int chunk = Math.Min(Random().Next(1000), numBytes - read);
                        @in.ReadBytes(verify, read, chunk);
                        read += chunk;
                    }
                }
                Assert.IsTrue(Arrays.Equals(answer, verify));

                BytesRef slice = new BytesRef();
                for (int iter2 = 0; iter2 < 100; iter2++)
                {
                    int pos = random.Next(numBytes - 1);
                    int len = random.Next(Math.Min(blockSize + 1, numBytes - pos));
                    reader.FillSlice(slice, pos, len);
                    for (int byteUpto = 0; byteUpto < len; byteUpto++)
                    {
                        Assert.AreEqual(answer[pos + byteUpto], (byte)slice.Bytes[slice.Offset + byteUpto]);
                    }
                }
                input.Dispose();
                dir.Dispose();
            }
        }

        // Writes random byte/s into PagedBytes via
        // .getDataOutput(), then verifies with
        // PagedBytes.getDataInput():
        [Test, LongRunningTest]
        public virtual void TestDataInputOutput2()
        {
            Random random = Random();
            for (int iter = 0; iter < 5 * RANDOM_MULTIPLIER; iter++)
            {
                int blockBits = TestUtil.NextInt(random, 1, 20);
                int blockSize = 1 << blockBits;
                PagedBytes p = new PagedBytes(blockBits);
                DataOutput @out = p.DataOutput;
                int numBytes = Random().Next(10000000);

                byte[] answer = new byte[numBytes];
                Random().NextBytes(answer);
                int written = 0;
                while (written < numBytes)
                {
                    if (Random().Next(10) == 7)
                    {
                        @out.WriteByte(answer[written++]);
                    }
                    else
                    {
                        int chunk = Math.Min(Random().Next(1000), numBytes - written);
                        @out.WriteBytes(answer, written, chunk);
                        written += chunk;
                    }
                }

                PagedBytes.Reader reader = p.Freeze(random.NextBoolean());

                DataInput @in = p.DataInput;

                byte[] verify = new byte[numBytes];
                int read = 0;
                while (read < numBytes)
                {
                    if (Random().Next(10) == 7)
                    {
                        verify[read++] = @in.ReadByte();
                    }
                    else
                    {
                        int chunk = Math.Min(Random().Next(1000), numBytes - read);
                        @in.ReadBytes(verify, read, chunk);
                        read += chunk;
                    }
                }
                Assert.IsTrue(Arrays.Equals(answer, verify));

                BytesRef slice = new BytesRef();
                for (int iter2 = 0; iter2 < 100; iter2++)
                {
                    int pos = random.Next(numBytes - 1);
                    int len = random.Next(Math.Min(blockSize + 1, numBytes - pos));
                    reader.FillSlice(slice, pos, len);
                    for (int byteUpto = 0; byteUpto < len; byteUpto++)
                    {
                        Assert.AreEqual(answer[pos + byteUpto], (byte)slice.Bytes[slice.Offset + byteUpto]);
                    }
                }
            }
        }

        [Ignore("memory hole")] // memory hole
        [Test]
        [LongRunningTest, MaxTime(120000)]
        public virtual void TestOverflow()
        {
            BaseDirectoryWrapper dir = NewFSDirectory(CreateTempDir("testOverflow"));
            if (dir is MockDirectoryWrapper)
            {
                ((MockDirectoryWrapper)dir).Throttling = MockDirectoryWrapper.Throttling_e.NEVER;
            }
            int blockBits = TestUtil.NextInt(Random(), 14, 28);
            int blockSize = 1 << blockBits;
            var arr = new byte[TestUtil.NextInt(Random(), blockSize / 2, blockSize * 2)];
            for (int i = 0; i < arr.Length; ++i)
            {
                arr[i] = (byte)(sbyte)i;
            }
            long numBytes = (1L << 31) + TestUtil.NextInt(Random(), 1, blockSize * 3);
            var p = new PagedBytes(blockBits);
            var @out = dir.CreateOutput("foo", IOContext.DEFAULT);
            for (long i = 0; i < numBytes; )
            {
                Assert.AreEqual(i, @out.FilePointer);
                int len = (int)Math.Min(arr.Length, numBytes - i);
                @out.WriteBytes(arr, len);
                i += len;
            }
            Assert.AreEqual(numBytes, @out.FilePointer);
            @out.Dispose();
            IndexInput @in = dir.OpenInput("foo", IOContext.DEFAULT);
            p.Copy(@in, numBytes);
            PagedBytes.Reader reader = p.Freeze(Random().NextBoolean());

            foreach (long offset in new long[] { 0L, int.MaxValue, numBytes - 1, TestUtil.NextLong(Random(), 1, numBytes - 2) })
            {
                BytesRef b = new BytesRef();
                reader.FillSlice(b, offset, 1);
                Assert.AreEqual(arr[(int)(offset % arr.Length)], b.Bytes[b.Offset]);
            }
            @in.Dispose();
            dir.Dispose();
        }
    }
}