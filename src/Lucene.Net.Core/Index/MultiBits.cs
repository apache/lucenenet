using System.Diagnostics;
using System.Text;

namespace Lucene.Net.Index
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

    using Bits = Lucene.Net.Util.Bits;

    /// <summary>
    /// Concatenates multiple Bits together, on every lookup.
    ///
    /// <p><b>NOTE</b>: this is very costly, as every lookup must
    /// do a binary search to locate the right sub-reader.
    ///
    /// @lucene.experimental
    /// </summary>
    internal sealed class MultiBits : Bits
    {
        private readonly Bits[] subs;

        // length is 1+subs.length (the last entry has the maxDoc):
        private readonly int[] starts;

        private readonly bool sefaultValue;

        public MultiBits(Bits[] subs, int[] starts, bool defaultValue)
        {
            Debug.Assert(starts.Length == 1 + subs.Length);
            this.subs = subs;
            this.starts = starts;
            this.sefaultValue = defaultValue;
        }

        private bool CheckLength(int reader, int doc)
        {
            int length = starts[1 + reader] - starts[reader];
            Debug.Assert(doc - starts[reader] < length, "doc=" + doc + " reader=" + reader + " starts[reader]=" + starts[reader] + " length=" + length);
            return true;
        }

        public bool Get(int doc)
        {
            int reader = ReaderUtil.SubIndex(doc, starts);
            Debug.Assert(reader != -1);
            Bits bits = subs[reader];
            if (bits == null)
            {
                return sefaultValue;
            }
            else
            {
                Debug.Assert(CheckLength(reader, doc));
                return bits.Get(doc - starts[reader]);
            }
        }

        public override string ToString()
        {
            StringBuilder b = new StringBuilder();
            b.Append(subs.Length + " subs: ");
            for (int i = 0; i < subs.Length; i++)
            {
                if (i != 0)
                {
                    b.Append("; ");
                }
                if (subs[i] == null)
                {
                    b.Append("s=" + starts[i] + " l=null");
                }
                else
                {
                    b.Append("s=" + starts[i] + " l=" + subs[i].Length() + " b=" + subs[i]);
                }
            }
            b.Append(" end=" + starts[subs.Length]);
            return b.ToString();
        }

        /// <summary>
        /// Represents a sub-Bits from
        /// <seealso cref="MultiBits#getMatchingSub(Lucene.Net.Index.ReaderSlice) getMatchingSub()"/>.
        /// </summary>
        public sealed class SubResult
        {
            public bool Matches { get; internal set; }
            public Bits Result { get; internal set; }
        }

        /// <summary>
        /// Returns a sub-Bits matching the provided <code>slice</code>
        /// <p>
        /// Because <code>null</code> usually has a special meaning for
        /// Bits (e.g. no deleted documents), you must check
        /// <seealso cref="SubResult#matches"/> instead to ensure the sub was
        /// actually found.
        /// </summary>
        public SubResult GetMatchingSub(ReaderSlice slice)
        {
            int reader = ReaderUtil.SubIndex(slice.Start, starts);
            Debug.Assert(reader != -1);
            Debug.Assert(reader < subs.Length, "slice=" + slice + " starts[-1]=" + starts[starts.Length - 1]);
            SubResult subResult = new SubResult();
            if (starts[reader] == slice.Start && starts[1 + reader] == slice.Start + slice.Length)
            {
                subResult.Matches = true;
                subResult.Result = subs[reader];
            }
            else
            {
                subResult.Matches = false;
            }
            return subResult;
        }

        public int Length()
        {
            return starts[starts.Length - 1];
        }
    }
}