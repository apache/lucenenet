/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using Lucene.Net.Search;
using Lucene.Net.Search.Grouping;
using Sharpen;

namespace Lucene.Net.Search.Grouping
{
	/// <summary>Represents result returned by a grouping search.</summary>
	/// <remarks>Represents result returned by a grouping search.</remarks>
	/// <lucene.experimental></lucene.experimental>
	public class TopGroups<GROUP_VALUE_TYPE>
	{
		/// <summary>Number of documents matching the search</summary>
		public readonly int totalHitCount;

		/// <summary>Number of documents grouped into the topN groups</summary>
		public readonly int totalGroupedHitCount;

		/// <summary>The total number of unique groups.</summary>
		/// <remarks>The total number of unique groups. If <code>null</code> this value is not computed.
		/// 	</remarks>
		public readonly int totalGroupCount;

		/// <summary>Group results in groupSort order</summary>
		public readonly GroupDocs<GROUP_VALUE_TYPE>[] groups;

		/// <summary>How groups are sorted against each other</summary>
		public readonly SortField[] groupSort;

		/// <summary>How docs are sorted within each group</summary>
		public readonly SortField[] withinGroupSort;

		/// <summary>
		/// Highest score across all hits, or
		/// <code>Float.NaN</code> if scores were not computed.
		/// </summary>
		/// <remarks>
		/// Highest score across all hits, or
		/// <code>Float.NaN</code> if scores were not computed.
		/// </remarks>
		public readonly float maxScore;

		public TopGroups(SortField[] groupSort, SortField[] withinGroupSort, int totalHitCount
			, int totalGroupedHitCount, GroupDocs<GROUP_VALUE_TYPE>[] groups, float maxScore
			)
		{
			this.groupSort = groupSort;
			this.withinGroupSort = withinGroupSort;
			this.totalHitCount = totalHitCount;
			this.totalGroupedHitCount = totalGroupedHitCount;
			this.groups = groups;
			this.totalGroupCount = null;
			this.maxScore = maxScore;
		}

		public TopGroups(Lucene.Net.Search.Grouping.TopGroups<GROUP_VALUE_TYPE> oldTopGroups
			, int totalGroupCount)
		{
			this.groupSort = oldTopGroups.groupSort;
			this.withinGroupSort = oldTopGroups.withinGroupSort;
			this.totalHitCount = oldTopGroups.totalHitCount;
			this.totalGroupedHitCount = oldTopGroups.totalGroupedHitCount;
			this.groups = oldTopGroups.groups;
			this.maxScore = oldTopGroups.maxScore;
			this.totalGroupCount = totalGroupCount;
		}

		/// <summary>How the GroupDocs score (if any) should be merged.</summary>
		/// <remarks>How the GroupDocs score (if any) should be merged.</remarks>
		public enum ScoreMergeMode
		{
			None,
			Total,
			Avg
		}

