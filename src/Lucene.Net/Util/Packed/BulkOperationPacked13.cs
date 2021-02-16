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
    internal sealed class BulkOperationPacked13 : BulkOperationPacked
    {
        public BulkOperationPacked13()
            : base(13)
        {
        }

        public override void Decode(long[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block0 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(block0.TripleShift(51));
                values[valuesOffset++] = (int)((block0.TripleShift(38)) & 8191L);
                values[valuesOffset++] = (int)((block0.TripleShift(25)) & 8191L);
                values[valuesOffset++] = (int)((block0.TripleShift(12)) & 8191L);
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block0 & 4095L) << 1) | (block1.TripleShift(63)));
                values[valuesOffset++] = (int)((block1.TripleShift(50)) & 8191L);
                values[valuesOffset++] = (int)((block1.TripleShift(37)) & 8191L);
                values[valuesOffset++] = (int)((block1.TripleShift(24)) & 8191L);
                values[valuesOffset++] = (int)((block1.TripleShift(11)) & 8191L);
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block1 & 2047L) << 2) | (block2.TripleShift(62)));
                values[valuesOffset++] = (int)((block2.TripleShift(49)) & 8191L);
                values[valuesOffset++] = (int)((block2.TripleShift(36)) & 8191L);
                values[valuesOffset++] = (int)((block2.TripleShift(23)) & 8191L);
                values[valuesOffset++] = (int)((block2.TripleShift(10)) & 8191L);
                long block3 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block2 & 1023L) << 3) | (block3.TripleShift(61)));
                values[valuesOffset++] = (int)((block3.TripleShift(48)) & 8191L);
                values[valuesOffset++] = (int)((block3.TripleShift(35)) & 8191L);
                values[valuesOffset++] = (int)((block3.TripleShift(22)) & 8191L);
                values[valuesOffset++] = (int)((block3.TripleShift(9)) & 8191L);
                long block4 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block3 & 511L) << 4) | (block4.TripleShift(60)));
                values[valuesOffset++] = (int)((block4.TripleShift(47)) & 8191L);
                values[valuesOffset++] = (int)((block4.TripleShift(34)) & 8191L);
                values[valuesOffset++] = (int)((block4.TripleShift(21)) & 8191L);
                values[valuesOffset++] = (int)((block4.TripleShift(8)) & 8191L);
                long block5 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block4 & 255L) << 5) | (block5.TripleShift(59)));
                values[valuesOffset++] = (int)((block5.TripleShift(46)) & 8191L);
                values[valuesOffset++] = (int)((block5.TripleShift(33)) & 8191L);
                values[valuesOffset++] = (int)((block5.TripleShift(20)) & 8191L);
                values[valuesOffset++] = (int)((block5.TripleShift(7)) & 8191L);
                long block6 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block5 & 127L) << 6) | (block6.TripleShift(58)));
                values[valuesOffset++] = (int)((block6.TripleShift(45)) & 8191L);
                values[valuesOffset++] = (int)((block6.TripleShift(32)) & 8191L);
                values[valuesOffset++] = (int)((block6.TripleShift(19)) & 8191L);
                values[valuesOffset++] = (int)((block6.TripleShift(6)) & 8191L);
                long block7 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block6 & 63L) << 7) | (block7.TripleShift(57)));
                values[valuesOffset++] = (int)((block7.TripleShift(44)) & 8191L);
                values[valuesOffset++] = (int)((block7.TripleShift(31)) & 8191L);
                values[valuesOffset++] = (int)((block7.TripleShift(18)) & 8191L);
                values[valuesOffset++] = (int)((block7.TripleShift(5)) & 8191L);
                long block8 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block7 & 31L) << 8) | (block8.TripleShift(56)));
                values[valuesOffset++] = (int)((block8.TripleShift(43)) & 8191L);
                values[valuesOffset++] = (int)((block8.TripleShift(30)) & 8191L);
                values[valuesOffset++] = (int)((block8.TripleShift(17)) & 8191L);
                values[valuesOffset++] = (int)((block8.TripleShift(4)) & 8191L);
                long block9 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block8 & 15L) << 9) | (block9.TripleShift(55)));
                values[valuesOffset++] = (int)((block9.TripleShift(42)) & 8191L);
                values[valuesOffset++] = (int)((block9.TripleShift(29)) & 8191L);
                values[valuesOffset++] = (int)((block9.TripleShift(16)) & 8191L);
                values[valuesOffset++] = (int)((block9.TripleShift(3)) & 8191L);
                long block10 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block9 & 7L) << 10) | (block10.TripleShift(54)));
                values[valuesOffset++] = (int)((block10.TripleShift(41)) & 8191L);
                values[valuesOffset++] = (int)((block10.TripleShift(28)) & 8191L);
                values[valuesOffset++] = (int)((block10.TripleShift(15)) & 8191L);
                values[valuesOffset++] = (int)((block10.TripleShift(2)) & 8191L);
                long block11 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block10 & 3L) << 11) | (block11.TripleShift(53)));
                values[valuesOffset++] = (int)((block11.TripleShift(40)) & 8191L);
                values[valuesOffset++] = (int)((block11.TripleShift(27)) & 8191L);
                values[valuesOffset++] = (int)((block11.TripleShift(14)) & 8191L);
                values[valuesOffset++] = (int)((block11.TripleShift(1)) & 8191L);
                long block12 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block11 & 1L) << 12) | (block12.TripleShift(52)));
                values[valuesOffset++] = (int)((block12.TripleShift(39)) & 8191L);
                values[valuesOffset++] = (int)((block12.TripleShift(26)) & 8191L);
                values[valuesOffset++] = (int)((block12.TripleShift(13)) & 8191L);
                values[valuesOffset++] = (int)(block12 & 8191L);
            }
        }

        public override void Decode(byte[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                int byte0 = blocks[blocksOffset++] & 0xFF;
                int byte1 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = (byte0 << 5) | (byte1.TripleShift(3));
                int byte2 = blocks[blocksOffset++] & 0xFF;
                int byte3 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte1 & 7) << 10) | (byte2 << 2) | (byte3.TripleShift(6));
                int byte4 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte3 & 63) << 7) | (byte4.TripleShift(1));
                int byte5 = blocks[blocksOffset++] & 0xFF;
                int byte6 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte4 & 1) << 12) | (byte5 << 4) | (byte6.TripleShift(4));
                int byte7 = blocks[blocksOffset++] & 0xFF;
                int byte8 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte6 & 15) << 9) | (byte7 << 1) | (byte8.TripleShift(7));
                int byte9 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte8 & 127) << 6) | (byte9.TripleShift(2));
                int byte10 = blocks[blocksOffset++] & 0xFF;
                int byte11 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte9 & 3) << 11) | (byte10 << 3) | (byte11.TripleShift(5));
                int byte12 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte11 & 31) << 8) | byte12;
            }
        }

        public override void Decode(long[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block0 = blocks[blocksOffset++];
                values[valuesOffset++] = block0.TripleShift(51);
                values[valuesOffset++] = (block0.TripleShift(38)) & 8191L;
                values[valuesOffset++] = (block0.TripleShift(25)) & 8191L;
                values[valuesOffset++] = (block0.TripleShift(12)) & 8191L;
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block0 & 4095L) << 1) | (block1.TripleShift(63));
                values[valuesOffset++] = (block1.TripleShift(50)) & 8191L;
                values[valuesOffset++] = (block1.TripleShift(37)) & 8191L;
                values[valuesOffset++] = (block1.TripleShift(24)) & 8191L;
                values[valuesOffset++] = (block1.TripleShift(11)) & 8191L;
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block1 & 2047L) << 2) | (block2.TripleShift(62));
                values[valuesOffset++] = (block2.TripleShift(49)) & 8191L;
                values[valuesOffset++] = (block2.TripleShift(36)) & 8191L;
                values[valuesOffset++] = (block2.TripleShift(23)) & 8191L;
                values[valuesOffset++] = (block2.TripleShift(10)) & 8191L;
                long block3 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block2 & 1023L) << 3) | (block3.TripleShift(61));
                values[valuesOffset++] = (block3.TripleShift(48)) & 8191L;
                values[valuesOffset++] = (block3.TripleShift(35)) & 8191L;
                values[valuesOffset++] = (block3.TripleShift(22)) & 8191L;
                values[valuesOffset++] = (block3.TripleShift(9)) & 8191L;
                long block4 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block3 & 511L) << 4) | (block4.TripleShift(60));
                values[valuesOffset++] = (block4.TripleShift(47)) & 8191L;
                values[valuesOffset++] = (block4.TripleShift(34)) & 8191L;
                values[valuesOffset++] = (block4.TripleShift(21)) & 8191L;
                values[valuesOffset++] = (block4.TripleShift(8)) & 8191L;
                long block5 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block4 & 255L) << 5) | (block5.TripleShift(59));
                values[valuesOffset++] = (block5.TripleShift(46)) & 8191L;
                values[valuesOffset++] = (block5.TripleShift(33)) & 8191L;
                values[valuesOffset++] = (block5.TripleShift(20)) & 8191L;
                values[valuesOffset++] = (block5.TripleShift(7)) & 8191L;
                long block6 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block5 & 127L) << 6) | (block6.TripleShift(58));
                values[valuesOffset++] = (block6.TripleShift(45)) & 8191L;
                values[valuesOffset++] = (block6.TripleShift(32)) & 8191L;
                values[valuesOffset++] = (block6.TripleShift(19)) & 8191L;
                values[valuesOffset++] = (block6.TripleShift(6)) & 8191L;
                long block7 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block6 & 63L) << 7) | (block7.TripleShift(57));
                values[valuesOffset++] = (block7.TripleShift(44)) & 8191L;
                values[valuesOffset++] = (block7.TripleShift(31)) & 8191L;
                values[valuesOffset++] = (block7.TripleShift(18)) & 8191L;
                values[valuesOffset++] = (block7.TripleShift(5)) & 8191L;
                long block8 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block7 & 31L) << 8) | (block8.TripleShift(56));
                values[valuesOffset++] = (block8.TripleShift(43)) & 8191L;
                values[valuesOffset++] = (block8.TripleShift(30)) & 8191L;
                values[valuesOffset++] = (block8.TripleShift(17)) & 8191L;
                values[valuesOffset++] = (block8.TripleShift(4)) & 8191L;
                long block9 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block8 & 15L) << 9) | (block9.TripleShift(55));
                values[valuesOffset++] = (block9.TripleShift(42)) & 8191L;
                values[valuesOffset++] = (block9.TripleShift(29)) & 8191L;
                values[valuesOffset++] = (block9.TripleShift(16)) & 8191L;
                values[valuesOffset++] = (block9.TripleShift(3)) & 8191L;
                long block10 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block9 & 7L) << 10) | (block10.TripleShift(54));
                values[valuesOffset++] = (block10.TripleShift(41)) & 8191L;
                values[valuesOffset++] = (block10.TripleShift(28)) & 8191L;
                values[valuesOffset++] = (block10.TripleShift(15)) & 8191L;
                values[valuesOffset++] = (block10.TripleShift(2)) & 8191L;
                long block11 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block10 & 3L) << 11) | (block11.TripleShift(53));
                values[valuesOffset++] = (block11.TripleShift(40)) & 8191L;
                values[valuesOffset++] = (block11.TripleShift(27)) & 8191L;
                values[valuesOffset++] = (block11.TripleShift(14)) & 8191L;
                values[valuesOffset++] = (block11.TripleShift(1)) & 8191L;
                long block12 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block11 & 1L) << 12) | (block12.TripleShift(52));
                values[valuesOffset++] = (block12.TripleShift(39)) & 8191L;
                values[valuesOffset++] = (block12.TripleShift(26)) & 8191L;
                values[valuesOffset++] = (block12.TripleShift(13)) & 8191L;
                values[valuesOffset++] = block12 & 8191L;
            }
        }

        public override void Decode(byte[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long byte0 = blocks[blocksOffset++] & 0xFF;
                long byte1 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = (byte0 << 5) | (byte1.TripleShift(3));
                long byte2 = blocks[blocksOffset++] & 0xFF;
                long byte3 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte1 & 7) << 10) | (byte2 << 2) | (byte3.TripleShift(6));
                long byte4 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte3 & 63) << 7) | (byte4.TripleShift(1));
                long byte5 = blocks[blocksOffset++] & 0xFF;
                long byte6 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte4 & 1) << 12) | (byte5 << 4) | (byte6.TripleShift(4));
                long byte7 = blocks[blocksOffset++] & 0xFF;
                long byte8 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte6 & 15) << 9) | (byte7 << 1) | (byte8.TripleShift(7));
                long byte9 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte8 & 127) << 6) | (byte9.TripleShift(2));
                long byte10 = blocks[blocksOffset++] & 0xFF;
                long byte11 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte9 & 3) << 11) | (byte10 << 3) | (byte11.TripleShift(5));
                long byte12 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte11 & 31) << 8) | byte12;
            }
        }
    }
}