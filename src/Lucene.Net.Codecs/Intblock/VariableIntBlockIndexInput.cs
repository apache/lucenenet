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

    // TODO: much of this can be shared code w/ the fixed case

    /// <summary>
    /// Abstract base class that reads variable-size blocks of ints
    /// from an IndexInput.  While this is a simple approach, a
    /// more performant approach would directly create an impl
    /// of IntIndexInput inside Directory.  Wrapping a generic
    /// IndexInput will likely cost performance.
    /// 
    /// @lucene.experimental
    /// </summary>
    public abstract class VariableIntBlockIndexInput : IntIndexInput
    {
        private readonly IndexInput input; // in
        protected readonly int maxBlockSize;

        protected internal VariableIntBlockIndexInput(IndexInput input)
        {
            this.input = input;
            maxBlockSize = input.ReadInt();
        }

        public override IntIndexInputReader Reader()
        {
            var buffer = new int[maxBlockSize];
            var clone = (IndexInput)input.Clone();
            // TODO: can this be simplified?
            return new InputReader(clone, buffer, GetBlockReader(clone, buffer));
        }

        public override void Dispose()
        {
            input.Dispose();
        }

        public override IntIndexInputIndex Index()
        {
            return new InputIndex(this);
        }

        protected abstract IBlockReader GetBlockReader(IndexInput @in, int[] buffer);

        /// <summary>
        /// Interface for variable-size block decoders.
        /// <para>
        /// Implementations should decode into the buffer in <see cref="ReadBlock()"/>.
        /// </para>
        /// </summary>
        public interface IBlockReader
        {
            int ReadBlock();
            void Seek(long pos);
        }

        private class InputReader : IntIndexInputReader // LUCENENET TODO: Rename Reader
        {
            private readonly IndexInput input;

            public readonly int[] pending;
            private int upto;

            private bool seekPending;
            private long pendingFP;
            private int pendingUpto;
            private long lastBlockFP;
            private int blockSize;
            private readonly IBlockReader blockReader;

            public InputReader(IndexInput input, int[] pending, IBlockReader blockReader)
            {
                this.input = input;
                this.pending = pending;
                this.blockReader = blockReader;
            }

            internal virtual void Seek(long fp, int upto)
            {
                // TODO: should we do this in real-time, not lazy?
                pendingFP = fp;
                pendingUpto = upto;
                Debug.Assert(pendingUpto >= 0, "pendingUpto=" + pendingUpto);
                seekPending = true;
            }

            private void MaybeSeek()
            {
                if (seekPending)
                {
                    if (pendingFP != lastBlockFP)
                    {
                        // need new block
                        input.Seek(pendingFP);
                        blockReader.Seek(pendingFP);
                        lastBlockFP = pendingFP;
                        blockSize = blockReader.ReadBlock();
                    }
                    upto = pendingUpto;

                    // TODO: if we were more clever when writing the
                    // index, such that a seek point wouldn't be written
                    // until the int encoder "committed", we could avoid
                    // this (likely minor) inefficiency:

                    // This is necessary for int encoders that are
                    // non-causal, ie must see future int values to
                    // encode the current ones.
                    while (upto >= blockSize)
                    {
                        upto -= blockSize;
                        lastBlockFP = input.FilePointer;
                        blockSize = blockReader.ReadBlock();
                    }
                    seekPending = false;
                }
            }

            public override int Next()
            {
                this.MaybeSeek();
                if (upto == blockSize)
                {
                    lastBlockFP = input.FilePointer;
                    blockSize = blockReader.ReadBlock();
                    upto = 0;
                }

                return pending[upto++];
            }
        }

        private class InputIndex : IntIndexInputIndex // LUCENENET TODO: Rename Index
        {
            private readonly VariableIntBlockIndexInput outerInstance;

            public InputIndex(VariableIntBlockIndexInput outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            private long fp;
            private int upto;

            public override void Read(DataInput indexIn, bool absolute)
            {
                if (absolute)
                {
                    upto = indexIn.ReadVInt();
                    fp = indexIn.ReadVLong();
                }
                else
                {
                    int uptoDelta = indexIn.ReadVInt();
                    if ((uptoDelta & 1) == 1)
                    {
                        // same block
                        upto += (int)((uint)uptoDelta >> 1);
                    }
                    else
                    {
                        // new block
                        upto = (int)((uint)uptoDelta >> 1);
                        fp += indexIn.ReadVLong();
                    }
                }
                // TODO: we can't do this assert because non-causal
                // int encoders can have upto over the buffer size
                //assert upto < maxBlockSize: "upto=" + upto + " max=" + maxBlockSize;
            }

            public override string ToString()
            {
                return "VarIntBlock.Index fp=" + fp + " upto=" + upto + " maxBlock=" + outerInstance.maxBlockSize;
            }

            public override void Seek(IntIndexInputReader other)
            {
                ((InputReader)other).Seek(fp, upto);
            }

            public override void CopyFrom(IntIndexInputIndex other)
            {
                InputIndex idx = (InputIndex)other;
                fp = idx.fp;
                upto = idx.upto;
            }

            public override IntIndexInputIndex Clone()
            {
                InputIndex other = new InputIndex(outerInstance);
                other.fp = fp;
                other.upto = upto;
                return other;
            }
        }
    }
}