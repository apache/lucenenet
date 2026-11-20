using J2N.Text;
using Lucene.Net.IO;
using Lucene.Net.Support.Buffers;
using Lucene.Net.Text;
using System;
using System.Buffers;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
#nullable enable

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

    /// <summary>
    /// A buffered <see cref="TextReader"/> decorator that supports
    /// allocation-free line reading into a reusable
    /// <see cref="ValueStringBuilder"/>.
    /// </summary>
    internal sealed class BufferedTextReader : TextReader, ISpanTextReadable
    {
        public const int DEFAULT_BUFFER_SIZE = 1024;

        private readonly TextReader reader;
        private readonly int bufferSize;
        private char[]? buffer;
        private int position;
        private int length;
        private bool endOfStream;
        private readonly bool leaveOpen;
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of <see cref="BufferedTextReader"/>
        /// using the default buffer size.
        /// </summary>
        public BufferedTextReader(TextReader reader)
            : this(reader, DEFAULT_BUFFER_SIZE, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="BufferedTextReader"/>
        /// using the default buffer size.
        /// </summary>
        /// <param name="reader">The underlying reader.</param>
        /// <param name="leaveOpen">
        /// <c>true</c> to leave the underlying reader open when this reader
        /// is disposed; otherwise, <c>false</c>.
        /// </param>
        public BufferedTextReader(TextReader reader, bool leaveOpen)
            : this(reader, DEFAULT_BUFFER_SIZE, leaveOpen)
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="BufferedTextReader"/>
        /// using the specified buffer size.
        /// </summary>
        /// <param name="reader">The underlying reader.</param>
        /// <param name="bufferSize">The character buffer size.</param>
        /// <param name="leaveOpen">
        /// <c>true</c> to leave the underlying reader open when this reader
        /// is disposed; otherwise, <c>false</c>.
        /// </param>
        public BufferedTextReader(
            TextReader reader,
            int bufferSize,
            bool leaveOpen = false)
        {
            this.reader = reader ?? throw new ArgumentNullException(nameof(reader));

            if (bufferSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(bufferSize));

            this.bufferSize = bufferSize;
            buffer = ArrayPool<char>.Shared.Rent(bufferSize);
            this.leaveOpen = leaveOpen;
        }

        /// <summary>
        /// Reads the next line into the supplied builder.
        /// </summary>
        /// <param name="builder">
        /// The builder into which the line is appended. Its existing contents
        /// are cleared before reading.
        /// </param>
        /// <returns>
        /// <c>true</c> if a line was read, including an empty line;
        /// otherwise, <c>false</c> if the end of the reader was reached.
        /// </returns>
        public bool TryReadLine(ref ValueStringBuilder builder)
        {
            EnsureOpen();

            builder.Length = 0;

            while (true)
            {
                if (position == length && !FillBuffer())
                    return builder.Length != 0;

                ReadOnlySpan<char> remaining =
                    new ReadOnlySpan<char>(buffer, position, length - position);

                // Find either CR or LF using the optimized span search.
                int newlineIndex = remaining.IndexOfAny('\r', '\n');

                if (newlineIndex < 0)
                {
                    builder.Append(remaining);
                    position = length;
                    continue;
                }

                // Append everything before the line terminator.
                builder.Append(remaining.Slice(0, newlineIndex));

                char terminator = buffer![position + newlineIndex];

                // Consume the line terminator.
                position += newlineIndex + 1;

                if (terminator == '\r')
                {
                    // Consume an optional LF, even if it is in the next buffer.
                    if (position == length)
                        FillBuffer();

                    if (position < length && buffer[position] == '\n')
                        position++;
                }

                return true;
            }
        }

        /// <summary>
        /// Reads a single character without advancing the reader.
        /// </summary>
        public override int Peek()
        {
            EnsureOpen();

            if (position == length && !FillBuffer())
                return -1;

            return buffer![position];
        }

        /// <summary>
        /// Reads a single character and advances the reader.
        /// </summary>
        public override int Read()
        {
            EnsureOpen();

            if (position == length && !FillBuffer())
                return -1;

            return buffer![position++];
        }

        /// <summary>
        /// Reads characters into the specified array.
        /// </summary>
        public override int Read(char[] buffer, int index, int count)
        {
            EnsureOpen();

            if (buffer is null)
                throw new ArgumentNullException(nameof(buffer));

            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index));

            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            if (index > buffer.Length - count)
                throw new ArgumentException(
                    "Offset and count exceed the bounds of the array.");

            return ReadCore(new Span<char>(buffer, index, count));
        }

