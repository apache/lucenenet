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
    internal sealed class BulkOperationPacked11 : BulkOperationPacked
    {
        public BulkOperationPacked11()
            : base(11)
        {
        }

        public override void Decode(long[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block0 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(block0.TripleShift(53));
                values[valuesOffset++] = (int)((block0.TripleShift(42)) & 2047L);
                values[valuesOffset++] = (int)((block0.TripleShift(31)) & 2047L);
                values[valuesOffset++] = (int)((block0.TripleShift(20)) & 2047L);
                values[valuesOffset++] = (int)((block0.TripleShift(9)) & 2047L);
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block0 & 511L) << 2) | (block1.TripleShift(62)));
                values[valuesOffset++] = (int)((block1.TripleShift(51)) & 2047L);
                values[valuesOffset++] = (int)((block1.TripleShift(40)) & 2047L);
                values[valuesOffset++] = (int)((block1.TripleShift(29)) & 2047L);
                values[valuesOffset++] = (int)((block1.TripleShift(18)) & 2047L);
                values[valuesOffset++] = (int)((block1.TripleShift(7)) & 2047L);
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block1 & 127L) << 4) | (block2.TripleShift(60)));
                values[valuesOffset++] = (int)((block2.TripleShift(49)) & 2047L);
                values[valuesOffset++] = (int)((block2.TripleShift(38)) & 2047L);
                values[valuesOffset++] = (int)((block2.TripleShift(27)) & 2047L);
                values[valuesOffset++] = (int)((block2.TripleShift(16)) & 2047L);
                values[valuesOffset++] = (int)((block2.TripleShift(5)) & 2047L);
                long block3 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block2 & 31L) << 6) | (block3.TripleShift(58)));
                values[valuesOffset++] = (int)((block3.TripleShift(47)) & 2047L);
                values[valuesOffset++] = (int)((block3.TripleShift(36)) & 2047L);
                values[valuesOffset++] = (int)((block3.TripleShift(25)) & 2047L);
                values[valuesOffset++] = (int)((block3.TripleShift(14)) & 2047L);
                values[valuesOffset++] = (int)((block3.TripleShift(3)) & 2047L);
                long block4 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block3 & 7L) << 8) | (block4.TripleShift(56)));
                values[valuesOffset++] = (int)((block4.TripleShift(45)) & 2047L);
                values[valuesOffset++] = (int)((block4.TripleShift(34)) & 2047L);
                values[valuesOffset++] = (int)((block4.TripleShift(23)) & 2047L);
                values[valuesOffset++] = (int)((block4.TripleShift(12)) & 2047L);
                values[valuesOffset++] = (int)((block4.TripleShift(1)) & 2047L);
                long block5 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block4 & 1L) << 10) | (block5.TripleShift(54)));
                values[valuesOffset++] = (int)((block5.TripleShift(43)) & 2047L);
                values[valuesOffset++] = (int)((block5.TripleShift(32)) & 2047L);
                values[valuesOffset++] = (int)((block5.TripleShift(21)) & 2047L);
                values[valuesOffset++] = (int)((block5.TripleShift(10)) & 2047L);
                long block6 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block5 & 1023L) << 1) | (block6.TripleShift(63)));
                values[valuesOffset++] = (int)((block6.TripleShift(52)) & 2047L);
                values[valuesOffset++] = (int)((block6.TripleShift(41)) & 2047L);
                values[valuesOffset++] = (int)((block6.TripleShift(30)) & 2047L);
                values[valuesOffset++] = (int)((block6.TripleShift(19)) & 2047L);
                values[valuesOffset++] = (int)((block6.TripleShift(8)) & 2047L);
                long block7 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block6 & 255L) << 3) | (block7.TripleShift(61)));
                values[valuesOffset++] = (int)((block7.TripleShift(50)) & 2047L);
                values[valuesOffset++] = (int)((block7.TripleShift(39)) & 2047L);
                values[valuesOffset++] = (int)((block7.TripleShift(28)) & 2047L);
                values[valuesOffset++] = (int)((block7.TripleShift(17)) & 2047L);
                values[valuesOffset++] = (int)((block7.TripleShift(6)) & 2047L);
                long block8 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block7 & 63L) << 5) | (block8.TripleShift(59)));
                values[valuesOffset++] = (int)((block8.TripleShift(48)) & 2047L);
                values[valuesOffset++] = (int)((block8.TripleShift(37)) & 2047L);
                values[valuesOffset++] = (int)((block8.TripleShift(26)) & 2047L);
                values[valuesOffset++] = (int)((block8.TripleShift(15)) & 2047L);
                values[valuesOffset++] = (int)((block8.TripleShift(4)) & 2047L);
                long block9 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block8 & 15L) << 7) | (block9.TripleShift(57)));
                values[valuesOffset++] = (int)((block9.TripleShift(46)) & 2047L);
                values[valuesOffset++] = (int)((block9.TripleShift(35)) & 2047L);
                values[valuesOffset++] = (int)((block9.TripleShift(24)) & 2047L);
                values[valuesOffset++] = (int)((block9.TripleShift(13)) & 2047L);
                values[valuesOffset++] = (int)((block9.TripleShift(2)) & 2047L);
                long block10 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block9 & 3L) << 9) | (block10.TripleShift(55)));
                values[valuesOffset++] = (int)((block10.TripleShift(44)) & 2047L);
                values[valuesOffset++] = (int)((block10.TripleShift(33)) & 2047L);
                values[valuesOffset++] = (int)((block10.TripleShift(22)) & 2047L);
                values[valuesOffset++] = (int)((block10.TripleShift(11)) & 2047L);
                values[valuesOffset++] = (int)(block10 & 2047L);
            }
        }

        public override void Decode(byte[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                int byte0 = blocks[blocksOffset++] & 0xFF;
                int byte1 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = (byte0 << 3) | (byte1.TripleShift(5));
                int byte2 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte1 & 31) << 6) | (byte2.TripleShift(2));
                int byte3 = blocks[blocksOffset++] & 0xFF;
                int byte4 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte2 & 3) << 9) | (byte3 << 1) | (byte4.TripleShift(7));
                int byte5 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte4 & 127) << 4) | (byte5.TripleShift(4));
                int byte6 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte5 & 15) << 7) | (byte6.TripleShift(1));
                int byte7 = blocks[blocksOffset++] & 0xFF;
                int byte8 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte6 & 1) << 10) | (byte7 << 2) | (byte8.TripleShift(6));
                int byte9 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte8 & 63) << 5) | (byte9.TripleShift(3));
                int byte10 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte9 & 7) << 8) | byte10;
            }
        }

        public override void Decode(long[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block0 = blocks[blocksOffset++];
                values[valuesOffset++] = block0.TripleShift(53);
                values[valuesOffset++] = (block0.TripleShift(42)) & 2047L;
                values[valuesOffset++] = (block0.TripleShift(31)) & 2047L;
                values[valuesOffset++] = (block0.TripleShift(20)) & 2047L;
                values[valuesOffset++] = (block0.TripleShift(9)) & 2047L;
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block0 & 511L) << 2) | (block1.TripleShift(62));
                values[valuesOffset++] = (block1.TripleShift(51)) & 2047L;
                values[valuesOffset++] = (block1.TripleShift(40)) & 2047L;
                values[valuesOffset++] = (block1.TripleShift(29)) & 2047L;
                values[valuesOffset++] = (block1.TripleShift(18)) & 2047L;
                values[valuesOffset++] = (block1.TripleShift(7)) & 2047L;
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block1 & 127L) << 4) | (block2.TripleShift(60));
                values[valuesOffset++] = (block2.TripleShift(49)) & 2047L;
                values[valuesOffset++] = (block2.TripleShift(38)) & 2047L;
                values[valuesOffset++] = (block2.TripleShift(27)) & 2047L;
                values[valuesOffset++] = (block2.TripleShift(16)) & 2047L;
                values[valuesOffset++] = (block2.TripleShift(5)) & 2047L;
                long block3 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block2 & 31L) << 6) | (block3.TripleShift(58));
                values[valuesOffset++] = (block3.TripleShift(47)) & 2047L;
                values[valuesOffset++] = (block3.TripleShift(36)) & 2047L;
                values[valuesOffset++] = (block3.TripleShift(25)) & 2047L;
                values[valuesOffset++] = (block3.TripleShift(14)) & 2047L;
                values[valuesOffset++] = (block3.TripleShift(3)) & 2047L;
                long block4 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block3 & 7L) << 8) | (block4.TripleShift(56));
                values[valuesOffset++] = (block4.TripleShift(45)) & 2047L;
                values[valuesOffset++] = (block4.TripleShift(34)) & 2047L;
                values[valuesOffset++] = (block4.TripleShift(23)) & 2047L;
                values[valuesOffset++] = (block4.TripleShift(12)) & 2047L;
                values[valuesOffset++] = (block4.TripleShift(1)) & 2047L;
                long block5 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block4 & 1L) << 10) | (block5.TripleShift(54));
                values[valuesOffset++] = (block5.TripleShift(43)) & 2047L;
                values[valuesOffset++] = (block5.TripleShift(32)) & 2047L;
                values[valuesOffset++] = (block5.TripleShift(21)) & 2047L;
                values[valuesOffset++] = (block5.TripleShift(10)) & 2047L;
                long block6 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block5 & 1023L) << 1) | (block6.TripleShift(63));
                values[valuesOffset++] = (block6.TripleShift(52)) & 2047L;
                values[valuesOffset++] = (block6.TripleShift(41)) & 2047L;
                values[valuesOffset++] = (block6.TripleShift(30)) & 2047L;
                values[valuesOffset++] = (block6.TripleShift(19)) & 2047L;
                values[valuesOffset++] = (block6.TripleShift(8)) & 2047L;
                long block7 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block6 & 255L) << 3) | (block7.TripleShift(61));
                values[valuesOffset++] = (block7.TripleShift(50)) & 2047L;
                values[valuesOffset++] = (block7.TripleShift(39)) & 2047L;
                values[valuesOffset++] = (block7.TripleShift(28)) & 2047L;
                values[valuesOffset++] = (block7.TripleShift(17)) & 2047L;
                values[valuesOffset++] = (block7.TripleShift(6)) & 2047L;
                long block8 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block7 & 63L) << 5) | (block8.TripleShift(59));
                values[valuesOffset++] = (block8.TripleShift(48)) & 2047L;
                values[valuesOffset++] = (block8.TripleShift(37)) & 2047L;
                values[valuesOffset++] = (block8.TripleShift(26)) & 2047L;
                values[valuesOffset++] = (block8.TripleShift(15)) & 2047L;
                values[valuesOffset++] = (block8.TripleShift(4)) & 2047L;
                long block9 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block8 & 15L) << 7) | (block9.TripleShift(57));
                values[valuesOffset++] = (block9.TripleShift(46)) & 2047L;
                values[valuesOffset++] = (block9.TripleShift(35)) & 2047L;
                values[valuesOffset++] = (block9.TripleShift(24)) & 2047L;
                values[valuesOffset++] = (block9.TripleShift(13)) & 2047L;
                values[valuesOffset++] = (block9.TripleShift(2)) & 2047L;
                long block10 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block9 & 3L) << 9) | (block10.TripleShift(55));
                values[valuesOffset++] = (block10.TripleShift(44)) & 2047L;
                values[valuesOffset++] = (block10.TripleShift(33)) & 2047L;
                values[valuesOffset++] = (block10.TripleShift(22)) & 2047L;
                values[valuesOffset++] = (block10.TripleShift(11)) & 2047L;
                values[valuesOffset++] = block10 & 2047L;
            }
        }

        public override void Decode(byte[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long byte0 = blocks[blocksOffset++] & 0xFF;
                long byte1 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = (byte0 << 3) | (byte1.TripleShift(5));
                long byte2 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte1 & 31) << 6) | (byte2.TripleShift(2));
                long byte3 = blocks[blocksOffset++] & 0xFF;
                long byte4 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte2 & 3) << 9) | (byte3 << 1) | (byte4.TripleShift(7));
                long byte5 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte4 & 127) << 4) | (byte5.TripleShift(4));
                long byte6 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte5 & 15) << 7) | (byte6.TripleShift(1));
                long byte7 = blocks[blocksOffset++] & 0xFF;
                long byte8 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte6 & 1) << 10) | (byte7 << 2) | (byte8.TripleShift(6));
                long byte9 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte8 & 63) << 5) | (byte9.TripleShift(3));
                long byte10 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte9 & 7) << 8) | byte10;
            }
        }
    }
}