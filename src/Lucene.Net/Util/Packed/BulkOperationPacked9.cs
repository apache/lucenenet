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
    internal sealed class BulkOperationPacked9 : BulkOperationPacked
    {
        public BulkOperationPacked9()
            : base(9)
        {
        }

        public override void Decode(long[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block0 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)((long)((ulong)block0 >> 55));
                values[valuesOffset++] = (int)(((long)((ulong)block0 >> 46)) & 511L);
                values[valuesOffset++] = (int)(((long)((ulong)block0 >> 37)) & 511L);
                values[valuesOffset++] = (int)(((long)((ulong)block0 >> 28)) & 511L);
                values[valuesOffset++] = (int)(((long)((ulong)block0 >> 19)) & 511L);
                values[valuesOffset++] = (int)(((long)((ulong)block0 >> 10)) & 511L);
                values[valuesOffset++] = (int)(((long)((ulong)block0 >> 1)) & 511L);
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block0 & 1L) << 8) | ((long)((ulong)block1 >> 56)));
                values[valuesOffset++] = (int)(((long)((ulong)block1 >> 47)) & 511L);
                values[valuesOffset++] = (int)(((long)((ulong)block1 >> 38)) & 511L);
                values[valuesOffset++] = (int)(((long)((ulong)block1 >> 29)) & 511L);
                values[valuesOffset++] = (int)(((long)((ulong)block1 >> 20)) & 511L);
                values[valuesOffset++] = (int)(((long)((ulong)block1 >> 11)) & 511L);
                values[valuesOffset++] = (int)(((long)((ulong)block1 >> 2)) & 511L);
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block1 & 3L) << 7) | ((long)((ulong)block2 >> 57)));
                values[valuesOffset++] = (int)(((long)((ulong)block2 >> 48)) & 511L);
                values[valuesOffset++] = (int)(((long)((ulong)block2 >> 39)) & 511L);
                values[valuesOffset++] = (int)(((long)((ulong)block2 >> 30)) & 511L);
                values[valuesOffset++] = (int)(((long)((ulong)block2 >> 21)) & 511L);
                values[valuesOffset++] = (int)(((long)((ulong)block2 >> 12)) & 511L);
                values[valuesOffset++] = (int)(((long)((ulong)block2 >> 3)) & 511L);
                long block3 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block2 & 7L) << 6) | ((long)((ulong)block3 >> 58)));
                values[valuesOffset++] = (int)(((long)((ulong)block3 >> 49)) & 511L);
                values[valuesOffset++] = (int)(((long)((ulong)block3 >> 40)) & 511L);
                values[valuesOffset++] = (int)(((long)((ulong)block3 >> 31)) & 511L);
                values[valuesOffset++] = (int)(((long)((ulong)block3 >> 22)) & 511L);
                values[valuesOffset++] = (int)(((long)((ulong)block3 >> 13)) & 511L);
                values[valuesOffset++] = (int)(((long)((ulong)block3 >> 4)) & 511L);
                long block4 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block3 & 15L) << 5) | ((long)((ulong)block4 >> 59)));
                values[valuesOffset++] = (int)(((long)((ulong)block4 >> 50)) & 511L);
                values[valuesOffset++] = (int)(((long)((ulong)block4 >> 41)) & 511L);
                values[valuesOffset++] = (int)(((long)((ulong)block4 >> 32)) & 511L);
                values[valuesOffset++] = (int)(((long)((ulong)block4 >> 23)) & 511L);
                values[valuesOffset++] = (int)(((long)((ulong)block4 >> 14)) & 511L);
                values[valuesOffset++] = (int)(((long)((ulong)block4 >> 5)) & 511L);
                long block5 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block4 & 31L) << 4) | ((long)((ulong)block5 >> 60)));
                values[valuesOffset++] = (int)(((long)((ulong)block5 >> 51)) & 511L);
                values[valuesOffset++] = (int)(((long)((ulong)block5 >> 42)) & 511L);
                values[valuesOffset++] = (int)(((long)((ulong)block5 >> 33)) & 511L);
                values[valuesOffset++] = (int)(((long)((ulong)block5 >> 24)) & 511L);
                values[valuesOffset++] = (int)(((long)((ulong)block5 >> 15)) & 511L);
                values[valuesOffset++] = (int)(((long)((ulong)block5 >> 6)) & 511L);
                long block6 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block5 & 63L) << 3) | ((long)((ulong)block6 >> 61)));
                values[valuesOffset++] = (int)(((long)((ulong)block6 >> 52)) & 511L);
                values[valuesOffset++] = (int)(((long)((ulong)block6 >> 43)) & 511L);
                values[valuesOffset++] = (int)(((long)((ulong)block6 >> 34)) & 511L);
                values[valuesOffset++] = (int)(((long)((ulong)block6 >> 25)) & 511L);
                values[valuesOffset++] = (int)(((long)((ulong)block6 >> 16)) & 511L);
                values[valuesOffset++] = (int)(((long)((ulong)block6 >> 7)) & 511L);
                long block7 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block6 & 127L) << 2) | ((long)((ulong)block7 >> 62)));
                values[valuesOffset++] = (int)(((long)((ulong)block7 >> 53)) & 511L);
                values[valuesOffset++] = (int)(((long)((ulong)block7 >> 44)) & 511L);
                values[valuesOffset++] = (int)(((long)((ulong)block7 >> 35)) & 511L);
                values[valuesOffset++] = (int)(((long)((ulong)block7 >> 26)) & 511L);
                values[valuesOffset++] = (int)(((long)((ulong)block7 >> 17)) & 511L);
                values[valuesOffset++] = (int)(((long)((ulong)block7 >> 8)) & 511L);
                long block8 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block7 & 255L) << 1) | ((long)((ulong)block8 >> 63)));
                values[valuesOffset++] = (int)(((long)((ulong)block8 >> 54)) & 511L);
                values[valuesOffset++] = (int)(((long)((ulong)block8 >> 45)) & 511L);
                values[valuesOffset++] = (int)(((long)((ulong)block8 >> 36)) & 511L);
                values[valuesOffset++] = (int)(((long)((ulong)block8 >> 27)) & 511L);
                values[valuesOffset++] = (int)(((long)((ulong)block8 >> 18)) & 511L);
                values[valuesOffset++] = (int)(((long)((ulong)block8 >> 9)) & 511L);
                values[valuesOffset++] = (int)(block8 & 511L);
            }
        }

        public override void Decode(byte[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                int byte0 = blocks[blocksOffset++] & 0xFF;
                int byte1 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = (byte0 << 1) | ((int)((uint)byte1 >> 7));
                int byte2 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte1 & 127) << 2) | ((int)((uint)byte2 >> 6));
                int byte3 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte2 & 63) << 3) | ((int)((uint)byte3 >> 5));
                int byte4 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte3 & 31) << 4) | ((int)((uint)byte4 >> 4));
                int byte5 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte4 & 15) << 5) | ((int)((uint)byte5 >> 3));
                int byte6 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte5 & 7) << 6) | ((int)((uint)byte6 >> 2));
                int byte7 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte6 & 3) << 7) | ((int)((uint)byte7 >> 1));
                int byte8 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte7 & 1) << 8) | byte8;
            }
        }

        public override void Decode(long[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block0 = blocks[blocksOffset++];
                values[valuesOffset++] = (long)((ulong)block0 >> 55);
                values[valuesOffset++] = ((long)((ulong)block0 >> 46)) & 511L;
                values[valuesOffset++] = ((long)((ulong)block0 >> 37)) & 511L;
                values[valuesOffset++] = ((long)((ulong)block0 >> 28)) & 511L;
                values[valuesOffset++] = ((long)((ulong)block0 >> 19)) & 511L;
                values[valuesOffset++] = ((long)((ulong)block0 >> 10)) & 511L;
                values[valuesOffset++] = ((long)((ulong)block0 >> 1)) & 511L;
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block0 & 1L) << 8) | ((long)((ulong)block1 >> 56));
                values[valuesOffset++] = ((long)((ulong)block1 >> 47)) & 511L;
                values[valuesOffset++] = ((long)((ulong)block1 >> 38)) & 511L;
                values[valuesOffset++] = ((long)((ulong)block1 >> 29)) & 511L;
                values[valuesOffset++] = ((long)((ulong)block1 >> 20)) & 511L;
                values[valuesOffset++] = ((long)((ulong)block1 >> 11)) & 511L;
                values[valuesOffset++] = ((long)((ulong)block1 >> 2)) & 511L;
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block1 & 3L) << 7) | ((long)((ulong)block2 >> 57));
                values[valuesOffset++] = ((long)((ulong)block2 >> 48)) & 511L;
                values[valuesOffset++] = ((long)((ulong)block2 >> 39)) & 511L;
                values[valuesOffset++] = ((long)((ulong)block2 >> 30)) & 511L;
                values[valuesOffset++] = ((long)((ulong)block2 >> 21)) & 511L;
                values[valuesOffset++] = ((long)((ulong)block2 >> 12)) & 511L;
                values[valuesOffset++] = ((long)((ulong)block2 >> 3)) & 511L;
                long block3 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block2 & 7L) << 6) | ((long)((ulong)block3 >> 58));
                values[valuesOffset++] = ((long)((ulong)block3 >> 49)) & 511L;
                values[valuesOffset++] = ((long)((ulong)block3 >> 40)) & 511L;
                values[valuesOffset++] = ((long)((ulong)block3 >> 31)) & 511L;
                values[valuesOffset++] = ((long)((ulong)block3 >> 22)) & 511L;
                values[valuesOffset++] = ((long)((ulong)block3 >> 13)) & 511L;
                values[valuesOffset++] = ((long)((ulong)block3 >> 4)) & 511L;
                long block4 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block3 & 15L) << 5) | ((long)((ulong)block4 >> 59));
                values[valuesOffset++] = ((long)((ulong)block4 >> 50)) & 511L;
                values[valuesOffset++] = ((long)((ulong)block4 >> 41)) & 511L;
                values[valuesOffset++] = ((long)((ulong)block4 >> 32)) & 511L;
                values[valuesOffset++] = ((long)((ulong)block4 >> 23)) & 511L;
                values[valuesOffset++] = ((long)((ulong)block4 >> 14)) & 511L;
                values[valuesOffset++] = ((long)((ulong)block4 >> 5)) & 511L;
                long block5 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block4 & 31L) << 4) | ((long)((ulong)block5 >> 60));
                values[valuesOffset++] = ((long)((ulong)block5 >> 51)) & 511L;
                values[valuesOffset++] = ((long)((ulong)block5 >> 42)) & 511L;
                values[valuesOffset++] = ((long)((ulong)block5 >> 33)) & 511L;
                values[valuesOffset++] = ((long)((ulong)block5 >> 24)) & 511L;
                values[valuesOffset++] = ((long)((ulong)block5 >> 15)) & 511L;
                values[valuesOffset++] = ((long)((ulong)block5 >> 6)) & 511L;
                long block6 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block5 & 63L) << 3) | ((long)((ulong)block6 >> 61));
                values[valuesOffset++] = ((long)((ulong)block6 >> 52)) & 511L;
                values[valuesOffset++] = ((long)((ulong)block6 >> 43)) & 511L;
                values[valuesOffset++] = ((long)((ulong)block6 >> 34)) & 511L;
                values[valuesOffset++] = ((long)((ulong)block6 >> 25)) & 511L;
                values[valuesOffset++] = ((long)((ulong)block6 >> 16)) & 511L;
                values[valuesOffset++] = ((long)((ulong)block6 >> 7)) & 511L;
                long block7 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block6 & 127L) << 2) | ((long)((ulong)block7 >> 62));
                values[valuesOffset++] = ((long)((ulong)block7 >> 53)) & 511L;
                values[valuesOffset++] = ((long)((ulong)block7 >> 44)) & 511L;
                values[valuesOffset++] = ((long)((ulong)block7 >> 35)) & 511L;
                values[valuesOffset++] = ((long)((ulong)block7 >> 26)) & 511L;
                values[valuesOffset++] = ((long)((ulong)block7 >> 17)) & 511L;
                values[valuesOffset++] = ((long)((ulong)block7 >> 8)) & 511L;
                long block8 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block7 & 255L) << 1) | ((long)((ulong)block8 >> 63));
                values[valuesOffset++] = ((long)((ulong)block8 >> 54)) & 511L;
                values[valuesOffset++] = ((long)((ulong)block8 >> 45)) & 511L;
                values[valuesOffset++] = ((long)((ulong)block8 >> 36)) & 511L;
                values[valuesOffset++] = ((long)((ulong)block8 >> 27)) & 511L;
                values[valuesOffset++] = ((long)((ulong)block8 >> 18)) & 511L;
                values[valuesOffset++] = ((long)((ulong)block8 >> 9)) & 511L;
                values[valuesOffset++] = block8 & 511L;
            }
        }

        public override void Decode(byte[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long byte0 = blocks[blocksOffset++] & 0xFF;
                long byte1 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = (byte0 << 1) | ((long)((ulong)byte1 >> 7));
                long byte2 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte1 & 127) << 2) | ((long)((ulong)byte2 >> 6));
                long byte3 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte2 & 63) << 3) | ((long)((ulong)byte3 >> 5));
                long byte4 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte3 & 31) << 4) | ((long)((ulong)byte4 >> 4));
                long byte5 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte4 & 15) << 5) | ((long)((ulong)byte5 >> 3));
                long byte6 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte5 & 7) << 6) | ((long)((ulong)byte6 >> 2));
                long byte7 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte6 & 3) << 7) | ((long)((ulong)byte7 >> 1));
                long byte8 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte7 & 1) << 8) | byte8;
            }
        }
    }
}