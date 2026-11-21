// Based on:
// https://github.com/dotnet/runtime/blob/v9.0.1/src/libraries/System.Memory/tests/ReadOnlySpan/AsSpan.cs
// https://github.com/dotnet/runtime/blob/v9.0.1/src/libraries/System.Memory/tests/Memory/AsMemory.cs
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Lucene.Net;
using Lucene.Net.Attributes;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Assert = Lucene.Net.TestFramework.Assert;

namespace Lucene.Net.Analysis.Compound.Hyphenation
{
    [LuceneNetSpecific]
    [TestFixture]
    public class TestByteVectorExtensions : LuceneTestCase
    {
        // Quick byte to byte conversion - ASCII only
        private static byte[] ToBytes(string text)
        {
            byte[] result = new byte[text.Length];
            for (int i = 0; i < text.Length; i++)
                result[i] = (byte)text[i];
            return result;
        }

        #region AsSpan

        [Test]
        public static void AsSpanNullary()
        {
            ByteVector s = new ByteVector(ToBytes("Hello"));

            ReadOnlySpan<byte> span = s.AsSpan();
            byte[] expected = s.array.AsSpan(0, s.Length).ToArray();
            span.Validate(expected);
        }

        [Test]
        public static void AsSpanEmptyString()
        {
            ByteVector s = new ByteVector();
            ReadOnlySpan<byte> span = s.AsSpan();
            span.ValidateNonNullEmpty();
        }

