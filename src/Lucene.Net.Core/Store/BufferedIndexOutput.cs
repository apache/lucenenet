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

        private readonly int bufferSize;
        private readonly byte[] buffer;
        private long bufferStart = 0; // position in file of buffer
        private int bufferPosition = 0; // position in buffer
        private readonly CRC32 crc = new CRC32();

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
            this.bufferSize = bufferSize;
            buffer = new byte[bufferSize];
        }

        public override void WriteByte(byte b)
        {
            if (bufferPosition >= bufferSize)
            {
                Flush();
            }
            buffer[bufferPosition++] = b;
        }

        public override void WriteBytes(byte[] b, int offset, int length)
        {
            int bytesLeft = bufferSize - bufferPosition;
            // is there enough space in the buffer?
            if (bytesLeft >= length)
            {
                // we add the data to the end of the buffer
                System.Buffer.BlockCopy(b, offset, buffer, bufferPosition, length);
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
                    crc.Update((byte[])(Array)b, offset, length);
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
                        System.Buffer.BlockCopy(b, pos + offset, buffer, bufferPosition, pieceLength);
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

        public override void Flush()
        {
            crc.Update((byte[])(Array)buffer, 0, bufferPosition);
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

        public override void Dispose()
        {
            Flush();
        }

        public override long FilePointer
        {
            get
            {
                return bufferStart + bufferPosition;
            }
        }

        public override void Seek(long pos)
        {
            Flush();
            bufferStart = pos;
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
                return bufferSize;
            }
        }

        public override long Checksum
        {
            get
            {
                Flush();
                return crc.Value;
            }
        }
    }
}