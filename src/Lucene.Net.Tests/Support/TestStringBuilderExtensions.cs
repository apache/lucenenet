/*
 *
 * Licensed to the Apache Software Foundation (ASF) under one
 * or more contributor license agreements.  See the NOTICE file
 * distributed with this work for additional information
 * regarding copyright ownership.  The ASF licenses this file
 * to you under the Apache License, Version 2.0 (the
 * "License"); you may not use this file except in compliance
 * with the License.  You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing,
 * software distributed under the License is distributed on an
 * "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
 * KIND, either express or implied.  See the License for the
 * specific language governing permissions and limitations
 * under the License.
 *
*/

using Lucene.Net.Attributes;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Text;

namespace Lucene.Net.Support
{
    [TestFixture]
    public class TestStringBuilderExtensions : LuceneTestCase
    {
        [Test, LuceneNetSpecific]
        public virtual void TestReverse()
        {
            var sb = new StringBuilder("foo 𝌆 bar𫀁mañana");

            sb.Reverse();

            Assert.AreEqual("anañam𫀁rab 𝌆 oof", sb.ToString());
        }

        [Test, LuceneNetSpecific]
        public virtual void TestAppendCodePointBmp()
        {
            var sb = new StringBuilder("foo bar");
            int codePoint = 97; // a

            sb.AppendCodePoint(codePoint);

            Assert.AreEqual("foo bara", sb.ToString());
        }

        [Test, LuceneNetSpecific]
        public virtual void TestAppendCodePointUnicode()
        {
            var sb = new StringBuilder("foo bar");
            int codePoint = 3594; // ช

            sb.AppendCodePoint(codePoint);

            Assert.AreEqual("foo barช", sb.ToString());
        }

        [Test, LuceneNetSpecific]
        public virtual void TestAppendCodePointUTF16Surrogates()
        {
            var sb = new StringBuilder("foo bar");
            int codePoint = 176129; // '\uD86C', '\uDC01' (𫀁)

            sb.AppendCodePoint(codePoint);

            Assert.AreEqual("foo bar𫀁", sb.ToString());
        }

        [Test, LuceneNetSpecific]
        public virtual void TestAppendCodePointTooHigh()
        {
            var sb = new StringBuilder("foo bar");
            int codePoint = Character.MAX_CODE_POINT + 1;

            Assert.Throws<ArgumentException>(() => sb.AppendCodePoint(codePoint));
        }

        [Test, LuceneNetSpecific]
        public virtual void TestAppendCodePointTooLow()
        {
            var sb = new StringBuilder("foo bar");
            int codePoint = Character.MIN_CODE_POINT - 1;

            Assert.Throws<ArgumentException>(() => sb.AppendCodePoint(codePoint));
        }

        #region Apache Harmony Tests

        private void reverseTest(String org, String rev, String back)
        {
            // create non-shared StringBuilder
            StringBuilder sb = new StringBuilder(org);
            sb.Reverse();
            String reversed = sb.toString();
            assertEquals(rev, reversed);
            // create non-shared StringBuilder
            sb = new StringBuilder(reversed);
            sb.Reverse();
            reversed = sb.toString();
            assertEquals(back, reversed);

            // test algorithm when StringBuilder is shared
            sb = new StringBuilder(org);
            String copy = sb.toString();
            assertEquals(org, copy);
            sb.Reverse();
            reversed = sb.toString();
            assertEquals(rev, reversed);
            sb = new StringBuilder(reversed);
            copy = sb.toString();
            assertEquals(rev, copy);
            sb.Reverse();
            reversed = sb.toString();
            assertEquals(back, reversed);
        }

