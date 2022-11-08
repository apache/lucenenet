using Lucene.Net.Support;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

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
    /// <para/>
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal MappingMultiDocsAndPositionsEnum Reset(MultiDocsAndPositionsEnum postingsEnum)
        {
            this.numSubs = postingsEnum.NumSubs;
            this.subs = postingsEnum.Subs;
            upto = -1;
            current = null;
            return this;
        }

        /// <summary>
        /// Gets or Sets the <see cref="Index.MergeState"/>, which is used to re-map
        /// document IDs.
        /// </summary>
        public MergeState MergeState
        {
            get => this.mergeState; // LUCENENET specific - per MSDN properties should always have a getter
            set => this.mergeState = value;
        }

        /// <summary>
        /// How many sub-readers we are merging. </summary>
        /// <seealso cref="Subs"/>
        public int NumSubs => numSubs;

        /// <summary>
        /// Returns sub-readers we are merging. </summary>
        [WritableArray]
        [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
        public MultiDocsAndPositionsEnum.EnumWithSlice[] Subs => subs;

        public override int Freq => current.Freq;

        public override int DocID => doc;

        public override int Advance(int target)
        {
            throw UnsupportedOperationException.Create();
        }

        public override int NextDoc()
        {
            while (true)
            {
                if (current is null)
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int NextPosition()
        {
            return current.NextPosition();
        }

        public override int StartOffset => current.StartOffset;

        public override int EndOffset => current.EndOffset;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override BytesRef GetPayload()
        {
            return current.GetPayload();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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