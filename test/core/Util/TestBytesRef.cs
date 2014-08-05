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
            BytesRef b = new BytesRef();
            assertEquals(BytesRef.EMPTY_BYTES, b.bytes);
            assertEquals(0, b.offset);
            assertEquals(0, b.length);
        }

        [Test]
        public void TestFromBytes()
        {
            var bytes = new[]
            {
                (sbyte) 'a', (sbyte) 'b', (sbyte) 'c', (sbyte) 'd'
            };


            BytesRef b = new BytesRef(bytes);
            assertEquals(bytes, b.bytes);
            assertEquals(0, b.offset);
            assertEquals(4, b.length);
            BytesRef b2 = new BytesRef(bytes, 1, 3);
            assertEquals(@"bcd", b2.Utf8ToString());
            assertFalse(b.Equals(b2));
        }

        [Test]
        public void TestFromChars()
        {
            for (int i = 0; i < 100; i++)
            {
                string s = _TestUtil.randomUnicodeString(new Random());
                string s2 = new BytesRef(s).Utf8ToString();
                assertEquals(s, s2);
            }

            assertEquals(@"?", new BytesRef(@"?").Utf8ToString());
        }

        [Test]
        public void TestAppend()
        {
            var bytes = new[]
            {
            (sbyte)'a', (sbyte)'b', (sbyte)'c', (sbyte)'d'
            };

            BytesRef b = new BytesRef(bytes, 1, 3);
            b.Append(new BytesRef(@"e"));
            assertEquals(@"bcde", b.Utf8ToString());
        }

        [Test]
        public void TestCopyBytes()
        {
            var bytes = new[]
            {
                (sbyte) 'a', (sbyte) 'b', (sbyte) 'c', (sbyte) 'd'
            };

            BytesRef b = new BytesRef(bytes, 1, 3);
            b.CopyBytes(new BytesRef(@"bcde"));
            assertEquals(@"bcde", b.Utf8ToString());
        }
    }
}