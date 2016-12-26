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
        internal long Current;
        internal int RemainingBits;

        /// <summary>
        /// Create a new instance that wraps <code>out</code>.
        /// </summary>
        public PackedDataOutput(DataOutput @out)
        {
            this.@out = @out;
            Current = 0;
            RemainingBits = 8;
        }

        /// <summary>
        /// Write a value using exactly <code>bitsPerValue</code> bits.
        /// </summary>
        public void WriteLong(long value, int bitsPerValue) // LUCENENET TODO: Rename WriteInt64 ?
        {
            Debug.Assert(bitsPerValue == 64 || (value >= 0 && value <= PackedInts.MaxValue(bitsPerValue)));
            while (bitsPerValue > 0)
            {
                if (RemainingBits == 0)
                {
                    @out.WriteByte((byte)(sbyte)Current);
                    Current = 0L;
                    RemainingBits = 8;
                }
                int bits = Math.Min(RemainingBits, bitsPerValue);
                Current = Current | ((((long)((ulong)value >> (bitsPerValue - bits))) & ((1L << bits) - 1)) << (RemainingBits - bits));
                bitsPerValue -= bits;
                RemainingBits -= bits;
            }
        }

        /// <summary>
        /// Flush pending bits to the underlying <seealso cref="DataOutput"/>.
        /// </summary>
        public void Flush()
        {
            if (RemainingBits < 8)
            {
                @out.WriteByte((byte)(sbyte)Current);
            }
            RemainingBits = 8;
            Current = 0L;
        }
    }
}