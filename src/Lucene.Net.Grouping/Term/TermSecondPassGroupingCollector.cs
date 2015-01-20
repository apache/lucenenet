/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Grouping;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Search.Grouping.Term
{
	/// <summary>
	/// Concrete implementation of
	/// <see cref="Lucene.Net.Search.Grouping.AbstractSecondPassGroupingCollector{GROUP_VALUE_TYPE}
	/// 	">Lucene.Net.Search.Grouping.AbstractSecondPassGroupingCollector&lt;GROUP_VALUE_TYPE&gt;
	/// 	</see>
	/// that groups based on
	/// field values and more specifically uses
	/// <see cref="Lucene.Net.Index.SortedDocValues">Lucene.Net.Index.SortedDocValues
	/// 	</see>
	/// to collect grouped docs.
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public class TermSecondPassGroupingCollector : AbstractSecondPassGroupingCollector
		<BytesRef>
	{
		private readonly SentinelIntSet ordSet;

		private SortedDocValues index;

		private readonly string groupField;

		/// <exception cref="System.IO.IOException"></exception>
		public TermSecondPassGroupingCollector(string groupField, ICollection<SearchGroup
			<BytesRef>> groups, Sort groupSort, Sort withinGroupSort, int maxDocsPerGroup, bool
			 getScores, bool getMaxScores, bool fillSortFields) : base(groups, groupSort, withinGroupSort
			, maxDocsPerGroup, getScores, getMaxScores, fillSortFields)
		{
			ordSet = new SentinelIntSet(groupMap.Count, -2);
			this.groupField = groupField;
			groupDocs = (AbstractSecondPassGroupingCollector.SearchGroupDocs<BytesRef>[])new 
				AbstractSecondPassGroupingCollector.SearchGroupDocs[ordSet.keys.Length];
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void SetNextReader(AtomicReaderContext readerContext)
		{
			base.SetNextReader(readerContext);
			index = FieldCache.DEFAULT.GetTermsIndex(((AtomicReader)readerContext.Reader()), 
				groupField);
			// Rebuild ordSet
			ordSet.Clear();
			foreach (AbstractSecondPassGroupingCollector.SearchGroupDocs<BytesRef> group in groupMap
				.Values)
			{
				//      System.out.println("  group=" + (group.groupValue == null ? "null" : group.groupValue.utf8ToString()));
				int ord = group.groupValue == null ? -1 : index.LookupTerm(group.groupValue);
				if (group.groupValue == null || ord >= 0)
				{
					groupDocs[ordSet.Put(ord)] = group;
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected internal override AbstractSecondPassGroupingCollector.SearchGroupDocs<BytesRef
			> RetrieveGroup(int doc)
		{
			int slot = ordSet.Find(index.GetOrd(doc));
			if (slot >= 0)
			{
				return groupDocs[slot];
			}
			return null;
		}
	}
}
