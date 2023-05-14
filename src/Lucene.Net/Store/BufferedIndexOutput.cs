using Lucene.Net.Support;
using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Lucene.Net.Store
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
    /// Base implementation class for buffered <see cref="IndexOutput"/>. </summary>
    public abstract class BufferedIndexOutput : IndexOutput
    {
        /// <summary>
        /// The default buffer size in bytes (<see cref="DEFAULT_BUFFER_SIZE"/>). </summary>
        public const int DEFAULT_BUFFER_SIZE = 16384;

        private readonly int bufferSize;
        private byte[] buffer;
        private long bufferStart = 0; // position in file of buffer
        private int bufferPosition = 0; // position in buffer
        private readonly CRC32 crc;
        private int disposed = 0; // LUCENENET specific - allow double-dispose

        /// <summary>
        /// Creates a new <see cref="BufferedIndexOutput"/> with the default buffer size
        /// (<see cref="DEFAULT_BUFFER_SIZE"/> bytes see <see cref="DEFAULT_BUFFER_SIZE"/>)
        /// </summary>
        protected BufferedIndexOutput() // LUCENENET: CA1012: Abstract types should not have constructors (marked protected)
            : this(DEFAULT_BUFFER_SIZE)
        {
        }

        /// <summary>
        /// Creates a new <see cref="BufferedIndexOutput"/> with the given buffer size. </summary>
        /// <param name="bufferSize"> the buffer size in bytes used to buffer writes internally. </param>
        /// <exception cref="ArgumentException"> if the given buffer size is less or equal to <c>0</c> </exception>
        protected BufferedIndexOutput(int bufferSize) // LUCENENET: CA1012: Abstract types should not have constructors (marked protected)
            : this(bufferSize, new CRC32())
        { }

        // LUCENENET specific - added constructor overload so FSDirectory can still subclass BufferedIndexOutput, but
        // utilize its own buffer, since FileStream is already buffered in .NET.
        private protected BufferedIndexOutput(int bufferSize, CRC32 crc)
        {
            if (bufferSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferSize), "bufferSize must be greater than 0 (got " + bufferSize + ")"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }
            this.bufferSize = bufferSize;
            // LUCENENET: We lazy-load the buffer, so we don't force all subclasses to allocate it
            this.crc = crc;
        }

        public override void WriteByte(byte b)
        {
            if (buffer is null) buffer = new byte[bufferSize]; // LUCENENET: Lazy-load the buffer, so we don't force all subclasses to allocate it
            if (bufferPosition >= bufferSize)
            {
                Flush();
            }
            buffer[bufferPosition++] = b;
        }

        public override void WriteBytes(byte[] b, int offset, int length)
        {
            if (buffer is null) buffer = new byte[bufferSize]; // LUCENENET: Lazy-load the buffer, so we don't force all subclasses to allocate it
            int bytesLeft = bufferSize - bufferPosition;
            // is there enough space in the buffer?
            if (bytesLeft >= length)
            {
                // we add the data to the end of the buffer
                Arrays.Copy(b, offset, buffer, bufferPosition, length);
                bufferPosition += length;
                // if the buffer is full, flush it
                if (bufferSize - bufferPosition == 0)
                {
                    Flush();
                }
            }
            else
            {
                // is data larger then buffer?
                if (length > bufferSize)
                {
                    // we flush the buffer
                    if (bufferPosition > 0)
                    {
                        Flush();
                    }
                    // and write data at once
                    crc.Update(b, offset, length);
                    FlushBuffer(b, offset, length);
                    bufferStart += length;
                }
                else
                {
                    // we fill/flush the buffer (until the input is written)
                    int pos = 0; // position in the input data
                    int pieceLength;
                    while (pos < length)
                    {
                        pieceLength = (length - pos < bytesLeft) ? length - pos : bytesLeft;
                        Arrays.Copy(b, pos + offset, buffer, bufferPosition, pieceLength);
                        pos += pieceLength;
                        bufferPosition += pieceLength;
                        // if the buffer is full, flush it
                        bytesLeft = bufferSize - bufferPosition;
                        if (bytesLeft == 0)
                        {
                            Flush();
                            bytesLeft = bufferSize;
                        }
                    }
                }
            }
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public override void Flush()
        {
            if (buffer is null) return; // LUCENENET: Lazy-load the buffer, so we don't force all subclasses to allocate it
            crc.Update(buffer, 0, bufferPosition);
            FlushBuffer(buffer, bufferPosition);
            bufferStart += bufferPosition;
            bufferPosition = 0;
        }

        /// <summary>
        /// Expert: implements buffer write.  Writes bytes at the current position in
        /// the output. </summary>
        /// <param name="b"> the bytes to write </param>
        /// <param name="len"> the number of bytes to write </param>
        private void FlushBuffer(byte[] b, int len)
        {
            FlushBuffer(b, 0, len);
        }

        /// <summary>
        /// Expert: implements buffer write.  Writes bytes at the current position in
        /// the output. </summary>
        /// <param name="b"> the bytes to write </param>
        /// <param name="offset"> the offset in the byte array </param>
        /// <param name="len"> the number of bytes to write </param>
        protected internal abstract void FlushBuffer(byte[] b, int offset, int len);

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (0 != Interlocked.CompareExchange(ref this.disposed, 1, 0)) return; // LUCENENET specific - allow double-dispose

            if (disposing)
            {
                Flush();
            }
        }

        public override long Position => bufferStart + bufferPosition; // LUCENENET specific: Renamed from getFilePointer() to match FileStream

        [Obsolete("(4.1) this method will be removed in Lucene 5.0")]
        public override void Seek(long pos)
        {
            EnsureOpen(); // LUCENENET specific - ensure we can't be abused after dispose
            Flush();
            bufferStart = pos;
        }

        public override abstract long Length { get; }

        /// <summary>
        /// Returns size of the used output buffer in bytes.
        /// </summary>
        public int BufferSize => bufferSize;

        public override long Checksum
        {
            get
            {
                EnsureOpen(); // LUCENENET specific - ensure we can't be abused after dispose
                Flush();
                return crc.Value;
            }
        }

        // LUCENENET specific - ensure we can't be abused after dispose
        private bool IsOpen => Interlocked.CompareExchange(ref this.disposed, 0, 0) == 0 ? true : false;

        // LUCENENET specific - ensure we can't be abused after dispose
        private void EnsureOpen()
        {
            if (!IsOpen)
            {
                throw AlreadyClosedException.Create(this.GetType().FullName, "this IndexOutput is disposed.");
            }
        }
    }
}