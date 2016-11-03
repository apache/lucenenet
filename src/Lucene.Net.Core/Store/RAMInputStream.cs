using System;
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
    /// A memory-resident <seealso cref="IndexInput"/> implementation.
    ///
    ///  @lucene.internal
    /// </summary>
    public class RAMInputStream : IndexInput
    {
        public const int BUFFER_SIZE = RAMOutputStream.BUFFER_SIZE;

        private RAMFile File;
        private long Length_Renamed;

        private byte[] CurrentBuffer;
        private int CurrentBufferIndex;

        private int BufferPosition;
        private long BufferStart;
        private int BufferLength;

        public RAMInputStream(string name, RAMFile f)
            : base("RAMInputStream(name=" + name + ")")
        {
            File = f;
            Length_Renamed = File.Length_Renamed;
            if (Length_Renamed / BUFFER_SIZE >= int.MaxValue)
            {
                throw new System.IO.IOException("RAMInputStream too large length=" + Length_Renamed + ": " + name);
            }

            // make sure that we switch to the
            // first needed buffer lazily
            CurrentBufferIndex = -1;
            CurrentBuffer = null;
        }

        public override void Dispose()
        {
            // nothing to do here
        }

        public override long Length()
        {
            return Length_Renamed;
        }

        public override byte ReadByte()
        {
            if (BufferPosition >= BufferLength)
            {
                CurrentBufferIndex++;
                SwitchCurrentBuffer(true);
            }
            return CurrentBuffer[BufferPosition++];
        }

        public override void ReadBytes(byte[] b, int offset, int len)
        {
            while (len > 0)
            {
                if (BufferPosition >= BufferLength)
                {
                    CurrentBufferIndex++;
                    SwitchCurrentBuffer(true);
                }

                int remainInBuffer = BufferLength - BufferPosition;
                int bytesToCopy = len < remainInBuffer ? len : remainInBuffer;
                System.Buffer.BlockCopy(CurrentBuffer, BufferPosition, b, offset, bytesToCopy);
                offset += bytesToCopy;
                len -= bytesToCopy;
                BufferPosition += bytesToCopy;
            }
        }

        private void SwitchCurrentBuffer(bool enforceEOF)
        {
            BufferStart = (long)BUFFER_SIZE * (long)CurrentBufferIndex;
            if (CurrentBufferIndex >= File.NumBuffers())
            {
                // end of file reached, no more buffers left
                if (enforceEOF)
                {
                    throw new EndOfStreamException("read past EOF: " + this);
                }
                else
                {
                    // Force EOF if a read takes place at this position
                    CurrentBufferIndex--;
                    BufferPosition = BUFFER_SIZE;
                }
            }
            else
            {
                CurrentBuffer = File.GetBuffer(CurrentBufferIndex);
                BufferPosition = 0;
                long buflen = Length_Renamed - BufferStart;
                BufferLength = buflen > BUFFER_SIZE ? BUFFER_SIZE : (int)buflen;
            }
        }

        public override long FilePointer
        {
            get
            {
                return CurrentBufferIndex < 0 ? 0 : BufferStart + BufferPosition;
            }
        }

        public override void Seek(long pos)
        {
            if (CurrentBuffer == null || pos < BufferStart || pos >= BufferStart + BUFFER_SIZE)
            {
                CurrentBufferIndex = (int)(pos / BUFFER_SIZE);
                SwitchCurrentBuffer(false);
            }
            BufferPosition = (int)(pos % BUFFER_SIZE);
        }
    }
}