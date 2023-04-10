using J2N.Numerics;
using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
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

    public abstract class AbstractBlockPackedWriter // LUCENENET NOTE: made public rather than internal because has public subclasses
    {
        internal const int MIN_BLOCK_SIZE = 64;
        internal const int MAX_BLOCK_SIZE = 1 << (30 - 3);
        internal const int MIN_VALUE_EQUALS_0 = 1 << 0;
        internal const int BPV_SHIFT = 1;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static long ZigZagEncode(long n)
        {
            return (n >> 63) ^ (n << 1);
        }

        // same as DataOutput.WriteVInt64 but accepts negative values
        /// <summary>
        /// NOTE: This was writeVLong() in Lucene.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void WriteVInt64(DataOutput @out, long i)
        {
            int k = 0;
            while ((i & ~0x7FL) != 0L && k++ < 8)
            {
                @out.WriteByte((byte)((i & 0x7FL) | 0x80L));
                i = i.TripleShift(7);
            }
            @out.WriteByte((byte)i);
        }

        protected DataOutput m_out;
        protected readonly long[] m_values;
        protected byte[] m_blocks;
        protected int m_off;
        protected long m_ord;
        protected bool m_finished;

        /// <summary>
        /// Sole constructor. </summary>
        /// <param name="blockSize"> the number of values of a single block, must be a multiple of <c>64</c>. </param>
        protected AbstractBlockPackedWriter(DataOutput @out, int blockSize) // LUCENENET specific - marked protected instead of public
        {
            PackedInt32s.CheckBlockSize(blockSize, MIN_BLOCK_SIZE, MAX_BLOCK_SIZE);
            ResetInternal(@out); // LUCENENET specific - calling private method instead of virtual Reset
            m_values = new long[blockSize];
        }

        /// <summary>
        /// Reset this writer to wrap <paramref name="out"/>. The block size remains unchanged.
        ///
        /// NOTE: When overriding this method, be aware that the constructor of this class calls 
        /// a private method and not this virtual method. So if you need to override
        /// the behavior during the initialization, call your own private method from the constructor
        /// with whatever custom behavior you need.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void Reset(DataOutput @out) => ResetInternal(@out);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ResetInternal(DataOutput @out)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(@out != null);
            this.m_out = @out;
            m_off = 0;
            m_ord = 0L;
            m_finished = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CheckNotFinished()
        {
            if (m_finished)
            {
                throw IllegalStateException.Create("Already finished");
            }
        }

        /// <summary>
        /// Append a new long. </summary>
        public virtual void Add(long l)
        {
            CheckNotFinished();
            if (m_off == m_values.Length)
            {
                Flush();
            }
            m_values[m_off++] = l;
            ++m_ord;
        }

        // For testing only
        internal virtual void AddBlockOfZeros()
        {
            CheckNotFinished();
            if (m_off != 0 && m_off != m_values.Length)
            {
                throw IllegalStateException.Create("" + m_off);
            }
            if (m_off == m_values.Length)
            {
                Flush();
            }
            Arrays.Fill(m_values, 0);
            m_off = m_values.Length;
            m_ord += m_values.Length;
        }

        /// <summary>
        /// Flush all buffered data to disk. This instance is not usable anymore
        /// after this method has been called until <see cref="Reset(DataOutput)"/> has
        /// been called.
        /// </summary>
        public virtual void Finish()
        {
            CheckNotFinished();
            if (m_off > 0)
            {
                Flush();
            }
            m_finished = true;
        }

        /// <summary>
        /// Return the number of values which have been added. </summary>
        public virtual long Ord => m_ord;

        [MethodImpl(MethodImplOptions.NoInlining)]
        protected abstract void Flush();

        protected void WriteValues(int bitsRequired)
        {
            PackedInt32s.IEncoder encoder = PackedInt32s.GetEncoder(PackedInt32s.Format.PACKED, PackedInt32s.VERSION_CURRENT, bitsRequired);
            int iterations = m_values.Length / encoder.ByteValueCount;
            int blockSize = encoder.ByteBlockCount * iterations;
            if (m_blocks is null || m_blocks.Length < blockSize)
            {
                m_blocks = new byte[blockSize];
            }
            if (m_off < m_values.Length)
            {
                Arrays.Fill(m_values, m_off, m_values.Length, 0L);
            }
            encoder.Encode(m_values, 0, m_blocks, 0, iterations);
            int blockCount = (int)PackedInt32s.Format.PACKED.ByteCount(PackedInt32s.VERSION_CURRENT, m_off, bitsRequired);
            m_out.WriteBytes(m_blocks, blockCount);
        }
    }
}