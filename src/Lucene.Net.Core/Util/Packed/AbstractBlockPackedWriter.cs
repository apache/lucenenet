using Lucene.Net.Support;
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

    public abstract class AbstractBlockPackedWriter // LUCENENET NOTE: made public rather than internal because has public subclasses
    {
        internal const int MIN_BLOCK_SIZE = 64;
        internal static readonly int MAX_BLOCK_SIZE = 1 << (30 - 3);
        internal static readonly int MIN_VALUE_EQUALS_0 = 1 << 0;
        internal const int BPV_SHIFT = 1;

        internal static long ZigZagEncode(long n)
        {
            return (n >> 63) ^ (n << 1);
        }

        // same as DataOutput.writeVLong but accepts negative values
        internal static void WriteVLong(DataOutput @out, long i)
        {
            int k = 0;
            while ((i & ~0x7FL) != 0L && k++ < 8)
            {
                @out.WriteByte(unchecked((byte)(sbyte)((i & 0x7FL) | 0x80L)));
                i = (long)((ulong)i >> 7);
            }
            @out.WriteByte((byte)(sbyte)i);
        }

        protected DataOutput m_out;
        protected readonly long[] m_values;
        protected byte[] m_blocks;
        protected int m_off;
        protected long m_ord;
        protected bool m_finished;

        /// <summary>
        /// Sole constructor. </summary>
        /// <param name="blockSize"> the number of values of a single block, must be a multiple of <tt>64</tt> </param>
        public AbstractBlockPackedWriter(DataOutput @out, int blockSize)
        {
            PackedInts.CheckBlockSize(blockSize, MIN_BLOCK_SIZE, MAX_BLOCK_SIZE);
            Reset(@out);
            m_values = new long[blockSize];
        }

        /// <summary>
        /// Reset this writer to wrap <code>out</code>. The block size remains unchanged. </summary>
        public virtual void Reset(DataOutput @out)
        {
            Debug.Assert(@out != null);
            this.m_out = @out;
            m_off = 0;
            m_ord = 0L;
            m_finished = false;
        }

        private void CheckNotFinished()
        {
            if (m_finished)
            {
                throw new InvalidOperationException("Already finished");
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
                throw new InvalidOperationException("" + m_off);
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
        /// Flush all buffered data to disk. this instance is not usable anymore
        ///  after this method has been called until <seealso cref="#reset(DataOutput)"/> has
        ///  been called.
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
        public virtual long Ord
        {
            get { return m_ord; }
        }

        protected abstract void Flush();

        protected void WriteValues(int bitsRequired)
        {
            PackedInts.IEncoder encoder = PackedInts.GetEncoder(PackedInts.Format.PACKED, PackedInts.VERSION_CURRENT, bitsRequired);
            int iterations = m_values.Length / encoder.ByteValueCount;
            int blockSize = encoder.ByteBlockCount * iterations;
            if (m_blocks == null || m_blocks.Length < blockSize)
            {
                m_blocks = new byte[blockSize];
            }
            if (m_off < m_values.Length)
            {
                Arrays.Fill(m_values, m_off, m_values.Length, 0L);
            }
            encoder.Encode(m_values, 0, m_blocks, 0, iterations);
            int blockCount = (int)PackedInts.Format.PACKED.ByteCount(PackedInts.VERSION_CURRENT, m_off, bitsRequired);
            m_out.WriteBytes(m_blocks, blockCount);
        }
    }
}