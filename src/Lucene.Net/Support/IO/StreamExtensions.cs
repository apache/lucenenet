using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

#if !FEATURE_RANDOMACCESS_READ
using Lucene.Net.Support.Threading;
using System.Runtime.CompilerServices;
#endif

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
    /// Extension methods that make a <see cref="Stream"/> effectively into a
    /// binary serializer with no encoding. We simply convert types into bytes
    /// and write them without any concern whether surrogate pairs are respected,
    /// similar to what BinaryFormatter does.
    /// This makes it possible to serialize/deserialize raw character arrays
    /// and get the data back in the same order without any exceptions warning
    /// that the order is not valid and without the need for BinaryFormatter.
    /// <para/>
    /// Byte order is little-endian (same as <see cref="BinaryReader"/> and <see cref="BinaryWriter"/>).
    /// </summary>
    internal static class StreamExtensions
    {
#if !FEATURE_RANDOMACCESS_READ
        private static readonly ConditionalWeakTable<Stream, object> lockCache = new ConditionalWeakTable<Stream, object>();
#endif

        /// <summary>
        /// Reads a sequence of bytes from a <see cref="Stream"/> to the given <see cref="Span{Byte}"/>,
        /// starting at the given <paramref name="position"/>. Prior to .NET Core, the <paramref name="stream"/>
        /// must be both seekable and readable.
        /// </summary>
        /// <param name="stream">The stream to read.</param>
        /// <param name="destination">The span to write to.</param>
        /// <param name="position">The file position at which the transfer is to begin; must be non-negative.</param>
        /// <returns>The number of bytes read, possibly zero.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <c>null</c></exception>
        /// <exception cref="NotSupportedException">
        /// <paramref name="stream"/> is not readable.
        /// <para/>
        /// -or-
        /// <para/>
        /// <paramref name="stream"/> is not seekable.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="position"/> is less than 0.
        /// <para/>
        /// -or-
        /// <para/>
        /// <paramref name="position"/> is greater than the <see cref="Stream.Length"/> of the stream.
        /// </exception>
        /// <exception cref="IOException">An I/O error occurs.</exception>
        /// <exception cref="ObjectDisposedException"><paramref name="stream"/> has already been disposed.</exception>
        /// <remarks>
        /// On .NET 6+, this method uses the RandomAccess class to synchronize, so it is completely threadsafe
        /// and does not require the stream to be seekable or readable.
        /// <para/>
        /// On older target frameworks, this method is atomic when used by itself, but does not synchronize with
        /// the rest of the stream methods.
        /// </remarks>
        public static int Read(this FileStream stream, Span<byte> destination, long position)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));

#if FEATURE_RANDOMACCESS_READ
            return RandomAccess.Read(stream.SafeFileHandle, destination, position);
#else
            if (position < 0)
                throw new ArgumentOutOfRangeException(nameof(position));
            if (!stream.CanSeek)
                throw new NotSupportedException("Stream does not support seeking.");
            if (!stream.CanRead)
                throw new NotSupportedException("Stream does not support reading.");
            if (position > stream.Length)
                return 0;

            int read;
            object readLock = lockCache.GetOrCreateValue(stream);
            UninterruptableMonitor.Enter(readLock);
            try
            {
                long originalPosition = stream.Position;
                stream.Seek(position, SeekOrigin.Begin);

                read = stream.Read(destination);

                // Per Java's FileChannel.Read(), we don't want to alter the position
                // of the stream, so we return it as it was originally.
                stream.Seek(originalPosition, SeekOrigin.Begin);
            }
            finally
            {
                UninterruptableMonitor.Exit(readLock);
            }

            return read;
