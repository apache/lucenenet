using System;
using System.Buffers;
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
    /// Provides extension methods for reading characters from a
    /// <see cref="TextReader"/> into spans.
    /// </summary>
    /// <remarks>
    /// These methods provide span-based reading on target frameworks where
    /// <see cref="TextReader"/> does not expose the corresponding span overloads.
    /// Readers with an optimized span-reading implementation are dispatched
    /// directly to that implementation. Other readers are supported through
    /// a temporary pooled character buffer.
    /// </remarks>
    public static class TextReaderExtensions
    {
        private const int BufferSize = 1024;

        /// <summary>
        /// Reads a sequence of characters from the input and writes them into
        /// the specified character span.
        /// </summary>
        /// <param name="reader">
        /// The reader from which to read.
        /// </param>
        /// <param name="buffer">
        /// The span into which the characters are written.
        /// </param>
        /// <returns>
        /// The number of characters read, or <c>0</c> if the end of the input
        /// has been reached.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="reader"/> is <see langword="null"/>.
        /// </exception>
        /// <remarks>
        /// If the reader does not provide an optimized span-reading
        /// implementation, this method reads through a temporary pooled
        /// character buffer. In that case, the returned count may be less
        /// than the length of <paramref name="buffer"/>, even when more
        /// characters remain in the input.
        /// </remarks>
        public static int Read(this TextReader reader, Span<char> buffer)
        {
            if (reader is null)
                throw new ArgumentNullException(nameof(reader));

            if (buffer.IsEmpty)
                return 0;

            if (reader is ISpanTextReadable spanReadable)
                return spanReadable.Read(buffer);

            char[] rented = ArrayPool<char>.Shared.Rent(
                Math.Min(buffer.Length, BufferSize));

            try
            {
                int count = Math.Min(buffer.Length, rented.Length);
                int charsRead = reader.Read(rented, 0, count);

                // Some legacy Lucene.NET readers return -1 at EOF.
                if (charsRead <= 0)
                    return 0;

                new ReadOnlySpan<char>(rented, 0, charsRead).CopyTo(buffer);
                return charsRead;
            }
            finally
            {
                ArrayPool<char>.Shared.Return(rented);
            }
        }

        /// <summary>
        /// Reads characters from the input until the specified span is filled
        /// or the end of the input is reached.
        /// </summary>
        /// <param name="reader">
        /// The reader from which to read.
        /// </param>
        /// <param name="buffer">
        /// The span into which the characters are written.
        /// </param>
        /// <returns>
        /// The number of characters read. This may be less than the length of
        /// <paramref name="buffer"/> if the end of the input is reached.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="reader"/> is <see langword="null"/>.
        /// </exception>
        public static int ReadBlock(this TextReader reader, Span<char> buffer)
        {
            if (reader is null)
                throw new ArgumentNullException(nameof(reader));

            if (buffer.IsEmpty)
                return 0;

            if (reader is ISpanTextReadable spanReadable)
                return spanReadable.ReadBlock(buffer);

            char[] rented = ArrayPool<char>.Shared.Rent(
                Math.Min(buffer.Length, BufferSize));

            try
            {
                int totalCharsRead = 0;

                while (totalCharsRead < buffer.Length)
                {
                    int count = Math.Min(
                        buffer.Length - totalCharsRead,
                        rented.Length);

                    int charsRead = reader.Read(rented, 0, count);

                    // Some legacy Lucene.NET readers return -1 at EOF.
                    if (charsRead <= 0)
                        break;

                    new ReadOnlySpan<char>(rented, 0, charsRead).CopyTo(
                        buffer.Slice(totalCharsRead, charsRead));

                    totalCharsRead += charsRead;
                }

                return totalCharsRead;
            }
            finally
            {
                ArrayPool<char>.Shared.Return(rented);
            }
        }
    }
}

