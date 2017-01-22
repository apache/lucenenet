using Lucene.Net.Support;

namespace Lucene.Net.Codecs
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

    using BytesRef = Lucene.Net.Util.BytesRef;
    using DocsAndPositionsEnum = Lucene.Net.Index.DocsAndPositionsEnum;
    using MergeState = Lucene.Net.Index.MergeState;
    using MultiDocsAndPositionsEnum = Lucene.Net.Index.MultiDocsAndPositionsEnum;

    /// <summary>
    /// Exposes flex API, merged from flex API of sub-segments,
    /// remapping docIDs (this is used for segment merging).
    ///
    /// @lucene.experimental
    /// </summary>
    public sealed class MappingMultiDocsAndPositionsEnum : DocsAndPositionsEnum
    {
        private MultiDocsAndPositionsEnum.EnumWithSlice[] subs;
        internal int numSubs;
        internal int upto;
        internal MergeState.DocMap currentMap;
        internal DocsAndPositionsEnum current;
        internal int currentBase;
        internal int doc = -1;
        private MergeState mergeState;

        /// <summary>
        /// Sole constructor. </summary>
        public MappingMultiDocsAndPositionsEnum()
        {
        }

        internal MappingMultiDocsAndPositionsEnum Reset(MultiDocsAndPositionsEnum postingsEnum)
        {
            this.numSubs = postingsEnum.NumSubs;
            this.subs = postingsEnum.Subs;
            upto = -1;
            current = null;
            return this;
        }

        /// <summary>
        /// Sets the <seealso cref="MergeState"/>, which is used to re-map
        ///  document IDs.
        /// </summary>
        public MergeState MergeState
        {
            get
            {
                return this.mergeState; // LUCENENET specific - per MSDN properties should always have a getter
            }
            set
            {
                this.mergeState = value;
            }
        }

        /// <summary>
        /// How many sub-readers we are merging. </summary>
        ///  <seealso cref= #getSubs  </seealso>
        public int NumSubs
        {
            get
            {
                return numSubs;
            }
        }

        /// <summary>
        /// Returns sub-readers we are merging. </summary>
        [WritableArray]
        public MultiDocsAndPositionsEnum.EnumWithSlice[] Subs
        {
            get { return subs; }
        }

        public override int Freq
        {
            get { return current.Freq; }
        }

        public override int DocID
        {
            get { return doc; }
        }

        public override int Advance(int target)
        {
            throw new System.NotSupportedException();
        }

        public override int NextDoc()
        {
            while (true)
            {
                if (current == null)
                {
                    if (upto == numSubs - 1)
                    {
                        return this.doc = NO_MORE_DOCS;
                    }
                    else
                    {
                        upto++;
                        int reader = subs[upto].Slice.ReaderIndex;
                        current = subs[upto].DocsAndPositionsEnum;
                        currentBase = mergeState.DocBase[reader];
                        currentMap = mergeState.DocMaps[reader];
                    }
                }

                int doc = current.NextDoc();
                if (doc != NO_MORE_DOCS)
                {
                    // compact deletions
                    doc = currentMap.Get(doc);
                    if (doc == -1)
                    {
                        continue;
                    }
                    return this.doc = currentBase + doc;
                }
                else
                {
                    current = null;
                }
            }
        }

        public override int NextPosition()
        {
            return current.NextPosition();
        }

        public override int StartOffset
        {
            get { return current.StartOffset; }
        }

        public override int EndOffset
        {
            get { return current.EndOffset; }
        }

        public override BytesRef Payload
        {
            get
            {
                return current.Payload;
            }
        }

        public override long GetCost()
        {
            long cost = 0;
            foreach (MultiDocsAndPositionsEnum.EnumWithSlice enumWithSlice in subs)
            {
                cost += enumWithSlice.DocsAndPositionsEnum.GetCost();
            }
            return cost;
        }
    }
}