#endif
        }

        /// <summary>
        /// Reads a sequence of bytes from the current stream and advances
        /// the position within the stream by the number of bytes read.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="buffer">A region of memory. When this method returns,
        /// the contents of this region are replaced by the bytes read from
        /// the current source.</param>
        /// <returns>The total number of bytes read into the buffer. This can be
        /// less than the size of the buffer if that many bytes are not currently
        /// available, or zero (0) if the buffer's length is zero or the end of
        /// the stream has been reached.</returns>
        /// <exception cref="IOException">An I/O error occurs.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <c>null</c>.</exception>
        /// <remarks>This is to patch .NET Standard and .NET Framework.</remarks>
        public static int Read(this Stream stream, Span<byte> buffer)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));

            byte[] sharedBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length);
            try
            {
                int numRead = stream.Read(sharedBuffer, 0, buffer.Length);
                if ((uint)numRead > (uint)buffer.Length)
                {
                    throw new IOException(SR.IO_StreamTooLong);
                }

                new ReadOnlySpan<byte>(sharedBuffer, 0, numRead).CopyTo(buffer);
                return numRead;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(sharedBuffer);
            }
        }

#if !FEATURE_STREAM_READEXACTLY
        /// <summary>
        /// Reads bytes from the stream into <paramref name="buffer"/> until it is completely filled.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="buffer">The buffer to read into.</param>
        /// <exception cref="ArgumentNullException"><paramref name="stream"/> is null.</exception>
        /// <exception cref="EndOfStreamException">The end of the stream is reached before filling the buffer.</exception>
        /// <remarks>
        /// This method is a polyfill for platforms (prior to .NET 7) that do not have the ReadExactly method.
        /// </remarks>
        public static void ReadExactly(this Stream stream, Span<byte> buffer)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));

            byte[] sharedBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length);
            try
            {
                while (buffer.Length > 0)
                {
                    int numRead = stream.Read(sharedBuffer, 0, buffer.Length);

                    if (numRead == 0)
                    {
                        throw new EndOfStreamException(SR.IO_EOF_ReadBeyondEOF);
                    }

                    new ReadOnlySpan<byte>(sharedBuffer, 0, numRead).CopyTo(buffer);
                    buffer = buffer.Slice(numRead);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(sharedBuffer);
            }
        }

        /// <summary>
        /// Reads bytes asynchronously from the stream into <paramref name="buffer"/> until it is completely filled.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="buffer">The buffer to read into.</param>
        /// <param name="offset">The byte offset in <paramref name="buffer"/> at which to begin writing data read from the stream.</param>
        /// <param name="count">The count of bytes to read.</param>
        /// <exception cref="ArgumentNullException"><paramref name="stream"/> is null.</exception>
        /// <exception cref="EndOfStreamException">The end of the stream is reached before filling the buffer.</exception>
        /// <remarks>
        /// This method is a polyfill for platforms (prior to .NET 7) that do not have the ReadExactlyAsync method.
        /// </remarks>
        public static async Task ReadExactlyAsync(this Stream stream, byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));

            while (count > 0)
            {
                int numRead = await stream.ReadAsync(buffer, offset, count, cancellationToken);

                if (numRead == 0)
                {
                    throw new EndOfStreamException(SR.IO_EOF_ReadBeyondEOF);
                }

                count -= numRead;
                offset += numRead;
            }
        }
