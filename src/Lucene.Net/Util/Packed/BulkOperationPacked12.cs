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
    internal sealed class BulkOperationPacked12 : BulkOperationPacked
    {
        public BulkOperationPacked12()
            : base(12)
        {
        }

        public override void Decode(long[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block0 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)((long)((ulong)block0 >> 52));
                values[valuesOffset++] = (int)(((long)((ulong)block0 >> 40)) & 4095L);
                values[valuesOffset++] = (int)(((long)((ulong)block0 >> 28)) & 4095L);
                values[valuesOffset++] = (int)(((long)((ulong)block0 >> 16)) & 4095L);
                values[valuesOffset++] = (int)(((long)((ulong)block0 >> 4)) & 4095L);
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block0 & 15L) << 8) | ((long)((ulong)block1 >> 56)));
                values[valuesOffset++] = (int)(((long)((ulong)block1 >> 44)) & 4095L);
                values[valuesOffset++] = (int)(((long)((ulong)block1 >> 32)) & 4095L);
                values[valuesOffset++] = (int)(((long)((ulong)block1 >> 20)) & 4095L);
                values[valuesOffset++] = (int)(((long)((ulong)block1 >> 8)) & 4095L);
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block1 & 255L) << 4) | ((long)((ulong)block2 >> 60)));
                values[valuesOffset++] = (int)(((long)((ulong)block2 >> 48)) & 4095L);
                values[valuesOffset++] = (int)(((long)((ulong)block2 >> 36)) & 4095L);
                values[valuesOffset++] = (int)(((long)((ulong)block2 >> 24)) & 4095L);
                values[valuesOffset++] = (int)(((long)((ulong)block2 >> 12)) & 4095L);
                values[valuesOffset++] = (int)(block2 & 4095L);
            }
        }

        public override void Decode(byte[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                int byte0 = blocks[blocksOffset++] & 0xFF;
                int byte1 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = (byte0 << 4) | ((int)((uint)byte1 >> 4));
                int byte2 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte1 & 15) << 8) | byte2;
            }
        }

        public override void Decode(long[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block0 = blocks[blocksOffset++];
                values[valuesOffset++] = (long)((ulong)block0 >> 52);
                values[valuesOffset++] = ((long)((ulong)block0 >> 40)) & 4095L;
                values[valuesOffset++] = ((long)((ulong)block0 >> 28)) & 4095L;
                values[valuesOffset++] = ((long)((ulong)block0 >> 16)) & 4095L;
                values[valuesOffset++] = ((long)((ulong)block0 >> 4)) & 4095L;
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block0 & 15L) << 8) | ((long)((ulong)block1 >> 56));
                values[valuesOffset++] = ((long)((ulong)block1 >> 44)) & 4095L;
                values[valuesOffset++] = ((long)((ulong)block1 >> 32)) & 4095L;
                values[valuesOffset++] = ((long)((ulong)block1 >> 20)) & 4095L;
                values[valuesOffset++] = ((long)((ulong)block1 >> 8)) & 4095L;
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block1 & 255L) << 4) | ((long)((ulong)block2 >> 60));
                values[valuesOffset++] = ((long)((ulong)block2 >> 48)) & 4095L;
                values[valuesOffset++] = ((long)((ulong)block2 >> 36)) & 4095L;
                values[valuesOffset++] = ((long)((ulong)block2 >> 24)) & 4095L;
                values[valuesOffset++] = ((long)((ulong)block2 >> 12)) & 4095L;
                values[valuesOffset++] = block2 & 4095L;
            }
        }

        public override void Decode(byte[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long byte0 = blocks[blocksOffset++] & 0xFF;
                long byte1 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = (byte0 << 4) | ((long)((ulong)byte1 >> 4));
                long byte2 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte1 & 15) << 8) | byte2;
            }
        }
    }
}