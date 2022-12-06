using J2N.Numerics;
using Lucene.Net.Codecs.Sep;
using Lucene.Net.Diagnostics;
using Lucene.Net.Store;
using Lucene.Net.Support;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

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

    // TODO: much of this can be shared code w/ the fixed case

    /// <summary>
    /// Abstract base class that reads variable-size blocks of ints
    /// from an <see cref="IndexInput"/>.  While this is a simple approach, a
    /// more performant approach would directly create an impl
    /// of <see cref="Int32IndexInput"/> inside <see cref="Directory"/>.  Wrapping a generic
    /// <see cref="IndexInput"/> will likely cost performance.
    /// <para/>
    /// NOTE: This was VariableIntBlockIndexInput in Lucene
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    /// <remarks>
    /// Naive int block API that writes vInts.  This is
    /// expected to give poor performance; it's really only for
    /// testing the pluggability.  One should typically use pfor instead. 
    /// </remarks>
    public abstract class VariableInt32BlockIndexInput : Int32IndexInput
    {
        private readonly IndexInput input;
        protected readonly int m_maxBlockSize;

        protected VariableInt32BlockIndexInput(IndexInput input)
        {
            this.input = input;
            m_maxBlockSize = input.ReadInt32();
        }

        public override Int32IndexInput.Reader GetReader()
        {
            var buffer = new int[m_maxBlockSize];
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

        new private class Reader : Int32IndexInput.Reader
        {
            private readonly IndexInput input;

            [WritableArray]
            [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
            public int[] Pending => pending;

            private readonly int[] pending;
            private int upto;

            private bool seekPending;
            private long pendingFP;
            private int pendingUpto;
            private long lastBlockFP;
            private int blockSize;
            private readonly IBlockReader blockReader;

            public Reader(IndexInput input, int[] pending, IBlockReader blockReader)
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
                if (Debugging.AssertsEnabled) Debugging.Assert(pendingUpto >= 0, "pendingUpto={0}", pendingUpto);
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
                        lastBlockFP = input.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
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
                    lastBlockFP = input.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                    blockSize = blockReader.ReadBlock();
                    upto = 0;
                }

                return pending[upto++];
            }
        }

        new private class Index : Int32IndexInput.Index
        {
            private readonly VariableInt32BlockIndexInput outerInstance;

            public Index(VariableInt32BlockIndexInput outerInstance)
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
                        upto += uptoDelta.TripleShift(1);
                    }
                    else
                    {
                        // new block
                        upto = uptoDelta.TripleShift(1);
                        fp += indexIn.ReadVInt64();
                    }
                }
                // TODO: we can't do this assert because non-causal
                // int encoders can have upto over the buffer size
                //assert upto < maxBlockSize: "upto=" + upto + " max=" + maxBlockSize;
            }

            public override string ToString()
            {
                return "VarIntBlock.Index fp=" + fp + " upto=" + upto + " maxBlock=" + outerInstance.m_maxBlockSize;
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
        }
    }
}