using Lucene.Net.Support;
using System;

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
    /// Base implementation class for buffered <seealso cref="IndexOutput"/>. </summary>
    public abstract class BufferedIndexOutput : IndexOutput
    {
        /// <summary>
        /// The default buffer size in bytes ({@value #DEFAULT_BUFFER_SIZE}). </summary>
        public const int DEFAULT_BUFFER_SIZE = 16384;

        private readonly int BufferSize_Renamed;
        private readonly byte[] Buffer;
        private long BufferStart = 0; // position in file of buffer
        private int BufferPosition = 0; // position in buffer
        private readonly CRC32 Crc = new CRC32();

        /// <summary>
        /// Creates a new <seealso cref="BufferedIndexOutput"/> with the default buffer size
        /// ({@value #DEFAULT_BUFFER_SIZE} bytes see <seealso cref="#DEFAULT_BUFFER_SIZE"/>)
        /// </summary>
        public BufferedIndexOutput()
            : this(DEFAULT_BUFFER_SIZE)
        {
        }

        /// <summary>
        /// Creates a new <seealso cref="BufferedIndexOutput"/> with the given buffer size. </summary>
        /// <param name="bufferSize"> the buffer size in bytes used to buffer writes internally. </param>
        /// <exception cref="IllegalArgumentException"> if the given buffer size is less or equal to <tt>0</tt> </exception>
        public BufferedIndexOutput(int bufferSize)
        {
            if (bufferSize <= 0)
            {
                throw new System.ArgumentException("bufferSize must be greater than 0 (got " + bufferSize + ")");
            }
            this.BufferSize_Renamed = bufferSize;
            Buffer = new byte[bufferSize];
        }

        public override void WriteByte(byte b)
        {
            if (BufferPosition >= BufferSize_Renamed)
            {
                Flush();
            }
            Buffer[BufferPosition++] = b;
        }

        public override void WriteBytes(byte[] b, int offset, int length)
        {
            int bytesLeft = BufferSize_Renamed - BufferPosition;
            // is there enough space in the buffer?
            if (bytesLeft >= length)
            {
                // we add the data to the end of the buffer
                System.Buffer.BlockCopy(b, offset, Buffer, BufferPosition, length);
                BufferPosition += length;
                // if the buffer is full, flush it
                if (BufferSize_Renamed - BufferPosition == 0)
                {
                    Flush();
                }
            }
            else
            {
                // is data larger then buffer?
                if (length > BufferSize_Renamed)
                {
                    // we flush the buffer
                    if (BufferPosition > 0)
                    {
                        Flush();
                    }
                    // and write data at once
                    Crc.Update((byte[])(Array)b, offset, length);
                    FlushBuffer(b, offset, length);
                    BufferStart += length;
                }
                else
                {
                    // we fill/flush the buffer (until the input is written)
                    int pos = 0; // position in the input data
                    int pieceLength;
                    while (pos < length)
                    {
                        pieceLength = (length - pos < bytesLeft) ? length - pos : bytesLeft;
                        System.Buffer.BlockCopy(b, pos + offset, Buffer, BufferPosition, pieceLength);
                        pos += pieceLength;
                        BufferPosition += pieceLength;
                        // if the buffer is full, flush it
                        bytesLeft = BufferSize_Renamed - BufferPosition;
                        if (bytesLeft == 0)
                        {
                            Flush();
                            bytesLeft = BufferSize_Renamed;
                        }
                    }
                }
            }
        }

        public override void Flush()
        {
            Crc.Update((byte[])(Array)Buffer, 0, BufferPosition);
            FlushBuffer(Buffer, BufferPosition);
            BufferStart += BufferPosition;
            BufferPosition = 0;
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

        public override void Dispose()
        {
            Flush();
        }

        public override long FilePointer
        {
            get
            {
                return BufferStart + BufferPosition;
            }
        }

        public override void Seek(long pos)
        {
            Flush();
            BufferStart = pos;
        }

        public override abstract long Length { get; }

        /// <summary>
        /// Returns size of the used output buffer in bytes.
        ///
        /// </summary>
        public int BufferSize
        {
            get
            {
                return BufferSize_Renamed;
            }
        }

        public override long Checksum
        {
            get
            {
                Flush();
                return Crc.Value;
            }
        }
    }
}