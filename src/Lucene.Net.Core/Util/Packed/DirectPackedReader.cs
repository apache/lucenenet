using System;

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

    internal class DirectPackedReader : PackedInts.ReaderImpl
    {
        private readonly IndexInput @in;
        private readonly long startPointer;
        private readonly long valueMask;

        public DirectPackedReader(int bitsPerValue, int valueCount, IndexInput @in)
            : base(valueCount, bitsPerValue)
        {
            this.@in = @in;

            startPointer = @in.FilePointer;
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
            long elementPos = (long)((ulong)majorBitPos >> 3);
            try
            {
                @in.Seek(startPointer + elementPos);

                int bitPos = (int)(majorBitPos & 7);
                // round up bits to a multiple of 8 to find total bytes needed to read
                int roundedBits = ((bitPos + m_bitsPerValue + 7) & ~7);
                // the number of extra bits read at the end to shift out
                int shiftRightBits = roundedBits - bitPos - m_bitsPerValue;

                long rawValue;
                switch ((int)((uint)roundedBits >> 3))
                {
                    case 1:
                        rawValue = @in.ReadByte();
                        break;

                    case 2:
                        rawValue = @in.ReadShort();
                        break;

                    case 3:
                        rawValue = ((long)@in.ReadShort() << 8) | (@in.ReadByte() & 0xFFL);
                        break;

                    case 4:
                        rawValue = @in.ReadInt();
                        break;

                    case 5:
                        rawValue = ((long)@in.ReadInt() << 8) | (@in.ReadByte() & 0xFFL);
                        break;

                    case 6:
                        rawValue = ((long)@in.ReadInt() << 16) | (@in.ReadShort() & 0xFFFFL);
                        break;

                    case 7:
                        rawValue = ((long)@in.ReadInt() << 24) | ((@in.ReadShort() & 0xFFFFL) << 8) | (@in.ReadByte() & 0xFFL);
                        break;

                    case 8:
                        rawValue = @in.ReadLong();
                        break;

                    case 9:
                        // We must be very careful not to shift out relevant bits. So we account for right shift
                        // we would normally do on return here, and reset it.
                        rawValue = (@in.ReadLong() << (8 - shiftRightBits)) | ((int)((uint)(@in.ReadByte() & 0xFFL) >> shiftRightBits));
                        shiftRightBits = 0;
                        break;

                    default:
                        throw new InvalidOperationException("bitsPerValue too large: " + m_bitsPerValue);
                }
                return ((long)((ulong)rawValue >> shiftRightBits)) & valueMask;
            }
            catch (System.IO.IOException ioe)
            {
                throw new InvalidOperationException("failed", ioe);
            }
        }

        public override long RamBytesUsed()
        {
            return 0;
        }
    }
}