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
    internal sealed class BulkOperationPacked18 : BulkOperationPacked
    {
        public BulkOperationPacked18()
            : base(18)
        {
        }

        public override void Decode(long[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block0 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)((long)((ulong)block0 >> 46));
                values[valuesOffset++] = (int)(((long)((ulong)block0 >> 28)) & 262143L);
                values[valuesOffset++] = (int)(((long)((ulong)block0 >> 10)) & 262143L);
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block0 & 1023L) << 8) | ((long)((ulong)block1 >> 56)));
                values[valuesOffset++] = (int)(((long)((ulong)block1 >> 38)) & 262143L);
                values[valuesOffset++] = (int)(((long)((ulong)block1 >> 20)) & 262143L);
                values[valuesOffset++] = (int)(((long)((ulong)block1 >> 2)) & 262143L);
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block1 & 3L) << 16) | ((long)((ulong)block2 >> 48)));
                values[valuesOffset++] = (int)(((long)((ulong)block2 >> 30)) & 262143L);
                values[valuesOffset++] = (int)(((long)((ulong)block2 >> 12)) & 262143L);
                long block3 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block2 & 4095L) << 6) | ((long)((ulong)block3 >> 58)));
                values[valuesOffset++] = (int)(((long)((ulong)block3 >> 40)) & 262143L);
                values[valuesOffset++] = (int)(((long)((ulong)block3 >> 22)) & 262143L);
                values[valuesOffset++] = (int)(((long)((ulong)block3 >> 4)) & 262143L);
                long block4 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block3 & 15L) << 14) | ((long)((ulong)block4 >> 50)));
                values[valuesOffset++] = (int)(((long)((ulong)block4 >> 32)) & 262143L);
                values[valuesOffset++] = (int)(((long)((ulong)block4 >> 14)) & 262143L);
                long block5 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block4 & 16383L) << 4) | ((long)((ulong)block5 >> 60)));
                values[valuesOffset++] = (int)(((long)((ulong)block5 >> 42)) & 262143L);
                values[valuesOffset++] = (int)(((long)((ulong)block5 >> 24)) & 262143L);
                values[valuesOffset++] = (int)(((long)((ulong)block5 >> 6)) & 262143L);
                long block6 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block5 & 63L) << 12) | ((long)((ulong)block6 >> 52)));
                values[valuesOffset++] = (int)(((long)((ulong)block6 >> 34)) & 262143L);
                values[valuesOffset++] = (int)(((long)((ulong)block6 >> 16)) & 262143L);
                long block7 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block6 & 65535L) << 2) | ((long)((ulong)block7 >> 62)));
                values[valuesOffset++] = (int)(((long)((ulong)block7 >> 44)) & 262143L);
                values[valuesOffset++] = (int)(((long)((ulong)block7 >> 26)) & 262143L);
                values[valuesOffset++] = (int)(((long)((ulong)block7 >> 8)) & 262143L);
                long block8 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block7 & 255L) << 10) | ((long)((ulong)block8 >> 54)));
                values[valuesOffset++] = (int)(((long)((ulong)block8 >> 36)) & 262143L);
                values[valuesOffset++] = (int)(((long)((ulong)block8 >> 18)) & 262143L);
                values[valuesOffset++] = (int)(block8 & 262143L);
            }
        }

        public override void Decode(byte[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                int byte0 = blocks[blocksOffset++] & 0xFF;
                int byte1 = blocks[blocksOffset++] & 0xFF;
                int byte2 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = (byte0 << 10) | (byte1 << 2) | ((int)((uint)byte2 >> 6));
                int byte3 = blocks[blocksOffset++] & 0xFF;
                int byte4 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte2 & 63) << 12) | (byte3 << 4) | ((int)((uint)byte4 >> 4));
                int byte5 = blocks[blocksOffset++] & 0xFF;
                int byte6 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte4 & 15) << 14) | (byte5 << 6) | ((int)((uint)byte6 >> 2));
                int byte7 = blocks[blocksOffset++] & 0xFF;
                int byte8 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte6 & 3) << 16) | (byte7 << 8) | byte8;
            }
        }

        public override void Decode(long[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block0 = blocks[blocksOffset++];
                values[valuesOffset++] = (long)((ulong)block0 >> 46);
                values[valuesOffset++] = ((long)((ulong)block0 >> 28)) & 262143L;
                values[valuesOffset++] = ((long)((ulong)block0 >> 10)) & 262143L;
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block0 & 1023L) << 8) | ((long)((ulong)block1 >> 56));
                values[valuesOffset++] = ((long)((ulong)block1 >> 38)) & 262143L;
                values[valuesOffset++] = ((long)((ulong)block1 >> 20)) & 262143L;
                values[valuesOffset++] = ((long)((ulong)block1 >> 2)) & 262143L;
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block1 & 3L) << 16) | ((long)((ulong)block2 >> 48));
                values[valuesOffset++] = ((long)((ulong)block2 >> 30)) & 262143L;
                values[valuesOffset++] = ((long)((ulong)block2 >> 12)) & 262143L;
                long block3 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block2 & 4095L) << 6) | ((long)((ulong)block3 >> 58));
                values[valuesOffset++] = ((long)((ulong)block3 >> 40)) & 262143L;
                values[valuesOffset++] = ((long)((ulong)block3 >> 22)) & 262143L;
                values[valuesOffset++] = ((long)((ulong)block3 >> 4)) & 262143L;
                long block4 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block3 & 15L) << 14) | ((long)((ulong)block4 >> 50));
                values[valuesOffset++] = ((long)((ulong)block4 >> 32)) & 262143L;
                values[valuesOffset++] = ((long)((ulong)block4 >> 14)) & 262143L;
                long block5 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block4 & 16383L) << 4) | ((long)((ulong)block5 >> 60));
                values[valuesOffset++] = ((long)((ulong)block5 >> 42)) & 262143L;
                values[valuesOffset++] = ((long)((ulong)block5 >> 24)) & 262143L;
                values[valuesOffset++] = ((long)((ulong)block5 >> 6)) & 262143L;
                long block6 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block5 & 63L) << 12) | ((long)((ulong)block6 >> 52));
                values[valuesOffset++] = ((long)((ulong)block6 >> 34)) & 262143L;
                values[valuesOffset++] = ((long)((ulong)block6 >> 16)) & 262143L;
                long block7 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block6 & 65535L) << 2) | ((long)((ulong)block7 >> 62));
                values[valuesOffset++] = ((long)((ulong)block7 >> 44)) & 262143L;
                values[valuesOffset++] = ((long)((ulong)block7 >> 26)) & 262143L;
                values[valuesOffset++] = ((long)((ulong)block7 >> 8)) & 262143L;
                long block8 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block7 & 255L) << 10) | ((long)((ulong)block8 >> 54));
                values[valuesOffset++] = ((long)((ulong)block8 >> 36)) & 262143L;
                values[valuesOffset++] = ((long)((ulong)block8 >> 18)) & 262143L;
                values[valuesOffset++] = block8 & 262143L;
            }
        }

        public override void Decode(byte[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long byte0 = blocks[blocksOffset++] & 0xFF;
                long byte1 = blocks[blocksOffset++] & 0xFF;
                long byte2 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = (byte0 << 10) | (byte1 << 2) | ((long)((ulong)byte2 >> 6));
                long byte3 = blocks[blocksOffset++] & 0xFF;
                long byte4 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte2 & 63) << 12) | (byte3 << 4) | ((long)((ulong)byte4 >> 4));
                long byte5 = blocks[blocksOffset++] & 0xFF;
                long byte6 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte4 & 15) << 14) | (byte5 << 6) | ((long)((ulong)byte6 >> 2));
                long byte7 = blocks[blocksOffset++] & 0xFF;
                long byte8 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte6 & 3) << 16) | (byte7 << 8) | byte8;
            }
        }
    }
}