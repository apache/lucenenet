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

    internal class IntBlockIndexInput : IntIndexInputIndex
    {
        private readonly IntIndexInput _outerInstance;
        private long _fp;
        private int _upto;

        public IntBlockIndexInput(IntIndexInput outerInstance)
        {
            _outerInstance = outerInstance;
        }

        public override void Read(DataInput indexIn, bool absolute)
        {
            if (absolute)
            {
                _upto = indexIn.ReadVInt();
                _fp = indexIn.ReadVLong();
            }
            else
            {
                int uptoDelta = indexIn.ReadVInt();
                if ((uptoDelta & 1) == 1)
                {
                    // same block
                    _upto += (int) ((uint) uptoDelta >> 1);
                }
                else
                {
                    // new block
                    _upto = (int) ((uint) uptoDelta >> 1);
                    _fp += indexIn.ReadVLong();
                }
            }
            // TODO: we can't do this assert because non-causal
            // int encoders can have upto over the buffer size
            //assert upto < maxBlockSize: "upto=" + upto + " max=" + maxBlockSize;
        }

        public override string ToString()
        {
            return "VarIntBlock.Index fp=" + _fp + " upto=" + _upto;
        }

        public override void Seek(IntIndexInputReader other)
        {
            ((IntBlockIndexReader)other).Seek(_fp, _upto);
        }

        public override void CopyFrom(IntIndexInputIndex other)
        {
            var idx = (IntBlockIndexInput)other;
            _fp = idx._fp;
            _upto = idx._upto;
        }

        public override IntIndexInputIndex Clone()
        {
            var other = new IntBlockIndexInput(_outerInstance) {_fp = _fp, _upto = _upto};
            return other;
        }
    }
}
