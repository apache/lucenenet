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
    using Sep;
    using IntIndexOutput = Sep.IntIndexOutput;
    using IndexOutput = Store.IndexOutput;

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
        private readonly int _blockSize;
        protected internal readonly int[] BUFFER;

        protected internal FixedIntBlockIndexOutput(IndexOutput output, int fixedBlockSize)
        {
            _blockSize = fixedBlockSize;
            OUTPUT = output;
            output.WriteVInt(_blockSize);
            BUFFER = new int[_blockSize];
        }

        protected internal abstract void FlushBlock();

        public override IntIndexOutputIndex Index()
        {
            return new IntBlockIndexOuput(this);
        }

        public override void Write(int v)
        {
            BUFFER[_upto++] = v;
            if (_upto == _blockSize)
            {
                FlushBlock();
                _upto = 0;
            }
        }

        public override void Dispose()
        {
            try
            {
                if (_upto > 0)
                {
                    // NOTE: entries in the block after current upto are
                    // invalid
                    FlushBlock();
                }
            }
            finally
            {
                OUTPUT.Dispose();
            }
        }
    }

}