#if FEATURE_TEXTREADER_SPAN
        /// <summary>
        /// Reads characters into the specified span.
        /// </summary>
        public override int Read(Span<char> buffer)
#else
        int ISpanTextReadable.Read(Span<char> buffer)
#endif
        {
            EnsureOpen();
            return ReadCore(buffer);
        }


        /// <summary>
        /// Reads a line and returns it as a string.
        /// </summary>
        public override string? ReadLine()
        {
            ValueStringBuilder builder = new(stackalloc char[256]);
            try
            {
                return TryReadLine(ref builder)
                    ? builder.ToString()
                    : null;
            }
            finally
            {
                builder.Dispose();
            }
        }

        /// <summary>
        /// Reads characters into the specified span using the internal buffer.
        /// </summary>
        private int ReadCore(Span<char> destination)
        {
            int totalRead = 0;

            while (!destination.IsEmpty)
            {
                if (position == length && !FillBuffer())
                    break;

                int available = length - position;
                int count = Math.Min(available, destination.Length);

                new ReadOnlySpan<char>(buffer, position, count)
                    .CopyTo(destination);

                position += count;
                destination = destination.Slice(count);
                totalRead += count;
            }

            return totalRead;
        }

        /// <summary>
        /// Refills the internal buffer.
        /// </summary>
        /// <returns>
        /// <c>true</c> if characters were read; otherwise, <c>false</c>.
        /// </returns>
        private bool FillBuffer()
        {
            if (endOfStream)
                return false;

            position = 0;

#if FEATURE_TEXTREADER_SPAN
            length = reader.Read(new Span<char>(buffer, 0, bufferSize));
#else
            length = reader.Read(buffer, 0, bufferSize);
#endif

            // Accept -1 from legacy readers during the EOF migration.
            if (length <= 0)
            {
                length = 0;
                endOfStream = true;
                return false;
            }

            return true;
        }

        private void EnsureOpen()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(BufferedTextReader));
        }

        /// <summary>
        /// Disposes this reader and, unless <c>leaveOpen</c> was specified,
        /// its underlying reader.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposed)
                return;

            char[]? bufferToReturn = Interlocked.Exchange(ref buffer, null);

            // Only the thread that successfully retrieves the buffer owns disposal.
            if (bufferToReturn is null)
                return;

            disposed = true;

            try
            {
                if (disposing && !leaveOpen)
                    reader.Dispose();
            }
            finally
            {
                try
                {
                    ArrayPool<char>.Shared.ReturnIfNotNull(bufferToReturn);
                }
                finally
                {
                    position = 0;
                    length = 0;
                    base.Dispose(disposing);
                }
            }
        }

        public override int ReadBlock(char[] buffer, int index, int count)
        {
            return Read(buffer, index, count);
        }

#if FEATURE_TEXTREADER_SPAN
        public override int ReadBlock(Span<char> buffer)
            => Read(buffer);
#else
        int ISpanTextReadable.ReadBlock(Span<char> buffer)
        {
            EnsureOpen();
            return ReadCore(buffer);
        }
#endif

        // Asynchronous operations are deliberately unsupported.
        public override Task<int> ReadAsync(char[] buffer, int index, int count)
        {
            throw new NotSupportedException();
        }

        public override Task<int> ReadBlockAsync(char[] buffer, int index, int count)
        {
            throw new NotSupportedException();
        }

        public override Task<string?> ReadLineAsync()
        {
            throw new NotSupportedException();
        }

        public override Task<string> ReadToEndAsync()
        {
            throw new NotSupportedException();
        }

#if FEATURE_TEXTREADER_MEMORY
        public override ValueTask<int> ReadAsync(Memory<char> buffer, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public override ValueTask<int> ReadBlockAsync(Memory<char> buffer, CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
#endif

#if FEATURE_TEXTREADER_ASYNC_CANCELLATION
        public override ValueTask<string?> ReadLineAsync(CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public override Task<string> ReadToEndAsync(CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
#endif
    }
}
