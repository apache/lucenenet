using J2N.Text;
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
    /// A <see cref="TextReader"/> that reads from a pooled character buffer.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This reader copies the supplied characters into an internally owned,
    /// pooled buffer. The buffer is returned to the pool when the reader is
    /// disposed. The reader cannot be reused after disposal.
    /// </para>
    /// <para>
    /// This type is particularly useful when reading from stack-allocated
    /// character data (for example, a <see langword="stackalloc"/>-allocated
    /// <see cref="Span{T}"/>). Since a <see cref="Span{T}"/> cannot be retained
    /// by a reader, its contents must be copied to heap memory before they can
    /// be consumed by a <see cref="TextReader"/> or a Lucene.NET
    /// <see cref="Lucene.Net.Analysis.TokenStream"/>.
    /// </para>
    /// <para>
    /// Because construction requires copying the input and renting a buffer,
    /// this reader is best suited to relatively short text. The maximum input
    /// length is constrained by the runtime's maximum supported array size,
    /// which is slightly less than 4 GB on current .NET implementations.
    /// Operations that produce a <see cref="string"/> are additionally
    /// constrained by the maximum supported string length, which is slightly
    /// less than 2 GB. In practice, available memory and hardware will usually
    /// impose much lower limits. For longer text, or text that is already
    /// backed by heap memory, prefer <see cref="MemoryStringReader"/>, which
    /// can read directly from the supplied memory without copying it.
    /// </para>
    /// <para>
    /// The input must not be modified while the constructor is executing.
    /// Once the constructor returns, the reader owns an independent copy, so
    /// the original input may be modified or its storage reused.
    /// </para>
    /// <para>
    /// This reader must be disposed to return its buffer to the pool. After
    /// disposal, all read operations throw <see cref="ObjectDisposedException"/>.
    /// </para>
    /// </remarks>
    /// <seealso cref="MemoryStringReader"/>
    /// <seealso cref="StringReader" />
    public sealed class PooledStringReader : TextReader, ISpanTextReadable
    {
        private readonly PooledTextBuilder s;
        private int pos;
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="PooledStringReader"/> class
        /// by copying the specified characters into an internally owned,
        /// pooled buffer.
        /// </summary>
        /// <param name="value">
        /// The characters to read. The contents are copied during construction.
        /// </param>
        /// <remarks>
        /// The supplied characters must not be modified while this constructor is
        /// executing. After the constructor returns, the original memory can be
        /// safely modified or reused because the reader has its own copy.
        /// </remarks>
        public PooledStringReader(ReadOnlySpan<char> value)
        {
            s = new PooledTextBuilder(value);
        }

        private void EnsureOpen()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(PooledStringReader));
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
                ? s[pos]
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
                ? s[pos++]
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
        /// The number of characters read, or <c>0</c> if the end of the input has
        /// been reached.
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
        /// The specified range extends beyond the bounds of <paramref name="buffer"/>.
        /// </exception>
        /// <remarks>
        /// Unlike some legacy Lucene.NET readers, this implementation follows the
        /// standard <see cref="TextReader"/> convention of returning <c>0</c> when
        /// the end of the input is reached.
        /// </remarks>
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
            s.CopyTo(pos, buffer, index, charsToCopy);

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
        /// The number of characters read, or <c>0</c> if the end of the input has
        /// been reached.
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
            s.AsSpan(pos, charsToCopy).CopyTo(buffer);

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
        /// read is sufficient to satisfy the ReadBlock contract.
        /// </remarks>
        public override int ReadBlock(Span<char> buffer)

        {
            EnsureOpen();

            // A single read satisfies the ReadBlock contract because all
            // available input is already in memory.
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
        /// The specified range extends beyond the bounds of <paramref name="buffer"/>.
        /// </exception>
        /// <remarks>
        /// Because the entire input is already available in memory, a single
        /// read is sufficient to satisfy the ReadBlock contract.
        /// </remarks>
        public override int ReadBlock(char[] buffer, int index, int count)
        {
            EnsureOpen();

            // A single read satisfies the ReadBlock contract because all
            // available input is already in memory.
            return Read(buffer, index, count);
        }

        /// <summary>
        /// Reads all remaining characters from the input and returns them as a string.
        /// </summary>
        /// <returns>
        /// A string containing all characters from the current position to the end
        /// of the input, or <see cref="string.Empty"/> if no characters remain.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        /// The reader has been disposed.
        /// </exception>
        /// <remarks>
        /// The returned string is a separate allocation; it does not expose the
        /// reader's internally pooled buffer.
        /// </remarks>
        public override string ReadToEnd()
        {
            EnsureOpen();

            if (pos >= s.Length)
                return string.Empty;

            string result = s.AsSpan(pos).ToString();
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
        /// Disposing the reader returns its internally owned character buffer
        /// to the pool. Disposal is idempotent. The reader cannot be used again
        /// after it has been disposed.
        /// </remarks>
        protected override void Dispose(bool disposing)
        {
            if (!disposed)
            {
                disposed = true;

                if (disposing)
                {
                    // Return the rented character buffer to the pool.
                    s.Dispose();
                }
            }

            base.Dispose(disposing);
        }
    }
}
