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

namespace Lucene.Net.Codecs.Intblock
{

    using System.Diagnostics;
    using Store;
    using Sep;

    /// <summary>
    /// Naive int block API that writes vInts.  This is expected to give poor 
    /// performance; it's really only for testing the pluggability.  One 
    /// should typically use pfor instead.
    ///
    /// TODO: much of this can be shared code w/ the fixed case
    /// 
    /// Abstract base class that writes variable-size blocks of ints
    /// to an IndexOutput.  While this is a simple approach, a
    /// more performant approach would directly create an impl
    /// of IntIndexOutput inside Directory.  Wrapping a generic
    /// IndexInput will likely cost performance.
    /// 
    /// @lucene.experimental
    /// </summary>
    public abstract class VariableIntBlockIndexOutput : IntIndexOutput
    {
        private readonly IndexOutput _output;
        private int _upto;
        private bool _hitExcDuringWrite;

        /// <Remarks>
        /// TODO what Var-Var codecs exist in practice, and what are their blocksizes like?
        /// If it's less than 128 should we set that as max and use byte?
        /// 
        /// NOTE: maxblockSize must be the maxium block size plus the max non-causal lookahed
        /// of your codec. EG Simple9 requires lookahead=1 becuase on seeing the Nth value it 
        /// knows it must now encode the N-1 values before it
        /// </Remarks>
        protected VariableIntBlockIndexOutput(IndexOutput output, int maxBlockSize)
        {
            _output = output;
            _output.WriteInt(maxBlockSize);
        }

        /// <summary>
        /// Called one value at a time. Return the number of 
        /// buffered input values that have been written out
        /// </summary>
        protected abstract int Add(int value);

        public override IntIndexOutputIndex Index()
        {
            return new VariableIntBlockIndexOutputIndex();
        }

        public override void Write(int v)
        {
            _hitExcDuringWrite = true;
            _upto -= Add(v) - 1;
            _hitExcDuringWrite = false;
            Debug.Assert(_upto >= 0);
        }

        public override void Dispose()
        {
            try
            {
                if (_hitExcDuringWrite) return;

                // stuff 0s in until the "real" data is flushed:
                var stuffed = 0;
                while (_upto > stuffed)
                {
                    _upto -= Add(0) - 1;
                    Debug.Assert(_upto >= 0);
                    stuffed += 1;
                }
            }
            finally
            {
                _output.Dispose();
            }
        }
    }

    internal class VariableIntBlockIndexOutputIndex : IntIndexOutputIndex
    {
        private long _fp;
        private int _upto;
        private long _lastFp;
        private int _lastUpto;

        public override void Mark()
            {
                _fp = output.FilePointer;
                _upto = VariableIntBlockIndexOutput.
                this._upto;
            }

        public override void CopyFrom(IntIndexOutputIndex other, bool copyLast)
        {
            var idx = (Index)other;
            _fp = idx.fp;
            _upto = idx.upto;
            if (!copyLast) return;

            _lastFp = _fp;
            _lastUpto = _upto;
        }

        public override void Write(DataOutput indexOut, bool absolute)
        {
            Debug.Assert(_upto >= 0);
            if (absolute)
            {
                indexOut.WriteVInt(_upto);
                indexOut.WriteVLong(_fp);
            }
            else if (_fp == _lastFp)
            {
                // same block
                Debug.Assert(_upto >= _lastUpto);
                var uptoDelta = _upto - _lastUpto;
                indexOut.WriteVInt(uptoDelta << 1 | 1);
            }
            else
            {
                // new block
                indexOut.WriteVInt(_upto << 1);
                indexOut.WriteVLong(_fp - _lastFp);
            }
            _lastUpto = _upto;
            _lastFp = _fp;
        }
    }
}