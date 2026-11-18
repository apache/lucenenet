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

    /// <summary>
    /// A <see cref="TextReader"/> that reads directly from a
    /// <see cref="ReadOnlyMemory{T}"/> of characters without copying its contents.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This reader retains the supplied memory and reads directly from its
    /// underlying storage. It does not allocate a character buffer or take
    /// ownership of the memory.
    /// </para>
    /// <para>
    /// The underlying memory must remain valid and its contents must not be
    /// modified until the reader has finished reading from it. In particular,
    /// if the memory refers to an array rented from an array pool, that array
    /// must not be returned to the pool while the reader is in use. Likewise,
    /// if the memory is backed by a disposable memory owner, that owner must
    /// not be disposed while the reader is in use.
    /// </para>
    /// <para>
    /// Unlike <see cref="PooledStringReader"/>, this reader does not copy its
    /// input. It is therefore preferable for longer text or text that is
    /// already backed by heap memory. For short-lived data such as a
    /// stack-allocated span, use <see cref="PooledStringReader"/> instead,
    /// which copies the input into an internally owned buffer.
    /// </para>
    /// <para>
    /// Disposing this reader releases its reference to the supplied memory,
    /// but does not modify, dispose, or otherwise release the underlying
    /// storage. The reader cannot be reused after disposal.
    /// </para>
    /// </remarks>
    /// <seealso cref="PooledStringReader"/>
    /// <seealso cref="StringReader" />
    public sealed class MemoryStringReader : TextReader, ISpanTextReadable
    {
        private ReadOnlyMemory<char> s;
        private int pos;
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="MemoryStringReader"/>
        /// class that reads directly from the specified memory.
        /// </summary>
        /// <param name="value">
        /// The memory containing the characters to read. The memory is retained
        /// without copying its contents.
        /// </param>
        /// <remarks>
        /// The underlying memory must remain valid and unchanged until the
        /// reader has finished reading from it or has been disposed.
        /// </remarks>
        public MemoryStringReader(ReadOnlyMemory<char> value)
        {
            s = value;
        }

        private void EnsureOpen()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(MemoryStringReader));
        }

        /// <summary>
        /// Returns the next available character without advancing the reader.
        /// </summary>
        /// <returns>
        /// The next character, or <c>-1</c> if the end of the input has been reached.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        /// The reader has been disposed.
        /// </exception>
        public override int Peek()
        {
            EnsureOpen();

            return pos < s.Length
                ? s.Span[pos]
                : -1;
        }

        /// <summary>
        /// Reads the next character from the input.
        /// </summary>
        /// <returns>
        /// The next character, or <c>-1</c> if the end of the input has been reached.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        /// The reader has been disposed.
        /// </exception>
        public override int Read()
        {
            EnsureOpen();

            return pos < s.Length
                ? s.Span[pos++]
                : -1;
        }

        /// <summary>
        /// Reads a specified number of characters from the input and stores them
        /// beginning at the specified index in the buffer.
        /// </summary>
        /// <param name="buffer">
        /// The buffer into which the characters are written.
        /// </param>
        /// <param name="index">
        /// The index in <paramref name="buffer"/> at which to begin writing.
        /// </param>
        /// <param name="count">
        /// The maximum number of characters to read.
        /// </param>
        /// <returns>
        /// The number of characters read, or <c>0</c> if the end of the input
        /// has been reached.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        /// The reader has been disposed.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="buffer"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="index"/> or <paramref name="count"/> is negative.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The specified range extends beyond the bounds of
        /// <paramref name="buffer"/>.
        /// </exception>
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
                throw new ArgumentException("Offset and count exceed the bounds of the array.");

            if (pos >= s.Length)
                return 0;

            int charsToCopy = Math.Min(count, s.Length - pos);
            s.Span.Slice(pos, charsToCopy).CopyTo(buffer.AsSpan(index, charsToCopy));

            pos += charsToCopy;
            return charsToCopy;
        }

