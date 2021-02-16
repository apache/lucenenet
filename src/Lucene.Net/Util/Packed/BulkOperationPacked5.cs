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
    internal sealed class BulkOperationPacked5 : BulkOperationPacked
    {
        public BulkOperationPacked5()
            : base(5)
        {
        }

        public override void Decode(long[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block0 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(block0.TripleShift(59));
                values[valuesOffset++] = (int)((block0.TripleShift(54)) & 31L);
                values[valuesOffset++] = (int)((block0.TripleShift(49)) & 31L);
                values[valuesOffset++] = (int)((block0.TripleShift(44)) & 31L);
                values[valuesOffset++] = (int)((block0.TripleShift(39)) & 31L);
                values[valuesOffset++] = (int)((block0.TripleShift(34)) & 31L);
                values[valuesOffset++] = (int)((block0.TripleShift(29)) & 31L);
                values[valuesOffset++] = (int)((block0.TripleShift(24)) & 31L);
                values[valuesOffset++] = (int)((block0.TripleShift(19)) & 31L);
                values[valuesOffset++] = (int)((block0.TripleShift(14)) & 31L);
                values[valuesOffset++] = (int)((block0.TripleShift(9)) & 31L);
                values[valuesOffset++] = (int)((block0.TripleShift(4)) & 31L);
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block0 & 15L) << 1) | (block1.TripleShift(63)));
                values[valuesOffset++] = (int)((block1.TripleShift(58)) & 31L);
                values[valuesOffset++] = (int)((block1.TripleShift(53)) & 31L);
                values[valuesOffset++] = (int)((block1.TripleShift(48)) & 31L);
                values[valuesOffset++] = (int)((block1.TripleShift(43)) & 31L);
                values[valuesOffset++] = (int)((block1.TripleShift(38)) & 31L);
                values[valuesOffset++] = (int)((block1.TripleShift(33)) & 31L);
                values[valuesOffset++] = (int)((block1.TripleShift(28)) & 31L);
                values[valuesOffset++] = (int)((block1.TripleShift(23)) & 31L);
                values[valuesOffset++] = (int)((block1.TripleShift(18)) & 31L);
                values[valuesOffset++] = (int)((block1.TripleShift(13)) & 31L);
                values[valuesOffset++] = (int)((block1.TripleShift(8)) & 31L);
                values[valuesOffset++] = (int)((block1.TripleShift(3)) & 31L);
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block1 & 7L) << 2) | (block2.TripleShift(62)));
                values[valuesOffset++] = (int)((block2.TripleShift(57)) & 31L);
                values[valuesOffset++] = (int)((block2.TripleShift(52)) & 31L);
                values[valuesOffset++] = (int)((block2.TripleShift(47)) & 31L);
                values[valuesOffset++] = (int)((block2.TripleShift(42)) & 31L);
                values[valuesOffset++] = (int)((block2.TripleShift(37)) & 31L);
                values[valuesOffset++] = (int)((block2.TripleShift(32)) & 31L);
                values[valuesOffset++] = (int)((block2.TripleShift(27)) & 31L);
                values[valuesOffset++] = (int)((block2.TripleShift(22)) & 31L);
                values[valuesOffset++] = (int)((block2.TripleShift(17)) & 31L);
                values[valuesOffset++] = (int)((block2.TripleShift(12)) & 31L);
                values[valuesOffset++] = (int)((block2.TripleShift(7)) & 31L);
                values[valuesOffset++] = (int)((block2.TripleShift(2)) & 31L);
                long block3 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block2 & 3L) << 3) | (block3.TripleShift(61)));
                values[valuesOffset++] = (int)((block3.TripleShift(56)) & 31L);
                values[valuesOffset++] = (int)((block3.TripleShift(51)) & 31L);
                values[valuesOffset++] = (int)((block3.TripleShift(46)) & 31L);
                values[valuesOffset++] = (int)((block3.TripleShift(41)) & 31L);
                values[valuesOffset++] = (int)((block3.TripleShift(36)) & 31L);
                values[valuesOffset++] = (int)((block3.TripleShift(31)) & 31L);
                values[valuesOffset++] = (int)((block3.TripleShift(26)) & 31L);
                values[valuesOffset++] = (int)((block3.TripleShift(21)) & 31L);
                values[valuesOffset++] = (int)((block3.TripleShift(16)) & 31L);
                values[valuesOffset++] = (int)((block3.TripleShift(11)) & 31L);
                values[valuesOffset++] = (int)((block3.TripleShift(6)) & 31L);
                values[valuesOffset++] = (int)((block3.TripleShift(1)) & 31L);
                long block4 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block3 & 1L) << 4) | (block4.TripleShift(60)));
                values[valuesOffset++] = (int)((block4.TripleShift(55)) & 31L);
                values[valuesOffset++] = (int)((block4.TripleShift(50)) & 31L);
                values[valuesOffset++] = (int)((block4.TripleShift(45)) & 31L);
                values[valuesOffset++] = (int)((block4.TripleShift(40)) & 31L);
                values[valuesOffset++] = (int)((block4.TripleShift(35)) & 31L);
                values[valuesOffset++] = (int)((block4.TripleShift(30)) & 31L);
                values[valuesOffset++] = (int)((block4.TripleShift(25)) & 31L);
                values[valuesOffset++] = (int)((block4.TripleShift(20)) & 31L);
                values[valuesOffset++] = (int)((block4.TripleShift(15)) & 31L);
                values[valuesOffset++] = (int)((block4.TripleShift(10)) & 31L);
                values[valuesOffset++] = (int)((block4.TripleShift(5)) & 31L);
                values[valuesOffset++] = (int)(block4 & 31L);
            }
        }

        public override void Decode(byte[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                int byte0 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = byte0.TripleShift(3);
                int byte1 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte0 & 7) << 2) | (byte1.TripleShift(6));
                values[valuesOffset++] = (byte1.TripleShift(1)) & 31;
                int byte2 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte1 & 1) << 4) | (byte2.TripleShift(4));
                int byte3 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte2 & 15) << 1) | (byte3.TripleShift(7));
                values[valuesOffset++] = (byte3.TripleShift(2)) & 31;
                int byte4 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte3 & 3) << 3) | (byte4.TripleShift(5));
                values[valuesOffset++] = byte4 & 31;
            }
        }

        public override void Decode(long[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block0 = blocks[blocksOffset++];
                values[valuesOffset++] = block0.TripleShift(59);
                values[valuesOffset++] = (block0.TripleShift(54)) & 31L;
                values[valuesOffset++] = (block0.TripleShift(49)) & 31L;
                values[valuesOffset++] = (block0.TripleShift(44)) & 31L;
                values[valuesOffset++] = (block0.TripleShift(39)) & 31L;
                values[valuesOffset++] = (block0.TripleShift(34)) & 31L;
                values[valuesOffset++] = (block0.TripleShift(29)) & 31L;
                values[valuesOffset++] = (block0.TripleShift(24)) & 31L;
                values[valuesOffset++] = (block0.TripleShift(19)) & 31L;
                values[valuesOffset++] = (block0.TripleShift(14)) & 31L;
                values[valuesOffset++] = (block0.TripleShift(9)) & 31L;
                values[valuesOffset++] = (block0.TripleShift(4)) & 31L;
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block0 & 15L) << 1) | (block1.TripleShift(63));
                values[valuesOffset++] = (block1.TripleShift(58)) & 31L;
                values[valuesOffset++] = (block1.TripleShift(53)) & 31L;
                values[valuesOffset++] = (block1.TripleShift(48)) & 31L;
                values[valuesOffset++] = (block1.TripleShift(43)) & 31L;
                values[valuesOffset++] = (block1.TripleShift(38)) & 31L;
                values[valuesOffset++] = (block1.TripleShift(33)) & 31L;
                values[valuesOffset++] = (block1.TripleShift(28)) & 31L;
                values[valuesOffset++] = (block1.TripleShift(23)) & 31L;
                values[valuesOffset++] = (block1.TripleShift(18)) & 31L;
                values[valuesOffset++] = (block1.TripleShift(13)) & 31L;
                values[valuesOffset++] = (block1.TripleShift(8)) & 31L;
                values[valuesOffset++] = (block1.TripleShift(3)) & 31L;
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block1 & 7L) << 2) | (block2.TripleShift(62));
                values[valuesOffset++] = (block2.TripleShift(57)) & 31L;
                values[valuesOffset++] = (block2.TripleShift(52)) & 31L;
                values[valuesOffset++] = (block2.TripleShift(47)) & 31L;
                values[valuesOffset++] = (block2.TripleShift(42)) & 31L;
                values[valuesOffset++] = (block2.TripleShift(37)) & 31L;
                values[valuesOffset++] = (block2.TripleShift(32)) & 31L;
                values[valuesOffset++] = (block2.TripleShift(27)) & 31L;
                values[valuesOffset++] = (block2.TripleShift(22)) & 31L;
                values[valuesOffset++] = (block2.TripleShift(17)) & 31L;
                values[valuesOffset++] = (block2.TripleShift(12)) & 31L;
                values[valuesOffset++] = (block2.TripleShift(7)) & 31L;
                values[valuesOffset++] = (block2.TripleShift(2)) & 31L;
                long block3 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block2 & 3L) << 3) | (block3.TripleShift(61));
                values[valuesOffset++] = (block3.TripleShift(56)) & 31L;
                values[valuesOffset++] = (block3.TripleShift(51)) & 31L;
                values[valuesOffset++] = (block3.TripleShift(46)) & 31L;
                values[valuesOffset++] = (block3.TripleShift(41)) & 31L;
                values[valuesOffset++] = (block3.TripleShift(36)) & 31L;
                values[valuesOffset++] = (block3.TripleShift(31)) & 31L;
                values[valuesOffset++] = (block3.TripleShift(26)) & 31L;
                values[valuesOffset++] = (block3.TripleShift(21)) & 31L;
                values[valuesOffset++] = (block3.TripleShift(16)) & 31L;
                values[valuesOffset++] = (block3.TripleShift(11)) & 31L;
                values[valuesOffset++] = (block3.TripleShift(6)) & 31L;
                values[valuesOffset++] = (block3.TripleShift(1)) & 31L;
                long block4 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block3 & 1L) << 4) | (block4.TripleShift(60));
                values[valuesOffset++] = (block4.TripleShift(55)) & 31L;
                values[valuesOffset++] = (block4.TripleShift(50)) & 31L;
                values[valuesOffset++] = (block4.TripleShift(45)) & 31L;
                values[valuesOffset++] = (block4.TripleShift(40)) & 31L;
                values[valuesOffset++] = (block4.TripleShift(35)) & 31L;
                values[valuesOffset++] = (block4.TripleShift(30)) & 31L;
                values[valuesOffset++] = (block4.TripleShift(25)) & 31L;
                values[valuesOffset++] = (block4.TripleShift(20)) & 31L;
                values[valuesOffset++] = (block4.TripleShift(15)) & 31L;
                values[valuesOffset++] = (block4.TripleShift(10)) & 31L;
                values[valuesOffset++] = (block4.TripleShift(5)) & 31L;
                values[valuesOffset++] = block4 & 31L;
            }
        }

        public override void Decode(byte[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long byte0 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = byte0.TripleShift(3);
                long byte1 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte0 & 7) << 2) | (byte1.TripleShift(6));
                values[valuesOffset++] = (byte1.TripleShift(1)) & 31;
                long byte2 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte1 & 1) << 4) | (byte2.TripleShift(4));
                long byte3 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte2 & 15) << 1) | (byte3.TripleShift(7));
                values[valuesOffset++] = (byte3.TripleShift(2)) & 31;
                long byte4 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte3 & 3) << 3) | (byte4.TripleShift(5));
                values[valuesOffset++] = byte4 & 31;
            }
        }
    }
}