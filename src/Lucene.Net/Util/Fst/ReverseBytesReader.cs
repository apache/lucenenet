using System.Runtime.CompilerServices;

namespace Lucene.Net.Util.Fst
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
    /// Reads in reverse from a single <see cref="T:byte[]"/>. </summary>
    internal sealed class ReverseBytesReader : FST.BytesReader
    {
        private readonly byte[] bytes;
        private int pos;

        public ReverseBytesReader(byte[] bytes)
        {
            this.bytes = bytes;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override byte ReadByte()
        {
            return bytes[pos--];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void ReadBytes(byte[] b, int offset, int len)
        {
            for (int i = 0; i < len; i++)
            {
                b[offset + i] = bytes[pos--];
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override void SkipBytes(int count)
        {
            pos -= count;
        }

        public override long Position
        {
            get => pos;
            set => this.pos = (int)value;
        }

        public override bool IsReversed => true;
    }
}