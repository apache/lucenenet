using Lucene.Net.Support;

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
    /// A <seealso cref="DataOutput"/> that can be used to build a byte[].
    /// @lucene.internal
    /// </summary>
    public sealed class GrowableByteArrayDataOutput : DataOutput
    {
        /// <summary>
        /// The bytes </summary>
        [WritableArray]
        public byte[] Bytes
        {
            get { return bytes; }
            set { bytes = value; }
        }
        private byte[] bytes;

        /// <summary>
        /// The length </summary>
        public int Length { get; set; }

        /// <summary>
        /// Create a <seealso cref="GrowableByteArrayDataOutput"/> with the given initial capacity. </summary>
        public GrowableByteArrayDataOutput(int cp)
        {
            this.bytes = new byte[ArrayUtil.Oversize(cp, 1)];
            this.Length = 0;
        }

        public override void WriteByte(byte b)
        {
            if (Length >= bytes.Length)
            {
                bytes = ArrayUtil.Grow(bytes);
            }
            bytes[Length++] = b;
        }

        public override void WriteBytes(byte[] b, int off, int len)
        {
            int newLength = Length + len;
            bytes = ArrayUtil.Grow(bytes, newLength);
            System.Buffer.BlockCopy(b, off, bytes, Length, len);
            Length = newLength;
        }
    }
}