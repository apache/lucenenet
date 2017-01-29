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
    /// expected to give poor performance; it's really only for
    /// testing the pluggability.  One should typically use pfor instead. 
    /// </summary>


    // TODO: much of this can be shared code w/ the fixed case

    /// <summary>
    /// Abstract base class that writes variable-size blocks of ints
    ///  to an IndexOutput.  While this is a simple approach, a
    ///  more performant approach would directly create an impl
    ///  of IntIndexOutput inside Directory.  Wrapping a generic
    ///  IndexInput will likely cost performance.
    /// 
    /// @lucene.experimental
    /// </summary>
    public abstract class VariableIntBlockIndexOutput : IntIndexOutput
    {
        protected readonly IndexOutput output; // out

        private int upto;
        private bool hitExcDuringWrite;

        // TODO what Var-Var codecs exist in practice... and what are there blocksizes like?
        // if its less than 128 we should set that as max and use byte?

        /// <summary>
        /// NOTE: maxBlockSize must be the maximum block size 
        ///  plus the max non-causal lookahead of your codec.  EG Simple9
        ///  requires lookahead=1 because on seeing the Nth value
        ///  it knows it must now encode the N-1 values before it. 
        /// </summary>
        protected VariableIntBlockIndexOutput(IndexOutput output, int maxBlockSize)
        {
            this.output = output;
            this.output.WriteInt(maxBlockSize);
        }

        /// <summary>
        /// Called one value at a time.  Return the number of
        /// buffered input values that have been written to out. 
        /// </summary>
        protected abstract int Add(int value);

        public override IntIndexOutputIndex Index()
        {
            return new OutputIndex(this);
        }

        private class OutputIndex : IntIndexOutputIndex // LUCENENET TODO: Rename Index
        {
            private readonly VariableIntBlockIndexOutput outerInstance;

            public OutputIndex(VariableIntBlockIndexOutput outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            private long fp;
            private int upto;
            private long lastFP;
            private int lastUpto;

            public override void Mark()
            {
                fp = outerInstance.output.FilePointer;
                upto = outerInstance.upto;
            }

            public override void CopyFrom(IntIndexOutputIndex other, bool copyLast)
            {
                OutputIndex idx = (OutputIndex)other;
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
                Debug.Assert(upto >= 0);
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
        }

        public override void Write(int v)
        {
            hitExcDuringWrite = true;
            upto -= Add(v) - 1;
            hitExcDuringWrite = false;
            Debug.Assert(upto >= 0);
        }

        public override void Dispose()
        {
            try
            {
                if (hitExcDuringWrite) return;

                // stuff 0s in until the "real" data is flushed:
                var stuffed = 0;
                while (upto > stuffed)
                {
                    upto -= Add(0) - 1;
                    Debug.Assert(upto >= 0);
                    stuffed += 1;
                }
            }
            finally
            {
                output.Dispose();
            }
        }
    }
}