        /**
         * @tests java.lang.StringBuilder.reverse()
         */
        [Test, LuceneNetSpecific]
        public void Test_Reverse()
        {
            String fixture = "0123456789";
            StringBuilder sb = new StringBuilder(fixture);
            assertSame(sb, sb.Reverse());
            assertEquals("9876543210", sb.toString());

            sb = new StringBuilder("012345678");
            assertSame(sb, sb.Reverse());
            assertEquals("876543210", sb.toString());

            sb.Length=(1);
            assertSame(sb, sb.Reverse());
            assertEquals("8", sb.toString());

            sb.Length=(0);
            assertSame(sb, sb.Reverse());
            assertEquals("", sb.toString());

            String str;
            str = "a";
            reverseTest(str, str, str);

            str = "ab";
            reverseTest(str, "ba", str);

            str = "abcdef";
            reverseTest(str, "fedcba", str);

            str = "abcdefg";
            reverseTest(str, "gfedcba", str);

            str = "\ud800\udc00";
            reverseTest(str, str, str);

            str = "\udc00\ud800";
            reverseTest(str, "\ud800\udc00", "\ud800\udc00");

            str = "a\ud800\udc00";
            reverseTest(str, "\ud800\udc00a", str);

            str = "ab\ud800\udc00";
            reverseTest(str, "\ud800\udc00ba", str);

            str = "abc\ud800\udc00";
            reverseTest(str, "\ud800\udc00cba", str);

            str = "\ud800\udc00\udc01\ud801\ud802\udc02";
            reverseTest(str, "\ud802\udc02\ud801\udc01\ud800\udc00",
                    "\ud800\udc00\ud801\udc01\ud802\udc02");

            str = "\ud800\udc00\ud801\udc01\ud802\udc02";
            reverseTest(str, "\ud802\udc02\ud801\udc01\ud800\udc00", str);

            str = "\ud800\udc00\udc01\ud801a";
            reverseTest(str, "a\ud801\udc01\ud800\udc00",
                    "\ud800\udc00\ud801\udc01a");

            str = "a\ud800\udc00\ud801\udc01";
            reverseTest(str, "\ud801\udc01\ud800\udc00a", str);

            str = "\ud800\udc00\udc01\ud801ab";
            reverseTest(str, "ba\ud801\udc01\ud800\udc00",
                    "\ud800\udc00\ud801\udc01ab");

            str = "ab\ud800\udc00\ud801\udc01";
            reverseTest(str, "\ud801\udc01\ud800\udc00ba", str);

            str = "\ud800\udc00\ud801\udc01";
            reverseTest(str, "\ud801\udc01\ud800\udc00", str);

            str = "a\ud800\udc00z\ud801\udc01";
            reverseTest(str, "\ud801\udc01z\ud800\udc00a", str);

            str = "a\ud800\udc00bz\ud801\udc01";
            reverseTest(str, "\ud801\udc01zb\ud800\udc00a", str);

            str = "abc\ud802\udc02\ud801\udc01\ud800\udc00";
            reverseTest(str, "\ud800\udc00\ud801\udc01\ud802\udc02cba", str);

            str = "abcd\ud802\udc02\ud801\udc01\ud800\udc00";
            reverseTest(str, "\ud800\udc00\ud801\udc01\ud802\udc02dcba", str);
        }

        /**
         * @tests java.lang.StringBuilder.codePointCount(int, int)
         */
        [Test, LuceneNetSpecific]
        public void Test_CodePointCountII()
        {
            assertEquals(1, new StringBuilder("\uD800\uDC00").CodePointCount(0, 2));
            assertEquals(1, new StringBuilder("\uD800\uDC01").CodePointCount(0, 2));
            assertEquals(1, new StringBuilder("\uD801\uDC01").CodePointCount(0, 2));
            assertEquals(1, new StringBuilder("\uDBFF\uDFFF").CodePointCount(0, 2));

            assertEquals(3, new StringBuilder("a\uD800\uDC00b").CodePointCount(0, 4));
            assertEquals(4, new StringBuilder("a\uD800\uDC00b\uD800").CodePointCount(0, 5));

            StringBuilder sb = new StringBuilder();
            sb.append("abc");
            try
            {
                sb.CodePointCount(-1, 2);
                fail("No IOOBE for negative begin index.");
            }
#pragma warning disable 168
            catch (IndexOutOfRangeException e)
#pragma warning restore 168
            {

            }

            try
            {
                sb.CodePointCount(0, 4);
                fail("No IOOBE for end index that's too large.");
            }
#pragma warning disable 168
            catch (IndexOutOfRangeException e)
#pragma warning restore 168
            {

            }

            try
            {
                sb.CodePointCount(3, 2);
                fail("No IOOBE for begin index larger than end index.");
            }
#pragma warning disable 168
            catch (IndexOutOfRangeException e)
#pragma warning restore 168
            {

            }
        }

