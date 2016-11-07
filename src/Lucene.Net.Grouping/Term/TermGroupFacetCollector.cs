using Lucene.Net.Index;
using Lucene.Net.Util;
using System.Collections.Generic;
using System.Diagnostics;

namespace Lucene.Net.Search.Grouping.Terms
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

    /// <summary>
    /// An implementation of <see cref="AbstractGroupFacetCollector"/> that computes grouped facets based on the indexed terms
    /// from the <see cref="FieldCache"/>.
    /// 
    /// @lucene.experimental
    /// </summary>
    public abstract class TermGroupFacetCollector : AbstractGroupFacetCollector
    {
        internal readonly List<GroupedFacetHit> groupedFacetHits;
        internal readonly SentinelIntSet segmentGroupedFacetHits;

        internal SortedDocValues groupFieldTermsIndex;

        /// <summary>
        /// Factory method for creating the right implementation based on the fact whether the facet field contains
        /// multiple tokens per documents.
        /// </summary>
        /// <param name="groupField">The group field</param>
        /// <param name="facetField">The facet field</param>
        /// <param name="facetFieldMultivalued">Whether the facet field has multiple tokens per document</param>
        /// <param name="facetPrefix">The facet prefix a facet entry should start with to be included.</param>
        /// <param name="initialSize">
        /// The initial allocation size of the internal int set and group facet list which should roughly
        /// match the total number of expected unique groups. Be aware that the heap usage is
        /// 4 bytes * initialSize.
        /// </param>
        /// <returns><see cref="TermGroupFacetCollector"/> implementation</returns>
        public static TermGroupFacetCollector CreateTermGroupFacetCollector(string groupField,
                                                                            string facetField,
                                                                            bool facetFieldMultivalued,
                                                                            BytesRef facetPrefix,
                                                                            int initialSize)
        {
            if (facetFieldMultivalued)
            {
                return new MV(groupField, facetField, facetPrefix, initialSize);
            }
            else
            {
                return new SV(groupField, facetField, facetPrefix, initialSize);
            }
        }

        internal TermGroupFacetCollector(string groupField, string facetField, BytesRef facetPrefix, int initialSize)
            : base(groupField, facetField, facetPrefix)
        {
            groupedFacetHits = new List<GroupedFacetHit>(initialSize);
            segmentGroupedFacetHits = new SentinelIntSet(initialSize, int.MinValue);
        }

        /// <summary>
        /// Implementation for single valued facet fields.
        /// </summary>
        internal class SV : TermGroupFacetCollector
        {

            private SortedDocValues facetFieldTermsIndex;

            internal SV(string groupField, string facetField, BytesRef facetPrefix, int initialSize)
                        : base(groupField, facetField, facetPrefix, initialSize)
            {
            }

            public override void Collect(int doc)
            {
                int facetOrd = facetFieldTermsIndex.GetOrd(doc);
                if (facetOrd < startFacetOrd || facetOrd >= endFacetOrd)
                {
                    return;
                }

                int groupOrd = groupFieldTermsIndex.GetOrd(doc);
                int segmentGroupedFacetsIndex = groupOrd * (facetFieldTermsIndex.ValueCount + 1) + facetOrd;
                if (segmentGroupedFacetHits.Exists(segmentGroupedFacetsIndex))
                {
                    return;
                }

                segmentTotalCount++;
                segmentFacetCounts[facetOrd + 1]++;

                segmentGroupedFacetHits.Put(segmentGroupedFacetsIndex);

                BytesRef groupKey;
                if (groupOrd == -1)
                {
                    groupKey = null;
                }
                else
                {
                    groupKey = new BytesRef();
                    groupFieldTermsIndex.LookupOrd(groupOrd, groupKey);
                }

                BytesRef facetKey;
                if (facetOrd == -1)
                {
                    facetKey = null;
                }
                else
                {
                    facetKey = new BytesRef();
                    facetFieldTermsIndex.LookupOrd(facetOrd, facetKey);
                }

                groupedFacetHits.Add(new GroupedFacetHit(groupKey, facetKey));
            }

            public override AtomicReaderContext NextReader
            {
                set
                {
                    if (segmentFacetCounts != null)
                    {
                        segmentResults.Add(CreateSegmentResult());
                    }

                    groupFieldTermsIndex = FieldCache.DEFAULT.GetTermsIndex(value.AtomicReader, groupField);
                    facetFieldTermsIndex = FieldCache.DEFAULT.GetTermsIndex(value.AtomicReader, facetField);

                    // 1+ to allow for the -1 "not set":
                    segmentFacetCounts = new int[facetFieldTermsIndex.ValueCount + 1];
                    segmentTotalCount = 0;

                    segmentGroupedFacetHits.Clear();
                    foreach (GroupedFacetHit groupedFacetHit in groupedFacetHits)
                    {
                        int facetOrd = groupedFacetHit.facetValue == null ? -1 : facetFieldTermsIndex.LookupTerm(groupedFacetHit.facetValue);
                        if (groupedFacetHit.facetValue != null && facetOrd < 0)
                        {
                            continue;
                        }

                        int groupOrd = groupedFacetHit.groupValue == null ? -1 : groupFieldTermsIndex.LookupTerm(groupedFacetHit.groupValue);
                        if (groupedFacetHit.groupValue != null && groupOrd < 0)
                        {
                            continue;
                        }

                        int segmentGroupedFacetsIndex = groupOrd * (facetFieldTermsIndex.ValueCount + 1) + facetOrd;
                        segmentGroupedFacetHits.Put(segmentGroupedFacetsIndex);
                    }

                    if (facetPrefix != null)
                    {
                        startFacetOrd = facetFieldTermsIndex.LookupTerm(facetPrefix);
                        if (startFacetOrd < 0)
                        {
                            // Points to the ord one higher than facetPrefix
                            startFacetOrd = -startFacetOrd - 1;
                        }
                        BytesRef facetEndPrefix = BytesRef.DeepCopyOf(facetPrefix);
                        facetEndPrefix.Append(UnicodeUtil.BIG_TERM);
                        endFacetOrd = facetFieldTermsIndex.LookupTerm(facetEndPrefix);
                        Debug.Assert(endFacetOrd < 0);
                        endFacetOrd = -endFacetOrd - 1; // Points to the ord one higher than facetEndPrefix
                    }
                    else
                    {
                        startFacetOrd = -1;
                        endFacetOrd = facetFieldTermsIndex.ValueCount;
                    }
                }
            }


            protected override AbstractSegmentResult CreateSegmentResult()
            {
                return new SegmentResult(segmentFacetCounts, segmentTotalCount, facetFieldTermsIndex.TermsEnum(), startFacetOrd, endFacetOrd);
            }

            internal class SegmentResult : AbstractGroupFacetCollector.AbstractSegmentResult
            {

                internal readonly TermsEnum tenum;

                internal SegmentResult(int[] counts, int total, TermsEnum tenum, int startFacetOrd, int endFacetOrd)
                                : base(counts, total - counts[0], counts[0], endFacetOrd + 1)
                {
                    this.tenum = tenum;
                    this.mergePos = startFacetOrd == -1 ? 1 : startFacetOrd + 1;
                    if (mergePos < maxTermPos)
                    {
                        Debug.Assert(tenum != null);
                        tenum.SeekExact(startFacetOrd == -1 ? 0 : startFacetOrd);
                        mergeTerm = tenum.Term();
                    }
                }

                protected internal override void NextTerm()
                {
                    mergeTerm = tenum.Next();
                }
            }
        }

        /// <summary>
        /// Implementation for multi valued facet fields.
        /// </summary>
        internal class MV : TermGroupFacetCollector
        {

            private SortedSetDocValues facetFieldDocTermOrds;
            private TermsEnum facetOrdTermsEnum;
            private int facetFieldNumTerms;
            private readonly BytesRef scratch = new BytesRef();

            internal MV(string groupField, string facetField, BytesRef facetPrefix, int initialSize)
                         : base(groupField, facetField, facetPrefix, initialSize)
            {
            }

            public override void Collect(int doc)
            {
                int groupOrd = groupFieldTermsIndex.GetOrd(doc);
                if (facetFieldNumTerms == 0)
                {
                    int segmentGroupedFacetsIndex = groupOrd * (facetFieldNumTerms + 1);
                    if (facetPrefix != null || segmentGroupedFacetHits.Exists(segmentGroupedFacetsIndex))
                    {
                        return;
                    }

                    segmentTotalCount++;
                    segmentFacetCounts[facetFieldNumTerms]++;

                    segmentGroupedFacetHits.Put(segmentGroupedFacetsIndex);
                    BytesRef groupKey;
                    if (groupOrd == -1)
                    {
                        groupKey = null;
                    }
                    else
                    {
                        groupKey = new BytesRef();
                        groupFieldTermsIndex.LookupOrd(groupOrd, groupKey);
                    }
                    groupedFacetHits.Add(new GroupedFacetHit(groupKey, null));
                    return;
                }

                facetFieldDocTermOrds.Document = doc;
                long ord;
                bool empty = true;
                while ((ord = facetFieldDocTermOrds.NextOrd()) != SortedSetDocValues.NO_MORE_ORDS)
                {
                    Process(groupOrd, (int)ord);
                    empty = false;
                }

                if (empty)
                {
                    Process(groupOrd, facetFieldNumTerms); // this facet ord is reserved for docs not containing facet field.
                }
            }

            private void Process(int groupOrd, int facetOrd)
            {
                if (facetOrd < startFacetOrd || facetOrd >= endFacetOrd)
                {
                    return;
                }

                int segmentGroupedFacetsIndex = groupOrd * (facetFieldNumTerms + 1) + facetOrd;
                if (segmentGroupedFacetHits.Exists(segmentGroupedFacetsIndex))
                {
                    return;
                }

                segmentTotalCount++;
                segmentFacetCounts[facetOrd]++;

                segmentGroupedFacetHits.Put(segmentGroupedFacetsIndex);

                BytesRef groupKey;
                if (groupOrd == -1)
                {
                    groupKey = null;
                }
                else
                {
                    groupKey = new BytesRef();
                    groupFieldTermsIndex.LookupOrd(groupOrd, groupKey);
                }

                BytesRef facetValue;
                if (facetOrd == facetFieldNumTerms)
                {
                    facetValue = null;
                }
                else
                {
                    facetFieldDocTermOrds.LookupOrd(facetOrd, scratch);
                    facetValue = BytesRef.DeepCopyOf(scratch); // must we?
                }
                groupedFacetHits.Add(new GroupedFacetHit(groupKey, facetValue));
            }

            public override AtomicReaderContext NextReader
            {
                set
                {
                    if (segmentFacetCounts != null)
                    {
                        segmentResults.Add(CreateSegmentResult());
                    }

                    groupFieldTermsIndex = FieldCache.DEFAULT.GetTermsIndex(value.AtomicReader, groupField);
                    facetFieldDocTermOrds = FieldCache.DEFAULT.GetDocTermOrds(value.AtomicReader, facetField);
                    facetFieldNumTerms = (int)facetFieldDocTermOrds.ValueCount;
                    if (facetFieldNumTerms == 0)
                    {
                        facetOrdTermsEnum = null;
                    }
                    else
                    {
                        facetOrdTermsEnum = facetFieldDocTermOrds.TermsEnum();
                    }
                    // [facetFieldNumTerms() + 1] for all possible facet values and docs not containing facet field
                    segmentFacetCounts = new int[facetFieldNumTerms + 1];
                    segmentTotalCount = 0;

                    segmentGroupedFacetHits.Clear();
                    foreach (GroupedFacetHit groupedFacetHit in groupedFacetHits)
                    {
                        int groupOrd = groupedFacetHit.groupValue == null ? -1 : groupFieldTermsIndex.LookupTerm(groupedFacetHit.groupValue);
                        if (groupedFacetHit.groupValue != null && groupOrd < 0)
                        {
                            continue;
                        }

                        int facetOrd;
                        if (groupedFacetHit.facetValue != null)
                        {
                            if (facetOrdTermsEnum == null || !facetOrdTermsEnum.SeekExact(groupedFacetHit.facetValue))
                            {
                                continue;
                            }
                            facetOrd = (int)facetOrdTermsEnum.Ord();
                        }
                        else
                        {
                            facetOrd = facetFieldNumTerms;
                        }

                        // (facetFieldDocTermOrds.numTerms() + 1) for all possible facet values and docs not containing facet field
                        int segmentGroupedFacetsIndex = groupOrd * (facetFieldNumTerms + 1) + facetOrd;
                        segmentGroupedFacetHits.Put(segmentGroupedFacetsIndex);
                    }

                    if (facetPrefix != null)
                    {
                        TermsEnum.SeekStatus seekStatus;
                        if (facetOrdTermsEnum != null)
                        {
                            seekStatus = facetOrdTermsEnum.SeekCeil(facetPrefix);
                        }
                        else
                        {
                            seekStatus = TermsEnum.SeekStatus.END;
                        }

                        if (seekStatus != TermsEnum.SeekStatus.END)
                        {
                            startFacetOrd = (int)facetOrdTermsEnum.Ord();
                        }
                        else
                        {
                            startFacetOrd = 0;
                            endFacetOrd = 0;
                            return;
                        }

                        BytesRef facetEndPrefix = BytesRef.DeepCopyOf(facetPrefix);
                        facetEndPrefix.Append(UnicodeUtil.BIG_TERM);
                        seekStatus = facetOrdTermsEnum.SeekCeil(facetEndPrefix);
                        if (seekStatus != TermsEnum.SeekStatus.END)
                        {
                            endFacetOrd = (int)facetOrdTermsEnum.Ord();
                        }
                        else
                        {
                            endFacetOrd = facetFieldNumTerms; // Don't include null...
                        }
                    }
                    else
                    {
                        startFacetOrd = 0;
                        endFacetOrd = facetFieldNumTerms + 1;
                    }
                }
            }

            protected override AbstractSegmentResult CreateSegmentResult()
            {
                return new SegmentResult(segmentFacetCounts, segmentTotalCount, facetFieldNumTerms, facetOrdTermsEnum, startFacetOrd, endFacetOrd);
            }

            internal class SegmentResult : AbstractGroupFacetCollector.AbstractSegmentResult
            {

                internal readonly TermsEnum tenum;

                internal SegmentResult(int[] counts, int total, int missingCountIndex, TermsEnum tenum, int startFacetOrd, int endFacetOrd)
                                : base(counts, total - counts[missingCountIndex], counts[missingCountIndex],
                        endFacetOrd == missingCountIndex + 1 ? missingCountIndex : endFacetOrd)
                {
                    this.tenum = tenum;
                    this.mergePos = startFacetOrd;
                    if (tenum != null)
                    {
                        tenum.SeekExact(mergePos);
                        mergeTerm = tenum.Term();
                    }
                }

                protected internal override void NextTerm()
                {
                    mergeTerm = tenum.Next();
                }
            }
        }
    }


    internal class GroupedFacetHit
    {
        internal readonly BytesRef groupValue;
        internal readonly BytesRef facetValue;

        internal GroupedFacetHit(BytesRef groupValue, BytesRef facetValue)
        {
            this.groupValue = groupValue;
            this.facetValue = facetValue;
        }
    }
}
