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
                values[valuesOffset++] = (int)(block0.TripleShift(46));
                values[valuesOffset++] = (int)((block0.TripleShift(28)) & 262143L);
                values[valuesOffset++] = (int)((block0.TripleShift(10)) & 262143L);
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block0 & 1023L) << 8) | (block1.TripleShift(56)));
                values[valuesOffset++] = (int)((block1.TripleShift(38)) & 262143L);
                values[valuesOffset++] = (int)((block1.TripleShift(20)) & 262143L);
                values[valuesOffset++] = (int)((block1.TripleShift(2)) & 262143L);
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block1 & 3L) << 16) | (block2.TripleShift(48)));
                values[valuesOffset++] = (int)((block2.TripleShift(30)) & 262143L);
                values[valuesOffset++] = (int)((block2.TripleShift(12)) & 262143L);
                long block3 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block2 & 4095L) << 6) | (block3.TripleShift(58)));
                values[valuesOffset++] = (int)((block3.TripleShift(40)) & 262143L);
                values[valuesOffset++] = (int)((block3.TripleShift(22)) & 262143L);
                values[valuesOffset++] = (int)((block3.TripleShift(4)) & 262143L);
                long block4 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block3 & 15L) << 14) | (block4.TripleShift(50)));
                values[valuesOffset++] = (int)((block4.TripleShift(32)) & 262143L);
                values[valuesOffset++] = (int)((block4.TripleShift(14)) & 262143L);
                long block5 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block4 & 16383L) << 4) | (block5.TripleShift(60)));
                values[valuesOffset++] = (int)((block5.TripleShift(42)) & 262143L);
                values[valuesOffset++] = (int)((block5.TripleShift(24)) & 262143L);
                values[valuesOffset++] = (int)((block5.TripleShift(6)) & 262143L);
                long block6 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block5 & 63L) << 12) | (block6.TripleShift(52)));
                values[valuesOffset++] = (int)((block6.TripleShift(34)) & 262143L);
                values[valuesOffset++] = (int)((block6.TripleShift(16)) & 262143L);
                long block7 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block6 & 65535L) << 2) | (block7.TripleShift(62)));
                values[valuesOffset++] = (int)((block7.TripleShift(44)) & 262143L);
                values[valuesOffset++] = (int)((block7.TripleShift(26)) & 262143L);
                values[valuesOffset++] = (int)((block7.TripleShift(8)) & 262143L);
                long block8 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block7 & 255L) << 10) | (block8.TripleShift(54)));
                values[valuesOffset++] = (int)((block8.TripleShift(36)) & 262143L);
                values[valuesOffset++] = (int)((block8.TripleShift(18)) & 262143L);
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
                values[valuesOffset++] = (byte0 << 10) | (byte1 << 2) | (byte2.TripleShift(6));
                int byte3 = blocks[blocksOffset++] & 0xFF;
                int byte4 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte2 & 63) << 12) | (byte3 << 4) | (byte4.TripleShift(4));
                int byte5 = blocks[blocksOffset++] & 0xFF;
                int byte6 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte4 & 15) << 14) | (byte5 << 6) | (byte6.TripleShift(2));
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
                values[valuesOffset++] = block0.TripleShift(46);
                values[valuesOffset++] = (block0.TripleShift(28)) & 262143L;
                values[valuesOffset++] = (block0.TripleShift(10)) & 262143L;
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block0 & 1023L) << 8) | (block1.TripleShift(56));
                values[valuesOffset++] = (block1.TripleShift(38)) & 262143L;
                values[valuesOffset++] = (block1.TripleShift(20)) & 262143L;
                values[valuesOffset++] = (block1.TripleShift(2)) & 262143L;
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block1 & 3L) << 16) | (block2.TripleShift(48));
                values[valuesOffset++] = (block2.TripleShift(30)) & 262143L;
                values[valuesOffset++] = (block2.TripleShift(12)) & 262143L;
                long block3 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block2 & 4095L) << 6) | (block3.TripleShift(58));
                values[valuesOffset++] = (block3.TripleShift(40)) & 262143L;
                values[valuesOffset++] = (block3.TripleShift(22)) & 262143L;
                values[valuesOffset++] = (block3.TripleShift(4)) & 262143L;
                long block4 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block3 & 15L) << 14) | (block4.TripleShift(50));
                values[valuesOffset++] = (block4.TripleShift(32)) & 262143L;
                values[valuesOffset++] = (block4.TripleShift(14)) & 262143L;
                long block5 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block4 & 16383L) << 4) | (block5.TripleShift(60));
                values[valuesOffset++] = (block5.TripleShift(42)) & 262143L;
                values[valuesOffset++] = (block5.TripleShift(24)) & 262143L;
                values[valuesOffset++] = (block5.TripleShift(6)) & 262143L;
                long block6 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block5 & 63L) << 12) | (block6.TripleShift(52));
                values[valuesOffset++] = (block6.TripleShift(34)) & 262143L;
                values[valuesOffset++] = (block6.TripleShift(16)) & 262143L;
                long block7 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block6 & 65535L) << 2) | (block7.TripleShift(62));
                values[valuesOffset++] = (block7.TripleShift(44)) & 262143L;
                values[valuesOffset++] = (block7.TripleShift(26)) & 262143L;
                values[valuesOffset++] = (block7.TripleShift(8)) & 262143L;
                long block8 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block7 & 255L) << 10) | (block8.TripleShift(54));
                values[valuesOffset++] = (block8.TripleShift(36)) & 262143L;
                values[valuesOffset++] = (block8.TripleShift(18)) & 262143L;
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
                values[valuesOffset++] = (byte0 << 10) | (byte1 << 2) | (byte2.TripleShift(6));
                long byte3 = blocks[blocksOffset++] & 0xFF;
                long byte4 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte2 & 63) << 12) | (byte3 << 4) | (byte4.TripleShift(4));
                long byte5 = blocks[blocksOffset++] & 0xFF;
                long byte6 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte4 & 15) << 14) | (byte5 << 6) | (byte6.TripleShift(2));
                long byte7 = blocks[blocksOffset++] & 0xFF;
                long byte8 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte6 & 3) << 16) | (byte7 << 8) | byte8;
            }
        }
    }
}