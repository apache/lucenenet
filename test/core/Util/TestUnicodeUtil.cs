using System;
using Lucene.Net.Support;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Test.Util
{
    [TestFixture]
    public class TestUnicodeUtil : LuceneTestCase
    {
        private Random random = new Random();
        
        [Test]
        public void TestCodePointCount()
        {
            // Check invalid codepoints.
            AssertcodePointCountThrowsAssertionOn(AsByteArray('z', 0x80, 'z', 'z', 'z'));
            AssertcodePointCountThrowsAssertionOn(AsByteArray('z', 0xc0 - 1, 'z', 'z', 'z'));
            // Check 5-byte and longer sequences.
            AssertcodePointCountThrowsAssertionOn(AsByteArray('z', 0xf8, 'z', 'z', 'z'));
            AssertcodePointCountThrowsAssertionOn(AsByteArray('z', 0xfc, 'z', 'z', 'z'));
            // Check improperly terminated codepoints.
            AssertcodePointCountThrowsAssertionOn(AsByteArray('z', 0xc2));
            AssertcodePointCountThrowsAssertionOn(AsByteArray('z', 0xe2));
            AssertcodePointCountThrowsAssertionOn(AsByteArray('z', 0xe2, 0x82));
            AssertcodePointCountThrowsAssertionOn(AsByteArray('z', 0xf0));
            AssertcodePointCountThrowsAssertionOn(AsByteArray('z', 0xf0, 0xa4));
            AssertcodePointCountThrowsAssertionOn(AsByteArray('z', 0xf0, 0xa4, 0xad));

            // Check some typical examples (multibyte).
            Assert.AreEqual(0, UnicodeUtil.CodePointCount(new BytesRef(AsByteArray())));
            Assert.AreEqual(3, UnicodeUtil.CodePointCount(new BytesRef(AsByteArray('z', 'z', 'z'))));
            Assert.AreEqual(2, UnicodeUtil.CodePointCount(new BytesRef(AsByteArray('z', 0xc2, 0xa2))));
            Assert.AreEqual(2, UnicodeUtil.CodePointCount(new BytesRef(AsByteArray('z', 0xe2, 0x82, 0xac))));
            Assert.AreEqual(2, UnicodeUtil.CodePointCount(new BytesRef(AsByteArray('z', 0xf0, 0xa4, 0xad, 0xa2))));

            // And do some random stuff.
            var utf8 = new BytesRef(20);
            int num = AtLeast(50000);
            for (var i = 0; i < num; i++)
            {
                string s = _TestUtil.RandomUnicodeString(random);
                UnicodeUtil.UTF16toUTF8(s, 0, s.Length, utf8);
                Assert.AreEqual(s.CodePointCount(0, s.Length),
                             UnicodeUtil.CodePointCount(utf8));
            }
        }

        private sbyte[] AsByteArray(params int[] ints)
        {
            var asByteArray = new sbyte[ints.Length];
            for (var i = 0; i < ints.Length; i++)
            {
                asByteArray[i] = (sbyte)ints[i];
            }
            return asByteArray;
        }

        private void AssertcodePointCountThrowsAssertionOn(params sbyte[] bytes)
        {
            var threwAssertion = false;
            try
            {
                UnicodeUtil.CodePointCount(new BytesRef(bytes));
            }
            catch (ArgumentException e)
            {
                threwAssertion = true;
            }
            Assert.IsTrue(threwAssertion);
        }

        [Test]
        public void TestUTF8toUTF32()
        {
            var utf8 = new BytesRef(20);
            var utf32 = new IntsRef(20);
            var codePoints = new int[20];
            int num = AtLeast(50000);
            for (var i = 0; i < num; i++)
            {
                string s = _TestUtil.RandomUnicodeString(random);
                UnicodeUtil.UTF16toUTF8(s, 0, s.Length, utf8);
                UnicodeUtil.UTF8toUTF32(utf8, utf32);

                var charUpto = 0;
                var intUpto = 0;
                while (charUpto < s.Length)
                {
                    int cp = s.CodePointAt(charUpto);
                    codePoints[intUpto++] = cp;
                    charUpto += Character.CharCount(cp);
                }
                if (!ArrayUtil.Equals(codePoints, 0, utf32.ints, utf32.offset, intUpto))
                {
                    Console.WriteLine("FAILED");
                    for (int j = 0; j < s.Length; j++)
                    {
                        Console.WriteLine("  char[" + j + "]=" + int.ToHexString(s.CharAt(j)));
                    }
                    Console.WriteLine();
                    Assert.AreEqual(intUpto, utf32.length);
                    for (int j = 0; j < intUpto; j++)
                    {
                        Console.WriteLine("  " + int.ToHexString(utf32.ints[j]) + " vs " + int.ToHexString(codePoints[j]));
                    }
                    Fail("mismatch");
                }
            }
        }

        [Test]
        public void TestNewString()
        {
            int[] codePoints = {
                Character.ToCodePoint(Character.MIN_HIGH_SURROGATE,
                    Character.MAX_LOW_SURROGATE),
                Character.ToCodePoint(Character.MAX_HIGH_SURROGATE,
                    Character.MIN_LOW_SURROGATE), Character.MAX_HIGH_SURROGATE, 'A',
                -1,};

            string cpString = "" + Character.MIN_HIGH_SURROGATE
                + Character.MAX_LOW_SURROGATE + Character.MAX_HIGH_SURROGATE
                + Character.MIN_LOW_SURROGATE + Character.MAX_HIGH_SURROGATE + 'A';

            var tests = new int[,] { {0, 1, 0, 2}, {0, 2, 0, 4}, {1, 1, 2, 2},
                {1, 2, 2, 3}, {1, 3, 2, 4}, {2, 2, 4, 2}, {2, 3, 0, -1}, {4, 5, 0, -1},
                {3, -1, 0, -1}};

            for (int i = 0; i < tests.length; ++i)
            {
                int[] t = tests[i];
                var s = t[0];
                var c = t[1];
                var rs = t[2];
                var rc = t[3];

                try
                {
                    string str = UnicodeUtil.NewString(codePoints, s, c);
                    Assert.IsFalse(rc == -1);
                    Assert.AreEqual(cpString.Substring(rs, rs + rc), str);
                    continue;
                }
                catch (IndexOutOfRangeException e1)
                {
                    // Ignored.
                }
                catch (ArgumentException e2)
                {
                    // Ignored.
                }
                Assert.IsTrue(rc == -1);
            }
        }

        public void testUTF8UTF16CharsRef()
        {
            int num = AtLeast(3989);
            for (int i = 0; i < num; i++)
            {
                string unicode = _TestUtil.RandomRealisticUnicodeString(random);
                var bytesRef = new BytesRef(unicode);
                var arr = new char[1 + random.Next(100)];
                var offset = random.Next(arr.Length);
                var len = random.Next(arr.Length - offset);
                var cRef = new CharsRef(arr, offset, len);
                UnicodeUtil.UTF8toUTF16(bytesRef, cRef);
                Assert.AreEqual(cRef.ToString(), unicode);
            }
        }
    }
}
