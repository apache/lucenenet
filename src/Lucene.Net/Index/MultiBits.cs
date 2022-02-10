using Lucene.Net.Diagnostics;
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

    using IBits = Lucene.Net.Util.IBits;

    /// <summary>
    /// Concatenates multiple <see cref="IBits"/> together, on every lookup.
    ///
    /// <para/><b>NOTE</b>: this is very costly, as every lookup must
    /// do a binary search to locate the right sub-reader.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    internal sealed class MultiBits : IBits
    {
        private readonly IBits[] subs;

        // length is 1+subs.length (the last entry has the maxDoc):
        private readonly int[] starts;

        private readonly bool sefaultValue;

        public MultiBits(IBits[] subs, int[] starts, bool defaultValue)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(starts.Length == 1 + subs.Length);
            this.subs = subs;
            this.starts = starts;
            this.sefaultValue = defaultValue;
        }

        private bool CheckLength(int reader, int doc)
        {
            int length = starts[1 + reader] - starts[reader];
            if (Debugging.AssertsEnabled) Debugging.Assert(doc - starts[reader] < length, "doc={0} reader={1} starts[reader]={2} length={3}", doc, reader, starts[reader], length);
            return true;
        }

        public bool Get(int doc)
        {
            int reader = ReaderUtil.SubIndex(doc, starts);
            if (Debugging.AssertsEnabled) Debugging.Assert(reader != -1);
            IBits bits = subs[reader];
            if (bits is null)
            {
                return sefaultValue;
            }
            else
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(CheckLength(reader, doc));
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
                if (subs[i] is null)
                {
                    b.Append("s=" + starts[i] + " l=null");
                }
                else
                {
                    b.Append("s=" + starts[i] + " l=" + subs[i].Length + " b=" + subs[i]);
                }
            }
            b.Append(" end=" + starts[subs.Length]);
            return b.ToString();
        }

        /// <summary>
        /// Represents a sub-Bits from
        /// <see cref="MultiBits.GetMatchingSub(Lucene.Net.Index.ReaderSlice)"/>.
        /// </summary>
        public sealed class SubResult
        {
            public bool Matches { get; internal set; }
            public IBits Result { get; internal set; }
        }

        /// <summary>
        /// Returns a sub-Bits matching the provided <paramref name="slice"/>
        /// <para/>
        /// Because <c>null</c> usually has a special meaning for
        /// <see cref="IBits"/> (e.g. no deleted documents), you must check
        /// <see cref="SubResult.Matches"/> instead to ensure the sub was
        /// actually found.
        /// </summary>
        public SubResult GetMatchingSub(ReaderSlice slice)
        {
            int reader = ReaderUtil.SubIndex(slice.Start, starts);
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(reader != -1);
                Debugging.Assert(reader < subs.Length,"slice={0} starts[-1]={1}", slice, starts[starts.Length - 1]);
            }
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

        public int Length => starts[starts.Length - 1];
    }
}