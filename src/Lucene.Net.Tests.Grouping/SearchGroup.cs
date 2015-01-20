/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Org.Apache.Lucene.Search;
using Org.Apache.Lucene.Search.Grouping;
using Sharpen;

namespace Org.Apache.Lucene.Search.Grouping
{
	/// <summary>Represents a group that is found during the first pass search.</summary>
	/// <remarks>Represents a group that is found during the first pass search.</remarks>
	/// <lucene.experimental></lucene.experimental>
	public class SearchGroup<GROUP_VALUE_TYPE>
	{
		/// <summary>The value that defines this group</summary>
		public GROUP_VALUE_TYPE groupValue;

		/// <summary>The sort values used during sorting.</summary>
		/// <remarks>
		/// The sort values used during sorting. These are the
		/// groupSort field values of the highest rank document
		/// (by the groupSort) within the group.  Can be
		/// <code>null</code> if <code>fillFields=false</code> had
		/// been passed to
		/// <see cref="AbstractFirstPassGroupingCollector{GROUP_VALUE_TYPE}.GetTopGroups(int, bool)
		/// 	">AbstractFirstPassGroupingCollector&lt;GROUP_VALUE_TYPE&gt;.GetTopGroups(int, bool)
		/// 	</see>
		/// 
		/// </remarks>
		public object[] sortValues;

		public override string ToString()
		{
			return ("SearchGroup(groupValue=" + groupValue + " sortValues=" + Arrays.ToString
				(sortValues) + ")");
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
			SearchGroup<object> that = (SearchGroup<object>)o;
			if (groupValue == null)
			{
				if (that.groupValue != null)
				{
					return false;
				}
			}
			else
			{
				if (!groupValue.Equals(that.groupValue))
				{
					return false;
				}
			}
			return true;
		}

		public override int GetHashCode()
		{
			return groupValue != null ? groupValue.GetHashCode() : 0;
		}

		private class ShardIter<T>
		{
			public readonly Iterator<SearchGroup<T>> iter;

			public readonly int shardIndex;

			public ShardIter(ICollection<SearchGroup<T>> shard, int shardIndex)
			{
				this.shardIndex = shardIndex;
				iter = shard.Iterator();
			}

			public virtual SearchGroup<T> Next()
			{
				SearchGroup<T> group = iter.HasNext().HasNext().Next();
				if (group.sortValues == null)
				{
					throw new ArgumentException("group.sortValues is null; you must pass fillFields=true to the first pass collector"
						);
				}
				return group;
			}

			public override string ToString()
			{
				return "ShardIter(shard=" + shardIndex + ")";
			}
		}

		private class MergedGroup<T>
		{
			public readonly T groupValue;

			public object[] topValues;

			public readonly IList<SearchGroup.ShardIter<T>> shards = new AList<SearchGroup.ShardIter
				<T>>();

			public int minShardIndex;

			public bool processed;

			public bool inQueue;

			public MergedGroup(T groupValue)
			{
				// Holds all shards currently on the same group
				// groupValue may be null!
				this.groupValue = groupValue;
			}

			// Only for assert
			private bool NeverEquals(object _other)
			{
				if (_other is SearchGroup.MergedGroup)
				{
					SearchGroup.MergedGroup<object> other = (SearchGroup.MergedGroup<object>)_other;
				}
				//HM:revisit
				return true;
			}

			public override bool Equals(object _other)
			{
				// We never have another MergedGroup instance with
				// same groupValue
				if (NeverEquals(_other) is SearchGroup.MergedGroup)
				{
					SearchGroup.MergedGroup<object> other = (SearchGroup.MergedGroup<object>)_other;
					if (groupValue == null)
					{
						return other == null;
					}
					else
					{
						return groupValue.Equals(other);
					}
				}
				else
				{
					return false;
				}
			}

			public override int GetHashCode()
			{
				if (groupValue == null)
				{
					return 0;
				}
				else
				{
					return groupValue.GetHashCode();
				}
			}
		}

