using System;
using System.Diagnostics;

namespace Lucene.Net.Util.Packed
{
    using Lucene.Net.Support;

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
    /// A writer for large monotonically increasing sequences of positive longs.
    /// <p>
    /// The sequence is divided into fixed-size blocks and for each block, values
    /// are modeled after a linear function f: x &rarr; A &times; x + B. The block
    /// encodes deltas from the expected values computed from this function using as
    /// few bits as possible. Each block has an overhead between 6 and 14 bytes.
    /// <p>
    /// Format:
    /// <ul>
    /// <li>&lt;BLock&gt;<sup>BlockCount</sup>
    /// <li>BlockCount: &lceil; ValueCount / BlockSize &rceil;
    /// <li>Block: &lt;Header, (Ints)&gt;
    /// <li>Header: &lt;B, A, BitsPerValue&gt;
    /// <li>B: the B from f: x &rarr; A &times; x + B using a
    ///     <seealso cref="DataOutput#writeVLong(long) variable-length long"/>
    /// <li>A: the A from f: x &rarr; A &times; x + B encoded using
    ///     <seealso cref="Float#floatToIntBits(float)"/> on
    ///     <seealso cref="DataOutput#writeInt(int) 4 bytes"/>
    /// <li>BitsPerValue: a <seealso cref="DataOutput#writeVInt(int) variable-length int"/>
    /// <li>Ints: if BitsPerValue is <tt>0</tt>, then there is nothing to read and
    ///     all values perfectly match the result of the function. Otherwise, these
    ///     are the
    ///     <a href="https://developers.google.com/protocol-buffers/docs/encoding#types">zigzag-encoded</a>
    ///     <seealso cref="PackedInts packed"/> deltas from the expected value (computed from
    ///     the function) using exaclty BitsPerValue bits per value
    /// </ul> </summary>
    /// <seealso cref= MonotonicBlockPackedReader
    /// @lucene.internal </seealso>
    public sealed class MonotonicBlockPackedWriter : AbstractBlockPackedWriter
    {
        /// <summary>
        /// Sole constructor. </summary>
        /// <param name="blockSize"> the number of values of a single block, must be a power of 2 </param>
        public MonotonicBlockPackedWriter(DataOutput @out, int blockSize)
            : base(@out, blockSize)
        {
        }

        public override void Add(long l)
        {
            Debug.Assert(l >= 0);
            base.Add(l);
        }

        protected override void Flush()
        {
            Debug.Assert(Off > 0);

            // TODO: perform a true linear regression?
            long min = Values[0];
            float avg = Off == 1 ? 0f : (float)(Values[Off - 1] - min) / (Off - 1);

            long maxZigZagDelta = 0;
            for (int i = 0; i < Off; ++i)
            {
                Values[i] = ZigZagEncode(Values[i] - min - (long)(avg * i));
                maxZigZagDelta = Math.Max(maxZigZagDelta, Values[i]);
            }

            @out.WriteVLong(min);
            @out.WriteInt(Number.FloatToIntBits(avg));
            if (maxZigZagDelta == 0)
            {
                @out.WriteVInt(0);
            }
            else
            {
                int bitsRequired = PackedInts.BitsRequired(maxZigZagDelta);
                @out.WriteVInt(bitsRequired);
                WriteValues(bitsRequired);
            }

            Off = 0;
        }
    }
}