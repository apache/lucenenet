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

namespace Lucene.Net.Analysis.TokenAttributes
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
    /// Tests for the <see cref="CharTermAttribute"/> class' <see cref="IBufferWriter{T}"/> implementation.
    /// </summary>
    [TestFixture]
    [LuceneNetSpecific]
    public class TestCharTermAttributeImplBufferWriter : LuceneTestCase
    {
        private const char DefaultChar = (char)0;

        // LUCENENET specific: 10 is MIN_BUFFER_SIZE, but it gets oversized
        private static readonly int DefaultBufferSize = ArrayUtil.Oversize(10, RamUsageEstimator.NUM_BYTES_CHAR);

        // LUCENENET specific: note that Clear behaves more like a Reset, which resets the position without clearing the
        // buffer. so this test looks a lot more like the ResetWrittenCount in ArrayBufferWriter tests, than Clear.
        [Test]
        public void Clear()
        {
            var output = new CharTermAttribute();
            int previousAvailable = output.FreeCapacity;
            WriteData(output, 2);
            Assert.True(output.FreeCapacity < previousAvailable);
            Assert.True(output.Length > 0);
            Assert.False(ReadOnlySpan<char>.Empty.SequenceEqual(output.AsSpan()));
            Assert.False(ReadOnlyMemory<char>.Empty.Span.SequenceEqual(output.AsMemory().Span));
            Assert.True(output.AsSpan().SequenceEqual(output.AsMemory().Span));

            ReadOnlyMemory<char> transientMemory = output.AsMemory();
            ReadOnlySpan<char> transientSpan = output.AsSpan();
            char t0 = transientMemory.Span[0];
            char t1 = transientSpan[1];
            Assert.AreNotEqual(DefaultChar, t0);
            Assert.AreNotEqual(DefaultChar, t1);
            output.Clear();
            Assert.AreEqual(t0, transientMemory.Span[0]);
            Assert.AreEqual(t1, transientSpan[1]);

            Assert.AreEqual(0, output.Length);
            Assert.True(ReadOnlySpan<char>.Empty.SequenceEqual(output.AsSpan()));
            Assert.True(ReadOnlyMemory<char>.Empty.Span.SequenceEqual(output.AsMemory().Span));
            Assert.AreEqual(previousAvailable, output.FreeCapacity);
        }

        // LUCENENET: this is equivalent to the ResetWrittenCount test
        [Test]
        public void SetLengthToZero()
        {
            var output = new CharTermAttribute();
            int previousAvailable = output.FreeCapacity;
            WriteData(output, 2);
            Assert.True(output.FreeCapacity < previousAvailable);
            Assert.True(output.Length > 0);
            Assert.False(ReadOnlySpan<char>.Empty.SequenceEqual(output.AsSpan()));
            Assert.False(ReadOnlyMemory<char>.Empty.Span.SequenceEqual(output.AsMemory().Span));
            Assert.True(output.AsSpan().SequenceEqual(output.AsMemory().Span));

            ReadOnlyMemory<char> transientMemory = output.AsMemory();
            ReadOnlySpan<char> transientSpan = output.AsSpan();
            char t0 = transientMemory.Span[0];
            char t1 = transientSpan[1];
            Assert.AreNotEqual(DefaultChar, t0);
            Assert.AreNotEqual(DefaultChar, t1);
            output.Length = 0;
            Assert.AreEqual(t0, transientMemory.Span[0]);
            Assert.AreEqual(t1, transientSpan[1]);

            Assert.AreEqual(0, output.Length);
            Assert.True(ReadOnlySpan<char>.Empty.SequenceEqual(output.AsSpan()));
            Assert.True(ReadOnlyMemory<char>.Empty.Span.SequenceEqual(output.AsMemory().Span));
            Assert.AreEqual(previousAvailable, output.FreeCapacity);
        }

        [Test]
        public void Advance()
        {
            {
                var output = new CharTermAttribute();
                int capacity = output.Capacity;
                Assert.AreEqual(capacity, output.FreeCapacity);
                output.Advance(output.FreeCapacity);
                Assert.AreEqual(capacity, output.Length);
                Assert.AreEqual(0, output.FreeCapacity);
            }

            {
                var output = new CharTermAttribute();
                output.Advance(output.Capacity);
                Assert.AreEqual(output.Capacity, output.Length);
                Assert.AreEqual(0, output.FreeCapacity);
                int previousCapacity = output.Capacity;
                Span<char> _ = output.GetSpan();
                Assert.True(output.Capacity > previousCapacity);
            }

            {
                var output = new CharTermAttribute();
                WriteData(output, 2);
                ReadOnlyMemory<char> previousMemory = output.AsMemory();
                ReadOnlySpan<char> previousSpan = output.AsSpan();
                Assert.True(previousSpan.SequenceEqual(previousMemory.Span));
                output.Advance(10);
                Assert.False(previousMemory.Span.SequenceEqual(output.AsMemory().Span));
                Assert.False(previousSpan.SequenceEqual(output.AsSpan()));
                Assert.True(output.AsSpan().SequenceEqual(output.AsMemory().Span));
            }

            {
                var output = new CharTermAttribute();
                _ = output.GetSpan(20);
                WriteData(output, 10);
                ReadOnlyMemory<char> previousMemory = output.AsMemory();
                ReadOnlySpan<char> previousSpan = output.AsSpan();
                Assert.True(previousSpan.SequenceEqual(previousMemory.Span));
                Assert.Throws<InvalidOperationException>(() => output.Advance(247));
                output.Advance(10);
                Assert.False(previousMemory.Span.SequenceEqual(output.AsMemory().Span));
                Assert.False(previousSpan.SequenceEqual(output.AsSpan()));
                Assert.True(output.AsSpan().SequenceEqual(output.AsMemory().Span));
            }
        }

        [Test]
        public void AdvanceZero()
        {
            var output = new CharTermAttribute();
            WriteData(output, 2);
            Assert.AreEqual(2, output.Length);
            ReadOnlyMemory<char> previousMemory = output.AsMemory();
            ReadOnlySpan<char> previousSpan = output.AsSpan();
            Assert.True(previousSpan.SequenceEqual(previousMemory.Span));
            output.Advance(0);
            Assert.AreEqual(2, output.Length);
            Assert.True(previousMemory.Span.SequenceEqual(output.AsMemory().Span));
            Assert.True(previousSpan.SequenceEqual(output.AsSpan()));
            Assert.True(output.AsSpan().SequenceEqual(output.AsMemory().Span));
        }

        [Test]
        public void InvalidAdvance()
        {
            {
                var output = new CharTermAttribute();
                Assert.Throws<ArgumentException>(() => output.Advance(-1));
                Assert.Throws<InvalidOperationException>(() => output.Advance(output.Capacity + 1));
            }

            {
                var output = new CharTermAttribute();
                WriteData(output, 100);
                Assert.Throws<InvalidOperationException>(() => output.Advance(output.FreeCapacity + 1));
            }
        }

        [Test]
        public void GetSpan_DefaultCtor()
        {
            var output = new CharTermAttribute();
            Span<char> span = output.GetSpan();
            Assert.AreEqual(DefaultBufferSize, span.Length);
        }

        [Test]
        [TestCaseSource(nameof(SizeHints))]
        public void GetSpan_DefaultCtor_WithSizeHint(int sizeHint)
        {
            var output = new CharTermAttribute();
            Span<char> span = output.GetSpan(sizeHint);
            // LUCENENET specific: due to our oversize logic, changed to GreaterOrEqual
            Assert.GreaterOrEqual(span.Length, sizeHint <= DefaultBufferSize ? DefaultBufferSize : sizeHint);
        }

        [Test]
        public void GetMemory_DefaultCtor()
        {
            var output = new CharTermAttribute();
            Memory<char> memory = output.GetMemory();
            Assert.AreEqual(DefaultBufferSize, memory.Length);
        }

        [Test]
        [TestCaseSource(nameof(SizeHints))]
        public void GetMemory_DefaultCtor_WithSizeHint(int sizeHint)
        {
            var output = new CharTermAttribute();
            Memory<char> memory = output.GetMemory(sizeHint);
            // LUCENENET specific: due to our oversize logic, changed to GreaterOrEqual
            Assert.GreaterOrEqual(memory.Length, sizeHint <= DefaultBufferSize ? DefaultBufferSize : sizeHint);
        }

        [Test]
        public void GetMemory_ExceedMaximumBufferSize_WithSmallStartingSize()
        {
            var output = new CharTermAttribute();
            Assert.Throws<OutOfMemoryException>(() => output.GetMemory(int.MaxValue));
        }

        // LUCENENET specific: This test allocates a very large buffer to verify that Advance()
        // performs its bounds check purely arithmetically (termLength vs. termBuffer.Length);
        // Advance() itself never touches buffer memory, and WriteData only touches the first 1,000
        // chars. If the allocation fails with OutOfMemoryException, the test is a no-op. Marked
        // [Slow] due to the large allocation.
        //
        // NOTE: The upstream ArrayBufferWriter test gated this on Windows/macOS, and so do we. The
        // risk is real on Linux but comes from the allocation itself, not from Advance(): under memory
        // overcommit the kernel may grant the large buffer optimistically, then the GC's
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
                    var output = new CharTermAttribute();

                    // LUCENENET specific: we don't (currently) have a ctor that takes a capacity,
                    // so we first request a span to force the buffer to grow to at least this size.
                    // The size stays under ~1.9B so it does not hit the ArrayUtil.Oversize int
                    // overflow clamp; the actual buffer is still oversized past the request.
                    // (The returned span is discarded; the grown buffer is retained by output.)
                    _ = output.GetSpan(1_500_000_000);

                    WriteData(output, 1_000);

                    // LUCENENET specific: the buffer is oversized past the requested size, so the
                    // over-advance boundary is Capacity-relative rather than a fixed request-based value.
                    Assert.Throws<InvalidOperationException>(() => output.Advance(int.MaxValue));
                    Assert.Throws<InvalidOperationException>(() => output.Advance(output.FreeCapacity + 1));
                }
            }
            catch (OutOfMemoryException) { }
        }

        [Test]
        public void GetMemoryAndSpan()
        {
            {
                var output = new CharTermAttribute();
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
                var output = new CharTermAttribute();
                WriteData(output, 2);
                ReadOnlyMemory<char> writtenSoFarMemory = output.AsMemory();
                ReadOnlySpan<char> writtenSoFar = output.AsSpan();
                Assert.True(writtenSoFarMemory.Span.SequenceEqual(writtenSoFar));
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
        public void GetSpanShouldAtLeastDoubleWhenGrowing()
        {
            var output = new CharTermAttribute();
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
                var output = new CharTermAttribute();
                _ = output.GetSpan();
                int previousAvailable = output.FreeCapacity;

                for (int i = 0; i < 10; i++)
                {
                    _ = output.GetSpan();
                    Assert.AreEqual(previousAvailable, output.FreeCapacity);
                }
            }

            {
                var output = new CharTermAttribute();
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
            var output = new CharTermAttribute();
            WriteData(output, 2);
            Assert.Throws<ArgumentException>(() => output.GetSpan(-1));
            Assert.Throws<ArgumentException>(() => output.GetMemory(-1));
        }

        [Test]
        public void MultipleCallsToGetSpan()
        {
            var output = new CharTermAttribute();
            Assert.True(MemoryMarshal.TryGetArray(output.GetMemory(), out ArraySegment<char> array));
            GCHandle pinnedArray = GCHandle.Alloc(array.Array, GCHandleType.Pinned);
            try
            {
                int previousAvailable = output.FreeCapacity;
                Assert.True(previousAvailable >= DefaultBufferSize);
                Assert.True(output.Capacity >= DefaultBufferSize);
                Assert.AreEqual(previousAvailable, output.Capacity);
                Span<char> span = output.GetSpan();
                Assert.True(span.Length >= previousAvailable);
                Assert.True(span.Length >= DefaultBufferSize);
                Span<char> newSpan = output.GetSpan();
                Assert.AreEqual(span.Length, newSpan.Length);
                // LUCENENET specific: changed expected to IntPtr.Zero due to presumed Xunit vs. NUnit differences
                Assert.AreEqual(IntPtr.Zero,
                    Unsafe.ByteOffset(ref MemoryMarshal.GetReference(span), ref MemoryMarshal.GetReference(newSpan)));
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
            var random = new Random(42);

            var data = new char[numChars];

            for (int i = 0; i < numChars; i++)
            {
                data[i] = (char)random.Next(0, char.MaxValue);
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
