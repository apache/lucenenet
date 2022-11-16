using Lucene.Net.Support;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Lucene.Net.Util
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

    using DataOutput = Lucene.Net.Store.DataOutput;

    /// <summary>
    /// A <see cref="DataOutput"/> that can be used to build a <see cref="T:byte[]"/>.
    /// <para/>
    /// @lucene.internal
    /// </summary>
    public sealed class GrowableByteArrayDataOutput : DataOutput
    {
        /// <summary>
        /// The bytes </summary>
        [WritableArray]
        [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
        public byte[] Bytes
        {
            get => bytes;
            set => bytes = value;
        }
        private byte[] bytes;

        /// <summary>
        /// The length </summary>
        public int Length { get; set; }

        /// <summary>
        /// Create a <see cref="GrowableByteArrayDataOutput"/> with the given initial capacity. </summary>
        public GrowableByteArrayDataOutput(int cp)
        {
            this.bytes = new byte[ArrayUtil.Oversize(cp, 1)];
            this.Length = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void WriteByte(byte b)
        {
            if (Length >= bytes.Length)
            {
                bytes = ArrayUtil.Grow(bytes);
            }
            bytes[Length++] = b;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void WriteBytes(byte[] b, int off, int len)
        {
            int newLength = Length + len;
            bytes = ArrayUtil.Grow(bytes, newLength);
            Arrays.Copy(b, off, bytes, Length, len);
            Length = newLength;
        }
    }
}