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
using Lucene.Net.Search.Join;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Search.Join
{
	/// <summary>
	/// Collects parent document hits for a Query containing one more more
	/// BlockJoinQuery clauses, sorted by the
	/// specified parent Sort.
	/// </summary>
	/// <remarks>
	/// Collects parent document hits for a Query containing one more more
	/// BlockJoinQuery clauses, sorted by the
	/// specified parent Sort.  Note that this cannot perform
	/// arbitrary joins; rather, it requires that all joined
	/// documents are indexed as a doc block (using
	/// <see cref="Lucene.Net.Index.IndexWriter.AddDocuments(Sharpen.Iterable{T})"
	/// 	>Lucene.Net.Index.IndexWriter.AddDocuments(Sharpen.Iterable&lt;T&gt;)</see>
	/// or
	/// <see cref="Lucene.Net.Index.IndexWriter.UpdateDocuments(Lucene.Net.Index.Term, Sharpen.Iterable{T})
	/// 	">Lucene.Net.Index.IndexWriter.UpdateDocuments(Lucene.Net.Index.Term, Sharpen.Iterable&lt;T&gt;)
	/// 	</see>
	/// ).  Ie, the join is computed
	/// at index time.
	/// <p>The parent Sort must only use
	/// fields from the parent documents; sorting by field in
	/// the child documents is not supported.</p>
	/// <p>You should only use this
	/// collector if one or more of the clauses in the query is
	/// a
	/// <see cref="ToParentBlockJoinQuery">ToParentBlockJoinQuery</see>
	/// .  This collector will find those query
	/// clauses and record the matching child documents for the
	/// top scoring parent documents.</p>
	/// <p>Multiple joins (star join) and nested joins and a mix
	/// of the two are allowed, as long as in all cases the
	/// documents corresponding to a single row of each joined
	/// parent table were indexed as a doc block.</p>
	/// <p>For the simple star join you can retrieve the
	/// <see cref="Lucene.Net.Search.Grouping.TopGroups{GROUP_VALUE_TYPE}">Lucene.Net.Search.Grouping.TopGroups&lt;GROUP_VALUE_TYPE&gt;
	/// 	</see>
	/// instance containing each
	/// <see cref="ToParentBlockJoinQuery">ToParentBlockJoinQuery</see>
	/// 's
	/// matching child documents for the top parent groups,
	/// using
	/// <see cref="GetTopGroups(ToParentBlockJoinQuery, Lucene.Net.Search.Sort, int, int, int, bool)
	/// 	">GetTopGroups(ToParentBlockJoinQuery, Lucene.Net.Search.Sort, int, int, int, bool)
	/// 	</see>
	/// .  Ie,
	/// a single query, which will contain two or more
	/// <see cref="ToParentBlockJoinQuery">ToParentBlockJoinQuery</see>
	/// 's as clauses representing the star join,
	/// can then retrieve two or more
	/// <see cref="Lucene.Net.Search.Grouping.TopGroups{GROUP_VALUE_TYPE}">Lucene.Net.Search.Grouping.TopGroups&lt;GROUP_VALUE_TYPE&gt;
	/// 	</see>
	/// instances.</p>
	/// <p>For nested joins, the query will run correctly (ie,
	/// match the right parent and child documents), however,
	/// because TopGroups is currently unable to support nesting
	/// (each group is not able to hold another TopGroups), you
	/// are only able to retrieve the TopGroups of the first
	/// join.  The TopGroups of the nested joins will not be
	/// correct.
	/// See
	/// <see cref="Lucene.Net.Search.Join">Lucene.Net.Search.Join</see>
	/// for a code
	/// sample.
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	public class ToParentBlockJoinCollector : Collector
	{
		private readonly Sort sort;

		private readonly IDictionary<Query, int> joinQueryID = new Dictionary<Query, int>
			();

		private readonly int numParentHits;

		private readonly FieldValueHitQueue<ToParentBlockJoinCollector.OneGroup> queue;

		private readonly FieldComparator[] comparators;

		private readonly int[] reverseMul;

		private readonly int compEnd;

		private readonly bool trackMaxScore;

		private readonly bool trackScores;

		private int docBase;

		private ToParentBlockJoinQuery.BlockJoinScorer[] joinScorers = new ToParentBlockJoinQuery.BlockJoinScorer
			[0];

		private AtomicReaderContext currentReaderContext;

		private Scorer scorer;

		private bool queueFull;

		private ToParentBlockJoinCollector.OneGroup bottom;

		private int totalHitCount;

		private float maxScore = float.NaN;

		/// <summary>Creates a ToParentBlockJoinCollector.</summary>
		/// <remarks>
		/// Creates a ToParentBlockJoinCollector.  The provided sort must
		/// not be null.  If you pass true trackScores, all
		/// ToParentBlockQuery instances must not use
		/// ScoreMode.None.
		/// </remarks>
		/// <exception cref="System.IO.IOException"></exception>
		public ToParentBlockJoinCollector(Sort sort, int numParentHits, bool trackScores, 
			bool trackMaxScore)
		{
			// javadocs
			// Maps each BlockJoinQuery instance to its "slot" in
			// joinScorers and in OneGroup's cached doc/scores/count:
			// TODO: allow null sort to be specialized to relevance
			// only collector
			this.sort = sort;
			this.trackMaxScore = trackMaxScore;
			if (trackMaxScore)
			{
				maxScore = float.MinValue;
			}
			//System.out.println("numParentHits=" + numParentHits);
			this.trackScores = trackScores;
			this.numParentHits = numParentHits;
			queue = FieldValueHitQueue.Create(sort.GetSort(), numParentHits);
			comparators = queue.GetComparators();
			reverseMul = queue.GetReverseMul();
			compEnd = comparators.Length - 1;
		}

		private sealed class OneGroup : FieldValueHitQueue.Entry
		{
			public OneGroup(int comparatorSlot, int parentDoc, float parentScore, int numJoins
				, bool doScores) : base(comparatorSlot, parentDoc, parentScore)
			{
				//System.out.println("make OneGroup parentDoc=" + parentDoc);
				docs = new int[numJoins][];
				for (int joinID = 0; joinID < numJoins; joinID++)
				{
					docs[joinID] = new int[5];
				}
				if (doScores)
				{
					scores = new float[numJoins][];
					for (int joinID_1 = 0; joinID_1 < numJoins; joinID_1++)
					{
						scores[joinID_1] = new float[5];
					}
				}
				counts = new int[numJoins];
			}

			internal AtomicReaderContext readerContext;

			internal int[][] docs;

			internal float[][] scores;

			internal int[] counts;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Collect(int parentDoc)
		{
			//System.out.println("\nC parentDoc=" + parentDoc);
			totalHitCount++;
			float score = float.NaN;
			if (trackMaxScore)
			{
				score = scorer.Score();
				maxScore = Math.Max(maxScore, score);
			}
			// TODO: we could sweep all joinScorers here and
			// aggregate total child hit count, so we can fill this
			// in getTopGroups (we wire it to 0 now)
			if (queueFull)
			{
				//System.out.println("  queueFull");
				// Fastmatch: return if this hit is not competitive
				for (int i = 0; ; i++)
				{
					int c = reverseMul[i] * comparators[i].CompareBottom(parentDoc);
					if (c < 0)
					{
						// Definitely not competitive.
						//System.out.println("    skip");
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
							if (i == compEnd)
							{
								// Here c=0. If we're at the last comparator, this doc is not
								// competitive, since docs are visited in doc Id order, which means
								// this doc cannot compete with any other document in the queue.
								//System.out.println("    skip");
								return;
							}
						}
					}
				}
				//System.out.println("    competes!  doc=" + (docBase + parentDoc));
				// This hit is competitive - replace bottom element in queue & adjustTop
				for (int i_1 = 0; i_1 < comparators.Length; i_1++)
				{
					comparators[i_1].Copy(bottom.slot, parentDoc);
				}
				if (!trackMaxScore && trackScores)
				{
					score = scorer.Score();
				}
				bottom.doc = docBase + parentDoc;
				bottom.readerContext = currentReaderContext;
				bottom.score = score;
				CopyGroups(bottom);
				bottom = queue.UpdateTop();
				for (int i_2 = 0; i_2 < comparators.Length; i_2++)
				{
					comparators[i_2].SetBottom(bottom.slot);
				}
			}
			else
			{
				// Startup transient: queue is not yet full:
				int comparatorSlot = totalHitCount - 1;
				// Copy hit into queue
				for (int i = 0; i < comparators.Length; i++)
				{
					comparators[i].Copy(comparatorSlot, parentDoc);
				}
				//System.out.println("  startup: new OG doc=" + (docBase+parentDoc));
				if (!trackMaxScore && trackScores)
				{
					score = scorer.Score();
				}
				ToParentBlockJoinCollector.OneGroup og = new ToParentBlockJoinCollector.OneGroup(
					comparatorSlot, docBase + parentDoc, score, joinScorers.Length, trackScores);
				og.readerContext = currentReaderContext;
				CopyGroups(og);
				bottom = queue.Add(og);
				queueFull = totalHitCount == numParentHits;
				if (queueFull)
				{
					// End of startup transient: queue just filled up:
					for (int i_1 = 0; i_1 < comparators.Length; i_1++)
					{
						comparators[i_1].SetBottom(bottom.slot);
					}
				}
			}
		}

		// Pulls out child doc and scores for all join queries:
		private void CopyGroups(ToParentBlockJoinCollector.OneGroup og)
		{
			// While rare, it's possible top arrays could be too
			// short if join query had null scorer on first
			// segment(s) but then became non-null on later segments
			int numSubScorers = joinScorers.Length;
			if (og.docs.Length < numSubScorers)
			{
				// While rare, this could happen if join query had
				// null scorer on first segment(s) but then became
				// non-null on later segments
				og.docs = ArrayUtil.Grow(og.docs);
			}
			if (og.counts.Length < numSubScorers)
			{
				og.counts = ArrayUtil.Grow(og.counts);
			}
			if (trackScores && og.scores.Length < numSubScorers)
			{
				og.scores = ArrayUtil.Grow(og.scores);
			}
			//System.out.println("\ncopyGroups parentDoc=" + og.doc);
			for (int scorerIDX = 0; scorerIDX < numSubScorers; scorerIDX++)
			{
				ToParentBlockJoinQuery.BlockJoinScorer joinScorer = joinScorers[scorerIDX];
				//System.out.println("  scorer=" + joinScorer);
				if (joinScorer != null && docBase + joinScorer.GetParentDoc() == og.doc)
				{
					og.counts[scorerIDX] = joinScorer.GetChildCount();
					//System.out.println("    count=" + og.counts[scorerIDX]);
					og.docs[scorerIDX] = joinScorer.SwapChildDocs(og.docs[scorerIDX]);
					//HM:revisit
					//assert og.docs[scorerIDX].length >= og.counts[scorerIDX]: "length=" + og.docs[scorerIDX].length + " vs count=" + og.counts[scorerIDX];
					//System.out.println("    len=" + og.docs[scorerIDX].length);
					if (trackScores)
					{
						//System.out.println("    copy scores");
						og.scores[scorerIDX] = joinScorer.SwapChildScores(og.scores[scorerIDX]);
					}
				}
				else
				{
					//HM:assert
					//assert og.scores[scorerIDX].length >= og.counts[scorerIDX]: "length=" + og.scores[scorerIDX].length + " vs count=" + og.counts[scorerIDX];
					og.counts[scorerIDX] = 0;
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void SetNextReader(AtomicReaderContext context)
		{
			currentReaderContext = context;
			docBase = context.docBase;
			for (int compIDX = 0; compIDX < comparators.Length; compIDX++)
			{
				queue.SetComparator(compIDX, comparators[compIDX].SetNextReader(context));
			}
		}

		public override bool AcceptsDocsOutOfOrder()
		{
			return false;
		}

		private void Enroll(ToParentBlockJoinQuery query, ToParentBlockJoinQuery.BlockJoinScorer
			 scorer)
		{
			scorer.TrackPendingChildHits();
			int slot = joinQueryID.Get(query);
			if (slot == null)
			{
				joinQueryID.Put(query, joinScorers.Length);
				//System.out.println("found JQ: " + query + " slot=" + joinScorers.length);
				ToParentBlockJoinQuery.BlockJoinScorer[] newArray = new ToParentBlockJoinQuery.BlockJoinScorer
					[1 + joinScorers.Length];
				System.Array.Copy(joinScorers, 0, newArray, 0, joinScorers.Length);
				joinScorers = newArray;
				joinScorers[joinScorers.Length - 1] = scorer;
			}
			else
			{
				joinScorers[slot] = scorer;
			}
		}

		public override void SetScorer(Scorer scorer)
		{
			//System.out.println("C.setScorer scorer=" + scorer);
			// Since we invoke .score(), and the comparators likely
			// do as well, cache it so it's only "really" computed
			// once:
			this.scorer = new ScoreCachingWrappingScorer(scorer);
			for (int compIDX = 0; compIDX < comparators.Length; compIDX++)
			{
				comparators[compIDX].SetScorer(this.scorer);
			}
			Arrays.Fill(joinScorers, null);
			Queue<Scorer> queue = new List<Scorer>();
			//System.out.println("\nqueue: add top scorer=" + scorer);
			queue.AddItem(scorer);
			while ((scorer = queue.Poll()) != null)
			{
				//System.out.println("  poll: " + scorer + "; " + scorer.getWeight().getQuery());
				if (scorer is ToParentBlockJoinQuery.BlockJoinScorer)
				{
					Enroll((ToParentBlockJoinQuery)scorer.GetWeight().GetQuery(), (ToParentBlockJoinQuery.BlockJoinScorer
						)scorer);
				}
				foreach (Scorer.ChildScorer sub in scorer.GetChildren())
				{
					//System.out.println("  add sub: " + sub.child + "; " + sub.child.getWeight().getQuery());
					queue.AddItem(sub.child);
				}
			}
		}

		private ToParentBlockJoinCollector.OneGroup[] sortedGroups;

		private void SortQueue()
		{
			sortedGroups = new ToParentBlockJoinCollector.OneGroup[queue.Size()];
			for (int downTo = queue.Size() - 1; downTo >= 0; downTo--)
			{
				sortedGroups[downTo] = queue.Pop();
			}
		}

		/// <summary>
		/// Returns the TopGroups for the specified
		/// BlockJoinQuery.
		/// </summary>
		/// <remarks>
		/// Returns the TopGroups for the specified
		/// BlockJoinQuery. The groupValue of each GroupDocs will
		/// be the parent docID for that group.
		/// The number of documents within each group is calculated as minimum of <code>maxDocsPerGroup</code>
		/// and number of matched child documents for that group.
		/// Returns null if no groups matched.
		/// </remarks>
		/// <param name="query">Search query</param>
		/// <param name="withinGroupSort">Sort criteria within groups</param>
		/// <param name="offset">Parent docs offset</param>
		/// <param name="maxDocsPerGroup">Upper bound of documents per group number</param>
		/// <param name="withinGroupOffset">Offset within each group of child docs</param>
		/// <param name="fillSortFields">Specifies whether to add sort fields or not</param>
		/// <returns>TopGroups for specified query</returns>
		/// <exception cref="System.IO.IOException">if there is a low-level I/O error</exception>
		public virtual TopGroups<int> GetTopGroups(ToParentBlockJoinQuery query, Sort withinGroupSort
			, int offset, int maxDocsPerGroup, int withinGroupOffset, bool fillSortFields)
		{
			int _slot = joinQueryID.Get(query);
			if (_slot == null && totalHitCount == 0)
			{
				return null;
			}
			if (sortedGroups == null)
			{
				if (offset >= queue.Size())
				{
					return null;
				}
				SortQueue();
			}
			else
			{
				if (offset > sortedGroups.Length)
				{
					return null;
				}
			}
			return AccumulateGroups(_slot == null ? -1 : _slot, offset, maxDocsPerGroup, withinGroupOffset
				, withinGroupSort, fillSortFields);
		}

		/// <summary>Accumulates groups for the BlockJoinQuery specified by its slot.</summary>
		/// <remarks>Accumulates groups for the BlockJoinQuery specified by its slot.</remarks>
		/// <param name="slot">Search query's slot</param>
		/// <param name="offset">Parent docs offset</param>
		/// <param name="maxDocsPerGroup">Upper bound of documents per group number</param>
		/// <param name="withinGroupOffset">Offset within each group of child docs</param>
		/// <param name="withinGroupSort">Sort criteria within groups</param>
		/// <param name="fillSortFields">Specifies whether to add sort fields or not</param>
		/// <returns>TopGroups for the query specified by slot</returns>
		/// <exception cref="System.IO.IOException">if there is a low-level I/O error</exception>
		private TopGroups<int> AccumulateGroups(int slot, int offset, int maxDocsPerGroup
			, int withinGroupOffset, Sort withinGroupSort, bool fillSortFields)
		{
			GroupDocs<int>[] groups = new GroupDocs[sortedGroups.Length - offset];
			FakeScorer fakeScorer = new FakeScorer();
			int totalGroupedHitCount = 0;
			//System.out.println("slot=" + slot);
			for (int groupIDX = offset; groupIDX < sortedGroups.Length; groupIDX++)
			{
				ToParentBlockJoinCollector.OneGroup og = sortedGroups[groupIDX];
				int numChildDocs;
				if (slot == -1 || slot >= og.counts.Length)
				{
					numChildDocs = 0;
				}
				else
				{
					numChildDocs = og.counts[slot];
				}
				// Number of documents in group should be bounded to prevent redundant memory allocation
				int numDocsInGroup = Math.Max(1, Math.Min(numChildDocs, maxDocsPerGroup));
				//System.out.println("parent doc=" + og.doc + " numChildDocs=" + numChildDocs + " maxDocsPG=" + maxDocsPerGroup);
				// At this point we hold all docs w/ in each group,
				// unsorted; we now sort them:
				TopDocsCollector<object> collector;
				if (withinGroupSort == null)
				{
					//System.out.println("sort by score");
					// Sort by score
					if (!trackScores)
					{
						throw new ArgumentException("cannot sort by relevance within group: trackScores=false"
							);
					}
					collector = TopScoreDocCollector.Create(numDocsInGroup, true);
				}
				else
				{
					// Sort by fields
					collector = TopFieldCollector.Create(withinGroupSort, numDocsInGroup, fillSortFields
						, trackScores, trackMaxScore, true);
				}
				collector.SetScorer(fakeScorer);
				collector.SetNextReader(og.readerContext);
				for (int docIDX = 0; docIDX < numChildDocs; docIDX++)
				{
					//System.out.println("docIDX=" + docIDX + " vs " + og.docs[slot].length);
					int doc = og.docs[slot][docIDX];
					fakeScorer.doc = doc;
					if (trackScores)
					{
						fakeScorer.score = og.scores[slot][docIDX];
					}
					collector.Collect(doc);
				}
				totalGroupedHitCount += numChildDocs;
				object[] groupSortValues;
				if (fillSortFields)
				{
					groupSortValues = new object[comparators.Length];
					for (int sortFieldIDX = 0; sortFieldIDX < comparators.Length; sortFieldIDX++)
					{
						groupSortValues[sortFieldIDX] = comparators[sortFieldIDX].Value(og.slot);
					}
				}
				else
				{
					groupSortValues = null;
				}
				TopDocs topDocs = collector.TopDocs(withinGroupOffset, numDocsInGroup);
				groups[groupIDX - offset] = new GroupDocs<int>(og.score, topDocs.GetMaxScore(), numChildDocs
					, topDocs.scoreDocs, og.doc, groupSortValues);
			}
			return new TopGroups<int>(new TopGroups<int>(sort.GetSort(), withinGroupSort == null
				 ? null : withinGroupSort.GetSort(), 0, totalGroupedHitCount, groups, maxScore), 
				totalHitCount);
		}

		/// <summary>Returns the TopGroups for the specified BlockJoinQuery.</summary>
		/// <remarks>
		/// Returns the TopGroups for the specified BlockJoinQuery.
		/// The groupValue of each GroupDocs will be the parent docID for that group.
		/// The number of documents within each group
		/// equals to the total number of matched child documents for that group.
		/// Returns null if no groups matched.
		/// </remarks>
		/// <param name="query">Search query</param>
		/// <param name="withinGroupSort">Sort criteria within groups</param>
		/// <param name="offset">Parent docs offset</param>
		/// <param name="withinGroupOffset">Offset within each group of child docs</param>
		/// <param name="fillSortFields">Specifies whether to add sort fields or not</param>
		/// <returns>TopGroups for specified query</returns>
		/// <exception cref="System.IO.IOException">if there is a low-level I/O error</exception>
		public virtual TopGroups<int> GetTopGroupsWithAllChildDocs(ToParentBlockJoinQuery
			 query, Sort withinGroupSort, int offset, int withinGroupOffset, bool fillSortFields
			)
		{
			return GetTopGroups(query, withinGroupSort, offset, int.MaxValue, withinGroupOffset
				, fillSortFields);
		}

		/// <summary>
		/// Returns the highest score across all collected parent hits, as long as
		/// <code>trackMaxScores=true</code> was passed
		/// <see cref="ToParentBlockJoinCollector(Lucene.Net.Search.Sort, int, bool, bool)
		/// 	">
		/// on
		/// construction
		/// </see>
		/// . Else, this returns <code>Float.NaN</code>
		/// </summary>
		public virtual float GetMaxScore()
		{
			return maxScore;
		}
	}
}
