using J2N.Text;
using Lucene.Net.Attributes;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Assert = Lucene.Net.TestFramework.Assert;

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

    [TestFixture]
    public class TestCharsRef : LuceneTestCase
    {
        [Test]
        public virtual void TestUTF16InUTF8Order()
        {
            int numStrings = AtLeast(1000);
            BytesRef[] utf8 = new BytesRef[numStrings];
            CharsRef[] utf16 = new CharsRef[numStrings];

            for (int i = 0; i < numStrings; i++)
            {
                string s = TestUtil.RandomUnicodeString(Random);
                utf8[i] = new BytesRef(s);
                utf16[i] = new CharsRef(s);
            }

            Array.Sort(utf8);
#pragma warning disable 612, 618
            Array.Sort(utf16, CharsRef.UTF16SortedAsUTF8Comparer);
#pragma warning restore 612, 618

            for (int i = 0; i < numStrings; i++)
            {
                Assert.AreEqual(utf8[i].Utf8ToString(), utf16[i].ToString());
            }
        }

        [Test]
        public virtual void TestAppend()
        {
            CharsRef @ref = new CharsRef();
            StringBuilder builder = new StringBuilder();
            int numStrings = AtLeast(10);
            for (int i = 0; i < numStrings; i++)
            {
                char[] charArray = TestUtil.RandomRealisticUnicodeString(Random, 1, 100).ToCharArray();
                int offset = Random.Next(charArray.Length);
                int length = charArray.Length - offset;
                builder.Append(charArray, offset, length);
                @ref.Append(charArray, offset, length);
            }

            Assert.AreEqual(builder.ToString(), @ref.ToString());
        }

        [Test]
        public virtual void TestCopy()
        {
            int numIters = AtLeast(10);
            for (int i = 0; i < numIters; i++)
            {
                CharsRef @ref = new CharsRef();
                char[] charArray = TestUtil.RandomRealisticUnicodeString(Random, 1, 100).ToCharArray();
                int offset = Random.Next(charArray.Length);
                int length = charArray.Length - offset;
                string str = new string(charArray, offset, length);
                @ref.CopyChars(charArray, offset, length);
                Assert.AreEqual(str, @ref.ToString());
            }
        }

        // LUCENE-3590, AIOOBE if you append to a charsref with offset != 0
        [Test]
        public virtual void TestAppendChars()
        {
            char[] chars = new char[] { 'a', 'b', 'c', 'd' };
            CharsRef c = new CharsRef(chars, 1, 3); // bcd
            c.Append(new char[] { 'e' }, 0, 1);
            Assert.AreEqual("bcde", c.ToString());
        }

        // LUCENE-3590, AIOOBE if you copy to a charsref with offset != 0
        [Test]
        public virtual void TestCopyChars()
        {
            char[] chars = new char[] { 'a', 'b', 'c', 'd' };
            CharsRef c = new CharsRef(chars, 1, 3); // bcd
            char[] otherchars = new char[] { 'b', 'c', 'd', 'e' };
            c.CopyChars(otherchars, 0, 4);
            Assert.AreEqual("bcde", c.ToString());
        }

        // LUCENE-3590, AIOOBE if you copy to a charsref with offset != 0
        [Test]
        public virtual void TestCopyCharsRef()
        {
            char[] chars = new char[] { 'a', 'b', 'c', 'd' };
            CharsRef c = new CharsRef(chars, 1, 3); // bcd
            char[] otherchars = new char[] { 'b', 'c', 'd', 'e' };
            c.CopyChars(new CharsRef(otherchars, 0, 4));
            Assert.AreEqual("bcde", c.ToString());
        }

        // LUCENENET NOTE: Removed the CharAt(int) method from the
        // ICharSequence interface and replaced with this[int]
        //// LUCENE-3590: fix charsequence to fully obey interface
        //[Test]
        //public virtual void TestCharSequenceCharAt()
        //{
        //    CharsRef c = new CharsRef("abc");

        //    Assert.AreEqual('b', c.CharAt(1));

        //    try
        //    {
        //        c.CharAt(-1);
        //        Assert.Fail();
        //    }
        //    catch (Exception expected) when (expected.IsIndexOutOfBoundsException())
        //    {
        //        // expected exception
        //    }

        //    try
        //    {
        //        c.CharAt(3);
        //        Assert.Fail();
        //    }
        //    catch (Exception expected) when (expected.IsIndexOutOfBoundsException())
        //    {
        //        // expected exception
        //    }
        //}

        // LUCENE-3590: fix charsequence to fully obey interface
        [Test, LuceneNetSpecific]
        public virtual void TestCharSequenceIndexer()
        {
            CharsRef c = new CharsRef("abc");

            Assert.AreEqual('b', c[1]);

            try
            {
                var _ = c[-1];
                Assert.Fail();
            }
            catch (Exception expected) when (expected.IsIndexOutOfBoundsException())
            {
                // expected exception
            }

            try
            {
                var _ = c[3];
                Assert.Fail();
            }
            catch (Exception expected) when (expected.IsIndexOutOfBoundsException())
            {
                // expected exception
            }
        }

        // LUCENE-3590: fix off-by-one in subsequence, and fully obey interface
        // LUCENE-4671: fix subSequence
        [Test]
        public virtual void TestCharSequenceSubSequence()
        {
            ICharSequence[] sequences = {
                new CharsRef("abc"),
                new CharsRef("0abc".ToCharArray(), 1, 3),
                new CharsRef("abc0".ToCharArray(), 0, 3),
                new CharsRef("0abc0".ToCharArray(), 1, 3)
            };

            foreach (ICharSequence c in sequences)
            {
                DoTestSequence(c);
            }
        }

        private void DoTestSequence(ICharSequence c)
        {
            // slice
            Assert.AreEqual("a", c.Subsequence(0, 1 - 0).ToString()); // LUCENENET: Corrected 2nd parameter
            // mid subsequence
            Assert.AreEqual("b", c.Subsequence(1, 2 - 1).ToString()); // LUCENENET: Corrected 2nd parameter
            // end subsequence
            Assert.AreEqual("bc", c.Subsequence(1, 3 - 1).ToString()); // LUCENENET: Corrected 2nd parameter
            // empty subsequence
            Assert.AreEqual("", c.Subsequence(0, 0 - 0).ToString()); // LUCENENET: Corrected 2nd parameter

            try
            {
                c.Subsequence(-1, 1 - -1); // LUCENENET: Corrected 2nd parameter
                Assert.Fail();
            }
            catch (Exception expected) when (expected.IsIndexOutOfBoundsException())
            {
                // expected exception
            }

            try
            {
                c.Subsequence(0, -1 - 0); // LUCENENET: Corrected 2nd parameter
                Assert.Fail();
            }
            catch (Exception expected) when (expected.IsIndexOutOfBoundsException())
            {
                // expected exception
            }

            try
            {
                c.Subsequence(0, 4 - 0); // LUCENENET: Corrected 2nd parameter
                Assert.Fail();
            }
            catch (Exception expected) when (expected.IsIndexOutOfBoundsException())
            {
                // expected exception
            }

            try
            {
                c.Subsequence(2, 1 - 2); // LUCENENET: Corrected 2nd parameter
                Assert.Fail();
            }
            catch (Exception expected) when (expected.IsIndexOutOfBoundsException())
            {
                // expected exception
            }
        }

#if FEATURE_SERIALIZABLE

        [Test, LuceneNetSpecific]
        public void TestSerialization()
        {
            var chars = "The quick brown fox jumped over the lazy dog.".ToCharArray();

            var charsRef = new CharsRef(chars, 8, 10);

            Assert.AreEqual(10, charsRef.Length);
            Assert.AreSame(chars, charsRef.Chars);
            Assert.AreEqual(chars, charsRef.Chars);
            Assert.AreEqual(8, charsRef.Offset);

            var clone = Clone(charsRef);

            Assert.AreEqual(10, clone.Length);
            Assert.AreNotSame(chars, clone.Chars);
            Assert.AreEqual(chars, clone.Chars);
            Assert.AreEqual(8, clone.Offset);
        }
#endif

        #region Test Data

        // Reuse existing string test data but add offsets
        public static IEnumerable<object[]> StringSliceTestData2
        {
            get
            {
                foreach (var data in TestHelpers.StringSliceTestData)
                {
                    // data = [text, start, length]
                    var text = (string)data[0];
                    var start = (int)data[1];
                    var length = (int)data[2];

                    // Offset = 0 case
                    yield return new object[] { text, start, length, 0 };

                    // Offset = 1 case, if feasible
                    if (text.Length > 0)
                        yield return new object[] { text, start, length, 1 };
                }
            }
        }

        public static IEnumerable<object[]> StringSlice1ArgTestOutOfRangeData2
        {
            get
            {
                foreach (var data in TestHelpers.StringSlice2ArgTestOutOfRangeData)
                {
                    // [text, start]
                    yield return new object[] { (string)data[0], (int)data[1], 0 };
                    yield return new object[] { (string)data[0], (int)data[1], 1 };
                }
            }
        }

        public static IEnumerable<object[]> StringSlice2ArgTestOutOfRangeData2
        {
            get
            {
                foreach (var data in TestHelpers.StringSlice3ArgTestOutOfRangeData)
                {
                    // [text, start, length]
                    yield return new object[] { (string)data[0], (int)data[1], (int)data[2], 0 };
                    yield return new object[] { (string)data[0], (int)data[1], (int)data[2], 1 };
                }
            }
        }

        #endregion

        #region AsSpan


        [Test]
        [LuceneNetSpecific]
        public static void Test_AsSpan_Nullary()
        {
            var buffer = "Hello".ToCharArray();

            var cr = new CharsRef(buffer) { Offset = 0, Length = buffer.Length };

            ReadOnlySpan<char> span = cr.AsSpan();
            char[] expected = cr.Chars.AsSpan(cr.Offset, cr.Length).ToArray();
            span.Validate(expected);
        }

        [Test]
        [LuceneNetSpecific]
        public static void Test_AsSpan_Empty()
        {
            var cr = new CharsRef(new char[0]) { Offset = 0, Length = 0 };
            ReadOnlySpan<char> span = cr.AsSpan();
            span.ValidateNonNullEmpty();
        }

        [TestCaseSource(nameof(StringSliceTestData2))]
        [LuceneNetSpecific]
        public static unsafe void Test_AsSpan_StartAndLength(string text, int start, int length, int offset)
        {
            var chars = text.ToCharArray();
            var buffer = new char[chars.Length + offset];
            Array.Copy(chars, 0, buffer, offset, chars.Length);

            var cr = new CharsRef(buffer) { Offset = offset, Length = chars.Length };

            if (start == -1)
            {
                Validate(cr, 0, cr.Length, cr.AsSpan());
                Validate(cr, 0, cr.Length, cr.AsSpan(0));
                Validate(cr, 0, cr.Length, cr.AsSpan(0..^0));
            }
            else if (length == -1)
            {
                Validate(cr, start, cr.Length - start, cr.AsSpan(start));
                Validate(cr, start, cr.Length - start, cr.AsSpan(start..));
            }
            else
            {
                Validate(cr, start, length, cr.AsSpan(start, length));
                Validate(cr, start, length, cr.AsSpan(start..(start + length)));
            }

            static unsafe void Validate(CharsRef text, int start, int length, ReadOnlySpan<char> span)
            {
                Assert.AreEqual(length, span.Length);
                fixed (char* pText = &MemoryMarshal.GetReference(text.Chars.AsSpan()))
                {
                    char* expected = pText + text.Offset + start;
                    void* actual = Unsafe.AsPointer(ref MemoryMarshal.GetReference(span));
                    Assert.AreEqual((IntPtr)expected, (IntPtr)actual);
                }
            }
        }

        [TestCaseSource(nameof(StringSlice1ArgTestOutOfRangeData2))]
        [LuceneNetSpecific]
        public static void Test_AsSpan_1Arg_OutOfRange(string text, int start, int offset)
        {
            var chars = text.ToCharArray();
            var buffer = new char[chars.Length + offset];
            Array.Copy(chars, 0, buffer, offset, chars.Length);

            var cr = new CharsRef(buffer) { Offset = offset, Length = chars.Length };

            Assert.Throws<ArgumentOutOfRangeException>(() => cr.AsSpan(start));
        }

        [TestCaseSource(nameof(StringSlice2ArgTestOutOfRangeData2))]
        [LuceneNetSpecific]
        public static void Test_AsSpan_2Arg_OutOfRange(string text, int start, int length, int offset)
        {
            var chars = text.ToCharArray();
            var buffer = new char[chars.Length + offset];
            Array.Copy(chars, 0, buffer, offset, chars.Length);

            var cr = new CharsRef(buffer) { Offset = offset, Length = chars.Length };

            Assert.Throws<ArgumentOutOfRangeException>(() => cr.AsSpan(start, length));
        }

        [TestCase(5, 0, 0)]   // Offset = 0
        [TestCase(5, 2, 0)]
        [TestCase(5, 5, 0)]
        [TestCase(5, 0, 1)]   // Offset = 1
        [TestCase(5, 2, 1)]
        [TestCase(5, 5, 1)]
        [LuceneNetSpecific]
        public static void Test_AsSpan_Index_FromEnd(int length, int startIndex, int offset)
        {
            var chars = new char[length + offset];
            for (int i = 0; i < length; i++) chars[i + offset] = (char)(i + 1);
            var cr = new CharsRef(chars) { Offset = offset, Length = length };

            // From-end Index
            if (startIndex <= length)
            {
                ReadOnlySpan<char> spanFromEnd = cr.AsSpan(^(length - startIndex));
                Assert.AreEqual(length - startIndex, spanFromEnd.Length);
                if (spanFromEnd.Length > 0) Assert.AreEqual((char)(startIndex + 1), spanFromEnd[0]);
            }
        }

        [TestCase(5, -1, 0)]
        [TestCase(5, 6, 0)]
        [TestCase(5, -1, 1)]
        [TestCase(5, 6, 1)]
        [LuceneNetSpecific]
        public static void Test_AsSpan_Index_FromEnd_OutOfRange(int length, int startIndex, int offset)
        {
            var chars = new char[length + offset];
            var cr = new CharsRef(chars) { Offset = offset, Length = length };

            Assert.Throws<ArgumentOutOfRangeException>(() => cr.AsSpan(^(length + 1))); // from-end invalid
        }

        [TestCase(5, 0, 3)]
        [TestCase(5, 1, 2)]
        [TestCase(5, 0, 5)]
        [LuceneNetSpecific]
        public static void Test_AsSpan_Range(int length, int start, int subLength)
        {
            var chars = new char[length + 1];
            for (int i = 0; i < length; i++) chars[i + 1] = (char)(i + 10);
            var cr = new CharsRef(chars) { Offset = 1, Length = length };

            // Range overload using .. syntax
            ReadOnlySpan<char> span = cr.AsSpan(start..(start + subLength));
            Assert.AreEqual(subLength, span.Length);
            if (subLength > 0) Assert.AreEqual((char)(10 + start), span[0]);
        }

        [TestCase(5, 0, 3, 0)]   // length, start, subLength, offset
        [TestCase(5, 1, 2, 0)]
        [TestCase(5, 0, 5, 0)]
        [TestCase(5, 0, 3, 1)]   // Offset = 1
        [TestCase(5, 1, 2, 1)]
        [TestCase(5, 0, 5, 1)]
        [LuceneNetSpecific]
        public static void Test_AsSpan_Range_WithOffset(int length, int start, int subLength, int offset)
        {
            var chars = new char[length + offset];
            for (int i = 0; i < length; i++) chars[i + offset] = (char)(i + 10);
            var cr = new CharsRef(chars) { Offset = offset, Length = length };

            ReadOnlySpan<char> span = cr.AsSpan(start..(start + subLength));
            Assert.AreEqual(subLength, span.Length);
            if (subLength > 0) Assert.AreEqual((char)(10 + start), span[0]);
        }

        [TestCase(5, 3, 3)]
        [TestCase(5, -1, 2)]
        [TestCase(5, 4, 2)]
        [LuceneNetSpecific]
        public static void Test_AsSpan_Range_OutOfRange(int length, int start, int subLength)
        {
            var chars = new char[length];
            var cr = new CharsRef(chars) { Offset = 0, Length = length };
            Assert.Throws<ArgumentOutOfRangeException>(() => cr.AsSpan(start..(start + subLength)));
        }

        [TestCase(5, 0, 5, 1)]  // offset + start + length > bytes.Length -> second check
        [TestCase(5, 2, 3, 1)]  // offset causes spill over backing array
        [TestCase(5, 0, 5, 2)]  // offset + length > bytes.Length
        [LuceneNetSpecific]
        public static void Test_AsSpan_Range_OutOfRange_OffsetCheck(int length, int start, int subLength, int offset)
        {
            var chars = new char[length]; // intentionally too short to trigger the second check
            var cr = new CharsRef(chars) { Offset = offset, Length = length };

            Assert.Throws<ArgumentOutOfRangeException>(() => cr.AsSpan(start, subLength));
            Assert.Throws<ArgumentOutOfRangeException>(() => cr.AsSpan(start..(start + subLength)));
        }

        #endregion

        #region AsMemory

        [TestCase(0, 0)]
        [TestCase(3, 0)]
        [TestCase(3, 1)]
        [TestCase(3, 2)]
        [TestCase(3, 3)]
        [TestCase(10, 0)]
        [TestCase(10, 3)]
        [TestCase(10, 10)]
        [LuceneNetSpecific]
        public static void Test_AsMemory_WithStart(int length, int start)
        {
            Span<char> preload = stackalloc char[length];
            preload.Fill('\0');
            var cr = new CharsRef(preload) { Offset = 0, Length = length };

            var m = cr.AsMemory(start);
            Assert.AreEqual(length - start, m.Length);
            if (start != length)
            {
                cr.Chars[cr.Offset + start] = (char)42;
                Assert.AreEqual((char)42, m.Span[0]);
            }
        }

        [TestCase(0, -1)]
        [TestCase(0, 1)]
        [TestCase(5, 6)]
        [LuceneNetSpecific]
        public static void Test_AsMemory_WithStart_OutOfRange(int length, int start)
        {
            var chars = new char[length];
            var cr = new CharsRef(chars) { Offset = 0, Length = length };
            Assert.Throws<ArgumentOutOfRangeException>(() => cr.AsMemory(start));
        }

        [TestCase(10, 3, 2, 0)]
        [TestCase(10, 3, 2, 2)] // offset
        [LuceneNetSpecific]
        public static void Test_AsMemory_WithStartAndLength(int length, int start, int subLength, int offset)
        {
            Span<char> preload = stackalloc char[length + offset];
            preload.Fill('\0');

            var cr = new CharsRef(preload) { Offset = offset, Length = length };

            var m = cr.AsMemory(start, subLength);
            Assert.AreEqual(subLength, m.Length);
            if (subLength != 0)
            {
                cr.Chars[offset + start] = (char)42;
                Assert.AreEqual((char)42, m.Span[0]);
            }
        }

        [TestCase(0, -1, 0)]
        [TestCase(0, 1, 0)]
        [TestCase(0, 0, -1)]
        [TestCase(0, 0, 1)]
        [TestCase(5, 6, 0)]
        [TestCase(5, 3, 3)]
        [LuceneNetSpecific]
        public static void Test_AsMemory_WithStartAndLength_OutOfRange(int length, int start, int subLength)
        {
            var chars = new char[length];
            var cr = new CharsRef(chars) { Offset = 0, Length = length };
            Assert.Throws<ArgumentOutOfRangeException>(() => cr.AsMemory(start, subLength));
        }

        [TestCase(5, 0, 0)]
        [TestCase(5, 2, 0)]
        [TestCase(5, 5, 0)]
        [TestCase(5, 0, 1)]
        [TestCase(5, 2, 1)]
        [TestCase(5, 5, 1)]
        [LuceneNetSpecific]
        public static void Test_AsMemory_Index_FromEnd(int length, int startIndex, int offset)
        {
            var chars = new char[length + offset];
            for (int i = 0; i < length; i++) chars[i + offset] = (char)(i + 1);
            var cr = new CharsRef(chars) { Offset = offset, Length = length };

            // From-end Index
            ReadOnlyMemory<char> memFromEnd = cr.AsMemory(^(length - startIndex));
            Assert.AreEqual(length - startIndex, memFromEnd.Length);
            if (memFromEnd.Length > 0) Assert.AreEqual((char)(startIndex + 1), memFromEnd.Span[0]);
        }

        [TestCase(5, -1, 0)]
        [TestCase(5, 6, 0)]
        [TestCase(5, -1, 1)]
        [TestCase(5, 6, 1)]
        [LuceneNetSpecific]
        public static void Test_AsMemory_Index_OutOfRange(int length, int startIndex, int offset)
        {
            var chars = new char[length + offset];
            var cr = new CharsRef(chars) { Offset = offset, Length = length };

            Assert.Throws<ArgumentOutOfRangeException>(() => cr.AsMemory(^(length + 1))); // from-end invalid
        }

        [TestCase(5, 0, 5)]
        [TestCase(5, 1, 3)]
        [TestCase(5, 2, 2)]
        [TestCase(5, 0, 0)]
        [LuceneNetSpecific]
        public static void Test_AsMemory_Range(int length, int start, int subLength)
        {
            var chars = new char[length + 1];
            for (int i = 0; i < length; i++) chars[i + 1] = (char)(i + 100);
            var cr = new CharsRef(chars) { Offset = 1, Length = length };

            // Correct: use Range syntax
            ReadOnlyMemory<char> m = cr.AsMemory(start..(start + subLength));
            Assert.AreEqual(subLength, m.Length);
            if (subLength > 0)
            {
                Assert.AreEqual((char)(100 + start), m.Span[0]);
            }

            // Also test start.. for completeness
            ReadOnlyMemory<char> m2 = cr.AsMemory(start..);
            Assert.AreEqual(length - start, m2.Length);
        }

        [TestCase(5, 0, 3, 0)]
        [TestCase(5, 1, 2, 0)]
        [TestCase(5, 0, 5, 0)]
        [TestCase(5, 0, 3, 1)]
        [TestCase(5, 1, 2, 1)]
        [TestCase(5, 0, 5, 1)]
        [LuceneNetSpecific]
        public static void Test_AsMemory_Range_WithOffset(int length, int start, int subLength, int offset)
        {
            var chars = new char[length + offset];
            for (int i = 0; i < length; i++) chars[i + offset] = (char)(i + 10);
            var cr = new CharsRef(chars) { Offset = offset, Length = length };

            ReadOnlyMemory<char> mem = cr.AsMemory(start..(start + subLength));
            Assert.AreEqual(subLength, mem.Length);
            if (subLength > 0) Assert.AreEqual((char)(10 + start), mem.Span[0]);
        }

        [TestCase(5, 0, 6)]
        [TestCase(5, 4, 2)]
        [TestCase(5, 3, -1)]
        [LuceneNetSpecific]
        public static void Test_AsMemory_Range_OutOfRange(int length, int start, int subLength)
        {
            var cr = new CharsRef(new char[length]) { Offset = 0, Length = length };

            Assert.Throws<ArgumentOutOfRangeException>(() => cr.AsMemory(start..(start + subLength)));
        }

        [TestCase(5, 0, 5, 1)]  // offset + start + length > chars.Length -> second check
        [TestCase(5, 2, 3, 1)]  // offset causes spill over backing array
        [TestCase(5, 0, 5, 2)]  // offset + length > chars.Length
        [LuceneNetSpecific]
        public static void Test_AsMemory_Range_OutOfRange_OffsetCheck(int length, int start, int subLength, int offset)
        {
            var chars = new char[length]; // intentionally too short to trigger the second check
            var cr = new CharsRef(chars) { Offset = offset, Length = length };

            Assert.Throws<ArgumentOutOfRangeException>(() => cr.AsMemory(start, subLength));
            Assert.Throws<ArgumentOutOfRangeException>(() => cr.AsMemory(start..(start + subLength)));
        }

        #endregion

        #region Append

        [Test]
        [LuceneNetSpecific]
        public static void Test_Append_WithinCapacity()
        {
            var cr = new CharsRef(new char[10]) { Length = 0 };
            cr.Append(new char[] { (char)1, (char)2, (char)3 });
            cr.Append(new char[] { (char)4, (char)5 });

            Assert.AreEqual(5, cr.Length);
            CollectionAssert.AreEqual(new char[] { (char)1, (char)2, (char)3, (char)4, (char)5 }, cr.AsSpan().ToArray());
        }

        [Test]
        [LuceneNetSpecific]
        public static void Test_Append_ExceedsCapacity()
        {
            var cr = new CharsRef(new char[2]) { Length = 0 };
            cr.Append(new char[] { (char)1, (char)2, (char)3 });

            Assert.AreEqual(3, cr.Length);
            CollectionAssert.AreEqual(new char[] { (char)1, (char)2, (char)3 }, cr.AsSpan().ToArray());
        }

        #endregion

        #region Constructors

        [Test]
        [LuceneNetSpecific]
        public static void Test_FromCharSpan()
        {
            var span = "abc".AsSpan();
            var br = new CharsRef(span);

            Assert.AreEqual(3, br.Length);
            Assert.AreEqual("abc", br.AsSpan().ToString());
        }

        #endregion

        #region CopyChars

        [Test]
        [LuceneNetSpecific]
        public static void Test_CopyChars()
        {
            var br = new CharsRef(new char[10]) { Offset = 0, Length = 0 };
            br.CopyChars("hello".AsSpan());

            Assert.AreEqual("hello", br.AsSpan().ToString());
        }

        #endregion
    }
}
