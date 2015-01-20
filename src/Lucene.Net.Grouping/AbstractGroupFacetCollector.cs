/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Lucene.Net.Search;
using Lucene.Net.Search.Grouping;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Search.Grouping
{
	/// <summary>Base class for computing grouped facets.</summary>
	/// <remarks>Base class for computing grouped facets.</remarks>
	/// <lucene.experimental></lucene.experimental>
	public abstract class AbstractGroupFacetCollector : Collector
	{
		protected internal readonly string groupField;

		protected internal readonly string facetField;

		protected internal readonly BytesRef facetPrefix;

		protected internal readonly IList<AbstractGroupFacetCollector.SegmentResult> segmentResults;

		protected internal int[] segmentFacetCounts;

		protected internal int segmentTotalCount;

		protected internal int startFacetOrd;

		protected internal int endFacetOrd;

		protected internal AbstractGroupFacetCollector(string groupField, string facetField
			, BytesRef facetPrefix)
		{
			this.groupField = groupField;
			this.facetField = facetField;
			this.facetPrefix = facetPrefix;
			segmentResults = new AList<AbstractGroupFacetCollector.SegmentResult>();
		}

		/// <summary>Returns grouped facet results that were computed over zero or more segments.
		/// 	</summary>
		/// <remarks>
		/// Returns grouped facet results that were computed over zero or more segments.
		/// Grouped facet counts are merged from zero or more segment results.
		/// </remarks>
		/// <param name="size">The total number of facets to include. This is typically offset + limit
		/// 	</param>
		/// <param name="minCount">The minimum count a facet entry should have to be included in the grouped facet result
		/// 	</param>
		/// <param name="orderByCount">
		/// Whether to sort the facet entries by facet entry count. If <code>false</code> then the facets
		/// are sorted lexicographically in ascending order.
		/// </param>
		/// <returns>grouped facet results</returns>
		/// <exception cref="System.IO.IOException">If I/O related errors occur during merging segment grouped facet counts.
		/// 	</exception>
		public virtual AbstractGroupFacetCollector.GroupedFacetResult MergeSegmentResults
			(int size, int minCount, bool orderByCount)
		{
			if (segmentFacetCounts != null)
			{
				segmentResults.AddItem(CreateSegmentResult());
				segmentFacetCounts = null;
			}
			// reset
			int totalCount = 0;
			int missingCount = 0;
			AbstractGroupFacetCollector.SegmentResultPriorityQueue segments = new AbstractGroupFacetCollector.SegmentResultPriorityQueue
				(segmentResults.Count);
			foreach (AbstractGroupFacetCollector.SegmentResult segmentResult in segmentResults)
			{
				missingCount += segmentResult.missing;
				if (segmentResult.mergePos >= segmentResult.maxTermPos)
				{
					continue;
				}
				totalCount += segmentResult.total;
				segments.Add(segmentResult);
			}
			AbstractGroupFacetCollector.GroupedFacetResult facetResult = new AbstractGroupFacetCollector.GroupedFacetResult
				(size, minCount, orderByCount, totalCount, missingCount);
			while (segments.Size() > 0)
			{
				AbstractGroupFacetCollector.SegmentResult segmentResult_1 = segments.Top();
				BytesRef currentFacetValue = BytesRef.DeepCopyOf(segmentResult_1.mergeTerm);
				int count = 0;
				do
				{
					count += segmentResult_1.counts[segmentResult_1.mergePos++];
					if (segmentResult_1.mergePos < segmentResult_1.maxTermPos)
					{
						segmentResult_1.NextTerm();
						segmentResult_1 = segments.UpdateTop();
					}
					else
					{
						segments.Pop();
						segmentResult_1 = segments.Top();
						if (segmentResult_1 == null)
						{
							break;
						}
					}
				}
				while (currentFacetValue.Equals(segmentResult_1.mergeTerm));
				facetResult.AddFacetCount(currentFacetValue, count);
			}
			return facetResult;
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected internal abstract AbstractGroupFacetCollector.SegmentResult CreateSegmentResult
			();

		/// <exception cref="System.IO.IOException"></exception>
		public override void SetScorer(Scorer scorer)
		{
		}

		public override bool AcceptsDocsOutOfOrder()
		{
			return true;
		}

		/// <summary>The grouped facet result.</summary>
		/// <remarks>The grouped facet result. Containing grouped facet entries, total count and total missing count.
		/// 	</remarks>
		public class GroupedFacetResult
		{
			private sealed class _IComparer_121 : IComparer<AbstractGroupFacetCollector.FacetEntry
				>
			{
				public _IComparer_121()
				{
				}

				public int Compare(AbstractGroupFacetCollector.FacetEntry a, AbstractGroupFacetCollector.FacetEntry
					 b)
				{
					int cmp = b.count - a.count;
					// Highest count first!
					if (cmp != 0)
					{
						return cmp;
					}
					return a.value.CompareTo(b.value);
				}
			}

			private static readonly IComparer<AbstractGroupFacetCollector.FacetEntry> orderByCountAndValue
				 = new _IComparer_121();

			private sealed class _IComparer_134 : IComparer<AbstractGroupFacetCollector.FacetEntry
				>
			{
				public _IComparer_134()
				{
				}

				public int Compare(AbstractGroupFacetCollector.FacetEntry a, AbstractGroupFacetCollector.FacetEntry
					 b)
				{
					return a.value.CompareTo(b.value);
				}
			}

			private static readonly IComparer<AbstractGroupFacetCollector.FacetEntry> orderByValue
				 = new _IComparer_134();

			private readonly int maxSize;

			private readonly NavigableSet<AbstractGroupFacetCollector.FacetEntry> facetEntries;

			private readonly int totalMissingCount;

			private readonly int totalCount;

			private int currentMin;

			public GroupedFacetResult(int size, int minCount, bool orderByCount, int totalCount
				, int totalMissingCount)
			{
				this.facetEntries = new TreeSet<AbstractGroupFacetCollector.FacetEntry>(orderByCount
					 ? orderByCountAndValue : orderByValue);
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
				AbstractGroupFacetCollector.FacetEntry facetEntry = new AbstractGroupFacetCollector.FacetEntry
					(facetValue, count);
				if (facetEntries.Count == maxSize)
				{
					if (facetEntries.Higher(facetEntry) == null)
					{
						return;
					}
					facetEntries.PollLast();
				}
				facetEntries.AddItem(facetEntry);
				if (facetEntries.Count == maxSize)
				{
					currentMin = facetEntries.Last().count;
				}
			}

			/// <summary>Returns a list of facet entries to be rendered based on the specified offset and limit.
			/// 	</summary>
			/// <remarks>
			/// Returns a list of facet entries to be rendered based on the specified offset and limit.
			/// The facet entries are retrieved from the facet entries collected during merging.
			/// </remarks>
			/// <param name="offset">The offset in the collected facet entries during merging</param>
			/// <param name="limit">The number of facets to return starting from the offset.</param>
			/// <returns>a list of facet entries to be rendered based on the specified offset and limit
			/// 	</returns>
			public virtual IList<AbstractGroupFacetCollector.FacetEntry> GetFacetEntries(int 
				offset, int limit)
			{
				IList<AbstractGroupFacetCollector.FacetEntry> entries = new List<AbstractGroupFacetCollector.FacetEntry
					>();
				int skipped = 0;
				int included = 0;
				foreach (AbstractGroupFacetCollector.FacetEntry facetEntry in facetEntries)
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
					entries.AddItem(facetEntry);
				}
				return entries;
			}

			/// <summary>Returns the sum of all facet entries counts.</summary>
			/// <remarks>Returns the sum of all facet entries counts.</remarks>
			/// <returns>the sum of all facet entries counts</returns>
			public virtual int GetTotalCount()
			{
				return totalCount;
			}

			/// <summary>Returns the number of groups that didn't have a facet value.</summary>
			/// <remarks>Returns the number of groups that didn't have a facet value.</remarks>
			/// <returns>the number of groups that didn't have a facet value</returns>
			public virtual int GetTotalMissingCount()
			{
				return totalMissingCount;
			}
		}

		/// <summary>Represents a facet entry with a value and a count.</summary>
		/// <remarks>Represents a facet entry with a value and a count.</remarks>
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
				if (this == o)
				{
					return true;
				}
				if (o == null || GetType() != o.GetType())
				{
					return false;
				}
				AbstractGroupFacetCollector.FacetEntry that = (AbstractGroupFacetCollector.FacetEntry
					)o;
				if (count != that.count)
				{
					return false;
				}
				if (!value.Equals(that.value))
				{
					return false;
				}
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
				return "FacetEntry{" + "value=" + value.Utf8ToString() + ", count=" + count + '}';
			}

			/// <returns>The value of this facet entry</returns>
			public virtual BytesRef GetValue()
			{
				return value;
			}

			/// <returns>The count (number of groups) of this facet entry.</returns>
			public virtual int GetCount()
			{
				return count;
			}
		}

		/// <summary>Contains the local grouped segment counts for a particular segment.</summary>
		/// <remarks>
		/// Contains the local grouped segment counts for a particular segment.
		/// Each <code>SegmentResult</code> must be added together.
		/// </remarks>
		protected internal abstract class SegmentResult
		{
			protected internal readonly int[] counts;

			protected internal readonly int total;

			protected internal readonly int missing;

			protected internal readonly int maxTermPos;

			protected internal BytesRef mergeTerm;

			protected internal int mergePos;

			protected internal SegmentResult(int[] counts, int total, int missing, int maxTermPos
				)
			{
				this.counts = counts;
				this.total = total;
				this.missing = missing;
				this.maxTermPos = maxTermPos;
			}

			/// <summary>Go to next term in this <code>SegmentResult</code> in order to retrieve the grouped facet counts.
			/// 	</summary>
			/// <remarks>Go to next term in this <code>SegmentResult</code> in order to retrieve the grouped facet counts.
			/// 	</remarks>
			/// <exception cref="System.IO.IOException">If I/O related errors occur</exception>
			protected internal abstract void NextTerm();
		}

		private class SegmentResultPriorityQueue : PriorityQueue<AbstractGroupFacetCollector.SegmentResult
			>
		{
			public SegmentResultPriorityQueue(int maxSize) : base(maxSize)
			{
			}

			protected override bool LessThan(AbstractGroupFacetCollector.SegmentResult a, AbstractGroupFacetCollector.SegmentResult
				 b)
			{
				return a.mergeTerm.CompareTo(b.mergeTerm) < 0;
			}
		}
	}
}
