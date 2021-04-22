using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using System;
using System.IO;
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

    internal sealed class PackedReaderIterator : PackedInt32s.ReaderIterator
    {
        internal readonly int packedIntsVersion;
        internal readonly PackedInt32s.Format format;
        internal readonly BulkOperation bulkOperation;
        internal readonly byte[] nextBlocks;
        internal readonly Int64sRef nextValues;
        internal readonly int iterations;
        internal int position;

        internal PackedReaderIterator(PackedInt32s.Format format, int packedIntsVersion, int valueCount, int bitsPerValue, DataInput @in, int mem)
            : base(valueCount, bitsPerValue, @in)
        {
            this.format = format;
            this.packedIntsVersion = packedIntsVersion;
            bulkOperation = BulkOperation.Of(format, bitsPerValue);
            iterations = Iterations(mem);
            if (Debugging.AssertsEnabled) Debugging.Assert(valueCount == 0 || iterations > 0);
            nextBlocks = new byte[iterations * bulkOperation.ByteBlockCount];
            nextValues = new Int64sRef(new long[iterations * bulkOperation.ByteValueCount], 0, 0);
            nextValues.Offset = nextValues.Int64s.Length;
            position = -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private int Iterations(int mem)
        {
            int iterations = bulkOperation.ComputeIterations(m_valueCount, mem);
            if (packedIntsVersion < PackedInt32s.VERSION_BYTE_ALIGNED)
            {
                // make sure iterations is a multiple of 8
                iterations = (iterations + 7) & unchecked((int)0xFFFFFFF8);
            }
            return iterations;
        }

        public override Int64sRef Next(int count)
        {
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(nextValues.Length >= 0);
                Debugging.Assert(count > 0);
                Debugging.Assert(nextValues.Offset + nextValues.Length <= nextValues.Int64s.Length);
            }

            nextValues.Offset += nextValues.Length;

            int remaining = m_valueCount - position - 1;
            if (remaining <= 0)
            {
                throw EOFException.Create();
            }
            count = Math.Min(remaining, count);

            if (nextValues.Offset == nextValues.Int64s.Length)
            {
                long remainingBlocks = format.ByteCount(packedIntsVersion, remaining, m_bitsPerValue);
                int blocksToRead = (int)Math.Min(remainingBlocks, nextBlocks.Length);
                m_in.ReadBytes(nextBlocks, 0, blocksToRead);
                if (blocksToRead < nextBlocks.Length)
                {
                    Arrays.Fill(nextBlocks, blocksToRead, nextBlocks.Length, (byte)0);
                }

                bulkOperation.Decode(nextBlocks, 0, nextValues.Int64s, 0, iterations);
                nextValues.Offset = 0;
            }

            nextValues.Length = Math.Min(nextValues.Int64s.Length - nextValues.Offset, count);
            position += nextValues.Length;
            return nextValues;
        }

        public override int Ord => position;
    }
}