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

using Lucene.Net.Codecs.Sep;
using Lucene.Net.Store;

namespace Lucene.Net.Codecs.Intblock
{

    /// <summary>
    /// Naive int block API that writes vInts.  This is
    /// expected to give poor performance; it's really only for
    /// testing the pluggability.  One should typically use pfor instead. */
    ///
    /// Abstract base class that reads fixed-size blocks of ints
    /// from an IndexInput.  While this is a simple approach, a
    /// more performant approach would directly create an impl
    /// of IntIndexInput inside Directory.  Wrapping a generic
    /// IndexInput will likely cost performance.
    /// 
    /// @lucene.experimental
    /// </summary>
    public abstract class FixedIntBlockIndexInput : IntIndexInput
    {

        private readonly IndexInput _input;
        private readonly int _blockSize;

        protected FixedIntBlockIndexInput(IndexInput input)
        {
            _input = input;
            _blockSize = input.ReadVInt();
        }

        public override IntIndexInputReader Reader()
        {
            var buffer = new int[_blockSize];
            var clone = (IndexInput)_input.Clone();

            // TODO: can this be simplified?
            return new Reader(clone, buffer, BlockReader(clone, buffer));
        }

        public override void Dispose()
        {
            _input.Dispose();
        }

        public override IntIndexInputIndex Index()
        {
            return new Index();
        }

        protected abstract VariableIntBlockIndexInput.BlockReader BlockReader(IndexInput input, int[] buffer);

    }
}