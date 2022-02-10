using J2N.Numerics;
using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using System.Diagnostics;

namespace Lucene.Net.Codecs.Memory
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

    using DataInput = Store.DataInput;
    using DataOutput = Store.DataOutput;
    using FieldInfo = Index.FieldInfo;
    using IndexOptions = Index.IndexOptions;

    /// <summary>
    /// An FST implementation for 
    /// <see cref="FSTTermsWriter"/>.
    /// <para/>
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
            internal long[] longs;
            internal byte[] bytes;
            internal int docFreq;
            internal long totalTermFreq;

            internal TermData()
            {
                longs = null;
                bytes = null;
                docFreq = 0;
                totalTermFreq = -1;
            }

            internal TermData(long[] longs, byte[] bytes, int docFreq, long totalTermFreq)
            {
                this.longs = longs;
                this.bytes = bytes;
                this.docFreq = docFreq;
                this.totalTermFreq = totalTermFreq;
            }

            // NOTE: actually, FST nodes are seldom 
            // identical when outputs on their arcs 
            // aren't NO_OUTPUTs.
            public override int GetHashCode()
            {
                var hash = 0;
                if (longs != null)
                {
                    var end = longs.Length;
                    for (var i = 0; i < end; i++)
                    {
                        hash -= (int) longs[i];
                    }
                }
                if (bytes != null)
                {
                    hash = -hash;
                    var end = bytes.Length;
                    for (var i = 0; i < end; i++)
                    {
                        hash += bytes[i];
                    }
                }
                hash += (int) (docFreq + totalTermFreq);
                return hash;
            }

            public override bool Equals(object other)
            {
                if (other == this)
                    return true;
                
                if (!(other is TermData))
                    return false;
                
                var _other = (TermData) other;
                return StatsEqual(this, _other) && Int64sEqual(this, _other) && BytesEqual(this, _other);
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
        /// <list type="number">
        ///     <item><description>every value in t1 is not larger than in t2, or</description></item>
        ///     <item><description>every value in t1 is not smaller than t2.</description></item>
        /// </list>
        /// </summary>
        public override TermData Common(TermData t1, TermData t2)
        {
            if (Equals(t1, NO_OUTPUT) || Equals(t2, NO_OUTPUT))
                return NO_OUTPUT;
            
            if (Debugging.AssertsEnabled) Debugging.Assert(t1.longs.Length == t2.longs.Length);

            long[] min = t1.longs, max = t2.longs;
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
                    min = t2.longs;
                    max = t1.longs;
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
            
            if (Debugging.AssertsEnabled) Debugging.Assert(t1.longs.Length == t2.longs.Length);

            int pos = 0;
            long diff = 0;
            var share = new long[_longsSize];

            while (pos < _longsSize)
            {
                share[pos] = t1.longs[pos] - t2.longs[pos];
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
                ret = new TermData(share, t1.bytes, t1.docFreq, t1.totalTermFreq);
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
            
            if (Debugging.AssertsEnabled) Debugging.Assert(t1.longs.Length == t2.longs.Length);

            var pos = 0;
            var accum = new long[_longsSize];

            while (pos < _longsSize)
            {
                accum[pos] = t1.longs[pos] + t2.longs[pos];
                pos++;
            }

            TermData ret;
            if (t2.bytes != null || t2.docFreq > 0)
            {
                ret = new TermData(accum, t2.bytes, t2.docFreq, t2.totalTermFreq);
            }
            else
            {
                ret = new TermData(accum, t1.bytes, t1.docFreq, t1.totalTermFreq);
            }

            return ret;
        }

        public override void Write(TermData data, DataOutput output)
        {
            int bit0 = AllZero(data.longs) ? 0 : 1;
            int bit1 = ((data.bytes is null || data.bytes.Length == 0) ? 0 : 1) << 1;
            int bit2 = ((data.docFreq == 0) ? 0 : 1) << 2;
            int bits = bit0 | bit1 | bit2;
            if (bit1 > 0) // determine extra length
            {
                if (data.bytes.Length < 32)
                {
                    bits |= (data.bytes.Length << 3);
                    output.WriteByte((byte) bits);
                }
                else
                {
                    output.WriteByte((byte) bits);
                    output.WriteVInt32(data.bytes.Length);
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
                    output.WriteVInt64(data.longs[pos]);
                }
            }
            if (bit1 > 0) // bytes exists
            {
                output.WriteBytes(data.bytes, 0, data.bytes.Length);
            }
            if (bit2 > 0) // stats exist
            {
                if (_hasPos)
                {
                    if (data.docFreq == data.totalTermFreq)
                    {
                        output.WriteVInt32((data.docFreq << 1) | 1);
                    }
                    else
                    {
                        output.WriteVInt32((data.docFreq << 1));
                        output.WriteVInt64(data.totalTermFreq - data.docFreq);
                    }
                }
                else
                {
                    output.WriteVInt32(data.docFreq);
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
            var bytesSize = bits.TripleShift(3);
            if (bit1 > 0 && bytesSize == 0) // determine extra length
            {
                bytesSize = input.ReadVInt32();
            }
            if (bit0 > 0) // not all-zero case
            {
                for (int pos = 0; pos < _longsSize; pos++)
                {
                    longs[pos] = input.ReadVInt64();
                }
            }
            if (bit1 > 0) // bytes exists
            {
                bytes = new byte[bytesSize];
                input.ReadBytes(bytes, 0, bytesSize);
            }
            if (bit2 > 0) // stats exist
            {
                int code = input.ReadVInt32();
                if (_hasPos)
                {
                    totalTermFreq = docFreq = code.TripleShift(1);
                    if ((code & 1) == 0)
                    {
                        totalTermFreq += input.ReadVInt64();
                    }
                }
                else
                {
                    docFreq = code;
                }
            }
            return new TermData(longs, bytes, docFreq, totalTermFreq);
        }

        public override TermData NoOutput => NO_OUTPUT;

        public override string OutputToString(TermData data)
        {
            return data.ToString();
        }

        private static bool StatsEqual(TermData t1, TermData t2)
        {
            return t1.docFreq == t2.docFreq && t1.totalTermFreq == t2.totalTermFreq;
        }

        private static bool BytesEqual(TermData t1, TermData t2)
        {
            if (t1.bytes is null && t2.bytes is null)
            {
                return true;
            }
            return t1.bytes != null && t2.bytes != null && Arrays.Equals(t1.bytes, t2.bytes);
        }

        /// <summary>
        /// NOTE: This was longsEqual() in Lucene.
        /// </summary>
        private static bool Int64sEqual(TermData t1, TermData t2)
        {
            if (t1.longs is null && t2.longs is null)
            {
                return true;
            }
            return t1.longs != null && t2.longs != null && Arrays.Equals(t1.longs, t2.longs);
        }

        private static bool AllZero(long[] l)
        {
            for (int i = 0; i < l.Length; i++)
            {
                if (l[i] != 0)
                    return false;
            }
            return true;
        }
    }
}