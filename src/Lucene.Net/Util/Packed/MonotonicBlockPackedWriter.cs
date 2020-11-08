using System;
using Lucene.Net.Diagnostics;
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
    /// A writer for large monotonically increasing sequences of positive <see cref="long"/>s.
    /// <para/>
    /// The sequence is divided into fixed-size blocks and for each block, values
    /// are modeled after a linear function f: x &#8594; A &#215; x + B. The block
    /// encodes deltas from the expected values computed from this function using as
    /// few bits as possible. Each block has an overhead between 6 and 14 bytes.
    /// <para/>
    /// Format:
    /// <list type="bullet">
    /// <item><description>&lt;BLock&gt;<sup>BlockCount</sup></description></item>
    /// <item><description>BlockCount: &#8968; ValueCount / BlockSize &#8969;</description></item>
    /// <item><description>Block: &lt;Header, (Ints)&gt;</description></item>
    /// <item><description>Header: &lt;B, A, BitsPerValue&gt;</description></item>
    /// <item><description>B: the B from f: x &#8594; A &#215; x + B using a
    ///     variable-length <see cref="long"/> (<see cref="DataOutput.WriteVInt64(long)"/>)</description></item>
    /// <item><description>A: the A from f: x &#8594; A &#215; x + B encoded using
    ///     <see cref="J2N.BitConversion.SingleToInt32Bits(float)"/> on
    ///     4 bytes (<see cref="DataOutput.WriteVInt32(int)"/>)</description></item>
    /// <item><description>BitsPerValue: a variable-length <see cref="int"/> (<see cref="DataOutput.WriteVInt32(int)"/>)</description></item>
    /// <item><description>Ints: if BitsPerValue is <c>0</c>, then there is nothing to read and
    ///     all values perfectly match the result of the function. Otherwise, these
    ///     are the
    ///     <a href="https://developers.google.com/protocol-buffers/docs/encoding#types">zigzag-encoded</a>
    ///     packed (<see cref="PackedInt32s"/>) deltas from the expected value (computed from
    ///     the function) using exaclty BitsPerValue bits per value</description></item>
    /// </list> 
    /// <para/>
    /// @lucene.internal
    /// </summary>
    /// <seealso cref="MonotonicBlockPackedReader"/>
    public sealed class MonotonicBlockPackedWriter : AbstractBlockPackedWriter
    {
        /// <summary>
        /// Sole constructor. </summary>
        /// <param name="blockSize"> The number of values of a single block, must be a power of 2. </param>
        public MonotonicBlockPackedWriter(DataOutput @out, int blockSize)
            : base(@out, blockSize)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void Add(long l)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(l >= 0);
            base.Add(l);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        protected override void Flush()
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(m_off > 0);

            // TODO: perform a true linear regression?
            long min = m_values[0];
            float avg = m_off == 1 ? 0f : (float)(m_values[m_off - 1] - min) / (m_off - 1);

            long maxZigZagDelta = 0;
            for (int i = 0; i < m_off; ++i)
            {
                // LUCENENET NOTE: IMPORTANT: The cast to float is critical here for it to work in x86
                m_values[i] = ZigZagEncode(m_values[i] - min - (long)(float)(avg * i));
                maxZigZagDelta = Math.Max(maxZigZagDelta, m_values[i]);
            }

            m_out.WriteVInt64(min);
            m_out.WriteInt32(J2N.BitConversion.SingleToInt32Bits(avg));
            if (maxZigZagDelta == 0)
            {
                m_out.WriteVInt32(0);
            }
            else
            {
                int bitsRequired = PackedInt32s.BitsRequired(maxZigZagDelta);
                m_out.WriteVInt32(bitsRequired);
                WriteValues(bitsRequired);
            }

            m_off = 0;
        }
    }
}