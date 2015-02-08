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
    using Store;
    using System.Diagnostics;

    internal class IntBlockIndexReader : IntIndexInputReader
    {
        private readonly IndexInput _input;

        public readonly int[] PENDING;
        private int _upto;

        private bool _seekPending;
        private long _pendingFp;
        private int _pendingUpto;
        private long _lastBlockFp;
        private int _blockSize;
        private readonly IBlockReader _blockReader;

        public IntBlockIndexReader(IndexInput input, int[] pending, IBlockReader blockReader)
        {
            _input = input;
            PENDING = pending;
            _blockReader = blockReader;
            _blockSize = pending.Length;
            _upto = _blockSize;
        }

        internal virtual void Seek(long fp, int upto)
        {
            // TODO: should we do this in real-time, not lazy?
            _pendingFp = fp;
            _pendingUpto = upto;
            Debug.Assert(_pendingUpto >= 0, "pendingUpto=" + _pendingUpto);
            _seekPending = true;
        }

        internal void MaybeSeek()
        {
            if (!_seekPending) return;

            if (_pendingFp != _lastBlockFp)
            {
                // need new block
                _input.Seek(_pendingFp);
                _lastBlockFp = _pendingFp;
                _blockReader.Seek(_pendingFp);
                _blockSize = _blockReader.ReadBlock();
            }
            _upto = _pendingUpto;

            // TODO: if we were more clever when writing the
            // index, such that a seek point wouldn't be written
            // until the int encoder "committed", we could avoid
            // this (likely minor) inefficiency:

            // This is necessary for int encoders that are
            // non-causal, ie must see future int values to
            // encode the current ones.
            while (_upto >= _blockSize)
            {
                _upto -= _blockSize;
                _lastBlockFp = _input.FilePointer;
                _blockSize = _blockReader.ReadBlock();
            }
            _seekPending = false;
        }

        public override int Next()
        {
            MaybeSeek();
            if (_upto == _blockSize)
            {
                _lastBlockFp = _input.FilePointer;
                _blockSize = _blockReader.ReadBlock();
                _upto = 0;
            }

            return PENDING[_upto++];
        }
    }
}