        [Test]
        public static void AsSpanNullChecked()
        {
#pragma warning disable CA2265 // Do not compare Span<T> to null or default
            ByteVector s = null;
            ReadOnlySpan<byte> span = s.AsSpan();
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
        public static void AsSpanNullNonZeroStartAndLength()
        {
            ByteVector str = null;

            Assert.Throws<ArgumentOutOfRangeException>(() => str.AsSpan(1).DontBox());
            Assert.Throws<ArgumentOutOfRangeException>(() => str.AsSpan(-1).DontBox());

            Assert.Throws<ArgumentOutOfRangeException>(() => str.AsSpan(0, 1).DontBox());
            Assert.Throws<ArgumentOutOfRangeException>(() => str.AsSpan(1, 0).DontBox());
            Assert.Throws<ArgumentOutOfRangeException>(() => str.AsSpan(1, 1).DontBox());
            Assert.Throws<ArgumentOutOfRangeException>(() => str.AsSpan(-1, -1).DontBox());

            Assert.Throws<ArgumentOutOfRangeException>(() => str.AsSpan(new System.Index(1)).DontBox());
            Assert.Throws<ArgumentOutOfRangeException>(() => str.AsSpan(new System.Index(0, fromEnd: true)).DontBox());

            Assert.Throws<ArgumentOutOfRangeException>(() => str.AsSpan(0..1).DontBox());
            Assert.Throws<ArgumentOutOfRangeException>(() => str.AsSpan(new Range(new System.Index(0), new System.Index(0, fromEnd: true))).DontBox());
            Assert.Throws<ArgumentOutOfRangeException>(() => str.AsSpan(new Range(new System.Index(0, fromEnd: true), new System.Index(0))).DontBox());
            Assert.Throws<ArgumentOutOfRangeException>(() => str.AsSpan(new Range(new System.Index(0, fromEnd: true), new System.Index(0, fromEnd: true))).DontBox());
        }

        [TestCaseSource(typeof(TestHelpers), nameof(TestHelpers.StringSliceTestData))]
        public static void AsSpan_StartAndLength(string textStr, int start, int length)
        {
            ByteVector text = new ByteVector(ToBytes(textStr));

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


            static unsafe void Validate(ByteVector text, int start, int length, ReadOnlySpan<byte> span)
            {
                Assert.AreEqual(length, span.Length);
                fixed (byte* pText = text.array)
                {
                    // Unsafe.AsPointer is safe here since it's pinned (since text and span should be the same string)
                    byte* expected = pText + start;
                    void* actual = Unsafe.AsPointer(ref MemoryMarshal.GetReference(span));
                    Assert.AreEqual((IntPtr)expected, (IntPtr)actual);
                }
            }
        }

        [TestCaseSource(typeof(TestHelpers), nameof(TestHelpers.StringSlice2ArgTestOutOfRangeData))]
        public static unsafe void AsSpan_2Arg_OutOfRange(string textStr, int start)
        {
            ByteVector text = new ByteVector(ToBytes(textStr));

            Assert.Throws<ArgumentOutOfRangeException>("start", () => text.AsSpan(start).DontBox());
            if (start >= 0)
            {
                Assert.Throws<ArgumentOutOfRangeException>("startIndex", () => text.AsSpan(new System.Index(start)).DontBox());
            }
        }

        [TestCaseSource(typeof(TestHelpers), nameof(TestHelpers.StringSlice3ArgTestOutOfRangeData))]
        public static unsafe void AsSpan_3Arg_OutOfRange(string textStr, int start, int length)
        {
            ByteVector text = new ByteVector(ToBytes(textStr));

            Assert.Throws<ArgumentOutOfRangeException>("start", () => text.AsSpan(start, length).DontBox());
            if (start >= 0 && length >= 0 && start + length >= 0)
            {
                Assert.Throws<ArgumentOutOfRangeException>("length", () => text.AsSpan(start..(start + length)).DontBox());
            }
        }

        #endregion AsSpan

        #region AsMemory

        [TestCase(0, 0)]
        [TestCase(3, 0)]
        [TestCase(3, 1)]
        [TestCase(3, 2)]
        [TestCase(3, 3)]
        [TestCase(10, 0)]
        [TestCase(10, 3)]
        [TestCase(10, 10)]
        public static void AsMemoryWithStart(int length, int start)
        {
            Span<byte> preload = stackalloc byte[length];
            preload.Fill(0);
            ByteVector a = new ByteVector(preload, length);
            ReadOnlyMemory<byte> m = a.AsMemory(start);
            Assert.AreEqual(length - start, m.Length);
            if (start != length)
            {
                a[start] = (byte)42;
                Assert.AreEqual((byte)42, m.Span[0]);
            }
        }

        [TestCase(0, 0, 0)]
        [TestCase(3, 0, 3)]
        [TestCase(3, 1, 2)]
        [TestCase(3, 2, 1)]
        [TestCase(3, 3, 0)]
        [TestCase(10, 0, 5)]
        [TestCase(10, 3, 2)]
        public static void AsMemoryWithStartAndLength(int length, int start, int subLength)
        {
            Span<byte> preload = stackalloc byte[length];
            preload.Fill(0);
            ByteVector a = new ByteVector(preload, length);
            ReadOnlyMemory<byte> m = a.AsMemory(start, subLength);
            Assert.AreEqual(subLength, m.Length);
            if (subLength != 0)
            {
                a[start] = (byte)42;
                Assert.AreEqual((byte)42, m.Span[0]);
            }
        }

        [TestCase(0, -1)]
        [TestCase(0, 1)]
        [TestCase(5, 6)]
        public static void AsMemoryWithStartNegative(int length, int start)
        {
            ByteVector a = new ByteVector(Array.Empty<byte>(), length);
            Assert.Throws<ArgumentOutOfRangeException>(() => a.AsMemory(start));
        }

        [TestCase(0, -1, 0)]
        [TestCase(0, 1, 0)]
        [TestCase(0, 0, -1)]
        [TestCase(0, 0, 1)]
        [TestCase(5, 6, 0)]
        [TestCase(5, 3, 3)]
        public static void WithStartAndLengthNegative(int length, int start, int subLength)
        {
            ByteVector a = new ByteVector(Array.Empty<byte>(), length);
            Assert.Throws<ArgumentOutOfRangeException>(() => a.AsMemory(start, subLength));
        }

        #endregion AsMemory
    }
}
