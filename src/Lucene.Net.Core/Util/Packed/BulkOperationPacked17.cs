// this file has been automatically generated, DO NOT EDIT

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
                values[valuesOffset++] = (int)((long)((ulong)block0 >> 47));
                values[valuesOffset++] = (int)(((long)((ulong)block0 >> 30)) & 131071L);
                values[valuesOffset++] = (int)(((long)((ulong)block0 >> 13)) & 131071L);
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block0 & 8191L) << 4) | ((long)((ulong)block1 >> 60)));
                values[valuesOffset++] = (int)(((long)((ulong)block1 >> 43)) & 131071L);
                values[valuesOffset++] = (int)(((long)((ulong)block1 >> 26)) & 131071L);
                values[valuesOffset++] = (int)(((long)((ulong)block1 >> 9)) & 131071L);
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block1 & 511L) << 8) | ((long)((ulong)block2 >> 56)));
                values[valuesOffset++] = (int)(((long)((ulong)block2 >> 39)) & 131071L);
                values[valuesOffset++] = (int)(((long)((ulong)block2 >> 22)) & 131071L);
                values[valuesOffset++] = (int)(((long)((ulong)block2 >> 5)) & 131071L);
                long block3 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block2 & 31L) << 12) | ((long)((ulong)block3 >> 52)));
                values[valuesOffset++] = (int)(((long)((ulong)block3 >> 35)) & 131071L);
                values[valuesOffset++] = (int)(((long)((ulong)block3 >> 18)) & 131071L);
                values[valuesOffset++] = (int)(((long)((ulong)block3 >> 1)) & 131071L);
                long block4 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block3 & 1L) << 16) | ((long)((ulong)block4 >> 48)));
                values[valuesOffset++] = (int)(((long)((ulong)block4 >> 31)) & 131071L);
                values[valuesOffset++] = (int)(((long)((ulong)block4 >> 14)) & 131071L);
                long block5 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block4 & 16383L) << 3) | ((long)((ulong)block5 >> 61)));
                values[valuesOffset++] = (int)(((long)((ulong)block5 >> 44)) & 131071L);
                values[valuesOffset++] = (int)(((long)((ulong)block5 >> 27)) & 131071L);
                values[valuesOffset++] = (int)(((long)((ulong)block5 >> 10)) & 131071L);
                long block6 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block5 & 1023L) << 7) | ((long)((ulong)block6 >> 57)));
                values[valuesOffset++] = (int)(((long)((ulong)block6 >> 40)) & 131071L);
                values[valuesOffset++] = (int)(((long)((ulong)block6 >> 23)) & 131071L);
                values[valuesOffset++] = (int)(((long)((ulong)block6 >> 6)) & 131071L);
                long block7 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block6 & 63L) << 11) | ((long)((ulong)block7 >> 53)));
                values[valuesOffset++] = (int)(((long)((ulong)block7 >> 36)) & 131071L);
                values[valuesOffset++] = (int)(((long)((ulong)block7 >> 19)) & 131071L);
                values[valuesOffset++] = (int)(((long)((ulong)block7 >> 2)) & 131071L);
                long block8 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block7 & 3L) << 15) | ((long)((ulong)block8 >> 49)));
                values[valuesOffset++] = (int)(((long)((ulong)block8 >> 32)) & 131071L);
                values[valuesOffset++] = (int)(((long)((ulong)block8 >> 15)) & 131071L);
                long block9 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block8 & 32767L) << 2) | ((long)((ulong)block9 >> 62)));
                values[valuesOffset++] = (int)(((long)((ulong)block9 >> 45)) & 131071L);
                values[valuesOffset++] = (int)(((long)((ulong)block9 >> 28)) & 131071L);
                values[valuesOffset++] = (int)(((long)((ulong)block9 >> 11)) & 131071L);
                long block10 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block9 & 2047L) << 6) | ((long)((ulong)block10 >> 58)));
                values[valuesOffset++] = (int)(((long)((ulong)block10 >> 41)) & 131071L);
                values[valuesOffset++] = (int)(((long)((ulong)block10 >> 24)) & 131071L);
                values[valuesOffset++] = (int)(((long)((ulong)block10 >> 7)) & 131071L);
                long block11 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block10 & 127L) << 10) | ((long)((ulong)block11 >> 54)));
                values[valuesOffset++] = (int)(((long)((ulong)block11 >> 37)) & 131071L);
                values[valuesOffset++] = (int)(((long)((ulong)block11 >> 20)) & 131071L);
                values[valuesOffset++] = (int)(((long)((ulong)block11 >> 3)) & 131071L);
                long block12 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block11 & 7L) << 14) | ((long)((ulong)block12 >> 50)));
                values[valuesOffset++] = (int)(((long)((ulong)block12 >> 33)) & 131071L);
                values[valuesOffset++] = (int)(((long)((ulong)block12 >> 16)) & 131071L);
                long block13 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block12 & 65535L) << 1) | ((long)((ulong)block13 >> 63)));
                values[valuesOffset++] = (int)(((long)((ulong)block13 >> 46)) & 131071L);
                values[valuesOffset++] = (int)(((long)((ulong)block13 >> 29)) & 131071L);
                values[valuesOffset++] = (int)(((long)((ulong)block13 >> 12)) & 131071L);
                long block14 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block13 & 4095L) << 5) | ((long)((ulong)block14 >> 59)));
                values[valuesOffset++] = (int)(((long)((ulong)block14 >> 42)) & 131071L);
                values[valuesOffset++] = (int)(((long)((ulong)block14 >> 25)) & 131071L);
                values[valuesOffset++] = (int)(((long)((ulong)block14 >> 8)) & 131071L);
                long block15 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block14 & 255L) << 9) | ((long)((ulong)block15 >> 55)));
                values[valuesOffset++] = (int)(((long)((ulong)block15 >> 38)) & 131071L);
                values[valuesOffset++] = (int)(((long)((ulong)block15 >> 21)) & 131071L);
                values[valuesOffset++] = (int)(((long)((ulong)block15 >> 4)) & 131071L);
                long block16 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block15 & 15L) << 13) | ((long)((ulong)block16 >> 51)));
                values[valuesOffset++] = (int)(((long)((ulong)block16 >> 34)) & 131071L);
                values[valuesOffset++] = (int)(((long)((ulong)block16 >> 17)) & 131071L);
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
                values[valuesOffset++] = (byte0 << 9) | (byte1 << 1) | ((int)((uint)byte2 >> 7));
                int byte3 = blocks[blocksOffset++] & 0xFF;
                int byte4 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte2 & 127) << 10) | (byte3 << 2) | ((int)((uint)byte4 >> 6));
                int byte5 = blocks[blocksOffset++] & 0xFF;
                int byte6 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte4 & 63) << 11) | (byte5 << 3) | ((int)((uint)byte6 >> 5));
                int byte7 = blocks[blocksOffset++] & 0xFF;
                int byte8 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte6 & 31) << 12) | (byte7 << 4) | ((int)((uint)byte8 >> 4));
                int byte9 = blocks[blocksOffset++] & 0xFF;
                int byte10 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte8 & 15) << 13) | (byte9 << 5) | ((int)((uint)byte10 >> 3));
                int byte11 = blocks[blocksOffset++] & 0xFF;
                int byte12 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte10 & 7) << 14) | (byte11 << 6) | ((int)((uint)byte12 >> 2));
                int byte13 = blocks[blocksOffset++] & 0xFF;
                int byte14 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte12 & 3) << 15) | (byte13 << 7) | ((int)((uint)byte14 >> 1));
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
                values[valuesOffset++] = (long)((ulong)block0 >> 47);
                values[valuesOffset++] = ((long)((ulong)block0 >> 30)) & 131071L;
                values[valuesOffset++] = ((long)((ulong)block0 >> 13)) & 131071L;
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block0 & 8191L) << 4) | ((long)((ulong)block1 >> 60));
                values[valuesOffset++] = ((long)((ulong)block1 >> 43)) & 131071L;
                values[valuesOffset++] = ((long)((ulong)block1 >> 26)) & 131071L;
                values[valuesOffset++] = ((long)((ulong)block1 >> 9)) & 131071L;
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block1 & 511L) << 8) | ((long)((ulong)block2 >> 56));
                values[valuesOffset++] = ((long)((ulong)block2 >> 39)) & 131071L;
                values[valuesOffset++] = ((long)((ulong)block2 >> 22)) & 131071L;
                values[valuesOffset++] = ((long)((ulong)block2 >> 5)) & 131071L;
                long block3 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block2 & 31L) << 12) | ((long)((ulong)block3 >> 52));
                values[valuesOffset++] = ((long)((ulong)block3 >> 35)) & 131071L;
                values[valuesOffset++] = ((long)((ulong)block3 >> 18)) & 131071L;
                values[valuesOffset++] = ((long)((ulong)block3 >> 1)) & 131071L;
                long block4 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block3 & 1L) << 16) | ((long)((ulong)block4 >> 48));
                values[valuesOffset++] = ((long)((ulong)block4 >> 31)) & 131071L;
                values[valuesOffset++] = ((long)((ulong)block4 >> 14)) & 131071L;
                long block5 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block4 & 16383L) << 3) | ((long)((ulong)block5 >> 61));
                values[valuesOffset++] = ((long)((ulong)block5 >> 44)) & 131071L;
                values[valuesOffset++] = ((long)((ulong)block5 >> 27)) & 131071L;
                values[valuesOffset++] = ((long)((ulong)block5 >> 10)) & 131071L;
                long block6 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block5 & 1023L) << 7) | ((long)((ulong)block6 >> 57));
                values[valuesOffset++] = ((long)((ulong)block6 >> 40)) & 131071L;
                values[valuesOffset++] = ((long)((ulong)block6 >> 23)) & 131071L;
                values[valuesOffset++] = ((long)((ulong)block6 >> 6)) & 131071L;
                long block7 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block6 & 63L) << 11) | ((long)((ulong)block7 >> 53));
                values[valuesOffset++] = ((long)((ulong)block7 >> 36)) & 131071L;
                values[valuesOffset++] = ((long)((ulong)block7 >> 19)) & 131071L;
                values[valuesOffset++] = ((long)((ulong)block7 >> 2)) & 131071L;
                long block8 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block7 & 3L) << 15) | ((long)((ulong)block8 >> 49));
                values[valuesOffset++] = ((long)((ulong)block8 >> 32)) & 131071L;
                values[valuesOffset++] = ((long)((ulong)block8 >> 15)) & 131071L;
                long block9 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block8 & 32767L) << 2) | ((long)((ulong)block9 >> 62));
                values[valuesOffset++] = ((long)((ulong)block9 >> 45)) & 131071L;
                values[valuesOffset++] = ((long)((ulong)block9 >> 28)) & 131071L;
                values[valuesOffset++] = ((long)((ulong)block9 >> 11)) & 131071L;
                long block10 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block9 & 2047L) << 6) | ((long)((ulong)block10 >> 58));
                values[valuesOffset++] = ((long)((ulong)block10 >> 41)) & 131071L;
                values[valuesOffset++] = ((long)((ulong)block10 >> 24)) & 131071L;
                values[valuesOffset++] = ((long)((ulong)block10 >> 7)) & 131071L;
                long block11 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block10 & 127L) << 10) | ((long)((ulong)block11 >> 54));
                values[valuesOffset++] = ((long)((ulong)block11 >> 37)) & 131071L;
                values[valuesOffset++] = ((long)((ulong)block11 >> 20)) & 131071L;
                values[valuesOffset++] = ((long)((ulong)block11 >> 3)) & 131071L;
                long block12 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block11 & 7L) << 14) | ((long)((ulong)block12 >> 50));
                values[valuesOffset++] = ((long)((ulong)block12 >> 33)) & 131071L;
                values[valuesOffset++] = ((long)((ulong)block12 >> 16)) & 131071L;
                long block13 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block12 & 65535L) << 1) | ((long)((ulong)block13 >> 63));
                values[valuesOffset++] = ((long)((ulong)block13 >> 46)) & 131071L;
                values[valuesOffset++] = ((long)((ulong)block13 >> 29)) & 131071L;
                values[valuesOffset++] = ((long)((ulong)block13 >> 12)) & 131071L;
                long block14 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block13 & 4095L) << 5) | ((long)((ulong)block14 >> 59));
                values[valuesOffset++] = ((long)((ulong)block14 >> 42)) & 131071L;
                values[valuesOffset++] = ((long)((ulong)block14 >> 25)) & 131071L;
                values[valuesOffset++] = ((long)((ulong)block14 >> 8)) & 131071L;
                long block15 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block14 & 255L) << 9) | ((long)((ulong)block15 >> 55));
                values[valuesOffset++] = ((long)((ulong)block15 >> 38)) & 131071L;
                values[valuesOffset++] = ((long)((ulong)block15 >> 21)) & 131071L;
                values[valuesOffset++] = ((long)((ulong)block15 >> 4)) & 131071L;
                long block16 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block15 & 15L) << 13) | ((long)((ulong)block16 >> 51));
                values[valuesOffset++] = ((long)((ulong)block16 >> 34)) & 131071L;
                values[valuesOffset++] = ((long)((ulong)block16 >> 17)) & 131071L;
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
                values[valuesOffset++] = (byte0 << 9) | (byte1 << 1) | ((long)((ulong)byte2 >> 7));
                long byte3 = blocks[blocksOffset++] & 0xFF;
                long byte4 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte2 & 127) << 10) | (byte3 << 2) | ((long)((ulong)byte4 >> 6));
                long byte5 = blocks[blocksOffset++] & 0xFF;
                long byte6 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte4 & 63) << 11) | (byte5 << 3) | ((long)((ulong)byte6 >> 5));
                long byte7 = blocks[blocksOffset++] & 0xFF;
                long byte8 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte6 & 31) << 12) | (byte7 << 4) | ((long)((ulong)byte8 >> 4));
                long byte9 = blocks[blocksOffset++] & 0xFF;
                long byte10 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte8 & 15) << 13) | (byte9 << 5) | ((long)((ulong)byte10 >> 3));
                long byte11 = blocks[blocksOffset++] & 0xFF;
                long byte12 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte10 & 7) << 14) | (byte11 << 6) | ((long)((ulong)byte12 >> 2));
                long byte13 = blocks[blocksOffset++] & 0xFF;
                long byte14 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte12 & 3) << 15) | (byte13 << 7) | ((long)((ulong)byte14 >> 1));
                long byte15 = blocks[blocksOffset++] & 0xFF;
                long byte16 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte14 & 1) << 16) | (byte15 << 8) | byte16;
            }
        }
    }
}