		private class GroupComparator<T> : IComparer<SearchGroup.MergedGroup<T>>
		{
			public readonly FieldComparator[] comparators;

			public readonly int[] reversed;

			/// <exception cref="System.IO.IOException"></exception>
			public GroupComparator(Sort groupSort)
			{
				SortField[] sortFields = groupSort.GetSort();
				comparators = new FieldComparator<object>[sortFields.Length];
				reversed = new int[sortFields.Length];
				for (int compIDX = 0; compIDX < sortFields.Length; compIDX++)
				{
					SortField sortField = sortFields[compIDX];
					comparators[compIDX] = sortField.GetComparator(1, compIDX);
					reversed[compIDX] = sortField.GetReverse() ? -1 : 1;
				}
			}

			public virtual int Compare(SearchGroup.MergedGroup<T> group, SearchGroup.MergedGroup
				<T> other)
			{
				if (group == other)
				{
					return 0;
				}
				//System.out.println("compare group=" + group + " other=" + other);
				object[] groupValues = group.topValues;
				object[] otherValues = other.topValues;
				//System.out.println("  groupValues=" + groupValues + " otherValues=" + otherValues);
				for (int compIDX = 0; compIDX < comparators.Length; compIDX++)
				{
					int c = reversed[compIDX] * comparators[compIDX].CompareValues(groupValues[compIDX
						], otherValues[compIDX]);
					if (c != 0)
					{
						return c;
					}
				}
				// Tie break by min shard index:
				return group.minShardIndex != other.minShardIndex.minShardIndex - other.minShardIndex;
			}
		}

		private class GroupMerger<T>
		{
			private readonly SearchGroup.GroupComparator<T> groupComp;

			private readonly NavigableSet<SearchGroup.MergedGroup<T>> queue;

			private readonly IDictionary<T, SearchGroup.MergedGroup<T>> groupsSeen;

			/// <exception cref="System.IO.IOException"></exception>
			public GroupMerger(Sort groupSort)
			{
				groupComp = new SearchGroup.GroupComparator<T>(groupSort);
				queue = new TreeSet<SearchGroup.MergedGroup<T>>(groupComp);
				groupsSeen = new Dictionary<T, SearchGroup.MergedGroup<T>>();
			}

			private void UpdateNextGroup(int topN, SearchGroup.ShardIter<T> shard)
			{
				while (shard.iter.HasNext())
				{
					SearchGroup<T> group = shard.Next();
					SearchGroup.MergedGroup<T> mergedGroup = groupsSeen.Get(group.groupValue);
					bool isNew = mergedGroup == null;
					//System.out.println("    next group=" + (group.groupValue == null ? "null" : ((BytesRef) group.groupValue).utf8ToString()) + " sort=" + Arrays.toString(group.sortValues));
					if (isNew)
					{
						// Start a new group:
						//System.out.println("      new");
						mergedGroup = new SearchGroup.MergedGroup<T>(group.groupValue);
						mergedGroup.minShardIndex = shard.shardIndex;
						group.sortValues != null.topValues = group.sortValues;
						groupsSeen.Put(group.groupValue, mergedGroup);
						mergedGroup.inQueue = true;
						queue.AddItem(mergedGroup);
					}
					else
					{
						if (mergedGroup.processed)
						{
							// This shard produced a group that we already
							// processed; move on to next group...
							continue;
						}
						else
						{
							//System.out.println("      old");
							bool competes = false;
							for (int compIDX = 0; compIDX < groupComp.comparators.Length; compIDX++)
							{
								int cmp = groupComp.reversed[compIDX] * groupComp.comparators[compIDX].CompareValues
									(group.sortValues[compIDX], mergedGroup.topValues[compIDX]);
								if (cmp < 0)
								{
									// Definitely competes
									competes = true;
									break;
								}
								else
								{
									if (cmp > 0)
									{
										// Definitely does not compete
										break;
									}
									else
									{
										if (compIDX == groupComp.comparators.Length - 1)
										{
											if (shard.shardIndex < mergedGroup.minShardIndex)
											{
												competes = true;
											}
										}
									}
								}
							}
							//System.out.println("      competes=" + competes);
							if (competes)
							{
								// Group's sort changed -- remove & re-insert
								if (mergedGroup.inQueue)
								{
									queue.Remove(mergedGroup);
								}
								mergedGroup.topValues = group.sortValues;
								mergedGroup.minShardIndex = shard.shardIndex;
								queue.AddItem(mergedGroup);
								mergedGroup.inQueue = true;
							}
						}
					}
					mergedGroup.shards.AddItem(shard);
					break;
				}
				// Prune un-competitive groups:
				while (queue.Count > topN)
				{
					SearchGroup.MergedGroup<T> group = queue.PollLast();
					//System.out.println("PRUNE: " + group);
					group.inQueue = false;
				}
			}

