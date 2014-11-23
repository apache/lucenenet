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
    using System;
    using System.Diagnostics;
    
    internal class Index : IntIndexInputIndex 
    {
        private long _fp;
        private int _upto;

        public override void Read(DataInput indexIn, bool absolute)
        {
            if (absolute)
            {
                _upto = indexIn.ReadVInt();
                _fp = indexIn.ReadVLong();
            }
            else
            {
                var uptoDelta = indexIn.ReadVInt();
                if ((uptoDelta & 1) == 1)
                {
                    // same block
                    _upto += (int)((uint)uptoDelta >> 1);
                }
                else
                {
                    // new block
                    _upto = (int)((uint)uptoDelta >> 1);
                    _fp += indexIn.ReadVLong();
                }
            }
            Debug.Assert(_upto < BlockSize);
        }

        public override void Seek(IntIndexInputReader other)
        {
            ((Reader) other).Seek(_fp, _upto);
        }

        public override void CopyFrom(IntIndexInputIndex other)
        {
            var idx = (Index) other;
            _fp = idx._fp;
            _upto = idx._upto;
        }

        public override IntIndexInputIndex Clone()
        {
            return new Index {_fp = _fp, _upto = _upto};
        }

        public override String ToString()
        {
            return String.Format("fp={0} upto={1}", _fp, _upto);
        }
    
    }
}
