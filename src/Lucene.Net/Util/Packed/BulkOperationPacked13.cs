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
                values[valuesOffset++] = (int)((long)((ulong)block0 >> 51));
                values[valuesOffset++] = (int)(((long)((ulong)block0 >> 38)) & 8191L);
                values[valuesOffset++] = (int)(((long)((ulong)block0 >> 25)) & 8191L);
                values[valuesOffset++] = (int)(((long)((ulong)block0 >> 12)) & 8191L);
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block0 & 4095L) << 1) | ((long)((ulong)block1 >> 63)));
                values[valuesOffset++] = (int)(((long)((ulong)block1 >> 50)) & 8191L);
                values[valuesOffset++] = (int)(((long)((ulong)block1 >> 37)) & 8191L);
                values[valuesOffset++] = (int)(((long)((ulong)block1 >> 24)) & 8191L);
                values[valuesOffset++] = (int)(((long)((ulong)block1 >> 11)) & 8191L);
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block1 & 2047L) << 2) | ((long)((ulong)block2 >> 62)));
                values[valuesOffset++] = (int)(((long)((ulong)block2 >> 49)) & 8191L);
                values[valuesOffset++] = (int)(((long)((ulong)block2 >> 36)) & 8191L);
                values[valuesOffset++] = (int)(((long)((ulong)block2 >> 23)) & 8191L);
                values[valuesOffset++] = (int)(((long)((ulong)block2 >> 10)) & 8191L);
                long block3 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block2 & 1023L) << 3) | ((long)((ulong)block3 >> 61)));
                values[valuesOffset++] = (int)(((long)((ulong)block3 >> 48)) & 8191L);
                values[valuesOffset++] = (int)(((long)((ulong)block3 >> 35)) & 8191L);
                values[valuesOffset++] = (int)(((long)((ulong)block3 >> 22)) & 8191L);
                values[valuesOffset++] = (int)(((long)((ulong)block3 >> 9)) & 8191L);
                long block4 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block3 & 511L) << 4) | ((long)((ulong)block4 >> 60)));
                values[valuesOffset++] = (int)(((long)((ulong)block4 >> 47)) & 8191L);
                values[valuesOffset++] = (int)(((long)((ulong)block4 >> 34)) & 8191L);
                values[valuesOffset++] = (int)(((long)((ulong)block4 >> 21)) & 8191L);
                values[valuesOffset++] = (int)(((long)((ulong)block4 >> 8)) & 8191L);
                long block5 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block4 & 255L) << 5) | ((long)((ulong)block5 >> 59)));
                values[valuesOffset++] = (int)(((long)((ulong)block5 >> 46)) & 8191L);
                values[valuesOffset++] = (int)(((long)((ulong)block5 >> 33)) & 8191L);
                values[valuesOffset++] = (int)(((long)((ulong)block5 >> 20)) & 8191L);
                values[valuesOffset++] = (int)(((long)((ulong)block5 >> 7)) & 8191L);
                long block6 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block5 & 127L) << 6) | ((long)((ulong)block6 >> 58)));
                values[valuesOffset++] = (int)(((long)((ulong)block6 >> 45)) & 8191L);
                values[valuesOffset++] = (int)(((long)((ulong)block6 >> 32)) & 8191L);
                values[valuesOffset++] = (int)(((long)((ulong)block6 >> 19)) & 8191L);
                values[valuesOffset++] = (int)(((long)((ulong)block6 >> 6)) & 8191L);
                long block7 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block6 & 63L) << 7) | ((long)((ulong)block7 >> 57)));
                values[valuesOffset++] = (int)(((long)((ulong)block7 >> 44)) & 8191L);
                values[valuesOffset++] = (int)(((long)((ulong)block7 >> 31)) & 8191L);
                values[valuesOffset++] = (int)(((long)((ulong)block7 >> 18)) & 8191L);
                values[valuesOffset++] = (int)(((long)((ulong)block7 >> 5)) & 8191L);
                long block8 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block7 & 31L) << 8) | ((long)((ulong)block8 >> 56)));
                values[valuesOffset++] = (int)(((long)((ulong)block8 >> 43)) & 8191L);
                values[valuesOffset++] = (int)(((long)((ulong)block8 >> 30)) & 8191L);
                values[valuesOffset++] = (int)(((long)((ulong)block8 >> 17)) & 8191L);
                values[valuesOffset++] = (int)(((long)((ulong)block8 >> 4)) & 8191L);
                long block9 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block8 & 15L) << 9) | ((long)((ulong)block9 >> 55)));
                values[valuesOffset++] = (int)(((long)((ulong)block9 >> 42)) & 8191L);
                values[valuesOffset++] = (int)(((long)((ulong)block9 >> 29)) & 8191L);
                values[valuesOffset++] = (int)(((long)((ulong)block9 >> 16)) & 8191L);
                values[valuesOffset++] = (int)(((long)((ulong)block9 >> 3)) & 8191L);
                long block10 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block9 & 7L) << 10) | ((long)((ulong)block10 >> 54)));
                values[valuesOffset++] = (int)(((long)((ulong)block10 >> 41)) & 8191L);
                values[valuesOffset++] = (int)(((long)((ulong)block10 >> 28)) & 8191L);
                values[valuesOffset++] = (int)(((long)((ulong)block10 >> 15)) & 8191L);
                values[valuesOffset++] = (int)(((long)((ulong)block10 >> 2)) & 8191L);
                long block11 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block10 & 3L) << 11) | ((long)((ulong)block11 >> 53)));
                values[valuesOffset++] = (int)(((long)((ulong)block11 >> 40)) & 8191L);
                values[valuesOffset++] = (int)(((long)((ulong)block11 >> 27)) & 8191L);
                values[valuesOffset++] = (int)(((long)((ulong)block11 >> 14)) & 8191L);
                values[valuesOffset++] = (int)(((long)((ulong)block11 >> 1)) & 8191L);
                long block12 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block11 & 1L) << 12) | ((long)((ulong)block12 >> 52)));
                values[valuesOffset++] = (int)(((long)((ulong)block12 >> 39)) & 8191L);
                values[valuesOffset++] = (int)(((long)((ulong)block12 >> 26)) & 8191L);
                values[valuesOffset++] = (int)(((long)((ulong)block12 >> 13)) & 8191L);
                values[valuesOffset++] = (int)(block12 & 8191L);
            }
        }

        public override void Decode(byte[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                int byte0 = blocks[blocksOffset++] & 0xFF;
                int byte1 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = (byte0 << 5) | ((int)((uint)byte1 >> 3));
                int byte2 = blocks[blocksOffset++] & 0xFF;
                int byte3 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte1 & 7) << 10) | (byte2 << 2) | ((int)((uint)byte3 >> 6));
                int byte4 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte3 & 63) << 7) | ((int)((uint)byte4 >> 1));
                int byte5 = blocks[blocksOffset++] & 0xFF;
                int byte6 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte4 & 1) << 12) | (byte5 << 4) | ((int)((uint)byte6 >> 4));
                int byte7 = blocks[blocksOffset++] & 0xFF;
                int byte8 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte6 & 15) << 9) | (byte7 << 1) | ((int)((uint)byte8 >> 7));
                int byte9 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte8 & 127) << 6) | ((int)((uint)byte9 >> 2));
                int byte10 = blocks[blocksOffset++] & 0xFF;
                int byte11 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte9 & 3) << 11) | (byte10 << 3) | ((int)((uint)byte11 >> 5));
                int byte12 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte11 & 31) << 8) | byte12;
            }
        }

        public override void Decode(long[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block0 = blocks[blocksOffset++];
                values[valuesOffset++] = (long)((ulong)block0 >> 51);
                values[valuesOffset++] = ((long)((ulong)block0 >> 38)) & 8191L;
                values[valuesOffset++] = ((long)((ulong)block0 >> 25)) & 8191L;
                values[valuesOffset++] = ((long)((ulong)block0 >> 12)) & 8191L;
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block0 & 4095L) << 1) | ((long)((ulong)block1 >> 63));
                values[valuesOffset++] = ((long)((ulong)block1 >> 50)) & 8191L;
                values[valuesOffset++] = ((long)((ulong)block1 >> 37)) & 8191L;
                values[valuesOffset++] = ((long)((ulong)block1 >> 24)) & 8191L;
                values[valuesOffset++] = ((long)((ulong)block1 >> 11)) & 8191L;
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block1 & 2047L) << 2) | ((long)((ulong)block2 >> 62));
                values[valuesOffset++] = ((long)((ulong)block2 >> 49)) & 8191L;
                values[valuesOffset++] = ((long)((ulong)block2 >> 36)) & 8191L;
                values[valuesOffset++] = ((long)((ulong)block2 >> 23)) & 8191L;
                values[valuesOffset++] = ((long)((ulong)block2 >> 10)) & 8191L;
                long block3 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block2 & 1023L) << 3) | ((long)((ulong)block3 >> 61));
                values[valuesOffset++] = ((long)((ulong)block3 >> 48)) & 8191L;
                values[valuesOffset++] = ((long)((ulong)block3 >> 35)) & 8191L;
                values[valuesOffset++] = ((long)((ulong)block3 >> 22)) & 8191L;
                values[valuesOffset++] = ((long)((ulong)block3 >> 9)) & 8191L;
                long block4 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block3 & 511L) << 4) | ((long)((ulong)block4 >> 60));
                values[valuesOffset++] = ((long)((ulong)block4 >> 47)) & 8191L;
                values[valuesOffset++] = ((long)((ulong)block4 >> 34)) & 8191L;
                values[valuesOffset++] = ((long)((ulong)block4 >> 21)) & 8191L;
                values[valuesOffset++] = ((long)((ulong)block4 >> 8)) & 8191L;
                long block5 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block4 & 255L) << 5) | ((long)((ulong)block5 >> 59));
                values[valuesOffset++] = ((long)((ulong)block5 >> 46)) & 8191L;
                values[valuesOffset++] = ((long)((ulong)block5 >> 33)) & 8191L;
                values[valuesOffset++] = ((long)((ulong)block5 >> 20)) & 8191L;
                values[valuesOffset++] = ((long)((ulong)block5 >> 7)) & 8191L;
                long block6 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block5 & 127L) << 6) | ((long)((ulong)block6 >> 58));
                values[valuesOffset++] = ((long)((ulong)block6 >> 45)) & 8191L;
                values[valuesOffset++] = ((long)((ulong)block6 >> 32)) & 8191L;
                values[valuesOffset++] = ((long)((ulong)block6 >> 19)) & 8191L;
                values[valuesOffset++] = ((long)((ulong)block6 >> 6)) & 8191L;
                long block7 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block6 & 63L) << 7) | ((long)((ulong)block7 >> 57));
                values[valuesOffset++] = ((long)((ulong)block7 >> 44)) & 8191L;
                values[valuesOffset++] = ((long)((ulong)block7 >> 31)) & 8191L;
                values[valuesOffset++] = ((long)((ulong)block7 >> 18)) & 8191L;
                values[valuesOffset++] = ((long)((ulong)block7 >> 5)) & 8191L;
                long block8 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block7 & 31L) << 8) | ((long)((ulong)block8 >> 56));
                values[valuesOffset++] = ((long)((ulong)block8 >> 43)) & 8191L;
                values[valuesOffset++] = ((long)((ulong)block8 >> 30)) & 8191L;
                values[valuesOffset++] = ((long)((ulong)block8 >> 17)) & 8191L;
                values[valuesOffset++] = ((long)((ulong)block8 >> 4)) & 8191L;
                long block9 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block8 & 15L) << 9) | ((long)((ulong)block9 >> 55));
                values[valuesOffset++] = ((long)((ulong)block9 >> 42)) & 8191L;
                values[valuesOffset++] = ((long)((ulong)block9 >> 29)) & 8191L;
                values[valuesOffset++] = ((long)((ulong)block9 >> 16)) & 8191L;
                values[valuesOffset++] = ((long)((ulong)block9 >> 3)) & 8191L;
                long block10 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block9 & 7L) << 10) | ((long)((ulong)block10 >> 54));
                values[valuesOffset++] = ((long)((ulong)block10 >> 41)) & 8191L;
                values[valuesOffset++] = ((long)((ulong)block10 >> 28)) & 8191L;
                values[valuesOffset++] = ((long)((ulong)block10 >> 15)) & 8191L;
                values[valuesOffset++] = ((long)((ulong)block10 >> 2)) & 8191L;
                long block11 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block10 & 3L) << 11) | ((long)((ulong)block11 >> 53));
                values[valuesOffset++] = ((long)((ulong)block11 >> 40)) & 8191L;
                values[valuesOffset++] = ((long)((ulong)block11 >> 27)) & 8191L;
                values[valuesOffset++] = ((long)((ulong)block11 >> 14)) & 8191L;
                values[valuesOffset++] = ((long)((ulong)block11 >> 1)) & 8191L;
                long block12 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block11 & 1L) << 12) | ((long)((ulong)block12 >> 52));
                values[valuesOffset++] = ((long)((ulong)block12 >> 39)) & 8191L;
                values[valuesOffset++] = ((long)((ulong)block12 >> 26)) & 8191L;
                values[valuesOffset++] = ((long)((ulong)block12 >> 13)) & 8191L;
                values[valuesOffset++] = block12 & 8191L;
            }
        }

        public override void Decode(byte[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long byte0 = blocks[blocksOffset++] & 0xFF;
                long byte1 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = (byte0 << 5) | ((long)((ulong)byte1 >> 3));
                long byte2 = blocks[blocksOffset++] & 0xFF;
                long byte3 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte1 & 7) << 10) | (byte2 << 2) | ((long)((ulong)byte3 >> 6));
                long byte4 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte3 & 63) << 7) | ((long)((ulong)byte4 >> 1));
                long byte5 = blocks[blocksOffset++] & 0xFF;
                long byte6 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte4 & 1) << 12) | (byte5 << 4) | ((long)((ulong)byte6 >> 4));
                long byte7 = blocks[blocksOffset++] & 0xFF;
                long byte8 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte6 & 15) << 9) | (byte7 << 1) | ((long)((ulong)byte8 >> 7));
                long byte9 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte8 & 127) << 6) | ((long)((ulong)byte9 >> 2));
                long byte10 = blocks[blocksOffset++] & 0xFF;
                long byte11 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte9 & 3) << 11) | (byte10 << 3) | ((long)((ulong)byte11 >> 5));
                long byte12 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte11 & 31) << 8) | byte12;
            }
        }
    }
}