			public virtual ICollection<SearchGroup<T>> Merge(IList<ICollection<SearchGroup<T>
				>> shards, int offset, int topN)
			{
				int maxQueueSize = offset + topN;
				//System.out.println("merge");
				// Init queue:
				for (int shardIDX = 0; shardIDX < shards.Count; shardIDX++)
				{
					ICollection<SearchGroup<T>> shard = shards[shardIDX];
					if (!shard.IsEmpty())
					{
						//System.out.println("  insert shard=" + shardIDX);
						UpdateNextGroup(maxQueueSize, new SearchGroup.ShardIter<T>(shard, shardIDX));
					}
				}
				// Pull merged topN groups:
				IList<SearchGroup<T>> newTopGroups = new AList<SearchGroup<T>>();
				int count = 0;
				while (queue.Count != 0)
				{
					SearchGroup.MergedGroup<T> group = queue.PollFirst();
					group.processed = true;
					//System.out.println("  pop: shards=" + group.shards + " group=" + (group.groupValue == null ? "null" : (((BytesRef) group.groupValue).utf8ToString())) + " sortValues=" + Arrays.toString(group.topValues));
					if (count++ >= offset)
					{
						SearchGroup<T> newGroup = new SearchGroup<T>();
						newGroup.groupValue = group.groupValue;
						newGroup.sortValues = group.topValues;
						newTopGroups.AddItem(newGroup);
						if (newTopGroups.Count == topN)
						{
							break;
						}
					}
					//} else {
					// System.out.println("    skip < offset");
					// Advance all iters in this group:
					foreach (SearchGroup.ShardIter<T> shardIter in group.shards)
					{
						UpdateNextGroup(maxQueueSize, shardIter);
					}
				}
				if (newTopGroups.Count == 0)
				{
					return null;
				}
				else
				{
					return newTopGroups;
				}
			}
		}

		/// <summary>
		/// Merges multiple collections of top groups, for example
		/// obtained from separate index shards.
		/// </summary>
		/// <remarks>
		/// Merges multiple collections of top groups, for example
		/// obtained from separate index shards.  The provided
		/// groupSort must match how the groups were sorted, and
		/// the provided SearchGroups must have been computed
		/// with fillFields=true passed to
		/// <see cref="AbstractFirstPassGroupingCollector{GROUP_VALUE_TYPE}.GetTopGroups(int, bool)
		/// 	">AbstractFirstPassGroupingCollector&lt;GROUP_VALUE_TYPE&gt;.GetTopGroups(int, bool)
		/// 	</see>
		/// .
		/// <p>NOTE: this returns null if the topGroups is empty.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public static ICollection<SearchGroup<T>> Merge<T>(IList<ICollection<SearchGroup<
			T>>> topGroups, int offset, int topN, Sort groupSort)
		{
			if (topGroups.Count == 0)
			{
				return null;
			}
			else
			{
				return new SearchGroup.GroupMerger<T>(groupSort).Merge(topGroups, offset, topN);
			}
		}
	}
}
