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
    /// Wraps another <seealso cref="Checksum"/> with an internal buffer
    /// to speed up checksum calculations.
    /// </summary>
    public class BufferedChecksum : IChecksum
    {
        private readonly IChecksum @in;
        private readonly byte[] Buffer;
        private int Upto;

        /// <summary>
        /// Default buffer size: 256 </summary>
        public const int DEFAULT_BUFFERSIZE = 256;

        /// <summary>
        /// Create a new BufferedChecksum with <seealso cref="#DEFAULT_BUFFERSIZE"/> </summary>
        public BufferedChecksum(IChecksum @in)
            : this(@in, DEFAULT_BUFFERSIZE)
        {
        }

        /// <summary>
        /// Create a new BufferedChecksum with the specified bufferSize </summary>
        public BufferedChecksum(IChecksum @in, int bufferSize)
        {
            this.@in = @in;
            this.Buffer = new byte[bufferSize];
        }

        public virtual void Update(int b)
        {
            if (Upto == Buffer.Length)
            {
                Flush();
            }
            Buffer[Upto++] = (byte)b;
        }

        public virtual void Update(byte[] b)
        {
            Update(b, 0, b.Length);
        }

        public virtual void Update(byte[] b, int off, int len)
        {
            if (len >= Buffer.Length)
            {
                Flush();
                @in.Update((byte[])(Array)b, off, len);
            }
            else
            {
                if (Upto + len > Buffer.Length)
                {
                    Flush();
                }
                System.Buffer.BlockCopy(b, off, Buffer, Upto, len);
                Upto += len;
            }
        }

        public long Value
        {
            get
            {
                Flush();
                return @in.Value;
            }
        }

        public void Reset()
        {
            Upto = 0;
            @in.Reset();
        }

        private void Flush()
        {
            if (Upto > 0)
            {
                @in.Update((byte[])(Array)Buffer, 0, Upto); // LUCENENET TODO: remove cast
            }
            Upto = 0;
        }
    }
}