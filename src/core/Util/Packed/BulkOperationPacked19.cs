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
    internal sealed class BulkOperationPacked19 : BulkOperationPacked
    {
        public BulkOperationPacked19()
            : base(19)
        {
        }

        public override void Decode(long[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final long block0 = blocks[blocksOffset++];
                long block0 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)((long)((ulong)block0 >> 45));
                values[valuesOffset++] = (int)(((long)((ulong)block0 >> 26)) & 524287L);
                values[valuesOffset++] = (int)(((long)((ulong)block0 >> 7)) & 524287L);
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final long block1 = blocks[blocksOffset++];
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block0 & 127L) << 12) | ((long)((ulong)block1 >> 52)));
                values[valuesOffset++] = (int)(((long)((ulong)block1 >> 33)) & 524287L);
                values[valuesOffset++] = (int)(((long)((ulong)block1 >> 14)) & 524287L);
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final long block2 = blocks[blocksOffset++];
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block1 & 16383L) << 5) | ((long)((ulong)block2 >> 59)));
                values[valuesOffset++] = (int)(((long)((ulong)block2 >> 40)) & 524287L);
                values[valuesOffset++] = (int)(((long)((ulong)block2 >> 21)) & 524287L);
                values[valuesOffset++] = (int)(((long)((ulong)block2 >> 2)) & 524287L);
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final long block3 = blocks[blocksOffset++];
                long block3 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block2 & 3L) << 17) | ((long)((ulong)block3 >> 47)));
                values[valuesOffset++] = (int)(((long)((ulong)block3 >> 28)) & 524287L);
                values[valuesOffset++] = (int)(((long)((ulong)block3 >> 9)) & 524287L);
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final long block4 = blocks[blocksOffset++];
                long block4 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block3 & 511L) << 10) | ((long)((ulong)block4 >> 54)));
                values[valuesOffset++] = (int)(((long)((ulong)block4 >> 35)) & 524287L);
                values[valuesOffset++] = (int)(((long)((ulong)block4 >> 16)) & 524287L);
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final long block5 = blocks[blocksOffset++];
                long block5 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block4 & 65535L) << 3) | ((long)((ulong)block5 >> 61)));
                values[valuesOffset++] = (int)(((long)((ulong)block5 >> 42)) & 524287L);
                values[valuesOffset++] = (int)(((long)((ulong)block5 >> 23)) & 524287L);
                values[valuesOffset++] = (int)(((long)((ulong)block5 >> 4)) & 524287L);
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final long block6 = blocks[blocksOffset++];
                long block6 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block5 & 15L) << 15) | ((long)((ulong)block6 >> 49)));
                values[valuesOffset++] = (int)(((long)((ulong)block6 >> 30)) & 524287L);
                values[valuesOffset++] = (int)(((long)((ulong)block6 >> 11)) & 524287L);
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final long block7 = blocks[blocksOffset++];
                long block7 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block6 & 2047L) << 8) | ((long)((ulong)block7 >> 56)));
                values[valuesOffset++] = (int)(((long)((ulong)block7 >> 37)) & 524287L);
                values[valuesOffset++] = (int)(((long)((ulong)block7 >> 18)) & 524287L);
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final long block8 = blocks[blocksOffset++];
                long block8 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block7 & 262143L) << 1) | ((long)((ulong)block8 >> 63)));
                values[valuesOffset++] = (int)(((long)((ulong)block8 >> 44)) & 524287L);
                values[valuesOffset++] = (int)(((long)((ulong)block8 >> 25)) & 524287L);
                values[valuesOffset++] = (int)(((long)((ulong)block8 >> 6)) & 524287L);
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final long block9 = blocks[blocksOffset++];
                long block9 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block8 & 63L) << 13) | ((long)((ulong)block9 >> 51)));
                values[valuesOffset++] = (int)(((long)((ulong)block9 >> 32)) & 524287L);
                values[valuesOffset++] = (int)(((long)((ulong)block9 >> 13)) & 524287L);
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final long block10 = blocks[blocksOffset++];
                long block10 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block9 & 8191L) << 6) | ((long)((ulong)block10 >> 58)));
                values[valuesOffset++] = (int)(((long)((ulong)block10 >> 39)) & 524287L);
                values[valuesOffset++] = (int)(((long)((ulong)block10 >> 20)) & 524287L);
                values[valuesOffset++] = (int)(((long)((ulong)block10 >> 1)) & 524287L);
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final long block11 = blocks[blocksOffset++];
                long block11 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block10 & 1L) << 18) | ((long)((ulong)block11 >> 46)));
                values[valuesOffset++] = (int)(((long)((ulong)block11 >> 27)) & 524287L);
                values[valuesOffset++] = (int)(((long)((ulong)block11 >> 8)) & 524287L);
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final long block12 = blocks[blocksOffset++];
                long block12 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block11 & 255L) << 11) | ((long)((ulong)block12 >> 53)));
                values[valuesOffset++] = (int)(((long)((ulong)block12 >> 34)) & 524287L);
                values[valuesOffset++] = (int)(((long)((ulong)block12 >> 15)) & 524287L);
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final long block13 = blocks[blocksOffset++];
                long block13 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block12 & 32767L) << 4) | ((long)((ulong)block13 >> 60)));
                values[valuesOffset++] = (int)(((long)((ulong)block13 >> 41)) & 524287L);
                values[valuesOffset++] = (int)(((long)((ulong)block13 >> 22)) & 524287L);
                values[valuesOffset++] = (int)(((long)((ulong)block13 >> 3)) & 524287L);
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final long block14 = blocks[blocksOffset++];
                long block14 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block13 & 7L) << 16) | ((long)((ulong)block14 >> 48)));
                values[valuesOffset++] = (int)(((long)((ulong)block14 >> 29)) & 524287L);
                values[valuesOffset++] = (int)(((long)((ulong)block14 >> 10)) & 524287L);
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final long block15 = blocks[blocksOffset++];
                long block15 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block14 & 1023L) << 9) | ((long)((ulong)block15 >> 55)));
                values[valuesOffset++] = (int)(((long)((ulong)block15 >> 36)) & 524287L);
                values[valuesOffset++] = (int)(((long)((ulong)block15 >> 17)) & 524287L);
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final long block16 = blocks[blocksOffset++];
                long block16 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block15 & 131071L) << 2) | ((long)((ulong)block16 >> 62)));
                values[valuesOffset++] = (int)(((long)((ulong)block16 >> 43)) & 524287L);
                values[valuesOffset++] = (int)(((long)((ulong)block16 >> 24)) & 524287L);
                values[valuesOffset++] = (int)(((long)((ulong)block16 >> 5)) & 524287L);
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final long block17 = blocks[blocksOffset++];
                long block17 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block16 & 31L) << 14) | ((long)((ulong)block17 >> 50)));
                values[valuesOffset++] = (int)(((long)((ulong)block17 >> 31)) & 524287L);
                values[valuesOffset++] = (int)(((long)((ulong)block17 >> 12)) & 524287L);
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final long block18 = blocks[blocksOffset++];
                long block18 = blocks[blocksOffset++];
                values[valuesOffset++] = (int)(((block17 & 4095L) << 7) | ((long)((ulong)block18 >> 57)));
                values[valuesOffset++] = (int)(((long)((ulong)block18 >> 38)) & 524287L);
                values[valuesOffset++] = (int)(((long)((ulong)block18 >> 19)) & 524287L);
                values[valuesOffset++] = (int)(block18 & 524287L);
            }
        }

        public override void Decode(sbyte[] blocks, int blocksOffset, int[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final int byte0 = blocks[blocksOffset++] & 0xFF;
                int byte0 = blocks[blocksOffset++] & 0xFF;
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final int byte1 = blocks[blocksOffset++] & 0xFF;
                int byte1 = blocks[blocksOffset++] & 0xFF;
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final int byte2 = blocks[blocksOffset++] & 0xFF;
                int byte2 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = (byte0 << 11) | (byte1 << 3) | ((int)((uint)byte2 >> 5));
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final int byte3 = blocks[blocksOffset++] & 0xFF;
                int byte3 = blocks[blocksOffset++] & 0xFF;
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final int byte4 = blocks[blocksOffset++] & 0xFF;
                int byte4 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte2 & 31) << 14) | (byte3 << 6) | ((int)((uint)byte4 >> 2));
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final int byte5 = blocks[blocksOffset++] & 0xFF;
                int byte5 = blocks[blocksOffset++] & 0xFF;
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final int byte6 = blocks[blocksOffset++] & 0xFF;
                int byte6 = blocks[blocksOffset++] & 0xFF;
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final int byte7 = blocks[blocksOffset++] & 0xFF;
                int byte7 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte4 & 3) << 17) | (byte5 << 9) | (byte6 << 1) | ((int)((uint)byte7 >> 7));
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final int byte8 = blocks[blocksOffset++] & 0xFF;
                int byte8 = blocks[blocksOffset++] & 0xFF;
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final int byte9 = blocks[blocksOffset++] & 0xFF;
                int byte9 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte7 & 127) << 12) | (byte8 << 4) | ((int)((uint)byte9 >> 4));
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final int byte10 = blocks[blocksOffset++] & 0xFF;
                int byte10 = blocks[blocksOffset++] & 0xFF;
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final int byte11 = blocks[blocksOffset++] & 0xFF;
                int byte11 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte9 & 15) << 15) | (byte10 << 7) | ((int)((uint)byte11 >> 1));
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final int byte12 = blocks[blocksOffset++] & 0xFF;
                int byte12 = blocks[blocksOffset++] & 0xFF;
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final int byte13 = blocks[blocksOffset++] & 0xFF;
                int byte13 = blocks[blocksOffset++] & 0xFF;
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final int byte14 = blocks[blocksOffset++] & 0xFF;
                int byte14 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte11 & 1) << 18) | (byte12 << 10) | (byte13 << 2) | ((int)((uint)byte14 >> 6));
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final int byte15 = blocks[blocksOffset++] & 0xFF;
                int byte15 = blocks[blocksOffset++] & 0xFF;
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final int byte16 = blocks[blocksOffset++] & 0xFF;
                int byte16 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte14 & 63) << 13) | (byte15 << 5) | ((int)((uint)byte16 >> 3));
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final int byte17 = blocks[blocksOffset++] & 0xFF;
                int byte17 = blocks[blocksOffset++] & 0xFF;
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final int byte18 = blocks[blocksOffset++] & 0xFF;
                int byte18 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte16 & 7) << 16) | (byte17 << 8) | byte18;
            }
        }

        public override void Decode(long[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final long block0 = blocks[blocksOffset++];
                long block0 = blocks[blocksOffset++];
                values[valuesOffset++] = (long)((ulong)block0 >> 45);
                values[valuesOffset++] = ((long)((ulong)block0 >> 26)) & 524287L;
                values[valuesOffset++] = ((long)((ulong)block0 >> 7)) & 524287L;
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final long block1 = blocks[blocksOffset++];
                long block1 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block0 & 127L) << 12) | ((long)((ulong)block1 >> 52));
                values[valuesOffset++] = ((long)((ulong)block1 >> 33)) & 524287L;
                values[valuesOffset++] = ((long)((ulong)block1 >> 14)) & 524287L;
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final long block2 = blocks[blocksOffset++];
                long block2 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block1 & 16383L) << 5) | ((long)((ulong)block2 >> 59));
                values[valuesOffset++] = ((long)((ulong)block2 >> 40)) & 524287L;
                values[valuesOffset++] = ((long)((ulong)block2 >> 21)) & 524287L;
                values[valuesOffset++] = ((long)((ulong)block2 >> 2)) & 524287L;
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final long block3 = blocks[blocksOffset++];
                long block3 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block2 & 3L) << 17) | ((long)((ulong)block3 >> 47));
                values[valuesOffset++] = ((long)((ulong)block3 >> 28)) & 524287L;
                values[valuesOffset++] = ((long)((ulong)block3 >> 9)) & 524287L;
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final long block4 = blocks[blocksOffset++];
                long block4 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block3 & 511L) << 10) | ((long)((ulong)block4 >> 54));
                values[valuesOffset++] = ((long)((ulong)block4 >> 35)) & 524287L;
                values[valuesOffset++] = ((long)((ulong)block4 >> 16)) & 524287L;
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final long block5 = blocks[blocksOffset++];
                long block5 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block4 & 65535L) << 3) | ((long)((ulong)block5 >> 61));
                values[valuesOffset++] = ((long)((ulong)block5 >> 42)) & 524287L;
                values[valuesOffset++] = ((long)((ulong)block5 >> 23)) & 524287L;
                values[valuesOffset++] = ((long)((ulong)block5 >> 4)) & 524287L;
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final long block6 = blocks[blocksOffset++];
                long block6 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block5 & 15L) << 15) | ((long)((ulong)block6 >> 49));
                values[valuesOffset++] = ((long)((ulong)block6 >> 30)) & 524287L;
                values[valuesOffset++] = ((long)((ulong)block6 >> 11)) & 524287L;
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final long block7 = blocks[blocksOffset++];
                long block7 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block6 & 2047L) << 8) | ((long)((ulong)block7 >> 56));
                values[valuesOffset++] = ((long)((ulong)block7 >> 37)) & 524287L;
                values[valuesOffset++] = ((long)((ulong)block7 >> 18)) & 524287L;
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final long block8 = blocks[blocksOffset++];
                long block8 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block7 & 262143L) << 1) | ((long)((ulong)block8 >> 63));
                values[valuesOffset++] = ((long)((ulong)block8 >> 44)) & 524287L;
                values[valuesOffset++] = ((long)((ulong)block8 >> 25)) & 524287L;
                values[valuesOffset++] = ((long)((ulong)block8 >> 6)) & 524287L;
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final long block9 = blocks[blocksOffset++];
                long block9 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block8 & 63L) << 13) | ((long)((ulong)block9 >> 51));
                values[valuesOffset++] = ((long)((ulong)block9 >> 32)) & 524287L;
                values[valuesOffset++] = ((long)((ulong)block9 >> 13)) & 524287L;
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final long block10 = blocks[blocksOffset++];
                long block10 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block9 & 8191L) << 6) | ((long)((ulong)block10 >> 58));
                values[valuesOffset++] = ((long)((ulong)block10 >> 39)) & 524287L;
                values[valuesOffset++] = ((long)((ulong)block10 >> 20)) & 524287L;
                values[valuesOffset++] = ((long)((ulong)block10 >> 1)) & 524287L;
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final long block11 = blocks[blocksOffset++];
                long block11 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block10 & 1L) << 18) | ((long)((ulong)block11 >> 46));
                values[valuesOffset++] = ((long)((ulong)block11 >> 27)) & 524287L;
                values[valuesOffset++] = ((long)((ulong)block11 >> 8)) & 524287L;
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final long block12 = blocks[blocksOffset++];
                long block12 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block11 & 255L) << 11) | ((long)((ulong)block12 >> 53));
                values[valuesOffset++] = ((long)((ulong)block12 >> 34)) & 524287L;
                values[valuesOffset++] = ((long)((ulong)block12 >> 15)) & 524287L;
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final long block13 = blocks[blocksOffset++];
                long block13 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block12 & 32767L) << 4) | ((long)((ulong)block13 >> 60));
                values[valuesOffset++] = ((long)((ulong)block13 >> 41)) & 524287L;
                values[valuesOffset++] = ((long)((ulong)block13 >> 22)) & 524287L;
                values[valuesOffset++] = ((long)((ulong)block13 >> 3)) & 524287L;
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final long block14 = blocks[blocksOffset++];
                long block14 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block13 & 7L) << 16) | ((long)((ulong)block14 >> 48));
                values[valuesOffset++] = ((long)((ulong)block14 >> 29)) & 524287L;
                values[valuesOffset++] = ((long)((ulong)block14 >> 10)) & 524287L;
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final long block15 = blocks[blocksOffset++];
                long block15 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block14 & 1023L) << 9) | ((long)((ulong)block15 >> 55));
                values[valuesOffset++] = ((long)((ulong)block15 >> 36)) & 524287L;
                values[valuesOffset++] = ((long)((ulong)block15 >> 17)) & 524287L;
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final long block16 = blocks[blocksOffset++];
                long block16 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block15 & 131071L) << 2) | ((long)((ulong)block16 >> 62));
                values[valuesOffset++] = ((long)((ulong)block16 >> 43)) & 524287L;
                values[valuesOffset++] = ((long)((ulong)block16 >> 24)) & 524287L;
                values[valuesOffset++] = ((long)((ulong)block16 >> 5)) & 524287L;
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final long block17 = blocks[blocksOffset++];
                long block17 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block16 & 31L) << 14) | ((long)((ulong)block17 >> 50));
                values[valuesOffset++] = ((long)((ulong)block17 >> 31)) & 524287L;
                values[valuesOffset++] = ((long)((ulong)block17 >> 12)) & 524287L;
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final long block18 = blocks[blocksOffset++];
                long block18 = blocks[blocksOffset++];
                values[valuesOffset++] = ((block17 & 4095L) << 7) | ((long)((ulong)block18 >> 57));
                values[valuesOffset++] = ((long)((ulong)block18 >> 38)) & 524287L;
                values[valuesOffset++] = ((long)((ulong)block18 >> 19)) & 524287L;
                values[valuesOffset++] = block18 & 524287L;
            }
        }

        public override void Decode(sbyte[] blocks, int blocksOffset, long[] values, int valuesOffset, int iterations)
        {
            for (int i = 0; i < iterations; ++i)
            {
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final long byte0 = blocks[blocksOffset++] & 0xFF;
                long byte0 = blocks[blocksOffset++] & 0xFF;
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final long byte1 = blocks[blocksOffset++] & 0xFF;
                long byte1 = blocks[blocksOffset++] & 0xFF;
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final long byte2 = blocks[blocksOffset++] & 0xFF;
                long byte2 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = (byte0 << 11) | (byte1 << 3) | ((long)((ulong)byte2 >> 5));
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final long byte3 = blocks[blocksOffset++] & 0xFF;
                long byte3 = blocks[blocksOffset++] & 0xFF;
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final long byte4 = blocks[blocksOffset++] & 0xFF;
                long byte4 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte2 & 31) << 14) | (byte3 << 6) | ((long)((ulong)byte4 >> 2));
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final long byte5 = blocks[blocksOffset++] & 0xFF;
                long byte5 = blocks[blocksOffset++] & 0xFF;
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final long byte6 = blocks[blocksOffset++] & 0xFF;
                long byte6 = blocks[blocksOffset++] & 0xFF;
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final long byte7 = blocks[blocksOffset++] & 0xFF;
                long byte7 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte4 & 3) << 17) | (byte5 << 9) | (byte6 << 1) | ((long)((ulong)byte7 >> 7));
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final long byte8 = blocks[blocksOffset++] & 0xFF;
                long byte8 = blocks[blocksOffset++] & 0xFF;
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final long byte9 = blocks[blocksOffset++] & 0xFF;
                long byte9 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte7 & 127) << 12) | (byte8 << 4) | ((long)((ulong)byte9 >> 4));
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final long byte10 = blocks[blocksOffset++] & 0xFF;
                long byte10 = blocks[blocksOffset++] & 0xFF;
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final long byte11 = blocks[blocksOffset++] & 0xFF;
                long byte11 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte9 & 15) << 15) | (byte10 << 7) | ((long)((ulong)byte11 >> 1));
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final long byte12 = blocks[blocksOffset++] & 0xFF;
                long byte12 = blocks[blocksOffset++] & 0xFF;
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final long byte13 = blocks[blocksOffset++] & 0xFF;
                long byte13 = blocks[blocksOffset++] & 0xFF;
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final long byte14 = blocks[blocksOffset++] & 0xFF;
                long byte14 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte11 & 1) << 18) | (byte12 << 10) | (byte13 << 2) | ((long)((ulong)byte14 >> 6));
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final long byte15 = blocks[blocksOffset++] & 0xFF;
                long byte15 = blocks[blocksOffset++] & 0xFF;
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final long byte16 = blocks[blocksOffset++] & 0xFF;
                long byte16 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte14 & 63) << 13) | (byte15 << 5) | ((long)((ulong)byte16 >> 3));
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final long byte17 = blocks[blocksOffset++] & 0xFF;
                long byte17 = blocks[blocksOffset++] & 0xFF;
                //JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
                //ORIGINAL LINE: final long byte18 = blocks[blocksOffset++] & 0xFF;
                long byte18 = blocks[blocksOffset++] & 0xFF;
                values[valuesOffset++] = ((byte16 & 7) << 16) | (byte17 << 8) | byte18;
            }
        }
    }
}