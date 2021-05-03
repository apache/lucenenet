using Lucene.Net.Codecs.Sep;
using Lucene.Net.Diagnostics;
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

    // TODO: much of this can be shared code w/ the fixed case

    /// <summary>
    /// Abstract base class that writes variable-size blocks of ints
    /// to an <see cref="IndexOutput"/>.  While this is a simple approach, a
    /// more performant approach would directly create an impl
    /// of <see cref="Int32IndexOutput"/> inside <see cref="Directory"/>.  Wrapping a generic
    /// <see cref="IndexOutput"/> will likely cost performance.
    /// <para/>
    /// NOTE: This was VariableIntBlockIndexOutput in Lucene
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    /// <remarks>
    /// Naive int block API that writes vInts.  This is
    /// expected to give poor performance; it's really only for
    /// testing the pluggability.  One should typically use pfor instead. 
    /// </remarks>
    public abstract class VariableInt32BlockIndexOutput : Int32IndexOutput
    {
        protected readonly IndexOutput m_output;

        private int upto;
        private bool hitExcDuringWrite;

        // TODO what Var-Var codecs exist in practice... and what are there blocksizes like?
        // if its less than 128 we should set that as max and use byte?

        /// <summary>
        /// NOTE: <paramref name="maxBlockSize"/> must be the maximum block size 
        /// plus the max non-causal lookahead of your codec.  EG Simple9
        /// requires lookahead=1 because on seeing the Nth value
        /// it knows it must now encode the N-1 values before it. 
        /// </summary>
        protected VariableInt32BlockIndexOutput(IndexOutput output, int maxBlockSize)
        {
            this.m_output = output;
            this.m_output.WriteInt32(maxBlockSize);
        }

        /// <summary>
        /// Called one value at a time.  Return the number of
        /// buffered input values that have been written to out. 
        /// </summary>
        protected abstract int Add(int value);

        public override Int32IndexOutput.Index GetIndex()
        {
            return new Index(this);
        }

        new private class Index : Int32IndexOutput.Index
        {
            private readonly VariableInt32BlockIndexOutput outerInstance;

            public Index(VariableInt32BlockIndexOutput outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            private long fp;
            private int upto;
            private long lastFP;
            private int lastUpto;

            public override void Mark()
            {
                fp = outerInstance.m_output.Position; // LUCENENET specific: Renamed from getFilePointer() to match FileStream
                upto = outerInstance.upto;
            }

            public override void CopyFrom(Int32IndexOutput.Index other, bool copyLast)
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
                if (Debugging.AssertsEnabled) Debugging.Assert(upto >= 0);
                if (absolute)
                {
                    indexOut.WriteVInt32(upto);
                    indexOut.WriteVInt64(fp);
                }
                else if (fp == lastFP)
                {
                    // same block
                    if (Debugging.AssertsEnabled) Debugging.Assert(upto >= lastUpto);
                    int uptoDelta = upto - lastUpto;
                    indexOut.WriteVInt32(uptoDelta << 1 | 1);
                }
                else
                {
                    // new block
                    indexOut.WriteVInt32(upto << 1);
                    indexOut.WriteVInt64(fp - lastFP);
                }
                lastUpto = upto;
                lastFP = fp;
            }
        }

        public override void Write(int v)
        {
            hitExcDuringWrite = true;
            upto -= Add(v) - 1;
            hitExcDuringWrite = false;
            if (Debugging.AssertsEnabled) Debugging.Assert(upto >= 0);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    if (hitExcDuringWrite) return;

                    // stuff 0s in until the "real" data is flushed:
                    var stuffed = 0;
                    while (upto > stuffed)
                    {
                        upto -= Add(0) - 1;
                        if (Debugging.AssertsEnabled) Debugging.Assert(upto >= 0);
                        stuffed += 1;
                    }
                }
                finally
                {
                    m_output.Dispose();
                }
            }
        }
    }
}