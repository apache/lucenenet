using System;
using System.Text;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Test.Util
{
    [TestFixture]
    public class TestCharsRef : LuceneTestCase
    {
        [Test]
        public void TestUTF16InUTF8Order()
        {
            int numStrings = AtLeast(1000);
            var utf8 = new BytesRef[numStrings];
            var utf16 = new CharsRef[numStrings];

            for (var i = 0; i < numStrings; i++)
            {
                string s = _TestUtil.RandomUnicodeString(new Random());
                utf8[i] = new BytesRef(s);
                utf16[i] = new CharsRef(s);
            }

            Array.Sort(utf8);
            Array.Sort(utf16, CharsRef.GetUTF16SortedAsUTF8Comparator());

            for (var i = 0; i < numStrings; i++)
            {
                Assert.Equals(utf8[i].Utf8ToString(), utf16[i].ToString());
            }
        }

        [Test]
        public void TestAppend()
        {
            var charsRef = new CharsRef();
            var builder = new StringBuilder();
            int numStrings = atLeast(10);
            for (var i = 0; i < numStrings; i++)
            {
                char[] charArray = _TestUtil.RandomRealisticUnicodeString(new Random(), 1, 100).ToCharArray();
                var offset = new Random().Next(charArray.Length);
                var length = charArray.Length - offset;
                builder.Append(charArray, offset, length);
                charsRef.Append(charArray, offset, length);
            }

            Assert.Equals(builder.ToString(), charsRef.ToString());
        }

        [Test]
        public void TestCopy()
        {
            int numIters = AtLeast(10);
            for (var i = 0; i < numIters; i++)
            {
                var charsRef = new CharsRef();
                char[] charArray = _TestUtil.RandomRealisticUnicodeString(new Random(), 1, 100).ToCharArray();
                var offset = new Random().Next(charArray.Length);
                var Length = charArray.Length - offset;
                var str = new string(charArray, offset, Length);
                charsRef.CopyChars(charArray, offset, Length);
                Assert.Equals(str, charsRef.ToString());
            }
        }

        // LUCENE-3590, AIOOBE if you Append to a charsref with offset != 0
        [Test]
        public void TestAppendChars()
        {
            var chars = new char[] { 'a', 'b', 'c', 'd' };
            var c = new CharsRef(chars, 1, 3); // bcd
            c.Append(new char[] { 'e' }, 0, 1);
            Assert.Equals("bcde", c.ToString());
        }

        // LUCENE-3590, AIOOBE if you copy to a charsref with offset != 0
        [Test]
        public void TestCopyChars()
        {
            var chars = new char[] { 'a', 'b', 'c', 'd' };
            var c = new CharsRef(chars, 1, 3); // bcd
            var otherchars = new char[] { 'b', 'c', 'd', 'e' };
            c.CopyChars(otherchars, 0, 4);
            Assert.Equals("bcde", c.ToString());
        }

        // LUCENE-3590, AIOOBE if you copy to a charsref with offset != 0
        [Test]
        public void TestCopyCharsRef()
        {
            var chars = new char[] { 'a', 'b', 'c', 'd' };
            var c = new CharsRef(chars, 1, 3); // bcd
            char otherchars = new char[] { 'b', 'c', 'd', 'e' };
            c.CopyChars(new CharsRef(otherchars, 0, 4));
            Assert.Equals("bcde", c.ToString());
        }

        // LUCENE-3590: fix charsequence to fully obey interface
        [Test]
        public void TestCharSequenceCharAt()
        {
            var c = new CharsRef("abc");

            Assert.Equals('b', c.CharAt(1));

            Assert.Throws<IndexOutOfRangeException>(() => c.CharAt(-1));
            Assert.Throws<IndexOutOfRangeException>(() => c.CharAt(3));
        }

        // LUCENE-3590: fix off-by-one in subsequence, and fully obey interface
        // LUCENE-4671: fix SubSequence
        [Test]
        public void TestCharSequenceSubSequence()
        {
            var sequences = new[] {
                new CharsRef("abc"),
                new CharsRef("0abc".ToCharArray(), 1, 3),
                new CharsRef("abc0".ToCharArray(), 0, 3),
                new CharsRef("0abc0".ToCharArray(), 1, 3)
            };

            foreach (var c in sequences)
            {
                DoTestSequence(c);
            }
        }

        private void DoTestSequence(CharSequence c)
        {
            // slice
            Assert.Equals("a", c.SubSequence(0, 1).toString());
            // mid subsequence
            Assert.Equals("b", c.SubSequence(1, 2).toString());
            // end subsequence
            Assert.Equals("bc", c.SubSequence(1, 3).toString());
            // empty subsequence
            Assert.Equals("", c.SubSequence(0, 0).toString());

            Assert.Throws<IndexOutOfRangeException>(() => c.SubSequence(-1, 1));
            Assert.Throws<IndexOutOfRangeException>(() => c.SubSequence(0, -1));
            Assert.Throws<IndexOutOfRangeException>(() => c.SubSequence(0, 4));
            Assert.Throws<IndexOutOfRangeException>(() => c.SubSequence(2, 1));
        }
    }
}
