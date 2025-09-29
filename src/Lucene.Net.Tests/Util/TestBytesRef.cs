using J2N.Text;
using Lucene.Net.Attributes;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
    public class TestBytesRef : LuceneTestCase
    {
        [Test]
        public virtual void TestEmpty()
        {
            BytesRef b = new BytesRef();
            Assert.AreEqual(BytesRef.EMPTY_BYTES, b.Bytes);
            Assert.AreEqual(0, b.Offset);
            Assert.AreEqual(0, b.Length);
        }

        [Test]
        public virtual void TestFromBytes()
        {
            var bytes = new[] { (byte)'a', (byte)'b', (byte)'c', (byte)'d' };
            BytesRef b = new BytesRef(bytes);
            Assert.AreEqual(bytes, b.Bytes);
            Assert.AreEqual(0, b.Offset);
            Assert.AreEqual(4, b.Length);

            BytesRef b2 = new BytesRef(bytes, 1, 3);
            Assert.AreEqual("bcd", b2.Utf8ToString());

            Assert.IsFalse(b.Equals(b2));
        }

        [Test]
        public virtual void TestFromChars()
        {
            for (int i = 0; i < 100; i++)
            {
                string s = TestUtil.RandomUnicodeString(Random);
                string s2 = new BytesRef(s).Utf8ToString();
                Assert.AreEqual(s, s2);
            }

            // only for 4.x
            Assert.AreEqual("\uFFFF", new BytesRef("\uFFFF").Utf8ToString());
        }

        [Test, LuceneNetSpecific]
        public virtual void TestFromCharSequence()
        {
            for (int i = 0; i < 100; i++)
            {
                ICharSequence s = new StringCharSequence(TestUtil.RandomUnicodeString(Random));
                ICharSequence s2 = new BytesRef(s).Utf8ToString().AsCharSequence();
                Assert.AreEqual(s, s2);
            }

            // only for 4.x
            Assert.AreEqual("\uFFFF", new BytesRef("\uFFFF").Utf8ToString());
        }

        // LUCENE-3590, AIOOBE if you append to a bytesref with offset != 0
        [Test]
        public virtual void TestAppend()
        {
            var bytes = new[] { (byte)'a', (byte)'b', (byte)'c', (byte)'d' };
            BytesRef b = new BytesRef(bytes, 1, 3); // bcd
            b.Append(new BytesRef("e"));
            Assert.AreEqual("bcde", b.Utf8ToString());
        }

        // LUCENE-3590, AIOOBE if you copy to a bytesref with offset != 0
        [Test]
        public virtual void TestCopyBytes()
        {
            var bytes = new[] { (byte)'a', (byte)'b', (byte)'c', (byte)'d' };
            BytesRef b = new BytesRef(bytes, 1, 3); // bcd
            b.CopyBytes(new BytesRef("bcde"));
            Assert.AreEqual("bcde", b.Utf8ToString());
        }

#if FEATURE_SERIALIZABLE

        [Test, LuceneNetSpecific]
        public void TestSerialization()
        {
            byte[] bytes = new byte[] { 44, 66, 77, 33, 99, 13, 74, 26 };

            var bytesRef = new BytesRef(bytes, 2, 4);

            Assert.AreEqual(4, bytesRef.Length);
            Assert.AreSame(bytes, bytesRef.Bytes);
            Assert.AreEqual(bytes, bytesRef.Bytes);
            Assert.AreEqual(2, bytesRef.Offset);

            var clone = Clone(bytesRef);

            Assert.AreEqual(4, clone.Length);
            Assert.AreNotSame(bytes, clone.Bytes);
            Assert.AreEqual(bytes, clone.Bytes);
            Assert.AreEqual(2, clone.Offset);
        }
#endif


        #region Test Data

        // Reuse existing string test data but add offsets
        public static IEnumerable<object[]> ByteSliceTestData
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

        public static IEnumerable<object[]> ByteSlice1ArgTestOutOfRangeData
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

        public static IEnumerable<object[]> ByteSlice2ArgTestOutOfRangeData
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
            var bytes = System.Text.Encoding.UTF8.GetBytes("Hello");
            var buffer = new byte[bytes.Length];
            Array.Copy(bytes, 0, buffer, 0, bytes.Length);

            var br = new BytesRef(buffer) { Offset = 0, Length = bytes.Length };

            ReadOnlySpan<byte> span = br.AsSpan();
            byte[] expected = br.Bytes.AsSpan(br.Offset, br.Length).ToArray();
            span.Validate(expected);
        }

        [Test]
        [LuceneNetSpecific]
        public static void Test_AsSpan_Empty()
        {
            var br = new BytesRef(new byte[0]) { Offset = 0, Length = 0 };
            ReadOnlySpan<byte> span = br.AsSpan();
            span.ValidateNonNullEmpty();
        }

        [TestCaseSource(nameof(ByteSliceTestData))]
        [LuceneNetSpecific]
        public static unsafe void Test_AsSpan_StartAndLength(string text, int start, int length, int offset)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(text);
            var buffer = new byte[bytes.Length + offset];
            Array.Copy(bytes, 0, buffer, offset, bytes.Length);

            var br = new BytesRef(buffer) { Offset = offset, Length = bytes.Length };

            if (start == -1)
            {
                Validate(br, 0, br.Length, br.AsSpan());
                Validate(br, 0, br.Length, br.AsSpan(0));
                Validate(br, 0, br.Length, br.AsSpan(0..^0));
            }
            else if (length == -1)
            {
                Validate(br, start, br.Length - start, br.AsSpan(start));
                Validate(br, start, br.Length - start, br.AsSpan(start..));
            }
            else
            {
                Validate(br, start, length, br.AsSpan(start, length));
                Validate(br, start, length, br.AsSpan(start..(start + length)));
            }

            static unsafe void Validate(BytesRef text, int start, int length, ReadOnlySpan<byte> span)
            {
                Assert.AreEqual(length, span.Length);
                fixed (byte* pText = &MemoryMarshal.GetReference(text.Bytes.AsSpan()))
                {
                    byte* expected = pText + text.Offset + start;
                    void* actual = Unsafe.AsPointer(ref MemoryMarshal.GetReference(span));
                    Assert.AreEqual((IntPtr)expected, (IntPtr)actual);
                }
            }
        }

        [TestCaseSource(nameof(ByteSlice1ArgTestOutOfRangeData))]
        [LuceneNetSpecific]
        public static void Test_AsSpan_1Arg_OutOfRange(string text, int start, int offset)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(text);
            var buffer = new byte[bytes.Length + offset];
            Array.Copy(bytes, 0, buffer, offset, bytes.Length);

            var br = new BytesRef(buffer) { Offset = offset, Length = bytes.Length };

            Assert.Throws<ArgumentOutOfRangeException>(() => br.AsSpan(start));
        }

        [TestCaseSource(nameof(ByteSlice2ArgTestOutOfRangeData))]
        [LuceneNetSpecific]
        public static void Test_AsSpan_2Arg_OutOfRange(string text, int start, int length, int offset)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(text);
            var buffer = new byte[bytes.Length + offset];
            Array.Copy(bytes, 0, buffer, offset, bytes.Length);

            var br = new BytesRef(buffer) { Offset = offset, Length = bytes.Length };

            Assert.Throws<ArgumentOutOfRangeException>(() => br.AsSpan(start, length));
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
            var bytes = new byte[length + offset];
            for (int i = 0; i < length; i++) bytes[i + offset] = (byte)(i + 1);
            var br = new BytesRef(bytes) { Offset = offset, Length = length };

            // From-end Index
            if (startIndex <= length)
            {
                ReadOnlySpan<byte> spanFromEnd = br.AsSpan(^(length - startIndex));
                Assert.AreEqual(length - startIndex, spanFromEnd.Length);
                if (spanFromEnd.Length > 0) Assert.AreEqual((byte)(startIndex + 1), spanFromEnd[0]);
            }
        }

        [TestCase(5, -1, 0)]
        [TestCase(5, 6, 0)]
        [TestCase(5, -1, 1)]
        [TestCase(5, 6, 1)]
        [LuceneNetSpecific]
        public static void Test_AsSpan_Index_FromEnd_OutOfRange(int length, int startIndex, int offset)
        {
            var bytes = new byte[length + offset];
            var br = new BytesRef(bytes) { Offset = offset, Length = length };

            Assert.Throws<ArgumentOutOfRangeException>(() => br.AsSpan(^(length + 1))); // from-end invalid
        }

        [TestCase(5, 0, 3)]
        [TestCase(5, 1, 2)]
        [TestCase(5, 0, 5)]
        [LuceneNetSpecific]
        public static void Test_AsSpan_Range(int length, int start, int subLength)
        {
            var bytes = new byte[length + 1];
            for (int i = 0; i < length; i++) bytes[i + 1] = (byte)(i + 10);
            var br = new BytesRef(bytes) { Offset = 1, Length = length };

            // Range overload using .. syntax
            ReadOnlySpan<byte> span = br.AsSpan(start..(start + subLength));
            Assert.AreEqual(subLength, span.Length);
            if (subLength > 0) Assert.AreEqual((byte)(10 + start), span[0]);
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
            var bytes = new byte[length + offset];
            for (int i = 0; i < length; i++) bytes[i + offset] = (byte)(i + 10);
            var br = new BytesRef(bytes) { Offset = offset, Length = length };

            ReadOnlySpan<byte> span = br.AsSpan(start..(start + subLength));
            Assert.AreEqual(subLength, span.Length);
            if (subLength > 0) Assert.AreEqual((byte)(10 + start), span[0]);
        }

        [TestCase(5, 3, 3)]
        [TestCase(5, -1, 2)]
        [TestCase(5, 4, 2)]
        [LuceneNetSpecific]
        public static void Test_AsSpan_Range_OutOfRange(int length, int start, int subLength)
        {
            var bytes = new byte[length];
            var br = new BytesRef(bytes) { Offset = 0, Length = length };
            Assert.Throws<ArgumentOutOfRangeException>(() => br.AsSpan(start..(start + subLength)));
        }

        [TestCase(5, 0, 5, 1)]  // offset + start + length > bytes.Length -> second check
        [TestCase(5, 2, 3, 1)]  // offset causes spill over backing array
        [TestCase(5, 0, 5, 2)]  // offset + length > bytes.Length
        [LuceneNetSpecific]
        public static void Test_AsSpan_Range_OutOfRange_OffsetCheck(int length, int start, int subLength, int offset)
        {
            var bytes = new byte[length]; // intentionally too short to trigger the second check
            var br = new BytesRef(bytes) { Offset = offset, Length = length };

            Assert.Throws<ArgumentOutOfRangeException>(() => br.AsSpan(start, subLength));
            Assert.Throws<ArgumentOutOfRangeException>(() => br.AsSpan(start..(start + subLength)));
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
            Span<byte> preload = stackalloc byte[length];
            preload.Fill((byte)0);
            var br = new BytesRef(preload) { Offset = 0, Length = length };

            var m = br.AsMemory(start);
            Assert.AreEqual(length - start, m.Length);
            if (start != length)
            {
                br.Bytes[br.Offset + start] = 42;
                Assert.AreEqual((byte)42, m.Span[0]);
            }
        }

        [TestCase(0, -1)]
        [TestCase(0, 1)]
        [TestCase(5, 6)]
        [LuceneNetSpecific]
        public static void Test_AsMemory_WithStart_OutOfRange(int length, int start)
        {
            var bytes = new byte[length];
            var br = new BytesRef(bytes) { Offset = 0, Length = length };
            Assert.Throws<ArgumentOutOfRangeException>(() => br.AsMemory(start));
        }

        [TestCase(10, 3, 2, 0)]
        [TestCase(10, 3, 2, 2)] // offset
        [LuceneNetSpecific]
        public static void Test_AsMemory_WithStartAndLength(int length, int start, int subLength, int offset)
        {
            Span<byte> preload = stackalloc byte[length + offset];
            preload.Fill((byte)0);

            var br = new BytesRef(preload) { Offset = offset, Length = length };

            var m = br.AsMemory(start, subLength);
            Assert.AreEqual(subLength, m.Length);
            if (subLength != 0)
            {
                br.Bytes[offset + start] = 42;
                Assert.AreEqual((byte)42, m.Span[0]);
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
            var bytes = new byte[length];
            var br = new BytesRef(bytes) { Offset = 0, Length = length };
            Assert.Throws<ArgumentOutOfRangeException>(() => br.AsMemory(start, subLength));
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
            var bytes = new byte[length + offset];
            for (int i = 0; i < length; i++) bytes[i + offset] = (byte)(i + 1);
            var br = new BytesRef(bytes) { Offset = offset, Length = length };

            // From-end Index
            ReadOnlyMemory<byte> memFromEnd = br.AsMemory(^(length - startIndex));
            Assert.AreEqual(length - startIndex, memFromEnd.Length);
            if (memFromEnd.Length > 0) Assert.AreEqual((byte)(startIndex + 1), memFromEnd.Span[0]);
        }

        [TestCase(5, -1, 0)]
        [TestCase(5, 6, 0)]
        [TestCase(5, -1, 1)]
        [TestCase(5, 6, 1)]
        [LuceneNetSpecific]
        public static void Test_AsMemory_Index_OutOfRange(int length, int startIndex, int offset)
        {
            var bytes = new byte[length + offset];
            var br = new BytesRef(bytes) { Offset = offset, Length = length };

            Assert.Throws<ArgumentOutOfRangeException>(() => br.AsMemory(^(length + 1))); // from-end invalid
        }

        [TestCase(5, 0, 5)]
        [TestCase(5, 1, 3)]
        [TestCase(5, 2, 2)]
        [TestCase(5, 0, 0)]
        [LuceneNetSpecific]
        public static void Test_AsMemory_Range(int length, int start, int subLength)
        {
            var bytes = new byte[length + 1];
            for (int i = 0; i < length; i++) bytes[i + 1] = (byte)(i + 100);
            var br = new BytesRef(bytes) { Offset = 1, Length = length };

            // Correct: use Range syntax
            ReadOnlyMemory<byte> m = br.AsMemory(start..(start + subLength));
            Assert.AreEqual(subLength, m.Length);
            if (subLength > 0)
            {
                Assert.AreEqual((byte)(100 + start), m.Span[0]);
            }

            // Also test start.. for completeness
            ReadOnlyMemory<byte> m2 = br.AsMemory(start..);
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
            var bytes = new byte[length + offset];
            for (int i = 0; i < length; i++) bytes[i + offset] = (byte)(i + 10);
            var br = new BytesRef(bytes) { Offset = offset, Length = length };

            ReadOnlyMemory<byte> mem = br.AsMemory(start..(start + subLength));
            Assert.AreEqual(subLength, mem.Length);
            if (subLength > 0) Assert.AreEqual((byte)(10 + start), mem.Span[0]);
        }

        [TestCase(5, 0, 6)]
        [TestCase(5, 4, 2)]
        [TestCase(5, 3, -1)]
        [LuceneNetSpecific]
        public static void Test_AsMemory_Range_OutOfRange(int length, int start, int subLength)
        {
            var br = new BytesRef(new byte[length]) { Offset = 0, Length = length };

            Assert.Throws<ArgumentOutOfRangeException>(() => br.AsMemory(start..(start + subLength)));
        }

        [TestCase(5, 0, 5, 1)]  // offset + start + length > bytes.Length -> second check
        [TestCase(5, 2, 3, 1)]  // offset causes spill over backing array
        [TestCase(5, 0, 5, 2)]  // offset + length > bytes.Length
        [LuceneNetSpecific]
        public static void Test_AsMemory_Range_OutOfRange_OffsetCheck(int length, int start, int subLength, int offset)
        {
            var bytes = new byte[length]; // intentionally too short to trigger the second check
            var br = new BytesRef(bytes) { Offset = offset, Length = length };

            Assert.Throws<ArgumentOutOfRangeException>(() => br.AsMemory(start, subLength));
            Assert.Throws<ArgumentOutOfRangeException>(() => br.AsMemory(start..(start + subLength)));
        }

        #endregion

        #region Append

        [Test]
        [LuceneNetSpecific]
        public static void Test_Append_WithinCapacity()
        {
            var br = new BytesRef(new byte[10]) { Length = 0 };
            br.Append(new byte[] { 1, 2, 3 });
            br.Append(new byte[] { 4, 5 });

            Assert.AreEqual(5, br.Length);
            CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 4, 5 }, br.AsSpan().ToArray());
        }

        [Test]
        [LuceneNetSpecific]
        public static void Test_Append_ExceedsCapacity()
        {
            var br = new BytesRef(new byte[2]) { Length = 0 };
            br.Append(new byte[] { 1, 2, 3 });

            Assert.AreEqual(3, br.Length);
            CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, br.AsSpan().ToArray());
        }

        #endregion

        #region Constructors

        [Test]
        [LuceneNetSpecific]
        public static void Test_FromByteSpan()
        {
            var span = new byte[] { 10, 20, 30 }.AsSpan();
            var br = new BytesRef(span);

            Assert.AreEqual(3, br.Length);
            CollectionAssert.AreEqual(new byte[] { 10, 20, 30 }, br.AsSpan().ToArray());
        }

        [Test]
        [LuceneNetSpecific]
        public static void Test_FromCharSpan()
        {
            var span = "abc".AsSpan();
            var br = new BytesRef(span);

            Assert.AreEqual(3, br.Length);
            Assert.AreEqual("abc", System.Text.Encoding.UTF8.GetString(br.AsSpan()));
        }

        #endregion

        #region CopyChars

        [Test]
        [LuceneNetSpecific]
        public static void Test_CopyChars()
        {
            var br = new BytesRef(new byte[10]) { Offset = 0, Length = 0 };
            br.CopyChars("hello".AsSpan());

            Assert.AreEqual("hello", System.Text.Encoding.UTF8.GetString(br.AsSpan()));
        }

        [Test]
        [LuceneNetSpecific]
        public static void Test_CopyChars_UnpairedSurrogates()
        {
            var br = new BytesRef(new byte[10]) { Offset = 0, Length = 0 };
            // Invalid surrogate pair -> should encode replacement U+FFFD
            br.CopyChars("\uD800".AsSpan());

            string decoded = System.Text.Encoding.UTF8.GetString(br.AsSpan());
            Assert.AreEqual("\uFFFD", decoded);
        }

        #endregion
    }
}
