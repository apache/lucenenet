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
        internal const int BUFFER_SIZE = RAMOutputStream.BUFFER_SIZE;

        private RAMFile file;
        private long length;

        private byte[] currentBuffer;
        private int currentBufferIndex;

        private int bufferPosition;
        private long bufferStart;
        private int bufferLength;

        public RAMInputStream(string name, RAMFile f)
            : base("RAMInputStream(name=" + name + ")")
        {
            file = f;
            length = file.length;
            if (length / BUFFER_SIZE >= int.MaxValue)
            {
                throw new System.IO.IOException("RAMInputStream too large length=" + length + ": " + name);
            }

            // make sure that we switch to the
            // first needed buffer lazily
            currentBufferIndex = -1;
            currentBuffer = null;
        }

        public override void Dispose()
        {
            // nothing to do here
        }

        public override long Length
        {
            get { return length; }
        }

        public override byte ReadByte()
        {
            if (bufferPosition >= bufferLength)
            {
                currentBufferIndex++;
                SwitchCurrentBuffer(true);
            }
            return currentBuffer[bufferPosition++];
        }

        public override void ReadBytes(byte[] b, int offset, int len)
        {
            while (len > 0)
            {
                if (bufferPosition >= bufferLength)
                {
                    currentBufferIndex++;
                    SwitchCurrentBuffer(true);
                }

                int remainInBuffer = bufferLength - bufferPosition;
                int bytesToCopy = len < remainInBuffer ? len : remainInBuffer;
                System.Buffer.BlockCopy(currentBuffer, bufferPosition, b, offset, bytesToCopy);
                offset += bytesToCopy;
                len -= bytesToCopy;
                bufferPosition += bytesToCopy;
            }
        }

        private void SwitchCurrentBuffer(bool enforceEOF)
        {
            bufferStart = (long)BUFFER_SIZE * (long)currentBufferIndex;
            if (currentBufferIndex >= file.NumBuffers())
            {
                // end of file reached, no more buffers left
                if (enforceEOF)
                {
                    throw new EndOfStreamException("read past EOF: " + this);
                }
                else
                {
                    // Force EOF if a read takes place at this position
                    currentBufferIndex--;
                    bufferPosition = BUFFER_SIZE;
                }
            }
            else
            {
                currentBuffer = file.GetBuffer(currentBufferIndex);
                bufferPosition = 0;
                long buflen = length - bufferStart;
                bufferLength = buflen > BUFFER_SIZE ? BUFFER_SIZE : (int)buflen;
            }
        }

        public override long FilePointer
        {
            get
            {
                return currentBufferIndex < 0 ? 0 : bufferStart + bufferPosition;
            }
        }

        public override void Seek(long pos)
        {
            if (currentBuffer == null || pos < bufferStart || pos >= bufferStart + BUFFER_SIZE)
            {
                currentBufferIndex = (int)(pos / BUFFER_SIZE);
                SwitchCurrentBuffer(false);
            }
            bufferPosition = (int)(pos % BUFFER_SIZE);
        }
    }
}