using J2N.Numerics;
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

    using IndexInput = Lucene.Net.Store.IndexInput;

    internal sealed class DirectPacked64SingleBlockReader : PackedInt32s.ReaderImpl
    {
        private readonly IndexInput @in;
        private readonly long startPointer;
        private readonly int valuesPerBlock;
        private readonly long mask;

        internal DirectPacked64SingleBlockReader(int bitsPerValue, int valueCount, IndexInput @in)
            : base(valueCount, bitsPerValue)
        {
            this.@in = @in;
            startPointer = @in.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            valuesPerBlock = 64 / bitsPerValue;
            mask = ~(~0L << bitsPerValue);
        }

        public override long Get(int index)
        {
            int blockOffset = index / valuesPerBlock;
            long skip = ((long)blockOffset) << 3;
            try
            {
                @in.Seek(startPointer + skip);

                long block = @in.ReadInt64();
                int offsetInBlock = index % valuesPerBlock;
                return (block.TripleShift(offsetInBlock * m_bitsPerValue)) & mask;
            }
            catch (Exception e) when (e.IsIOException())
            {
                throw IllegalStateException.Create("failed", e);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override long RamBytesUsed()
        {
            return 0;
        }
    }
}