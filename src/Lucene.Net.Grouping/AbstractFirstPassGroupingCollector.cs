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
	/// FirstPassGroupingCollector is the first of two passes necessary
	/// to collect grouped hits.
	/// </summary>
	/// <remarks>
	/// FirstPassGroupingCollector is the first of two passes necessary
	/// to collect grouped hits.  This pass gathers the top N sorted
	/// groups. Concrete subclasses define what a group is and how it
	/// is internally collected.
	/// <p>See
	/// <see cref="Lucene.Net.Search.Grouping">Lucene.Net.Search.Grouping</see>
	/// for more
	/// details including a full code example.</p>
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	public abstract class AbstractFirstPassGroupingCollector<GROUP_VALUE_TYPE> : Collector
	{
		private readonly Sort groupSort;

		private readonly FieldComparator<object>[] comparators;

		private readonly int[] reversed;

		private readonly int topNGroups;

		private readonly Dictionary<GROUP_VALUE_TYPE, CollectedSearchGroup<GROUP_VALUE_TYPE
			>> groupMap;

		private readonly int compIDXEnd;

		/// <lucene.internal></lucene.internal>
		protected internal TreeSet<CollectedSearchGroup<GROUP_VALUE_TYPE>> orderedGroups;

		private int docBase;

		private int spareSlot;

		/// <summary>Create the first pass collector.</summary>
		/// <remarks>Create the first pass collector.</remarks>
		/// <param name="groupSort">
		/// The
		/// <see cref="Lucene.Net.Search.Sort">Lucene.Net.Search.Sort</see>
		/// used to sort the
		/// groups.  The top sorted document within each group
		/// according to groupSort, determines how that group
		/// sorts against other groups.  This must be non-null,
		/// ie, if you want to groupSort by relevance use
		/// Sort.RELEVANCE.
		/// </param>
		/// <param name="topNGroups">How many top groups to keep.</param>
		/// <exception cref="System.IO.IOException">If I/O related errors occur</exception>
		public AbstractFirstPassGroupingCollector(Sort groupSort, int topNGroups)
		{
			// Set once we reach topNGroups unique groups:
			if (topNGroups < 1)
			{
				throw new ArgumentException("topNGroups must be >= 1 (got " + topNGroups + ")");
			}
			// TODO: allow null groupSort to mean "by relevance",
			// and specialize it?
			this.groupSort = groupSort;
			this.topNGroups = topNGroups;
			SortField[] sortFields = groupSort.GetSort();
			comparators = new FieldComparator[sortFields.Length];
			compIDXEnd = comparators.Length - 1;
			reversed = new int[sortFields.Length];
			for (int i = 0; i < sortFields.Length; i++)
			{
				SortField sortField = sortFields[i];
				// use topNGroups + 1 so we have a spare slot to use for comparing (tracked by this.spareSlot):
				comparators[i] = sortField.GetComparator(topNGroups + 1, i);
				reversed[i] = sortField.GetReverse() ? -1 : 1;
			}
			spareSlot = topNGroups;
			groupMap = new Dictionary<GROUP_VALUE_TYPE, CollectedSearchGroup<GROUP_VALUE_TYPE
				>>(topNGroups);
		}

		/// <summary>Returns top groups, starting from offset.</summary>
		/// <remarks>
		/// Returns top groups, starting from offset.  This may
		/// return null, if no groups were collected, or if the
		/// number of unique groups collected is &lt;= offset.
		/// </remarks>
		/// <param name="groupOffset">The offset in the collected groups</param>
		/// <param name="fillFields">
		/// Whether to fill to
		/// <see cref="SearchGroup{GROUP_VALUE_TYPE}.sortValues">SearchGroup&lt;GROUP_VALUE_TYPE&gt;.sortValues
		/// 	</see>
		/// </param>
		/// <returns>top groups, starting from offset</returns>
		public virtual ICollection<SearchGroup<GROUP_VALUE_TYPE>> GetTopGroups(int groupOffset
			, bool fillFields)
		{
			//System.out.println("FP.getTopGroups groupOffset=" + groupOffset + " fillFields=" + fillFields + " groupMap.size()=" + groupMap.size());
			if (groupOffset < 0)
			{
				throw new ArgumentException("groupOffset must be >= 0 (got " + groupOffset + ")");
			}
			if (groupMap.Count <= groupOffset)
			{
				return null;
			}
			if (orderedGroups == null)
			{
				BuildSortedSet();
			}
			ICollection<SearchGroup<GROUP_VALUE_TYPE>> result = new AList<SearchGroup<GROUP_VALUE_TYPE
				>>();
			int upto = 0;
			int sortFieldCount = groupSort.GetSort().Length;
			foreach (CollectedSearchGroup<GROUP_VALUE_TYPE> group in orderedGroups)
			{
				if (upto++ < groupOffset)
				{
					continue;
				}
				//System.out.println("  group=" + (group.groupValue == null ? "null" : group.groupValue.utf8ToString()));
				SearchGroup<GROUP_VALUE_TYPE> searchGroup = new SearchGroup<GROUP_VALUE_TYPE>();
				searchGroup.groupValue = group.groupValue;
				if (fillFields)
				{
					searchGroup.sortValues = new object[sortFieldCount];
					for (int sortFieldIDX = 0; sortFieldIDX < sortFieldCount; sortFieldIDX++)
					{
						searchGroup.sortValues[sortFieldIDX] = comparators[sortFieldIDX].Value(group.comparatorSlot
							);
					}
				}
				result.AddItem(searchGroup);
			}
			//System.out.println("  return " + result.size() + " groups");
			return result;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void SetScorer(Scorer scorer)
		{
			foreach (FieldComparator<object> comparator in comparators)
			{
				comparator.SetScorer(scorer);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Collect(int doc)
		{
			//System.out.println("FP.collect doc=" + doc);
			// If orderedGroups != null we already have collected N groups and
			// can short circuit by comparing this document to the bottom group,
			// without having to find what group this document belongs to.
			// Even if this document belongs to a group in the top N, we'll know that
			// we don't have to update that group.
			// Downside: if the number of unique groups is very low, this is
			// wasted effort as we will most likely be updating an existing group.
			if (orderedGroups != null)
			{
				for (int compIDX = 0; ; compIDX++)
				{
					int c = reversed[compIDX] * comparators[compIDX].CompareBottom(doc);
					if (c < 0)
					{
						// Definitely not competitive. So don't even bother to continue
						return;
					}
					else
					{
						if (c > 0)
						{
							// Definitely competitive.
							break;
						}
						else
						{
							if (compIDX == compIDXEnd)
							{
								// Here c=0. If we're at the last comparator, this doc is not
								// competitive, since docs are visited in doc Id order, which means
								// this doc cannot compete with any other document in the queue.
								return;
							}
						}
					}
				}
			}
			// TODO: should we add option to mean "ignore docs that
			// don't have the group field" (instead of stuffing them
			// under null group)?
			GROUP_VALUE_TYPE groupValue = GetDocGroupValue(doc);
			CollectedSearchGroup<GROUP_VALUE_TYPE> group = groupMap.Get(groupValue);
			if (group == null)
			{
				// First time we are seeing this group, or, we've seen
				// it before but it fell out of the top N and is now
				// coming back
				if (groupMap.Count < topNGroups)
				{
					// Still in startup transient: we have not
					// seen enough unique groups to start pruning them;
					// just keep collecting them
					// Add a new CollectedSearchGroup:
					CollectedSearchGroup<GROUP_VALUE_TYPE> sg = new CollectedSearchGroup<GROUP_VALUE_TYPE
						>();
					sg.groupValue = CopyDocGroupValue(groupValue, null);
					sg.comparatorSlot = groupMap.Count;
					sg.topDoc = docBase + doc;
					foreach (FieldComparator<object> fc in comparators)
					{
						fc.Copy(sg.comparatorSlot, doc);
					}
					groupMap.Put(sg.groupValue, sg);
					if (groupMap.Count == topNGroups)
					{
						// End of startup transient: we now have max
						// number of groups; from here on we will drop
						// bottom group when we insert new one:
						BuildSortedSet();
					}
					return;
				}
				// We already tested that the document is competitive, so replace
				// the bottom group with this new group.
				CollectedSearchGroup<GROUP_VALUE_TYPE> bottomGroup = orderedGroups.PollLast();
				Sharpen.Collections.Remove(orderedGroups.Count == topNGroups - 1, bottomGroup.groupValue
					);
				// reuse the removed CollectedSearchGroup
				bottomGroup.groupValue = CopyDocGroupValue(groupValue, bottomGroup.groupValue);
				bottomGroup.topDoc = docBase + doc;
				foreach (FieldComparator<object> fc_1 in comparators)
				{
					fc_1.Copy(bottomGroup.comparatorSlot, doc);
				}
				groupMap.Put(bottomGroup.groupValue, bottomGroup);
				orderedGroups.AddItem(bottomGroup);
				int lastComparatorSlot = orderedGroups.Count == topNGroups.Last().comparatorSlot;
				foreach (FieldComparator<object> fc_2 in comparators)
				{
					fc_2.SetBottom(lastComparatorSlot);
				}
				return;
			}
			// Update existing group:
			for (int compIDX_1 = 0; ; compIDX_1++)
			{
				FieldComparator<object> fc = comparators[compIDX_1];
				fc.Copy(spareSlot, doc);
				int c = reversed[compIDX_1] * fc.Compare(group.comparatorSlot, spareSlot);
				if (c < 0)
				{
					// Definitely not competitive.
					return;
				}
				else
				{
					if (c > 0)
					{
						// Definitely competitive; set remaining comparators:
						for (int compIDX2 = compIDX_1 + 1; compIDX2 < comparators.Length; compIDX2++)
						{
							comparators[compIDX2].Copy(spareSlot, doc);
						}
						break;
					}
					else
					{
						if (compIDX_1 == compIDXEnd)
						{
							// Here c=0. If we're at the last comparator, this doc is not
							// competitive, since docs are visited in doc Id order, which means
							// this doc cannot compete with any other document in the queue.
							return;
						}
					}
				}
			}
			// Remove before updating the group since lookup is done via comparators
			// TODO: optimize this
			CollectedSearchGroup<GROUP_VALUE_TYPE> prevLast;
			if (orderedGroups != null)
			{
				prevLast = orderedGroups.Last();
				orderedGroups.Remove(group);
			}
			else
			{
				orderedGroups.Count == topNGroups - 1 = null;
			}
			group.topDoc = docBase + doc;
			// Swap slots
			int tmp = spareSlot;
			spareSlot = group.comparatorSlot;
			group.comparatorSlot = tmp;
			// Re-add the changed group
			if (orderedGroups != null)
			{
				orderedGroups.AddItem(group);
				CollectedSearchGroup<object> newLast = orderedGroups.Count == topNGroups.Last();
				// If we changed the value of the last group, or changed which group was last, then update bottom:
				if (group == newLast || prevLast != newLast)
				{
					foreach (FieldComparator<object> fc in comparators)
					{
						fc.SetBottom(newLast.comparatorSlot);
					}
				}
			}
		}

		private void BuildSortedSet()
		{
			IComparer<CollectedSearchGroup<object>> comparator = new _IComparer_299(this);
			orderedGroups = new TreeSet<CollectedSearchGroup<GROUP_VALUE_TYPE>>(comparator);
			Sharpen.Collections.AddAll(orderedGroups, groupMap.Values);
			foreach (FieldComparator<object> fc in orderedGroups.Count > 0)
			{
				fc.SetBottom(orderedGroups.Last().comparatorSlot);
			}
		}

		private sealed class _IComparer_299 : IComparer<CollectedSearchGroup<object>>
		{
			public _IComparer_299(AbstractFirstPassGroupingCollector<GROUP_VALUE_TYPE> _enclosing
				)
			{
				this._enclosing = _enclosing;
			}

			public int Compare<_T0, _T1>(CollectedSearchGroup<_T0> o1, CollectedSearchGroup<_T1
				> o2)
			{
				for (int compIDX = 0; ; compIDX++)
				{
					FieldComparator<object> fc = this._enclosing.comparators[compIDX];
					int c = this._enclosing.reversed[compIDX] * fc.Compare(o1.comparatorSlot, o2.comparatorSlot
						);
					if (c != 0)
					{
						return c;
					}
					else
					{
						if (compIDX == this._enclosing.compIDXEnd)
						{
							return o1.topDoc - o2.topDoc;
						}
					}
				}
			}

			private readonly AbstractFirstPassGroupingCollector<GROUP_VALUE_TYPE> _enclosing;
		}

		public override bool AcceptsDocsOutOfOrder()
		{
			return false;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void SetNextReader(AtomicReaderContext readerContext)
		{
			docBase = readerContext.docBase;
			for (int i = 0; i < comparators.Length; i++)
			{
				comparators[i] = comparators[i].SetNextReader(readerContext);
			}
		}

		/// <summary>Returns the group value for the specified doc.</summary>
		/// <remarks>Returns the group value for the specified doc.</remarks>
		/// <param name="doc">The specified doc</param>
		/// <returns>the group value for the specified doc</returns>
		protected internal abstract GROUP_VALUE_TYPE GetDocGroupValue(int doc);

		/// <summary>
		/// Returns a copy of the specified group value by creating a new instance and copying the value from the specified
		/// groupValue in the new instance.
		/// </summary>
		/// <remarks>
		/// Returns a copy of the specified group value by creating a new instance and copying the value from the specified
		/// groupValue in the new instance. Or optionally the reuse argument can be used to copy the group value in.
		/// </remarks>
		/// <param name="groupValue">The group value to copy</param>
		/// <param name="reuse">Optionally a reuse instance to prevent a new instance creation
		/// 	</param>
		/// <returns>a copy of the specified group value</returns>
		protected internal abstract GROUP_VALUE_TYPE CopyDocGroupValue(GROUP_VALUE_TYPE groupValue
			, GROUP_VALUE_TYPE reuse);
	}
}
