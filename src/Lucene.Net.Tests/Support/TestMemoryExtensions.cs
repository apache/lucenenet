// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using J2N.Text;
using Lucene.Net.Analysis.TokenAttributes;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net
{
    [TestFixture]
    public class TestMemoryExtensions : LuceneTestCase
    {
        #region AsSpan (ICharTermAttribute)

        [Test]
        public static void CharTermAttributeAsSpanNullary()
        {
            ICharTermAttribute s = new CharTermAttribute();
            s.Append("Hello");

            ReadOnlySpan<char> span = s.AsSpan();
            char[] expected = s.Buffer.AsSpan(0, s.Length).ToArray();
            span.Validate(expected);
        }

        [Test]
        public static void CharTermAttributeAsSpanEmptyString()
        {
            ICharTermAttribute s = new CharTermAttribute();
            ReadOnlySpan<char> span = s.AsSpan();
            span.ValidateNonNullEmpty();
        }

        [Test]
        public static void CharTermAttributeAsSpanNullChecked()
        {
#pragma warning disable CA2265 // Do not compare Span<T> to null or default
            ICharTermAttribute s = null;
            ReadOnlySpan<char> span = s.AsSpan();
            span.Validate();
            Assert.True(span == default);

            span = s.AsSpan(0);
            span.Validate();
            Assert.True(span == default);

            span = s.AsSpan(0, 0);
            span.Validate();
            Assert.True(span == default);
#pragma warning restore CA2265 // Do not compare Span<T> to null or default
        }

        [Test]
        public static void CharTermAttributeAsSpanNullNonZeroStartAndLength()
        {
            ICharTermAttribute str = null;

            Assert.Throws<ArgumentOutOfRangeException>(() => str.AsSpan(1).DontBox());
            Assert.Throws<ArgumentOutOfRangeException>(() => str.AsSpan(-1).DontBox());

            Assert.Throws<ArgumentOutOfRangeException>(() => str.AsSpan(0, 1).DontBox());
            Assert.Throws<ArgumentOutOfRangeException>(() => str.AsSpan(1, 0).DontBox());
            Assert.Throws<ArgumentOutOfRangeException>(() => str.AsSpan(1, 1).DontBox());
            Assert.Throws<ArgumentOutOfRangeException>(() => str.AsSpan(-1, -1).DontBox());

            Assert.Throws<ArgumentOutOfRangeException>(() => str.AsSpan(new System.Index(1)).DontBox());
            Assert.Throws<ArgumentOutOfRangeException>(() => str.AsSpan(new System.Index(0, fromEnd: true)).DontBox());

            Assert.Throws<ArgumentNullException>(() => str.AsSpan(0..1).DontBox());
            Assert.Throws<ArgumentNullException>(() => str.AsSpan(new Range(new System.Index(0), new System.Index(0, fromEnd: true))).DontBox());
            Assert.Throws<ArgumentNullException>(() => str.AsSpan(new Range(new System.Index(0, fromEnd: true), new System.Index(0))).DontBox());
            Assert.Throws<ArgumentNullException>(() => str.AsSpan(new Range(new System.Index(0, fromEnd: true), new System.Index(0, fromEnd: true))).DontBox());
        }

        [TestCaseSource(typeof(TestHelpers), nameof(TestHelpers.StringSliceTestData))]
        public static void CharTermAttributeAsSpan_StartAndLength(string textStr, int start, int length)
        {
            ICharTermAttribute text = new CharTermAttribute();
            text.Append(textStr);

            if (start == -1)
            {
                Validate(text, 0, text.Length, text.AsSpan());
                Validate(text, 0, text.Length, text.AsSpan(0));
                Validate(text, 0, text.Length, text.AsSpan(0..^0));
            }
            else if (length == -1)
            {
                Validate(text, start, text.Length - start, text.AsSpan(start));
                Validate(text, start, text.Length - start, text.AsSpan(start..));
            }
            else
            {
                Validate(text, start, length, text.AsSpan(start, length));
                Validate(text, start, length, text.AsSpan(start..(start + length)));
            }


            static unsafe void Validate(ICharTermAttribute text, int start, int length, ReadOnlySpan<char> span)
            {
                Assert.AreEqual(length, span.Length);
                fixed (char* pText = text.Buffer)
                {
                    // Unsafe.AsPointer is safe here since it's pinned (since text and span should be the same string)
                    char* expected = pText + start;
                    void* actual = Unsafe.AsPointer(ref MemoryMarshal.GetReference(span));
                    Assert.AreEqual((IntPtr)expected, (IntPtr)actual);
                }
            }
        }

        [TestCaseSource(typeof(TestHelpers), nameof(TestHelpers.StringSlice2ArgTestOutOfRangeData))]
        public static unsafe void CharTermAttributeAsSpan_2Arg_OutOfRange(string textStr, int start)
        {
            ICharTermAttribute text = new CharTermAttribute();
            text.Append(textStr);

            Assert.Throws<ArgumentOutOfRangeException>("start", () => text.AsSpan(start).DontBox());
            if (start >= 0)
            {
                Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => text.AsSpan(new System.Index(start)).DontBox());
            }
        }

        [TestCaseSource(typeof(TestHelpers), nameof(TestHelpers.StringSlice3ArgTestOutOfRangeData))]
        public static unsafe void CharTermAttributeAsSpan_3Arg_OutOfRange(string textStr, int start, int length)
        {
            ICharTermAttribute text = new CharTermAttribute();
            text.Append(textStr);

            Assert.Throws<ArgumentOutOfRangeException>("start", () => text.AsSpan(start, length).DontBox());
            if (start >= 0 && length >= 0 && start + length >= 0)
            {
                Assert.Throws<ArgumentOutOfRangeException>("length", () => text.AsSpan(start..(start + length)).DontBox());
            }
        }

        #endregion AsSpan (ICharTermAttribute)

        #region AsMemory (ICharTermAttribute)

        [TestCase(0, 0)]
        [TestCase(3, 0)]
        [TestCase(3, 1)]
        [TestCase(3, 2)]
        [TestCase(3, 3)]
        [TestCase(10, 0)]
        [TestCase(10, 3)]
        [TestCase(10, 10)]
        public static void CharTermAttributeAsMemoryWithStart(int length, int start)
        {
            ICharTermAttribute a = new CharTermAttribute();
            a.ResizeBuffer(length);
            Span<char> preload = stackalloc char[length];
            preload.Fill('\0');
            a.Append(preload);
            ReadOnlyMemory<char> m = a.AsMemory(start);
            Assert.AreEqual(length - start, m.Length);
            if (start != length)
            {
                a[start] = (char)42;
                Assert.AreEqual(42, m.Span[0]);
            }
        }

        [TestCase(0, 0, 0)]
        [TestCase(3, 0, 3)]
        [TestCase(3, 1, 2)]
        [TestCase(3, 2, 1)]
        [TestCase(3, 3, 0)]
        [TestCase(10, 0, 5)]
        [TestCase(10, 3, 2)]
        public static void CharTermAttributeAsMemoryWithStartAndLength(int length, int start, int subLength)
        {
            ICharTermAttribute a = new CharTermAttribute();
            a.ResizeBuffer(length);
            Span<char> preload = stackalloc char[length];
            preload.Fill('\0');
            a.Append(preload);

            ReadOnlyMemory<char> m = a.AsMemory(start, subLength);
            Assert.AreEqual(subLength, m.Length);
            if (subLength != 0)
            {
                a[start] = (char)42;
                Assert.AreEqual(42, m.Span[0]);
            }
        }

        [TestCase(0, -1)]
        [TestCase(0, 1)]
        [TestCase(5, 6)]
        public static void CharTermAttributeAsMemoryWithStartNegative(int length, int start)
        {
            ICharTermAttribute a = new CharTermAttribute();
            a.ResizeBuffer(length);
            Assert.Throws<ArgumentOutOfRangeException>(() => a.AsMemory(start));
        }

        [TestCase(0, -1, 0)]
        [TestCase(0, 1, 0)]
        [TestCase(0, 0, -1)]
        [TestCase(0, 0, 1)]
        [TestCase(5, 6, 0)]
        [TestCase(5, 3, 3)]
        public static void CharTermAttributeWithStartAndLengthNegative(int length, int start, int subLength)
        {
            ICharTermAttribute a = new CharTermAttribute();
            a.ResizeBuffer(length);
            Assert.Throws<ArgumentOutOfRangeException>(() => a.AsMemory(start, subLength));
        }

        #endregion AsMemory (ICharTermAttribute)
    }
}
