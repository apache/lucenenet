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
    internal sealed class BulkOperationPacked23 : BulkOperationPacked
    {
        public BulkOperationPacked23()
            : base(23)
        {
        }

        public override void Decode(long[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block0 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(block0.TripleShift(41));
                values[valuesOffset++] = (int)((block0.TripleShift(18)) & 8388607L);
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block0 & 262143L) << 5) | (block1.TripleShift(59)));
                values[valuesOffset++] = (int)((block1.TripleShift(36)) & 8388607L);
                values[valuesOffset++] = (int)((block1.TripleShift(13)) & 8388607L);
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block1 & 8191L) << 10) | (block2.TripleShift(54)));
                values[valuesOffset++] = (int)((block2.TripleShift(31)) & 8388607L);
                values[valuesOffset++] = (int)((block2.TripleShift(8)) & 8388607L);
                long block3 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block2 & 255L) << 15) | (block3.TripleShift(49)));
                values[valuesOffset++] = (int)((block3.TripleShift(26)) & 8388607L);
                values[valuesOffset++] = (int)((block3.TripleShift(3)) & 8388607L);
                long block4 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block3 & 7L) << 20) | (block4.TripleShift(44)));
                values[valuesOffset++] = (int)((block4.TripleShift(21)) & 8388607L);
                long block5 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block4 & 2097151L) << 2) | (block5.TripleShift(62)));
                values[valuesOffset++] = (int)((block5.TripleShift(39)) & 8388607L);
                values[valuesOffset++] = (int)((block5.TripleShift(16)) & 8388607L);
                long block6 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block5 & 65535L) << 7) | (block6.TripleShift(57)));
                values[valuesOffset++] = (int)((block6.TripleShift(34)) & 8388607L);
                values[valuesOffset++] = (int)((block6.TripleShift(11)) & 8388607L);
                long block7 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block6 & 2047L) << 12) | (block7.TripleShift(52)));
                values[valuesOffset++] = (int)((block7.TripleShift(29)) & 8388607L);
                values[valuesOffset++] = (int)((block7.TripleShift(6)) & 8388607L);
                long block8 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block7 & 63L) << 17) | (block8.TripleShift(47)));
                values[valuesOffset++] = (int)((block8.TripleShift(24)) & 8388607L);
                values[valuesOffset++] = (int)((block8.TripleShift(1)) & 8388607L);
                long block9 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block8 & 1L) << 22) | (block9.TripleShift(42)));
                values[valuesOffset++] = (int)((block9.TripleShift(19)) & 8388607L);
                long block10 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block9 & 524287L) << 4) | (block10.TripleShift(60)));
                values[valuesOffset++] = (int)((block10.TripleShift(37)) & 8388607L);
                values[valuesOffset++] = (int)((block10.TripleShift(14)) & 8388607L);
                long block11 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block10 & 16383L) << 9) | (block11.TripleShift(55)));
                values[valuesOffset++] = (int)((block11.TripleShift(32)) & 8388607L);
                values[valuesOffset++] = (int)((block11.TripleShift(9)) & 8388607L);
                long block12 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block11 & 511L) << 14) | (block12.TripleShift(50)));
                values[valuesOffset++] = (int)((block12.TripleShift(27)) & 8388607L);
                values[valuesOffset++] = (int)((block12.TripleShift(4)) & 8388607L);
                long block13 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block12 & 15L) << 19) | (block13.TripleShift(45)));
                values[valuesOffset++] = (int)((block13.TripleShift(22)) & 8388607L);
                long block14 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block13 & 4194303L) << 1) | (block14.TripleShift(63)));
                values[valuesOffset++] = (int)((block14.TripleShift(40)) & 8388607L);
                values[valuesOffset++] = (int)((block14.TripleShift(17)) & 8388607L);
                long block15 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block14 & 131071L) << 6) | (block15.TripleShift(58)));
                values[valuesOffset++] = (int)((block15.TripleShift(35)) & 8388607L);
                values[valuesOffset++] = (int)((block15.TripleShift(12)) & 8388607L);
                long block16 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block15 & 4095L) << 11) | (block16.TripleShift(53)));
                values[valuesOffset++] = (int)((block16.TripleShift(30)) & 8388607L);
                values[valuesOffset++] = (int)((block16.TripleShift(7)) & 8388607L);
                long block17 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block16 & 127L) << 16) | (block17.TripleShift(48)));
                values[valuesOffset++] = (int)((block17.TripleShift(25)) & 8388607L);
                values[valuesOffset++] = (int)((block17.TripleShift(2)) & 8388607L);
                long block18 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block17 & 3L) << 21) | (block18.TripleShift(43)));
                values[valuesOffset++] = (int)((block18.TripleShift(20)) & 8388607L);
                long block19 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block18 & 1048575L) << 3) | (block19.TripleShift(61)));
                values[valuesOffset++] = (int)((block19.TripleShift(38)) & 8388607L);
                values[valuesOffset++] = (int)((block19.TripleShift(15)) & 8388607L);
                long block20 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block19 & 32767L) << 8) | (block20.TripleShift(56)));
                values[valuesOffset++] = (int)((block20.TripleShift(33)) & 8388607L);
                values[valuesOffset++] = (int)((block20.TripleShift(10)) & 8388607L);
                long block21 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block20 & 1023L) << 13) | (block21.TripleShift(51)));
                values[valuesOffset++] = (int)((block21.TripleShift(28)) & 8388607L);
                values[valuesOffset++] = (int)((block21.TripleShift(5)) & 8388607L);
                long block22 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block21 & 31L) << 18) | (block22.TripleShift(46)));
                values[valuesOffset++] = (int)((block22.TripleShift(23)) & 8388607L);
                values[valuesOffset++] = (int)(block22 & 8388607L);
            }
        }

        public override void Decode(byte[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                int byte0 = blocks[blocksOffset++] & 0xFF;
                int byte1 = blocks[blocksOffset++] & 0xFF;
                int byte2 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = (byte0 << 15) | (byte1 << 7) | (byte2.TripleShift(1));
                int byte3 = blocks[blocksOffset++] & 0xFF;
                int byte4 = blocks[blocksOffset++] & 0xFF;
                int byte5 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte2 & 1) << 22) | (byte3 << 14) | (byte4 << 6) | (byte5.TripleShift(2));
                int byte6 = blocks[blocksOffset++] & 0xFF;
                int byte7 = blocks[blocksOffset++] & 0xFF;
                int byte8 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte5 & 3) << 21) | (byte6 << 13) | (byte7 << 5) | (byte8.TripleShift(3));
                int byte9 = blocks[blocksOffset++] & 0xFF;
                int byte10 = blocks[blocksOffset++] & 0xFF;
                int byte11 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte8 & 7) << 20) | (byte9 << 12) | (byte10 << 4) | (byte11.TripleShift(4));
                int byte12 = blocks[blocksOffset++] & 0xFF;
                int byte13 = blocks[blocksOffset++] & 0xFF;
                int byte14 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte11 & 15) << 19) | (byte12 << 11) | (byte13 << 3) | (byte14.TripleShift(5));
                int byte15 = blocks[blocksOffset++] & 0xFF;
                int byte16 = blocks[blocksOffset++] & 0xFF;
                int byte17 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte14 & 31) << 18) | (byte15 << 10) | (byte16 << 2) | (byte17.TripleShift(6));
                int byte18 = blocks[blocksOffset++] & 0xFF;
                int byte19 = blocks[blocksOffset++] & 0xFF;
                int byte20 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte17 & 63) << 17) | (byte18 << 9) | (byte19 << 1) | (byte20.TripleShift(7));
                int byte21 = blocks[blocksOffset++] & 0xFF;
                int byte22 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte20 & 127) << 16) | (byte21 << 8) | byte22;
            }
        }

        public override void Decode(long[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block0 = blocks[blocksOffset++];
                values[valuesOffset++] = block0.TripleShift(41);
                values[valuesOffset++] = (block0.TripleShift(18)) & 8388607L;
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block0 & 262143L) << 5) | (block1.TripleShift(59));
                values[valuesOffset++] = (block1.TripleShift(36)) & 8388607L;
                values[valuesOffset++] = (block1.TripleShift(13)) & 8388607L;
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block1 & 8191L) << 10) | (block2.TripleShift(54));
                values[valuesOffset++] = (block2.TripleShift(31)) & 8388607L;
                values[valuesOffset++] = (block2.TripleShift(8)) & 8388607L;
                long block3 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block2 & 255L) << 15) | (block3.TripleShift(49));
                values[valuesOffset++] = (block3.TripleShift(26)) & 8388607L;
                values[valuesOffset++] = (block3.TripleShift(3)) & 8388607L;
                long block4 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block3 & 7L) << 20) | (block4.TripleShift(44));
                values[valuesOffset++] = (block4.TripleShift(21)) & 8388607L;
                long block5 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block4 & 2097151L) << 2) | (block5.TripleShift(62));
                values[valuesOffset++] = (block5.TripleShift(39)) & 8388607L;
                values[valuesOffset++] = (block5.TripleShift(16)) & 8388607L;
                long block6 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block5 & 65535L) << 7) | (block6.TripleShift(57));
                values[valuesOffset++] = (block6.TripleShift(34)) & 8388607L;
                values[valuesOffset++] = (block6.TripleShift(11)) & 8388607L;
                long block7 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block6 & 2047L) << 12) | (block7.TripleShift(52));
                values[valuesOffset++] = (block7.TripleShift(29)) & 8388607L;
                values[valuesOffset++] = (block7.TripleShift(6)) & 8388607L;
                long block8 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block7 & 63L) << 17) | (block8.TripleShift(47));
                values[valuesOffset++] = (block8.TripleShift(24)) & 8388607L;
                values[valuesOffset++] = (block8.TripleShift(1)) & 8388607L;
                long block9 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block8 & 1L) << 22) | (block9.TripleShift(42));
                values[valuesOffset++] = (block9.TripleShift(19)) & 8388607L;
                long block10 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block9 & 524287L) << 4) | (block10.TripleShift(60));
                values[valuesOffset++] = (block10.TripleShift(37)) & 8388607L;
                values[valuesOffset++] = (block10.TripleShift(14)) & 8388607L;
                long block11 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block10 & 16383L) << 9) | (block11.TripleShift(55));
                values[valuesOffset++] = (block11.TripleShift(32)) & 8388607L;
                values[valuesOffset++] = (block11.TripleShift(9)) & 8388607L;
                long block12 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block11 & 511L) << 14) | (block12.TripleShift(50));
                values[valuesOffset++] = (block12.TripleShift(27)) & 8388607L;
                values[valuesOffset++] = (block12.TripleShift(4)) & 8388607L;
                long block13 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block12 & 15L) << 19) | (block13.TripleShift(45));
                values[valuesOffset++] = (block13.TripleShift(22)) & 8388607L;
                long block14 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block13 & 4194303L) << 1) | (block14.TripleShift(63));
                values[valuesOffset++] = (block14.TripleShift(40)) & 8388607L;
                values[valuesOffset++] = (block14.TripleShift(17)) & 8388607L;
                long block15 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block14 & 131071L) << 6) | (block15.TripleShift(58));
                values[valuesOffset++] = (block15.TripleShift(35)) & 8388607L;
                values[valuesOffset++] = (block15.TripleShift(12)) & 8388607L;
                long block16 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block15 & 4095L) << 11) | (block16.TripleShift(53));
                values[valuesOffset++] = (block16.TripleShift(30)) & 8388607L;
                values[valuesOffset++] = (block16.TripleShift(7)) & 8388607L;
                long block17 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block16 & 127L) << 16) | (block17.TripleShift(48));
                values[valuesOffset++] = (block17.TripleShift(25)) & 8388607L;
                values[valuesOffset++] = (block17.TripleShift(2)) & 8388607L;
                long block18 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block17 & 3L) << 21) | (block18.TripleShift(43));
                values[valuesOffset++] = (block18.TripleShift(20)) & 8388607L;
                long block19 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block18 & 1048575L) << 3) | (block19.TripleShift(61));
                values[valuesOffset++] = (block19.TripleShift(38)) & 8388607L;
                values[valuesOffset++] = (block19.TripleShift(15)) & 8388607L;
                long block20 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block19 & 32767L) << 8) | (block20.TripleShift(56));
                values[valuesOffset++] = (block20.TripleShift(33)) & 8388607L;
                values[valuesOffset++] = (block20.TripleShift(10)) & 8388607L;
                long block21 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block20 & 1023L) << 13) | (block21.TripleShift(51));
                values[valuesOffset++] = (block21.TripleShift(28)) & 8388607L;
                values[valuesOffset++] = (block21.TripleShift(5)) & 8388607L;
                long block22 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block21 & 31L) << 18) | (block22.TripleShift(46));
                values[valuesOffset++] = (block22.TripleShift(23)) & 8388607L;
                values[valuesOffset++] = block22 & 8388607L;
            }
        }

        public override void Decode(byte[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long byte0 = blocks[blocksOffset++] & 0xFF;
                long byte1 = blocks[blocksOffset++] & 0xFF;
                long byte2 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = (byte0 << 15) | (byte1 << 7) | (byte2.TripleShift(1));
                long byte3 = blocks[blocksOffset++] & 0xFF;
                long byte4 = blocks[blocksOffset++] & 0xFF;
                long byte5 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte2 & 1) << 22) | (byte3 << 14) | (byte4 << 6) | (byte5.TripleShift(2));
                long byte6 = blocks[blocksOffset++] & 0xFF;
                long byte7 = blocks[blocksOffset++] & 0xFF;
                long byte8 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte5 & 3) << 21) | (byte6 << 13) | (byte7 << 5) | (byte8.TripleShift(3));
                long byte9 = blocks[blocksOffset++] & 0xFF;
                long byte10 = blocks[blocksOffset++] & 0xFF;
                long byte11 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte8 & 7) << 20) | (byte9 << 12) | (byte10 << 4) | (byte11.TripleShift(4));
                long byte12 = blocks[blocksOffset++] & 0xFF;
                long byte13 = blocks[blocksOffset++] & 0xFF;
                long byte14 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte11 & 15) << 19) | (byte12 << 11) | (byte13 << 3) | (byte14.TripleShift(5));
                long byte15 = blocks[blocksOffset++] & 0xFF;
                long byte16 = blocks[blocksOffset++] & 0xFF;
                long byte17 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte14 & 31) << 18) | (byte15 << 10) | (byte16 << 2) | (byte17.TripleShift(6));
                long byte18 = blocks[blocksOffset++] & 0xFF;
                long byte19 = blocks[blocksOffset++] & 0xFF;
                long byte20 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte17 & 63) << 17) | (byte18 << 9) | (byte19 << 1) | (byte20.TripleShift(7));
                long byte21 = blocks[blocksOffset++] & 0xFF;
                long byte22 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte20 & 127) << 16) | (byte21 << 8) | byte22;
            }
        }
    }
}