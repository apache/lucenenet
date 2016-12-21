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

using System.Linq;

namespace Lucene.Net.Codecs.Memory
{

    using System.Diagnostics;
    using Support;

    using FieldInfo = Index.FieldInfo;
    using IndexOptions = Index.IndexOptions;
    using DataInput = Store.DataInput;
    using DataOutput = Store.DataOutput;
   
    /// <summary>
    /// An FST implementation for 
    /// <seealso cref="FSTTermsWriter"/>.
    /// 
    /// @lucene.experimental
    /// </summary>

    // NOTE: outputs should be per-field, since
    // longsSize is fixed for each field
    internal class FSTTermOutputs : Util.Fst.Outputs<FSTTermOutputs.TermData>
    {
        private static readonly TermData NO_OUTPUT = new TermData();
        private readonly bool _hasPos;
        private readonly int _longsSize;

        /// <summary>
        /// Represents the metadata for one term.
        /// On an FST, only long[] part is 'shared' and pushed towards root.
        /// byte[] and term stats will be kept on deeper arcs.
        /// </summary>
        internal class TermData
        {
            internal long[] LONGS;
            internal byte[] BYTES;
            internal int DOC_FREQ;
            internal long TOTAL_TERM_FREQ;

            internal TermData()
            {
                LONGS = null;
                BYTES = null;
                DOC_FREQ = 0;
                TOTAL_TERM_FREQ = -1;
            }

            internal TermData(long[] longs, byte[] bytes, int docFreq, long totalTermFreq)
            {
                LONGS = longs;
                BYTES = bytes;
                DOC_FREQ = docFreq;
                TOTAL_TERM_FREQ = totalTermFreq;
            }

            // NOTE: actually, FST nodes are seldom 
            // identical when outputs on their arcs 
            // aren't NO_OUTPUTs.
            public override int GetHashCode()
            {
                var hash = 0;
                if (LONGS != null)
                {
                    var end = LONGS.Length;
                    for (var i = 0; i < end; i++)
                    {
                        hash -= (int) LONGS[i];
                    }
                }
                if (BYTES != null)
                {
                    hash = -hash;
                    var end = BYTES.Length;
                    for (var i = 0; i < end; i++)
                    {
                        hash += BYTES[i];
                    }
                }
                hash += (int) (DOC_FREQ + TOTAL_TERM_FREQ);
                return hash;
            }

            public override bool Equals(object other)
            {
                if (other == this)
                    return true;
                
                if (!(other is TermData))
                    return false;
                
                var _other = (TermData) other;
                return StatsEqual(this, _other) && LongsEqual(this, _other) && BytesEqual(this, _other);
            }

        }

        protected internal FSTTermOutputs(FieldInfo fieldInfo, int longsSize)
        {
            _hasPos = (fieldInfo.IndexOptions != IndexOptions.DOCS_ONLY);
            _longsSize = longsSize;
        }

        /// <summary>
        /// The return value will be the smaller one, when these two are 
        /// 'comparable', i.e. 
        /// 1. every value in t1 is not larger than in t2, or
        /// 2. every value in t1 is not smaller than t2.
        /// </summary>
        /// <param name="t1"></param>
        /// <param name="t2"></param>
        /// <returns></returns>
        public override TermData Common(TermData t1, TermData t2)
        {
            if (Equals(t1, NO_OUTPUT) || Equals(t2, NO_OUTPUT))
                return NO_OUTPUT;
            
            Debug.Assert(t1.LONGS.Length == t2.LONGS.Length);

            long[] min = t1.LONGS, max = t2.LONGS;
            int pos = 0;
            TermData ret;

            while (pos < _longsSize && min[pos] == max[pos])
            {
                pos++;
            }
            if (pos < _longsSize) // unequal long[]
            {
                if (min[pos] > max[pos])
                {
                    min = t2.LONGS;
                    max = t1.LONGS;
                }
                // check whether strictly smaller
                while (pos < _longsSize && min[pos] <= max[pos])
                {
                    pos++;
                }
                if (pos < _longsSize || AllZero(min)) // not comparable or all-zero
                {
                    ret = NO_OUTPUT;
                }
                else
                {
                    ret = new TermData(min, null, 0, -1);
                }
            } // equal long[]
            else
            {
                if (StatsEqual(t1, t2) && BytesEqual(t1, t2))
                {
                    ret = t1;
                }
                else if (AllZero(min))
                {
                    ret = NO_OUTPUT;
                }
                else
                {
                    ret = new TermData(min, null, 0, -1);
                }
            }
            //if (TEST) System.out.println("ret:"+ret);
            return ret;
        }

        public override TermData Subtract(TermData t1, TermData t2)
        {
            if (Equals(t2, NO_OUTPUT))
                return t1;
            
            Debug.Assert(t1.LONGS.Length == t2.LONGS.Length);

            int pos = 0;
            long diff = 0;
            var share = new long[_longsSize];

            while (pos < _longsSize)
            {
                share[pos] = t1.LONGS[pos] - t2.LONGS[pos];
                diff += share[pos];
                pos++;
            }

            TermData ret;
            if (diff == 0 && StatsEqual(t1, t2) && BytesEqual(t1, t2))
            {
                ret = NO_OUTPUT;
            }
            else
            {
                ret = new TermData(share, t1.BYTES, t1.DOC_FREQ, t1.TOTAL_TERM_FREQ);
            }
            //if (TEST) System.out.println("ret:"+ret);
            return ret;
        }

