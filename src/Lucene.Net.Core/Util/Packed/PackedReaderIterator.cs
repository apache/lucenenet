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

    using DataInput = Lucene.Net.Store.DataInput;

    internal sealed class PackedReaderIterator : PackedInts.ReaderIteratorImpl
    {
        internal readonly int PackedIntsVersion;
        internal readonly PackedInts.Format Format;
        internal readonly BulkOperation BulkOperation;
        internal readonly byte[] NextBlocks;
        internal readonly LongsRef NextValues;
        internal readonly int Iterations_Renamed;
        internal int Position;

        internal PackedReaderIterator(PackedInts.Format format, int packedIntsVersion, int valueCount, int bitsPerValue, DataInput @in, int mem)
            : base(valueCount, bitsPerValue, @in)
        {
            this.Format = format;
            this.PackedIntsVersion = packedIntsVersion;
            BulkOperation = BulkOperation.Of(format, bitsPerValue);
            Iterations_Renamed = Iterations(mem);
            Debug.Assert(valueCount == 0 || Iterations_Renamed > 0);
            NextBlocks = new byte[Iterations_Renamed * BulkOperation.ByteBlockCount()];
            NextValues = new LongsRef(new long[Iterations_Renamed * BulkOperation.ByteValueCount()], 0, 0);
            NextValues.Offset = NextValues.Longs.Length;
            Position = -1;
        }

        private int Iterations(int mem)
        {
            int iterations = BulkOperation.ComputeIterations(valueCount, mem);
            if (PackedIntsVersion < PackedInts.VERSION_BYTE_ALIGNED)
            {
                // make sure iterations is a multiple of 8
                iterations = (iterations + 7) & unchecked((int)0xFFFFFFF8);
            }
            return iterations;
        }

        public override LongsRef Next(int count)
        {
            Debug.Assert(NextValues.Length >= 0);
            Debug.Assert(count > 0);
            Debug.Assert(NextValues.Offset + NextValues.Length <= NextValues.Longs.Length);

            NextValues.Offset += NextValues.Length;

            int remaining = valueCount - Position - 1;
            if (remaining <= 0)
            {
                throw new System.IO.EndOfStreamException();
            }
            count = Math.Min(remaining, count);

            if (NextValues.Offset == NextValues.Longs.Length)
            {
                long remainingBlocks = Format.ByteCount(PackedIntsVersion, remaining, bitsPerValue);
                int blocksToRead = (int)Math.Min(remainingBlocks, NextBlocks.Length);
                @in.ReadBytes(NextBlocks, 0, blocksToRead);
                if (blocksToRead < NextBlocks.Length)
                {
                    Arrays.Fill(NextBlocks, blocksToRead, NextBlocks.Length, (byte)0);
                }

                BulkOperation.Decode(NextBlocks, 0, NextValues.Longs, 0, Iterations_Renamed);
                NextValues.Offset = 0;
            }

            NextValues.Length = Math.Min(NextValues.Longs.Length - NextValues.Offset, count);
            Position += NextValues.Length;
            return NextValues;
        }

        public override int Ord()
        {
            return Position;
        }
    }
}