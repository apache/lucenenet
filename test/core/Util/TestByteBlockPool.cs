using System;
using System.Collections.Generic;
using Lucene.Net.Test.Support;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Test.Util
{
    [TestFixture]
    public class TestByteBlockPool : LuceneTestCase
    {
        [Test]
        public void TestReadAndWrite()
        {
            var random = new Random();

            Counter bytesUsed = Counter.NewCounter();
            var pool = new ByteBlockPool(new ByteBlockPool.DirectTrackingAllocator(bytesUsed));
            pool.NextBuffer();
            var reuseFirst = random.NextBool();
            for (var j = 0; j < 2; j++)
            {

                var list = new List<BytesRef>();
                int maxLength = AtLeast(500);
                int numValues = AtLeast(100);
                var bytesRef = new BytesRef();
                for (var i = 0; i < numValues; i++)
                {
                    string value = _TestUtil.RandomRealisticUnicodeString(random,
                       maxLength);
                    list.Add(new BytesRef(value));
                    bytesRef.CopyChars(value);
                    pool.Append(bytesRef);
                }
                // verify
                long position = 0;
                foreach (var expected in list)
                {
                    bytesRef.Grow(expected.length);
                    bytesRef.length = expected.length;
                    pool.ReadBytes(position, bytesRef.bytes, bytesRef.offset, bytesRef.length);
                    assertEquals(expected, bytesRef);
                    position += bytesRef.length;
                }
                pool.Reset(random.NextBool(), reuseFirst);
                if (reuseFirst)
                {
                    assertEquals(ByteBlockPool.BYTE_BLOCK_SIZE, bytesUsed.Get());
                }
                else
                {
                    assertEquals(0, bytesUsed.Get());
                    pool.NextBuffer(); // prepare for next iter
                }
            }
        }
    }
}
