using Lucene.Net.Diagnostics;
using Lucene.Net.Index;
using Lucene.Net.Util;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

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
        internal readonly IList<GroupedFacetHit> groupedFacetHits;
        internal readonly SentinelInt32Set segmentGroupedFacetHits;

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

        private protected TermGroupFacetCollector(string groupField, string facetField, BytesRef facetPrefix, int initialSize) // LUCENENET: Changed from internal to private protected
            : base(groupField, facetField, facetPrefix)
        {
            groupedFacetHits = new JCG.List<GroupedFacetHit>(initialSize);
            segmentGroupedFacetHits = new SentinelInt32Set(initialSize, int.MinValue);
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
                if (facetOrd < m_startFacetOrd || facetOrd >= m_endFacetOrd)
                {
                    return;
                }

                int groupOrd = groupFieldTermsIndex.GetOrd(doc);
                int segmentGroupedFacetsIndex = groupOrd * (facetFieldTermsIndex.ValueCount + 1) + facetOrd;
                if (segmentGroupedFacetHits.Exists(segmentGroupedFacetsIndex))
                {
                    return;
                }

                m_segmentTotalCount++;
                m_segmentFacetCounts[facetOrd + 1]++;

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

            public override void SetNextReader(AtomicReaderContext context)
            {
                if (m_segmentFacetCounts != null)
                {
                    m_segmentResults.Add(CreateSegmentResult());
                }

                groupFieldTermsIndex = FieldCache.DEFAULT.GetTermsIndex(context.AtomicReader, m_groupField);
                facetFieldTermsIndex = FieldCache.DEFAULT.GetTermsIndex(context.AtomicReader, m_facetField);

                // 1+ to allow for the -1 "not set":
                m_segmentFacetCounts = new int[facetFieldTermsIndex.ValueCount + 1];
                m_segmentTotalCount = 0;

                segmentGroupedFacetHits.Clear();
                foreach (GroupedFacetHit groupedFacetHit in groupedFacetHits)
                {
                    int facetOrd = groupedFacetHit.facetValue is null ? -1 : facetFieldTermsIndex.LookupTerm(groupedFacetHit.facetValue);
                    if (groupedFacetHit.facetValue != null && facetOrd < 0)
                    {
                        continue;
                    }

                    int groupOrd = groupedFacetHit.groupValue is null ? -1 : groupFieldTermsIndex.LookupTerm(groupedFacetHit.groupValue);
                    if (groupedFacetHit.groupValue != null && groupOrd < 0)
                    {
                        continue;
                    }

                    int segmentGroupedFacetsIndex = groupOrd * (facetFieldTermsIndex.ValueCount + 1) + facetOrd;
                    segmentGroupedFacetHits.Put(segmentGroupedFacetsIndex);
                }

                if (m_facetPrefix != null)
                {
                    m_startFacetOrd = facetFieldTermsIndex.LookupTerm(m_facetPrefix);
                    if (m_startFacetOrd < 0)
                    {
                        // Points to the ord one higher than facetPrefix
                        m_startFacetOrd = -m_startFacetOrd - 1;
                    }
                    BytesRef facetEndPrefix = BytesRef.DeepCopyOf(m_facetPrefix);
                    facetEndPrefix.Append(UnicodeUtil.BIG_TERM);
                    m_endFacetOrd = facetFieldTermsIndex.LookupTerm(facetEndPrefix);
                    if (Debugging.AssertsEnabled) Debugging.Assert(m_endFacetOrd < 0);
                    m_endFacetOrd = -m_endFacetOrd - 1; // Points to the ord one higher than facetEndPrefix
                }
                else
                {
                    m_startFacetOrd = -1;
                    m_endFacetOrd = facetFieldTermsIndex.ValueCount;
                }
            }


            protected override AbstractSegmentResult CreateSegmentResult()
            {
                return new SegmentResult(m_segmentFacetCounts, m_segmentTotalCount, facetFieldTermsIndex.GetTermsEnum(), m_startFacetOrd, m_endFacetOrd);
            }

            internal class SegmentResult : AbstractGroupFacetCollector.AbstractSegmentResult
            {

                internal readonly TermsEnum tenum;

                internal SegmentResult(int[] counts, int total, TermsEnum tenum, int startFacetOrd, int endFacetOrd)
                                : base(counts, total - counts[0], counts[0], endFacetOrd + 1)
                {
                    this.tenum = tenum;
                    this.m_mergePos = startFacetOrd == -1 ? 1 : startFacetOrd + 1;
                    if (m_mergePos < m_maxTermPos)
                    {
                        if (Debugging.AssertsEnabled) Debugging.Assert(tenum != null);
                        tenum.SeekExact(startFacetOrd == -1 ? 0 : startFacetOrd);
                        m_mergeTerm = tenum.Term;
                    }
                }

                protected internal override void NextTerm()
                {
                    m_mergeTerm = tenum.MoveNext() ? tenum.Term : null;
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
                    if (m_facetPrefix != null || segmentGroupedFacetHits.Exists(segmentGroupedFacetsIndex))
                    {
                        return;
                    }

                    m_segmentTotalCount++;
                    m_segmentFacetCounts[facetFieldNumTerms]++;

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

                facetFieldDocTermOrds.SetDocument(doc);
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
                if (facetOrd < m_startFacetOrd || facetOrd >= m_endFacetOrd)
                {
                    return;
                }

                int segmentGroupedFacetsIndex = groupOrd * (facetFieldNumTerms + 1) + facetOrd;
                if (segmentGroupedFacetHits.Exists(segmentGroupedFacetsIndex))
                {
                    return;
                }

                m_segmentTotalCount++;
                m_segmentFacetCounts[facetOrd]++;

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

            public override void SetNextReader(AtomicReaderContext context)
            {
                if (m_segmentFacetCounts != null)
                {
                    m_segmentResults.Add(CreateSegmentResult());
                }

                groupFieldTermsIndex = FieldCache.DEFAULT.GetTermsIndex(context.AtomicReader, m_groupField);
                facetFieldDocTermOrds = FieldCache.DEFAULT.GetDocTermOrds(context.AtomicReader, m_facetField);
                facetFieldNumTerms = (int)facetFieldDocTermOrds.ValueCount;
                if (facetFieldNumTerms == 0)
                {
                    facetOrdTermsEnum = null;
                }
                else
                {
                    facetOrdTermsEnum = facetFieldDocTermOrds.GetTermsEnum();
                }
                // [facetFieldNumTerms() + 1] for all possible facet values and docs not containing facet field
                m_segmentFacetCounts = new int[facetFieldNumTerms + 1];
                m_segmentTotalCount = 0;

                segmentGroupedFacetHits.Clear();
                foreach (GroupedFacetHit groupedFacetHit in groupedFacetHits)
                {
                    int groupOrd = groupedFacetHit.groupValue is null ? -1 : groupFieldTermsIndex.LookupTerm(groupedFacetHit.groupValue);
                    if (groupedFacetHit.groupValue != null && groupOrd < 0)
                    {
                        continue;
                    }

                    int facetOrd;
                    if (groupedFacetHit.facetValue != null)
                    {
                        if (facetOrdTermsEnum is null || !facetOrdTermsEnum.SeekExact(groupedFacetHit.facetValue))
                        {
                            continue;
                        }
                        facetOrd = (int)facetOrdTermsEnum.Ord;
                    }
                    else
                    {
                        facetOrd = facetFieldNumTerms;
                    }

                    // (facetFieldDocTermOrds.numTerms() + 1) for all possible facet values and docs not containing facet field
                    int segmentGroupedFacetsIndex = groupOrd * (facetFieldNumTerms + 1) + facetOrd;
                    segmentGroupedFacetHits.Put(segmentGroupedFacetsIndex);
                }

                if (m_facetPrefix != null)
                {
                    TermsEnum.SeekStatus seekStatus;
                    if (facetOrdTermsEnum != null)
                    {
                        seekStatus = facetOrdTermsEnum.SeekCeil(m_facetPrefix);
                    }
                    else
                    {
                        seekStatus = TermsEnum.SeekStatus.END;
                    }

                    if (seekStatus != TermsEnum.SeekStatus.END)
                    {
                        m_startFacetOrd = (int)facetOrdTermsEnum.Ord;
                    }
                    else
                    {
                        m_startFacetOrd = 0;
                        m_endFacetOrd = 0;
                        return;
                    }

                    BytesRef facetEndPrefix = BytesRef.DeepCopyOf(m_facetPrefix);
                    facetEndPrefix.Append(UnicodeUtil.BIG_TERM);
                    seekStatus = facetOrdTermsEnum.SeekCeil(facetEndPrefix);
                    if (seekStatus != TermsEnum.SeekStatus.END)
                    {
                        m_endFacetOrd = (int)facetOrdTermsEnum.Ord;
                    }
                    else
                    {
                        m_endFacetOrd = facetFieldNumTerms; // Don't include null...
                    }
                }
                else
                {
                    m_startFacetOrd = 0;
                    m_endFacetOrd = facetFieldNumTerms + 1;
                }
            }

            protected override AbstractSegmentResult CreateSegmentResult()
            {
                return new SegmentResult(m_segmentFacetCounts, m_segmentTotalCount, facetFieldNumTerms, facetOrdTermsEnum, m_startFacetOrd, m_endFacetOrd);
            }

            internal class SegmentResult : AbstractGroupFacetCollector.AbstractSegmentResult
            {

                internal readonly TermsEnum tenum;

                internal SegmentResult(int[] counts, int total, int missingCountIndex, TermsEnum tenum, int startFacetOrd, int endFacetOrd)
                                : base(counts, total - counts[missingCountIndex], counts[missingCountIndex],
                        endFacetOrd == missingCountIndex + 1 ? missingCountIndex : endFacetOrd)
                {
                    this.tenum = tenum;
                    this.m_mergePos = startFacetOrd;
                    if (tenum != null)
                    {
                        tenum.SeekExact(m_mergePos);
                        m_mergeTerm = tenum.Term;
                    }
                }

                protected internal override void NextTerm()
                {
                    m_mergeTerm = tenum.MoveNext() ? tenum.Term : null;
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
