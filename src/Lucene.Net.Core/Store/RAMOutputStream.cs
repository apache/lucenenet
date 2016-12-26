using Lucene.Net.Support;
using System.Diagnostics;

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
    ///
    /// @lucene.internal
    /// </summary>
    public class RAMOutputStream : IndexOutput
    {
        internal const int BUFFER_SIZE = 1024;

        private RAMFile File;

        private byte[] CurrentBuffer;
        private int CurrentBufferIndex;

        private int BufferPosition;
        private long BufferStart;
        private int BufferLength;

        private BufferedChecksum Crc = new BufferedChecksum(new CRC32());

        /// <summary>
        /// Construct an empty output buffer. </summary>
        public RAMOutputStream()
            : this(new RAMFile())
        {
        }

        public RAMOutputStream(RAMFile f)
        {
            File = f;

            // make sure that we switch to the
            // first needed buffer lazily
            CurrentBufferIndex = -1;
            CurrentBuffer = null;
        }

        /// <summary>
        /// Copy the current contents of this buffer to the named output. </summary>
        public virtual void WriteTo(DataOutput @out)
        {
            Flush();
            long end = File.length;
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
                @out.WriteBytes(File.GetBuffer(buffer++), length);
                pos = nextPos;
            }
        }

        /// <summary>
        /// Copy the current contents of this buffer to output
        ///  byte array
        /// </summary>
        public virtual void WriteTo(byte[] bytes, int offset)
        {
            Flush();
            long end = File.length;
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
                System.Buffer.BlockCopy(File.GetBuffer(buffer++), 0, bytes, bytesUpto, length);
                bytesUpto += length;
                pos = nextPos;
            }
        }

        /// <summary>
        /// Resets this to an empty file. </summary>
        public virtual void Reset()
        {
            CurrentBuffer = null;
            CurrentBufferIndex = -1;
            BufferPosition = 0;
            BufferStart = 0;
            BufferLength = 0;
            File.Length = 0;
            Crc.Reset();
        }

        public override void Dispose()
        {
            Flush();
        }

        public override void Seek(long pos)
        {
            // set the file length in case we seek back
            // and flush() has not been called yet
            SetFileLength();
            if (pos < BufferStart || pos >= BufferStart + BufferLength)
            {
                CurrentBufferIndex = (int)(pos / BUFFER_SIZE);
                SwitchCurrentBuffer();
            }

            BufferPosition = (int)(pos % BUFFER_SIZE);
        }

        public override long Length
        {
            get
            {
                return File.length;
            }
            set
            {
            }
        }

        public override void WriteByte(byte b)
        {
            if (BufferPosition == BufferLength)
            {
                CurrentBufferIndex++;
                SwitchCurrentBuffer();
            }
            Crc.Update(b);
            CurrentBuffer[BufferPosition++] = b;
        }

        public override void WriteBytes(byte[] b, int offset, int len)
        {
            Debug.Assert(b != null);
            Crc.Update(b, offset, len);
            while (len > 0)
            {
                if (BufferPosition == BufferLength)
                {
                    CurrentBufferIndex++;
                    SwitchCurrentBuffer();
                }

                int remainInBuffer = CurrentBuffer.Length - BufferPosition;
                int bytesToCopy = len < remainInBuffer ? len : remainInBuffer;
                System.Buffer.BlockCopy(b, offset, CurrentBuffer, BufferPosition, bytesToCopy);
                offset += bytesToCopy;
                len -= bytesToCopy;
                BufferPosition += bytesToCopy;
            }
        }

        private void SwitchCurrentBuffer()
        {
            if (CurrentBufferIndex == File.NumBuffers())
            {
                CurrentBuffer = File.AddBuffer(BUFFER_SIZE);
            }
            else
            {
                CurrentBuffer = File.GetBuffer(CurrentBufferIndex);
            }
            BufferPosition = 0;
            BufferStart = (long)BUFFER_SIZE * (long)CurrentBufferIndex;
            BufferLength = CurrentBuffer.Length;
        }

        private void SetFileLength()
        {
            long pointer = BufferStart + BufferPosition;
            if (pointer > File.length)
            {
                File.Length = pointer;
            }
        }

        public override void Flush()
        {
            SetFileLength();
        }

        public override long FilePointer
        {
            get
            {
                return CurrentBufferIndex < 0 ? 0 : BufferStart + BufferPosition;
            }
        }

        /// <summary>
        /// Returns byte usage of all buffers. </summary>
        public virtual long SizeInBytes()
        {
            return (long)File.NumBuffers() * (long)BUFFER_SIZE;
        }

        public override long Checksum
        {
            get
            {
                return Crc.Value;
            }
        }
    }
}