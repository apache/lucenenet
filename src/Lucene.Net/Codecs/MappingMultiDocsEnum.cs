using Lucene.Net.Diagnostics;
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

    using DocsEnum = Lucene.Net.Index.DocsEnum;
    using MergeState = Lucene.Net.Index.MergeState;
    using MultiDocsEnum = Lucene.Net.Index.MultiDocsEnum;

    /// <summary>
    /// Exposes flex API, merged from flex API of sub-segments,
    /// remapping docIDs (this is used for segment merging).
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public sealed class MappingMultiDocsEnum : DocsEnum
    {
        private MultiDocsEnum.EnumWithSlice[] subs;
        internal int numSubs;
        internal int upto;
        internal MergeState.DocMap currentMap;
        internal DocsEnum current;
        internal int currentBase;
        internal int doc = -1;
        private MergeState mergeState;

        /// <summary>
        /// Sole constructor. </summary>
        public MappingMultiDocsEnum()
        {
        }

        internal MappingMultiDocsEnum Reset(MultiDocsEnum docsEnum)
        {
            this.numSubs = docsEnum.NumSubs;
            this.subs = docsEnum.Subs;
            upto = -1;
            current = null;
            return this;
        }

        /// <summary>
        /// Sets the <see cref="MergeState"/>, which is used to re-map
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
        public MultiDocsEnum.EnumWithSlice[] Subs => subs;

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
                        current = subs[upto].DocsEnum;
                        currentBase = mergeState.DocBase[reader];
                        currentMap = mergeState.DocMaps[reader];
                        if (Debugging.AssertsEnabled) Debugging.Assert(currentMap.MaxDoc == subs[upto].Slice.Length, "readerIndex={0} subs.len={1} len1={2} vs {3}", reader, subs.Length, currentMap.MaxDoc, subs[upto].Slice.Length);
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
        public override long GetCost()
        {
            long cost = 0;
            foreach (MultiDocsEnum.EnumWithSlice enumWithSlice in subs)
            {
                cost += enumWithSlice.DocsEnum.GetCost();
            }
            return cost;
        }
    }
}