		/// <summary>
		/// Merges an array of TopGroups, for example obtained
		/// from the second-pass collector across multiple
		/// shards.
		/// </summary>
		/// <remarks>
		/// Merges an array of TopGroups, for example obtained
		/// from the second-pass collector across multiple
		/// shards.  Each TopGroups must have been sorted by the
		/// same groupSort and docSort, and the top groups passed
		/// to all second-pass collectors must be the same.
		/// <b>NOTE</b>: We can't always compute an exact totalGroupCount.
		/// Documents belonging to a group may occur on more than
		/// one shard and thus the merged totalGroupCount can be
		/// higher than the actual totalGroupCount. In this case the
		/// totalGroupCount represents a upper bound. If the documents
		/// of one group do only reside in one shard then the
		/// totalGroupCount is exact.
		/// <b>NOTE</b>: the topDocs in each GroupDocs is actually
		/// an instance of TopDocsAndShards
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public static Lucene.Net.Search.Grouping.TopGroups<T> Merge<T>(Lucene.Net.Search.Grouping.TopGroups
			<T>[] shardGroups, Sort groupSort, Sort docSort, int docOffset, int docTopN, TopGroups.ScoreMergeMode
			 scoreMergeMode)
		{
			//System.out.println("TopGroups.merge");
			if (shardGroups.Length == 0)
			{
				return null;
			}
			int totalHitCount = 0;
			int totalGroupedHitCount = 0;
			// Optionally merge the totalGroupCount.
			int totalGroupCount = null;
			int numGroups = shardGroups[0].groups.Length;
			foreach (Lucene.Net.Search.Grouping.TopGroups<T> shard in shardGroups)
			{
				if (numGroups != shard.groups.Length)
				{
					throw new ArgumentException("number of groups differs across shards; you must pass same top groups to all shards' second-pass collector"
						);
				}
				totalHitCount += shard.totalHitCount;
				totalGroupedHitCount += shard.totalGroupedHitCount;
				if (shard.totalGroupCount != null)
				{
					if (totalGroupCount == null)
					{
						totalGroupCount = 0;
					}
					totalGroupCount += shard.totalGroupCount;
				}
			}
			GroupDocs<T>[] mergedGroupDocs = new GroupDocs[numGroups];
			TopDocs[] shardTopDocs = new TopDocs[shardGroups.Length];
			float totalMaxScore = float.MinValue;
			for (int groupIDX = 0; groupIDX < numGroups; groupIDX++)
			{
				T groupValue = shardGroups[0].groups[groupIDX].groupValue;
				//System.out.println("  merge groupValue=" + groupValue + " sortValues=" + Arrays.toString(shardGroups[0].groups[groupIDX].groupSortValues));
				float maxScore = float.MinValue;
				int totalHits = 0;
				double scoreSum = 0.0;
				for (int shardIDX = 0; shardIDX < shardGroups.Length; shardIDX++)
				{
					//System.out.println("    shard=" + shardIDX);
					Lucene.Net.Search.Grouping.TopGroups<T> shard_1 = shardGroups[shardIDX];
					GroupDocs<object> shardGroupDocs = shard_1.groups[groupIDX];
					if (groupValue == null)
					{
						if (shardGroupDocs.groupValue != null)
						{
							throw new ArgumentException("group values differ across shards; you must pass same top groups to all shards' second-pass collector"
								);
						}
					}
					else
					{
						if (!groupValue.Equals(shardGroupDocs.groupValue))
						{
							throw new ArgumentException("group values differ across shards; you must pass same top groups to all shards' second-pass collector"
								);
						}
					}
					shardTopDocs[shardIDX] = new TopDocs(shardGroupDocs.totalHits, shardGroupDocs.scoreDocs
						, shardGroupDocs.maxScore);
					maxScore = Math.Max(maxScore, shardGroupDocs.maxScore);
					totalHits += shardGroupDocs.totalHits;
					scoreSum += shardGroupDocs.score;
				}
				TopDocs mergedTopDocs = TopDocs.Merge(docSort, docOffset + docTopN, shardTopDocs);
				// Slice;
				ScoreDoc[] mergedScoreDocs;
				if (docOffset == 0)
				{
					mergedScoreDocs = mergedTopDocs.scoreDocs;
				}
				else
				{
					if (docOffset >= mergedTopDocs.scoreDocs.Length)
					{
						mergedScoreDocs = new ScoreDoc[0];
					}
					else
					{
						mergedScoreDocs = new ScoreDoc[mergedTopDocs.scoreDocs.Length - docOffset];
						System.Array.Copy(mergedTopDocs.scoreDocs, docOffset, mergedScoreDocs, 0, mergedTopDocs
							.scoreDocs.Length - docOffset);
					}
				}
				float groupScore;
				switch (scoreMergeMode)
				{
					case TopGroups.ScoreMergeMode.None:
					{
						groupScore = float.NaN;
						break;
					}

					case TopGroups.ScoreMergeMode.Avg:
					{
						if (totalHits > 0)
						{
							groupScore = (float)(scoreSum / totalHits);
						}
						else
						{
							groupScore = float.NaN;
						}
						break;
					}

					case TopGroups.ScoreMergeMode.Total:
					{
						groupScore = (float)scoreSum;
						break;
					}

					default:
					{
						throw new ArgumentException("can't handle ScoreMergeMode " + scoreMergeMode);
					}
				}
				//System.out.println("SHARDS=" + Arrays.toString(mergedTopDocs.shardIndex));
				mergedGroupDocs[groupIDX] = new GroupDocs<T>(groupScore, maxScore, totalHits, mergedScoreDocs
					, groupValue, shardGroups[0].groups[groupIDX].groupSortValues);
				totalMaxScore = Math.Max(totalMaxScore, maxScore);
			}
			if (totalGroupCount != null)
			{
				Lucene.Net.Search.Grouping.TopGroups<T> result = new Lucene.Net.Search.Grouping.TopGroups
					<T>(groupSort.GetSort(), docSort == null ? null : docSort.GetSort(), totalHitCount
					, totalGroupedHitCount, mergedGroupDocs, totalMaxScore);
				return new Lucene.Net.Search.Grouping.TopGroups<T>(result, totalGroupCount
					);
			}
			else
			{
				return new Lucene.Net.Search.Grouping.TopGroups<T>(groupSort.GetSort(), docSort
					 == null ? null : docSort.GetSort(), totalHitCount, totalGroupedHitCount, mergedGroupDocs
					, totalMaxScore);
			}
		}
	}
}
