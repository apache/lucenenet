using System;
using Lucene.Net.Store;
using Lucene.Net.Test.Support;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Test.Util
{
    [Ignore("You must increase heap to > 2 G to run this")]
    [TestFixture]
    public class Test2BPagedBytes : LuceneTestCase
    {
        [Test]
        public void Test()
        {
            BaseDirectoryWrapper dir = NewFSDirectory(_TestUtil.GetTempDir("test2BPagedBytes"));
            if (dir is MockDirectoryWrapper)
            {
                ((MockDirectoryWrapper) dir).SetThrottling(MockDirectoryWrapper.Throttling.NEVER);
            }
            var pb = new PagedBytes(15);
            IndexOutput dataOutput = dir.CreateOutput("foo", IOContext.DEFAULT);
            long netBytes = 0;
            var seed = new Random().Next();
            long lastFP = 0;
            var r2 = new Random(seed);
            while (netBytes < 1.1*int.MaxValue)
            {
                var numBytes = _TestUtil.NextInt(r2, 1, 32768);
                var bytes = new sbyte[numBytes];
                r2.NextBytes(bytes);
                dataOutput.WriteBytes(bytes, bytes.Length);
                var fp = dataOutput.FilePointer;
                // assert fp == lastFP + numBytes;
                lastFP = fp;
                netBytes += numBytes;
            }
            dataOutput.Dispose();
            IndexInput input = dir.OpenInput("foo", IOContext.DEFAULT);
            pb.Copy(input, input.Length);
            input.Dispose();
            var reader = pb.Freeze(true);

            r2 = new Random(seed);
            netBytes = 0;
            while (netBytes < 1.1 * int.MaxValue)
            {
                int numBytes = _TestUtil.NextInt(r2, 1, 32768);
                var bytes = new sbyte[numBytes];
                r2.NextBytes(bytes);
                var expected = new BytesRef(bytes);

                var actual = new BytesRef();
                reader.FillSlice(actual, netBytes, numBytes);
                assertEquals(expected, actual);

                netBytes += numBytes;
            }
            dir.Close();
        }
    }
}
