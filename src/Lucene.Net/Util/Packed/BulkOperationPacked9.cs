// this file has been automatically generated, DO NOT EDIT

using J2N.Numerics;

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

    /// <summary>
    /// Efficient sequential read/write of packed integers.
    /// </summary>
    internal sealed class BulkOperationPacked9 : BulkOperationPacked
    {
        public BulkOperationPacked9()
            : base(9)
        {
        }

        public override void Decode(long[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block0 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(block0.TripleShift(55));
                values[valuesOffset++] = (int)((block0.TripleShift(46)) & 511L);
                values[valuesOffset++] = (int)((block0.TripleShift(37)) & 511L);
                values[valuesOffset++] = (int)((block0.TripleShift(28)) & 511L);
                values[valuesOffset++] = (int)((block0.TripleShift(19)) & 511L);
                values[valuesOffset++] = (int)((block0.TripleShift(10)) & 511L);
                values[valuesOffset++] = (int)((block0.TripleShift(1)) & 511L);
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block0 & 1L) << 8) | (block1.TripleShift(56)));
                values[valuesOffset++] = (int)((block1.TripleShift(47)) & 511L);
                values[valuesOffset++] = (int)((block1.TripleShift(38)) & 511L);
                values[valuesOffset++] = (int)((block1.TripleShift(29)) & 511L);
                values[valuesOffset++] = (int)((block1.TripleShift(20)) & 511L);
                values[valuesOffset++] = (int)((block1.TripleShift(11)) & 511L);
                values[valuesOffset++] = (int)((block1.TripleShift(2)) & 511L);
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block1 & 3L) << 7) | (block2.TripleShift(57)));
                values[valuesOffset++] = (int)((block2.TripleShift(48)) & 511L);
                values[valuesOffset++] = (int)((block2.TripleShift(39)) & 511L);
                values[valuesOffset++] = (int)((block2.TripleShift(30)) & 511L);
                values[valuesOffset++] = (int)((block2.TripleShift(21)) & 511L);
                values[valuesOffset++] = (int)((block2.TripleShift(12)) & 511L);
                values[valuesOffset++] = (int)((block2.TripleShift(3)) & 511L);
                long block3 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block2 & 7L) << 6) | (block3.TripleShift(58)));
                values[valuesOffset++] = (int)((block3.TripleShift(49)) & 511L);
                values[valuesOffset++] = (int)((block3.TripleShift(40)) & 511L);
                values[valuesOffset++] = (int)((block3.TripleShift(31)) & 511L);
                values[valuesOffset++] = (int)((block3.TripleShift(22)) & 511L);
                values[valuesOffset++] = (int)((block3.TripleShift(13)) & 511L);
                values[valuesOffset++] = (int)((block3.TripleShift(4)) & 511L);
                long block4 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block3 & 15L) << 5) | (block4.TripleShift(59)));
                values[valuesOffset++] = (int)((block4.TripleShift(50)) & 511L);
                values[valuesOffset++] = (int)((block4.TripleShift(41)) & 511L);
                values[valuesOffset++] = (int)((block4.TripleShift(32)) & 511L);
                values[valuesOffset++] = (int)((block4.TripleShift(23)) & 511L);
                values[valuesOffset++] = (int)((block4.TripleShift(14)) & 511L);
                values[valuesOffset++] = (int)((block4.TripleShift(5)) & 511L);
                long block5 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block4 & 31L) << 4) | (block5.TripleShift(60)));
                values[valuesOffset++] = (int)((block5.TripleShift(51)) & 511L);
                values[valuesOffset++] = (int)((block5.TripleShift(42)) & 511L);
                values[valuesOffset++] = (int)((block5.TripleShift(33)) & 511L);
                values[valuesOffset++] = (int)((block5.TripleShift(24)) & 511L);
                values[valuesOffset++] = (int)((block5.TripleShift(15)) & 511L);
                values[valuesOffset++] = (int)((block5.TripleShift(6)) & 511L);
                long block6 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block5 & 63L) << 3) | (block6.TripleShift(61)));
                values[valuesOffset++] = (int)((block6.TripleShift(52)) & 511L);
                values[valuesOffset++] = (int)((block6.TripleShift(43)) & 511L);
                values[valuesOffset++] = (int)((block6.TripleShift(34)) & 511L);
                values[valuesOffset++] = (int)((block6.TripleShift(25)) & 511L);
                values[valuesOffset++] = (int)((block6.TripleShift(16)) & 511L);
                values[valuesOffset++] = (int)((block6.TripleShift(7)) & 511L);
                long block7 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block6 & 127L) << 2) | (block7.TripleShift(62)));
                values[valuesOffset++] = (int)((block7.TripleShift(53)) & 511L);
                values[valuesOffset++] = (int)((block7.TripleShift(44)) & 511L);
                values[valuesOffset++] = (int)((block7.TripleShift(35)) & 511L);
                values[valuesOffset++] = (int)((block7.TripleShift(26)) & 511L);
                values[valuesOffset++] = (int)((block7.TripleShift(17)) & 511L);
                values[valuesOffset++] = (int)((block7.TripleShift(8)) & 511L);
                long block8 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block7 & 255L) << 1) | (block8.TripleShift(63)));
                values[valuesOffset++] = (int)((block8.TripleShift(54)) & 511L);
                values[valuesOffset++] = (int)((block8.TripleShift(45)) & 511L);
                values[valuesOffset++] = (int)((block8.TripleShift(36)) & 511L);
                values[valuesOffset++] = (int)((block8.TripleShift(27)) & 511L);
                values[valuesOffset++] = (int)((block8.TripleShift(18)) & 511L);
                values[valuesOffset++] = (int)((block8.TripleShift(9)) & 511L);
                values[valuesOffset++] = (int)(block8 & 511L);
            }
        }

        public override void Decode(byte[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                int byte0 = blocks[blocksOffset++] & 0xFF;
                int byte1 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = (byte0 << 1) | (byte1.TripleShift(7));
                int byte2 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte1 & 127) << 2) | (byte2.TripleShift(6));
                int byte3 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte2 & 63) << 3) | (byte3.TripleShift(5));
                int byte4 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte3 & 31) << 4) | (byte4.TripleShift(4));
                int byte5 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte4 & 15) << 5) | (byte5.TripleShift(3));
                int byte6 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte5 & 7) << 6) | (byte6.TripleShift(2));
                int byte7 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte6 & 3) << 7) | (byte7.TripleShift(1));
                int byte8 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte7 & 1) << 8) | byte8;
            }
        }

        public override void Decode(long[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block0 = blocks[blocksOffset++];
                values[valuesOffset++] = (long)((ulong)block0 >> 55);
                values[valuesOffset++] = (block0.TripleShift(46)) & 511L;
                values[valuesOffset++] = (block0.TripleShift(37)) & 511L;
                values[valuesOffset++] = (block0.TripleShift(28)) & 511L;
                values[valuesOffset++] = (block0.TripleShift(19)) & 511L;
                values[valuesOffset++] = (block0.TripleShift(10)) & 511L;
                values[valuesOffset++] = (block0.TripleShift(1)) & 511L;
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block0 & 1L) << 8) | (block1.TripleShift(56));
                values[valuesOffset++] = (block1.TripleShift(47)) & 511L;
                values[valuesOffset++] = (block1.TripleShift(38)) & 511L;
                values[valuesOffset++] = (block1.TripleShift(29)) & 511L;
                values[valuesOffset++] = (block1.TripleShift(20)) & 511L;
                values[valuesOffset++] = (block1.TripleShift(11)) & 511L;
                values[valuesOffset++] = (block1.TripleShift(2)) & 511L;
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block1 & 3L) << 7) | (block2.TripleShift(57));
                values[valuesOffset++] = (block2.TripleShift(48)) & 511L;
                values[valuesOffset++] = (block2.TripleShift(39)) & 511L;
                values[valuesOffset++] = (block2.TripleShift(30)) & 511L;
                values[valuesOffset++] = (block2.TripleShift(21)) & 511L;
                values[valuesOffset++] = (block2.TripleShift(12)) & 511L;
                values[valuesOffset++] = (block2.TripleShift(3)) & 511L;
                long block3 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block2 & 7L) << 6) | (block3.TripleShift(58));
                values[valuesOffset++] = (block3.TripleShift(49)) & 511L;
                values[valuesOffset++] = (block3.TripleShift(40)) & 511L;
                values[valuesOffset++] = (block3.TripleShift(31)) & 511L;
                values[valuesOffset++] = (block3.TripleShift(22)) & 511L;
                values[valuesOffset++] = (block3.TripleShift(13)) & 511L;
                values[valuesOffset++] = (block3.TripleShift(4)) & 511L;
                long block4 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block3 & 15L) << 5) | (block4.TripleShift(59));
                values[valuesOffset++] = (block4.TripleShift(50)) & 511L;
                values[valuesOffset++] = (block4.TripleShift(41)) & 511L;
                values[valuesOffset++] = (block4.TripleShift(32)) & 511L;
                values[valuesOffset++] = (block4.TripleShift(23)) & 511L;
                values[valuesOffset++] = (block4.TripleShift(14)) & 511L;
                values[valuesOffset++] = (block4.TripleShift(5)) & 511L;
                long block5 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block4 & 31L) << 4) | (block5.TripleShift(60));
                values[valuesOffset++] = (block5.TripleShift(51)) & 511L;
                values[valuesOffset++] = (block5.TripleShift(42)) & 511L;
                values[valuesOffset++] = (block5.TripleShift(33)) & 511L;
                values[valuesOffset++] = (block5.TripleShift(24)) & 511L;
                values[valuesOffset++] = (block5.TripleShift(15)) & 511L;
                values[valuesOffset++] = (block5.TripleShift(6)) & 511L;
                long block6 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block5 & 63L) << 3) | (block6.TripleShift(61));
                values[valuesOffset++] = (block6.TripleShift(52)) & 511L;
                values[valuesOffset++] = (block6.TripleShift(43)) & 511L;
                values[valuesOffset++] = (block6.TripleShift(34)) & 511L;
                values[valuesOffset++] = (block6.TripleShift(25)) & 511L;
                values[valuesOffset++] = (block6.TripleShift(16)) & 511L;
                values[valuesOffset++] = (block6.TripleShift(7)) & 511L;
                long block7 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block6 & 127L) << 2) | (block7.TripleShift(62));
                values[valuesOffset++] = (block7.TripleShift(53)) & 511L;
                values[valuesOffset++] = (block7.TripleShift(44)) & 511L;
                values[valuesOffset++] = (block7.TripleShift(35)) & 511L;
                values[valuesOffset++] = (block7.TripleShift(26)) & 511L;
                values[valuesOffset++] = (block7.TripleShift(17)) & 511L;
                values[valuesOffset++] = (block7.TripleShift(8)) & 511L;
                long block8 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block7 & 255L) << 1) | (block8.TripleShift(63));
                values[valuesOffset++] = (block8.TripleShift(54)) & 511L;
                values[valuesOffset++] = (block8.TripleShift(45)) & 511L;
                values[valuesOffset++] = (block8.TripleShift(36)) & 511L;
                values[valuesOffset++] = (block8.TripleShift(27)) & 511L;
                values[valuesOffset++] = (block8.TripleShift(18)) & 511L;
                values[valuesOffset++] = (block8.TripleShift(9)) & 511L;
                values[valuesOffset++] = block8 & 511L;
            }
        }

        public override void Decode(byte[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long byte0 = blocks[blocksOffset++] & 0xFF;
                long byte1 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = (byte0 << 1) | (byte1.TripleShift(7));
                long byte2 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte1 & 127) << 2) | (byte2.TripleShift(6));
                long byte3 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte2 & 63) << 3) | (byte3.TripleShift(5));
                long byte4 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte3 & 31) << 4) | (byte4.TripleShift(4));
                long byte5 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte4 & 15) << 5) | (byte5.TripleShift(3));
                long byte6 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte5 & 7) << 6) | (byte6.TripleShift(2));
                long byte7 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte6 & 3) << 7) | (byte7.TripleShift(1));
                long byte8 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte7 & 1) << 8) | byte8;
            }
        }
    }
}