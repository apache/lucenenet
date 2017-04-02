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

    // Naive int block API that writes vInts.  This is
    // expected to give poor performance; it's really only for
    // testing the pluggability.  One should typically use pfor instead. 

    /// <summary>
    /// Abstract base class that reads fixed-size blocks of ints
    ///  from an IndexInput.  While this is a simple approach, a
    ///  more performant approach would directly create an impl
    ///  of IntIndexInput inside Directory.  Wrapping a generic
    ///  IndexInput will likely cost performance.
    /// <para/>
    /// NOTE: This was FixedIntBlockIndexInput in Lucene
    /// 
    /// @lucene.experimental
    /// </summary>
    public abstract class FixedInt32BlockIndexInput : Int32IndexInput
    {
        private readonly IndexInput input;
        protected readonly int m_blockSize;

        public FixedInt32BlockIndexInput(IndexInput @in)
        {
            input = @in;
            m_blockSize = @in.ReadVInt32();
        }

        public override Int32IndexInput.Reader GetReader()
        {
            var buffer = new int[m_blockSize];
            var clone = (IndexInput)input.Clone();
            // TODO: can this be simplified?
            return new Reader(clone, buffer, GetBlockReader(clone, buffer));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                input.Dispose();
            }
        }

        public override Int32IndexInput.Index GetIndex()
        {
            return new Index(this);
        }

        protected abstract IBlockReader GetBlockReader(IndexInput @in, int[] buffer);


        /// <summary>
        /// Interface for fixed-size block decoders.
        /// <para>
        /// Implementations should decode into the buffer in <see cref="ReadBlock()"/>.
        /// </para>
        /// </summary>
        public interface IBlockReader
        {
            void ReadBlock();
        }

        new private class Reader : Int32IndexInput.Reader
        {
            private readonly IndexInput input;
            private readonly IBlockReader blockReader;
            private readonly int blockSize;
            private readonly int[] pending;

            private int upto;
            private bool seekPending;
            private long pendingFP;
            private long lastBlockFP = -1;

            public Reader(IndexInput input, int[] pending, IBlockReader blockReader)
            {
                this.input = input;
                this.pending = pending;
                this.blockSize = pending.Length;
                this.blockReader = blockReader;
                upto = blockSize;
            }

            internal virtual void Seek(long fp, int upto)
            {
                Debug.Assert(upto < blockSize);
                if (seekPending || fp != lastBlockFP)
                {
                    pendingFP = fp;
                    seekPending = true;
                }
                this.upto = upto;
            }

            public override int Next()
            {
                if (seekPending)
                {
                    // Seek & load new block
                    input.Seek(pendingFP);
                    lastBlockFP = pendingFP;
                    blockReader.ReadBlock();
                    seekPending = false;
                }
                else if (upto == blockSize)
                {
                    // Load new block
                    lastBlockFP = input.GetFilePointer();
                    blockReader.ReadBlock();
                    upto = 0;
                }
                return pending[upto++];
            }
        }

        new private class Index : Int32IndexInput.Index
        {
            private readonly FixedInt32BlockIndexInput outerInstance;

            public Index(FixedInt32BlockIndexInput outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            private long fp;
            private int upto;

            public override void Read(DataInput indexIn, bool absolute)
            {
                if (absolute)
                {
                    upto = indexIn.ReadVInt32();
                    fp = indexIn.ReadVInt64();
                }
                else
                {
                    int uptoDelta = indexIn.ReadVInt32();
                    if ((uptoDelta & 1) == 1)
                    {
                        // same block
                        upto += (int)((uint)uptoDelta >> 1);
                    }
                    else
                    {
                        // new block
                        upto = (int)((uint)uptoDelta >> 1);
                        fp += indexIn.ReadVInt64();
                    }
                }
                Debug.Assert(upto < outerInstance.m_blockSize);
            }

            public override void Seek(Int32IndexInput.Reader other)
            {
                ((Reader)other).Seek(fp, upto);
            }

            public override void CopyFrom(Int32IndexInput.Index other)
            {
                Index idx = (Index)other;
                fp = idx.fp;
                upto = idx.upto;
            }

            public override object Clone()
            {
                Index other = new Index(outerInstance);
                other.fp = fp;
                other.upto = upto;
                return other;
            }

            public override string ToString()
            {
                return "fp=" + fp + " upto=" + upto;
            }
        }
    }
}