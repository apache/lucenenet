// Adapted from MIT-licensed ArrayBufferWriter tests here:
// https://github.com/dotnet/runtime/blob/v10.0.6/src/libraries/System.Memory/tests/ArrayBufferWriter/ArrayBufferWriterTests.T.cs

using Lucene.Net.Attributes;
using Lucene.Net.Util;
using NUnit.Framework;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Lucene.Net.Analysis.Util
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

    /// <summary>
    /// Tests for <see cref="OpenStringBuilder"/>'s <see cref="IBufferWriter{T}"/> implementation.
    /// </summary>
    [TestFixture]
    [LuceneNetSpecific]
    public class TestOpenStringBuilderBufferWriter : LuceneTestCase
    {
        private const char DefaultChar = (char)0;
        private const int DefaultBufferSize = 32; // copied from hardcoded value in OpenStringBuilder

        // LUCENENET specific: works around a .NET Framework x64 RyuJIT bug where
        // ReadOnlyMemory<char>.Span returns an empty span when the property is read
        // inline on a local inside a large method (it only reproduces with the
        // System.Memory polyfill; modern .NET is unaffected). Forcing the .Span read
        // into its own non-inlined method makes the JIT compile it correctly. These
        // helpers preserve the original assertion intent (comparing the Memory view
        // against the Span view) on all target frameworks.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool SpanEqualsMemory(ReadOnlySpan<char> span, ReadOnlyMemory<char> memory)
            => span.SequenceEqual(memory.Span);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static bool MemoryEqualsMemory(ReadOnlyMemory<char> a, ReadOnlyMemory<char> b)
            => a.Span.SequenceEqual(b.Span);

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static char MemoryElementAt(ReadOnlyMemory<char> memory, int index)
            => memory.Span[index];

        [Test]
        public void ArrayBufferWriter_Ctor()
        {
            {
                var output = new OpenStringBuilder();
                Assert.AreEqual(DefaultBufferSize, output.FreeCapacity);
                Assert.AreEqual(DefaultBufferSize, output.Capacity);
                Assert.AreEqual(0, output.Length);
                Assert.True(ReadOnlySpan<char>.Empty.SequenceEqual(output.AsSpan()));
                Assert.True(MemoryEqualsMemory(ReadOnlyMemory<char>.Empty, output.AsMemory()));
            }

            {
                var output = new OpenStringBuilder(200);
                Assert.True(output.FreeCapacity >= 200);
                Assert.True(output.Capacity >= 200);
                Assert.AreEqual(0, output.Length);
                Assert.True(ReadOnlySpan<char>.Empty.SequenceEqual(output.AsSpan()));
                Assert.True(MemoryEqualsMemory(ReadOnlyMemory<char>.Empty, output.AsMemory()));
            }

            {
                OpenStringBuilder output = default;
                Assert.Null(output);
            }
        }

        [Test]
        public void Invalid_Ctor()
        {
            // LUCENENET specific - changed to ArgumentOutOfRangeException
            Assert.Throws<ArgumentOutOfRangeException>(() => new OpenStringBuilder(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new OpenStringBuilder(-1));
            Assert.Throws<OutOfMemoryException>(() => new OpenStringBuilder(int.MaxValue));
        }

        [Test]
        public void Reset()
        {
            var output = new OpenStringBuilder(256);
            int previousAvailable = output.FreeCapacity;
            WriteData(output, 2);
            Assert.True(output.FreeCapacity < previousAvailable);
            Assert.True(output.Length > 0);
            Assert.False(ReadOnlySpan<char>.Empty.SequenceEqual(output.AsSpan()));
            Assert.False(MemoryEqualsMemory(ReadOnlyMemory<char>.Empty, output.AsMemory()));
            Assert.True(SpanEqualsMemory(output.AsSpan(), output.AsMemory()));

            ReadOnlyMemory<char> transientMemory = output.AsMemory();
            ReadOnlySpan<char> transientSpan = output.AsSpan();
            char t0 = MemoryElementAt(transientMemory, 0);
            char t1 = transientSpan[1];
            Assert.AreNotEqual(DefaultChar, t0);
            Assert.AreNotEqual(DefaultChar, t1);
            output.Reset();
            Assert.AreEqual(t0, MemoryElementAt(transientMemory, 0));
            Assert.AreEqual(t1, transientSpan[1]);

            Assert.AreEqual(0, output.Length);
            Assert.True(ReadOnlySpan<char>.Empty.SequenceEqual(output.AsSpan()));
            Assert.True(MemoryEqualsMemory(ReadOnlyMemory<char>.Empty, output.AsMemory()));
            Assert.AreEqual(previousAvailable, output.FreeCapacity);
        }

        [Test]
        public void Advance()
        {
            {
                var output = new OpenStringBuilder();
                int capacity = output.Capacity;
                Assert.AreEqual(capacity, output.FreeCapacity);
                output.Advance(output.FreeCapacity);
                Assert.AreEqual(capacity, output.Length);
                Assert.AreEqual(0, output.FreeCapacity);
            }

            {
                var output = new OpenStringBuilder();
                output.Advance(output.Capacity);
                Assert.AreEqual(output.Capacity, output.Length);
                Assert.AreEqual(0, output.FreeCapacity);
                int previousCapacity = output.Capacity;
                Span<char> _ = output.GetSpan();
                Assert.True(output.Capacity > previousCapacity);
            }

            {
                var output = new OpenStringBuilder(256);
                WriteData(output, 2);
                ReadOnlyMemory<char> previousMemory = output.AsMemory();
                ReadOnlySpan<char> previousSpan = output.AsSpan();
                Assert.True(SpanEqualsMemory(previousSpan, previousMemory));
                output.Advance(10);
                Assert.False(MemoryEqualsMemory(previousMemory, output.AsMemory()));
                Assert.False(previousSpan.SequenceEqual(output.AsSpan()));
                Assert.True(SpanEqualsMemory(output.AsSpan(), output.AsMemory()));
            }

            {
                var output = new OpenStringBuilder();
                _ = output.GetSpan(20);
                WriteData(output, 10);
                ReadOnlyMemory<char> previousMemory = output.AsMemory();
                ReadOnlySpan<char> previousSpan = output.AsSpan();
                Assert.True(SpanEqualsMemory(previousSpan, previousMemory));
                Assert.Throws<InvalidOperationException>(() => output.Advance(247));
                output.Advance(10);
                Assert.False(MemoryEqualsMemory(previousMemory, output.AsMemory()));
                Assert.False(previousSpan.SequenceEqual(output.AsSpan()));
                Assert.True(SpanEqualsMemory(output.AsSpan(), output.AsMemory()));
            }
        }

        [Test]
        public void AdvanceZero()
        {
            var output = new OpenStringBuilder();
            WriteData(output, 2);
            Assert.AreEqual(2, output.Length);
            ReadOnlyMemory<char> previousMemory = output.AsMemory();
            ReadOnlySpan<char> previousSpan = output.AsSpan();
            Assert.True(SpanEqualsMemory(previousSpan, previousMemory));
            output.Advance(0);
            Assert.AreEqual(2, output.Length);
            Assert.True(MemoryEqualsMemory(previousMemory, output.AsMemory()));
            Assert.True(previousSpan.SequenceEqual(output.AsSpan()));
            Assert.True(SpanEqualsMemory(output.AsSpan(), output.AsMemory()));
        }

        [Test]
        public void InvalidAdvance()
        {
            {
                var output = new OpenStringBuilder();
                Assert.Throws<ArgumentOutOfRangeException>(() => output.Advance(-1));
                Assert.Throws<InvalidOperationException>(() => output.Advance(output.Capacity + 1));
            }

            {
                var output = new OpenStringBuilder();
                WriteData(output, 100);
                Assert.Throws<InvalidOperationException>(() => output.Advance(output.FreeCapacity + 1));
            }
        }

        [Test]
        public void GetSpan_DefaultCtor()
        {
            var output = new OpenStringBuilder();
            Span<char> span = output.GetSpan();
            Assert.AreEqual(DefaultBufferSize, span.Length);
        }

        [Test]
        [TestCaseSource(nameof(SizeHints))]
        public void GetSpan_DefaultCtor_WithSizeHint(int sizeHint)
        {
            var output = new OpenStringBuilder();
            Span<char> span = output.GetSpan(sizeHint);
            Assert.AreEqual(sizeHint <= DefaultBufferSize ? DefaultBufferSize : sizeHint, span.Length);
        }

        [Test]
        public void GetSpan_InitSizeCtor()
        {
            var output = new OpenStringBuilder(100);
            Span<char> span = output.GetSpan();
            Assert.AreEqual(100, span.Length);
        }

        [Test]
        [TestCaseSource(nameof(SizeHints))]
        public void GetSpan_InitSizeCtor_WithSizeHint(int sizeHint)
        {
            {
                var output = new OpenStringBuilder(256);
                Span<char> span = output.GetSpan(sizeHint);
                // LUCENENET specific: due to our oversize logic, changed to GreaterOrEqual and removed sizeHint addend
                Assert.GreaterOrEqual(span.Length, sizeHint <= 256 ? 256 : sizeHint);
            }

            {
                var output = new OpenStringBuilder(1000);
                Span<char> span = output.GetSpan(sizeHint);
                // LUCENENET specific: due to our oversize logic, changed to GreaterOrEqual and removed sizeHint addend
                Assert.GreaterOrEqual(span.Length, sizeHint <= 1000 ? 1000 : sizeHint);
            }
        }

        [Test]
        public void GetMemory_DefaultCtor()
        {
            var output = new OpenStringBuilder();
            Memory<char> memory = output.GetMemory();
            Assert.AreEqual(DefaultBufferSize, memory.Length);
        }

        [Test]
        [TestCaseSource(nameof(SizeHints))]
        public void GetMemory_DefaultCtor_WithSizeHint(int sizeHint)
        {
            var output = new OpenStringBuilder();
            Memory<char> memory = output.GetMemory(sizeHint);
            Assert.AreEqual(sizeHint <= DefaultBufferSize ? DefaultBufferSize : sizeHint, memory.Length);
        }

        [Test]
        public void GetMemory_ExceedMaximumBufferSize_WithSmallStartingSize()
        {
            var output = new OpenStringBuilder(256);
            Assert.Throws<OutOfMemoryException>(() => output.GetMemory(int.MaxValue));
        }

        [Test]
        public void GetMemory_InitSizeCtor()
        {
            var output = new OpenStringBuilder(100);
            Memory<char> memory = output.GetMemory();
            Assert.AreEqual(100, memory.Length);
        }

        [Test]
        [TestCaseSource(nameof(SizeHints))]
        public void GetMemory_InitSizeCtor_WithSizeHint(int sizeHint)
        {
            {
                var output = new OpenStringBuilder(256);
                Memory<char> memory = output.GetMemory(sizeHint);
                // LUCENENET specific: due to our oversize logic, changed to GreaterOrEqual and removed sizeHint addend
                Assert.GreaterOrEqual(memory.Length, sizeHint <= 256 ? 256 : sizeHint);
            }

            {
                var output = new OpenStringBuilder(1000);
                Memory<char> memory = output.GetMemory(sizeHint);
                // LUCENENET specific: due to our oversize logic, changed to GreaterOrEqual and removed sizeHint addend
                Assert.GreaterOrEqual(memory.Length, sizeHint <= 1000 ? 1000 : sizeHint);
            }
        }

        // LUCENENET specific: This test allocates a very large buffer to verify that Advance()
        // performs its bounds check purely arithmetically (m_len vs. m_buf.Length); Advance() itself
        // never touches buffer memory, and WriteData only touches the first 1,000 chars. If the
        // allocation fails with OutOfMemoryException, the test is a no-op. Marked [Slow] due to the
        // large allocation.
        //
        // NOTE: The upstream ArrayBufferWriter test gated this on Windows/macOS, and so do we. The
        // risk is real on Linux but comes from the allocation itself, not from Advance(): under memory
        // overcommit the kernel may grant new char[2_000_000_000] optimistically, then the GC's
        // zero-initialization of the array touches every page, which can invoke the OOM killer (an
        // uncatchable SIGKILL, not an OutOfMemoryException) and tear down the whole test run.
        [Test]
        [Slow]
        public void InvalidAdvance_Large()
        {
            // LUCENENET specific: skip on Linux (see note above); the large allocation can be killed
            // by the OOM killer with an uncatchable SIGKILL rather than a catchable OutOfMemoryException.
            Assume.That(!Constants.LINUX, "Skipped on Linux: the large allocation may trigger the OOM killer.");

            try
            {
                {
                    var output = new OpenStringBuilder(2_000_000_000);
                    WriteData(output, 1_000);
                    Assert.Throws<InvalidOperationException>(() => output.Advance(int.MaxValue));
                    Assert.Throws<InvalidOperationException>(() => output.Advance(2_000_000_000 - 1_000 + 1));
                }
            }
            catch (OutOfMemoryException) { }
        }

        [Test]
        public void GetMemoryAndSpan()
        {
            {
                var output = new OpenStringBuilder();
                WriteData(output, 2);
                Span<char> span = output.GetSpan();
                Memory<char> memory = output.GetMemory();
                Span<char> memorySpan = memory.Span;
                Assert.True(span.Length > 0);
                Assert.True(memorySpan.Length > 0);
                Assert.AreEqual(span.Length, memorySpan.Length);
                for (int i = 0; i < span.Length; i++)
                {
                    Assert.AreEqual(DefaultChar, span[i]);
                    Assert.AreEqual(DefaultChar, memorySpan[i]);
                }
            }

            {
                var output = new OpenStringBuilder();
                WriteData(output, 2);
                ReadOnlyMemory<char> writtenSoFarMemory = output.AsMemory();
                ReadOnlySpan<char> writtenSoFar = output.AsSpan();
                Assert.True(SpanEqualsMemory(writtenSoFar, writtenSoFarMemory));
                int previousAvailable = output.FreeCapacity;
                Span<char> span = output.GetSpan(500);
                Assert.True(span.Length >= 500);
                Assert.True(output.FreeCapacity >= 500);
                Assert.True(output.FreeCapacity > previousAvailable);

                Assert.AreEqual(writtenSoFar.Length, output.Length);
                Assert.False(writtenSoFar.SequenceEqual(span.Slice(0, output.Length)));

                Memory<char> memory = output.GetMemory();
                Span<char> memorySpan = memory.Span;
                Assert.True(span.Length >= 500);
                Assert.True(memorySpan.Length >= 500);
                Assert.AreEqual(span.Length, memorySpan.Length);
                for (int i = 0; i < span.Length; i++)
                {
                    Assert.AreEqual(DefaultChar, span[i]);
                    Assert.AreEqual(DefaultChar, memorySpan[i]);
                }

                memory = output.GetMemory(500);
                memorySpan = memory.Span;
                Assert.True(memorySpan.Length >= 500);
                Assert.AreEqual(span.Length, memorySpan.Length);
                for (int i = 0; i < memorySpan.Length; i++)
                {
                    Assert.AreEqual(DefaultChar, memorySpan[i]);
                }
            }
        }

        [Test]
        public void GetSpanShouldAtleastDoubleWhenGrowing()
        {
            var output = new OpenStringBuilder(256);
            WriteData(output, 100);
            int previousAvailable = output.FreeCapacity;

            _ = output.GetSpan(previousAvailable);
            Assert.AreEqual(previousAvailable, output.FreeCapacity);

            _ = output.GetSpan(previousAvailable + 1);
            Assert.True(output.FreeCapacity >= previousAvailable * 2);
        }

        [Test]
        public void GetSpanOnlyGrowsAboveThreshold()
        {
            {
                var output = new OpenStringBuilder();
                _ = output.GetSpan();
                int previousAvailable = output.FreeCapacity;

                for (int i = 0; i < 10; i++)
                {
                    _ = output.GetSpan();
                    Assert.AreEqual(previousAvailable, output.FreeCapacity);
                }
            }

            {
                var output = new OpenStringBuilder();
                _ = output.GetSpan(10);
                int previousAvailable = output.FreeCapacity;

                for (int i = 0; i < 10; i++)
                {
                    _ = output.GetSpan(previousAvailable);
                    Assert.AreEqual(previousAvailable, output.FreeCapacity);
                }
            }
        }

        [Test]
        public void InvalidGetMemoryAndSpan()
        {
            var output = new OpenStringBuilder();
            WriteData(output, 2);
            Assert.Throws<ArgumentOutOfRangeException>(() => output.GetSpan(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => output.GetMemory(-1));
        }

        [Test]
        public void MultipleCallsToGetSpan()
        {
            var output = new OpenStringBuilder(300);
            Assert.True(MemoryMarshal.TryGetArray(output.GetMemory(), out ArraySegment<char> array));
            GCHandle pinnedArray = GCHandle.Alloc(array.Array, GCHandleType.Pinned);
            try
            {
                int previousAvailable = output.FreeCapacity;
                Assert.True(previousAvailable >= 300);
                Assert.True(output.Capacity >= 300);
                Assert.AreEqual(previousAvailable, output.Capacity);
                Span<char> span = output.GetSpan();
                Assert.True(span.Length >= previousAvailable);
                Assert.True(span.Length >= 256);
                Span<char> newSpan = output.GetSpan();
                Assert.AreEqual(span.Length, newSpan.Length);
                // LUCENENET specific: changed expected to IntPtr.Zero due to presumed Xunit vs. NUnit differences
                Assert.AreEqual(IntPtr.Zero, Unsafe.ByteOffset(ref MemoryMarshal.GetReference(span), ref MemoryMarshal.GetReference(newSpan)));
                Assert.AreEqual(span.Length, output.GetSpan().Length);
            }
            finally
            {
                pinnedArray.Free();
            }
        }

        protected static void WriteData(IBufferWriter<char> bufferWriter, int numChars)
        {
            Span<char> outputSpan = bufferWriter.GetSpan(numChars);
            Debug.Assert(outputSpan.Length >= numChars);

            var data = new char[numChars];

            for (int i = 0; i < numChars; i++)
            {
                data[i] = (char)Random.Next(0, char.MaxValue);
            }

            data.CopyTo(outputSpan);

            bufferWriter.Advance(numChars);
        }

        public static IEnumerable<object[]> SizeHints
        {
            get
            {
                return new List<object[]>
                {
                    new object[] { 0 },
                    new object[] { 1 },
                    new object[] { 2 },
                    new object[] { 3 },
                    new object[] { 99 },
                    new object[] { 100 },
                    new object[] { 101 },
                    new object[] { 255 },
                    new object[] { 256 },
                    new object[] { 257 },
                    new object[] { 1000 },
                    new object[] { 2000 },
                };
            }
        }
    }
}
