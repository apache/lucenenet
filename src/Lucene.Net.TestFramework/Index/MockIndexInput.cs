using Lucene.Net.Support;

namespace Lucene.Net.Index
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

    using BufferedIndexInput = Lucene.Net.Store.BufferedIndexInput;

    // TODO: what is this used for? just testing BufferedIndexInput?
    // if so it should be pkg-private. otherwise its a dup of ByteArrayIndexInput?
    /// <summary>
    /// <see cref="Store.IndexInput"/> backed by a <see cref="T:byte[]"/> for testing.
    /// </summary>
    public class MockIndexInput : BufferedIndexInput
    {
        private readonly byte[] buffer;
        private int pointer = 0;
        private readonly long length;

        public MockIndexInput(byte[] bytes)
            : base("MockIndexInput", BufferedIndexInput.BUFFER_SIZE)
        {
            buffer = bytes;
            length = bytes.Length;
        }

        protected override void ReadInternal(byte[] dest, int destOffset, int len)
        {
            int remainder = len;
            int start = pointer;
            while (remainder != 0)
            {
                //          int bufferNumber = start / buffer.length;
                int bufferOffset = start % buffer.Length;
                int bytesInBuffer = buffer.Length - bufferOffset;
                int bytesToCopy = bytesInBuffer >= remainder ? remainder : bytesInBuffer;
                Arrays.Copy(buffer, bufferOffset, dest, destOffset, bytesToCopy);
                destOffset += bytesToCopy;
                start += bytesToCopy;
                remainder -= bytesToCopy;
            }
            pointer += len;
        }

        protected override void Dispose(bool disposing)
        {
            // ignore
        }

        protected override void SeekInternal(long pos)
        {
            pointer = (int)pos;
        }

        public override long Length => length;
    }
}