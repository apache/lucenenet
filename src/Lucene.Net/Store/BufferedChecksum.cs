using Lucene.Net.Support;
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
    /// Wraps another <see cref="IChecksum"/> with an internal buffer
    /// to speed up checksum calculations.
    /// </summary>
    // LUCENENET NOTE: This class was public in Lucene. But it relies on the Java-centric
    // IChecksum interface.
    //
    // But there is no real benefit to changing this to use .NET's HashAlgorithm class:
    // 1) There are no APIs to update the checksum
    // 2) There is no way to make a single decorator class that can buffer any HashAlgorthm
    //    because it requires calls to protected members of HashAlgorithm.
    // 3) The HashAlgorithm class creates a huge number of allocations compared using this
    //    implementation.
    // See https://github.com/apache/lucenenet/pull/436#issuecomment-796797535
    //
    // We could potentially move IChecksum to J2N and make this public, but it would only
    // make sense to do so if there are users who would benefit from having this implementation available.
    // Given that .NET already has a way to make checksums, it is probably not worth the effort.
    internal class BufferedChecksum : IChecksum
    {
        private readonly IChecksum @in;
        private readonly byte[] buffer;
        private int upto;

        /// <summary>
        /// Default buffer size: 256 </summary>
        public const int DEFAULT_BUFFERSIZE = 256;

        /// <summary>
        /// Create a new <see cref="BufferedChecksum"/> with <see cref="DEFAULT_BUFFERSIZE"/> </summary>
        public BufferedChecksum(IChecksum @in)
            : this(@in, DEFAULT_BUFFERSIZE)
        {
        }

        /// <summary>
        /// Create a new <see cref="BufferedChecksum"/> with the specified <paramref name="bufferSize"/> </summary>
        public BufferedChecksum(IChecksum @in, int bufferSize)
        {
            this.@in = @in;
            this.buffer = new byte[bufferSize];
        }

        public virtual void Update(int b)
        {
            if (upto == buffer.Length)
            {
                Flush();
            }
            buffer[upto++] = (byte)b;
        }

        // LUCENENET specific overload for updating a whole byte[] array
        public virtual void Update(byte[] b)
        {
            Update(b, 0, b.Length);
        }

        public virtual void Update(byte[] b, int off, int len)
        {
            if (len >= buffer.Length)
            {
                Flush();
                @in.Update(b, off, len);
            }
            else
            {
                if (upto + len > buffer.Length)
                {
                    Flush();
                }
                Arrays.Copy(b, off, buffer, upto, len);
                upto += len;
            }
        }

        public virtual long Value
        {
            get
            {
                Flush();
                return @in.Value;
            }
        }

        public virtual void Reset()
        {
            upto = 0;
            @in.Reset();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Flush()
        {
            if (upto > 0)
            {
                @in.Update(buffer, 0, upto);
            }
            upto = 0;
        }
    }
}