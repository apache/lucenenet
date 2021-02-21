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
    internal sealed class BulkOperationPacked17 : BulkOperationPacked
    {
        public BulkOperationPacked17()
            : base(17)
        {
        }

        public override void Decode(long[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block0 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(block0.TripleShift(47));
                values[valuesOffset++] = (int)((block0.TripleShift(30)) & 131071L);
                values[valuesOffset++] = (int)((block0.TripleShift(13)) & 131071L);
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block0 & 8191L) << 4) | (block1.TripleShift(60)));
                values[valuesOffset++] = (int)((block1.TripleShift(43)) & 131071L);
                values[valuesOffset++] = (int)((block1.TripleShift(26)) & 131071L);
                values[valuesOffset++] = (int)((block1.TripleShift(9)) & 131071L);
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block1 & 511L) << 8) | (block2.TripleShift(56)));
                values[valuesOffset++] = (int)((block2.TripleShift(39)) & 131071L);
                values[valuesOffset++] = (int)((block2.TripleShift(22)) & 131071L);
                values[valuesOffset++] = (int)((block2.TripleShift(5)) & 131071L);
                long block3 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block2 & 31L) << 12) | (block3.TripleShift(52)));
                values[valuesOffset++] = (int)((block3.TripleShift(35)) & 131071L);
                values[valuesOffset++] = (int)((block3.TripleShift(18)) & 131071L);
                values[valuesOffset++] = (int)((block3.TripleShift(1)) & 131071L);
                long block4 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block3 & 1L) << 16) | (block4.TripleShift(48)));
                values[valuesOffset++] = (int)((block4.TripleShift(31)) & 131071L);
                values[valuesOffset++] = (int)((block4.TripleShift(14)) & 131071L);
                long block5 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block4 & 16383L) << 3) | (block5.TripleShift(61)));
                values[valuesOffset++] = (int)((block5.TripleShift(44)) & 131071L);
                values[valuesOffset++] = (int)((block5.TripleShift(27)) & 131071L);
                values[valuesOffset++] = (int)((block5.TripleShift(10)) & 131071L);
                long block6 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block5 & 1023L) << 7) | (block6.TripleShift(57)));
                values[valuesOffset++] = (int)((block6.TripleShift(40)) & 131071L);
                values[valuesOffset++] = (int)((block6.TripleShift(23)) & 131071L);
                values[valuesOffset++] = (int)((block6.TripleShift(6)) & 131071L);
                long block7 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block6 & 63L) << 11) | (block7.TripleShift(53)));
                values[valuesOffset++] = (int)((block7.TripleShift(36)) & 131071L);
                values[valuesOffset++] = (int)((block7.TripleShift(19)) & 131071L);
                values[valuesOffset++] = (int)((block7.TripleShift(2)) & 131071L);
                long block8 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block7 & 3L) << 15) | (block8.TripleShift(49)));
                values[valuesOffset++] = (int)((block8.TripleShift(32)) & 131071L);
                values[valuesOffset++] = (int)((block8.TripleShift(15)) & 131071L);
                long block9 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block8 & 32767L) << 2) | (block9.TripleShift(62)));
                values[valuesOffset++] = (int)((block9.TripleShift(45)) & 131071L);
                values[valuesOffset++] = (int)((block9.TripleShift(28)) & 131071L);
                values[valuesOffset++] = (int)((block9.TripleShift(11)) & 131071L);
                long block10 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block9 & 2047L) << 6) | (block10.TripleShift(58)));
                values[valuesOffset++] = (int)((block10.TripleShift(41)) & 131071L);
                values[valuesOffset++] = (int)((block10.TripleShift(24)) & 131071L);
                values[valuesOffset++] = (int)((block10.TripleShift(7)) & 131071L);
                long block11 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block10 & 127L) << 10) | (block11.TripleShift(54)));
                values[valuesOffset++] = (int)((block11.TripleShift(37)) & 131071L);
                values[valuesOffset++] = (int)((block11.TripleShift(20)) & 131071L);
                values[valuesOffset++] = (int)((block11.TripleShift(3)) & 131071L);
                long block12 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block11 & 7L) << 14) | (block12.TripleShift(50)));
                values[valuesOffset++] = (int)((block12.TripleShift(33)) & 131071L);
                values[valuesOffset++] = (int)((block12.TripleShift(16)) & 131071L);
                long block13 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block12 & 65535L) << 1) | (block13.TripleShift(63)));
                values[valuesOffset++] = (int)((block13.TripleShift(46)) & 131071L);
                values[valuesOffset++] = (int)((block13.TripleShift(29)) & 131071L);
                values[valuesOffset++] = (int)((block13.TripleShift(12)) & 131071L);
                long block14 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block13 & 4095L) << 5) | (block14.TripleShift(59)));
                values[valuesOffset++] = (int)((block14.TripleShift(42)) & 131071L);
                values[valuesOffset++] = (int)((block14.TripleShift(25)) & 131071L);
                values[valuesOffset++] = (int)((block14.TripleShift(8)) & 131071L);
                long block15 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block14 & 255L) << 9) | (block15.TripleShift(55)));
                values[valuesOffset++] = (int)((block15.TripleShift(38)) & 131071L);
                values[valuesOffset++] = (int)((block15.TripleShift(21)) & 131071L);
                values[valuesOffset++] = (int)((block15.TripleShift(4)) & 131071L);
                long block16 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block15 & 15L) << 13) | (block16.TripleShift(51)));
                values[valuesOffset++] = (int)((block16.TripleShift(34)) & 131071L);
                values[valuesOffset++] = (int)((block16.TripleShift(17)) & 131071L);
                values[valuesOffset++] = (int)(block16 & 131071L);
            }
        }

        public override void Decode(byte[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                int byte0 = blocks[blocksOffset++] & 0xFF;
                int byte1 = blocks[blocksOffset++] & 0xFF;
                int byte2 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = (byte0 << 9) | (byte1 << 1) | (byte2.TripleShift(7));
                int byte3 = blocks[blocksOffset++] & 0xFF;
                int byte4 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte2 & 127) << 10) | (byte3 << 2) | (byte4.TripleShift(6));
                int byte5 = blocks[blocksOffset++] & 0xFF;
                int byte6 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte4 & 63) << 11) | (byte5 << 3) | (byte6.TripleShift(5));
                int byte7 = blocks[blocksOffset++] & 0xFF;
                int byte8 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte6 & 31) << 12) | (byte7 << 4) | (byte8.TripleShift(4));
                int byte9 = blocks[blocksOffset++] & 0xFF;
                int byte10 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte8 & 15) << 13) | (byte9 << 5) | (byte10.TripleShift(3));
                int byte11 = blocks[blocksOffset++] & 0xFF;
                int byte12 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte10 & 7) << 14) | (byte11 << 6) | (byte12.TripleShift(2));
                int byte13 = blocks[blocksOffset++] & 0xFF;
                int byte14 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte12 & 3) << 15) | (byte13 << 7) | (byte14.TripleShift(1));
                int byte15 = blocks[blocksOffset++] & 0xFF;
                int byte16 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte14 & 1) << 16) | (byte15 << 8) | byte16;
            }
        }

        public override void Decode(long[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block0 = blocks[blocksOffset++];
                values[valuesOffset++] = block0.TripleShift(47);
                values[valuesOffset++] = (block0.TripleShift(30)) & 131071L;
                values[valuesOffset++] = (block0.TripleShift(13)) & 131071L;
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block0 & 8191L) << 4) | (block1.TripleShift(60));
                values[valuesOffset++] = (block1.TripleShift(43)) & 131071L;
                values[valuesOffset++] = (block1.TripleShift(26)) & 131071L;
                values[valuesOffset++] = (block1.TripleShift(9)) & 131071L;
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block1 & 511L) << 8) | (block2.TripleShift(56));
                values[valuesOffset++] = (block2.TripleShift(39)) & 131071L;
                values[valuesOffset++] = (block2.TripleShift(22)) & 131071L;
                values[valuesOffset++] = (block2.TripleShift(5)) & 131071L;
                long block3 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block2 & 31L) << 12) | (block3.TripleShift(52));
                values[valuesOffset++] = (block3.TripleShift(35)) & 131071L;
                values[valuesOffset++] = (block3.TripleShift(18)) & 131071L;
                values[valuesOffset++] = (block3.TripleShift(1)) & 131071L;
                long block4 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block3 & 1L) << 16) | (block4.TripleShift(48));
                values[valuesOffset++] = (block4.TripleShift(31)) & 131071L;
                values[valuesOffset++] = (block4.TripleShift(14)) & 131071L;
                long block5 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block4 & 16383L) << 3) | (block5.TripleShift(61));
                values[valuesOffset++] = (block5.TripleShift(44)) & 131071L;
                values[valuesOffset++] = (block5.TripleShift(27)) & 131071L;
                values[valuesOffset++] = (block5.TripleShift(10)) & 131071L;
                long block6 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block5 & 1023L) << 7) | (block6.TripleShift(57));
                values[valuesOffset++] = (block6.TripleShift(40)) & 131071L;
                values[valuesOffset++] = (block6.TripleShift(23)) & 131071L;
                values[valuesOffset++] = (block6.TripleShift(6)) & 131071L;
                long block7 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block6 & 63L) << 11) | (block7.TripleShift(53));
                values[valuesOffset++] = (block7.TripleShift(36)) & 131071L;
                values[valuesOffset++] = (block7.TripleShift(19)) & 131071L;
                values[valuesOffset++] = (block7.TripleShift(2)) & 131071L;
                long block8 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block7 & 3L) << 15) | (block8.TripleShift(49));
                values[valuesOffset++] = (block8.TripleShift(32)) & 131071L;
                values[valuesOffset++] = (block8.TripleShift(15)) & 131071L;
                long block9 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block8 & 32767L) << 2) | (block9.TripleShift(62));
                values[valuesOffset++] = (block9.TripleShift(45)) & 131071L;
                values[valuesOffset++] = (block9.TripleShift(28)) & 131071L;
                values[valuesOffset++] = (block9.TripleShift(11)) & 131071L;
                long block10 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block9 & 2047L) << 6) | (block10.TripleShift(58));
                values[valuesOffset++] = (block10.TripleShift(41)) & 131071L;
                values[valuesOffset++] = (block10.TripleShift(24)) & 131071L;
                values[valuesOffset++] = (block10.TripleShift(7)) & 131071L;
                long block11 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block10 & 127L) << 10) | (block11.TripleShift(54));
                values[valuesOffset++] = (block11.TripleShift(37)) & 131071L;
                values[valuesOffset++] = (block11.TripleShift(20)) & 131071L;
                values[valuesOffset++] = (block11.TripleShift(3)) & 131071L;
                long block12 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block11 & 7L) << 14) | (block12.TripleShift(50));
                values[valuesOffset++] = (block12.TripleShift(33)) & 131071L;
                values[valuesOffset++] = (block12.TripleShift(16)) & 131071L;
                long block13 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block12 & 65535L) << 1) | (block13.TripleShift(63));
                values[valuesOffset++] = (block13.TripleShift(46)) & 131071L;
                values[valuesOffset++] = (block13.TripleShift(29)) & 131071L;
                values[valuesOffset++] = (block13.TripleShift(12)) & 131071L;
                long block14 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block13 & 4095L) << 5) | (block14.TripleShift(59));
                values[valuesOffset++] = (block14.TripleShift(42)) & 131071L;
                values[valuesOffset++] = (block14.TripleShift(25)) & 131071L;
                values[valuesOffset++] = (block14.TripleShift(8)) & 131071L;
                long block15 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block14 & 255L) << 9) | (block15.TripleShift(55));
                values[valuesOffset++] = (block15.TripleShift(38)) & 131071L;
                values[valuesOffset++] = (block15.TripleShift(21)) & 131071L;
                values[valuesOffset++] = (block15.TripleShift(4)) & 131071L;
                long block16 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block15 & 15L) << 13) | (block16.TripleShift(51));
                values[valuesOffset++] = (block16.TripleShift(34)) & 131071L;
                values[valuesOffset++] = (block16.TripleShift(17)) & 131071L;
                values[valuesOffset++] = block16 & 131071L;
            }
        }

        public override void Decode(byte[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long byte0 = blocks[blocksOffset++] & 0xFF;
                long byte1 = blocks[blocksOffset++] & 0xFF;
                long byte2 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = (byte0 << 9) | (byte1 << 1) | (byte2.TripleShift(7));
                long byte3 = blocks[blocksOffset++] & 0xFF;
                long byte4 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte2 & 127) << 10) | (byte3 << 2) | (byte4.TripleShift(6));
                long byte5 = blocks[blocksOffset++] & 0xFF;
                long byte6 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte4 & 63) << 11) | (byte5 << 3) | (byte6.TripleShift(5));
                long byte7 = blocks[blocksOffset++] & 0xFF;
                long byte8 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte6 & 31) << 12) | (byte7 << 4) | (byte8.TripleShift(4));
                long byte9 = blocks[blocksOffset++] & 0xFF;
                long byte10 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte8 & 15) << 13) | (byte9 << 5) | (byte10.TripleShift(3));
                long byte11 = blocks[blocksOffset++] & 0xFF;
                long byte12 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte10 & 7) << 14) | (byte11 << 6) | (byte12.TripleShift(2));
                long byte13 = blocks[blocksOffset++] & 0xFF;
                long byte14 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte12 & 3) << 15) | (byte13 << 7) | (byte14.TripleShift(1));
                long byte15 = blocks[blocksOffset++] & 0xFF;
                long byte16 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte14 & 1) << 16) | (byte15 << 8) | byte16;
            }
        }
    }
}