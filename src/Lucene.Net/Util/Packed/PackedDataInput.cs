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

    using DataInput = Lucene.Net.Store.DataInput;

    /// <summary>
    /// A <see cref="DataInput"/> wrapper to read unaligned, variable-length packed
    /// integers. This API is much slower than the <see cref="PackedInt32s"/> fixed-length
    /// API but can be convenient to save space. 
    /// <para/>
    /// @lucene.internal
    /// </summary>
    /// <seealso cref="PackedDataOutput"/>
    public sealed class PackedDataInput
    {
        internal readonly DataInput @in;
        internal long current;
        internal int remainingBits;

        /// <summary>
        /// Create a new instance that wraps <paramref name="in"/>.
        /// </summary>
        public PackedDataInput(DataInput @in)
        {
            this.@in = @in;
            SkipToNextByte();
        }

        /// <summary>
        /// Read the next <see cref="long"/> using exactly <paramref name="bitsPerValue"/> bits.
        /// <para/>
        /// NOTE: This was readLong() in Lucene.
        /// </summary>
        public long ReadInt64(int bitsPerValue)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(bitsPerValue > 0 && bitsPerValue <= 64, "{0}", bitsPerValue);
            long r = 0;
            while (bitsPerValue > 0)
            {
                if (remainingBits == 0)
                {
                    current = @in.ReadByte() & 0xFF;
                    remainingBits = 8;
                }
                int bits = Math.Min(bitsPerValue, remainingBits);
                r = (r << bits) | ((current.TripleShift((remainingBits - bits))) & ((1L << bits) - 1));
                bitsPerValue -= bits;
                remainingBits -= bits;
            }
            return r;
        }

        /// <summary>
        /// If there are pending bits (at most 7), they will be ignored and the next
        /// value will be read starting at the next byte.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SkipToNextByte()
        {
            remainingBits = 0;
        }
    }
}