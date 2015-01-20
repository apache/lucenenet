/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Grouping;
using Sharpen;

namespace Lucene.Net.Search.Grouping
{
	/// <summary>
	/// SecondPassGroupingCollector is the second of two passes
	/// necessary to collect grouped docs.
	/// </summary>
	/// <remarks>
	/// SecondPassGroupingCollector is the second of two passes
	/// necessary to collect grouped docs.  This pass gathers the
	/// top N documents per top group computed from the
	/// first pass. Concrete subclasses define what a group is and how it
	/// is internally collected.
	/// <p>See
	/// <see cref="Lucene.Net.Search.Grouping">Lucene.Net.Search.Grouping</see>
	/// for more
	/// details including a full code example.</p>
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	public abstract class AbstractSecondPassGroupingCollector<GROUP_VALUE_TYPE> : Collector
	{
		protected internal readonly IDictionary<GROUP_VALUE_TYPE, AbstractSecondPassGroupingCollector.SearchGroupDocs
			<GROUP_VALUE_TYPE>> groupMap;

		private readonly int maxDocsPerGroup;

		protected internal AbstractSecondPassGroupingCollector.SearchGroupDocs<GROUP_VALUE_TYPE
			>[] groupDocs;

		private readonly ICollection<SearchGroup<GROUP_VALUE_TYPE>> groups;

		private readonly Sort withinGroupSort;

		private readonly Sort groupSort;

		private int totalHitCount;

		private int totalGroupedHitCount;

		/// <exception cref="System.IO.IOException"></exception>
		public AbstractSecondPassGroupingCollector(ICollection<SearchGroup<GROUP_VALUE_TYPE
			>> groups, Sort groupSort, Sort withinGroupSort, int maxDocsPerGroup, bool getScores
			, bool getMaxScores, bool fillSortFields)
		{
			//System.out.println("SP init");
			if (groups.Count == 0)
			{
				throw new ArgumentException("no groups to collect (groups.size() is 0)");
			}
			this.groupSort = groupSort;
			this.withinGroupSort = withinGroupSort;
			this.groups = groups;
			this.maxDocsPerGroup = maxDocsPerGroup;
			groupMap = new Dictionary<GROUP_VALUE_TYPE, AbstractSecondPassGroupingCollector.SearchGroupDocs
				<GROUP_VALUE_TYPE>>(groups.Count);
			foreach (SearchGroup<GROUP_VALUE_TYPE> group in groups)
			{
				//System.out.println("  prep group=" + (group.groupValue == null ? "null" : group.groupValue.utf8ToString()));
				TopDocsCollector<object> collector;
				if (withinGroupSort == null)
				{
					// Sort by score
					collector = TopScoreDocCollector.Create(maxDocsPerGroup, true);
				}
				else
				{
					// Sort by fields
					collector = TopFieldCollector.Create(withinGroupSort, maxDocsPerGroup, fillSortFields
						, getScores, getMaxScores, true);
				}
				groupMap.Put(group.groupValue, new AbstractSecondPassGroupingCollector.SearchGroupDocs
					<GROUP_VALUE_TYPE>(this, group.groupValue, collector));
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void SetScorer(Scorer scorer)
		{
			foreach (AbstractSecondPassGroupingCollector.SearchGroupDocs<GROUP_VALUE_TYPE> group
				 in groupMap.Values)
			{
				group.collector.SetScorer(scorer);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Collect(int doc)
		{
			totalHitCount++;
			AbstractSecondPassGroupingCollector.SearchGroupDocs<GROUP_VALUE_TYPE> group = RetrieveGroup
				(doc);
			if (group != null)
			{
				totalGroupedHitCount++;
				group.collector.Collect(doc);
			}
		}

		/// <summary>Returns the group the specified doc belongs to or <code>null</code> if no group could be retrieved.
		/// 	</summary>
		/// <remarks>Returns the group the specified doc belongs to or <code>null</code> if no group could be retrieved.
		/// 	</remarks>
		/// <param name="doc">The specified doc</param>
		/// <returns>the group the specified doc belongs to or <code>null</code> if no group could be retrieved
		/// 	</returns>
		/// <exception cref="System.IO.IOException">If an I/O related error occurred</exception>
		protected internal abstract AbstractSecondPassGroupingCollector.SearchGroupDocs<GROUP_VALUE_TYPE
			> RetrieveGroup(int doc);

		/// <exception cref="System.IO.IOException"></exception>
		public override void SetNextReader(AtomicReaderContext readerContext)
		{
			//System.out.println("SP.setNextReader");
			foreach (AbstractSecondPassGroupingCollector.SearchGroupDocs<GROUP_VALUE_TYPE> group
				 in groupMap.Values)
			{
				group.collector.SetNextReader(readerContext);
			}
		}

		public override bool AcceptsDocsOutOfOrder()
		{
			return false;
		}

		public virtual TopGroups<GROUP_VALUE_TYPE> GetTopGroups(int withinGroupOffset)
		{
			GroupDocs<GROUP_VALUE_TYPE>[] groupDocsResult = (GroupDocs<GROUP_VALUE_TYPE>[])new 
				GroupDocs[groups.Count];
			int groupIDX = 0;
			float maxScore = float.MinValue;
			foreach (SearchGroup<object> group in groups)
			{
				AbstractSecondPassGroupingCollector.SearchGroupDocs<GROUP_VALUE_TYPE> groupDocs = 
					groupMap.Get(group.groupValue);
				TopDocs topDocs = groupDocs.collector.TopDocs(withinGroupOffset, maxDocsPerGroup);
				groupDocsResult[groupIDX++] = new GroupDocs<GROUP_VALUE_TYPE>(float.NaN, topDocs.
					GetMaxScore(), topDocs.totalHits, topDocs.scoreDocs, groupDocs.groupValue, group
					.sortValues);
				maxScore = Math.Max(maxScore, topDocs.GetMaxScore());
			}
			return new TopGroups<GROUP_VALUE_TYPE>(groupSort.GetSort(), withinGroupSort == null
				 ? null : withinGroupSort.GetSort(), totalHitCount, totalGroupedHitCount, groupDocsResult
				, maxScore);
		}

		public class SearchGroupDocs<GROUP_VALUE_TYPE>
		{
			public readonly GROUP_VALUE_TYPE groupValue;

			public readonly TopDocsCollector<object> collector;

			public SearchGroupDocs(AbstractSecondPassGroupingCollector<GROUP_VALUE_TYPE> _enclosing
				, GROUP_VALUE_TYPE groupValue, TopDocsCollector<object> collector)
			{
				this._enclosing = _enclosing;
				// TODO: merge with SearchGroup or not?
				// ad: don't need to build a new hashmap
				// disad: blows up the size of SearchGroup if we need many of them, and couples implementations
				this.groupValue = groupValue;
				this.collector = collector;
			}

			private readonly AbstractSecondPassGroupingCollector<GROUP_VALUE_TYPE> _enclosing;
		}
	}
}
