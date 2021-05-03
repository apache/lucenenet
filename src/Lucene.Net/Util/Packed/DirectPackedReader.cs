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

    /* Reads directly from disk on each get */

    internal class DirectPackedReader : PackedInt32s.ReaderImpl
    {
        private readonly IndexInput @in;
        private readonly long startPointer;
        private readonly long valueMask;

        public DirectPackedReader(int bitsPerValue, int valueCount, IndexInput @in)
            : base(valueCount, bitsPerValue)
        {
            this.@in = @in;

            startPointer = @in.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
            if (bitsPerValue == 64)
            {
                valueMask = -1L;
            }
            else
            {
                valueMask = (1L << bitsPerValue) - 1;
            }
        }

        public override long Get(int index)
        {
            long majorBitPos = (long)index * m_bitsPerValue;
            long elementPos = majorBitPos.TripleShift(3);
            try
            {
                @in.Seek(startPointer + elementPos);

                int bitPos = (int)(majorBitPos & 7);
                // round up bits to a multiple of 8 to find total bytes needed to read
                int roundedBits = ((bitPos + m_bitsPerValue + 7) & ~7);
                // the number of extra bits read at the end to shift out
                int shiftRightBits = roundedBits - bitPos - m_bitsPerValue;

                long rawValue;
                switch (roundedBits.TripleShift(3))
                {
                    case 1:
                        rawValue = @in.ReadByte();
                        break;

                    case 2:
                        rawValue = @in.ReadInt16();
                        break;

                    case 3:
                        rawValue = ((long)@in.ReadInt16() << 8) | (@in.ReadByte() & 0xFFL);
                        break;

                    case 4:
                        rawValue = @in.ReadInt32();
                        break;

                    case 5:
                        rawValue = ((long)@in.ReadInt32() << 8) | (@in.ReadByte() & 0xFFL);
                        break;

                    case 6:
                        rawValue = ((long)@in.ReadInt32() << 16) | (@in.ReadInt16() & 0xFFFFL);
                        break;

                    case 7:
                        rawValue = ((long)@in.ReadInt32() << 24) | ((@in.ReadInt16() & 0xFFFFL) << 8) | (@in.ReadByte() & 0xFFL);
                        break;

                    case 8:
                        rawValue = @in.ReadInt64();
                        break;

                    case 9:
                        // We must be very careful not to shift out relevant bits. So we account for right shift
                        // we would normally do on return here, and reset it.
                        rawValue = (@in.ReadInt64() << (8 - shiftRightBits)) | (((uint)(@in.ReadByte() & 0xFFL) >> shiftRightBits));
                        shiftRightBits = 0;
                        break;

                    default:
                        throw AssertionError.Create("bitsPerValue too large: " + m_bitsPerValue);
                }
                return (rawValue.TripleShift(shiftRightBits)) & valueMask;
            }
            catch (Exception ioe) when (ioe.IsIOException())
            {
                throw IllegalStateException.Create("failed", ioe);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override long RamBytesUsed()
        {
            return 0;
        }
    }
}