#if FEATURE_TEXTREADER_SPAN
        /// <summary>
        /// Reads a sequence of characters from the input and writes them into
        /// the specified character span.
        /// </summary>
        /// <param name="buffer">
        /// The span into which the characters are written.
        /// </param>
        /// <returns>
        /// The number of characters read, or <c>0</c> if the end of the input
        /// has been reached.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        /// The reader has been disposed.
        /// </exception>
        public override int Read(Span<char> buffer)
#else
        int ISpanTextReadable.Read(Span<char> buffer)
            => ReadCore(buffer);

        private int ReadCore(Span<char> buffer)
#endif
        {
            EnsureOpen();

            if (buffer.IsEmpty || pos >= s.Length)
                return 0;

            int charsToCopy = Math.Min(buffer.Length, s.Length - pos);
            s.Span.Slice(pos, charsToCopy).CopyTo(buffer);

            pos += charsToCopy;
            return charsToCopy;
        }

#if FEATURE_TEXTREADER_SPAN
        /// <summary>
        /// Reads characters from the input until the specified span is filled
        /// or the end of the input is reached.
        /// </summary>
        /// <param name="buffer">
        /// The span into which the characters are written.
        /// </param>
        /// <returns>
        /// The number of characters read. This may be less than the length of
        /// <paramref name="buffer"/> if the end of the input is reached.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        /// The reader has been disposed.
        /// </exception>
        /// <remarks>
        /// Because the entire input is already available in memory, a single
        /// read is sufficient to satisfy the <see cref="TextReader.ReadBlock(Span{char})"/>
        /// contract.
        /// </remarks>
        public override int ReadBlock(Span<char> buffer)
        {
            EnsureOpen();
            return Read(buffer);
        }
#else
        int ISpanTextReadable.ReadBlock(Span<char> buffer)
            => ReadCore(buffer);
#endif

        /// <summary>
        /// Reads characters from the input until the specified buffer range is
        /// filled or the end of the input is reached.
        /// </summary>
        /// <param name="buffer">
        /// The buffer into which the characters are written.
        /// </param>
        /// <param name="index">
        /// The index in <paramref name="buffer"/> at which to begin writing.
        /// </param>
        /// <param name="count">
        /// The maximum number of characters to read.
        /// </param>
        /// <returns>
        /// The number of characters read. This may be less than
        /// <paramref name="count"/> if the end of the input is reached.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        /// The reader has been disposed.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="buffer"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="index"/> or <paramref name="count"/> is negative.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// The specified range extends beyond the bounds of
        /// <paramref name="buffer"/>.
        /// </exception>
        /// <remarks>
        /// Because the entire input is already available in memory, a single
        /// read is sufficient to satisfy the
        /// <see cref="TextReader.ReadBlock(char[], int, int)"/> contract.
        /// </remarks>
        public override int ReadBlock(char[] buffer, int index, int count)
        {
            EnsureOpen();
            return Read(buffer, index, count);
        }

        /// <summary>
        /// Reads all remaining characters from the input and returns them as a string.
        /// </summary>
        /// <returns>
        /// A string containing all characters from the current position to the
        /// end of the input, or <see cref="string.Empty"/> if no characters remain.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        /// The reader has been disposed.
        /// </exception>
        /// <remarks>
        /// The returned string is a separate allocation. It does not expose
        /// the underlying memory retained by this reader.
        /// </remarks>
        public override string ReadToEnd()
        {
            EnsureOpen();

            if (pos >= s.Length)
                return string.Empty;

            string result = s.Span.Slice(pos).ToString();
            pos = s.Length;
            return result;
        }

        /// <summary>
        /// Releases the resources used by this reader.
        /// </summary>
        /// <param name="disposing">
        /// <see langword="true"/> to release managed resources; otherwise,
        /// <see langword="false"/>.
        /// </param>
        /// <remarks>
        /// Disposing the reader releases its reference to the supplied memory.
        /// It does not modify, dispose, or otherwise release the underlying
        /// storage. Disposal is idempotent. The reader cannot be used again
        /// after it has been disposed.
        /// </remarks>
        protected override void Dispose(bool disposing)
        {
            if (!disposed)
            {
                disposed = true;
                s = default;
                pos = 0;
            }

            base.Dispose(disposing);
        }
    }
}
