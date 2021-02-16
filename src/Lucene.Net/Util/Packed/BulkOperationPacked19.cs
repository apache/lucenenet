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
    internal sealed class BulkOperationPacked19 : BulkOperationPacked
    {
        public BulkOperationPacked19()
            : base(19)
        {
        }

        public override void Decode(long[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block0 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(block0.TripleShift(45));
                values[valuesOffset++] = (int)((block0.TripleShift(26)) & 524287L);
                values[valuesOffset++] = (int)((block0.TripleShift(7)) & 524287L);
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block0 & 127L) << 12) | (block1.TripleShift(52)));
                values[valuesOffset++] = (int)((block1.TripleShift(33)) & 524287L);
                values[valuesOffset++] = (int)((block1.TripleShift(14)) & 524287L);
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block1 & 16383L) << 5) | (block2.TripleShift(59)));
                values[valuesOffset++] = (int)((block2.TripleShift(40)) & 524287L);
                values[valuesOffset++] = (int)((block2.TripleShift(21)) & 524287L);
                values[valuesOffset++] = (int)((block2.TripleShift(2)) & 524287L);
                long block3 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block2 & 3L) << 17) | (block3.TripleShift(47)));
                values[valuesOffset++] = (int)((block3.TripleShift(28)) & 524287L);
                values[valuesOffset++] = (int)((block3.TripleShift(9)) & 524287L);
                long block4 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block3 & 511L) << 10) | (block4.TripleShift(54)));
                values[valuesOffset++] = (int)((block4.TripleShift(35)) & 524287L);
                values[valuesOffset++] = (int)((block4.TripleShift(16)) & 524287L);
                long block5 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block4 & 65535L) << 3) | (block5.TripleShift(61)));
                values[valuesOffset++] = (int)((block5.TripleShift(42)) & 524287L);
                values[valuesOffset++] = (int)((block5.TripleShift(23)) & 524287L);
                values[valuesOffset++] = (int)((block5.TripleShift(4)) & 524287L);
                long block6 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block5 & 15L) << 15) | (block6.TripleShift(49)));
                values[valuesOffset++] = (int)((block6.TripleShift(30)) & 524287L);
                values[valuesOffset++] = (int)((block6.TripleShift(11)) & 524287L);
                long block7 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block6 & 2047L) << 8) | (block7.TripleShift(56)));
                values[valuesOffset++] = (int)((block7.TripleShift(37)) & 524287L);
                values[valuesOffset++] = (int)((block7.TripleShift(18)) & 524287L);
                long block8 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block7 & 262143L) << 1) | (block8.TripleShift(63)));
                values[valuesOffset++] = (int)((block8.TripleShift(44)) & 524287L);
                values[valuesOffset++] = (int)((block8.TripleShift(25)) & 524287L);
                values[valuesOffset++] = (int)((block8.TripleShift(6)) & 524287L);
                long block9 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block8 & 63L) << 13) | (block9.TripleShift(51)));
                values[valuesOffset++] = (int)((block9.TripleShift(32)) & 524287L);
                values[valuesOffset++] = (int)((block9.TripleShift(13)) & 524287L);
                long block10 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block9 & 8191L) << 6) | (block10.TripleShift(58)));
                values[valuesOffset++] = (int)((block10.TripleShift(39)) & 524287L);
                values[valuesOffset++] = (int)((block10.TripleShift(20)) & 524287L);
                values[valuesOffset++] = (int)((block10.TripleShift(1)) & 524287L);
                long block11 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block10 & 1L) << 18) | (block11.TripleShift(46)));
                values[valuesOffset++] = (int)((block11.TripleShift(27)) & 524287L);
                values[valuesOffset++] = (int)((block11.TripleShift(8)) & 524287L);
                long block12 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block11 & 255L) << 11) | (block12.TripleShift(53)));
                values[valuesOffset++] = (int)((block12.TripleShift(34)) & 524287L);
                values[valuesOffset++] = (int)((block12.TripleShift(15)) & 524287L);
                long block13 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block12 & 32767L) << 4) | (block13.TripleShift(60)));
                values[valuesOffset++] = (int)((block13.TripleShift(41)) & 524287L);
                values[valuesOffset++] = (int)((block13.TripleShift(22)) & 524287L);
                values[valuesOffset++] = (int)((block13.TripleShift(3)) & 524287L);
                long block14 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block13 & 7L) << 16) | (block14.TripleShift(48)));
                values[valuesOffset++] = (int)((block14.TripleShift(29)) & 524287L);
                values[valuesOffset++] = (int)((block14.TripleShift(10)) & 524287L);
                long block15 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block14 & 1023L) << 9) | (block15.TripleShift(55)));
                values[valuesOffset++] = (int)((block15.TripleShift(36)) & 524287L);
                values[valuesOffset++] = (int)((block15.TripleShift(17)) & 524287L);
                long block16 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block15 & 131071L) << 2) | (block16.TripleShift(62)));
                values[valuesOffset++] = (int)((block16.TripleShift(43)) & 524287L);
                values[valuesOffset++] = (int)((block16.TripleShift(24)) & 524287L);
                values[valuesOffset++] = (int)((block16.TripleShift(5)) & 524287L);
                long block17 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block16 & 31L) << 14) | (block17.TripleShift(50)));
                values[valuesOffset++] = (int)((block17.TripleShift(31)) & 524287L);
                values[valuesOffset++] = (int)((block17.TripleShift(12)) & 524287L);
                long block18 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block17 & 4095L) << 7) | (block18.TripleShift(57)));
                values[valuesOffset++] = (int)((block18.TripleShift(38)) & 524287L);
                values[valuesOffset++] = (int)((block18.TripleShift(19)) & 524287L);
                values[valuesOffset++] = (int)(block18 & 524287L);
            }
        }

        public override void Decode(byte[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                int byte0 = blocks[blocksOffset++] & 0xFF;
                int byte1 = blocks[blocksOffset++] & 0xFF;
                int byte2 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = (byte0 << 11) | (byte1 << 3) | (byte2.TripleShift(5));
                int byte3 = blocks[blocksOffset++] & 0xFF;
                int byte4 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte2 & 31) << 14) | (byte3 << 6) | (byte4.TripleShift(2));
                int byte5 = blocks[blocksOffset++] & 0xFF;
                int byte6 = blocks[blocksOffset++] & 0xFF;
                int byte7 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte4 & 3) << 17) | (byte5 << 9) | (byte6 << 1) | (byte7.TripleShift(7));
                int byte8 = blocks[blocksOffset++] & 0xFF;
                int byte9 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte7 & 127) << 12) | (byte8 << 4) | (byte9.TripleShift(4));
                int byte10 = blocks[blocksOffset++] & 0xFF;
                int byte11 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte9 & 15) << 15) | (byte10 << 7) | (byte11.TripleShift(1));
                int byte12 = blocks[blocksOffset++] & 0xFF;
                int byte13 = blocks[blocksOffset++] & 0xFF;
                int byte14 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte11 & 1) << 18) | (byte12 << 10) | (byte13 << 2) | (byte14.TripleShift(6));
                int byte15 = blocks[blocksOffset++] & 0xFF;
                int byte16 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte14 & 63) << 13) | (byte15 << 5) | (byte16.TripleShift(3));
                int byte17 = blocks[blocksOffset++] & 0xFF;
                int byte18 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte16 & 7) << 16) | (byte17 << 8) | byte18;
            }
        }

        public override void Decode(long[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block0 = blocks[blocksOffset++];
                values[valuesOffset++] = block0.TripleShift(45);
                values[valuesOffset++] = (block0.TripleShift(26)) & 524287L;
                values[valuesOffset++] = (block0.TripleShift(7)) & 524287L;
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block0 & 127L) << 12) | (block1.TripleShift(52));
                values[valuesOffset++] = (block1.TripleShift(33)) & 524287L;
                values[valuesOffset++] = (block1.TripleShift(14)) & 524287L;
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block1 & 16383L) << 5) | (block2.TripleShift(59));
                values[valuesOffset++] = (block2.TripleShift(40)) & 524287L;
                values[valuesOffset++] = (block2.TripleShift(21)) & 524287L;
                values[valuesOffset++] = (block2.TripleShift(2)) & 524287L;
                long block3 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block2 & 3L) << 17) | (block3.TripleShift(47));
                values[valuesOffset++] = (block3.TripleShift(28)) & 524287L;
                values[valuesOffset++] = (block3.TripleShift(9)) & 524287L;
                long block4 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block3 & 511L) << 10) | (block4.TripleShift(54));
                values[valuesOffset++] = (block4.TripleShift(35)) & 524287L;
                values[valuesOffset++] = (block4.TripleShift(16)) & 524287L;
                long block5 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block4 & 65535L) << 3) | (block5.TripleShift(61));
                values[valuesOffset++] = (block5.TripleShift(42)) & 524287L;
                values[valuesOffset++] = (block5.TripleShift(23)) & 524287L;
                values[valuesOffset++] = (block5.TripleShift(4)) & 524287L;
                long block6 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block5 & 15L) << 15) | (block6.TripleShift(49));
                values[valuesOffset++] = (block6.TripleShift(30)) & 524287L;
                values[valuesOffset++] = (block6.TripleShift(11)) & 524287L;
                long block7 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block6 & 2047L) << 8) | (block7.TripleShift(56));
                values[valuesOffset++] = (block7.TripleShift(37)) & 524287L;
                values[valuesOffset++] = (block7.TripleShift(18)) & 524287L;
                long block8 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block7 & 262143L) << 1) | (block8.TripleShift(63));
                values[valuesOffset++] = (block8.TripleShift(44)) & 524287L;
                values[valuesOffset++] = (block8.TripleShift(25)) & 524287L;
                values[valuesOffset++] = (block8.TripleShift(6)) & 524287L;
                long block9 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block8 & 63L) << 13) | (block9.TripleShift(51));
                values[valuesOffset++] = (block9.TripleShift(32)) & 524287L;
                values[valuesOffset++] = (block9.TripleShift(13)) & 524287L;
                long block10 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block9 & 8191L) << 6) | (block10.TripleShift(58));
                values[valuesOffset++] = (block10.TripleShift(39)) & 524287L;
                values[valuesOffset++] = (block10.TripleShift(20)) & 524287L;
                values[valuesOffset++] = (block10.TripleShift(1)) & 524287L;
                long block11 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block10 & 1L) << 18) | (block11.TripleShift(46));
                values[valuesOffset++] = (block11.TripleShift(27)) & 524287L;
                values[valuesOffset++] = (block11.TripleShift(8)) & 524287L;
                long block12 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block11 & 255L) << 11) | (block12.TripleShift(53));
                values[valuesOffset++] = (block12.TripleShift(34)) & 524287L;
                values[valuesOffset++] = (block12.TripleShift(15)) & 524287L;
                long block13 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block12 & 32767L) << 4) | (block13.TripleShift(60));
                values[valuesOffset++] = (block13.TripleShift(41)) & 524287L;
                values[valuesOffset++] = (block13.TripleShift(22)) & 524287L;
                values[valuesOffset++] = (block13.TripleShift(3)) & 524287L;
                long block14 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block13 & 7L) << 16) | (block14.TripleShift(48));
                values[valuesOffset++] = (block14.TripleShift(29)) & 524287L;
                values[valuesOffset++] = (block14.TripleShift(10)) & 524287L;
                long block15 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block14 & 1023L) << 9) | (block15.TripleShift(55));
                values[valuesOffset++] = (block15.TripleShift(36)) & 524287L;
                values[valuesOffset++] = (block15.TripleShift(17)) & 524287L;
                long block16 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block15 & 131071L) << 2) | (block16.TripleShift(62));
                values[valuesOffset++] = (block16.TripleShift(43)) & 524287L;
                values[valuesOffset++] = (block16.TripleShift(24)) & 524287L;
                values[valuesOffset++] = (block16.TripleShift(5)) & 524287L;
                long block17 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block16 & 31L) << 14) | (block17.TripleShift(50));
                values[valuesOffset++] = (block17.TripleShift(31)) & 524287L;
                values[valuesOffset++] = (block17.TripleShift(12)) & 524287L;
                long block18 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block17 & 4095L) << 7) | (block18.TripleShift(57));
                values[valuesOffset++] = (block18.TripleShift(38)) & 524287L;
                values[valuesOffset++] = (block18.TripleShift(19)) & 524287L;
                values[valuesOffset++] = block18 & 524287L;
            }
        }

        public override void Decode(byte[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long byte0 = blocks[blocksOffset++] & 0xFF;
                long byte1 = blocks[blocksOffset++] & 0xFF;
                long byte2 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = (byte0 << 11) | (byte1 << 3) | (byte2.TripleShift(5));
                long byte3 = blocks[blocksOffset++] & 0xFF;
                long byte4 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte2 & 31) << 14) | (byte3 << 6) | (byte4.TripleShift(2));
                long byte5 = blocks[blocksOffset++] & 0xFF;
                long byte6 = blocks[blocksOffset++] & 0xFF;
                long byte7 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte4 & 3) << 17) | (byte5 << 9) | (byte6 << 1) | (byte7.TripleShift(7));
                long byte8 = blocks[blocksOffset++] & 0xFF;
                long byte9 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte7 & 127) << 12) | (byte8 << 4) | (byte9.TripleShift(4));
                long byte10 = blocks[blocksOffset++] & 0xFF;
                long byte11 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte9 & 15) << 15) | (byte10 << 7) | (byte11.TripleShift(1));
                long byte12 = blocks[blocksOffset++] & 0xFF;
                long byte13 = blocks[blocksOffset++] & 0xFF;
                long byte14 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte11 & 1) << 18) | (byte12 << 10) | (byte13 << 2) | (byte14.TripleShift(6));
                long byte15 = blocks[blocksOffset++] & 0xFF;
                long byte16 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte14 & 63) << 13) | (byte15 << 5) | (byte16.TripleShift(3));
                long byte17 = blocks[blocksOffset++] & 0xFF;
                long byte18 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte16 & 7) << 16) | (byte17 << 8) | byte18;
            }
        }
    }
}