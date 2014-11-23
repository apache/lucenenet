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

using System.Diagnostics;
using Lucene.Net.Codecs.Sep;
using Lucene.Net.Store;

namespace Lucene.Net.Codecs.Intblock
{
    internal class Reader : IntIndexInputReader
    {

        private readonly IndexInput _input;
        private readonly VariableIntBlockIndexInput.BlockReader _blockReader;
        private readonly int _blockSize;
        private readonly int[] _pending;

        private int _upto;
        private bool _seekPending;
        private long _pendingFp;
        private long _lastBlockFp = -1;

        public Reader(IndexInput input, int[] pending, VariableIntBlockIndexInput.BlockReader blockReader)
        {
            _input = input;
            _pending = pending;
            _blockSize = pending.Length;
            _blockReader = blockReader;
            _upto = _blockSize;
        }

        private void Seek(long fp, int upto)
        {
            Debug.Assert(upto < _blockSize);
            
            if (_seekPending || fp != _lastBlockFp)
            {
                _pendingFp = fp;
                _seekPending = true;
            }
            
            _upto = upto;
        }

        public override int Next()
        {
            if (_seekPending)
            {
                // Seek & load new block
                _input.Seek(_pendingFp);
                _lastBlockFp = _pendingFp;
                _blockReader.readBlock();
                _seekPending = false;
            }
            else if (_upto == _blockSize)
            {
                // Load new block
                _lastBlockFp = _input.FilePointer;
                _blockReader.readBlock();
                _upto = 0;
            }

            return _pending[_upto++];
        }
    }
}

