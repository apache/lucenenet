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

    internal class IntBlockIndexOuput : IntIndexOutputIndex
    {
        private readonly IntIndexOutput _outerInstance;
        private long _fp;
        private int _upto;
        private long _lastFp;
        private int _lastUpto;

        public IntBlockIndexOuput(IntIndexOutput outerInstance)
        {
            _outerInstance = outerInstance;
        }

        public override void Mark()
        {
            _fp = _outerInstance.OUTPUT.FilePointer;
            _upto = _outerInstance._upto;
        }

        public override void CopyFrom(IntIndexOutputIndex other, bool copyLast)
        {
            var idx = (IntBlockIndexOuput)other;
            _fp = idx._fp;
            _upto = idx._upto;
            if (copyLast)
            {
                _lastFp = _fp;
                _lastUpto = _upto;
            }
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

        public override string ToString()
        {
            return "fp=" + _fp + " upto=" + _upto;
        }
    }
}
