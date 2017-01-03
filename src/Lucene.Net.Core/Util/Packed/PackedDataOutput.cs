using System;
using System.Diagnostics;

namespace Lucene.Net.Util.Packed
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
    /// A <seealso cref="DataOutput"/> wrapper to write unaligned, variable-length packed
    /// integers. </summary>
    /// <seealso cref= PackedDataInput
    /// @lucene.internal </seealso>
    public sealed class PackedDataOutput
    {
        internal readonly DataOutput @out;
        internal long current;
        internal int remainingBits;

        /// <summary>
        /// Create a new instance that wraps <code>out</code>.
        /// </summary>
        public PackedDataOutput(DataOutput @out)
        {
            this.@out = @out;
            current = 0;
            remainingBits = 8;
        }

        /// <summary>
        /// Write a value using exactly <code>bitsPerValue</code> bits.
        /// </summary>
        public void WriteLong(long value, int bitsPerValue) // LUCENENET TODO: Rename WriteInt64 ?
        {
            Debug.Assert(bitsPerValue == 64 || (value >= 0 && value <= PackedInts.MaxValue(bitsPerValue)));
            while (bitsPerValue > 0)
            {
                if (remainingBits == 0)
                {
                    @out.WriteByte((byte)(sbyte)current);
                    current = 0L;
                    remainingBits = 8;
                }
                int bits = Math.Min(remainingBits, bitsPerValue);
                current = current | ((((long)((ulong)value >> (bitsPerValue - bits))) & ((1L << bits) - 1)) << (remainingBits - bits));
                bitsPerValue -= bits;
                remainingBits -= bits;
            }
        }

        /// <summary>
        /// Flush pending bits to the underlying <seealso cref="DataOutput"/>.
        /// </summary>
        public void Flush()
        {
            if (remainingBits < 8)
            {
                @out.WriteByte((byte)(sbyte)current);
            }
            remainingBits = 8;
            current = 0L;
        }
    }
}