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
    internal sealed class BulkOperationPacked7 : BulkOperationPacked
    {
        public BulkOperationPacked7()
            : base(7)
        {
        }

        public override void Decode(long[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block0 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)((long)((ulong)block0 >> 57));
                values[valuesOffset++] = (int)(((long)((ulong)block0 >> 50)) & 127L);
                values[valuesOffset++] = (int)(((long)((ulong)block0 >> 43)) & 127L);
                values[valuesOffset++] = (int)(((long)((ulong)block0 >> 36)) & 127L);
                values[valuesOffset++] = (int)(((long)((ulong)block0 >> 29)) & 127L);
                values[valuesOffset++] = (int)(((long)((ulong)block0 >> 22)) & 127L);
                values[valuesOffset++] = (int)(((long)((ulong)block0 >> 15)) & 127L);
                values[valuesOffset++] = (int)(((long)((ulong)block0 >> 8)) & 127L);
                values[valuesOffset++] = (int)(((long)((ulong)block0 >> 1)) & 127L);
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block0 & 1L) << 6) | ((long)((ulong)block1 >> 58)));
                values[valuesOffset++] = (int)(((long)((ulong)block1 >> 51)) & 127L);
                values[valuesOffset++] = (int)(((long)((ulong)block1 >> 44)) & 127L);
                values[valuesOffset++] = (int)(((long)((ulong)block1 >> 37)) & 127L);
                values[valuesOffset++] = (int)(((long)((ulong)block1 >> 30)) & 127L);
                values[valuesOffset++] = (int)(((long)((ulong)block1 >> 23)) & 127L);
                values[valuesOffset++] = (int)(((long)((ulong)block1 >> 16)) & 127L);
                values[valuesOffset++] = (int)(((long)((ulong)block1 >> 9)) & 127L);
                values[valuesOffset++] = (int)(((long)((ulong)block1 >> 2)) & 127L);
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block1 & 3L) << 5) | ((long)((ulong)block2 >> 59)));
                values[valuesOffset++] = (int)(((long)((ulong)block2 >> 52)) & 127L);
                values[valuesOffset++] = (int)(((long)((ulong)block2 >> 45)) & 127L);
                values[valuesOffset++] = (int)(((long)((ulong)block2 >> 38)) & 127L);
                values[valuesOffset++] = (int)(((long)((ulong)block2 >> 31)) & 127L);
                values[valuesOffset++] = (int)(((long)((ulong)block2 >> 24)) & 127L);
                values[valuesOffset++] = (int)(((long)((ulong)block2 >> 17)) & 127L);
                values[valuesOffset++] = (int)(((long)((ulong)block2 >> 10)) & 127L);
                values[valuesOffset++] = (int)(((long)((ulong)block2 >> 3)) & 127L);
                long block3 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block2 & 7L) << 4) | ((long)((ulong)block3 >> 60)));
                values[valuesOffset++] = (int)(((long)((ulong)block3 >> 53)) & 127L);
                values[valuesOffset++] = (int)(((long)((ulong)block3 >> 46)) & 127L);
                values[valuesOffset++] = (int)(((long)((ulong)block3 >> 39)) & 127L);
                values[valuesOffset++] = (int)(((long)((ulong)block3 >> 32)) & 127L);
                values[valuesOffset++] = (int)(((long)((ulong)block3 >> 25)) & 127L);
                values[valuesOffset++] = (int)(((long)((ulong)block3 >> 18)) & 127L);
                values[valuesOffset++] = (int)(((long)((ulong)block3 >> 11)) & 127L);
                values[valuesOffset++] = (int)(((long)((ulong)block3 >> 4)) & 127L);
                long block4 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block3 & 15L) << 3) | ((long)((ulong)block4 >> 61)));
                values[valuesOffset++] = (int)(((long)((ulong)block4 >> 54)) & 127L);
                values[valuesOffset++] = (int)(((long)((ulong)block4 >> 47)) & 127L);
                values[valuesOffset++] = (int)(((long)((ulong)block4 >> 40)) & 127L);
                values[valuesOffset++] = (int)(((long)((ulong)block4 >> 33)) & 127L);
                values[valuesOffset++] = (int)(((long)((ulong)block4 >> 26)) & 127L);
                values[valuesOffset++] = (int)(((long)((ulong)block4 >> 19)) & 127L);
                values[valuesOffset++] = (int)(((long)((ulong)block4 >> 12)) & 127L);
                values[valuesOffset++] = (int)(((long)((ulong)block4 >> 5)) & 127L);
                long block5 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block4 & 31L) << 2) | ((long)((ulong)block5 >> 62)));
                values[valuesOffset++] = (int)(((long)((ulong)block5 >> 55)) & 127L);
                values[valuesOffset++] = (int)(((long)((ulong)block5 >> 48)) & 127L);
                values[valuesOffset++] = (int)(((long)((ulong)block5 >> 41)) & 127L);
                values[valuesOffset++] = (int)(((long)((ulong)block5 >> 34)) & 127L);
                values[valuesOffset++] = (int)(((long)((ulong)block5 >> 27)) & 127L);
                values[valuesOffset++] = (int)(((long)((ulong)block5 >> 20)) & 127L);
                values[valuesOffset++] = (int)(((long)((ulong)block5 >> 13)) & 127L);
                values[valuesOffset++] = (int)(((long)((ulong)block5 >> 6)) & 127L);
                long block6 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block5 & 63L) << 1) | ((long)((ulong)block6 >> 63)));
                values[valuesOffset++] = (int)(((long)((ulong)block6 >> 56)) & 127L);
                values[valuesOffset++] = (int)(((long)((ulong)block6 >> 49)) & 127L);
                values[valuesOffset++] = (int)(((long)((ulong)block6 >> 42)) & 127L);
                values[valuesOffset++] = (int)(((long)((ulong)block6 >> 35)) & 127L);
                values[valuesOffset++] = (int)(((long)((ulong)block6 >> 28)) & 127L);
                values[valuesOffset++] = (int)(((long)((ulong)block6 >> 21)) & 127L);
                values[valuesOffset++] = (int)(((long)((ulong)block6 >> 14)) & 127L);
                values[valuesOffset++] = (int)(((long)((ulong)block6 >> 7)) & 127L);
                values[valuesOffset++] = (int)(block6 & 127L);
            }
        }

        public override void Decode(byte[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                int byte0 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = (int)((uint)byte0 >> 1);
                int byte1 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte0 & 1) << 6) | ((int)((uint)byte1 >> 2));
                int byte2 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte1 & 3) << 5) | ((int)((uint)byte2 >> 3));
                int byte3 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte2 & 7) << 4) | ((int)((uint)byte3 >> 4));
                int byte4 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte3 & 15) << 3) | ((int)((uint)byte4 >> 5));
                int byte5 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte4 & 31) << 2) | ((int)((uint)byte5 >> 6));
                int byte6 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte5 & 63) << 1) | ((int)((uint)byte6 >> 7));
                values[valuesOffset++] = byte6 & 127;
            }
        }

        public override void Decode(long[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block0 = blocks[blocksOffset++];
                values[valuesOffset++] = (long)((ulong)block0 >> 57);
                values[valuesOffset++] = ((long)((ulong)block0 >> 50)) & 127L;
                values[valuesOffset++] = ((long)((ulong)block0 >> 43)) & 127L;
                values[valuesOffset++] = ((long)((ulong)block0 >> 36)) & 127L;
                values[valuesOffset++] = ((long)((ulong)block0 >> 29)) & 127L;
                values[valuesOffset++] = ((long)((ulong)block0 >> 22)) & 127L;
                values[valuesOffset++] = ((long)((ulong)block0 >> 15)) & 127L;
                values[valuesOffset++] = ((long)((ulong)block0 >> 8)) & 127L;
                values[valuesOffset++] = ((long)((ulong)block0 >> 1)) & 127L;
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block0 & 1L) << 6) | ((long)((ulong)block1 >> 58));
                values[valuesOffset++] = ((long)((ulong)block1 >> 51)) & 127L;
                values[valuesOffset++] = ((long)((ulong)block1 >> 44)) & 127L;
                values[valuesOffset++] = ((long)((ulong)block1 >> 37)) & 127L;
                values[valuesOffset++] = ((long)((ulong)block1 >> 30)) & 127L;
                values[valuesOffset++] = ((long)((ulong)block1 >> 23)) & 127L;
                values[valuesOffset++] = ((long)((ulong)block1 >> 16)) & 127L;
                values[valuesOffset++] = ((long)((ulong)block1 >> 9)) & 127L;
                values[valuesOffset++] = ((long)((ulong)block1 >> 2)) & 127L;
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block1 & 3L) << 5) | ((long)((ulong)block2 >> 59));
                values[valuesOffset++] = ((long)((ulong)block2 >> 52)) & 127L;
                values[valuesOffset++] = ((long)((ulong)block2 >> 45)) & 127L;
                values[valuesOffset++] = ((long)((ulong)block2 >> 38)) & 127L;
                values[valuesOffset++] = ((long)((ulong)block2 >> 31)) & 127L;
                values[valuesOffset++] = ((long)((ulong)block2 >> 24)) & 127L;
                values[valuesOffset++] = ((long)((ulong)block2 >> 17)) & 127L;
                values[valuesOffset++] = ((long)((ulong)block2 >> 10)) & 127L;
                values[valuesOffset++] = ((long)((ulong)block2 >> 3)) & 127L;
                long block3 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block2 & 7L) << 4) | ((long)((ulong)block3 >> 60));
                values[valuesOffset++] = ((long)((ulong)block3 >> 53)) & 127L;
                values[valuesOffset++] = ((long)((ulong)block3 >> 46)) & 127L;
                values[valuesOffset++] = ((long)((ulong)block3 >> 39)) & 127L;
                values[valuesOffset++] = ((long)((ulong)block3 >> 32)) & 127L;
                values[valuesOffset++] = ((long)((ulong)block3 >> 25)) & 127L;
                values[valuesOffset++] = ((long)((ulong)block3 >> 18)) & 127L;
                values[valuesOffset++] = ((long)((ulong)block3 >> 11)) & 127L;
                values[valuesOffset++] = ((long)((ulong)block3 >> 4)) & 127L;
                long block4 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block3 & 15L) << 3) | ((long)((ulong)block4 >> 61));
                values[valuesOffset++] = ((long)((ulong)block4 >> 54)) & 127L;
                values[valuesOffset++] = ((long)((ulong)block4 >> 47)) & 127L;
                values[valuesOffset++] = ((long)((ulong)block4 >> 40)) & 127L;
                values[valuesOffset++] = ((long)((ulong)block4 >> 33)) & 127L;
                values[valuesOffset++] = ((long)((ulong)block4 >> 26)) & 127L;
                values[valuesOffset++] = ((long)((ulong)block4 >> 19)) & 127L;
                values[valuesOffset++] = ((long)((ulong)block4 >> 12)) & 127L;
                values[valuesOffset++] = ((long)((ulong)block4 >> 5)) & 127L;
                long block5 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block4 & 31L) << 2) | ((long)((ulong)block5 >> 62));
                values[valuesOffset++] = ((long)((ulong)block5 >> 55)) & 127L;
                values[valuesOffset++] = ((long)((ulong)block5 >> 48)) & 127L;
                values[valuesOffset++] = ((long)((ulong)block5 >> 41)) & 127L;
                values[valuesOffset++] = ((long)((ulong)block5 >> 34)) & 127L;
                values[valuesOffset++] = ((long)((ulong)block5 >> 27)) & 127L;
                values[valuesOffset++] = ((long)((ulong)block5 >> 20)) & 127L;
                values[valuesOffset++] = ((long)((ulong)block5 >> 13)) & 127L;
                values[valuesOffset++] = ((long)((ulong)block5 >> 6)) & 127L;
                long block6 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block5 & 63L) << 1) | ((long)((ulong)block6 >> 63));
                values[valuesOffset++] = ((long)((ulong)block6 >> 56)) & 127L;
                values[valuesOffset++] = ((long)((ulong)block6 >> 49)) & 127L;
                values[valuesOffset++] = ((long)((ulong)block6 >> 42)) & 127L;
                values[valuesOffset++] = ((long)((ulong)block6 >> 35)) & 127L;
                values[valuesOffset++] = ((long)((ulong)block6 >> 28)) & 127L;
                values[valuesOffset++] = ((long)((ulong)block6 >> 21)) & 127L;
                values[valuesOffset++] = ((long)((ulong)block6 >> 14)) & 127L;
                values[valuesOffset++] = ((long)((ulong)block6 >> 7)) & 127L;
                values[valuesOffset++] = block6 & 127L;
            }
        }

        public override void Decode(byte[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long byte0 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = (long)((ulong)byte0 >> 1);
                long byte1 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte0 & 1) << 6) | ((long)((ulong)byte1 >> 2));
                long byte2 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte1 & 3) << 5) | ((long)((ulong)byte2 >> 3));
                long byte3 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte2 & 7) << 4) | ((long)((ulong)byte3 >> 4));
                long byte4 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte3 & 15) << 3) | ((long)((ulong)byte4 >> 5));
                long byte5 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte4 & 31) << 2) | ((long)((ulong)byte5 >> 6));
                long byte6 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte5 & 63) << 1) | ((long)((ulong)byte6 >> 7));
                values[valuesOffset++] = byte6 & 127;
            }
        }
    }
}