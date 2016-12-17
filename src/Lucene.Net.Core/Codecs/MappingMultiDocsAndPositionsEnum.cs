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
        private MultiDocsAndPositionsEnum.EnumWithSlice[] Subs_Renamed;
        internal int NumSubs_Renamed;
        internal int Upto;
        internal MergeState.DocMap CurrentMap;
        internal DocsAndPositionsEnum Current;
        internal int CurrentBase;
        internal int Doc = -1;
        private MergeState MergeState_Renamed;

        /// <summary>
        /// Sole constructor. </summary>
        public MappingMultiDocsAndPositionsEnum()
        {
        }

        internal MappingMultiDocsAndPositionsEnum Reset(MultiDocsAndPositionsEnum postingsEnum)
        {
            this.NumSubs_Renamed = postingsEnum.NumSubs;
            this.Subs_Renamed = postingsEnum.Subs;
            Upto = -1;
            Current = null;
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
                return this.MergeState_Renamed; // LUCENENET specific - per MSDN properties should always have a getter
            }
            set
            {
                this.MergeState_Renamed = value;
            }
        }

        /// <summary>
        /// How many sub-readers we are merging. </summary>
        ///  <seealso cref= #getSubs  </seealso>
        public int NumSubs
        {
            get
            {
                return NumSubs_Renamed;
            }
        }

        /// <summary>
        /// Returns sub-readers we are merging. </summary>
        public MultiDocsAndPositionsEnum.EnumWithSlice[] Subs // LUCENENET TODO: Change to GetSubs()
        {
            get
            {
                return Subs_Renamed;
            }
        }

        public override int Freq()
        {
            return Current.Freq();
        }

        public override int DocID()
        {
            return Doc;
        }

        public override int Advance(int target)
        {
            throw new System.NotSupportedException();
        }

        public override int NextDoc()
        {
            while (true)
            {
                if (Current == null)
                {
                    if (Upto == NumSubs_Renamed - 1)
                    {
                        return this.Doc = NO_MORE_DOCS;
                    }
                    else
                    {
                        Upto++;
                        int reader = Subs_Renamed[Upto].Slice.ReaderIndex;
                        Current = Subs_Renamed[Upto].DocsAndPositionsEnum;
                        CurrentBase = MergeState_Renamed.DocBase[reader];
                        CurrentMap = MergeState_Renamed.DocMaps[reader];
                    }
                }

                int doc = Current.NextDoc();
                if (doc != NO_MORE_DOCS)
                {
                    // compact deletions
                    doc = CurrentMap.Get(doc);
                    if (doc == -1)
                    {
                        continue;
                    }
                    return this.Doc = CurrentBase + doc;
                }
                else
                {
                    Current = null;
                }
            }
        }

        public override int NextPosition()
        {
            return Current.NextPosition();
        }

        public override int StartOffset()
        {
            return Current.StartOffset();
        }

        public override int EndOffset()
        {
            return Current.EndOffset();
        }

        public override BytesRef Payload
        {
            get
            {
                return Current.Payload;
            }
        }

        public override long Cost()
        {
            long cost = 0;
            foreach (MultiDocsAndPositionsEnum.EnumWithSlice enumWithSlice in Subs_Renamed)
            {
                cost += enumWithSlice.DocsAndPositionsEnum.Cost();
            }
            return cost;
        }
    }
}