#endif

        /// <summary>
        /// Writes a sequence of bytes to the current stream and advances the current
        /// position within this stream by the number of bytes written.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        /// <param name="buffer">A region of memory. This method copies the contents of this region to the current stream.</param>
        /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <c>null</c>.</exception>
        /// <remarks>This is to patch .NET Standard and .NET Framework.</remarks>
        public static void Write(this Stream stream, ReadOnlySpan<byte> buffer)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));

            byte[] sharedBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length);
            try
            {
                buffer.CopyTo(sharedBuffer);
                stream.Write(sharedBuffer, 0, buffer.Length);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(sharedBuffer);
            }
        }

        public static void Write(this Stream stream, char[] chars)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));
            if (chars is null)
                throw new ArgumentNullException(nameof(chars));

            int byteCount = chars.Length * 2;
            byte[] newBytes = ArrayPool<byte>.Shared.Rent(byteCount);
            try
            {
                for (int index = 0; index < chars.Length; index++)
                {
                    int newIndex = index == 0 ? index : index * 2;
                    newBytes[newIndex] = (byte)chars[index];
                    newBytes[newIndex + 1] = (byte)(chars[index] >> 8);
                }

                stream.Write(newBytes, 0, byteCount);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(newBytes);
            }
        }

        public static char[] ReadChars(this Stream stream, int count)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            Span<byte> buff = stackalloc byte[2];
            char[] newChars = new char[count];
            for (int i = 0; i < count; i++)
            {
                stream.ReadExactly(buff);
                newChars[i] = (char)((buff[0] & 0xff) | ((buff[1] & 0xff) << 8));
            }
            return newChars;
        }

        public static void Write(this Stream stream, int value)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));

            Span<byte> buff = stackalloc byte[4];
            buff[0] = (byte)(value);
            buff[1] = (byte)(value >> 8);
            buff[2] = (byte)(value >> 16);
            buff[3] = (byte)(value >> 24);
            stream.Write(buff);
        }

        public static int ReadInt32(this Stream stream)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));

            Span<byte> buff = stackalloc byte[4];
            stream.ReadExactly(buff);
            return (buff[0] & 0xff) | ((buff[1] & 0xff) << 8) |
                ((buff[2] & 0xff) << 16) | ((buff[3] & 0xff) << 24);
        }

        public static void Write(this Stream stream, long value)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));

            Span<byte> buff = stackalloc byte[8];
            buff[0] = (byte)value;
            buff[1] = (byte)(value >> 8);
            buff[2] = (byte)(value >> 16);
            buff[3] = (byte)(value >> 24);
            buff[4] = (byte)(value >> 32);
            buff[5] = (byte)(value >> 40);
            buff[6] = (byte)(value >> 48);
            buff[7] = (byte)(value >> 56);
            stream.Write(buff);
        }

        public static long ReadInt64(this Stream stream)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));

            Span<byte> buff = stackalloc byte[8];
            stream.ReadExactly(buff);
            uint lo = (uint)(buff[0] | buff[1] << 8 |
                             buff[2] << 16 | buff[3] << 24);
            uint hi = (uint)(buff[4] | buff[5] << 8 |
                             buff[6] << 16 | buff[7] << 24);
            return (long)((ulong)hi) << 32 | lo;
        }

        // async versions of the above methods
        public static async Task WriteInt32BigEndianAsync(this Stream output, int value, CancellationToken cancellationToken = default)
        {
            if (output is null)
                throw new ArgumentNullException(nameof(output));

            byte[] buff = ArrayPool<byte>.Shared.Rent(4);
            try
            {
                buff[0] = (byte)(value >> 24);
                buff[1] = (byte)(value >> 16);
                buff[2] = (byte)(value >> 8);
                buff[3] = (byte)value;

                await output.WriteAsync(buff, 0, 4, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buff);
            }
        }

        public static async Task WriteInt64BigEndianAsync(this Stream output, long value, CancellationToken cancellationToken = default)
        {
            if (output is null)
                throw new ArgumentNullException(nameof(output));

            byte[] buff = ArrayPool<byte>.Shared.Rent(8);
            try
            {
                buff[0] = (byte)(value >> 56);
                buff[1] = (byte)(value >> 48);
                buff[2] = (byte)(value >> 40);
                buff[3] = (byte)(value >> 32);
                buff[4] = (byte)(value >> 24);
                buff[5] = (byte)(value >> 16);
                buff[6] = (byte)(value >> 8);
                buff[7] = (byte)value;

                await output.WriteAsync(buff, 0, 8, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buff);
            }
        }

        public static async Task WriteUTFAsync(this Stream output, string value, CancellationToken cancellationToken = default)
        {
            if (output is null)
                throw new ArgumentNullException(nameof(output));
            if (value is null)
                throw new ArgumentNullException(nameof(value));

            long utfCount = CountUTFBytes(value);
            if (utfCount > ushort.MaxValue)
                throw new EncoderFallbackException("Encoded string too long.");

            byte[] buffer = ArrayPool<byte>.Shared.Rent((int)utfCount + 2);
            try
            {
                int offset = 0;
                offset = WriteInt16BigEndianToBuffer((int)utfCount, buffer, offset);
                offset = WriteUTFBytesToBuffer(value, buffer, offset);

                await output.WriteAsync(buffer, 0, offset, cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
        public static async Task<int> ReadInt32BigEndianAsync(this Stream input, CancellationToken cancellationToken = default)
        {
            if (input is null)
                throw new ArgumentNullException(nameof(input));

            byte[] buff = ArrayPool<byte>.Shared.Rent(4);
            try
            {
                await input.ReadExactlyAsync(buff, 0, 4, cancellationToken).ConfigureAwait(false);

                return (buff[0] << 24) | (buff[1] << 16) | (buff[2] << 8) | buff[3];
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buff);
            }
        }

        public static async Task<long> ReadInt64BigEndianAsync(this Stream input, CancellationToken cancellationToken = default)
        {
            if (input is null)
                throw new ArgumentNullException(nameof(input));

            byte[] buff = ArrayPool<byte>.Shared.Rent(8);
            try
            {
                await input.ReadExactlyAsync(buff, 0, 8, cancellationToken).ConfigureAwait(false);

                return ((long)buff[0] << 56) |
                       ((long)buff[1] << 48) |
                       ((long)buff[2] << 40) |
                       ((long)buff[3] << 32) |
                       ((long)buff[4] << 24) |
                       ((long)buff[5] << 16) |
                       ((long)buff[6] << 8) |
                       buff[7];
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buff);
            }
        }

        public static async Task<string> ReadUTFAsync(this Stream input, CancellationToken cancellationToken = default)
        {
            if (input is null)
                throw new ArgumentNullException(nameof(input));

            byte[] lenBuff = ArrayPool<byte>.Shared.Rent(2);
            try
            {
                await input.ReadExactlyAsync(lenBuff, 0, 2, cancellationToken).ConfigureAwait(false);

                int length = (lenBuff[0] << 8) | lenBuff[1];

                byte[] buffer = ArrayPool<byte>.Shared.Rent(length);
                try
                {
                    await input.ReadExactlyAsync(buffer, 0, length, cancellationToken).ConfigureAwait(false);

                    return Encoding.UTF8.GetString(buffer, 0, length);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(lenBuff);
            }
        }


        // ========================
        // Helper methods for UTF
        // ========================
        private static long CountUTFBytes(string value)
        {
            long utfCount = 0;
            foreach (char ch in value)
            {
                if (ch > 0 && ch <= 127)
                    utfCount++;
                else if (ch <= 2047)
                    utfCount += 2;
                else
                    utfCount += 3;
            }
            return utfCount;
        }

        private static int WriteInt16BigEndianToBuffer(int value, Span<byte> buffer, int offset)
        {
            buffer[offset++] = (byte)(value >> 8);
            buffer[offset++] = (byte)value;
            return offset;
        }

        private static int WriteUTFBytesToBuffer(string value, Span<byte> buffer, int offset)
        {
            foreach (char ch in value)
            {
                if (ch > 0 && ch <= 127)
                {
                    buffer[offset++] = (byte)ch;
                }
                else if (ch <= 2047)
                {
                    buffer[offset++] = (byte)(0xc0 | (0x1f & (ch >> 6)));
                    buffer[offset++] = (byte)(0x80 | (0x3f & ch));
                }
                else
                {
                    buffer[offset++] = (byte)(0xe0 | (0x0f & (ch >> 12)));
                    buffer[offset++] = (byte)(0x80 | (0x3f & (ch >> 6)));
                    buffer[offset++] = (byte)(0x80 | (0x3f & ch));
                }
            }
            return offset;
        }

        private static class SR
        {
            public const string IO_StreamTooLong = "Stream was too long.";
            public const string IO_EOF_ReadBeyondEOF = "Unable to read beyond the end of the stream.";
        }
    }
}
