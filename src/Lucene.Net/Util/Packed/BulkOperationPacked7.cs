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
    internal sealed class BulkOperationPacked7 : BulkOperationPacked
    {
        public BulkOperationPacked7()
            : base(7)
        {
        }

        public override void Decode(long[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block0 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(block0.TripleShift(57));
                values[valuesOffset++] = (int)((block0.TripleShift(50)) & 127L);
                values[valuesOffset++] = (int)((block0.TripleShift(43)) & 127L);
                values[valuesOffset++] = (int)((block0.TripleShift(36)) & 127L);
                values[valuesOffset++] = (int)((block0.TripleShift(29)) & 127L);
                values[valuesOffset++] = (int)((block0.TripleShift(22)) & 127L);
                values[valuesOffset++] = (int)((block0.TripleShift(15)) & 127L);
                values[valuesOffset++] = (int)((block0.TripleShift(8)) & 127L);
                values[valuesOffset++] = (int)((block0.TripleShift(1)) & 127L);
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block0 & 1L) << 6) | (block1.TripleShift(58)));
                values[valuesOffset++] = (int)((block1.TripleShift(51)) & 127L);
                values[valuesOffset++] = (int)((block1.TripleShift(44)) & 127L);
                values[valuesOffset++] = (int)((block1.TripleShift(37)) & 127L);
                values[valuesOffset++] = (int)((block1.TripleShift(30)) & 127L);
                values[valuesOffset++] = (int)((block1.TripleShift(23)) & 127L);
                values[valuesOffset++] = (int)((block1.TripleShift(16)) & 127L);
                values[valuesOffset++] = (int)((block1.TripleShift(9)) & 127L);
                values[valuesOffset++] = (int)((block1.TripleShift(2)) & 127L);
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block1 & 3L) << 5) | (block2.TripleShift(59)));
                values[valuesOffset++] = (int)((block2.TripleShift(52)) & 127L);
                values[valuesOffset++] = (int)((block2.TripleShift(45)) & 127L);
                values[valuesOffset++] = (int)((block2.TripleShift(38)) & 127L);
                values[valuesOffset++] = (int)((block2.TripleShift(31)) & 127L);
                values[valuesOffset++] = (int)((block2.TripleShift(24)) & 127L);
                values[valuesOffset++] = (int)((block2.TripleShift(17)) & 127L);
                values[valuesOffset++] = (int)((block2.TripleShift(10)) & 127L);
                values[valuesOffset++] = (int)((block2.TripleShift(3)) & 127L);
                long block3 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block2 & 7L) << 4) | (block3.TripleShift(60)));
                values[valuesOffset++] = (int)((block3.TripleShift(53)) & 127L);
                values[valuesOffset++] = (int)((block3.TripleShift(46)) & 127L);
                values[valuesOffset++] = (int)((block3.TripleShift(39)) & 127L);
                values[valuesOffset++] = (int)((block3.TripleShift(32)) & 127L);
                values[valuesOffset++] = (int)((block3.TripleShift(25)) & 127L);
                values[valuesOffset++] = (int)((block3.TripleShift(18)) & 127L);
                values[valuesOffset++] = (int)((block3.TripleShift(11)) & 127L);
                values[valuesOffset++] = (int)((block3.TripleShift(4)) & 127L);
                long block4 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block3 & 15L) << 3) | (block4.TripleShift(61)));
                values[valuesOffset++] = (int)((block4.TripleShift(54)) & 127L);
                values[valuesOffset++] = (int)((block4.TripleShift(47)) & 127L);
                values[valuesOffset++] = (int)((block4.TripleShift(40)) & 127L);
                values[valuesOffset++] = (int)((block4.TripleShift(33)) & 127L);
                values[valuesOffset++] = (int)((block4.TripleShift(26)) & 127L);
                values[valuesOffset++] = (int)((block4.TripleShift(19)) & 127L);
                values[valuesOffset++] = (int)((block4.TripleShift(12)) & 127L);
                values[valuesOffset++] = (int)((block4.TripleShift(5)) & 127L);
                long block5 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block4 & 31L) << 2) | (block5.TripleShift(62)));
                values[valuesOffset++] = (int)((block5.TripleShift(55)) & 127L);
                values[valuesOffset++] = (int)((block5.TripleShift(48)) & 127L);
                values[valuesOffset++] = (int)((block5.TripleShift(41)) & 127L);
                values[valuesOffset++] = (int)((block5.TripleShift(34)) & 127L);
                values[valuesOffset++] = (int)((block5.TripleShift(27)) & 127L);
                values[valuesOffset++] = (int)((block5.TripleShift(20)) & 127L);
                values[valuesOffset++] = (int)((block5.TripleShift(13)) & 127L);
                values[valuesOffset++] = (int)((block5.TripleShift(6)) & 127L);
                long block6 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block5 & 63L) << 1) | (block6.TripleShift(63)));
                values[valuesOffset++] = (int)((block6.TripleShift(56)) & 127L);
                values[valuesOffset++] = (int)((block6.TripleShift(49)) & 127L);
                values[valuesOffset++] = (int)((block6.TripleShift(42)) & 127L);
                values[valuesOffset++] = (int)((block6.TripleShift(35)) & 127L);
                values[valuesOffset++] = (int)((block6.TripleShift(28)) & 127L);
                values[valuesOffset++] = (int)((block6.TripleShift(21)) & 127L);
                values[valuesOffset++] = (int)((block6.TripleShift(14)) & 127L);
                values[valuesOffset++] = (int)((block6.TripleShift(7)) & 127L);
                values[valuesOffset++] = (int)(block6 & 127L);
            }
        }

        public override void Decode(byte[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                int byte0 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = byte0.TripleShift(1);
                int byte1 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte0 & 1) << 6) | (byte1.TripleShift(2));
                int byte2 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte1 & 3) << 5) | (byte2.TripleShift(3));
                int byte3 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte2 & 7) << 4) | (byte3.TripleShift(4));
                int byte4 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte3 & 15) << 3) | (byte4.TripleShift(5));
                int byte5 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte4 & 31) << 2) | (byte5.TripleShift(6));
                int byte6 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte5 & 63) << 1) | (byte6.TripleShift(7));
                values[valuesOffset++] = byte6 & 127;
            }
        }

        public override void Decode(long[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block0 = blocks[blocksOffset++];
                values[valuesOffset++] = block0.TripleShift(57);
                values[valuesOffset++] = (block0.TripleShift(50)) & 127L;
                values[valuesOffset++] = (block0.TripleShift(43)) & 127L;
                values[valuesOffset++] = (block0.TripleShift(36)) & 127L;
                values[valuesOffset++] = (block0.TripleShift(29)) & 127L;
                values[valuesOffset++] = (block0.TripleShift(22)) & 127L;
                values[valuesOffset++] = (block0.TripleShift(15)) & 127L;
                values[valuesOffset++] = (block0.TripleShift(8)) & 127L;
                values[valuesOffset++] = (block0.TripleShift(1)) & 127L;
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block0 & 1L) << 6) | (block1.TripleShift(58));
                values[valuesOffset++] = (block1.TripleShift(51)) & 127L;
                values[valuesOffset++] = (block1.TripleShift(44)) & 127L;
                values[valuesOffset++] = (block1.TripleShift(37)) & 127L;
                values[valuesOffset++] = (block1.TripleShift(30)) & 127L;
                values[valuesOffset++] = (block1.TripleShift(23)) & 127L;
                values[valuesOffset++] = (block1.TripleShift(16)) & 127L;
                values[valuesOffset++] = (block1.TripleShift(9)) & 127L;
                values[valuesOffset++] = (block1.TripleShift(2)) & 127L;
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block1 & 3L) << 5) | (block2.TripleShift(59));
                values[valuesOffset++] = (block2.TripleShift(52)) & 127L;
                values[valuesOffset++] = (block2.TripleShift(45)) & 127L;
                values[valuesOffset++] = (block2.TripleShift(38)) & 127L;
                values[valuesOffset++] = (block2.TripleShift(31)) & 127L;
                values[valuesOffset++] = (block2.TripleShift(24)) & 127L;
                values[valuesOffset++] = (block2.TripleShift(17)) & 127L;
                values[valuesOffset++] = (block2.TripleShift(10)) & 127L;
                values[valuesOffset++] = (block2.TripleShift(3)) & 127L;
                long block3 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block2 & 7L) << 4) | (block3.TripleShift(60));
                values[valuesOffset++] = (block3.TripleShift(53)) & 127L;
                values[valuesOffset++] = (block3.TripleShift(46)) & 127L;
                values[valuesOffset++] = (block3.TripleShift(39)) & 127L;
                values[valuesOffset++] = (block3.TripleShift(32)) & 127L;
                values[valuesOffset++] = (block3.TripleShift(25)) & 127L;
                values[valuesOffset++] = (block3.TripleShift(18)) & 127L;
                values[valuesOffset++] = (block3.TripleShift(11)) & 127L;
                values[valuesOffset++] = (block3.TripleShift(4)) & 127L;
                long block4 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block3 & 15L) << 3) | (block4.TripleShift(61));
                values[valuesOffset++] = (block4.TripleShift(54)) & 127L;
                values[valuesOffset++] = (block4.TripleShift(47)) & 127L;
                values[valuesOffset++] = (block4.TripleShift(40)) & 127L;
                values[valuesOffset++] = (block4.TripleShift(33)) & 127L;
                values[valuesOffset++] = (block4.TripleShift(26)) & 127L;
                values[valuesOffset++] = (block4.TripleShift(19)) & 127L;
                values[valuesOffset++] = (block4.TripleShift(12)) & 127L;
                values[valuesOffset++] = (block4.TripleShift(5)) & 127L;
                long block5 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block4 & 31L) << 2) | (block5.TripleShift(62));
                values[valuesOffset++] = (block5.TripleShift(55)) & 127L;
                values[valuesOffset++] = (block5.TripleShift(48)) & 127L;
                values[valuesOffset++] = (block5.TripleShift(41)) & 127L;
                values[valuesOffset++] = (block5.TripleShift(34)) & 127L;
                values[valuesOffset++] = (block5.TripleShift(27)) & 127L;
                values[valuesOffset++] = (block5.TripleShift(20)) & 127L;
                values[valuesOffset++] = (block5.TripleShift(13)) & 127L;
                values[valuesOffset++] = (block5.TripleShift(6)) & 127L;
                long block6 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block5 & 63L) << 1) | (block6.TripleShift(63));
                values[valuesOffset++] = (block6.TripleShift(56)) & 127L;
                values[valuesOffset++] = (block6.TripleShift(49)) & 127L;
                values[valuesOffset++] = (block6.TripleShift(42)) & 127L;
                values[valuesOffset++] = (block6.TripleShift(35)) & 127L;
                values[valuesOffset++] = (block6.TripleShift(28)) & 127L;
                values[valuesOffset++] = (block6.TripleShift(21)) & 127L;
                values[valuesOffset++] = (block6.TripleShift(14)) & 127L;
                values[valuesOffset++] = (block6.TripleShift(7)) & 127L;
                values[valuesOffset++] = block6 & 127L;
            }
        }

        public override void Decode(byte[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long byte0 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = byte0.TripleShift(1);
                long byte1 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte0 & 1) << 6) | (byte1.TripleShift(2));
                long byte2 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte1 & 3) << 5) | (byte2.TripleShift(3));
                long byte3 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte2 & 7) << 4) | (byte3.TripleShift(4));
                long byte4 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte3 & 15) << 3) | (byte4.TripleShift(5));
                long byte5 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte4 & 31) << 2) | (byte5.TripleShift(6));
                long byte6 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte5 & 63) << 1) | (byte6.TripleShift(7));
                values[valuesOffset++] = byte6 & 127;
            }
        }
    }
}