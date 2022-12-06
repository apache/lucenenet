using J2N;
using J2N.Text;
using Lucene.Net.Attributes;
using NUnit.Framework;
using System;
using Assert = Lucene.Net.TestFramework.Assert;
using Console = Lucene.Net.Util.SystemConsole;

namespace Lucene.Net.Util
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

    /*
     * Some of this code came from the excellent Unicode
     * conversion examples from:
     *
     *   http://www.unicode.org/Public/PROGRAMS/CVTUTF
     *
     * Full Copyright for that code follows:
    */

    /*
     * Copyright 2001-2004 Unicode, Inc.
     *
     * Disclaimer
     *
     * this source code is provided as is by Unicode, Inc. No claims are
     * made as to fitness for any particular purpose. No warranties of any
     * kind are expressed or implied. The recipient agrees to determine
     * applicability of information provided. If this file has been
     * purchased on magnetic or optical media from Unicode, Inc., the
     * sole remedy for any claim will be exchange of defective media
     * within 90 days of receipt.
     *
     * Limitations on Rights to Redistribute this Code
     *
     * Unicode, Inc. hereby grants the right to freely use the information
     * supplied in this file in the creation of products supporting the
     * Unicode Standard, and to make copies of this file in any form
     * for internal or external distribution as long as this notice
     * remains attached.
     */

    /*
     * Additional code came from the IBM ICU library.
     *
     *  http://www.icu-project.org
     *
     * Full Copyright for that code follows.
     */

    /*
     * Copyright (C) 1999-2010, International Business Machines
     * Corporation and others.  All Rights Reserved.
     *
     * Permission is hereby granted, free of charge, to any person obtaining a copy
     * of this software and associated documentation files (the "Software"), to deal
     * in the Software without restriction, including without limitation the rights
     * to use, copy, modify, merge, publish, distribute, and/or sell copies of the
     * Software, and to permit persons to whom the Software is furnished to do so,
     * provided that the above copyright notice(s) and this permission notice appear
     * in all copies of the Software and that both the above copyright notice(s) and
     * this permission notice appear in supporting documentation.
     *
     * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
     * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
     * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT OF THIRD PARTY RIGHTS.
     * IN NO EVENT SHALL THE COPYRIGHT HOLDER OR HOLDERS INCLUDED IN THIS NOTICE BE
     * LIABLE FOR ANY CLAIM, OR ANY SPECIAL INDIRECT OR CONSEQUENTIAL DAMAGES, OR
     * ANY DAMAGES WHATSOEVER RESULTING FROM LOSS OF USE, DATA OR PROFITS, WHETHER
     * IN AN ACTION OF CONTRACT, NEGLIGENCE OR OTHER TORTIOUS ACTION, ARISING OUT
     * OF OR IN CONNECTION WITH THE USE OR PERFORMANCE OF THIS SOFTWARE.
     *
     * Except as contained in this notice, the name of a copyright holder shall not
     * be used in advertising or otherwise to promote the sale, use or other
     * dealings in this Software without prior written authorization of the
     * copyright holder.
     */

    [TestFixture]
    public class TestUnicodeUtil : LuceneTestCase
    {
        [Test]
        public virtual void TestCodePointCount()
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
            BytesRef utf8 = new BytesRef(20);
            int num = AtLeast(50000);
            for (int i = 0; i < num; i++)
            {
                string s = TestUtil.RandomUnicodeString(Random);
                UnicodeUtil.UTF16toUTF8(s, 0, s.Length, utf8);
                assertEquals(s.CodePointCount(0, s.Length),
                   UnicodeUtil.CodePointCount(utf8));
            }
        }

        private static byte[] AsByteArray(params int[] ints)
        {
            var asByteArray = new byte[ints.Length];
            for (int i = 0; i < ints.Length; i++)
            {
                asByteArray[i] = (byte)ints[i];
            }
            return asByteArray;
        }

        private static void AssertcodePointCountThrowsAssertionOn(params byte[] bytes)
        {
            bool threwAssertion = false;
            try
            {
                UnicodeUtil.CodePointCount(new BytesRef(bytes));
            }
            catch (Exception e) when (e.IsIllegalArgumentException())
            {
                threwAssertion = true;
            }
            Assert.IsTrue(threwAssertion);
        }

        [Test]
        public virtual void TestUTF8toUTF32()
        {
            BytesRef utf8 = new BytesRef(20);
            Int32sRef utf32 = new Int32sRef(20);
            int[] codePoints = new int[20];
            int num = AtLeast(50000);
            for (int i = 0; i < num; i++)
            {
                string s = TestUtil.RandomUnicodeString(Random);
                UnicodeUtil.UTF16toUTF8(s, 0, s.Length, utf8);
                UnicodeUtil.UTF8toUTF32(utf8, utf32);

                int charUpto = 0;
                int intUpto = 0;

                while (charUpto < s.Length)
                {
                    int cp = Character.CodePointAt(s, charUpto);
                    codePoints[intUpto++] = cp;
                    charUpto += Character.CharCount(cp);
                }
                if (!ArrayUtil.Equals(codePoints, 0, utf32.Int32s, utf32.Offset, intUpto))
                {
                    Console.WriteLine("FAILED");
                    for (int j = 0; j < s.Length; j++)
                    {
                        Console.WriteLine("  char[" + j + "]=" + ((int)s[j]).ToString("x"));
                    }
                    Console.WriteLine();
                    Assert.AreEqual(intUpto, utf32.Length);
                    for (int j = 0; j < intUpto; j++)
                    {
                        Console.WriteLine("  " + utf32.Int32s[j].ToString("x") + " vs " + codePoints[j].ToString("x"));
                    }
                    Assert.Fail("mismatch");
                }
            }
        }

        [Test, LuceneNetSpecific]
        public virtual void TestUTF8toUTF32_ICharSequence()
        {
            BytesRef utf8 = new BytesRef(20);
            Int32sRef utf32 = new Int32sRef(20);
            int[] codePoints = new int[20];
            int num = AtLeast(50000);
            for (int i = 0; i < num; i++)
            {
                string s = TestUtil.RandomUnicodeString(Random);
                UnicodeUtil.UTF16toUTF8(s.AsCharSequence(), 0, s.Length, utf8);
                UnicodeUtil.UTF8toUTF32(utf8, utf32);

                int charUpto = 0;
                int intUpto = 0;

                while (charUpto < s.Length)
                {
                    int cp = Character.CodePointAt(s, charUpto);
                    codePoints[intUpto++] = cp;
                    charUpto += Character.CharCount(cp);
                }
                if (!ArrayUtil.Equals(codePoints, 0, utf32.Int32s, utf32.Offset, intUpto))
                {
                    Console.WriteLine("FAILED");
                    for (int j = 0; j < s.Length; j++)
                    {
                        Console.WriteLine("  char[" + j + "]=" + ((int)s[j]).ToString("x"));
                    }
                    Console.WriteLine();
                    Assert.AreEqual(intUpto, utf32.Length);
                    for (int j = 0; j < intUpto; j++)
                    {
                        Console.WriteLine("  " + utf32.Int32s[j].ToString("x") + " vs " + codePoints[j].ToString("x"));
                    }
                    Assert.Fail("mismatch");
                }
            }
        }

        [Test, LuceneNetSpecific]
        public virtual void TestUTF8toUTF32_CharArray()
        {
            BytesRef utf8 = new BytesRef(20);
            Int32sRef utf32 = new Int32sRef(20);
            int[] codePoints = new int[20];
            int num = AtLeast(50000);
            for (int i = 0; i < num; i++)
            {
                string s = TestUtil.RandomUnicodeString(Random);
                UnicodeUtil.UTF16toUTF8(s.ToCharArray(), 0, s.Length, utf8);
                UnicodeUtil.UTF8toUTF32(utf8, utf32);

                int charUpto = 0;
                int intUpto = 0;

                while (charUpto < s.Length)
                {
                    int cp = Character.CodePointAt(s, charUpto);
                    codePoints[intUpto++] = cp;
                    charUpto += Character.CharCount(cp);
                }
                if (!ArrayUtil.Equals(codePoints, 0, utf32.Int32s, utf32.Offset, intUpto))
                {
                    Console.WriteLine("FAILED");
                    for (int j = 0; j < s.Length; j++)
                    {
                        Console.WriteLine("  char[" + j + "]=" + ((int)s[j]).ToString("x"));
                    }
                    Console.WriteLine();
                    Assert.AreEqual(intUpto, utf32.Length);
                    for (int j = 0; j < intUpto; j++)
                    {
                        Console.WriteLine("  " + utf32.Int32s[j].ToString("x") + " vs " + codePoints[j].ToString("x"));
                    }
                    Assert.Fail("mismatch");
                }
            }
        }

        [Test]
        public virtual void TestNewString()
        {
            int[] codePoints = new int[] { Character.ToCodePoint(Character.MinHighSurrogate, Character.MaxLowSurrogate), Character.ToCodePoint(Character.MaxHighSurrogate, Character.MinLowSurrogate), Character.MaxHighSurrogate, 'A', -1 };

            string cpString = "" + Character.MinHighSurrogate + Character.MaxLowSurrogate + Character.MaxHighSurrogate + Character.MinLowSurrogate + Character.MaxHighSurrogate + 'A';

            int[][] tests = new int[][] { new int[] { 0, 1, 0, 2 }, new int[] { 0, 2, 0, 4 }, new int[] { 1, 1, 2, 2 }, new int[] { 1, 2, 2, 3 }, new int[] { 1, 3, 2, 4 }, new int[] { 2, 2, 4, 2 }, new int[] { 2, 3, 0, -1 }, new int[] { 4, 5, 0, -1 }, new int[] { 3, -1, 0, -1 } };

            for (int i = 0; i < tests.Length; ++i)
            {
                int[] t = tests[i];
                int s = t[0];
                int c = t[1];
                int rs = t[2];
                int rc = t[3];

                try
                {
                    string str = UnicodeUtil.NewString(codePoints, s, c);
                    Assert.IsFalse(rc == -1);
                    Assert.AreEqual(cpString.Substring(rs, rc), str);
                    continue;
                }
                catch (Exception e1) when (e1.IsIndexOutOfBoundsException())
                {
                    // Ignored.
                }
                catch (Exception e2) when (e2.IsIllegalArgumentException())
                {
                    // Ignored.
                }
                Assert.IsTrue(rc == -1);
            }
        }

        [Test]
        public virtual void TestUTF8UTF16CharsRef()
        {
            int num = AtLeast(3989);
            for (int i = 0; i < num; i++)
            {
                string unicode = TestUtil.RandomRealisticUnicodeString(Random);
                BytesRef @ref = new BytesRef(unicode);
                char[] arr = new char[1 + Random.Next(100)];
                int offset = Random.Next(arr.Length);
                int len = Random.Next(arr.Length - offset);
                CharsRef cRef = new CharsRef(arr, offset, len);
                UnicodeUtil.UTF8toUTF16(@ref, cRef);
                Assert.AreEqual(cRef.ToString(), unicode);
            }
        }
    }
}