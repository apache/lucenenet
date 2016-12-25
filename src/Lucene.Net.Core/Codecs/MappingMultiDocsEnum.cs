using System.Diagnostics;

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

    using DocsEnum = Lucene.Net.Index.DocsEnum;
    using MergeState = Lucene.Net.Index.MergeState;
    using MultiDocsEnum = Lucene.Net.Index.MultiDocsEnum;

    /// <summary>
    /// Exposes flex API, merged from flex API of sub-segments,
    /// remapping docIDs (this is used for segment merging).
    ///
    /// @lucene.experimental
    /// </summary>
    public sealed class MappingMultiDocsEnum : DocsEnum
    {
        private MultiDocsEnum.EnumWithSlice[] Subs_Renamed;
        internal int NumSubs_Renamed;
        internal int Upto;
        internal MergeState.DocMap CurrentMap;
        internal DocsEnum Current;
        internal int CurrentBase;
        internal int Doc = -1;
        private MergeState MergeState_Renamed;

        /// <summary>
        /// Sole constructor. </summary>
        public MappingMultiDocsEnum()
        {
        }

        internal MappingMultiDocsEnum Reset(MultiDocsEnum docsEnum)
        {
            this.NumSubs_Renamed = docsEnum.NumSubs;
            this.Subs_Renamed = docsEnum.GetSubs();
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
        public MultiDocsEnum.EnumWithSlice[] GetSubs()
        {
            return Subs_Renamed;
        }

        public override int Freq
        {
            get { return Current.Freq; }
        }

        public override int DocID
        {
            get { return Doc; }
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
                        Current = Subs_Renamed[Upto].DocsEnum;
                        CurrentBase = MergeState_Renamed.DocBase[reader];
                        CurrentMap = MergeState_Renamed.DocMaps[reader];
                        Debug.Assert(CurrentMap.MaxDoc == Subs_Renamed[Upto].Slice.Length, "readerIndex=" + reader + " subs.len=" + Subs_Renamed.Length + " len1=" + CurrentMap.MaxDoc + " vs " + Subs_Renamed[Upto].Slice.Length);
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

        public override long Cost()
        {
            long cost = 0;
            foreach (MultiDocsEnum.EnumWithSlice enumWithSlice in Subs_Renamed)
            {
                cost += enumWithSlice.DocsEnum.Cost();
            }
            return cost;
        }
    }
}