using Lucene.Net.Support;
using Lucene.Net.Util;
using System.Collections.Generic;

namespace Lucene.Net.Search.Grouping
{
    /// <summary>
    /// Base class for computing grouped facets.
    /// @lucene.experimental
    /// </summary>
    public abstract class AbstractGroupFacetCollector : Collector
    {
        protected readonly string groupField;
        protected readonly string facetField;
        protected readonly BytesRef facetPrefix;
        protected readonly IList<AbstractSegmentResult> segmentResults;

        protected int[] segmentFacetCounts;
        protected int segmentTotalCount;
        protected int startFacetOrd;
        protected int endFacetOrd;

        protected AbstractGroupFacetCollector(string groupField, string facetField, BytesRef facetPrefix)
        {
            this.groupField = groupField;
            this.facetField = facetField;
            this.facetPrefix = facetPrefix;
            segmentResults = new List<AbstractSegmentResult>();
        }

        /**
         * Returns grouped facet results that were computed over zero or more segments.
         * Grouped facet counts are merged from zero or more segment results.
         *
         * @param size The total number of facets to include. This is typically offset + limit
         * @param minCount The minimum count a facet entry should have to be included in the grouped facet result
         * @param orderByCount Whether to sort the facet entries by facet entry count. If <code>false</code> then the facets
         *                     are sorted lexicographically in ascending order.
         * @return grouped facet results
         * @throws IOException If I/O related errors occur during merging segment grouped facet counts.
         */
        public GroupedFacetResult MergeSegmentResults(int size, int minCount, bool orderByCount)
        {
            if (segmentFacetCounts != null)
            {
                segmentResults.Add(CreateSegmentResult());
                segmentFacetCounts = null; // reset
            }

            int totalCount = 0;
            int missingCount = 0;
            SegmentResultPriorityQueue segments = new SegmentResultPriorityQueue(segmentResults.Count);
            foreach (AbstractSegmentResult segmentResult in segmentResults)
            {
                missingCount += segmentResult.missing;
                if (segmentResult.mergePos >= segmentResult.maxTermPos)
                {
                    continue;
                }
                totalCount += segmentResult.total;
                segments.Add(segmentResult);
            }

            GroupedFacetResult facetResult = new GroupedFacetResult(size, minCount, orderByCount, totalCount, missingCount);
            while (segments.Size() > 0)
            {
                AbstractSegmentResult segmentResult = segments.Top();
                BytesRef currentFacetValue = BytesRef.DeepCopyOf(segmentResult.mergeTerm);
                int count = 0;

                do
                {
                    count += segmentResult.counts[segmentResult.mergePos++];
                    if (segmentResult.mergePos < segmentResult.maxTermPos)
                    {
                        segmentResult.NextTerm();
                        segmentResult = segments.UpdateTop();
                    }
                    else
                    {
                        segments.Pop();
                        segmentResult = segments.Top();
                        if (segmentResult == null)
                        {
                            break;
                        }
                    }
                } while (currentFacetValue.Equals(segmentResult.mergeTerm));
                facetResult.AddFacetCount(currentFacetValue, count);
            }
            return facetResult;
        }

        protected abstract AbstractSegmentResult CreateSegmentResult();

        public override Scorer Scorer
        {
            set
            {
            }
        }

        public override bool AcceptsDocsOutOfOrder()
        {
            return true;
        }

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

        /**
         * The grouped facet result. Containing grouped facet entries, total count and total missing count.
         */
        public class GroupedFacetResult
        {
            private readonly static IComparer<FacetEntry> orderByCountAndValue = new OrderByCountAndValueComparer();
            private readonly static IComparer<FacetEntry> orderByValue = new OrderByValueComparer();

            private readonly int maxSize;
            private readonly TreeSet<FacetEntry> facetEntries;
            private readonly int totalMissingCount;
            private readonly int totalCount;

            private int currentMin;

            public GroupedFacetResult(int size, int minCount, bool orderByCount, int totalCount, int totalMissingCount)
            {
                this.facetEntries = new TreeSet<FacetEntry>(orderByCount ? orderByCountAndValue : orderByValue);
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
                    FacetEntry temp;
                    if (!facetEntries.TrySuccessor(facetEntry, out temp))
                    {
                        return;
                    }
                    facetEntries.DeleteMax();
                }
                facetEntries.Add(facetEntry);

                if (facetEntries.Count == maxSize)
                {
                    currentMin = facetEntries.FindMax().Count;
                }
            }

            /**
             * Returns a list of facet entries to be rendered based on the specified offset and limit.
             * The facet entries are retrieved from the facet entries collected during merging.
             *
             * @param offset The offset in the collected facet entries during merging
             * @param limit The number of facets to return starting from the offset.
             * @return a list of facet entries to be rendered based on the specified offset and limit
             */
            public List<FacetEntry> GetFacetEntries(int offset, int limit)
            {
                List<FacetEntry> entries = new List<FacetEntry>();

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

            /**
             * Returns the sum of all facet entries counts.
             *
             * @return the sum of all facet entries counts
             */
            public int TotalCount
            {
                get
                {
                    return totalCount;
                }
            }

            /**
             * Returns the number of groups that didn't have a facet value.
             *
             * @return the number of groups that didn't have a facet value
             */
            public int TotalMissingCount
            {
                get
                {
                    return totalMissingCount;
                }
            }
        }

        /**
         * Represents a facet entry with a value and a count.
         */
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
                if (o == null || GetType() != o.GetType()) return false;

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

            /**
             * @return The value of this facet entry
             */
            public BytesRef Value
            {
                get
                {
                    return value;
                }
            }

            /**
             * @return The count (number of groups) of this facet entry.
             */
            public int Count
            {
                get
                {
                    return count;
                }
            }
        }

        /**
         * Contains the local grouped segment counts for a particular segment.
         * Each <code>SegmentResult</code> must be added together.
         */
        protected internal abstract class AbstractSegmentResult
        {
            protected internal readonly int[] counts;
            protected internal readonly int total;
            protected internal readonly int missing;
            protected internal readonly int maxTermPos;

            protected internal BytesRef mergeTerm;
            protected internal int mergePos;

            protected AbstractSegmentResult(int[] counts, int total, int missing, int maxTermPos)
            {
                this.counts = counts;
                this.total = total;
                this.missing = missing;
                this.maxTermPos = maxTermPos;
            }

            /**
             * Go to next term in this <code>SegmentResult</code> in order to retrieve the grouped facet counts.
             *
             * @throws IOException If I/O related errors occur
             */
            protected internal abstract void NextTerm();

        }

        private class SegmentResultPriorityQueue : Util.PriorityQueue<AbstractSegmentResult>
        {
            internal SegmentResultPriorityQueue(int maxSize)
                : base(maxSize)
            {
            }

            public override bool LessThan(AbstractSegmentResult a, AbstractSegmentResult b)
            {
                return a.mergeTerm.CompareTo(b.mergeTerm) < 0;
            }
        }
    }
}
