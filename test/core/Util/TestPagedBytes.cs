using System;
using Lucene.Net.Store;
using Lucene.Net.Support;
using Lucene.Net.Test.Support;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Test.Util
{
    [TestFixture]
    public class TestPagedBytes : LuceneTestCase
    {
        [Test]
        public virtual void TestDataInputOutput()
        {
            var random = new Random();
            for (var iter = 0; iter < 5 * RANDOM_MULTIPLIER; iter++)
            {
                var dir = NewFSDirectory(_TestUtil.GetTempDir("testOverflow"));
                if (dir is MockDirectoryWrapper)
                {
                    ((MockDirectoryWrapper)dir).SetThrottling(MockDirectoryWrapper.Throttling.NEVER);
                }
                int blockBits = _TestUtil.Next(random, 1, 20);
                var blockSize = 1 << blockBits;
                var p = new PagedBytes(blockBits);
                var indexOutput = dir.CeateOutput("foo", IOContext.DEFAULT);
                int numBytes = _TestUtil.NextInt(random, 2, 10000000);

                var answer = new byte[numBytes];
                random.NextBytes(answer);
                var written = 0;
                while (written < numBytes)
                {
                    if (random.Next(10) == 7)
                    {
                        indexOutput.WriteByte(answer[written++]);
                    }
                    else
                    {
                        int chunk = Math.Min(random.Next(1000), numBytes - written);
                        indexOutput.WriteBytes(answer, written, chunk);
                        written += chunk;
                    }
                }

                indexOutput.Dispose();
                IndexInput input = dir.OpenInput("foo", IOContext.DEFAULT);
                var dataInput = (DataInput)input.Clone();

                p.Copy(input, input.Length);
                var reader = p.Freeze(random.NextBool());

                var verify = new byte[numBytes];
                var read = 0;
                while (read < numBytes)
                {
                    if (random.Next(10) == 7)
                    {
                        verify[read++] = dataInput.ReadByte();
                    }
                    else
                    {
                        var chunk = Math.Min(random.Next(1000), numBytes - read);
                        dataInput.ReadBytes(verify, read, chunk);
                        read += chunk;
                    }
                }
                assertTrue(Arrays.Equals(answer, verify));

                var slice = new BytesRef();
                for (var iter2 = 0; iter2 < 100; iter2++)
                {
                    var pos = random.Next(numBytes - 1);
                    var len = random.Next(Math.Min(blockSize + 1, numBytes - pos));
                    reader.FillSlice(slice, pos, len);
                    for (var byteUpto = 0; byteUpto < len; byteUpto++)
                    {
                        assertEquals(answer[pos + byteUpto], slice.bytes[slice.offset + byteUpto]);
                    }
                }
                input.Dispose();
                dir.Dispose();
            }
        }

        [Ignore] // memory hole
        [Test]
        public virtual void TestOverflow()
        {
            var random = new Random();

            var dir = NewFSDirectory(_TestUtil.GetTempDir("testOverflow"));
            if (dir is MockDirectoryWrapper)
            {
                ((MockDirectoryWrapper)dir).SetThrottling(MockDirectoryWrapper.Throttling.NEVER);
            }
            int blockBits = _TestUtil.NextInt(random, 14, 28);
            var blockSize = 1 << blockBits;
            var arr = new byte[_TestUtil.NextInt(random, blockSize / 2, blockSize * 2)];
            for (var i = 0; i < arr.Length; ++i)
            {
                arr[i] = (byte)i;
            }
            var numBytes = (1L << 31) + _TestUtil.NextInt(random, 1, blockSize * 3);
            var p = new PagedBytes(blockBits);
            IndexOutput indexOutput = dir.CreateOutput("foo", IOContext.DEFAULT);
            for (long i = 0; i < numBytes; )
            {
                assertEquals(i, indexOutput.FilePointer);
                var len = (int)Math.Min(arr.Length, numBytes - i);
                indexOutput.WriteBytes(arr, len);
                i += len;
            }
            assertEquals(numBytes, indexOutput.FilePointer);
            indexOutput.Dispose();
            IndexInput indexInput = dir.OpenInput("foo", IOContext.DEFAULT);
            p.Copy(indexInput, numBytes);
            var reader = p.Freeze(random.NextBool());

            foreach (var offset in new long[] {0L, int.MinValue, numBytes - 1, _TestUtil.NextLong(random, 1, numBytes - 2)})
            {
                var b = new BytesRef();
                reader.FillSlice(b, offset, 1);
                assertEquals(arr[(int)(offset % arr.Length)], b.bytes[b.offset]);
            }
            indexInput.Dispose();
            dir.Dispose();
        }
    }
}
