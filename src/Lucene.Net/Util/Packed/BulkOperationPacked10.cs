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
    internal sealed class BulkOperationPacked10 : BulkOperationPacked
    {
        public BulkOperationPacked10()
            : base(10)
        {
        }

        public override void Decode(long[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block0 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(block0.TripleShift(54));
                values[valuesOffset++] = (int)((block0.TripleShift(44)) & 1023L);
                values[valuesOffset++] = (int)((block0.TripleShift(34)) & 1023L);
                values[valuesOffset++] = (int)((block0.TripleShift(24)) & 1023L);
                values[valuesOffset++] = (int)((block0.TripleShift(14)) & 1023L);
                values[valuesOffset++] = (int)((block0.TripleShift(4)) & 1023L);
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block0 & 15L) << 6) | (block1.TripleShift(58)));
                values[valuesOffset++] = (int)((block1.TripleShift(48)) & 1023L);
                values[valuesOffset++] = (int)((block1.TripleShift(38)) & 1023L);
                values[valuesOffset++] = (int)((block1.TripleShift(28)) & 1023L);
                values[valuesOffset++] = (int)((block1.TripleShift(18)) & 1023L);
                values[valuesOffset++] = (int)((block1.TripleShift(8)) & 1023L);
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block1 & 255L) << 2) | (block2.TripleShift(62)));
                values[valuesOffset++] = (int)((block2.TripleShift(52)) & 1023L);
                values[valuesOffset++] = (int)((block2.TripleShift(42)) & 1023L);
                values[valuesOffset++] = (int)((block2.TripleShift(32)) & 1023L);
                values[valuesOffset++] = (int)((block2.TripleShift(22)) & 1023L);
                values[valuesOffset++] = (int)((block2.TripleShift(12)) & 1023L);
                values[valuesOffset++] = (int)((block2.TripleShift(2)) & 1023L);
                long block3 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block2 & 3L) << 8) | (block3.TripleShift(56)));
                values[valuesOffset++] = (int)((block3.TripleShift(46)) & 1023L);
                values[valuesOffset++] = (int)((block3.TripleShift(36)) & 1023L);
                values[valuesOffset++] = (int)((block3.TripleShift(26)) & 1023L);
                values[valuesOffset++] = (int)((block3.TripleShift(16)) & 1023L);
                values[valuesOffset++] = (int)((block3.TripleShift(6)) & 1023L);
                long block4 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block3 & 63L) << 4) | (block4.TripleShift(60)));
                values[valuesOffset++] = (int)((block4.TripleShift(50)) & 1023L);
                values[valuesOffset++] = (int)((block4.TripleShift(40)) & 1023L);
                values[valuesOffset++] = (int)((block4.TripleShift(30)) & 1023L);
                values[valuesOffset++] = (int)((block4.TripleShift(20)) & 1023L);
                values[valuesOffset++] = (int)((block4.TripleShift(10)) & 1023L);
                values[valuesOffset++] = (int)(block4 & 1023L);
            }
        }

        public override void Decode(byte[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                int byte0 = blocks[blocksOffset++] & 0xFF;
                int byte1 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = (byte0 << 2) | (byte1.TripleShift(6));
                int byte2 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte1 & 63) << 4) | (byte2.TripleShift(4));
                int byte3 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte2 & 15) << 6) | (byte3.TripleShift(2));
                int byte4 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte3 & 3) << 8) | byte4;
            }
        }

        public override void Decode(long[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block0 = blocks[blocksOffset++];
                values[valuesOffset++] = (block0.TripleShift(54));
                values[valuesOffset++] = (block0.TripleShift(44)) & 1023L;
                values[valuesOffset++] = (block0.TripleShift(34)) & 1023L;
                values[valuesOffset++] = (block0.TripleShift(24)) & 1023L;
                values[valuesOffset++] = (block0.TripleShift(14)) & 1023L;
                values[valuesOffset++] = (block0.TripleShift(4)) & 1023L;
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block0 & 15L) << 6) | (block1.TripleShift(58));
                values[valuesOffset++] = (block1.TripleShift(48)) & 1023L;
                values[valuesOffset++] = (block1.TripleShift(38)) & 1023L;
                values[valuesOffset++] = (block1.TripleShift(28)) & 1023L;
                values[valuesOffset++] = (block1.TripleShift(18)) & 1023L;
                values[valuesOffset++] = (block1.TripleShift(8)) & 1023L;
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block1 & 255L) << 2) | (block2.TripleShift(62));
                values[valuesOffset++] = (block2.TripleShift(52)) & 1023L;
                values[valuesOffset++] = (block2.TripleShift(42)) & 1023L;
                values[valuesOffset++] = (block2.TripleShift(32)) & 1023L;
                values[valuesOffset++] = (block2.TripleShift(22)) & 1023L;
                values[valuesOffset++] = (block2.TripleShift(12)) & 1023L;
                values[valuesOffset++] = (block2.TripleShift(2)) & 1023L;
                long block3 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block2 & 3L) << 8) | (block3.TripleShift(56));
                values[valuesOffset++] = (block3.TripleShift(46)) & 1023L;
                values[valuesOffset++] = (block3.TripleShift(36)) & 1023L;
                values[valuesOffset++] = (block3.TripleShift(26)) & 1023L;
                values[valuesOffset++] = (block3.TripleShift(16)) & 1023L;
                values[valuesOffset++] = (block3.TripleShift(6)) & 1023L;
                long block4 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block3 & 63L) << 4) | (block4.TripleShift(60));
                values[valuesOffset++] = (block4.TripleShift(50)) & 1023L;
                values[valuesOffset++] = (block4.TripleShift(40)) & 1023L;
                values[valuesOffset++] = (block4.TripleShift(30)) & 1023L;
                values[valuesOffset++] = (block4.TripleShift(20)) & 1023L;
                values[valuesOffset++] = (block4.TripleShift(10)) & 1023L;
                values[valuesOffset++] = block4 & 1023L;
            }
        }

        public override void Decode(byte[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long byte0 = blocks[blocksOffset++] & 0xFF;
                long byte1 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = (byte0 << 2) | (byte1.TripleShift(6));
                long byte2 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte1 & 63) << 4) | (byte2.TripleShift(4));
                long byte3 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte2 & 15) << 6) | (byte3.TripleShift(2));
                long byte4 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte3 & 3) << 8) | byte4;
            }
        }
    }
}