using Lucene.Net.Attributes;
using NUnit.Framework;
using System;
using System.IO;

namespace Lucene.Net.IO
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
    [LuceneNetSpecific]
    public class TestTextReaderExtensions
    {
        [Test]
        public virtual void TestReadWithStringReader()
        {
            using StringReader reader = new StringReader("foobar");

            Span<char> buffer = stackalloc char[4];

            Assert.AreEqual(4, TextReaderExtensions.Read(reader, buffer));
            Assert.AreEqual("foob", new string(buffer));

            Assert.AreEqual(2, TextReaderExtensions.Read(reader, buffer));
            Assert.AreEqual("ar", new string(buffer.Slice(0, 2)));

            Assert.AreEqual(0, TextReaderExtensions.Read(reader, buffer));
        }

        [Test]
        public virtual void TestReadWithOrdinaryTextReader()
        {
            using ChunkedTextReader reader = new ChunkedTextReader("foobar", 2);

            Span<char> buffer = stackalloc char[4];

            // Read performs one underlying read, so it may return fewer
            // characters than the destination span can hold.
            Assert.AreEqual(2, TextReaderExtensions.Read(reader, buffer));
            Assert.AreEqual("fo", new string(buffer.Slice(0, 2)));

            Assert.AreEqual(2, TextReaderExtensions.Read(reader, buffer));
            Assert.AreEqual("ob", new string(buffer.Slice(0, 2)));

            Assert.AreEqual(2, TextReaderExtensions.Read(reader, buffer));
            Assert.AreEqual("ar", new string(buffer.Slice(0, 2)));

            Assert.AreEqual(0, TextReaderExtensions.Read(reader, buffer));
        }

        [Test]
        public virtual void TestReadBlockWithStringReader()
        {
            using StringReader reader = new StringReader("foobar");

            Span<char> buffer = stackalloc char[4];

            Assert.AreEqual(4, TextReaderExtensions.ReadBlock(reader, buffer));
            Assert.AreEqual("foob", new string(buffer));

            Assert.AreEqual(2, TextReaderExtensions.ReadBlock(reader, buffer));
            Assert.AreEqual("ar", new string(buffer.Slice(0, 2)));

            Assert.AreEqual(0, TextReaderExtensions.ReadBlock(reader, buffer));
        }

        [Test]
        public virtual void TestReadBlockWithPartialReads()
        {
            using ChunkedTextReader reader = new ChunkedTextReader("foobar", 2);

            Span<char> buffer = stackalloc char[4];

            // ReadBlock must continue reading until the destination is full
            // or the underlying reader reaches EOF.
            Assert.AreEqual(4, TextReaderExtensions.ReadBlock(reader, buffer));
            Assert.AreEqual("foob", new string(buffer));

            Assert.AreEqual(2, TextReaderExtensions.ReadBlock(reader, buffer));
            Assert.AreEqual("ar", new string(buffer.Slice(0, 2)));

            Assert.AreEqual(0, TextReaderExtensions.ReadBlock(reader, buffer));
        }

        [Test]
        public virtual void TestReadWithSpanTextReadable()
        {
            using SpanTextReader reader = new SpanTextReader("foobar");

            Span<char> buffer = stackalloc char[4];

            Assert.AreEqual(4, TextReaderExtensions.Read(reader, buffer));
            Assert.AreEqual("foob", new string(buffer));

            Assert.AreEqual(2, TextReaderExtensions.Read(reader, buffer));
            Assert.AreEqual("ar", new string(buffer.Slice(0, 2)));

            Assert.AreEqual(0, TextReaderExtensions.Read(reader, buffer));

            Assert.AreEqual(3, reader.SpanReadCount);
        }

        [Test]
        public virtual void TestReadBlockWithSpanTextReadable()
        {
            using SpanTextReader reader = new SpanTextReader("foobar");

            Span<char> buffer = stackalloc char[4];

            Assert.AreEqual(4, TextReaderExtensions.ReadBlock(reader, buffer));
            Assert.AreEqual("foob", new string(buffer));

            Assert.AreEqual(2, TextReaderExtensions.ReadBlock(reader, buffer));
            Assert.AreEqual("ar", new string(buffer.Slice(0, 2)));

            Assert.AreEqual(0, TextReaderExtensions.ReadBlock(reader, buffer));

            Assert.AreEqual(3, reader.SpanBlockReadCount);
        }

        [Test]
        public virtual void TestReadEmptySpan()
        {
            using StringReader reader = new StringReader("foobar");

            Span<char> buffer = Span<char>.Empty;

            Assert.AreEqual(0, TextReaderExtensions.Read(reader, buffer));
            Assert.AreEqual(0, TextReaderExtensions.ReadBlock(reader, buffer));

            Assert.AreEqual('f', reader.Read());
        }

        [Test]
        public virtual void TestReadNullReader()
        {
            Assert.Throws<ArgumentNullException>(
                () => TextReaderExtensions.Read(null, stackalloc char[4]));

            Assert.Throws<ArgumentNullException>(
                () => TextReaderExtensions.ReadBlock(null, stackalloc char[4]));
        }

        [Test]
        public virtual void TestReadNormalizesLegacyEndOfInput()
        {
            using LegacyEofTextReader reader = new LegacyEofTextReader();

            Span<char> buffer = stackalloc char[4];

            Assert.AreEqual(0, TextReaderExtensions.Read(reader, buffer));
            Assert.AreEqual(0, TextReaderExtensions.ReadBlock(reader, buffer));
        }

        private sealed class ChunkedTextReader : TextReader
        {
            private readonly string value;
            private readonly int maxRead;
            private int position;

            public ChunkedTextReader(string value, int maxRead)
            {
                this.value = value;
                this.maxRead = maxRead;
            }

            public override int Read(char[] buffer, int index, int count)
            {
                if (buffer is null)
                    throw new ArgumentNullException(nameof(buffer));

                if (index < 0)
                    throw new ArgumentOutOfRangeException(nameof(index));

                if (count < 0)
                    throw new ArgumentOutOfRangeException(nameof(count));

                if (index > buffer.Length - count)
                    throw new ArgumentException();

                if (position >= value.Length)
                    return 0;

                int charsToCopy = Math.Min(
                    Math.Min(count, maxRead),
                    value.Length - position);

                value.CopyTo(position, buffer, index, charsToCopy);
                position += charsToCopy;

                return charsToCopy;
            }
        }

        private sealed class SpanTextReader : TextReader, ISpanTextReadable
        {
            private readonly string value;
            private int position;

            public SpanTextReader(string value)
            {
                this.value = value;
            }

            public int SpanReadCount { get; private set; }

            public int SpanBlockReadCount { get; private set; }

            public override int Read(char[] buffer, int index, int count)
            {
                if (position >= value.Length)
                    return 0;

                int charsToCopy = Math.Min(count, value.Length - position);
                value.CopyTo(position, buffer, index, charsToCopy);
                position += charsToCopy;

                return charsToCopy;
            }

#if FEATURE_TEXTREADER_SPAN
            public override int Read(Span<char> buffer)
#else
            int ISpanTextReadable.Read(Span<char> buffer)
#endif
            {
                SpanReadCount++;

                if (buffer.IsEmpty || position >= value.Length)
                    return 0;

                int charsToCopy = Math.Min(buffer.Length, value.Length - position);
                value.AsSpan(position, charsToCopy).CopyTo(buffer);
                position += charsToCopy;

                return charsToCopy;
            }

#if FEATURE_TEXTREADER_SPAN
            public override int ReadBlock(Span<char> buffer)
#else
            int ISpanTextReadable.ReadBlock(Span<char> buffer)
#endif
            {
                SpanBlockReadCount++;

                if (buffer.IsEmpty || position >= value.Length)
                    return 0;

                int charsToCopy = Math.Min(buffer.Length, value.Length - position);
                value.AsSpan(position, charsToCopy).CopyTo(buffer);
                position += charsToCopy;

                return charsToCopy;
            }
        }

        private sealed class LegacyEofTextReader : TextReader
        {
            public override int Read(char[] buffer, int index, int count)
            {
                return -1;
            }
        }
    }
}
