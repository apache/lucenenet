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
    internal sealed class BulkOperationPacked3 : BulkOperationPacked
    {
        public BulkOperationPacked3()
            : base(3)
        {
        }

        public override void Decode(long[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block0 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(block0.TripleShift(61));
                values[valuesOffset++] = (int)((block0.TripleShift(58)) & 7L);
                values[valuesOffset++] = (int)((block0.TripleShift(55)) & 7L);
                values[valuesOffset++] = (int)((block0.TripleShift(52)) & 7L);
                values[valuesOffset++] = (int)((block0.TripleShift(49)) & 7L);
                values[valuesOffset++] = (int)((block0.TripleShift(46)) & 7L);
                values[valuesOffset++] = (int)((block0.TripleShift(43)) & 7L);
                values[valuesOffset++] = (int)((block0.TripleShift(40)) & 7L);
                values[valuesOffset++] = (int)((block0.TripleShift(37)) & 7L);
                values[valuesOffset++] = (int)((block0.TripleShift(34)) & 7L);
                values[valuesOffset++] = (int)((block0.TripleShift(31)) & 7L);
                values[valuesOffset++] = (int)((block0.TripleShift(28)) & 7L);
                values[valuesOffset++] = (int)((block0.TripleShift(25)) & 7L);
                values[valuesOffset++] = (int)((block0.TripleShift(22)) & 7L);
                values[valuesOffset++] = (int)((block0.TripleShift(19)) & 7L);
                values[valuesOffset++] = (int)((block0.TripleShift(16)) & 7L);
                values[valuesOffset++] = (int)((block0.TripleShift(13)) & 7L);
                values[valuesOffset++] = (int)((block0.TripleShift(10)) & 7L);
                values[valuesOffset++] = (int)((block0.TripleShift(7)) & 7L);
                values[valuesOffset++] = (int)((block0.TripleShift(4)) & 7L);
                values[valuesOffset++] = (int)((block0.TripleShift(1)) & 7L);
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block0 & 1L) << 2) | (block1.TripleShift(62)));
                values[valuesOffset++] = (int)((block1.TripleShift(59)) & 7L);
                values[valuesOffset++] = (int)((block1.TripleShift(56)) & 7L);
                values[valuesOffset++] = (int)((block1.TripleShift(53)) & 7L);
                values[valuesOffset++] = (int)((block1.TripleShift(50)) & 7L);
                values[valuesOffset++] = (int)((block1.TripleShift(47)) & 7L);
                values[valuesOffset++] = (int)((block1.TripleShift(44)) & 7L);
                values[valuesOffset++] = (int)((block1.TripleShift(41)) & 7L);
                values[valuesOffset++] = (int)((block1.TripleShift(38)) & 7L);
                values[valuesOffset++] = (int)((block1.TripleShift(35)) & 7L);
                values[valuesOffset++] = (int)((block1.TripleShift(32)) & 7L);
                values[valuesOffset++] = (int)((block1.TripleShift(29)) & 7L);
                values[valuesOffset++] = (int)((block1.TripleShift(26)) & 7L);
                values[valuesOffset++] = (int)((block1.TripleShift(23)) & 7L);
                values[valuesOffset++] = (int)((block1.TripleShift(20)) & 7L);
                values[valuesOffset++] = (int)((block1.TripleShift(17)) & 7L);
                values[valuesOffset++] = (int)((block1.TripleShift(14)) & 7L);
                values[valuesOffset++] = (int)((block1.TripleShift(11)) & 7L);
                values[valuesOffset++] = (int)((block1.TripleShift(8)) & 7L);
                values[valuesOffset++] = (int)((block1.TripleShift(5)) & 7L);
                values[valuesOffset++] = (int)((block1.TripleShift(2)) & 7L);
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block1 & 3L) << 1) | (block2.TripleShift(63)));
                values[valuesOffset++] = (int)((block2.TripleShift(60)) & 7L);
                values[valuesOffset++] = (int)((block2.TripleShift(57)) & 7L);
                values[valuesOffset++] = (int)((block2.TripleShift(54)) & 7L);
                values[valuesOffset++] = (int)((block2.TripleShift(51)) & 7L);
                values[valuesOffset++] = (int)((block2.TripleShift(48)) & 7L);
                values[valuesOffset++] = (int)((block2.TripleShift(45)) & 7L);
                values[valuesOffset++] = (int)((block2.TripleShift(42)) & 7L);
                values[valuesOffset++] = (int)((block2.TripleShift(39)) & 7L);
                values[valuesOffset++] = (int)((block2.TripleShift(36)) & 7L);
                values[valuesOffset++] = (int)((block2.TripleShift(33)) & 7L);
                values[valuesOffset++] = (int)((block2.TripleShift(30)) & 7L);
                values[valuesOffset++] = (int)((block2.TripleShift(27)) & 7L);
                values[valuesOffset++] = (int)((block2.TripleShift(24)) & 7L);
                values[valuesOffset++] = (int)((block2.TripleShift(21)) & 7L);
                values[valuesOffset++] = (int)((block2.TripleShift(18)) & 7L);
                values[valuesOffset++] = (int)((block2.TripleShift(15)) & 7L);
                values[valuesOffset++] = (int)((block2.TripleShift(12)) & 7L);
                values[valuesOffset++] = (int)((block2.TripleShift(9)) & 7L);
                values[valuesOffset++] = (int)((block2.TripleShift(6)) & 7L);
                values[valuesOffset++] = (int)((block2.TripleShift(3)) & 7L);
                values[valuesOffset++] = (int)(block2 & 7L);
            }
        }

        public override void Decode(byte[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                int byte0 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = byte0.TripleShift(5);
                values[valuesOffset++] = (byte0.TripleShift(2)) & 7;
                int byte1 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte0 & 3) << 1) | (byte1.TripleShift(7));
                values[valuesOffset++] = (byte1.TripleShift(4)) & 7;
                values[valuesOffset++] = (byte1.TripleShift(1)) & 7;
                int byte2 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte1 & 1) << 2) | (byte2.TripleShift(6));
                values[valuesOffset++] = (byte2.TripleShift(3)) & 7;
                values[valuesOffset++] = byte2 & 7;
            }
        }

        public override void Decode(long[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block0 = blocks[blocksOffset++];
                values[valuesOffset++] = block0.TripleShift(61);
                values[valuesOffset++] = (block0.TripleShift(58)) & 7L;
                values[valuesOffset++] = (block0.TripleShift(55)) & 7L;
                values[valuesOffset++] = (block0.TripleShift(52)) & 7L;
                values[valuesOffset++] = (block0.TripleShift(49)) & 7L;
                values[valuesOffset++] = (block0.TripleShift(46)) & 7L;
                values[valuesOffset++] = (block0.TripleShift(43)) & 7L;
                values[valuesOffset++] = (block0.TripleShift(40)) & 7L;
                values[valuesOffset++] = (block0.TripleShift(37)) & 7L;
                values[valuesOffset++] = (block0.TripleShift(34)) & 7L;
                values[valuesOffset++] = (block0.TripleShift(31)) & 7L;
                values[valuesOffset++] = (block0.TripleShift(28)) & 7L;
                values[valuesOffset++] = (block0.TripleShift(25)) & 7L;
                values[valuesOffset++] = (block0.TripleShift(22)) & 7L;
                values[valuesOffset++] = (block0.TripleShift(19)) & 7L;
                values[valuesOffset++] = (block0.TripleShift(16)) & 7L;
                values[valuesOffset++] = (block0.TripleShift(13)) & 7L;
                values[valuesOffset++] = (block0.TripleShift(10)) & 7L;
                values[valuesOffset++] = (block0.TripleShift(7)) & 7L;
                values[valuesOffset++] = (block0.TripleShift(4)) & 7L;
                values[valuesOffset++] = (block0.TripleShift(1)) & 7L;
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block0 & 1L) << 2) | (block1.TripleShift(62));
                values[valuesOffset++] = (block1.TripleShift(59)) & 7L;
                values[valuesOffset++] = (block1.TripleShift(56)) & 7L;
                values[valuesOffset++] = (block1.TripleShift(53)) & 7L;
                values[valuesOffset++] = (block1.TripleShift(50)) & 7L;
                values[valuesOffset++] = (block1.TripleShift(47)) & 7L;
                values[valuesOffset++] = (block1.TripleShift(44)) & 7L;
                values[valuesOffset++] = (block1.TripleShift(41)) & 7L;
                values[valuesOffset++] = (block1.TripleShift(38)) & 7L;
                values[valuesOffset++] = (block1.TripleShift(35)) & 7L;
                values[valuesOffset++] = (block1.TripleShift(32)) & 7L;
                values[valuesOffset++] = (block1.TripleShift(29)) & 7L;
                values[valuesOffset++] = (block1.TripleShift(26)) & 7L;
                values[valuesOffset++] = (block1.TripleShift(23)) & 7L;
                values[valuesOffset++] = (block1.TripleShift(20)) & 7L;
                values[valuesOffset++] = (block1.TripleShift(17)) & 7L;
                values[valuesOffset++] = (block1.TripleShift(14)) & 7L;
                values[valuesOffset++] = (block1.TripleShift(11)) & 7L;
                values[valuesOffset++] = (block1.TripleShift(8)) & 7L;
                values[valuesOffset++] = (block1.TripleShift(5)) & 7L;
                values[valuesOffset++] = (block1.TripleShift(2)) & 7L;
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block1 & 3L) << 1) | (block2.TripleShift(63));
                values[valuesOffset++] = (block2.TripleShift(60)) & 7L;
                values[valuesOffset++] = (block2.TripleShift(57)) & 7L;
                values[valuesOffset++] = (block2.TripleShift(54)) & 7L;
                values[valuesOffset++] = (block2.TripleShift(51)) & 7L;
                values[valuesOffset++] = (block2.TripleShift(48)) & 7L;
                values[valuesOffset++] = (block2.TripleShift(45)) & 7L;
                values[valuesOffset++] = (block2.TripleShift(42)) & 7L;
                values[valuesOffset++] = (block2.TripleShift(39)) & 7L;
                values[valuesOffset++] = (block2.TripleShift(36)) & 7L;
                values[valuesOffset++] = (block2.TripleShift(33)) & 7L;
                values[valuesOffset++] = (block2.TripleShift(30)) & 7L;
                values[valuesOffset++] = (block2.TripleShift(27)) & 7L;
                values[valuesOffset++] = (block2.TripleShift(24)) & 7L;
                values[valuesOffset++] = (block2.TripleShift(21)) & 7L;
                values[valuesOffset++] = (block2.TripleShift(18)) & 7L;
                values[valuesOffset++] = (block2.TripleShift(15)) & 7L;
                values[valuesOffset++] = (block2.TripleShift(12)) & 7L;
                values[valuesOffset++] = (block2.TripleShift(9)) & 7L;
                values[valuesOffset++] = (block2.TripleShift(6)) & 7L;
                values[valuesOffset++] = (block2.TripleShift(3)) & 7L;
                values[valuesOffset++] = block2 & 7L;
            }
        }

        public override void Decode(byte[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long byte0 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = byte0.TripleShift(5);
                values[valuesOffset++] = (byte0.TripleShift(2)) & 7;
                long byte1 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte0 & 3) << 1) | (byte1.TripleShift(7));
                values[valuesOffset++] = (byte1.TripleShift(4)) & 7;
                values[valuesOffset++] = (byte1.TripleShift(1)) & 7;
                long byte2 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte1 & 1) << 2) | (byte2.TripleShift(6));
                values[valuesOffset++] = (byte2.TripleShift(3)) & 7;
                values[valuesOffset++] = byte2 & 7;
            }
        }
    }
}