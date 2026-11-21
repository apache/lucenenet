using Lucene.Net.Attributes;
using Lucene.Net.Text;
using NUnit.Framework;
using System;
using System.IO;
using System.Threading;

namespace Lucene.Net.Support.IO
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
    public class TestBufferedTextReader
    {
        [Test]
        public void TestConstructor()
        {
            Assert.Throws<ArgumentNullException>(
                () => new BufferedTextReader(null!));

            Assert.Throws<ArgumentOutOfRangeException>(
                () => new BufferedTextReader(new StringReader(""), 0));

            Assert.Throws<ArgumentOutOfRangeException>(
                () => new BufferedTextReader(new StringReader(""), -1));

            using var reader = new BufferedTextReader(new StringReader(""));
            Assert.That(reader.Peek(), Is.EqualTo(-1));
        }

        [Test]
        public void TestTryReadLine()
        {
            const string input = "first\r\nsecond\rthird\n\nfifth";

            using var reader = new BufferedTextReader(
                new StringReader(input), bufferSize: 3);

            ValueStringBuilder builder = new(stackalloc char[16]);

            try
            {
                AssertNextLine(reader, ref builder, "first");
                AssertNextLine(reader, ref builder, "second");
                AssertNextLine(reader, ref builder, "third");
                AssertNextLine(reader, ref builder, "");
                AssertNextLine(reader, ref builder, "fifth");

                Assert.That(reader.TryReadLine(ref builder), Is.False);
                Assert.That(builder.Length, Is.Zero);
            }
            finally
            {
                builder.Dispose();
            }
        }

        [Test]
        public void TestTryReadLineEmptyInput()
        {
            using var reader = new BufferedTextReader(new StringReader(""));

            ValueStringBuilder builder = new(stackalloc char[16]);

            try
            {
                Assert.That(reader.TryReadLine(ref builder), Is.False);
                Assert.That(builder.Length, Is.Zero);
            }
            finally
            {
                builder.Dispose();
            }
        }

        [Test]
        public void TestTryReadLineEmptyLine()
        {
            using var reader = new BufferedTextReader(new StringReader("\n"));

            ValueStringBuilder builder = new(stackalloc char[16]);

            try
            {
                Assert.That(reader.TryReadLine(ref builder), Is.True);
                Assert.That(builder.Length, Is.Zero);

                Assert.That(reader.TryReadLine(ref builder), Is.False);
            }
            finally
            {
                builder.Dispose();
            }
        }

        [Test]
        public void TestTryReadLineFinalLineWithoutTerminator()
        {
            using var reader = new BufferedTextReader(
                new StringReader("first\nlast"));

            ValueStringBuilder builder = new(stackalloc char[16]);

            try
            {
                AssertNextLine(reader, ref builder, "first");
                AssertNextLine(reader, ref builder, "last");

                Assert.That(reader.TryReadLine(ref builder), Is.False);
            }
            finally
            {
                builder.Dispose();
            }
        }

        [Test]
        public void TestTryReadLineLongerThanBuffer()
        {
            const string expected = "abcdefghijklmnopqrstuvwxyz";

            using var reader = new BufferedTextReader(
                new StringReader(expected + "\nnext"), bufferSize: 3);

            ValueStringBuilder builder = new(stackalloc char[4]);

            try
            {
                AssertNextLine(reader, ref builder, expected);
                AssertNextLine(reader, ref builder, "next");

                Assert.That(reader.TryReadLine(ref builder), Is.False);
            }
            finally
            {
                builder.Dispose();
            }
        }

        [Test]
        public void TestTryReadLineCRLFAtBufferBoundary()
        {
            // With a one-character buffer, CR and LF must always be
            // encountered in separate buffer reads.
            using var reader = new BufferedTextReader(
                new StringReader("a\r\nb\r\nc"), bufferSize: 1);

            ValueStringBuilder builder = new(stackalloc char[16]);

            try
            {
                AssertNextLine(reader, ref builder, "a");
                AssertNextLine(reader, ref builder, "b");
                AssertNextLine(reader, ref builder, "c");

                Assert.That(reader.TryReadLine(ref builder), Is.False);
            }
            finally
            {
                builder.Dispose();
            }
        }

        [Test]
        public void TestPeek()
        {
            using var reader = new BufferedTextReader(
                new StringReader("abc"), bufferSize: 2);

            Assert.That(reader.Peek(), Is.EqualTo((int)'a'));
            Assert.That(reader.Peek(), Is.EqualTo((int)'a'));
            Assert.That(reader.Read(), Is.EqualTo((int)'a'));
            Assert.That(reader.Peek(), Is.EqualTo((int)'b'));
            Assert.That(reader.Read(), Is.EqualTo((int)'b'));
            Assert.That(reader.Read(), Is.EqualTo((int)'c'));
            Assert.That(reader.Peek(), Is.EqualTo(-1));
            Assert.That(reader.Peek(), Is.EqualTo(-1));
        }

        [Test]
        public void TestRead()
        {
            using var reader = new BufferedTextReader(
                new StringReader("abc"), bufferSize: 2);

            Assert.That(reader.Read(), Is.EqualTo((int)'a'));
            Assert.That(reader.Read(), Is.EqualTo((int)'b'));
            Assert.That(reader.Read(), Is.EqualTo((int)'c'));
            Assert.That(reader.Read(), Is.EqualTo(-1));
        }

        [Test]
        public void TestReadCharArray()
        {
            using var reader = new BufferedTextReader(
                new StringReader("abcdefgh"), bufferSize: 3);

            char[] buffer = new char[10];

            int count = reader.Read(buffer, 2, 5);

            Assert.That(count, Is.EqualTo(5));
            Assert.That(new string(buffer, 2, 5), Is.EqualTo("abcde"));

            count = reader.Read(buffer, 0, buffer.Length);

            Assert.That(count, Is.EqualTo(3));
            Assert.That(new string(buffer, 0, count), Is.EqualTo("fgh"));

            Assert.That(reader.Read(buffer, 0, buffer.Length), Is.Zero);
        }

        [Test]
        public void TestReadCharArrayZeroCount()
        {
            using var reader = new BufferedTextReader(
                new StringReader("abc"));

            char[] buffer = new char[3];

            Assert.That(reader.Read(buffer, 0, 0), Is.Zero);
            Assert.That(reader.Read(), Is.EqualTo((int)'a'));
        }

        [Test]
        public void TestReadCharArrayInvalidArguments()
        {
            using var reader = new BufferedTextReader(new StringReader("abc"));

            Assert.Throws<ArgumentNullException>(
                () => reader.Read(null!, 0, 1));

            Assert.Throws<ArgumentOutOfRangeException>(
                () => reader.Read(new char[3], -1, 1));

            Assert.Throws<ArgumentOutOfRangeException>(
                () => reader.Read(new char[3], 0, -1));

            Assert.Throws<ArgumentException>(
                () => reader.Read(new char[3], 2, 2));
        }

#if FEATURE_TEXTREADER_SPAN
        [Test]
        public void TestReadSpan()
        {
            using var reader = new BufferedTextReader(
                new StringReader("abcdefgh"), bufferSize: 3);

            Span<char> buffer = stackalloc char[5];

            int count = reader.Read(buffer);

            Assert.That(count, Is.EqualTo(5));
            Assert.That(buffer.ToString(), Is.EqualTo("abcde"));

            count = reader.Read(buffer);

            Assert.That(count, Is.EqualTo(3));
            Assert.That(buffer.Slice(0, count).ToString(), Is.EqualTo("fgh"));

            Assert.That(reader.Read(buffer), Is.Zero);
        }
#endif

        [Test]
        public void TestReadLine()
        {
            using var reader = new BufferedTextReader(
                new StringReader("first\r\n\nthird\rfourth"),
                bufferSize: 2);

            Assert.That(reader.ReadLine(), Is.EqualTo("first"));
            Assert.That(reader.ReadLine(), Is.EqualTo(""));
            Assert.That(reader.ReadLine(), Is.EqualTo("third"));
            Assert.That(reader.ReadLine(), Is.EqualTo("fourth"));
            Assert.That(reader.ReadLine(), Is.Null);
            Assert.That(reader.ReadLine(), Is.Null);
        }

        [Test]
        public void TestReadBlockCharArray()
        {
            using var reader = new BufferedTextReader(
                new StringReader("abcdefgh"), bufferSize: 2);

            char[] buffer = new char[6];

            int count = reader.ReadBlock(buffer, 0, buffer.Length);

            Assert.That(count, Is.EqualTo(6));
            Assert.That(new string(buffer), Is.EqualTo("abcdef"));

            count = reader.ReadBlock(buffer, 0, buffer.Length);

            Assert.That(count, Is.EqualTo(2));
            Assert.That(new string(buffer, 0, count), Is.EqualTo("gh"));

            Assert.That(
                reader.ReadBlock(buffer, 0, buffer.Length),
                Is.Zero);
        }

#if FEATURE_TEXTREADER_SPAN
        [Test]
        public void TestReadBlockSpan()
        {
            using var reader = new BufferedTextReader(
                new StringReader("abcdefgh"), bufferSize: 2);

            Span<char> buffer = stackalloc char[6];

            int count = reader.ReadBlock(buffer);

            Assert.That(count, Is.EqualTo(6));
            Assert.That(buffer.ToString(), Is.EqualTo("abcdef"));

            count = reader.ReadBlock(buffer);

            Assert.That(count, Is.EqualTo(2));
            Assert.That(buffer.Slice(0, count).ToString(), Is.EqualTo("gh"));

            Assert.That(reader.ReadBlock(buffer), Is.Zero);
        }
#endif

        [Test]
        public void TestReadToEnd()
        {
            using var reader = new BufferedTextReader(
                new StringReader("abcdefgh"), bufferSize: 3);

            Assert.That(reader.Read(), Is.EqualTo((int)'a'));
            Assert.That(reader.ReadToEnd(), Is.EqualTo("bcdefgh"));
            Assert.That(reader.ReadToEnd(), Is.EqualTo(""));
        }

        [Test]
        public void TestMixedReadOperations()
        {
            using var reader = new BufferedTextReader(
                new StringReader("abcdef\r\nghij"), bufferSize: 3);

            ValueStringBuilder builder = new(stackalloc char[16]);

            try
            {
                Assert.That(reader.Read(), Is.EqualTo((int)'a'));
                Assert.That(reader.Peek(), Is.EqualTo((int)'b'));

                AssertNextLine(reader, ref builder, "bcdef");

                Assert.That(reader.Read(), Is.EqualTo((int)'g'));

                AssertNextLine(reader, ref builder, "hij");

                Assert.That(reader.Read(), Is.EqualTo(-1));
            }
            finally
            {
                builder.Dispose();
            }
        }

        [Test]
        public void TestLeaveOpen()
        {
            var underlyingReader = new StringReader("abc");

            using (var reader = new BufferedTextReader(
                underlyingReader, bufferSize: 1, leaveOpen: true))
            {
                Assert.That(reader.Read(), Is.EqualTo((int)'a'));
            }

            // The wrapper is disposed, but the underlying reader remains open.
            // Its position has advanced by the character already buffered.
            Assert.That(underlyingReader.Read(), Is.EqualTo((int)'b'));

            underlyingReader.Dispose();
        }

        [Test]
        public void TestDisposeUnderlyingReader()
        {
            var underlyingReader = new StringReader("abc");

            var reader = new BufferedTextReader(
                underlyingReader, leaveOpen: false);

            reader.Dispose();

            Assert.Throws<ObjectDisposedException>(
                () => underlyingReader.Read());

            Assert.Throws<ObjectDisposedException>(
                () => reader.Read());
        }

        [Test]
        public void TestDisposeIdempotent()
        {
            var underlyingReader = new StringReader("abc");

            var reader = new BufferedTextReader(
                underlyingReader, leaveOpen: true);

            Assert.DoesNotThrow(() => reader.Dispose());
            Assert.DoesNotThrow(() => reader.Dispose());

            Assert.Throws<ObjectDisposedException>(
                () => reader.Read());

            Assert.That(underlyingReader.Read(), Is.EqualTo((int)'a'));

            underlyingReader.Dispose();
        }

        [Test]
        public void TestAsyncMethodsNotSupported()
        {
            using var reader = new BufferedTextReader(
                new StringReader("abc"));

            char[] buffer = new char[3];

            Assert.Throws<NotSupportedException>(
                () => reader.ReadAsync(buffer, 0, buffer.Length));

            Assert.Throws<NotSupportedException>(
                () => reader.ReadBlockAsync(buffer, 0, buffer.Length));

            Assert.Throws<NotSupportedException>(
                () => reader.ReadLineAsync());

            Assert.Throws<NotSupportedException>(
                () => reader.ReadToEndAsync());
        }

#if FEATURE_TEXTREADER_MEMORY
        [Test]
        public void TestMemoryAsyncMethodsNotSupported()
        {
            using var reader = new BufferedTextReader(
                new StringReader("abc"));

            Memory<char> buffer = new char[3];

            Assert.Throws<NotSupportedException>(
                () => reader.ReadAsync(buffer));

            Assert.Throws<NotSupportedException>(
                () => reader.ReadBlockAsync(buffer));

            Assert.Throws<NotSupportedException>(
                () => reader.ReadLineAsync(CancellationToken.None));

            Assert.Throws<NotSupportedException>(
                () => reader.ReadToEndAsync(CancellationToken.None));
        }
#endif

        [Test]
        public void TestTryReadLineFinalCR()
        {
            using var reader = new BufferedTextReader(
                new StringReader("first\r"), bufferSize: 2);

            ValueStringBuilder builder = new(stackalloc char[16]);

            try
            {
                AssertNextLine(reader, ref builder, "first");
                Assert.That(reader.TryReadLine(ref builder), Is.False);
            }
            finally
            {
                builder.Dispose();
            }
        }

        private static void AssertNextLine(
            BufferedTextReader reader,
            ref ValueStringBuilder builder,
            string expected)
        {
            Assert.That(reader.TryReadLine(ref builder), Is.True);
            Assert.That(builder.ToString(), Is.EqualTo(expected));
        }
    }
}
