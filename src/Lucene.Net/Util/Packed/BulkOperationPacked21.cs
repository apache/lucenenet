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
    internal sealed class BulkOperationPacked21 : BulkOperationPacked
    {
        public BulkOperationPacked21()
            : base(21)
        {
        }

        public override void Decode(long[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block0 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(block0.TripleShift(43));
                values[valuesOffset++] = (int)((block0.TripleShift(22)) & 2097151L);
                values[valuesOffset++] = (int)((block0.TripleShift(1)) & 2097151L);
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block0 & 1L) << 20) | (block1.TripleShift(44)));
                values[valuesOffset++] = (int)((block1.TripleShift(23)) & 2097151L);
                values[valuesOffset++] = (int)((block1.TripleShift(2)) & 2097151L);
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block1 & 3L) << 19) | (block2.TripleShift(45)));
                values[valuesOffset++] = (int)((block2.TripleShift(24)) & 2097151L);
                values[valuesOffset++] = (int)((block2.TripleShift(3)) & 2097151L);
                long block3 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block2 & 7L) << 18) | (block3.TripleShift(46)));
                values[valuesOffset++] = (int)((block3.TripleShift(25)) & 2097151L);
                values[valuesOffset++] = (int)((block3.TripleShift(4)) & 2097151L);
                long block4 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block3 & 15L) << 17) | (block4.TripleShift(47)));
                values[valuesOffset++] = (int)((block4.TripleShift(26)) & 2097151L);
                values[valuesOffset++] = (int)((block4.TripleShift(5)) & 2097151L);
                long block5 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block4 & 31L) << 16) | (block5.TripleShift(48)));
                values[valuesOffset++] = (int)((block5.TripleShift(27)) & 2097151L);
                values[valuesOffset++] = (int)((block5.TripleShift(6)) & 2097151L);
                long block6 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block5 & 63L) << 15) | (block6.TripleShift(49)));
                values[valuesOffset++] = (int)((block6.TripleShift(28)) & 2097151L);
                values[valuesOffset++] = (int)((block6.TripleShift(7)) & 2097151L);
                long block7 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block6 & 127L) << 14) | (block7.TripleShift(50)));
                values[valuesOffset++] = (int)((block7.TripleShift(29)) & 2097151L);
                values[valuesOffset++] = (int)((block7.TripleShift(8)) & 2097151L);
                long block8 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block7 & 255L) << 13) | (block8.TripleShift(51)));
                values[valuesOffset++] = (int)((block8.TripleShift(30)) & 2097151L);
                values[valuesOffset++] = (int)((block8.TripleShift(9)) & 2097151L);
                long block9 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block8 & 511L) << 12) | (block9.TripleShift(52)));
                values[valuesOffset++] = (int)((block9.TripleShift(31)) & 2097151L);
                values[valuesOffset++] = (int)((block9.TripleShift(10)) & 2097151L);
                long block10 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block9 & 1023L) << 11) | (block10.TripleShift(53)));
                values[valuesOffset++] = (int)((block10.TripleShift(32)) & 2097151L);
                values[valuesOffset++] = (int)((block10.TripleShift(11)) & 2097151L);
                long block11 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block10 & 2047L) << 10) | (block11.TripleShift(54)));
                values[valuesOffset++] = (int)((block11.TripleShift(33)) & 2097151L);
                values[valuesOffset++] = (int)((block11.TripleShift(12)) & 2097151L);
                long block12 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block11 & 4095L) << 9) | (block12.TripleShift(55)));
                values[valuesOffset++] = (int)((block12.TripleShift(34)) & 2097151L);
                values[valuesOffset++] = (int)((block12.TripleShift(13)) & 2097151L);
                long block13 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block12 & 8191L) << 8) | (block13.TripleShift(56)));
                values[valuesOffset++] = (int)((block13.TripleShift(35)) & 2097151L);
                values[valuesOffset++] = (int)((block13.TripleShift(14)) & 2097151L);
                long block14 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block13 & 16383L) << 7) | (block14.TripleShift(57)));
                values[valuesOffset++] = (int)((block14.TripleShift(36)) & 2097151L);
                values[valuesOffset++] = (int)((block14.TripleShift(15)) & 2097151L);
                long block15 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block14 & 32767L) << 6) | (block15.TripleShift(58)));
                values[valuesOffset++] = (int)((block15.TripleShift(37)) & 2097151L);
                values[valuesOffset++] = (int)((block15.TripleShift(16)) & 2097151L);
                long block16 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block15 & 65535L) << 5) | (block16.TripleShift(59)));
                values[valuesOffset++] = (int)((block16.TripleShift(38)) & 2097151L);
                values[valuesOffset++] = (int)((block16.TripleShift(17)) & 2097151L);
                long block17 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block16 & 131071L) << 4) | (block17.TripleShift(60)));
                values[valuesOffset++] = (int)((block17.TripleShift(39)) & 2097151L);
                values[valuesOffset++] = (int)((block17.TripleShift(18)) & 2097151L);
                long block18 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block17 & 262143L) << 3) | (block18.TripleShift(61)));
                values[valuesOffset++] = (int)((block18.TripleShift(40)) & 2097151L);
                values[valuesOffset++] = (int)((block18.TripleShift(19)) & 2097151L);
                long block19 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block18 & 524287L) << 2) | (block19.TripleShift(62)));
                values[valuesOffset++] = (int)((block19.TripleShift(41)) & 2097151L);
                values[valuesOffset++] = (int)((block19.TripleShift(20)) & 2097151L);
                long block20 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block19 & 1048575L) << 1) | (block20.TripleShift(63)));
                values[valuesOffset++] = (int)((block20.TripleShift(42)) & 2097151L);
                values[valuesOffset++] = (int)((block20.TripleShift(21)) & 2097151L);
                values[valuesOffset++] = (int)(block20 & 2097151L);
            }
        }

        public override void Decode(byte[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                int byte0 = blocks[blocksOffset++] & 0xFF;
                int byte1 = blocks[blocksOffset++] & 0xFF;
                int byte2 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = (byte0 << 13) | (byte1 << 5) | (byte2.TripleShift(3));
                int byte3 = blocks[blocksOffset++] & 0xFF;
                int byte4 = blocks[blocksOffset++] & 0xFF;
                int byte5 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte2 & 7) << 18) | (byte3 << 10) | (byte4 << 2) | (byte5.TripleShift(6));
                int byte6 = blocks[blocksOffset++] & 0xFF;
                int byte7 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte5 & 63) << 15) | (byte6 << 7) | (byte7.TripleShift(1));
                int byte8 = blocks[blocksOffset++] & 0xFF;
                int byte9 = blocks[blocksOffset++] & 0xFF;
                int byte10 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte7 & 1) << 20) | (byte8 << 12) | (byte9 << 4) | (byte10.TripleShift(4));
                int byte11 = blocks[blocksOffset++] & 0xFF;
                int byte12 = blocks[blocksOffset++] & 0xFF;
                int byte13 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte10 & 15) << 17) | (byte11 << 9) | (byte12 << 1) | (byte13.TripleShift(7));
                int byte14 = blocks[blocksOffset++] & 0xFF;
                int byte15 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte13 & 127) << 14) | (byte14 << 6) | (byte15.TripleShift(2));
                int byte16 = blocks[blocksOffset++] & 0xFF;
                int byte17 = blocks[blocksOffset++] & 0xFF;
                int byte18 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte15 & 3) << 19) | (byte16 << 11) | (byte17 << 3) | (byte18.TripleShift(5));
                int byte19 = blocks[blocksOffset++] & 0xFF;
                int byte20 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte18 & 31) << 16) | (byte19 << 8) | byte20;
            }
        }

        public override void Decode(long[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block0 = blocks[blocksOffset++];
                values[valuesOffset++] = block0.TripleShift(43);
                values[valuesOffset++] = (block0.TripleShift(22)) & 2097151L;
                values[valuesOffset++] = (block0.TripleShift(1)) & 2097151L;
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block0 & 1L) << 20) | (block1.TripleShift(44));
                values[valuesOffset++] = (block1.TripleShift(23)) & 2097151L;
                values[valuesOffset++] = (block1.TripleShift(2)) & 2097151L;
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block1 & 3L) << 19) | (block2.TripleShift(45));
                values[valuesOffset++] = (block2.TripleShift(24)) & 2097151L;
                values[valuesOffset++] = (block2.TripleShift(3)) & 2097151L;
                long block3 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block2 & 7L) << 18) | (block3.TripleShift(46));
                values[valuesOffset++] = (block3.TripleShift(25)) & 2097151L;
                values[valuesOffset++] = (block3.TripleShift(4)) & 2097151L;
                long block4 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block3 & 15L) << 17) | (block4.TripleShift(47));
                values[valuesOffset++] = (block4.TripleShift(26)) & 2097151L;
                values[valuesOffset++] = (block4.TripleShift(5)) & 2097151L;
                long block5 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block4 & 31L) << 16) | (block5.TripleShift(48));
                values[valuesOffset++] = (block5.TripleShift(27)) & 2097151L;
                values[valuesOffset++] = (block5.TripleShift(6)) & 2097151L;
                long block6 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block5 & 63L) << 15) | (block6.TripleShift(49));
                values[valuesOffset++] = (block6.TripleShift(28)) & 2097151L;
                values[valuesOffset++] = (block6.TripleShift(7)) & 2097151L;
                long block7 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block6 & 127L) << 14) | (block7.TripleShift(50));
                values[valuesOffset++] = (block7.TripleShift(29)) & 2097151L;
                values[valuesOffset++] = (block7.TripleShift(8)) & 2097151L;
                long block8 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block7 & 255L) << 13) | (block8.TripleShift(51));
                values[valuesOffset++] = (block8.TripleShift(30)) & 2097151L;
                values[valuesOffset++] = (block8.TripleShift(9)) & 2097151L;
                long block9 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block8 & 511L) << 12) | (block9.TripleShift(52));
                values[valuesOffset++] = (block9.TripleShift(31)) & 2097151L;
                values[valuesOffset++] = (block9.TripleShift(10)) & 2097151L;
                long block10 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block9 & 1023L) << 11) | (block10.TripleShift(53));
                values[valuesOffset++] = (block10.TripleShift(32)) & 2097151L;
                values[valuesOffset++] = (block10.TripleShift(11)) & 2097151L;
                long block11 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block10 & 2047L) << 10) | (block11.TripleShift(54));
                values[valuesOffset++] = (block11.TripleShift(33)) & 2097151L;
                values[valuesOffset++] = (block11.TripleShift(12)) & 2097151L;
                long block12 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block11 & 4095L) << 9) | (block12.TripleShift(55));
                values[valuesOffset++] = (block12.TripleShift(34)) & 2097151L;
                values[valuesOffset++] = (block12.TripleShift(13)) & 2097151L;
                long block13 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block12 & 8191L) << 8) | (block13.TripleShift(56));
                values[valuesOffset++] = (block13.TripleShift(35)) & 2097151L;
                values[valuesOffset++] = (block13.TripleShift(14)) & 2097151L;
                long block14 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block13 & 16383L) << 7) | (block14.TripleShift(57));
                values[valuesOffset++] = (block14.TripleShift(36)) & 2097151L;
                values[valuesOffset++] = (block14.TripleShift(15)) & 2097151L;
                long block15 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block14 & 32767L) << 6) | (block15.TripleShift(58));
                values[valuesOffset++] = (block15.TripleShift(37)) & 2097151L;
                values[valuesOffset++] = (block15.TripleShift(16)) & 2097151L;
                long block16 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block15 & 65535L) << 5) | (block16.TripleShift(59));
                values[valuesOffset++] = (block16.TripleShift(38)) & 2097151L;
                values[valuesOffset++] = (block16.TripleShift(17)) & 2097151L;
                long block17 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block16 & 131071L) << 4) | (block17.TripleShift(60));
                values[valuesOffset++] = (block17.TripleShift(39)) & 2097151L;
                values[valuesOffset++] = (block17.TripleShift(18)) & 2097151L;
                long block18 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block17 & 262143L) << 3) | (block18.TripleShift(61));
                values[valuesOffset++] = (block18.TripleShift(40)) & 2097151L;
                values[valuesOffset++] = (block18.TripleShift(19)) & 2097151L;
                long block19 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block18 & 524287L) << 2) | (block19.TripleShift(62));
                values[valuesOffset++] = (block19.TripleShift(41)) & 2097151L;
                values[valuesOffset++] = (block19.TripleShift(20)) & 2097151L;
                long block20 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block19 & 1048575L) << 1) | (block20.TripleShift(63));
                values[valuesOffset++] = (block20.TripleShift(42)) & 2097151L;
                values[valuesOffset++] = (block20.TripleShift(21)) & 2097151L;
                values[valuesOffset++] = block20 & 2097151L;
            }
        }

        public override void Decode(byte[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long byte0 = blocks[blocksOffset++] & 0xFF;
                long byte1 = blocks[blocksOffset++] & 0xFF;
                long byte2 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = (byte0 << 13) | (byte1 << 5) | (byte2.TripleShift(3));
                long byte3 = blocks[blocksOffset++] & 0xFF;
                long byte4 = blocks[blocksOffset++] & 0xFF;
                long byte5 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte2 & 7) << 18) | (byte3 << 10) | (byte4 << 2) | (byte5.TripleShift(6));
                long byte6 = blocks[blocksOffset++] & 0xFF;
                long byte7 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte5 & 63) << 15) | (byte6 << 7) | (byte7.TripleShift(1));
                long byte8 = blocks[blocksOffset++] & 0xFF;
                long byte9 = blocks[blocksOffset++] & 0xFF;
                long byte10 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte7 & 1) << 20) | (byte8 << 12) | (byte9 << 4) | (byte10.TripleShift(4));
                long byte11 = blocks[blocksOffset++] & 0xFF;
                long byte12 = blocks[blocksOffset++] & 0xFF;
                long byte13 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte10 & 15) << 17) | (byte11 << 9) | (byte12 << 1) | (byte13.TripleShift(7));
                long byte14 = blocks[blocksOffset++] & 0xFF;
                long byte15 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte13 & 127) << 14) | (byte14 << 6) | (byte15.TripleShift(2));
                long byte16 = blocks[blocksOffset++] & 0xFF;
                long byte17 = blocks[blocksOffset++] & 0xFF;
                long byte18 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte15 & 3) << 19) | (byte16 << 11) | (byte17 << 3) | (byte18.TripleShift(5));
                long byte19 = blocks[blocksOffset++] & 0xFF;
                long byte20 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte18 & 31) << 16) | (byte19 << 8) | byte20;
            }
        }
    }
}