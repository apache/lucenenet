using J2N.IO;
using Lucene.Net.Support.Threading;
using System;
using System.IO;
using System.Runtime.CompilerServices;

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
        private static readonly ConditionalWeakTable<Stream, object> lockCache = new ConditionalWeakTable<Stream, object>();

        /// <summary>
        /// Reads a sequence of bytes from a <see cref="Stream"/> to the given <see cref="ByteBuffer"/>, starting at the given position.
        /// The <paramref name="stream"/> must be both seekable and readable.
        /// </summary>
        /// <param name="stream">The stream to read.</param>
        /// <param name="destination">The <see cref="ByteBuffer"/> to write to.</param>
        /// <param name="position">The file position at which the transfer is to begin; must be non-negative.</param>
        /// <returns>The number of bytes read, possibly zero.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="stream"/> or <paramref name="destination"/> is <c>null</c></exception>
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
        /// This method is atomic when used by itself, but does not synchronize with the rest of the stream methods.
        /// </remarks>
        public static int Read(this Stream stream, ByteBuffer destination, long position)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));
            if (destination is null)
                throw new ArgumentNullException(nameof(destination));
            if (position < 0)
                throw new ArgumentOutOfRangeException(nameof(position));
            if (!stream.CanSeek)
                throw new NotSupportedException("Stream does not support seeking.");
            if (!stream.CanRead)
                throw new NotSupportedException("Stream does not support reading.");
            if (position > stream.Length)
                return 0;

            int read = 0;
            object readLock = lockCache.GetOrCreateValue(stream);
            UninterruptableMonitor.Enter(readLock);
            try
            {
                long originalPosition = stream.Position;
                stream.Seek(position, SeekOrigin.Begin);

                if (destination.HasArray)
                {
                    // If the buffer has an array, we can write to it directly and save
                    // an extra copy operation.

                    // Read from the stream
                    read = stream.Read(destination.Array, destination.Position, destination.Remaining);
                    destination.Position += read;
                }
                else
                {
                    // If the buffer has no array, we must use a local buffer
                    byte[] buffer = new byte[destination.Remaining];

                    // Read from the stream
                    read = stream.Read(buffer, 0, buffer.Length);

                    // Write to the byte buffer
                    destination.Put(buffer, 0, read);
                }

                // Per Java's FileChannel.Read(), we don't want to alter the position
                // of the stream, so we return it as it was originally.
                stream.Seek(originalPosition, SeekOrigin.Begin);
            }
            finally
            {
                UninterruptableMonitor.Exit(readLock);
            }

            return read;
        }

        public static void Write(this Stream stream, char[] chars)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));
            if (chars is null)
                throw new ArgumentNullException(nameof(chars));

            byte[] newBytes = new byte[chars.Length * 2];
            for (int index = 0; index < chars.Length; index++)
            {
                int newIndex = index == 0 ? index : index * 2;
                newBytes[newIndex] = (byte)chars[index];
                newBytes[newIndex + 1] = (byte)(chars[index] >> 8);
            }
            stream.Write(newBytes, 0, newBytes.Length);
        }

        public static char[] ReadChars(this Stream stream, int count)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));
            if (count < 0)
                throw new ArgumentOutOfRangeException(nameof(count));

            byte[] buff = new byte[2];
            char[] newChars = new char[count];
            for (int i = 0; i < count; i++)
            {
                stream.Read(buff, 0, 2);
                newChars[i] = (char)((buff[0] & 0xff) | ((buff[1] & 0xff) << 8));
            }
            return newChars;
        }

        public static void Write(this Stream stream, int value)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));

            byte[] buff = new byte[4];
            buff[0] = (byte)(value);
            buff[1] = (byte)(value >> 8);
            buff[2] = (byte)(value >> 16);
            buff[3] = (byte)(value >> 24);
            stream.Write(buff, 0, buff.Length);
        }

        public static int ReadInt32(this Stream stream)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));

            byte[] buff = new byte[4];
            stream.Read(buff, 0, buff.Length);
            return (buff[0] & 0xff) | ((buff[1] & 0xff) << 8) |
                ((buff[2] & 0xff) << 16) | ((buff[3] & 0xff) << 24);
        }

        public static void Write(this Stream stream, long value)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));

            byte[] buff = new byte[8];
            buff[0] = (byte)value;
            buff[1] = (byte)(value >> 8);
            buff[2] = (byte)(value >> 16);
            buff[3] = (byte)(value >> 24);
            buff[4] = (byte)(value >> 32);
            buff[5] = (byte)(value >> 40);
            buff[6] = (byte)(value >> 48);
            buff[7] = (byte)(value >> 56);
            stream.Write(buff, 0, buff.Length);
        }

        public static long ReadInt64(this Stream stream)
        {
            if (stream is null)
                throw new ArgumentNullException(nameof(stream));

            byte[] buff = new byte[8];
            stream.Read(buff, 0, buff.Length);
            uint lo = (uint)(buff[0] | buff[1] << 8 |
                             buff[2] << 16 | buff[3] << 24);
            uint hi = (uint)(buff[4] | buff[5] << 8 |
                             buff[6] << 16 | buff[7] << 24);
            return (long)((ulong)hi) << 32 | lo;
        }
    }
}
