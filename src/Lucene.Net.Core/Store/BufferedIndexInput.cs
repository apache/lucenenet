using System;
using System.Diagnostics;
using System.IO;

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
    /// Base implementation class for buffered <seealso cref="IndexInput"/>. </summary>
    public abstract class BufferedIndexInput : IndexInput
    {
        /// <summary>
        /// Default buffer size set to {@value #BUFFER_SIZE}. </summary>
        public const int BUFFER_SIZE = 1024;

        // The normal read buffer size defaults to 1024, but
        // increasing this during merging seems to yield
        // performance gains.  However we don't want to increase
        // it too much because there are quite a few
        // BufferedIndexInputs created during merging.  See
        // LUCENE-888 for details.
        /// <summary>
        /// A buffer size for merges set to {@value #MERGE_BUFFER_SIZE}.
        /// </summary>
        public const int MERGE_BUFFER_SIZE = 4096;

        private int bufferSize = BUFFER_SIZE;

        protected internal byte[] Buffer;

        private long BufferStart = 0; // position in file of buffer
        private int BufferLength = 0; // end of valid bytes
        private int BufferPosition = 0; // next byte to read

        public override sealed byte ReadByte()
        {
            if (BufferPosition >= BufferLength)
            {
                Refill();
            }
            return Buffer[BufferPosition++];
        }

        public BufferedIndexInput(string resourceDesc)
            : this(resourceDesc, BUFFER_SIZE)
        {
        }

        public BufferedIndexInput(string resourceDesc, IOContext context)
            : this(resourceDesc, BufferSize(context))
        {
        }

        /// <summary>
        /// Inits BufferedIndexInput with a specific bufferSize </summary>
        public BufferedIndexInput(string resourceDesc, int bufferSize)
            : base(resourceDesc)
        {
            CheckBufferSize(bufferSize);
            this.bufferSize = bufferSize;
        }

        /// <summary>
        /// Change the buffer size used by this IndexInput </summary>
        public int BufferSize_ // LUCENENET TODO: Rename BufferSize
        {
            set // LUCENENET TODO: make this into SetBufferSize() (complexity)
            {
                Debug.Assert(Buffer == null || bufferSize == Buffer.Length, "buffer=" + Buffer + " bufferSize=" + bufferSize + " buffer.length=" + (Buffer != null ? Buffer.Length : 0));
                if (value != bufferSize)
                {
                    CheckBufferSize(value);
                    bufferSize = value;
                    if (Buffer != null)
                    {
                        // Resize the existing buffer and carefully save as
                        // many bytes as possible starting from the current
                        // bufferPosition
                        byte[] newBuffer = new byte[value];
                        int leftInBuffer = BufferLength - BufferPosition;
                        int numToCopy;
                        if (leftInBuffer > value)
                        {
                            numToCopy = value;
                        }
                        else
                        {
                            numToCopy = leftInBuffer;
                        }
                        Array.Copy(Buffer, BufferPosition, newBuffer, 0, numToCopy);
                        BufferStart += BufferPosition;
                        BufferPosition = 0;
                        BufferLength = numToCopy;
                        NewBuffer(newBuffer);
                    }
                }
            }
            get
            {
                return bufferSize;
            }
        }

        protected virtual void NewBuffer(byte[] newBuffer)
        {
            // Subclasses can do something here
            Buffer = newBuffer;
        }

        private void CheckBufferSize(int bufferSize)
        {
            if (bufferSize <= 0)
            {
                throw new System.ArgumentException("bufferSize must be greater than 0 (got " + bufferSize + ")");
            }
        }

        public override sealed void ReadBytes(byte[] b, int offset, int len)
        {
            ReadBytes(b, offset, len, true);
        }

        public override sealed void ReadBytes(byte[] b, int offset, int len, bool useBuffer)
        {
            int available = BufferLength - BufferPosition;
            if (len <= available)
            {
                // the buffer contains enough data to satisfy this request
                if (len > 0) // to allow b to be null if len is 0...
                {
                    System.Buffer.BlockCopy(Buffer, BufferPosition, b, offset, len);
                }
                BufferPosition += len;
            }
            else
            {
                // the buffer does not have enough data. First serve all we've got.
                if (available > 0)
                {
                    System.Buffer.BlockCopy(Buffer, BufferPosition, b, offset, available);
                    offset += available;
                    len -= available;
                    BufferPosition += available;
                }
                // and now, read the remaining 'len' bytes:
                if (useBuffer && len < bufferSize)
                {
                    // If the amount left to read is small enough, and
                    // we are allowed to use our buffer, do it in the usual
                    // buffered way: fill the buffer and copy from it:
                    Refill();
                    if (BufferLength < len)
                    {
                        // Throw an exception when refill() could not read len bytes:
                        System.Buffer.BlockCopy(Buffer, 0, b, offset, BufferLength);
                        throw new EndOfStreamException("read past EOF: " + this);
                    }
                    else
                    {
                        System.Buffer.BlockCopy(Buffer, 0, b, offset, len);
                        BufferPosition = len;
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
                    long after = BufferStart + BufferPosition + len;
                    if (after > Length())
                    {
                        throw new EndOfStreamException("read past EOF: " + this);
                    }
                    ReadInternal(b, offset, len);
                    BufferStart = after;
                    BufferPosition = 0;
                    BufferLength = 0; // trigger refill() on read
                }
            }
        }

        public override sealed short ReadShort()
        {
            if (2 <= (BufferLength - BufferPosition))
            {
                return (short)(((Buffer[BufferPosition++] & 0xFF) << 8) | (Buffer[BufferPosition++] & 0xFF));
            }
            else
            {
                return base.ReadShort();
            }
        }

        public override sealed int ReadInt()
        {
            if (4 <= (BufferLength - BufferPosition))
            {
                return ((Buffer[BufferPosition++] & 0xFF) << 24) | ((Buffer[BufferPosition++] & 0xFF) << 16) | ((Buffer[BufferPosition++] & 0xFF) << 8) | (Buffer[BufferPosition++] & 0xFF);
            }
            else
            {
                return base.ReadInt();
            }
        }

        public override sealed long ReadLong()
        {
            if (8 <= (BufferLength - BufferPosition))
            {
                int i1 = ((Buffer[BufferPosition++] & 0xff) << 24) | ((Buffer[BufferPosition++] & 0xff) << 16) | ((Buffer[BufferPosition++] & 0xff) << 8) | (Buffer[BufferPosition++] & 0xff);
                int i2 = ((Buffer[BufferPosition++] & 0xff) << 24) | ((Buffer[BufferPosition++] & 0xff) << 16) | ((Buffer[BufferPosition++] & 0xff) << 8) | (Buffer[BufferPosition++] & 0xff);
                return (((long)i1) << 32) | (i2 & 0xFFFFFFFFL);
            }
            else
            {
                return base.ReadLong();
            }
        }

        public override sealed int ReadVInt()
        {
            if (5 <= (BufferLength - BufferPosition))
            {
                byte b = Buffer[BufferPosition++];
                if ((sbyte)b >= 0)
                {
                    return b;
                }
                int i = b & 0x7F;
                b = Buffer[BufferPosition++];
                i |= (b & 0x7F) << 7;
                if ((sbyte)b >= 0)
                {
                    return i;
                }
                b = Buffer[BufferPosition++];
                i |= (b & 0x7F) << 14;
                if ((sbyte)b >= 0)
                {
                    return i;
                }
                b = Buffer[BufferPosition++];
                i |= (b & 0x7F) << 21;
                if ((sbyte)b >= 0)
                {
                    return i;
                }
                b = Buffer[BufferPosition++];
                // Warning: the next ands use 0x0F / 0xF0 - beware copy/paste errors:
                i |= (b & 0x0F) << 28;
                if ((b & 0xF0) == 0)
                {
                    return i;
                }
                throw new System.IO.IOException("Invalid vInt detected (too many bits)");
            }
            else
            {
                return base.ReadVInt();
            }
        }

        public override sealed long ReadVLong()
        {
            if (9 <= BufferLength - BufferPosition)
            {
                byte b = Buffer[BufferPosition++];
                if ((sbyte)b >= 0)
                {
                    return b;
                }
                long i = b & 0x7FL;
                b = Buffer[BufferPosition++];
                i |= (b & 0x7FL) << 7;
                if ((sbyte)b >= 0)
                {
                    return i;
                }
                b = Buffer[BufferPosition++];
                i |= (b & 0x7FL) << 14;
                if ((sbyte)b >= 0)
                {
                    return i;
                }
                b = Buffer[BufferPosition++];
                i |= (b & 0x7FL) << 21;
                if ((sbyte)b >= 0)
                {
                    return i;
                }
                b = Buffer[BufferPosition++];
                i |= (b & 0x7FL) << 28;
                if ((sbyte)b >= 0)
                {
                    return i;
                }
                b = Buffer[BufferPosition++];
                i |= (b & 0x7FL) << 35;
                if ((sbyte)b >= 0)
                {
                    return i;
                }
                b = Buffer[BufferPosition++];
                i |= (b & 0x7FL) << 42;
                if ((sbyte)b >= 0)
                {
                    return i;
                }
                b = Buffer[BufferPosition++];
                i |= (b & 0x7FL) << 49;
                if ((sbyte)b >= 0)
                {
                    return i;
                }
                b = Buffer[BufferPosition++];
                i |= (b & 0x7FL) << 56;
                if ((sbyte)b >= 0)
                {
                    return i;
                }
                throw new System.IO.IOException("Invalid vLong detected (negative values disallowed)");
            }
            else
            {
                return base.ReadVLong();
            }
        }

        private void Refill()
        {
            long start = BufferStart + BufferPosition;
            long end = start + bufferSize;
            if (end > Length()) // don't read past EOF
            {
                end = Length();
            }
            int newLength = (int)(end - start);
            if (newLength <= 0)
            {
                throw new EndOfStreamException("read past EOF: " + this);
            }

            if (Buffer == null)
            {
                NewBuffer(new byte[bufferSize]); // allocate buffer lazily
                SeekInternal(BufferStart);
            }
            ReadInternal(Buffer, 0, newLength);
            BufferLength = newLength;
            BufferStart = start;
            BufferPosition = 0;
        }

        /// <summary>
        /// Expert: implements buffer refill.  Reads bytes from the current position
        /// in the input. </summary>
        /// <param name="b"> the array to read bytes into </param>
        /// <param name="offset"> the offset in the array to start storing bytes </param>
        /// <param name="length"> the number of bytes to read </param>
        protected abstract void ReadInternal(byte[] b, int offset, int length);

        public override sealed long FilePointer
        {
            get
            {
                return BufferStart + BufferPosition;
            }
        }

        public override sealed void Seek(long pos)
        {
            if (pos >= BufferStart && pos < (BufferStart + BufferLength))
            {
                BufferPosition = (int)(pos - BufferStart); // seek within buffer
            }
            else
            {
                BufferStart = pos;
                BufferPosition = 0;
                BufferLength = 0; // trigger refill() on read()
                SeekInternal(pos);
            }
        }

        /// <summary>
        /// Expert: implements seek.  Sets current position in this file, where the
        /// next <seealso cref="#readInternal(byte[],int,int)"/> will occur. </summary>
        /// <seealso cref= #readInternal(byte[],int,int) </seealso>
        protected abstract void SeekInternal(long pos);

        public override object Clone()
        {
            BufferedIndexInput clone = (BufferedIndexInput)base.Clone();

            clone.Buffer = null;
            clone.BufferLength = 0;
            clone.BufferPosition = 0;
            clone.BufferStart = FilePointer;

            return clone;
        }

        /// <summary>
        /// Flushes the in-memory buffer to the given output, copying at most
        /// <code>numBytes</code>.
        /// <p>
        /// <b>NOTE:</b> this method does not refill the buffer, however it does
        /// advance the buffer position.
        /// </summary>
        /// <returns> the number of bytes actually flushed from the in-memory buffer. </returns>
        protected int FlushBuffer(IndexOutput @out, long numBytes)
        {
            int toCopy = BufferLength - BufferPosition;
            if (toCopy > numBytes)
            {
                toCopy = (int)numBytes;
            }
            if (toCopy > 0)
            {
                @out.WriteBytes(Buffer, BufferPosition, toCopy);
                BufferPosition += toCopy;
            }
            return toCopy;
        }

        /// <summary>
        /// Returns default buffer sizes for the given <seealso cref="IOContext"/>
        /// </summary>
        public static int BufferSize(IOContext context)
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