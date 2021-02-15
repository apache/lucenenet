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
    /// A writer for large sequences of longs.
    /// <para/>
    /// The sequence is divided into fixed-size blocks and for each block, the
    /// difference between each value and the minimum value of the block is encoded
    /// using as few bits as possible. Memory usage of this class is proportional to
    /// the block size. Each block has an overhead between 1 and 10 bytes to store
    /// the minimum value and the number of bits per value of the block.
    /// <para/>
    /// Format:
    /// <list type="bullet">
    /// <item><description>&lt;BLock&gt;<sup>BlockCount</sup></description></item>
    /// <item><description>BlockCount: &#8968; ValueCount / BlockSize &#8969;</description></item>
    /// <item><description>Block: &lt;Header, (Ints)&gt;</description></item>
    /// <item><description>Header: &lt;Token, (MinValue)&gt;</description></item>
    /// <item><description>Token: a byte (<see cref="DataOutput.WriteByte(byte)"/>), first 7 bits are the
    ///     number of bits per value (<c>bitsPerValue</c>). If the 8th bit is 1,
    ///     then MinValue (see next) is <c>0</c>, otherwise MinValue and needs to
    ///     be decoded</description></item>
    /// <item><description>MinValue: a
    ///     <a href="https://developers.google.com/protocol-buffers/docs/encoding#types">zigzag-encoded</a>
    ///      variable-length <see cref="long"/> (<see cref="DataOutput.WriteVInt64(long)"/>) whose value
    ///     should be added to every int from the block to restore the original
    ///     values</description></item>
    /// <item><description>Ints: If the number of bits per value is <c>0</c>, then there is
    ///     nothing to decode and all ints are equal to MinValue. Otherwise: BlockSize
    ///     packed ints (<see cref="PackedInt32s"/>) encoded on exactly <c>bitsPerValue</c>
    ///     bits per value. They are the subtraction of the original values and
    ///     MinValue</description></item>
    /// </list>
    /// <para/>
    /// @lucene.internal
    /// </summary>
    /// <seealso cref="BlockPackedReaderIterator"/>
    /// <seealso cref="BlockPackedReader"/>
    public sealed class BlockPackedWriter : AbstractBlockPackedWriter
    {
        /// <summary>
        /// Sole constructor. </summary>
        /// <param name="blockSize"> the number of values of a single block, must be a power of 2 </param>
        public BlockPackedWriter(DataOutput @out, int blockSize)
            : base(@out, blockSize)
        {
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        protected override void Flush()
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(m_off > 0);
            long min = long.MaxValue, max = long.MinValue;
            for (int i = 0; i < m_off; ++i)
            {
                min = Math.Min(m_values[i], min);
                max = Math.Max(m_values[i], max);
            }

            long delta = max - min;
            int bitsRequired = delta < 0 ? 64 : delta == 0L ? 0 : PackedInt32s.BitsRequired(delta);
            if (bitsRequired == 64)
            {
                // no need to delta-encode
                min = 0L;
            }
            else if (min > 0L)
            {
                // make min as small as possible so that writeVLong requires fewer bytes
                min = Math.Max(0L, max - PackedInt32s.MaxValue(bitsRequired));
            }

            int token = (bitsRequired << BPV_SHIFT) | (min == 0 ? MIN_VALUE_EQUALS_0 : 0);
            m_out.WriteByte((byte)token);

            if (min != 0)
            {
                WriteVInt64(m_out, ZigZagEncode(min) - 1);
            }

            if (bitsRequired > 0)
            {
                if (min != 0)
                {
                    for (int i = 0; i < m_off; ++i)
                    {
                        m_values[i] -= min;
                    }
                }
                WriteValues(bitsRequired);
            }

            m_off = 0;
        }
    }
}