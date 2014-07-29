using System.Diagnostics;

namespace Lucene.Net.Util.Packed
{
    using Lucene.Net.Support;
    using System;

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

    public abstract class AbstractBlockPackedWriter
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
                @out.WriteByte(unchecked((sbyte)((i & 0x7FL) | 0x80L)));
                i = (long)((ulong)i >> 7);
            }
            @out.WriteByte((sbyte)i);
        }

        protected internal DataOutput @out;
        protected internal readonly long[] Values;
        protected internal sbyte[] Blocks;
        protected internal int Off;
        protected internal long Ord_Renamed;
        protected internal bool Finished;

        /// <summary>
        /// Sole constructor. </summary>
        /// <param name="blockSize"> the number of values of a single block, must be a multiple of <tt>64</tt> </param>
        public AbstractBlockPackedWriter(DataOutput @out, int blockSize)
        {
            PackedInts.CheckBlockSize(blockSize, MIN_BLOCK_SIZE, MAX_BLOCK_SIZE);
            Reset(@out);
            Values = new long[blockSize];
        }

        /// <summary>
        /// Reset this writer to wrap <code>out</code>. The block size remains unchanged. </summary>
        public virtual void Reset(DataOutput @out)
        {
            Debug.Assert(@out != null);
            this.@out = @out;
            Off = 0;
            Ord_Renamed = 0L;
            Finished = false;
        }

        private void CheckNotFinished()
        {
            if (Finished)
            {
                throw new InvalidOperationException("Already finished");
            }
        }

        /// <summary>
        /// Append a new long. </summary>
        public virtual void Add(long l)
        {
            CheckNotFinished();
            if (Off == Values.Length)
            {
                Flush();
            }
            Values[Off++] = l;
            ++Ord_Renamed;
        }

        // For testing only
        public virtual void AddBlockOfZeros()
        {
            CheckNotFinished();
            if (Off != 0 && Off != Values.Length)
            {
                throw new InvalidOperationException("" + Off);
            }
            if (Off == Values.Length)
            {
                Flush();
            }
            Arrays.Fill(Values, 0);
            Off = Values.Length;
            Ord_Renamed += Values.Length;
        }

        /// <summary>
        /// Flush all buffered data to disk. this instance is not usable anymore
        ///  after this method has been called until <seealso cref="#reset(DataOutput)"/> has
        ///  been called.
        /// </summary>
        public virtual void Finish()
        {
            CheckNotFinished();
            if (Off > 0)
            {
                Flush();
            }
            Finished = true;
        }

        /// <summary>
        /// Return the number of values which have been added. </summary>
        public virtual long Ord()
        {
            return Ord_Renamed;
        }

        protected internal abstract void Flush();

        protected internal void WriteValues(int bitsRequired)
        {
            PackedInts.Encoder encoder = PackedInts.GetEncoder(PackedInts.Format.PACKED, PackedInts.VERSION_CURRENT, bitsRequired);
            int iterations = Values.Length / encoder.ByteValueCount();
            int blockSize = encoder.ByteBlockCount() * iterations;
            if (Blocks == null || Blocks.Length < blockSize)
            {
                Blocks = new sbyte[blockSize];
            }
            if (Off < Values.Length)
            {
                Arrays.Fill(Values, Off, Values.Length, 0L);
            }
            encoder.Encode(Values, 0, Blocks, 0, iterations);
            int blockCount = (int)PackedInts.Format.PACKED.ByteCount(PackedInts.VERSION_CURRENT, Off, bitsRequired);
            @out.WriteBytes(Blocks, blockCount);
        }
    }
}