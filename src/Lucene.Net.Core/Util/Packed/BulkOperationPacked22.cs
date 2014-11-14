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
    internal sealed class BulkOperationPacked22 : BulkOperationPacked
    {
        public BulkOperationPacked22()
            : base(22)
        {
        }

        public override void Decode(long[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block0 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)((long)((ulong)block0 >> 42));
                values[valuesOffset++] = (int)(((long)((ulong)block0 >> 20)) & 4194303L);
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block0 & 1048575L) << 2) | ((long)((ulong)block1 >> 62)));
                values[valuesOffset++] = (int)(((long)((ulong)block1 >> 40)) & 4194303L);
                values[valuesOffset++] = (int)(((long)((ulong)block1 >> 18)) & 4194303L);
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block1 & 262143L) << 4) | ((long)((ulong)block2 >> 60)));
                values[valuesOffset++] = (int)(((long)((ulong)block2 >> 38)) & 4194303L);
                values[valuesOffset++] = (int)(((long)((ulong)block2 >> 16)) & 4194303L);
                long block3 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block2 & 65535L) << 6) | ((long)((ulong)block3 >> 58)));
                values[valuesOffset++] = (int)(((long)((ulong)block3 >> 36)) & 4194303L);
                values[valuesOffset++] = (int)(((long)((ulong)block3 >> 14)) & 4194303L);
                long block4 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block3 & 16383L) << 8) | ((long)((ulong)block4 >> 56)));
                values[valuesOffset++] = (int)(((long)((ulong)block4 >> 34)) & 4194303L);
                values[valuesOffset++] = (int)(((long)((ulong)block4 >> 12)) & 4194303L);
                long block5 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block4 & 4095L) << 10) | ((long)((ulong)block5 >> 54)));
                values[valuesOffset++] = (int)(((long)((ulong)block5 >> 32)) & 4194303L);
                values[valuesOffset++] = (int)(((long)((ulong)block5 >> 10)) & 4194303L);
                long block6 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block5 & 1023L) << 12) | ((long)((ulong)block6 >> 52)));
                values[valuesOffset++] = (int)(((long)((ulong)block6 >> 30)) & 4194303L);
                values[valuesOffset++] = (int)(((long)((ulong)block6 >> 8)) & 4194303L);
                long block7 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block6 & 255L) << 14) | ((long)((ulong)block7 >> 50)));
                values[valuesOffset++] = (int)(((long)((ulong)block7 >> 28)) & 4194303L);
                values[valuesOffset++] = (int)(((long)((ulong)block7 >> 6)) & 4194303L);
                long block8 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block7 & 63L) << 16) | ((long)((ulong)block8 >> 48)));
                values[valuesOffset++] = (int)(((long)((ulong)block8 >> 26)) & 4194303L);
                values[valuesOffset++] = (int)(((long)((ulong)block8 >> 4)) & 4194303L);
                long block9 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block8 & 15L) << 18) | ((long)((ulong)block9 >> 46)));
                values[valuesOffset++] = (int)(((long)((ulong)block9 >> 24)) & 4194303L);
                values[valuesOffset++] = (int)(((long)((ulong)block9 >> 2)) & 4194303L);
                long block10 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block9 & 3L) << 20) | ((long)((ulong)block10 >> 44)));
                values[valuesOffset++] = (int)(((long)((ulong)block10 >> 22)) & 4194303L);
                values[valuesOffset++] = (int)(block10 & 4194303L);
            }
        }

        public override void Decode(byte[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                int byte0 = blocks[blocksOffset++] & 0xFF;
                int byte1 = blocks[blocksOffset++] & 0xFF;
                int byte2 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = (byte0 << 14) | (byte1 << 6) | ((int)((uint)byte2 >> 2));
                int byte3 = blocks[blocksOffset++] & 0xFF;
                int byte4 = blocks[blocksOffset++] & 0xFF;
                int byte5 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte2 & 3) << 20) | (byte3 << 12) | (byte4 << 4) | ((int)((uint)byte5 >> 4));
                int byte6 = blocks[blocksOffset++] & 0xFF;
                int byte7 = blocks[blocksOffset++] & 0xFF;
                int byte8 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte5 & 15) << 18) | (byte6 << 10) | (byte7 << 2) | ((int)((uint)byte8 >> 6));
                int byte9 = blocks[blocksOffset++] & 0xFF;
                int byte10 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte8 & 63) << 16) | (byte9 << 8) | byte10;
            }
        }

        public override void Decode(long[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block0 = blocks[blocksOffset++];
                values[valuesOffset++] = (long)((ulong)block0 >> 42);
                values[valuesOffset++] = ((long)((ulong)block0 >> 20)) & 4194303L;
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block0 & 1048575L) << 2) | ((long)((ulong)block1 >> 62));
                values[valuesOffset++] = ((long)((ulong)block1 >> 40)) & 4194303L;
                values[valuesOffset++] = ((long)((ulong)block1 >> 18)) & 4194303L;
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block1 & 262143L) << 4) | ((long)((ulong)block2 >> 60));
                values[valuesOffset++] = ((long)((ulong)block2 >> 38)) & 4194303L;
                values[valuesOffset++] = ((long)((ulong)block2 >> 16)) & 4194303L;
                long block3 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block2 & 65535L) << 6) | ((long)((ulong)block3 >> 58));
                values[valuesOffset++] = ((long)((ulong)block3 >> 36)) & 4194303L;
                values[valuesOffset++] = ((long)((ulong)block3 >> 14)) & 4194303L;
                long block4 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block3 & 16383L) << 8) | ((long)((ulong)block4 >> 56));
                values[valuesOffset++] = ((long)((ulong)block4 >> 34)) & 4194303L;
                values[valuesOffset++] = ((long)((ulong)block4 >> 12)) & 4194303L;
                long block5 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block4 & 4095L) << 10) | ((long)((ulong)block5 >> 54));
                values[valuesOffset++] = ((long)((ulong)block5 >> 32)) & 4194303L;
                values[valuesOffset++] = ((long)((ulong)block5 >> 10)) & 4194303L;
                long block6 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block5 & 1023L) << 12) | ((long)((ulong)block6 >> 52));
                values[valuesOffset++] = ((long)((ulong)block6 >> 30)) & 4194303L;
                values[valuesOffset++] = ((long)((ulong)block6 >> 8)) & 4194303L;
                long block7 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block6 & 255L) << 14) | ((long)((ulong)block7 >> 50));
                values[valuesOffset++] = ((long)((ulong)block7 >> 28)) & 4194303L;
                values[valuesOffset++] = ((long)((ulong)block7 >> 6)) & 4194303L;
                long block8 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block7 & 63L) << 16) | ((long)((ulong)block8 >> 48));
                values[valuesOffset++] = ((long)((ulong)block8 >> 26)) & 4194303L;
                values[valuesOffset++] = ((long)((ulong)block8 >> 4)) & 4194303L;
                long block9 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block8 & 15L) << 18) | ((long)((ulong)block9 >> 46));
                values[valuesOffset++] = ((long)((ulong)block9 >> 24)) & 4194303L;
                values[valuesOffset++] = ((long)((ulong)block9 >> 2)) & 4194303L;
                long block10 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block9 & 3L) << 20) | ((long)((ulong)block10 >> 44));
                values[valuesOffset++] = ((long)((ulong)block10 >> 22)) & 4194303L;
                values[valuesOffset++] = block10 & 4194303L;
            }
        }

        public override void Decode(byte[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long byte0 = blocks[blocksOffset++] & 0xFF;
                long byte1 = blocks[blocksOffset++] & 0xFF;
                long byte2 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = (byte0 << 14) | (byte1 << 6) | ((long)((ulong)byte2 >> 2));
                long byte3 = blocks[blocksOffset++] & 0xFF;
                long byte4 = blocks[blocksOffset++] & 0xFF;
                long byte5 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte2 & 3) << 20) | (byte3 << 12) | (byte4 << 4) | ((long)((ulong)byte5 >> 4));
                long byte6 = blocks[blocksOffset++] & 0xFF;
                long byte7 = blocks[blocksOffset++] & 0xFF;
                long byte8 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte5 & 15) << 18) | (byte6 << 10) | (byte7 << 2) | ((long)((ulong)byte8 >> 6));
                long byte9 = blocks[blocksOffset++] & 0xFF;
                long byte10 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte8 & 63) << 16) | (byte9 << 8) | byte10;
            }
        }
    }
}