using System;
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

    using BytesRef = Lucene.Net.Util.BytesRef;

    /// <summary>
    /// DataOutput backed by a byte array.
    /// <b>WARNING:</b> this class omits most low-level checks,
    /// so be sure to test heavily with assertions enabled.
    /// @lucene.experimental
    /// </summary>
    public class ByteArrayDataOutput : DataOutput
    {
        private byte[] bytes;

        private int pos;
        private int limit;

        public ByteArrayDataOutput(byte[] bytes)
        {
            Reset(bytes);
        }

        public ByteArrayDataOutput(byte[] bytes, int offset, int len)
        {
            Reset(bytes, offset, len);
        }

        public ByteArrayDataOutput()
        {
            Reset(BytesRef.EMPTY_BYTES);
        }

        public virtual void Reset(byte[] bytes)
        {
            Reset(bytes, 0, bytes.Length);
        }

        public virtual void Reset(byte[] bytes, int offset, int len)
        {
            this.bytes = bytes;
            pos = offset;
            limit = offset + len;
        }

        public virtual int Position
        {
            get
            {
                return pos;
            }
        }

        public override void WriteByte(byte b)
        {
            Debug.Assert(pos < limit);
            bytes[pos++] = b;
        }

        public override void WriteBytes(byte[] b, int offset, int length)
        {
            Debug.Assert(pos + length <= limit);
            System.Buffer.BlockCopy(b, offset, bytes, pos, length);
            pos += length;
        }
    }
}