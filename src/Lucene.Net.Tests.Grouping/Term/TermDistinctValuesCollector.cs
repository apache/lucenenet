/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Search;
using Org.Apache.Lucene.Search.Grouping;
using Org.Apache.Lucene.Search.Grouping.Term;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Org.Apache.Lucene.Search.Grouping.Term
{
	/// <summary>
	/// A term based implementation of
	/// <see cref="Org.Apache.Lucene.Search.Grouping.AbstractDistinctValuesCollector{GC}"
	/// 	>Org.Apache.Lucene.Search.Grouping.AbstractDistinctValuesCollector&lt;GC&gt;</see>
	/// that relies
	/// on
	/// <see cref="Org.Apache.Lucene.Index.SortedDocValues">Org.Apache.Lucene.Index.SortedDocValues
	/// 	</see>
	/// to count the distinct values per group.
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public class TermDistinctValuesCollector : AbstractDistinctValuesCollector<TermDistinctValuesCollector.GroupCount
		>
	{
		private readonly string groupField;

		private readonly string countField;

		private readonly IList<TermDistinctValuesCollector.GroupCount> groups;

		private readonly SentinelIntSet ordSet;

		private readonly TermDistinctValuesCollector.GroupCount groupCounts;

		private SortedDocValues groupFieldTermIndex;

		private SortedDocValues countFieldTermIndex;

		/// <summary>
		/// Constructs
		/// <see cref="TermDistinctValuesCollector">TermDistinctValuesCollector</see>
		/// instance.
		/// </summary>
		/// <param name="groupField">The field to group by</param>
		/// <param name="countField">The field to count distinct values for</param>
		/// <param name="groups">The top N groups, collected during the first phase search</param>
		public TermDistinctValuesCollector(string groupField, string countField, ICollection
			<SearchGroup<BytesRef>> groups)
		{
			this.groupField = groupField;
			this.countField = countField;
			this.groups = new AList<TermDistinctValuesCollector.GroupCount>(groups.Count);
			foreach (SearchGroup<BytesRef> group in groups)
			{
				this.groups.AddItem(new TermDistinctValuesCollector.GroupCount(group.groupValue));
			}
			ordSet = new SentinelIntSet(groups.Count, -2);
			groupCounts = new TermDistinctValuesCollector.GroupCount[ordSet.keys.Length];
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Collect(int doc)
		{
			int slot = ordSet.Find(groupFieldTermIndex.GetOrd(doc));
			if (slot < 0)
			{
				return;
			}
			TermDistinctValuesCollector.GroupCount gc = groupCounts[slot];
			int countOrd = countFieldTermIndex.GetOrd(doc);
			if (DoesNotContainOrd(countOrd, gc.ords))
			{
				if (countOrd == -1)
				{
					gc.uniqueValues.AddItem(null);
				}
				else
				{
					BytesRef br = new BytesRef();
					countFieldTermIndex.LookupOrd(countOrd, br);
					gc.uniqueValues.AddItem(br);
				}
				gc.ords = Arrays.CopyOf(gc.ords, gc.ords.Length + 1);
				gc.ords[gc.ords.Length - 1] = countOrd;
				if (gc.ords.Length > 1)
				{
					Arrays.Sort(gc.ords);
				}
			}
		}

		private bool DoesNotContainOrd(int ord, int[] ords)
		{
			if (ords.Length == 0)
			{
				return true;
			}
			else
			{
				if (ords.Length == 1)
				{
					return ord != ords[0];
				}
			}
			return System.Array.BinarySearch(ords, ord) < 0;
		}

		public override IList<TermDistinctValuesCollector.GroupCount> GetGroups()
		{
			return groups;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void SetNextReader(AtomicReaderContext context)
		{
			groupFieldTermIndex = FieldCache.DEFAULT.GetTermsIndex(((AtomicReader)context.Reader
				()), groupField);
			countFieldTermIndex = FieldCache.DEFAULT.GetTermsIndex(((AtomicReader)context.Reader
				()), countField);
			ordSet.Clear();
			foreach (TermDistinctValuesCollector.GroupCount group in groups)
			{
				int groupOrd = group.groupValue == null ? -1 : groupFieldTermIndex.LookupTerm(group
					.groupValue);
				if (group.groupValue != null && groupOrd < 0)
				{
					continue;
				}
				groupCounts[ordSet.Put(groupOrd)] = group;
				group.ords = new int[group.uniqueValues.Count];
				Arrays.Fill(group.ords, -2);
				int i = 0;
				foreach (BytesRef value in group.uniqueValues)
				{
					int countOrd = value == null ? -1 : countFieldTermIndex.LookupTerm(value);
					if (value == null || countOrd >= 0)
					{
						group.ords[i++] = countOrd;
					}
				}
			}
		}

		/// <summary>Holds distinct values for a single group.</summary>
		/// <remarks>Holds distinct values for a single group.</remarks>
		/// <lucene.experimental></lucene.experimental>
		public class GroupCount : AbstractDistinctValuesCollector.GroupCount<BytesRef>
		{
			internal int[] ords;

			public GroupCount(BytesRef groupValue) : base(groupValue)
			{
			}
		}
	}
}
