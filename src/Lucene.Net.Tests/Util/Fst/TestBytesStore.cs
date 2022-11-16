using Lucene.Net.Support;
using NUnit.Framework;
using RandomizedTesting.Generators;
using System;
using Assert = Lucene.Net.TestFramework.Assert;
using Console = Lucene.Net.Util.SystemConsole;

namespace Lucene.Net.Util.Fst
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

    using Directory = Lucene.Net.Store.Directory;
    using IndexInput = Lucene.Net.Store.IndexInput;
    using IndexOutput = Lucene.Net.Store.IndexOutput;
    using IOContext = Lucene.Net.Store.IOContext;

    [TestFixture]
    public class TestBytesStore : LuceneTestCase
    {

        [Test]
        [Slow]
        public virtual void TestRandom()
        {

            int iters = AtLeast(10);
            for (int iter = 0; iter < iters; iter++)
            {
                int numBytes = TestUtil.NextInt32(Random, 1, 200000);
                byte[] expected = new byte[numBytes];
                int blockBits = TestUtil.NextInt32(Random, 8, 15);
                BytesStore bytes = new BytesStore(blockBits);
                if (Verbose)
                {
                    Console.WriteLine("TEST: iter=" + iter + " numBytes=" + numBytes + " blockBits=" + blockBits);
                }

                int pos = 0;
                while (pos < numBytes)
                {
                    int op = Random.Next(8);
                    if (Verbose)
                    {
                        Console.WriteLine("  cycle pos=" + pos);
                    }
                    switch (op)
                    {

                        case 0:
                            {
                                // write random byte
                                byte b = (byte)Random.Next(256);
                                if (Verbose)
                                {
                                    Console.WriteLine("    writeByte b=" + b);
                                }

                                expected[pos++] = b;
                                bytes.WriteByte(b);
                            }
                            break;

                        case 1:
                            {
                                // write random byte[]
                                int len = Random.Next(Math.Min(numBytes - pos, 100));
                                byte[] temp = new byte[len];
                                Random.NextBytes(temp);
                                if (Verbose)
                                {
                                    Console.WriteLine("    writeBytes len=" + len + " bytes=" + Arrays.ToString(temp));
                                }
                                Arrays.Copy(temp, 0, expected, pos, temp.Length);
                                bytes.WriteBytes(temp, 0, temp.Length);
                                pos += len;
                            }
                            break;

                        case 2:
                            {
                                // write int @ absolute pos
                                if (pos > 4)
                                {
                                    int x = Random.Next();
                                    int randomPos = Random.Next(pos - 4);
                                    if (Verbose)
                                    {
                                        Console.WriteLine("    abs writeInt pos=" + randomPos + " x=" + x);
                                    }
                                    bytes.WriteInt32(randomPos, x);
                                    expected[randomPos++] = (byte)(x >> 24);
                                    expected[randomPos++] = (byte)(x >> 16);
                                    expected[randomPos++] = (byte)(x >> 8);
                                    expected[randomPos++] = (byte)x;
                                }
                            }
                            break;

                        case 3:
                            {
                                // reverse bytes
                                if (pos > 1)
                                {
                                    int len = TestUtil.NextInt32(Random, 2, Math.Min(100, pos));
                                    int start;
                                    if (len == pos)
                                    {
                                        start = 0;
                                    }
                                    else
                                    {
                                        start = Random.Next(pos - len);
                                    }
                                    int end = start + len - 1;
                                    if (Verbose)
                                    {
                                        Console.WriteLine("    reverse start=" + start + " end=" + end + " len=" + len + " pos=" + pos);
                                    }
                                    bytes.Reverse(start, end);

                                    while (start <= end)
                                    {
                                        byte b = expected[end];
                                        expected[end] = expected[start];
                                        expected[start] = b;
                                        start++;
                                        end--;
                                    }
                                }
                            }
                            break;

                        case 4:
                            {
                                // abs write random byte[]
                                if (pos > 2)
                                {
                                    int randomPos = Random.Next(pos - 1);
                                    int len = TestUtil.NextInt32(Random, 1, Math.Min(pos - randomPos - 1, 100));
                                    byte[] temp = new byte[len];
                                    Random.NextBytes(temp);
                                    if (Verbose)
                                    {
                                        Console.WriteLine("    abs writeBytes pos=" + randomPos + " len=" + len + " bytes=" + Arrays.ToString(temp));
                                    }
                                    Arrays.Copy(temp, 0, expected, randomPos, temp.Length);
                                    bytes.WriteBytes(randomPos, temp, 0, temp.Length);
                                }
                            }
                            break;

                        case 5:
                            {
                                // copyBytes
                                if (pos > 1)
                                {
                                    int src = Random.Next(pos - 1);
                                    int dest = TestUtil.NextInt32(Random, src + 1, pos - 1);
                                    int len = TestUtil.NextInt32(Random, 1, Math.Min(300, pos - dest));
                                    if (Verbose)
                                    {
                                        Console.WriteLine("    copyBytes src=" + src + " dest=" + dest + " len=" + len);
                                    }
                                    Arrays.Copy(expected, src, expected, dest, len);
                                    bytes.CopyBytes(src, dest, len);
                                }
                            }
                            break;

                        case 6:
                            {
                                // skip
                                int len = Random.Next(Math.Min(100, numBytes - pos));

                                if (Verbose)
                                {
                                    Console.WriteLine("    skip len=" + len);
                                }

                                pos += len;
                                bytes.SkipBytes(len);

                                // NOTE: must fill in zeros in case truncate was
                                // used, else we get false fails:
                                if (len > 0)
                                {
                                    byte[] zeros = new byte[len];
                                    bytes.WriteBytes(pos - len, zeros, 0, len);
                                }
                            }
                            break;

                        case 7:
                            {
                                // absWriteByte
                                if (pos > 0)
                                {
                                    int dest = Random.Next(pos);
                                    byte b = (byte)Random.Next(256);
                                    expected[dest] = b;
                                    bytes.WriteByte(dest, b);
                                }
                                break;
                            }
                    }

                    Assert.AreEqual(pos, bytes.Position);

                    if (pos > 0 && Random.Next(50) == 17)
                    {
                        // truncate
                        int len = TestUtil.NextInt32(Random, 1, Math.Min(pos, 100));
                        bytes.Truncate(pos - len);
                        pos -= len;
                        Arrays.Fill(expected, pos, pos + len, (byte)0);
                        if (Verbose)
                        {
                            Console.WriteLine("    truncate len=" + len + " newPos=" + pos);
                        }
                    }

                    if ((pos > 0 && Random.Next(200) == 17))
                    {
                        Verify(bytes, expected, pos);
                    }
                }

                BytesStore bytesToVerify;

                if (Random.NextBoolean())
                {
                    if (Verbose)
                    {
                        Console.WriteLine("TEST: save/load final bytes");
                    }
                    Directory dir = NewDirectory();
                    IndexOutput @out = dir.CreateOutput("bytes", IOContext.DEFAULT);
                    bytes.WriteTo(@out);
                    @out.Dispose();
                    IndexInput @in = dir.OpenInput("bytes", IOContext.DEFAULT);
                    bytesToVerify = new BytesStore(@in, numBytes, TestUtil.NextInt32(Random, 256, int.MaxValue));
                    @in.Dispose();
                    dir.Dispose();
                }
                else
                {
                    bytesToVerify = bytes;
                }

                Verify(bytesToVerify, expected, numBytes);
            }
        }

        private void Verify(BytesStore bytes, byte[] expected, int totalLength)
        {
            Assert.AreEqual(totalLength, bytes.Position);
            if (totalLength == 0)
            {
                return;
            }
            if (Verbose)
            {
                Console.WriteLine("  verify...");
            }

            // First verify whole thing in one blast:
            byte[] actual = new byte[totalLength];
            if (Random.NextBoolean())
            {
                if (Verbose)
                {
                    Console.WriteLine("    bulk: reversed");
                }
                // reversed
                FST.BytesReader r2 = bytes.GetReverseReader();
                Assert.IsTrue(r2.IsReversed);
                r2.Position = totalLength - 1;
                r2.ReadBytes(actual, 0, actual.Length);
                int start = 0;
                int end = totalLength - 1;
                while (start < end)
                {
                    byte b = actual[start];
                    actual[start] = actual[end];
                    actual[end] = b;
                    start++;
                    end--;
                }
            }
            else
            {
                // forward
                if (Verbose)
                {
                    Console.WriteLine("    bulk: forward");
                }
                FST.BytesReader r3 = bytes.GetForwardReader();
                Assert.IsFalse(r3.IsReversed);
                r3.ReadBytes(actual, 0, actual.Length);
            }

            for (int i = 0; i < totalLength; i++)
            {
                assertEquals("byte @ index=" + i, expected[i], actual[i]);
            }

            FST.BytesReader r;

            // Then verify ops:
            bool reversed = Random.NextBoolean();
            if (reversed)
            {
                if (Verbose)
                {
                    Console.WriteLine("    ops: reversed");
                }
                r = bytes.GetReverseReader();
            }
            else
            {
                if (Verbose)
                {
                    Console.WriteLine("    ops: forward");
                }
                r = bytes.GetForwardReader();
            }

            if (totalLength > 1)
            {
                int numOps = TestUtil.NextInt32(Random, 100, 200);
                for (int op = 0; op < numOps; op++)
                {

                    int numBytes = Random.Next(Math.Min(1000, totalLength - 1));
                    int pos;
                    if (reversed)
                    {
                        pos = TestUtil.NextInt32(Random, numBytes, totalLength - 1);
                    }
                    else
                    {
                        pos = Random.Next(totalLength - numBytes);
                    }
                    if (Verbose)
                    {
                        Console.WriteLine("    op iter=" + op + " reversed=" + reversed + " numBytes=" + numBytes + " pos=" + pos);
                    }
                    byte[] temp = new byte[numBytes];
                    r.Position = pos;
                    Assert.AreEqual(pos, r.Position);
                    r.ReadBytes(temp, 0, temp.Length);
                    for (int i = 0; i < numBytes; i++)
                    {
                        byte expectedByte;
                        if (reversed)
                        {
                            expectedByte = expected[pos - i];
                        }
                        else
                        {
                            expectedByte = expected[pos + i];
                        }
                        assertEquals("byte @ index=" + i, expectedByte, temp[i]);
                    }

                    int left;
                    int expectedPos;

                    if (reversed)
                    {
                        expectedPos = pos - numBytes;
                        left = (int)r.Position;
                    }
                    else
                    {
                        expectedPos = pos + numBytes;
                        left = (int)(totalLength - r.Position);
                    }
                    Assert.AreEqual(expectedPos, r.Position);

                    if (left > 4)
                    {
                        int skipBytes = Random.Next(left - 4);

                        int expectedInt = 0;
                        if (reversed)
                        {
                            expectedPos -= skipBytes;
                            expectedInt |= (expected[expectedPos--] & 0xFF) << 24;
                            expectedInt |= (expected[expectedPos--] & 0xFF) << 16;
                            expectedInt |= (expected[expectedPos--] & 0xFF) << 8;
                            expectedInt |= (expected[expectedPos--] & 0xFF);
                        }
                        else
                        {
                            expectedPos += skipBytes;
                            expectedInt |= (expected[expectedPos++] & 0xFF) << 24;
                            expectedInt |= (expected[expectedPos++] & 0xFF) << 16;
                            expectedInt |= (expected[expectedPos++] & 0xFF) << 8;
                            expectedInt |= (expected[expectedPos++] & 0xFF);
                        }

                        if (Verbose)
                        {
                            Console.WriteLine("    skip numBytes=" + skipBytes);
                            Console.WriteLine("    readInt");
                        }

                        r.SkipBytes(skipBytes);
                        Assert.AreEqual(expectedInt, r.ReadInt32());
                    }
                }
            }
        }
    }
}