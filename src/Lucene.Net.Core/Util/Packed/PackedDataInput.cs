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

    using DataInput = Lucene.Net.Store.DataInput;

    /// <summary>
    /// A <seealso cref="DataInput"/> wrapper to read unaligned, variable-length packed
    /// integers. this API is much slower than the <seealso cref="PackedInts"/> fixed-length
    /// API but can be convenient to save space. </summary>
    /// <seealso cref= PackedDataOutput
    /// @lucene.internal </seealso>
    public sealed class PackedDataInput
    {
        internal readonly DataInput @in;
        internal long current;
        internal int remainingBits;

        /// <summary>
        /// Create a new instance that wraps <code>in</code>.
        /// </summary>
        public PackedDataInput(DataInput @in)
        {
            this.@in = @in;
            SkipToNextByte();
        }

        /// <summary>
        /// Read the next long using exactly <code>bitsPerValue</code> bits.
        /// </summary>
        public long ReadLong(int bitsPerValue)
        {
            Debug.Assert(bitsPerValue > 0 && bitsPerValue <= 64, bitsPerValue.ToString());
            long r = 0;
            while (bitsPerValue > 0)
            {
                if (remainingBits == 0)
                {
                    current = @in.ReadByte() & 0xFF;
                    remainingBits = 8;
                }
                int bits = Math.Min(bitsPerValue, remainingBits);
                r = (r << bits) | (((long)((ulong)current >> (remainingBits - bits))) & ((1L << bits) - 1));
                bitsPerValue -= bits;
                remainingBits -= bits;
            }
            return r;
        }

        /// <summary>
        /// If there are pending bits (at most 7), they will be ignored and the next
        /// value will be read starting at the next byte.
        /// </summary>
        public void SkipToNextByte()
        {
            remainingBits = 0;
        }
    }
}