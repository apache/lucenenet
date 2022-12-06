using Lucene.Net.Support;
using System;
using Lucene.Net.Diagnostics;
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
    /// A memory-resident <see cref="IndexOutput"/> implementation.
    /// <para/>
    /// @lucene.internal
    /// </summary>
    public class RAMOutputStream : IndexOutput
    {
        internal const int BUFFER_SIZE = 1024;

        private readonly RAMFile file; // LUCENENET: marked readonly

        private byte[] currentBuffer;
        private int currentBufferIndex;

        private int bufferPosition;
        private long bufferStart;
        private int bufferLength;

        private readonly BufferedChecksum crc = new BufferedChecksum(new CRC32()); // LUCENENET: marked readonly

        /// <summary>
        /// Construct an empty output buffer. </summary>
        public RAMOutputStream()
            : this(new RAMFile())
        {
        }

        public RAMOutputStream(RAMFile f)
        {
            file = f;

            // make sure that we switch to the
            // first needed buffer lazily
            currentBufferIndex = -1;
            currentBuffer = null;
        }

        /// <summary>
        /// Copy the current contents of this buffer to the named output. </summary>
        public virtual void WriteTo(DataOutput @out)
        {
            Flush();
            long end = file.length;
            long pos = 0;
            int buffer = 0;
            while (pos < end)
            {
                int length = BUFFER_SIZE;
                long nextPos = pos + length;
                if (nextPos > end) // at the last buffer
                {
                    length = (int)(end - pos);
                }
                @out.WriteBytes(file.GetBuffer(buffer++), length);
                pos = nextPos;
            }
        }

        /// <summary>
        /// Copy the current contents of this buffer to output
        /// byte array
        /// </summary>
        public virtual void WriteTo(byte[] bytes, int offset)
        {
            Flush();
            long end = file.length;
            long pos = 0;
            int buffer = 0;
            int bytesUpto = offset;
            while (pos < end)
            {
                int length = BUFFER_SIZE;
                long nextPos = pos + length;
                if (nextPos > end) // at the last buffer
                {
                    length = (int)(end - pos);
                }
                Arrays.Copy(file.GetBuffer(buffer++), 0, bytes, bytesUpto, length);
                bytesUpto += length;
                pos = nextPos;
            }
        }

        /// <summary>
        /// Resets this to an empty file. </summary>
        public virtual void Reset()
        {
            currentBuffer = null;
            currentBufferIndex = -1;
            bufferPosition = 0;
            bufferStart = 0;
            bufferLength = 0;
            file.Length = 0;
            crc.Reset();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Flush();
            }
        }

        [Obsolete("(4.1) this method will be removed in Lucene 5.0")]
        public override void Seek(long pos)
        {
            // set the file length in case we seek back
            // and flush() has not been called yet
            SetFileLength();
            if (pos < bufferStart || pos >= bufferStart + bufferLength)
            {
                currentBufferIndex = (int)(pos / BUFFER_SIZE);
                SwitchCurrentBuffer();
            }

            bufferPosition = (int)(pos % BUFFER_SIZE);
        }

        public override long Length
        {
            get => file.length;
            set
            {
            }
        }

        public override void WriteByte(byte b)
        {
            if (bufferPosition == bufferLength)
            {
                currentBufferIndex++;
                SwitchCurrentBuffer();
            }
            crc.Update(b);
            currentBuffer[bufferPosition++] = b;
        }

        public override void WriteBytes(byte[] b, int offset, int len)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(b != null);
            crc.Update(b, offset, len);
            while (len > 0)
            {
                if (bufferPosition == bufferLength)
                {
                    currentBufferIndex++;
                    SwitchCurrentBuffer();
                }

                int remainInBuffer = currentBuffer.Length - bufferPosition;
                int bytesToCopy = len < remainInBuffer ? len : remainInBuffer;
                Arrays.Copy(b, offset, currentBuffer, bufferPosition, bytesToCopy);
                offset += bytesToCopy;
                len -= bytesToCopy;
                bufferPosition += bytesToCopy;
            }
        }

        private void SwitchCurrentBuffer()
        {
            if (currentBufferIndex == file.NumBuffers)
            {
                currentBuffer = file.AddBuffer(BUFFER_SIZE);
            }
            else
            {
                currentBuffer = file.GetBuffer(currentBufferIndex);
            }
            bufferPosition = 0;
            bufferStart = (long)BUFFER_SIZE * (long)currentBufferIndex;
            bufferLength = currentBuffer.Length;
        }

        private void SetFileLength()
        {
            long pointer = bufferStart + bufferPosition;
            if (pointer > file.length)
            {
                file.Length = pointer;
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public override void Flush()
        {
            SetFileLength();
        }

        public override long Position => currentBufferIndex < 0 ? 0 : bufferStart + bufferPosition; // LUCENENET specific: Renamed from getFilePointer() to match FileStream

        /// <summary>
        /// Returns byte usage of all buffers. </summary>
        public virtual long GetSizeInBytes()
        {
            return (long)file.NumBuffers * (long)BUFFER_SIZE;
        }

        public override long Checksum => crc.Value;
    }
}