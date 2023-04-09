using Lucene.Net.Diagnostics;
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

    using BytesRef = Lucene.Net.Util.BytesRef;

    /// <summary>
    /// DataOutput backed by a byte array.
    /// <b>WARNING:</b> this class omits most low-level checks,
    /// so be sure to test heavily with assertions enabled.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public class ByteArrayDataOutput : DataOutput
    {
        private byte[] bytes;

        private int pos;
        private int limit;

        public ByteArrayDataOutput(byte[] bytes)
        {
            // LUCENENET: Changed to call private method to avoid virtual method call in constructor
            ResetInternal(bytes, 0, bytes?.Length ?? 0);
        }

        public ByteArrayDataOutput(byte[] bytes, int offset, int len)
        {
            // LUCENENET: Changed to call private method to avoid virtual method call in constructor
            ResetInternal(bytes, offset, len);
        }

        public ByteArrayDataOutput()
        {
            // LUCENENET: Changed to call private method to avoid virtual method call in constructor
            ResetInternal(BytesRef.EMPTY_BYTES, 0, BytesRef.EMPTY_BYTES.Length);
        }

        /// <summary>
        /// 
        /// NOTE: When overriding this method, be aware that the constructor of this class calls 
        /// a private method and not this virtual method. So if you need to override
        /// the behavior during the initialization, call your own private method from the constructor
        /// with whatever custom behavior you need.
        /// </summary>
        public virtual void Reset(byte[] bytes) =>
            ResetInternal(bytes, 0, bytes?.Length ?? 0);

        /// <summary>
        /// 
        /// NOTE: When overriding this method, be aware that the constructor of this class calls 
        /// a private method and not this virtual method. So if you need to override
        /// the behavior during the initialization, call your own private method from the constructor
        /// with whatever custom behavior you need.
        /// </summary>
        public virtual void Reset(byte[] bytes, int offset, int len) =>
            ResetInternal(bytes, offset, len);

        // LUCENENET specific - created a private method that can be called
        // from the constructor and the Reset methods to avoid virtual method
        // calls in the constructor.
        private void ResetInternal(byte[] bytes, int offset, int len)
        {
            // LUCENENET: Added guard clauses
            if (bytes is null)
                throw new ArgumentNullException(nameof(bytes));
            if (offset < 0)
                throw new ArgumentOutOfRangeException(nameof(offset), offset, "Non-negative number required.");
            if (len < 0)
                throw new ArgumentOutOfRangeException(nameof(len), len, "Non-negative number required.");
            if (bytes.Length - offset < len)
                throw new ArgumentException("Offset and length were out of bounds for the array or length is greater than the number of elements from index to the end of the source array.");
                
            this.bytes = bytes;
            pos = offset;
            limit = offset + len;
        }

        public virtual int Position => pos;

        public override void WriteByte(byte b)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(pos < limit);
            bytes[pos++] = b;
        }

        public override void WriteBytes(byte[] b, int offset, int length)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(pos + length <= limit);
            Arrays.Copy(b, offset, bytes, pos, length);
            pos += length;
        }
    }
}