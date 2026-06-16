using Lucene.Net.Support;
using System;
using System.Buffers.Binary;

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
    /// DataInput backed by a byte array.
    /// <b>WARNING:</b> this class omits all low-level checks.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public sealed class ByteArrayDataInput : DataInput
    {
        private byte[] bytes;

        private int pos;
        private int limit;

        public ByteArrayDataInput(byte[] bytes)
        {
            Reset(bytes);
        }

        public ByteArrayDataInput(byte[] bytes, int offset, int len)
        {
            Reset(bytes, offset, len);
        }

        public ByteArrayDataInput()
        {
            Reset(BytesRef.EMPTY_BYTES);
        }

        public void Reset(byte[] bytes)
        {
            Reset(bytes, 0, bytes.Length);
        }

        /// <summary>
        /// NOTE: sets pos to 0, which is not right if you had
        /// called reset w/ non-zero offset!!
        /// </summary>
        public void Rewind()
        {
            pos = 0;
        }

        public int Position
        {
            get => pos;
            set => this.pos = value;
        }

        public void Reset(byte[] bytes, int offset, int len)
        {
            this.bytes = bytes;
            pos = offset;
            limit = offset + len;
        }

        public int Length => limit;

        public bool Eof => pos == limit;

        public override void SkipBytes(long count)
        {
            pos += (int)count;
        }

        /// <summary>
        /// LUCENENET NOTE: Important - always cast to ushort (System.UInt16) before using to ensure
        /// the value is positive!
        /// <para/>
        /// NOTE: this was readShort() in Lucene
        /// </summary>
        public override short ReadInt16()
        {
            // return (short)(((bytes[pos++] & 0xFF) << 8) | (bytes[pos++] & 0xFF));

            // LUCENENET: Use BinaryPrimitives for JIT-intrinsics opportunity
            short value = BinaryPrimitives.ReadInt16BigEndian(bytes.AsSpan(pos, sizeof(short)));
            pos += sizeof(short);
            return value;
        }

        /// <summary>
        /// NOTE: this was readInt() in Lucene
        /// </summary>
        public override int ReadInt32()
        {
            // return ((bytes[pos++] & 0xFF) << 24) | ((bytes[pos++] & 0xFF) << 16)
            //     | ((bytes[pos++] & 0xFF) << 8) | (bytes[pos++] & 0xFF);

            // LUCENENET: Use BinaryPrimitives for JIT-intrinsics opportunity
            int value = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(pos, sizeof(int)));
            pos += sizeof(int);
            return value;
        }

        /// <summary>
        /// NOTE: this was readLong() in Lucene
        /// </summary>
        public override long ReadInt64()
        {
            // int i1 = ((bytes[pos++] & 0xff) << 24) | ((bytes[pos++] & 0xff) << 16)
            //     | ((bytes[pos++] & 0xff) << 8) | (bytes[pos++] & 0xff);
            // int i2 = ((bytes[pos++] & 0xff) << 24) | ((bytes[pos++] & 0xff) << 16)
            //     | ((bytes[pos++] & 0xff) << 8) | (bytes[pos++] & 0xff);
            // return (((long)i1) << 32) | (i2 & 0xFFFFFFFFL);

            // LUCENENET: Use BinaryPrimitives for JIT-intrinsics opportunity
            long value = BinaryPrimitives.ReadInt64BigEndian(bytes.AsSpan(pos, sizeof(long)));
            pos += sizeof(long);
            return value;
        }

        /// <summary>
        /// NOTE: this was readVInt() in Lucene
        /// </summary>
        public override int ReadVInt32()
        {
            // LUCENENET: unify logic in VIntUtils. Unlike the buffered readers, this class
            // intentionally omits a MaxVInt32Length bounds guard (and the base.ReadVInt32()
            // fallback): per its class summary it "omits all low-level checks", matching the
            // original byte-by-byte code which read straight from the array and let an
            // out-of-range access throw on truncated/malformed input.
            bool ok = VIntUtils.TryReadVInt32(bytes.AsSpan(pos), out int value, out int count);
            pos += count;
            if (!ok)
            {
                VIntUtils.ThrowRuntimeException_InvalidVInt32();
            }
            return value;
        }

        /// <summary>
        /// NOTE: this was readVLong() in Lucene
        /// </summary>
        public override long ReadVInt64()
        {
            // LUCENENET: unify logic in VIntUtils. See ReadVInt32() above for why this
            // intentionally omits a MaxVInt64Length bounds guard and base fallback.
            bool ok = VIntUtils.TryReadVInt64(bytes.AsSpan(pos), out long value, out int count);
            pos += count;
            if (!ok)
            {
                VIntUtils.ThrowRuntimeException_InvalidVInt64();
            }
            return value;
        }

        // NOTE: AIOOBE not EOF if you read too much
        public override byte ReadByte()
        {
            return bytes[pos++];
        }

        // NOTE: AIOOBE not EOF if you read too much
        // LUCENENET: Use Span<byte> instead of byte[] for better compatibility.
        public override void ReadBytes(Span<byte> destination)
        {
            int len = destination.Length;
            Arrays.Copy(bytes, pos, destination, /*offset*/ 0, len);
            pos += len;
        }
    }
}
