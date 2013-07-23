using System;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Test.Util
{
    [TestFixture]
    public class TestBytesRef : LuceneTestCase
    {
        [Test]
        public void TestEmpty()
        {
            var b = new BytesRef();
            Assert.Equals(BytesRef.EMPTY_BYTES, b.bytes);
            Assert.Equals(0, b.offset);
            Assert.Equals(0, b.length);
        }

        [Test]
        public void TestFromBytes()
        {
            var bytes = new sbyte[] { (sbyte)'a', (sbyte)'b', (sbyte)'c', (sbyte)'d' };
            var b = new BytesRef(bytes);
            Assert.Equals(bytes, b.bytes);
            Assert.Equals(0, b.offset);
            Assert.Equals(4, b.length);

            var b2 = new BytesRef(bytes, 1, 3);
            Assert.Equals("bcd", b2.Utf8ToString());

            Assert.IsFalse(b.Equals(b2));
        }

        [Test]
        public void TestFromChars()
        {
            for (var i = 0; i < 100; i++)
            {
                string s = _TestUtil.RandomUnicodeString(new Random());
                string s2 = new BytesRef(s).Utf8ToString();
                Assert.Equals(s, s2);
            }

            // only for 4.x
            Assert.Equals("\uFFFF", new BytesRef("\uFFFF").Utf8ToString());
        }

        // LUCENE-3590, AIOOBE if you append to a bytesref with offset != 0
        [Test]
        public void TestAppend()
        {
            var bytes = new sbyte[] { (sbyte)'a', (sbyte)'b', (sbyte)'c', (sbyte)'d' };
            var b = new BytesRef(bytes, 1, 3); // bcd
            b.Append(new BytesRef("e"));
            Assert.Equals("bcde", b.Utf8ToString());
        }

        // LUCENE-3590, AIOOBE if you copy to a bytesref with offset != 0
        [Test]
        public void TestCopyBytes()
        {
            var bytes = new sbyte[] { (sbyte)'a', (sbyte)'b', (sbyte)'c', (sbyte)'d' };
            var b = new BytesRef(bytes, 1, 3); // bcd
            b.CopyBytes(new BytesRef("bcde"));
            Assert.Equals("bcde", b.Utf8ToString());
        }
    }
}
