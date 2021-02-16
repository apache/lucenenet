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
    internal sealed class BulkOperationPacked15 : BulkOperationPacked
    {
        public BulkOperationPacked15()
            : base(15)
        {
        }

        public override void Decode(long[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block0 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(block0.TripleShift(49));
                values[valuesOffset++] = (int)((block0.TripleShift(34)) & 32767L);
                values[valuesOffset++] = (int)((block0.TripleShift(19)) & 32767L);
                values[valuesOffset++] = (int)((block0.TripleShift(4)) & 32767L);
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block0 & 15L) << 11) | (block1.TripleShift(53)));
                values[valuesOffset++] = (int)((block1.TripleShift(38)) & 32767L);
                values[valuesOffset++] = (int)((block1.TripleShift(23)) & 32767L);
                values[valuesOffset++] = (int)((block1.TripleShift(8)) & 32767L);
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block1 & 255L) << 7) | (block2.TripleShift(57)));
                values[valuesOffset++] = (int)((block2.TripleShift(42)) & 32767L);
                values[valuesOffset++] = (int)((block2.TripleShift(27)) & 32767L);
                values[valuesOffset++] = (int)((block2.TripleShift(12)) & 32767L);
                long block3 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block2 & 4095L) << 3) | (block3.TripleShift(61)));
                values[valuesOffset++] = (int)((block3.TripleShift(46)) & 32767L);
                values[valuesOffset++] = (int)((block3.TripleShift(31)) & 32767L);
                values[valuesOffset++] = (int)((block3.TripleShift(16)) & 32767L);
                values[valuesOffset++] = (int)((block3.TripleShift(1)) & 32767L);
                long block4 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block3 & 1L) << 14) | (block4.TripleShift(50)));
                values[valuesOffset++] = (int)((block4.TripleShift(35)) & 32767L);
                values[valuesOffset++] = (int)((block4.TripleShift(20)) & 32767L);
                values[valuesOffset++] = (int)((block4.TripleShift(5)) & 32767L);
                long block5 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block4 & 31L) << 10) | (block5.TripleShift(54)));
                values[valuesOffset++] = (int)((block5.TripleShift(39)) & 32767L);
                values[valuesOffset++] = (int)((block5.TripleShift(24)) & 32767L);
                values[valuesOffset++] = (int)((block5.TripleShift(9)) & 32767L);
                long block6 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block5 & 511L) << 6) | (block6.TripleShift(58)));
                values[valuesOffset++] = (int)((block6.TripleShift(43)) & 32767L);
                values[valuesOffset++] = (int)((block6.TripleShift(28)) & 32767L);
                values[valuesOffset++] = (int)((block6.TripleShift(13)) & 32767L);
                long block7 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block6 & 8191L) << 2) | (block7.TripleShift(62)));
                values[valuesOffset++] = (int)((block7.TripleShift(47)) & 32767L);
                values[valuesOffset++] = (int)((block7.TripleShift(32)) & 32767L);
                values[valuesOffset++] = (int)((block7.TripleShift(17)) & 32767L);
                values[valuesOffset++] = (int)((block7.TripleShift(2)) & 32767L);
                long block8 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block7 & 3L) << 13) | (block8.TripleShift(51)));
                values[valuesOffset++] = (int)((block8.TripleShift(36)) & 32767L);
                values[valuesOffset++] = (int)((block8.TripleShift(21)) & 32767L);
                values[valuesOffset++] = (int)((block8.TripleShift(6)) & 32767L);
                long block9 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block8 & 63L) << 9) | (block9.TripleShift(55)));
                values[valuesOffset++] = (int)((block9.TripleShift(40)) & 32767L);
                values[valuesOffset++] = (int)((block9.TripleShift(25)) & 32767L);
                values[valuesOffset++] = (int)((block9.TripleShift(10)) & 32767L);
                long block10 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block9 & 1023L) << 5) | (block10.TripleShift(59)));
                values[valuesOffset++] = (int)((block10.TripleShift(44)) & 32767L);
                values[valuesOffset++] = (int)((block10.TripleShift(29)) & 32767L);
                values[valuesOffset++] = (int)((block10.TripleShift(14)) & 32767L);
                long block11 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block10 & 16383L) << 1) | (block11.TripleShift(63)));
                values[valuesOffset++] = (int)((block11.TripleShift(48)) & 32767L);
                values[valuesOffset++] = (int)((block11.TripleShift(33)) & 32767L);
                values[valuesOffset++] = (int)((block11.TripleShift(18)) & 32767L);
                values[valuesOffset++] = (int)((block11.TripleShift(3)) & 32767L);
                long block12 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block11 & 7L) << 12) | (block12.TripleShift(52)));
                values[valuesOffset++] = (int)((block12.TripleShift(37)) & 32767L);
                values[valuesOffset++] = (int)((block12.TripleShift(22)) & 32767L);
                values[valuesOffset++] = (int)((block12.TripleShift(7)) & 32767L);
                long block13 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block12 & 127L) << 8) | (block13.TripleShift(56)));
                values[valuesOffset++] = (int)((block13.TripleShift(41)) & 32767L);
                values[valuesOffset++] = (int)((block13.TripleShift(26)) & 32767L);
                values[valuesOffset++] = (int)((block13.TripleShift(11)) & 32767L);
                long block14 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block13 & 2047L) << 4) | (block14.TripleShift(60)));
                values[valuesOffset++] = (int)((block14.TripleShift(45)) & 32767L);
                values[valuesOffset++] = (int)((block14.TripleShift(30)) & 32767L);
                values[valuesOffset++] = (int)((block14.TripleShift(15)) & 32767L);
                values[valuesOffset++] = (int)(block14 & 32767L);
            }
        }

        public override void Decode(byte[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                int byte0 = blocks[blocksOffset++] & 0xFF;
                int byte1 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = (byte0 << 7) | (byte1.TripleShift(1));
                int byte2 = blocks[blocksOffset++] & 0xFF;
                int byte3 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte1 & 1) << 14) | (byte2 << 6) | (byte3.TripleShift(2));
                int byte4 = blocks[blocksOffset++] & 0xFF;
                int byte5 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte3 & 3) << 13) | (byte4 << 5) | (byte5.TripleShift(3));
                int byte6 = blocks[blocksOffset++] & 0xFF;
                int byte7 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte5 & 7) << 12) | (byte6 << 4) | (byte7.TripleShift(4));
                int byte8 = blocks[blocksOffset++] & 0xFF;
                int byte9 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte7 & 15) << 11) | (byte8 << 3) | (byte9.TripleShift(5));
                int byte10 = blocks[blocksOffset++] & 0xFF;
                int byte11 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte9 & 31) << 10) | (byte10 << 2) | (byte11.TripleShift(6));
                int byte12 = blocks[blocksOffset++] & 0xFF;
                int byte13 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte11 & 63) << 9) | (byte12 << 1) | (byte13.TripleShift(7));
                int byte14 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte13 & 127) << 8) | byte14;
            }
        }

        public override void Decode(long[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block0 = blocks[blocksOffset++];
                values[valuesOffset++] = block0.TripleShift(49);
                values[valuesOffset++] = (block0.TripleShift(34)) & 32767L;
                values[valuesOffset++] = (block0.TripleShift(19)) & 32767L;
                values[valuesOffset++] = (block0.TripleShift(4)) & 32767L;
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block0 & 15L) << 11) | (block1.TripleShift(53));
                values[valuesOffset++] = (block1.TripleShift(38)) & 32767L;
                values[valuesOffset++] = (block1.TripleShift(23)) & 32767L;
                values[valuesOffset++] = (block1.TripleShift(8)) & 32767L;
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block1 & 255L) << 7) | (block2.TripleShift(57));
                values[valuesOffset++] = (block2.TripleShift(42)) & 32767L;
                values[valuesOffset++] = (block2.TripleShift(27)) & 32767L;
                values[valuesOffset++] = (block2.TripleShift(12)) & 32767L;
                long block3 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block2 & 4095L) << 3) | (block3.TripleShift(61));
                values[valuesOffset++] = (block3.TripleShift(46)) & 32767L;
                values[valuesOffset++] = (block3.TripleShift(31)) & 32767L;
                values[valuesOffset++] = (block3.TripleShift(16)) & 32767L;
                values[valuesOffset++] = (block3.TripleShift(1)) & 32767L;
                long block4 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block3 & 1L) << 14) | (block4.TripleShift(50));
                values[valuesOffset++] = (block4.TripleShift(35)) & 32767L;
                values[valuesOffset++] = (block4.TripleShift(20)) & 32767L;
                values[valuesOffset++] = (block4.TripleShift(5)) & 32767L;
                long block5 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block4 & 31L) << 10) | (block5.TripleShift(54));
                values[valuesOffset++] = (block5.TripleShift(39)) & 32767L;
                values[valuesOffset++] = (block5.TripleShift(24)) & 32767L;
                values[valuesOffset++] = (block5.TripleShift(9)) & 32767L;
                long block6 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block5 & 511L) << 6) | (block6.TripleShift(58));
                values[valuesOffset++] = (block6.TripleShift(43)) & 32767L;
                values[valuesOffset++] = (block6.TripleShift(28)) & 32767L;
                values[valuesOffset++] = (block6.TripleShift(13)) & 32767L;
                long block7 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block6 & 8191L) << 2) | (block7.TripleShift(62));
                values[valuesOffset++] = (block7.TripleShift(47)) & 32767L;
                values[valuesOffset++] = (block7.TripleShift(32)) & 32767L;
                values[valuesOffset++] = (block7.TripleShift(17)) & 32767L;
                values[valuesOffset++] = (block7.TripleShift(2)) & 32767L;
                long block8 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block7 & 3L) << 13) | (block8.TripleShift(51));
                values[valuesOffset++] = (block8.TripleShift(36)) & 32767L;
                values[valuesOffset++] = (block8.TripleShift(21)) & 32767L;
                values[valuesOffset++] = (block8.TripleShift(6)) & 32767L;
                long block9 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block8 & 63L) << 9) | (block9.TripleShift(55));
                values[valuesOffset++] = (block9.TripleShift(40)) & 32767L;
                values[valuesOffset++] = (block9.TripleShift(25)) & 32767L;
                values[valuesOffset++] = (block9.TripleShift(10)) & 32767L;
                long block10 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block9 & 1023L) << 5) | (block10.TripleShift(59));
                values[valuesOffset++] = (block10.TripleShift(44)) & 32767L;
                values[valuesOffset++] = (block10.TripleShift(29)) & 32767L;
                values[valuesOffset++] = (block10.TripleShift(14)) & 32767L;
                long block11 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block10 & 16383L) << 1) | (block11.TripleShift(63));
                values[valuesOffset++] = (block11.TripleShift(48)) & 32767L;
                values[valuesOffset++] = (block11.TripleShift(33)) & 32767L;
                values[valuesOffset++] = (block11.TripleShift(18)) & 32767L;
                values[valuesOffset++] = (block11.TripleShift(3)) & 32767L;
                long block12 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block11 & 7L) << 12) | (block12.TripleShift(52));
                values[valuesOffset++] = (block12.TripleShift(37)) & 32767L;
                values[valuesOffset++] = (block12.TripleShift(22)) & 32767L;
                values[valuesOffset++] = (block12.TripleShift(7)) & 32767L;
                long block13 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block12 & 127L) << 8) | (block13.TripleShift(56));
                values[valuesOffset++] = (block13.TripleShift(41)) & 32767L;
                values[valuesOffset++] = (block13.TripleShift(26)) & 32767L;
                values[valuesOffset++] = (block13.TripleShift(11)) & 32767L;
                long block14 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block13 & 2047L) << 4) | (block14.TripleShift(60));
                values[valuesOffset++] = (block14.TripleShift(45)) & 32767L;
                values[valuesOffset++] = (block14.TripleShift(30)) & 32767L;
                values[valuesOffset++] = (block14.TripleShift(15)) & 32767L;
                values[valuesOffset++] = block14 & 32767L;
            }
        }

        public override void Decode(byte[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long byte0 = blocks[blocksOffset++] & 0xFF;
                long byte1 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = (byte0 << 7) | (byte1.TripleShift(1));
                long byte2 = blocks[blocksOffset++] & 0xFF;
                long byte3 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte1 & 1) << 14) | (byte2 << 6) | (byte3.TripleShift(2));
                long byte4 = blocks[blocksOffset++] & 0xFF;
                long byte5 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte3 & 3) << 13) | (byte4 << 5) | (byte5.TripleShift(3));
                long byte6 = blocks[blocksOffset++] & 0xFF;
                long byte7 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte5 & 7) << 12) | (byte6 << 4) | (byte7.TripleShift(4));
                long byte8 = blocks[blocksOffset++] & 0xFF;
                long byte9 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte7 & 15) << 11) | (byte8 << 3) | (byte9.TripleShift(5));
                long byte10 = blocks[blocksOffset++] & 0xFF;
                long byte11 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte9 & 31) << 10) | (byte10 << 2) | (byte11.TripleShift(6));
                long byte12 = blocks[blocksOffset++] & 0xFF;
                long byte13 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte11 & 63) << 9) | (byte12 << 1) | (byte13.TripleShift(7));
                long byte14 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte13 & 127) << 8) | byte14;
            }
        }
    }
}