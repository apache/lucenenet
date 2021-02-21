using J2N.Numerics;
using Lucene.Net.Diagnostics;
using System;
using System.Runtime.CompilerServices;

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
    /// A <see cref="DataOutput"/> wrapper to write unaligned, variable-length packed
    /// integers.
    /// <para/>
    /// @lucene.internal
    /// </summary>
    /// <seealso cref="PackedDataInput"/>
    public sealed class PackedDataOutput
    {
        internal readonly DataOutput @out;
        internal long current;
        internal int remainingBits;

        /// <summary>
        /// Create a new instance that wraps <paramref name="out"/>.
        /// </summary>
        public PackedDataOutput(DataOutput @out)
        {
            this.@out = @out;
            current = 0;
            remainingBits = 8;
        }

        /// <summary>
        /// Write a value using exactly <paramref name="bitsPerValue"/> bits.
        /// <para/>
        /// NOTE: This was writeLong() in Lucene.
        /// </summary>
        public void WriteInt64(long value, int bitsPerValue)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(bitsPerValue == 64 || (value >= 0 && value <= PackedInt32s.MaxValue(bitsPerValue)));
            while (bitsPerValue > 0)
            {
                if (remainingBits == 0)
                {
                    @out.WriteByte((byte)current);
                    current = 0L;
                    remainingBits = 8;
                }
                int bits = Math.Min(remainingBits, bitsPerValue);
                current = current | (((value.TripleShift((bitsPerValue - bits))) & ((1L << bits) - 1)) << (remainingBits - bits));
                bitsPerValue -= bits;
                remainingBits -= bits;
            }
        }

        /// <summary>
        /// Flush pending bits to the underlying <see cref="DataOutput"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Flush()
        {
            if (remainingBits < 8)
            {
                @out.WriteByte((byte)current);
            }
            remainingBits = 8;
            current = 0L;
        }
    }
}