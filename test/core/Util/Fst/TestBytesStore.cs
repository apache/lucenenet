using System;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Util;
using Lucene.Net.Util.Fst;
using NUnit.Framework;
using Lucene.Net.Randomized.Generators;

namespace Lucene.Net.Test.Util.Fst
{
    [TestFixture]
    public class TestBytesStore : LuceneTestCase
    {

        
        [Test]
        public void TestRandom()
        {

            int iters = AtLeast(10);

            for (var iter = 0; iter < iters; iter++)
            {
                int numBytes = new Random().NextIntBetween(1, 200000);
                var expected = new byte[numBytes];
                int blockBits = new Random().NextIntBetween(8, 15);
                var bytes = new BytesStore(blockBits);
                if (VERBOSE)
                {
                    Console.WriteLine("TEST: iter=" + iter + " numBytes=" + numBytes + " blockBits=" + blockBits);
                }

                var pos = 0;
                while (pos < numBytes)
                {
                    int op = new Random().Next(8);
                    if (VERBOSE)
                    {
                        Console.WriteLine("  cycle pos=" + pos);
                    }
                    switch (op)
                    {

                        case 0:
                            {
                                // write random byte
                                var b = (byte)new Random().Next(256);
                                if (VERBOSE)
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
                                var len = new Random().Next(Math.Min(numBytes - pos, 100));
                                var temp = new byte[len];
                                new Random().NextBytes(temp);
                                if (VERBOSE)
                                {
                                    Console.WriteLine("    writeBytes len=" + len + " bytes=" + Arrays.ToString(temp));
                                }
                                Array.Copy(temp, 0, expected, pos, temp.Length);
                                bytes.WriteBytes(temp, 0, temp.Length);
                                pos += len;
                            }
                            break;

                        case 2:
                            {
                                // write int @ absolute pos
                                if (pos > 4)
                                {
                                    int x = new Random().Next();
                                    int randomPos = new Random().Next(pos - 4);
                                    if (VERBOSE)
                                    {
                                        Console.WriteLine("    abs writeInt pos=" + randomPos + " x=" + x);
                                    }
                                    bytes.WriteInt(randomPos, x);
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
                                    int len = new Random().NextIntBetween(2, Math.Min(100, pos));
                                    int start;
                                    if (len == pos)
                                    {
                                        start = 0;
                                    }
                                    else
                                    {
                                        start = new Random().Next(pos - len);
                                    }
                                    var end = start + len - 1;
                                    if (VERBOSE)
                                    {
                                        Console.WriteLine("    reverse start=" + start + " end=" + end + " len=" + len + " pos=" + pos);
                                    }
                                    bytes.Reverse(start, end);

                                    while (start <= end)
                                    {
                                        var b = expected[end];
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
                                    int randomPos = new Random().Next(pos - 1);
                                    int len = new Random().NextIntBetween(1, Math.Min(pos - randomPos - 1, 100));
                                    byte[] temp = new byte[len];
                                    new Random().NextBytes(temp);
                                    if (VERBOSE)
                                    {
                                        Console.WriteLine("    abs writeBytes pos=" + randomPos + " len=" + len + " bytes=" + Arrays.ToString(temp));
                                    }
                                    Array.Copy(temp, 0, expected, randomPos, temp.Length);
                                    bytes.WriteBytes(randomPos, temp, 0, temp.Length);
                                }
                            }
                            break;

                        case 5:
                            {
                                // copyBytes
                                if (pos > 1)
                                {
                                    int src = new Random().Next(pos - 1);
                                    int dest = new Random().NextIntBetween(src + 1, pos - 1);
                                    int len = new Random().NextIntBetween( 1, Math.Min(300, pos - dest));
                                    if (VERBOSE)
                                    {
                                        Console.WriteLine("    copyBytes src=" + src + " dest=" + dest + " len=" + len);
                                    }
                                    Array.Copy(expected, src, expected, dest, len);
                                    bytes.CopyBytes(src, dest, len);
                                }
                            }
                            break;

                        case 6:
                            {
                                // skip
                                var len = new Random().Next(Math.Min(100, numBytes - pos));

                                if (VERBOSE)
                                {
                                    Console.WriteLine("    skip len=" + len);
                                }

                                pos += len;
                                bytes.SkipBytes(len);

                                // NOTE: must fill in zeros in case truncate was
                                // used, else we get false fails:
                                if (len > 0)
                                {
                                    var zeros = new byte[len];
                                    bytes.WriteBytes(pos - len, zeros, 0, len);
                                }
                            }
                            break;

                        case 7:
                            {
                                // absWriteByte
                                if (pos > 0)
                                {
                                    var dest = new Random().Next(pos);
                                    var b = (byte)new Random().Next(256);
                                    expected[dest] = b;
                                    bytes.WriteByte(dest, b);
                                }
                                break;
                            }
                    }

                    Assert.AreEqual(pos, bytes.GetPosition());

                    if (pos > 0 && new Random().Next(50) == 17)
                    {
                        // truncate
                        int len = new Random().NextIntBetween(1, Math.Min(pos, 100));
                        bytes.Truncate(pos - len);
                        pos -= len;
                        Arrays.Fill(expected, pos, pos + len, (byte)0);
                        if (VERBOSE)
                        {
                            Console.WriteLine("    truncate len=" + len + " newPos=" + pos);
                        }
                    }

                    if ((pos > 0 && new Random().Next(200) == 17))
                    {
                        Verify(bytes, expected, pos);
                    }
                }

                BytesStore bytesToVerify;

                if (new Random().NextBoolean())
                {
                    if (VERBOSE)
                    {
                        Console.WriteLine("TEST: save/load  bytes");
                    }
                    Directory dir = NewDirectory();
                    var output = dir.CreateOutput("bytes", IOContext.DEFAULT);
                    bytes.WriteTo(output);
                    output.Dispose();
                    var input = dir.OpenInput("bytes", IOContext.DEFAULT);
                    bytesToVerify = new BytesStore(input, numBytes, new Random().NextIntBetween(256, int.MaxValue));
                    input.Dispose();
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
            Assert.AreEqual(totalLength, bytes.GetPosition());
            if (totalLength == 0)
            {
                return;
            }
            if (VERBOSE)
            {
                Console.WriteLine("  verify...");
            }

            // First verify whole thing in one blast:
            var actual = new byte[totalLength];
            if (new Random().NextBoolean())
            {
                if (VERBOSE)
                {
                    Console.WriteLine("    bulk: reversed");
                }
                // reversed
                var reverseReader = bytes.GetReverseReader();
                Assert.IsTrue(reverseReader.Reversed());
                reverseReader.Position = totalLength - 1;
                reverseReader.ReadBytes(actual, 0, actual.Length);
                var start = 0;
                var end = totalLength - 1;
                while (start < end)
                {
                    var b = actual[start];
                    actual[start] = actual[end];
                    actual[end] = b;
                    start++;
                    end--;
                }
            }
            else
            {
                // forward
                if (VERBOSE)
                {
                    Console.WriteLine("    bulk: forward");
                }
                var forwardReader = bytes.GetForwardReader();
                Assert.IsFalse(forwardReader.Reversed());
                forwardReader.ReadBytes(actual, 0, actual.Length);
            }

            for (int i = 0; i < totalLength; i++)
            {
                Assert.AreEqual(expected[i], actual[i], "byte @ index=" + i);
            }

            FST.BytesReader r;

            // Then verify ops:
            bool reversed = new Random().NextBoolean();
            if (reversed)
            {
                if (VERBOSE)
                {
                    Console.WriteLine("    ops: reversed");
                }
                r = bytes.GetReverseReader();
            }
            else
            {
                if (VERBOSE)
                {
                    Console.WriteLine("    ops: forward");
                }
                r = bytes.GetForwardReader();
            }

            if (totalLength > 1)
            {
                int numOps = new Random().NextIntBetween(100, 200);
                for (int op = 0; op < numOps; op++)
                {

                    int numBytes = new Random().Next(Math.Min(1000, totalLength - 1));
                    int pos;
                    if (reversed)
                    {
                        pos = new Random().NextIntBetween(numBytes, totalLength - 1);
                    }
                    else
                    {
                        pos = new Random().Next(totalLength - numBytes);
                    }
                    if (VERBOSE)
                    {
                        Console.WriteLine("    op iter=" + op + " reversed=" + reversed + " numBytes=" + numBytes + " pos=" + pos);
                    }
                    var temp = new sbyte[numBytes];
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
                        Assert.AreEqual(expectedByte, temp[i], "byte @ index=" + i);
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
                        int skipBytes = new Random().Next(left - 4);

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

                        if (VERBOSE)
                        {
                            Console.WriteLine("    skip numBytes=" + skipBytes);
                            Console.WriteLine("    readInt");
                        }

                        r.SkipBytes(skipBytes);
                        Assert.AreEqual(expectedInt, r.ReadInt());
                    }
                }
            }
        }
    }
}
