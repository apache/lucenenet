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
    internal sealed class BulkOperationPacked24 : BulkOperationPacked
    {
        public BulkOperationPacked24()
            : base(24)
        {
        }

        public override void Decode(long[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block0 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(block0.TripleShift(40));
                values[valuesOffset++] = (int)((block0.TripleShift(16)) & 16777215L);
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block0 & 65535L) << 8) | (block1.TripleShift(56)));
                values[valuesOffset++] = (int)((block1.TripleShift(32)) & 16777215L);
                values[valuesOffset++] = (int)((block1.TripleShift(8)) & 16777215L);
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block1 & 255L) << 16) | (block2.TripleShift(48)));
                values[valuesOffset++] = (int)((block2.TripleShift(24)) & 16777215L);
                values[valuesOffset++] = (int)(block2 & 16777215L);
            }
        }

        public override void Decode(byte[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                int byte0 = blocks[blocksOffset++] & 0xFF;
                int byte1 = blocks[blocksOffset++] & 0xFF;
                int byte2 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = (byte0 << 16) | (byte1 << 8) | byte2;
            }
        }

        public override void Decode(long[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long block0 = blocks[blocksOffset++];
                values[valuesOffset++] = block0.TripleShift(40);
                values[valuesOffset++] = (block0.TripleShift(16)) & 16777215L;
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block0 & 65535L) << 8) | (block1.TripleShift(56));
                values[valuesOffset++] = (block1.TripleShift(32)) & 16777215L;
                values[valuesOffset++] = (block1.TripleShift(8)) & 16777215L;
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block1 & 255L) << 16) | (block2.TripleShift(48));
                values[valuesOffset++] = (block2.TripleShift(24)) & 16777215L;
                values[valuesOffset++] = block2 & 16777215L;
            }
        }

        public override void Decode(byte[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                long byte0 = blocks[blocksOffset++] & 0xFF;
                long byte1 = blocks[blocksOffset++] & 0xFF;
                long byte2 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = (byte0 << 16) | (byte1 << 8) | byte2;
            }
        }
    }
}