        // TODO: if we refactor a 'addSelf(TermData other)',
        // we can gain about 5~7% for fuzzy queries, however this also 
        // means we are putting too much stress on FST Outputs decoding?
        public override TermData Add(TermData t1, TermData t2)
        {
            if (Equals(t1, NO_OUTPUT))
                return t2;
            
            if (Equals(t2, NO_OUTPUT))
                return t1;
            
            Debug.Assert(t1.LONGS.Length == t2.LONGS.Length);

            var pos = 0;
            var accum = new long[_longsSize];

            while (pos < _longsSize)
            {
                accum[pos] = t1.LONGS[pos] + t2.LONGS[pos];
                pos++;
            }

            TermData ret;
            if (t2.BYTES != null || t2.DOC_FREQ > 0)
            {
                ret = new TermData(accum, t2.BYTES, t2.DOC_FREQ, t2.TOTAL_TERM_FREQ);
            }
            else
            {
                ret = new TermData(accum, t1.BYTES, t1.DOC_FREQ, t1.TOTAL_TERM_FREQ);
            }

            return ret;
        }

        public override void Write(TermData data, DataOutput output)
        {
            int bit0 = AllZero(data.LONGS) ? 0 : 1;
            int bit1 = ((data.BYTES == null || data.BYTES.Length == 0) ? 0 : 1) << 1;
            int bit2 = ((data.DOC_FREQ == 0) ? 0 : 1) << 2;
            int bits = bit0 | bit1 | bit2;
            if (bit1 > 0) // determine extra length
            {
                if (data.BYTES.Length < 32)
                {
                    bits |= (data.BYTES.Length << 3);
                    output.WriteByte((byte) bits);
                }
                else
                {
                    output.WriteByte((byte) bits);
                    output.WriteVInt(data.BYTES.Length);
                }
            }
            else
            {
                output.WriteByte((byte) bits);
            }
            if (bit0 > 0) // not all-zero case
            {
                for (int pos = 0; pos < _longsSize; pos++)
                {
                    output.WriteVLong(data.LONGS[pos]);
                }
            }
            if (bit1 > 0) // bytes exists
            {
                output.WriteBytes(data.BYTES, 0, data.BYTES.Length);
            }
            if (bit2 > 0) // stats exist
            {
                if (_hasPos)
                {
                    if (data.DOC_FREQ == data.TOTAL_TERM_FREQ)
                    {
                        output.WriteVInt((data.DOC_FREQ << 1) | 1);
                    }
                    else
                    {
                        output.WriteVInt((data.DOC_FREQ << 1));
                        output.WriteVLong(data.TOTAL_TERM_FREQ - data.DOC_FREQ);
                    }
                }
                else
                {
                    output.WriteVInt(data.DOC_FREQ);
                }
            }
        }

        public override TermData Read(DataInput input)
        {
            var longs = new long[_longsSize];
            byte[] bytes = null;
            int docFreq = 0;
            long totalTermFreq = -1;
            int bits = input.ReadByte() & 0xff;
            int bit0 = bits & 1;
            int bit1 = bits & 2;
            int bit2 = bits & 4;
            var bytesSize = ((int) ((uint) bits >> 3));
            if (bit1 > 0 && bytesSize == 0) // determine extra length
            {
                bytesSize = input.ReadVInt();
            }
            if (bit0 > 0) // not all-zero case
            {
                for (int pos = 0; pos < _longsSize; pos++)
                {
                    longs[pos] = input.ReadVLong();
                }
            }
            if (bit1 > 0) // bytes exists
            {
                bytes = new byte[bytesSize];
                input.ReadBytes(bytes, 0, bytesSize);
            }
            if (bit2 > 0) // stats exist
            {
                int code = input.ReadVInt();
                if (_hasPos)
                {
                    totalTermFreq = docFreq = (int) ((uint) code >> 1);
                    if ((code & 1) == 0)
                    {
                        totalTermFreq += input.ReadVLong();
                    }
                }
                else
                {
                    docFreq = code;
                }
            }
            return new TermData(longs, bytes, docFreq, totalTermFreq);
        }

        public override TermData NoOutput
        {
            get { return NO_OUTPUT; }
        }

        public override string OutputToString(TermData data)
        {
            return data.ToString();
        }

        internal static bool StatsEqual(TermData t1, TermData t2)
        {
            return t1.DOC_FREQ == t2.DOC_FREQ && t1.TOTAL_TERM_FREQ == t2.TOTAL_TERM_FREQ;
        }

        internal static bool BytesEqual(TermData t1, TermData t2)
        {
            if (t1.BYTES == null && t2.BYTES == null)
            {
                return true;
            }
            return t1.BYTES != null && t2.BYTES != null && Arrays.Equals(t1.BYTES, t2.BYTES);
        }

        internal static bool LongsEqual(TermData t1, TermData t2)
        {
            if (t1.LONGS == null && t2.LONGS == null)
            {
                return true;
            }
            return t1.LONGS != null && t2.LONGS != null && Arrays.Equals(t1.LONGS, t2.LONGS);
        }

        internal static bool AllZero(long[] l)
        {
            return l.All(t => t == 0);
        }
    }
}