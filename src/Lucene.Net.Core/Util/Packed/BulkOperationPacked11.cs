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
                values[valuesOffset++] = (int)((long)((ulong)block0 >> 53));
                values[valuesOffset++] = (int)(((long)((ulong)block0 >> 42)) & 2047L);
                values[valuesOffset++] = (int)(((long)((ulong)block0 >> 31)) & 2047L);
                values[valuesOffset++] = (int)(((long)((ulong)block0 >> 20)) & 2047L);
                values[valuesOffset++] = (int)(((long)((ulong)block0 >> 9)) & 2047L);
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block0 & 511L) << 2) | ((long)((ulong)block1 >> 62)));
                values[valuesOffset++] = (int)(((long)((ulong)block1 >> 51)) & 2047L);
                values[valuesOffset++] = (int)(((long)((ulong)block1 >> 40)) & 2047L);
                values[valuesOffset++] = (int)(((long)((ulong)block1 >> 29)) & 2047L);
                values[valuesOffset++] = (int)(((long)((ulong)block1 >> 18)) & 2047L);
                values[valuesOffset++] = (int)(((long)((ulong)block1 >> 7)) & 2047L);
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block1 & 127L) << 4) | ((long)((ulong)block2 >> 60)));
                values[valuesOffset++] = (int)(((long)((ulong)block2 >> 49)) & 2047L);
                values[valuesOffset++] = (int)(((long)((ulong)block2 >> 38)) & 2047L);
                values[valuesOffset++] = (int)(((long)((ulong)block2 >> 27)) & 2047L);
                values[valuesOffset++] = (int)(((long)((ulong)block2 >> 16)) & 2047L);
                values[valuesOffset++] = (int)(((long)((ulong)block2 >> 5)) & 2047L);
                long block3 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block2 & 31L) << 6) | ((long)((ulong)block3 >> 58)));
                values[valuesOffset++] = (int)(((long)((ulong)block3 >> 47)) & 2047L);
                values[valuesOffset++] = (int)(((long)((ulong)block3 >> 36)) & 2047L);
                values[valuesOffset++] = (int)(((long)((ulong)block3 >> 25)) & 2047L);
                values[valuesOffset++] = (int)(((long)((ulong)block3 >> 14)) & 2047L);
                values[valuesOffset++] = (int)(((long)((ulong)block3 >> 3)) & 2047L);
                long block4 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block3 & 7L) << 8) | ((long)((ulong)block4 >> 56)));
                values[valuesOffset++] = (int)(((long)((ulong)block4 >> 45)) & 2047L);
                values[valuesOffset++] = (int)(((long)((ulong)block4 >> 34)) & 2047L);
                values[valuesOffset++] = (int)(((long)((ulong)block4 >> 23)) & 2047L);
                values[valuesOffset++] = (int)(((long)((ulong)block4 >> 12)) & 2047L);
                values[valuesOffset++] = (int)(((long)((ulong)block4 >> 1)) & 2047L);
                long block5 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block4 & 1L) << 10) | ((long)((ulong)block5 >> 54)));
                values[valuesOffset++] = (int)(((long)((ulong)block5 >> 43)) & 2047L);
                values[valuesOffset++] = (int)(((long)((ulong)block5 >> 32)) & 2047L);
                values[valuesOffset++] = (int)(((long)((ulong)block5 >> 21)) & 2047L);
                values[valuesOffset++] = (int)(((long)((ulong)block5 >> 10)) & 2047L);
                long block6 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block5 & 1023L) << 1) | ((long)((ulong)block6 >> 63)));
                values[valuesOffset++] = (int)(((long)((ulong)block6 >> 52)) & 2047L);
                values[valuesOffset++] = (int)(((long)((ulong)block6 >> 41)) & 2047L);
                values[valuesOffset++] = (int)(((long)((ulong)block6 >> 30)) & 2047L);
                values[valuesOffset++] = (int)(((long)((ulong)block6 >> 19)) & 2047L);
                values[valuesOffset++] = (int)(((long)((ulong)block6 >> 8)) & 2047L);
                long block7 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block6 & 255L) << 3) | ((long)((ulong)block7 >> 61)));
                values[valuesOffset++] = (int)(((long)((ulong)block7 >> 50)) & 2047L);
                values[valuesOffset++] = (int)(((long)((ulong)block7 >> 39)) & 2047L);
                values[valuesOffset++] = (int)(((long)((ulong)block7 >> 28)) & 2047L);
                values[valuesOffset++] = (int)(((long)((ulong)block7 >> 17)) & 2047L);
                values[valuesOffset++] = (int)(((long)((ulong)block7 >> 6)) & 2047L);
                long block8 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block7 & 63L) << 5) | ((long)((ulong)block8 >> 59)));
                values[valuesOffset++] = (int)(((long)((ulong)block8 >> 48)) & 2047L);
                values[valuesOffset++] = (int)(((long)((ulong)block8 >> 37)) & 2047L);
                values[valuesOffset++] = (int)(((long)((ulong)block8 >> 26)) & 2047L);
                values[valuesOffset++] = (int)(((long)((ulong)block8 >> 15)) & 2047L);
                values[valuesOffset++] = (int)(((long)((ulong)block8 >> 4)) & 2047L);
                long block9 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block8 & 15L) << 7) | ((long)((ulong)block9 >> 57)));
                values[valuesOffset++] = (int)(((long)((ulong)block9 >> 46)) & 2047L);
                values[valuesOffset++] = (int)(((long)((ulong)block9 >> 35)) & 2047L);
                values[valuesOffset++] = (int)(((long)((ulong)block9 >> 24)) & 2047L);
                values[valuesOffset++] = (int)(((long)((ulong)block9 >> 13)) & 2047L);
                values[valuesOffset++] = (int)(((long)((ulong)block9 >> 2)) & 2047L);
                long block10 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block9 & 3L) << 9) | ((long)((ulong)block10 >> 55)));
                values[valuesOffset++] = (int)(((long)((ulong)block10 >> 44)) & 2047L);
                values[valuesOffset++] = (int)(((long)((ulong)block10 >> 33)) & 2047L);
                values[valuesOffset++] = (int)(((long)((ulong)block10 >> 22)) & 2047L);
                values[valuesOffset++] = (int)(((long)((ulong)block10 >> 11)) & 2047L);
                values[valuesOffset++] = (int)(block10 & 2047L);
            }
        }

        public override void Decode(byte[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                int byte0 = blocks[blocksOffset++] & 0xFF;
                int byte1 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = (byte0 << 3) | ((int)((uint)byte1 >> 5));
                int byte2 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte1 & 31) << 6) | ((int)((uint)byte2 >> 2));
                int byte3 = blocks[blocksOffset++] & 0xFF;
                int byte4 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte2 & 3) << 9) | (byte3 << 1) | ((int)((uint)byte4 >> 7));
                int byte5 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte4 & 127) << 4) | ((int)((uint)byte5 >> 4));
                int byte6 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte5 & 15) << 7) | ((int)((uint)byte6 >> 1));
                int byte7 = blocks[blocksOffset++] & 0xFF;
                int byte8 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte6 & 1) << 10) | (byte7 << 2) | ((int)((uint)byte8 >> 6));
                int byte9 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte8 & 63) << 5) | ((int)((uint)byte9 >> 3));
                int byte10 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte9 & 7) << 8) | byte10;
            }
        }

        public override void Decode(long[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block0 = blocks[blocksOffset++];
                values[valuesOffset++] = (long)((ulong)block0 >> 53);
                values[valuesOffset++] = ((long)((ulong)block0 >> 42)) & 2047L;
                values[valuesOffset++] = ((long)((ulong)block0 >> 31)) & 2047L;
                values[valuesOffset++] = ((long)((ulong)block0 >> 20)) & 2047L;
                values[valuesOffset++] = ((long)((ulong)block0 >> 9)) & 2047L;
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block0 & 511L) << 2) | ((long)((ulong)block1 >> 62));
                values[valuesOffset++] = ((long)((ulong)block1 >> 51)) & 2047L;
                values[valuesOffset++] = ((long)((ulong)block1 >> 40)) & 2047L;
                values[valuesOffset++] = ((long)((ulong)block1 >> 29)) & 2047L;
                values[valuesOffset++] = ((long)((ulong)block1 >> 18)) & 2047L;
                values[valuesOffset++] = ((long)((ulong)block1 >> 7)) & 2047L;
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block1 & 127L) << 4) | ((long)((ulong)block2 >> 60));
                values[valuesOffset++] = ((long)((ulong)block2 >> 49)) & 2047L;
                values[valuesOffset++] = ((long)((ulong)block2 >> 38)) & 2047L;
                values[valuesOffset++] = ((long)((ulong)block2 >> 27)) & 2047L;
                values[valuesOffset++] = ((long)((ulong)block2 >> 16)) & 2047L;
                values[valuesOffset++] = ((long)((ulong)block2 >> 5)) & 2047L;
                long block3 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block2 & 31L) << 6) | ((long)((ulong)block3 >> 58));
                values[valuesOffset++] = ((long)((ulong)block3 >> 47)) & 2047L;
                values[valuesOffset++] = ((long)((ulong)block3 >> 36)) & 2047L;
                values[valuesOffset++] = ((long)((ulong)block3 >> 25)) & 2047L;
                values[valuesOffset++] = ((long)((ulong)block3 >> 14)) & 2047L;
                values[valuesOffset++] = ((long)((ulong)block3 >> 3)) & 2047L;
                long block4 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block3 & 7L) << 8) | ((long)((ulong)block4 >> 56));
                values[valuesOffset++] = ((long)((ulong)block4 >> 45)) & 2047L;
                values[valuesOffset++] = ((long)((ulong)block4 >> 34)) & 2047L;
                values[valuesOffset++] = ((long)((ulong)block4 >> 23)) & 2047L;
                values[valuesOffset++] = ((long)((ulong)block4 >> 12)) & 2047L;
                values[valuesOffset++] = ((long)((ulong)block4 >> 1)) & 2047L;
                long block5 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block4 & 1L) << 10) | ((long)((ulong)block5 >> 54));
                values[valuesOffset++] = ((long)((ulong)block5 >> 43)) & 2047L;
                values[valuesOffset++] = ((long)((ulong)block5 >> 32)) & 2047L;
                values[valuesOffset++] = ((long)((ulong)block5 >> 21)) & 2047L;
                values[valuesOffset++] = ((long)((ulong)block5 >> 10)) & 2047L;
                long block6 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block5 & 1023L) << 1) | ((long)((ulong)block6 >> 63));
                values[valuesOffset++] = ((long)((ulong)block6 >> 52)) & 2047L;
                values[valuesOffset++] = ((long)((ulong)block6 >> 41)) & 2047L;
                values[valuesOffset++] = ((long)((ulong)block6 >> 30)) & 2047L;
                values[valuesOffset++] = ((long)((ulong)block6 >> 19)) & 2047L;
                values[valuesOffset++] = ((long)((ulong)block6 >> 8)) & 2047L;
                long block7 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block6 & 255L) << 3) | ((long)((ulong)block7 >> 61));
                values[valuesOffset++] = ((long)((ulong)block7 >> 50)) & 2047L;
                values[valuesOffset++] = ((long)((ulong)block7 >> 39)) & 2047L;
                values[valuesOffset++] = ((long)((ulong)block7 >> 28)) & 2047L;
                values[valuesOffset++] = ((long)((ulong)block7 >> 17)) & 2047L;
                values[valuesOffset++] = ((long)((ulong)block7 >> 6)) & 2047L;
                long block8 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block7 & 63L) << 5) | ((long)((ulong)block8 >> 59));
                values[valuesOffset++] = ((long)((ulong)block8 >> 48)) & 2047L;
                values[valuesOffset++] = ((long)((ulong)block8 >> 37)) & 2047L;
                values[valuesOffset++] = ((long)((ulong)block8 >> 26)) & 2047L;
                values[valuesOffset++] = ((long)((ulong)block8 >> 15)) & 2047L;
                values[valuesOffset++] = ((long)((ulong)block8 >> 4)) & 2047L;
                long block9 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block8 & 15L) << 7) | ((long)((ulong)block9 >> 57));
                values[valuesOffset++] = ((long)((ulong)block9 >> 46)) & 2047L;
                values[valuesOffset++] = ((long)((ulong)block9 >> 35)) & 2047L;
                values[valuesOffset++] = ((long)((ulong)block9 >> 24)) & 2047L;
                values[valuesOffset++] = ((long)((ulong)block9 >> 13)) & 2047L;
                values[valuesOffset++] = ((long)((ulong)block9 >> 2)) & 2047L;
                long block10 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block9 & 3L) << 9) | ((long)((ulong)block10 >> 55));
                values[valuesOffset++] = ((long)((ulong)block10 >> 44)) & 2047L;
                values[valuesOffset++] = ((long)((ulong)block10 >> 33)) & 2047L;
                values[valuesOffset++] = ((long)((ulong)block10 >> 22)) & 2047L;
                values[valuesOffset++] = ((long)((ulong)block10 >> 11)) & 2047L;
                values[valuesOffset++] = block10 & 2047L;
            }
        }

        public override void Decode(byte[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long byte0 = blocks[blocksOffset++] & 0xFF;
                long byte1 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = (byte0 << 3) | ((long)((ulong)byte1 >> 5));
                long byte2 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte1 & 31) << 6) | ((long)((ulong)byte2 >> 2));
                long byte3 = blocks[blocksOffset++] & 0xFF;
                long byte4 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte2 & 3) << 9) | (byte3 << 1) | ((long)((ulong)byte4 >> 7));
                long byte5 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte4 & 127) << 4) | ((long)((ulong)byte5 >> 4));
                long byte6 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte5 & 15) << 7) | ((long)((ulong)byte6 >> 1));
                long byte7 = blocks[blocksOffset++] & 0xFF;
                long byte8 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte6 & 1) << 10) | (byte7 << 2) | ((long)((ulong)byte8 >> 6));
                long byte9 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte8 & 63) << 5) | ((long)((ulong)byte9 >> 3));
                long byte10 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte9 & 7) << 8) | byte10;
            }
        }
    }
}