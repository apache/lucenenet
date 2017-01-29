using Lucene.Net.Codecs.Sep;
using Lucene.Net.Store;
using System.Diagnostics;

namespace Lucene.Net.Codecs.IntBlock
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
    /// Naive int block API that writes vInts.  This is
    ///  expected to give poor performance; it's really only for
    ///  testing the pluggability.  One should typically use pfor instead. 
    /// </summary>


    /// <summary>
    /// Abstract base class that writes fixed-size blocks of ints
    ///  to an IndexOutput.  While this is a simple approach, a
    ///  more performant approach would directly create an impl
    ///  of IntIndexOutput inside Directory.  Wrapping a generic
    ///  IndexInput will likely cost performance.
    /// 
    /// @lucene.experimental
    /// </summary>
    public abstract class FixedIntBlockIndexOutput : IntIndexOutput
    {
        protected readonly IndexOutput m_output;
        private readonly int blockSize;
        protected readonly int[] m_buffer;
        private int upto;

        protected FixedIntBlockIndexOutput(IndexOutput output, int fixedBlockSize)
        {
            blockSize = fixedBlockSize;
            this.m_output = output;
            output.WriteVInt(blockSize);
            m_buffer = new int[blockSize];
        }

        protected abstract void FlushBlock();

        public override AbstractIndex GetIndex()
        {
            return new Index(this);
        }

        private class Index : AbstractIndex
        {
            private readonly FixedIntBlockIndexOutput outerInstance;

            public Index(FixedIntBlockIndexOutput outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            internal long fp;
            internal int upto;
            internal long lastFP;
            internal int lastUpto;

            public override void Mark()
            {
                fp = outerInstance.m_output.FilePointer;
                upto = outerInstance.upto;
            }

            public override void CopyFrom(AbstractIndex other, bool copyLast)
            {
                Index idx = (Index)other;
                fp = idx.fp;
                upto = idx.upto;
                if (copyLast)
                {
                    lastFP = fp;
                    lastUpto = upto;
                }
            }

            public override void Write(DataOutput indexOut, bool absolute)
            {
                if (absolute)
                {
                    indexOut.WriteVInt(upto);
                    indexOut.WriteVLong(fp);
                }
                else if (fp == lastFP)
                {
                    // same block
                    Debug.Assert(upto >= lastUpto);
                    int uptoDelta = upto - lastUpto;
                    indexOut.WriteVInt(uptoDelta << 1 | 1);
                }
                else
                {
                    // new block
                    indexOut.WriteVInt(upto << 1);
                    indexOut.WriteVLong(fp - lastFP);
                }
                lastUpto = upto;
                lastFP = fp;
            }

            public override string ToString()
            {
                return "fp=" + fp + " upto=" + upto;
            }
        }

        public override void Write(int v)
        {
            m_buffer[upto++] = v;
            if (upto == blockSize)
            {
                FlushBlock();
                upto = 0;
            }
        }

        public override void Dispose()
        {
            try
            {
                if (upto > 0)
                {
                    // NOTE: entries in the block after current upto are
                    // invalid
                    FlushBlock();
                }
            }
            finally
            {
                m_output.Dispose();
            }
        }
    }
}