        /**
	     * @tests java.lang.StringBuilder.codePointAt(int)
	     */
        [Test, LuceneNetSpecific]
        public void Test_CodePointAtI()
        {
            StringBuilder sb = new StringBuilder("abc");
            assertEquals('a', sb.CodePointAt(0));
            assertEquals('b', sb.CodePointAt(1));
            assertEquals('c', sb.CodePointAt(2));

            sb = new StringBuilder("\uD800\uDC00");
            assertEquals(0x10000, sb.CodePointAt(0));
            assertEquals('\uDC00', sb.CodePointAt(1));

            sb = new StringBuilder();
            sb.append("abc");
            try
            {
                sb.CodePointAt(-1);
                fail("No IOOBE on negative index.");
            }
#pragma warning disable 168
            catch (IndexOutOfRangeException e)
#pragma warning restore 168
            {

            }

            try
            {
                sb.CodePointAt(sb.Length);
                fail("No IOOBE on index equal to length.");
            }
#pragma warning disable 168
            catch (IndexOutOfRangeException e)
#pragma warning restore 168
            {

            }

            try
            {
                sb.CodePointAt(sb.Length + 1);
                fail("No IOOBE on index greater than length.");
            }
#pragma warning disable 168
            catch (IndexOutOfRangeException e)
#pragma warning restore 168
            {

            }
        }

        /**
         * @tests java.lang.StringBuilder.indexOf(String)
         */
        [Test, LuceneNetSpecific]
        public void Test_IndexOfLSystem_String()
        {
            String fixture = "0123456789";
            StringBuilder sb = new StringBuilder(fixture);
            assertEquals(0, sb.IndexOf("0"));
            assertEquals(0, sb.IndexOf("012"));
            assertEquals(-1, sb.IndexOf("02"));
            assertEquals(8, sb.IndexOf("89"));

            try
            {
                sb.IndexOf(null);
                fail("no NPE");
            }
#pragma warning disable 168
            catch (ArgumentNullException e)
#pragma warning restore 168
            {
                // Expected
            }
        }

        /**
         * @tests java.lang.StringBuilder.indexOf(String, int)
         */
        [Test, LuceneNetSpecific]
        public void Test_IndexOfStringInt()
        {
            String fixture = "0123456789";
            StringBuilder sb = new StringBuilder(fixture);
            assertEquals(0, sb.IndexOf("0"));
            assertEquals(0, sb.IndexOf("012"));
            assertEquals(-1, sb.IndexOf("02"));
            assertEquals(8, sb.IndexOf("89"));

            assertEquals(0, sb.IndexOf("0"), 0);
            assertEquals(0, sb.IndexOf("012"), 0);
            assertEquals(-1, sb.IndexOf("02"), 0);
            assertEquals(8, sb.IndexOf("89"), 0);

            assertEquals(-1, sb.IndexOf("0"), 5);
            assertEquals(-1, sb.IndexOf("012"), 5);
            assertEquals(-1, sb.IndexOf("02"), 0);
            assertEquals(8, sb.IndexOf("89"), 5);

            try
            {
                sb.IndexOf(null, 0);
                fail("no NPE");
            }
#pragma warning disable 168
            catch (ArgumentNullException e)
#pragma warning restore 168
            {
                // Expected
            }
        }

        #endregion Apache Harmony Tests
    }
}
