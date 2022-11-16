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
            return (short)(((bytes[pos++] & 0xFF) << 8) | (bytes[pos++] & 0xFF));
        }

        /// <summary>
        /// NOTE: this was readInt() in Lucene
        /// </summary>
        public override int ReadInt32()
        {
            return ((bytes[pos++] & 0xFF) << 24) | ((bytes[pos++] & 0xFF) << 16) 
                | ((bytes[pos++] & 0xFF) << 8) | (bytes[pos++] & 0xFF);
        }

        /// <summary>
        /// NOTE: this was readLong() in Lucene
        /// </summary>
        public override long ReadInt64()
        {
            int i1 = ((bytes[pos++] & 0xff) << 24) | ((bytes[pos++] & 0xff) << 16) 
                | ((bytes[pos++] & 0xff) << 8) | (bytes[pos++] & 0xff);
            int i2 = ((bytes[pos++] & 0xff) << 24) | ((bytes[pos++] & 0xff) << 16) 
                | ((bytes[pos++] & 0xff) << 8) | (bytes[pos++] & 0xff);
            return (((long)i1) << 32) | (i2 & 0xFFFFFFFFL);
        }

        /// <summary>
        /// NOTE: this was readVInt() in Lucene
        /// </summary>
        public override int ReadVInt32()
        {
            byte b = bytes[pos++];
            if (b <= sbyte.MaxValue) // LUCENENET: Optimized equivalent of "if ((sbyte)b >= 0)"
            {
                return b;
            }
            int i = b & 0x7F;
            b = bytes[pos++];
            i |= (b & 0x7F) << 7;
            if (b <= sbyte.MaxValue) // LUCENENET: Optimized equivalent of "if ((sbyte)b >= 0)"
            {
                return i;
            }
            b = bytes[pos++];
            i |= (b & 0x7F) << 14;
            if (b <= sbyte.MaxValue) // LUCENENET: Optimized equivalent of "if ((sbyte)b >= 0)"
            {
                return i;
            }
            b = bytes[pos++];
            i |= (b & 0x7F) << 21;
            if (b <= sbyte.MaxValue) // LUCENENET: Optimized equivalent of "if ((sbyte)b >= 0)"
            {
                return i;
            }
            b = bytes[pos++];
            // Warning: the next ands use 0x0F / 0xF0 - beware copy/paste errors:
            i |= (b & 0x0F) << 28;
            if ((b & 0xF0) == 0)
            {
                return i;
            }
            throw RuntimeException.Create("Invalid VInt32 detected (too many bits)");
        }

        /// <summary>
        /// NOTE: this was readVLong() in Lucene
        /// </summary>
        public override long ReadVInt64()
        {
            byte b = bytes[pos++];
            if (b <= sbyte.MaxValue) // LUCENENET: Optimized equivalent of "if ((sbyte)b >= 0)"
            {
                return b;
            }
            long i = b & 0x7FL;
            b = bytes[pos++];
            i |= (b & 0x7FL) << 7;
            if (b <= sbyte.MaxValue) // LUCENENET: Optimized equivalent of "if ((sbyte)b >= 0)"
            {
                return i;
            }
            b = bytes[pos++];
            i |= (b & 0x7FL) << 14;
            if (b <= sbyte.MaxValue) // LUCENENET: Optimized equivalent of "if ((sbyte)b >= 0)"
            {
                return i;
            }
            b = bytes[pos++];
            i |= (b & 0x7FL) << 21;
            if (b <= sbyte.MaxValue) // LUCENENET: Optimized equivalent of "if ((sbyte)b >= 0)"
            {
                return i;
            }
            b = bytes[pos++];
            i |= (b & 0x7FL) << 28;
            if (b <= sbyte.MaxValue) // LUCENENET: Optimized equivalent of "if ((sbyte)b >= 0)"
            {
                return i;
            }
            b = bytes[pos++];
            i |= (b & 0x7FL) << 35;
            if (b <= sbyte.MaxValue) // LUCENENET: Optimized equivalent of "if ((sbyte)b >= 0)"
            {
                return i;
            }
            b = bytes[pos++];
            i |= (b & 0x7FL) << 42;
            if (b <= sbyte.MaxValue) // LUCENENET: Optimized equivalent of "if ((sbyte)b >= 0)"
            {
                return i;
            }
            b = bytes[pos++];
            i |= (b & 0x7FL) << 49;
            if (b <= sbyte.MaxValue) // LUCENENET: Optimized equivalent of "if ((sbyte)b >= 0)"
            {
                return i;
            }
            b = bytes[pos++];
            i |= (b & 0x7FL) << 56;
            if (b <= sbyte.MaxValue) // LUCENENET: Optimized equivalent of "if ((sbyte)b >= 0)"
            {
                return i;
            }
            throw RuntimeException.Create("Invalid VInt64 detected (negative values disallowed)");
        }

        // NOTE: AIOOBE not EOF if you read too much
        public override byte ReadByte()
        {
            return bytes[pos++];
        }

        // NOTE: AIOOBE not EOF if you read too much
        public override void ReadBytes(byte[] b, int offset, int len)
        {
            Arrays.Copy(bytes, pos, b, offset, len);
            pos += len;
        }
    }
}