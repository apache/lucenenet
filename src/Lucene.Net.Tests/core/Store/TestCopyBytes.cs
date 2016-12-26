using Lucene.Net.Randomized.Generators;
using Lucene.Net.Support;
using NUnit.Framework;
using System;
using System.IO;

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
    using TestUtil = Lucene.Net.Util.TestUtil;

    [TestFixture]
    public class TestCopyBytes : LuceneTestCase
    {
        private byte Value(int idx)
        {
            return unchecked((byte)((idx % 256) * (1 + (idx / 256))));
        }

        [Test]
        public virtual void TestCopyBytesMem()
        {
            int num = AtLeast(10);
            for (int iter = 0; iter < num; iter++)
            {
                Directory dir = NewDirectory();
                if (VERBOSE)
                {
                    Console.WriteLine("TEST: iter=" + iter + " dir=" + dir);
                }

                // make random file
                IndexOutput @out = dir.CreateOutput("test", NewIOContext(Random()));
                var bytes = new byte[TestUtil.NextInt(Random(), 1, 77777)];
                int size = TestUtil.NextInt(Random(), 1, 1777777);
                int upto = 0;
                int byteUpto = 0;
                while (upto < size)
                {
                    bytes[byteUpto++] = Value(upto);
                    upto++;
                    if (byteUpto == bytes.Length)
                    {
                        @out.WriteBytes(bytes, 0, bytes.Length);
                        byteUpto = 0;
                    }
                }

                @out.WriteBytes(bytes, 0, byteUpto);
                Assert.AreEqual(size, @out.FilePointer);
                @out.Dispose();
                Assert.AreEqual(size, dir.FileLength("test"));

                // copy from test -> test2
                IndexInput @in = dir.OpenInput("test", NewIOContext(Random()));

                @out = dir.CreateOutput("test2", NewIOContext(Random()));

                upto = 0;
                while (upto < size)
                {
                    if (Random().NextBoolean())
                    {
                        @out.WriteByte(@in.ReadByte());
                        upto++;
                    }
                    else
                    {
                        int chunk = Math.Min(TestUtil.NextInt(Random(), 1, bytes.Length), size - upto);
                        @out.CopyBytes(@in, chunk);
                        upto += chunk;
                    }
                }
                Assert.AreEqual(size, upto);
                @out.Dispose();
                @in.Dispose();

                // verify
                IndexInput in2 = dir.OpenInput("test2", NewIOContext(Random()));
                upto = 0;
                while (upto < size)
                {
                    if (Random().NextBoolean())
                    {
                        var v = in2.ReadByte();
                        Assert.AreEqual(Value(upto), v);
                        upto++;
                    }
                    else
                    {
                        int limit = Math.Min(TestUtil.NextInt(Random(), 1, bytes.Length), size - upto);
                        in2.ReadBytes(bytes, 0, limit);
                        for (int byteIdx = 0; byteIdx < limit; byteIdx++)
                        {
                            Assert.AreEqual(Value(upto), bytes[byteIdx]);
                            upto++;
                        }
                    }
                }
                in2.Dispose();

                dir.DeleteFile("test");
                dir.DeleteFile("test2");

                dir.Dispose();
            }
        }

        // LUCENE-3541
        [Test]
        public virtual void TestCopyBytesWithThreads()
        {
            int datalen = TestUtil.NextInt(Random(), 101, 10000);
            byte[] data = new byte[datalen];
            Random().NextBytes(data);

            Directory d = NewDirectory();
            IndexOutput output = d.CreateOutput("data", IOContext.DEFAULT);
            output.WriteBytes(data, 0, datalen);
            output.Dispose();

            IndexInput input = d.OpenInput("data", IOContext.DEFAULT);
            IndexOutput outputHeader = d.CreateOutput("header", IOContext.DEFAULT);
            // copy our 100-byte header
            outputHeader.CopyBytes(input, 100);
            outputHeader.Dispose();

            // now make N copies of the remaining bytes
            CopyThread[] copies = new CopyThread[10];
            for (int i = 0; i < copies.Length; i++)
            {
                copies[i] = new CopyThread((IndexInput)input.Clone(), d.CreateOutput("copy" + i, IOContext.DEFAULT));
            }

            for (int i = 0; i < copies.Length; i++)
            {
                copies[i].Start();
            }

            for (int i = 0; i < copies.Length; i++)
            {
                copies[i].Join();
            }

            for (int i = 0; i < copies.Length; i++)
            {
                IndexInput copiedData = d.OpenInput("copy" + i, IOContext.DEFAULT);
                byte[] dataCopy = new byte[datalen];
                System.Buffer.BlockCopy(data, 0, dataCopy, 0, 100); // copy the header for easy testing
                copiedData.ReadBytes(dataCopy, 100, datalen - 100);
                Assert.AreEqual(data, dataCopy);
                copiedData.Dispose();
            }
            input.Dispose();
            d.Dispose();
        }

        internal class CopyThread : ThreadClass
        {
            internal readonly IndexInput Src;
            internal readonly IndexOutput Dst;

            internal CopyThread(IndexInput src, IndexOutput dst)
            {
                this.Src = src;
                this.Dst = dst;
            }

            public override void Run()
            {
                try
                {
                    Dst.CopyBytes(Src, Src.Length - 100);
                    Dst.Dispose();
                }
                catch (IOException ex)
                {
                    throw new Exception(ex.Message, ex);
                }
            }
        }
    }
}