using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using System;
using System.IO;
using System.Runtime.CompilerServices;

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
    /// Base implementation class for buffered <see cref="IndexInput"/>. </summary>
    public abstract class BufferedIndexInput : IndexInput
    {
        /// <summary>
        /// Default buffer size set to <see cref="BUFFER_SIZE"/>. </summary>
        public const int BUFFER_SIZE = 1024;

        // The normal read buffer size defaults to 1024, but
        // increasing this during merging seems to yield
        // performance gains.  However we don't want to increase
        // it too much because there are quite a few
        // BufferedIndexInputs created during merging.  See
        // LUCENE-888 for details.
        /// <summary>
        /// A buffer size for merges set to <see cref="MERGE_BUFFER_SIZE"/>.
        /// </summary>
        public const int MERGE_BUFFER_SIZE = 4096;

        private int bufferSize = BUFFER_SIZE;

        protected byte[] m_buffer;

        private long bufferStart = 0; // position in file of buffer
        private int bufferLength = 0; // end of valid bytes
        private int bufferPosition = 0; // next byte to read

        public override sealed byte ReadByte()
        {
            if (bufferPosition >= bufferLength)
            {
                Refill();
            }
            return m_buffer[bufferPosition++];
        }

        protected BufferedIndexInput(string resourceDesc) // LUCENENET: CA1012: Abstract types should not have constructors (marked protected)
            : this(resourceDesc, BUFFER_SIZE)
        {
        }

        protected BufferedIndexInput(string resourceDesc, IOContext context) // LUCENENET: CA1012: Abstract types should not have constructors (marked protected)
            : this(resourceDesc, GetBufferSize(context))
        {
        }

        /// <summary>
        /// Inits <see cref="BufferedIndexInput"/> with a specific <paramref name="bufferSize"/> </summary>
        protected BufferedIndexInput(string resourceDesc, int bufferSize) // LUCENENET: CA1012: Abstract types should not have constructors (marked protected)
            : base(resourceDesc)
        {
            CheckBufferSize(bufferSize);
            this.bufferSize = bufferSize;
        }

        /// <summary>
        /// Change the buffer size used by this <see cref="IndexInput"/> </summary>
        public void SetBufferSize(int newSize)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(m_buffer is null || bufferSize == m_buffer.Length, "buffer={0} bufferSize={1} buffer.length={2}", m_buffer, bufferSize, (m_buffer != null ? m_buffer.Length : 0));
            if (newSize != bufferSize)
            {
                CheckBufferSize(newSize);
                bufferSize = newSize;
                if (m_buffer != null)
                {
                    // Resize the existing buffer and carefully save as
                    // many bytes as possible starting from the current
                    // bufferPosition
                    byte[] newBuffer = new byte[newSize];
                    int leftInBuffer = bufferLength - bufferPosition;
                    int numToCopy;
                    if (leftInBuffer > newSize)
                    {
                        numToCopy = newSize;
                    }
                    else
                    {
                        numToCopy = leftInBuffer;
                    }
                    Arrays.Copy(m_buffer, bufferPosition, newBuffer, 0, numToCopy);
                    bufferStart += bufferPosition;
                    bufferPosition = 0;
                    bufferLength = numToCopy;
                    NewBuffer(newBuffer);
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected virtual void NewBuffer(byte[] newBuffer)
        {
            // Subclasses can do something here
            m_buffer = newBuffer;
        }

        /// <summary>
        /// Returns buffer size.
        /// </summary>
        /// <seealso cref="SetBufferSize(int)"/>
        public int BufferSize => bufferSize;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void CheckBufferSize(int bufferSize) // LUCENENET: CA1822: Mark members as static
        {
            if (bufferSize <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bufferSize), "bufferSize must be greater than 0 (got " + bufferSize + ")"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override sealed void ReadBytes(byte[] b, int offset, int len)
        {
            ReadBytes(b, offset, len, true);
        }

        public override sealed void ReadBytes(byte[] b, int offset, int len, bool useBuffer)
        {
            int available = bufferLength - bufferPosition;
            if (len <= available)
            {
                // the buffer contains enough data to satisfy this request
                if (len > 0) // to allow b to be null if len is 0...
                {
                    Arrays.Copy(m_buffer, bufferPosition, b, offset, len);
                }
                bufferPosition += len;
            }
            else
            {
                // the buffer does not have enough data. First serve all we've got.
                if (available > 0)
                {
                    Arrays.Copy(m_buffer, bufferPosition, b, offset, available);
                    offset += available;
                    len -= available;
                    bufferPosition += available;
                }
                // and now, read the remaining 'len' bytes:
                if (useBuffer && len < bufferSize)
                {
                    // If the amount left to read is small enough, and
                    // we are allowed to use our buffer, do it in the usual
                    // buffered way: fill the buffer and copy from it:
                    Refill();
                    if (bufferLength < len)
                    {
                        // Throw an exception when refill() could not read len bytes:
                        Arrays.Copy(m_buffer, 0, b, offset, bufferLength);
                        throw EOFException.Create("read past EOF: " + this);
                    }
                    else
                    {
                        Arrays.Copy(m_buffer, 0, b, offset, len);
                        bufferPosition = len;
                    }
                }
                else
                {
                    // The amount left to read is larger than the buffer
                    // or we've been asked to not use our buffer -
                    // there's no performance reason not to read it all
                    // at once. Note that unlike the previous code of
                    // this function, there is no need to do a seek
                    // here, because there's no need to reread what we
                    // had in the buffer.
                    long after = bufferStart + bufferPosition + len;
                    if (after > Length)
                    {
                        throw EOFException.Create("read past EOF: " + this);
                    }
                    ReadInternal(b, offset, len);
                    bufferStart = after;
                    bufferPosition = 0;
                    bufferLength = 0; // trigger refill() on read
                }
            }
        }

        /// <summary>
        /// NOTE: this was readShort() in Lucene
        /// </summary>
        public override sealed short ReadInt16()
        {
            if (2 <= (bufferLength - bufferPosition))
            {
                return (short)(((m_buffer[bufferPosition++] & 0xFF) << 8) | (m_buffer[bufferPosition++] & 0xFF));
            }
            else
            {
                return base.ReadInt16();
            }
        }

        /// <summary>
        /// NOTE: this was readInt() in Lucene
        /// </summary>
        public override sealed int ReadInt32()
        {
            if (4 <= (bufferLength - bufferPosition))
            {
                return ((m_buffer[bufferPosition++] & 0xFF) << 24) | ((m_buffer[bufferPosition++] & 0xFF) << 16)
                    | ((m_buffer[bufferPosition++] & 0xFF) << 8) | (m_buffer[bufferPosition++] & 0xFF);
            }
            else
            {
                return base.ReadInt32();
            }
        }

        /// <summary>
        /// NOTE: this was readLong() in Lucene
        /// </summary>
        public override sealed long ReadInt64()
        {
            if (8 <= (bufferLength - bufferPosition))
            {
                int i1 = ((m_buffer[bufferPosition++] & 0xff) << 24) | ((m_buffer[bufferPosition++] & 0xff) << 16)
                    | ((m_buffer[bufferPosition++] & 0xff) << 8) | (m_buffer[bufferPosition++] & 0xff);
                int i2 = ((m_buffer[bufferPosition++] & 0xff) << 24) | ((m_buffer[bufferPosition++] & 0xff) << 16)
                    | ((m_buffer[bufferPosition++] & 0xff) << 8) | (m_buffer[bufferPosition++] & 0xff);
                return (((long)i1) << 32) | (i2 & 0xFFFFFFFFL);
            }
            else
            {
                return base.ReadInt64();
            }
        }

        /// <summary>
        /// NOTE: this was readVInt() in Lucene
        /// </summary>
        public override sealed int ReadVInt32()
        {
            if (5 <= (bufferLength - bufferPosition))
            {
                byte b = m_buffer[bufferPosition++];
                if (b <= sbyte.MaxValue) // LUCENENET: Optimized equivalent of "if ((sbyte)b >= 0)"
                {
                    return b;
                }
                int i = b & 0x7F;
                b = m_buffer[bufferPosition++];
                i |= (b & 0x7F) << 7;
                if (b <= sbyte.MaxValue) // LUCENENET: Optimized equivalent of "if ((sbyte)b >= 0)"
                {
                    return i;
                }
                b = m_buffer[bufferPosition++];
                i |= (b & 0x7F) << 14;
                if (b <= sbyte.MaxValue) // LUCENENET: Optimized equivalent of "if ((sbyte)b >= 0)"
                {
                    return i;
                }
                b = m_buffer[bufferPosition++];
                i |= (b & 0x7F) << 21;
                if (b <= sbyte.MaxValue) // LUCENENET: Optimized equivalent of "if ((sbyte)b >= 0)"
                {
                    return i;
                }
                b = m_buffer[bufferPosition++];
                // Warning: the next ands use 0x0F / 0xF0 - beware copy/paste errors:
                i |= (b & 0x0F) << 28;
                if ((b & 0xF0) == 0)
                {
                    return i;
                }
                throw new IOException("Invalid VInt32 detected (too many bits)");
            }
            else
            {
                return base.ReadVInt32();
            }
        }

        /// <summary>
        /// NOTE: this was readVLong() in Lucene
        /// </summary>
        public override sealed long ReadVInt64()
        {
            if (9 <= bufferLength - bufferPosition)
            {
                byte b = m_buffer[bufferPosition++];
                if (b <= sbyte.MaxValue) // LUCENENET: Optimized equivalent of "if ((sbyte)b >= 0)"
                {
                    return b;
                }
                long i = b & 0x7FL;
                b = m_buffer[bufferPosition++];
                i |= (b & 0x7FL) << 7;
                if (b <= sbyte.MaxValue) // LUCENENET: Optimized equivalent of "if ((sbyte)b >= 0)"
                {
                    return i;
                }
                b = m_buffer[bufferPosition++];
                i |= (b & 0x7FL) << 14;
                if (b <= sbyte.MaxValue) // LUCENENET: Optimized equivalent of "if ((sbyte)b >= 0)"
                {
                    return i;
                }
                b = m_buffer[bufferPosition++];
                i |= (b & 0x7FL) << 21;
                if (b <= sbyte.MaxValue) // LUCENENET: Optimized equivalent of "if ((sbyte)b >= 0)"
                {
                    return i;
                }
                b = m_buffer[bufferPosition++];
                i |= (b & 0x7FL) << 28;
                if (b <= sbyte.MaxValue) // LUCENENET: Optimized equivalent of "if ((sbyte)b >= 0)"
                {
                    return i;
                }
                b = m_buffer[bufferPosition++];
                i |= (b & 0x7FL) << 35;
                if (b <= sbyte.MaxValue) // LUCENENET: Optimized equivalent of "if ((sbyte)b >= 0)"
                {
                    return i;
                }
                b = m_buffer[bufferPosition++];
                i |= (b & 0x7FL) << 42;
                if (b <= sbyte.MaxValue) // LUCENENET: Optimized equivalent of "if ((sbyte)b >= 0)"
                {
                    return i;
                }
                b = m_buffer[bufferPosition++];
                i |= (b & 0x7FL) << 49;
                if (b <= sbyte.MaxValue) // LUCENENET: Optimized equivalent of "if ((sbyte)b >= 0)"
                {
                    return i;
                }
                b = m_buffer[bufferPosition++];
                i |= (b & 0x7FL) << 56;
                if (b <= sbyte.MaxValue) // LUCENENET: Optimized equivalent of "if ((sbyte)b >= 0)"
                {
                    return i;
                }
                throw new IOException("Invalid VInt64 detected (negative values disallowed)");
            }
            else
            {
                return base.ReadVInt64();
            }
        }

        private void Refill()
        {
            long start = bufferStart + bufferPosition;
            long end = start + bufferSize;
            if (end > Length) // don't read past EOF
            {
                end = Length;
            }
            int newLength = (int)(end - start);
            if (newLength <= 0)
            {
                throw EOFException.Create("read past EOF: " + this);
            }

            if (m_buffer is null)
            {
                NewBuffer(new byte[bufferSize]); // allocate buffer lazily
                SeekInternal(bufferStart);
            }
            ReadInternal(m_buffer, 0, newLength);
            bufferLength = newLength;
            bufferStart = start;
            bufferPosition = 0;
        }

        /// <summary>
        /// Expert: implements buffer refill.  Reads bytes from the current position
        /// in the input. </summary>
        /// <param name="b"> the array to read bytes into </param>
        /// <param name="offset"> the offset in the array to start storing bytes </param>
        /// <param name="length"> the number of bytes to read </param>
        protected abstract void ReadInternal(byte[] b, int offset, int length);

        public override sealed long Position => bufferStart + bufferPosition; // LUCENENET specific: Renamed from getFilePointer() to match FileStream

        public override sealed void Seek(long pos)
        {
            if (pos >= bufferStart && pos < (bufferStart + bufferLength))
            {
                bufferPosition = (int)(pos - bufferStart); // seek within buffer
            }
            else
            {
                bufferStart = pos;
                bufferPosition = 0;
                bufferLength = 0; // trigger refill() on read()
                SeekInternal(pos);
            }
        }

        /// <summary>
        /// Expert: implements seek.  Sets current position in this file, where the
        /// next <see cref="ReadInternal(byte[], int, int)"/> will occur. </summary>
        /// <seealso cref="ReadInternal(byte[], int, int)"/>
        protected abstract void SeekInternal(long pos);

        public override object Clone()
        {
            BufferedIndexInput clone = (BufferedIndexInput)base.Clone();

            clone.m_buffer = null;
            clone.bufferLength = 0;
            clone.bufferPosition = 0;
            clone.bufferStart = Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream

            return clone;
        }

        /// <summary>
        /// Flushes the in-memory buffer to the given output, copying at most
        /// <paramref name="numBytes"/>.
        /// <para/>
        /// <b>NOTE:</b> this method does not refill the buffer, however it does
        /// advance the buffer position.
        /// </summary>
        /// <returns> the number of bytes actually flushed from the in-memory buffer. </returns>
        protected int FlushBuffer(IndexOutput @out, long numBytes)
        {
            int toCopy = bufferLength - bufferPosition;
            if (toCopy > numBytes)
            {
                toCopy = (int)numBytes;
            }
            if (toCopy > 0)
            {
                @out.WriteBytes(m_buffer, bufferPosition, toCopy);
                bufferPosition += toCopy;
            }
            return toCopy;
        }

        /// <summary>
        /// Returns default buffer sizes for the given <see cref="IOContext"/>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetBufferSize(IOContext context) // LUCENENET NOTE: Renamed from BufferSize to prevent naming conflict
        {
            switch (context.Context)
            {
                case IOContext.UsageContext.MERGE:
                    return MERGE_BUFFER_SIZE;

                default:
                    return BUFFER_SIZE;
            }
        }
    }
}