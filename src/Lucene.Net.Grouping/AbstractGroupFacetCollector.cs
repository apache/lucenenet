using Lucene.Net.Index;
using Lucene.Net.Util;
using System.Collections.Generic;
using System.IO;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Search.Grouping
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
    /// Base class for computing grouped facets.
    /// @lucene.experimental
    /// </summary>
    public abstract class AbstractGroupFacetCollector : ICollector
    {
        protected readonly string m_groupField;
        protected readonly string m_facetField;
        protected readonly BytesRef m_facetPrefix;
        protected readonly IList<AbstractSegmentResult> m_segmentResults;

        protected int[] m_segmentFacetCounts;
        protected int m_segmentTotalCount;
        protected int m_startFacetOrd;
        protected int m_endFacetOrd;

        protected AbstractGroupFacetCollector(string groupField, string facetField, BytesRef facetPrefix)
        {
            this.m_groupField = groupField;
            this.m_facetField = facetField;
            this.m_facetPrefix = facetPrefix;
            m_segmentResults = new JCG.List<AbstractSegmentResult>();
        }

        /// <summary>
        /// Returns grouped facet results that were computed over zero or more segments.
        /// Grouped facet counts are merged from zero or more segment results.
        /// </summary>
        /// <param name="size">The total number of facets to include. This is typically offset + limit</param>
        /// <param name="minCount">The minimum count a facet entry should have to be included in the grouped facet result</param>
        /// <param name="orderByCount">
        /// Whether to sort the facet entries by facet entry count. If <c>false</c> then the facets
        /// are sorted lexicographically in ascending order.
        /// </param>
        /// <returns>grouped facet results</returns>
        /// <exception cref="IOException">If I/O related errors occur during merging segment grouped facet counts.</exception>
        public virtual GroupedFacetResult MergeSegmentResults(int size, int minCount, bool orderByCount)
        {
            if (m_segmentFacetCounts != null)
            {
                m_segmentResults.Add(CreateSegmentResult());
                m_segmentFacetCounts = null; // reset
            }

            int totalCount = 0;
            int missingCount = 0;
            SegmentResultPriorityQueue segments = new SegmentResultPriorityQueue(m_segmentResults.Count);
            foreach (AbstractSegmentResult segmentResult in m_segmentResults)
            {
                missingCount += segmentResult.m_missing;
                if (segmentResult.m_mergePos >= segmentResult.m_maxTermPos)
                {
                    continue;
                }
                totalCount += segmentResult.m_total;
                segments.Add(segmentResult);
            }

            GroupedFacetResult facetResult = new GroupedFacetResult(size, minCount, orderByCount, totalCount, missingCount);
            while (segments.Count > 0)
            {
                AbstractSegmentResult segmentResult = segments.Top;
                BytesRef currentFacetValue = BytesRef.DeepCopyOf(segmentResult.m_mergeTerm);
                int count = 0;

                do
                {
                    count += segmentResult.m_counts[segmentResult.m_mergePos++];
                    if (segmentResult.m_mergePos < segmentResult.m_maxTermPos)
                    {
                        segmentResult.NextTerm();
                        segmentResult = segments.UpdateTop();
                    }
                    else
                    {
                        segments.Pop();
                        segmentResult = segments.Top;
                        if (segmentResult is null)
                        {
                            break;
                        }
                    }
                } while (currentFacetValue.Equals(segmentResult.m_mergeTerm));
                facetResult.AddFacetCount(currentFacetValue, count);
            }
            return facetResult;
        }

        protected abstract AbstractSegmentResult CreateSegmentResult();

        public virtual void SetScorer(Scorer scorer)
        {
        }

        // LUCENENET specific - need an abstract definition of this method, since our interface does not provide an implementation.
        public abstract void Collect(int doc);

        // LUCENENET specific - need an abstract definition of this method, since our interface does not provide an implementation.
        public abstract void SetNextReader(AtomicReaderContext context);

        public virtual bool AcceptsDocsOutOfOrder => true;

        private class OrderByCountAndValueComparer : IComparer<FacetEntry>
        {
            public int Compare(FacetEntry a, FacetEntry b)
            {
                int cmp = b.Count - a.Count; // Highest count first!
                if (cmp != 0)
                {
                    return cmp;
                }
                return a.Value.CompareTo(b.Value);
            }
        }

        private class OrderByValueComparer : IComparer<FacetEntry>
        {
            public int Compare(FacetEntry a, FacetEntry b)
            {
                return a.Value.CompareTo(b.Value);
            }
        }

        /// <summary>
        /// The grouped facet result. Containing grouped facet entries, total count and total missing count.
        /// </summary>
        public class GroupedFacetResult
        {
            private readonly static IComparer<FacetEntry> orderByCountAndValue = new OrderByCountAndValueComparer();
            private readonly static IComparer<FacetEntry> orderByValue = new OrderByValueComparer();

            private readonly int maxSize;
            private readonly JCG.SortedSet<FacetEntry> facetEntries;
            private readonly int totalMissingCount;
            private readonly int totalCount;

            private int currentMin;

            public GroupedFacetResult(int size, int minCount, bool orderByCount, int totalCount, int totalMissingCount)
            {
                this.facetEntries = new JCG.SortedSet<FacetEntry>(orderByCount ? orderByCountAndValue : orderByValue);
                this.totalMissingCount = totalMissingCount;
                this.totalCount = totalCount;
                maxSize = size;
                currentMin = minCount;
            }

            public virtual void AddFacetCount(BytesRef facetValue, int count)
            {
                if (count < currentMin)
                {
                    return;
                }

                FacetEntry facetEntry = new FacetEntry(facetValue, count);
                if (facetEntries.Count == maxSize)
                {
                    if (!facetEntries.TryGetSuccessor(facetEntry, out FacetEntry _))
                    {
                        return;
                    }
                    var max = facetEntries.Max;
                    if (max != null)
                        facetEntries.Remove(max);
                }
                facetEntries.Add(facetEntry);

                if (facetEntries.Count == maxSize)
                {
                    var max = facetEntries.Max;
                    currentMin =  max != null ? max.Count : 0;
                }
            }

            /// <summary>
            /// Returns a list of facet entries to be rendered based on the specified offset and limit.
            /// The facet entries are retrieved from the facet entries collected during merging.
            /// </summary>
            /// <param name="offset">The offset in the collected facet entries during merging</param>
            /// <param name="limit">The number of facets to return starting from the offset.</param>
            /// <returns>a list of facet entries to be rendered based on the specified offset and limit</returns>
            public virtual IList<FacetEntry> GetFacetEntries(int offset, int limit)
            {
                IList<FacetEntry> entries = new JCG.List<FacetEntry>();

                int skipped = 0;
                int included = 0;
                foreach (FacetEntry facetEntry in facetEntries)
                {
                    if (skipped < offset)
                    {
                        skipped++;
                        continue;
                    }
                    if (included++ >= limit)
                    {
                        break;
                    }
                    entries.Add(facetEntry);
                }
                return entries;
            }

            /// <summary>
            /// Gets the sum of all facet entries counts.
            /// </summary>
            public virtual int TotalCount => totalCount;

            /// <summary>
            /// Gets the number of groups that didn't have a facet value.
            /// </summary>
            public virtual int TotalMissingCount => totalMissingCount;
        }

        /// <summary>
        /// Represents a facet entry with a value and a count.
        /// </summary>
        public class FacetEntry
        {

            private readonly BytesRef value;
            private readonly int count;

            public FacetEntry(BytesRef value, int count)
            {
                this.value = value;
                this.count = count;
            }

            public override bool Equals(object o)
            {
                if (this == o) return true;
                if (o is null || GetType() != o.GetType()) return false;

                FacetEntry that = (FacetEntry)o;

                if (count != that.count) return false;
                if (!value.Equals(that.value)) return false;

                return true;
            }

            public override int GetHashCode()
            {
                int result = value.GetHashCode();
                result = 31 * result + count;
                return result;
            }

            public override string ToString()
            {
                return "FacetEntry{" +
                    "value=" + value.Utf8ToString() +
                    ", count=" + count +
                    '}';
            }

            /// <summary>
            /// Gets the value of this facet entry
            /// </summary>
            public virtual BytesRef Value => value;

            /// <summary>
            /// Gets the count (number of groups) of this facet entry.
            /// </summary>
            public virtual int Count => count;
        }

        /// <summary>
        /// Contains the local grouped segment counts for a particular segment.
        /// Each <see cref="AbstractSegmentResult"/> must be added together.
        /// </summary>
        /// <remarks>
        /// LUCENENET NOTE: Renamed from SegmentResult to AbstractSegmentResult
        /// to avoid naming conflicts with subclasses.
        /// </remarks>
        protected internal abstract class AbstractSegmentResult
        {
            protected internal readonly int[] m_counts;
            protected internal readonly int m_total;
            protected internal readonly int m_missing;
            protected internal readonly int m_maxTermPos;

            protected internal BytesRef m_mergeTerm;
            protected internal int m_mergePos;

            protected AbstractSegmentResult(int[] counts, int total, int missing, int maxTermPos)
            {
                this.m_counts = counts;
                this.m_total = total;
                this.m_missing = missing;
                this.m_maxTermPos = maxTermPos;
            }

            /// <summary>
            /// Go to next term in this <see cref="AbstractSegmentResult"/> in order to retrieve the grouped facet counts.
            /// </summary>
            /// <exception cref="IOException">If I/O related errors occur</exception>
            protected internal abstract void NextTerm();

        }

        private class SegmentResultPriorityQueue : Util.PriorityQueue<AbstractSegmentResult>
        {
            internal SegmentResultPriorityQueue(int maxSize)
                : base(maxSize)
            {
            }

            protected internal override bool LessThan(AbstractSegmentResult a, AbstractSegmentResult b)
            {
                return a.m_mergeTerm.CompareTo(b.m_mergeTerm) < 0;
            }
        }
    }
}
