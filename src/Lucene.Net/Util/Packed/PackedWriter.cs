using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using System.IO;
using System.Runtime.CompilerServices;

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

    using DataOutput = Lucene.Net.Store.DataOutput;

    // Packs high order byte first, to match
    // IndexOutput.writeInt/Long/Short byte order

    internal sealed class PackedWriter : PackedInt32s.Writer
    {
        internal bool finished;
        internal readonly PackedInt32s.Format format;
        internal readonly BulkOperation encoder;
        internal readonly byte[] nextBlocks;
        internal readonly long[] nextValues;
        internal readonly int iterations;
        internal int off;
        internal int written;

        internal PackedWriter(PackedInt32s.Format format, DataOutput @out, int valueCount, int bitsPerValue, int mem)
            : base(@out, valueCount, bitsPerValue)
        {
            this.format = format;
            encoder = BulkOperation.Of(format, bitsPerValue);
            iterations = encoder.ComputeIterations(valueCount, mem);
            nextBlocks = new byte[iterations * encoder.ByteBlockCount];
            nextValues = new long[iterations * encoder.ByteValueCount];
            off = 0;
            written = 0;
            finished = false;
        }

        protected internal override PackedInt32s.Format Format => format;

        public override void Add(long v)
        {
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(m_bitsPerValue == 64 || (v >= 0 && v <= PackedInt32s.MaxValue(m_bitsPerValue)), "{0}", m_bitsPerValue);
                Debugging.Assert(!finished);
            }
            if (m_valueCount != -1 && written >= m_valueCount)
            {
                throw EOFException.Create("Writing past end of stream");
            }
            nextValues[off++] = v;
            if (off == nextValues.Length)
            {
                Flush();
            }
            ++written;
        }

        public override void Finish()
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(!finished);
            if (m_valueCount != -1)
            {
                while (written < m_valueCount)
                {
                    Add(0L);
                }
            }
            Flush();
            finished = true;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void Flush()
        {
            encoder.Encode(nextValues, 0, nextBlocks, 0, iterations);
            int blockCount = (int)format.ByteCount(PackedInt32s.VERSION_CURRENT, off, m_bitsPerValue);
            m_out.WriteBytes(nextBlocks, blockCount);
            Arrays.Fill(nextValues, 0L);
            off = 0;
        }

        public override int Ord => written - 1;
    }
}