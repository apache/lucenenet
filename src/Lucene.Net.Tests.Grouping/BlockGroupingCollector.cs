/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Search;
using Org.Apache.Lucene.Search.Grouping;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Org.Apache.Lucene.Search.Grouping
{
	/// <summary>
	/// BlockGroupingCollector performs grouping with a
	/// single pass collector, as long as you are grouping by a
	/// doc block field, ie all documents sharing a given group
	/// value were indexed as a doc block using the atomic
	/// <see cref="Org.Apache.Lucene.Index.IndexWriter.AddDocuments(Sharpen.Iterable{T})"
	/// 	>IndexWriter.addDocuments()</see>
	/// 
	/// or
	/// <see cref="Org.Apache.Lucene.Index.IndexWriter.UpdateDocuments(Org.Apache.Lucene.Index.Term, Sharpen.Iterable{T})
	/// 	">IndexWriter.updateDocuments()</see>
	/// 
	/// API.
	/// <p>This results in faster performance (~25% faster QPS)
	/// than the two-pass grouping collectors, with the tradeoff
	/// being that the documents in each group must always be
	/// indexed as a block.  This collector also fills in
	/// TopGroups.totalGroupCount without requiring the separate
	/// <see cref="Org.Apache.Lucene.Search.Grouping.Term.TermAllGroupsCollector">Org.Apache.Lucene.Search.Grouping.Term.TermAllGroupsCollector
	/// 	</see>
	/// .  However, this collector does
	/// not fill in the groupValue of each group; this field
	/// will always be null.
	/// <p><b>NOTE</b>: this collector makes no effort to verify
	/// the docs were in fact indexed as a block, so it's up to
	/// you to ensure this was the case.
	/// <p>See
	/// <see cref="Org.Apache.Lucene.Search.Grouping">Org.Apache.Lucene.Search.Grouping</see>
	/// for more
	/// details including a full code example.</p>
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public class BlockGroupingCollector : Collector
	{
		private int[] pendingSubDocs;

		private float[] pendingSubScores;

		private int subDocUpto;

		private readonly Sort groupSort;

		private readonly int topNGroups;

		private readonly Filter lastDocPerGroup;

		private readonly bool needsScores;

		private readonly FieldComparator<object>[] comparators;

		private readonly int[] reversed;

		private readonly int compIDXEnd;

		private int bottomSlot;

		private bool queueFull;

		private AtomicReaderContext currentReaderContext;

		private int topGroupDoc;

		private int totalHitCount;

		private int totalGroupCount;

		private int docBase;

		private int groupEndDocID;

		private DocIdSetIterator lastDocPerGroupBits;

		private Scorer scorer;

		private readonly BlockGroupingCollector.GroupQueue groupQueue;

		private bool groupCompetes;

		private sealed class FakeScorer : Scorer
		{
			internal float score;

			internal int doc;

			public FakeScorer() : base(null)
			{
			}

			// TODO: this sentence is too long for the class summary.
			// TODO: specialize into 2 classes, static "create" method:
			public override float Score()
			{
				return score;
			}

			public override int Freq()
			{
				throw new NotSupportedException();
			}

			// TODO: wtf does this class do?
			public override int DocID()
			{
				return doc;
			}

			public override int Advance(int target)
			{
				throw new NotSupportedException();
			}

			public override int NextDoc()
			{
				throw new NotSupportedException();
			}

			public override long Cost()
			{
				return 1;
			}

			public override Weight GetWeight()
			{
				throw new NotSupportedException();
			}

			public override ICollection<Scorer.ChildScorer> GetChildren()
			{
				throw new NotSupportedException();
			}
		}

		private sealed class OneGroup
		{
			internal AtomicReaderContext readerContext;

			internal int topGroupDoc;

			internal int[] docs;

			internal float[] scores;

			internal int count;

			internal int comparatorSlot;
			//int groupOrd;
		}

		private sealed class GroupQueue : PriorityQueue<BlockGroupingCollector.OneGroup>
		{
			public GroupQueue(BlockGroupingCollector _enclosing, int size) : base(size)
			{
				this._enclosing = _enclosing;
			}

			// Sorts by groupSort.  Not static -- uses comparators, reversed
			protected override bool LessThan(BlockGroupingCollector.OneGroup group1, BlockGroupingCollector.OneGroup
				 group2)
			{
				//System.out.println("    ltcheck");
				//HM:revisit
				int numComparators = this._enclosing.comparators.Length;
				for (int compIDX = 0; compIDX < numComparators; compIDX++)
				{
					int c = this._enclosing.reversed[compIDX] * this._enclosing.comparators[compIDX].
						Compare(group1.comparatorSlot, group2.comparatorSlot);
					if (c != 0)
					{
						// Short circuit
						return c > 0;
					}
				}
				// Break ties by docID; lower docID is always sorted first
				return group1.topGroupDoc > group2.topGroupDoc;
			}

			private readonly BlockGroupingCollector _enclosing;
		}

		// Called when we transition to another group; if the
		// group is competitive we insert into the group queue
		private void ProcessGroup()
		{
			totalGroupCount++;
			//System.out.println("    processGroup ord=" + lastGroupOrd + " competes=" + groupCompetes + " count=" + subDocUpto + " groupDoc=" + topGroupDoc);
			if (groupCompetes)
			{
				if (!queueFull)
				{
					// Startup transient: always add a new OneGroup
					BlockGroupingCollector.OneGroup og = new BlockGroupingCollector.OneGroup();
					og.count = subDocUpto;
					og.topGroupDoc = docBase + topGroupDoc;
					og.docs = pendingSubDocs;
					pendingSubDocs = new int[10];
					if (needsScores)
					{
						og.scores = pendingSubScores;
						pendingSubScores = new float[10];
					}
					og.readerContext = currentReaderContext;
					//og.groupOrd = lastGroupOrd;
					og.comparatorSlot = bottomSlot;
					BlockGroupingCollector.OneGroup bottomGroup = groupQueue.Add(og);
					//System.out.println("      ADD group=" + getGroupString(lastGroupOrd) + " newBottom=" + getGroupString(bottomGroup.groupOrd));
					queueFull = groupQueue.Size() == topNGroups;
					if (queueFull)
					{
						// Queue just became full; now set the real bottom
						// in the comparators:
						bottomSlot = bottomGroup.comparatorSlot;
						//System.out.println("    set bottom=" + bottomSlot);
						for (int i = 0; i < comparators.Length; i++)
						{
							comparators[i].SetBottom(bottomSlot);
						}
					}
					else
					{
						//System.out.println("     QUEUE FULL");
						// Queue not full yet -- just advance bottomSlot:
						bottomSlot = groupQueue.Size();
					}
				}
				else
				{
					// Replace bottom element in PQ and then updateTop
					BlockGroupingCollector.OneGroup og = groupQueue.Top();
					og != null.count = subDocUpto;
					og.topGroupDoc = docBase + topGroupDoc;
					// Swap pending docs
					int[] savDocs = og.docs;
					og.docs = pendingSubDocs;
					pendingSubDocs = savDocs;
					if (needsScores)
					{
						// Swap pending scores
						float[] savScores = og.scores;
						og.scores = pendingSubScores;
						pendingSubScores = savScores;
					}
					og.readerContext = currentReaderContext;
					//og.groupOrd = lastGroupOrd;
					bottomSlot = groupQueue.UpdateTop().comparatorSlot;
					//System.out.println("    set bottom=" + bottomSlot);
					for (int i = 0; i < comparators.Length; i++)
					{
						comparators[i].SetBottom(bottomSlot);
					}
				}
			}
			subDocUpto = 0;
		}

		/// <summary>Create the single pass collector.</summary>
		/// <remarks>Create the single pass collector.</remarks>
		/// <param name="groupSort">
		/// The
		/// <see cref="Org.Apache.Lucene.Search.Sort">Org.Apache.Lucene.Search.Sort</see>
		/// used to sort the
		/// groups.  The top sorted document within each group
		/// according to groupSort, determines how that group
		/// sorts against other groups.  This must be non-null,
		/// ie, if you want to groupSort by relevance use
		/// Sort.RELEVANCE.
		/// </param>
		/// <param name="topNGroups">How many top groups to keep.</param>
		/// <param name="needsScores">
		/// true if the collected documents
		/// require scores, either because relevance is included
		/// in the withinGroupSort or because you plan to pass true
		/// for either getSscores or getMaxScores to
		/// <see cref="GetTopGroups(Org.Apache.Lucene.Search.Sort, int, int, int, bool)">GetTopGroups(Org.Apache.Lucene.Search.Sort, int, int, int, bool)
		/// 	</see>
		/// </param>
		/// <param name="lastDocPerGroup">
		/// a
		/// <see cref="Org.Apache.Lucene.Search.Filter">Org.Apache.Lucene.Search.Filter</see>
		/// that marks the
		/// last document in each group.
		/// </param>
		/// <exception cref="System.IO.IOException"></exception>
		public BlockGroupingCollector(Sort groupSort, int topNGroups, bool needsScores, Filter
			 lastDocPerGroup)
		{
			if (topNGroups < 1)
			{
				throw new ArgumentException("topNGroups must be >= 1 (got " + topNGroups + ")");
			}
			groupQueue = new BlockGroupingCollector.GroupQueue(this, topNGroups);
			pendingSubDocs = new int[10];
			if (needsScores)
			{
				pendingSubScores = new float[10];
			}
			this.needsScores = needsScores;
			this.lastDocPerGroup = lastDocPerGroup;
			// TODO: allow null groupSort to mean "by relevance",
			// and specialize it?
			this.groupSort = groupSort;
			this.topNGroups = topNGroups;
			SortField[] sortFields = groupSort.GetSort();
			comparators = new FieldComparator<object>[sortFields.Length];
			compIDXEnd = comparators.Length - 1;
			reversed = new int[sortFields.Length];
			for (int i = 0; i < sortFields.Length; i++)
			{
				SortField sortField = sortFields[i];
				comparators[i] = sortField.GetComparator(topNGroups, i);
				reversed[i] = sortField.GetReverse() ? -1 : 1;
			}
		}

		// TODO: maybe allow no sort on retrieving groups?  app
		// may want to simply process docs in the group itself?
		// typically they will be presented as a "single" result
		// in the UI?
		/// <summary>Returns the grouped results.</summary>
		/// <remarks>
		/// Returns the grouped results.  Returns null if the
		/// number of groups collected is &lt;= groupOffset.
		/// <p><b>NOTE</b>: This collector is unable to compute
		/// the groupValue per group so it will always be null.
		/// This is normally not a problem, as you can obtain the
		/// value just like you obtain other values for each
		/// matching document (eg, via stored fields, via
		/// FieldCache, etc.)
		/// </remarks>
		/// <param name="withinGroupSort">
		/// The
		/// <see cref="Org.Apache.Lucene.Search.Sort">Org.Apache.Lucene.Search.Sort</see>
		/// used to sort
		/// documents within each group.  Passing null is
		/// allowed, to sort by relevance.
		/// </param>
		/// <param name="groupOffset">Which group to start from</param>
		/// <param name="withinGroupOffset">
		/// Which document to start from
		/// within each group
		/// </param>
		/// <param name="maxDocsPerGroup">
		/// How many top documents to keep
		/// within each group.
		/// </param>
		/// <param name="fillSortFields">
		/// If true then the Comparable
		/// values for the sort fields will be set
		/// </param>
		/// <exception cref="System.IO.IOException"></exception>
		public virtual TopGroups<object> GetTopGroups(Sort withinGroupSort, int groupOffset
			, int withinGroupOffset, int maxDocsPerGroup, bool fillSortFields)
		{
			//if (queueFull) {
			//System.out.println("getTopGroups groupOffset=" + groupOffset + " topNGroups=" + topNGroups);
			//}
			if (subDocUpto != 0)
			{
				ProcessGroup();
			}
			if (groupOffset >= groupQueue.Size())
			{
				return null;
			}
			int totalGroupedHitCount = 0;
			BlockGroupingCollector.FakeScorer fakeScorer = new BlockGroupingCollector.FakeScorer
				();
			float maxScore = float.MinValue;
			GroupDocs<object>[] groups = new GroupDocs[groupQueue.Size() - groupOffset];
			for (int downTo = groupQueue.Size() - groupOffset - 1; downTo >= 0; downTo--)
			{
				BlockGroupingCollector.OneGroup og = groupQueue.Pop();
				// At this point we hold all docs w/ in each group,
				// unsorted; we now sort them:
				TopDocsCollector<object> collector;
				if (withinGroupSort == null)
				{
					// Sort by score
					if (!needsScores)
					{
						throw new ArgumentException("cannot sort by relevance within group: needsScores=false"
							);
					}
					collector = TopScoreDocCollector.Create(maxDocsPerGroup, true);
				}
				else
				{
					// Sort by fields
					collector = TopFieldCollector.Create(withinGroupSort, maxDocsPerGroup, fillSortFields
						, needsScores, needsScores, true);
				}
				collector.SetScorer(fakeScorer);
				collector.SetNextReader(og.readerContext);
				for (int docIDX = 0; docIDX < og.count; docIDX++)
				{
					int doc = og.docs[docIDX];
					fakeScorer.doc = doc;
					if (needsScores)
					{
						fakeScorer.score = og.scores[docIDX];
					}
					collector.Collect(doc);
				}
				totalGroupedHitCount += og.count;
				object[] groupSortValues;
				if (fillSortFields)
				{
					groupSortValues = new Comparable<object>[comparators.Length];
					for (int sortFieldIDX = 0; sortFieldIDX < comparators.Length; sortFieldIDX++)
					{
						groupSortValues[sortFieldIDX] = comparators[sortFieldIDX].Value(og.comparatorSlot
							);
					}
				}
				else
				{
					groupSortValues = null;
				}
				TopDocs topDocs = collector.TopDocs(withinGroupOffset, maxDocsPerGroup);
				// TODO: we could aggregate scores across children
				// by Sum/Avg instead of passing NaN:
				groups[downTo] = new GroupDocs<object>(float.NaN, topDocs.GetMaxScore(), og.count
					, topDocs.scoreDocs, null, groupSortValues);
				maxScore = Math.Max(maxScore, topDocs.GetMaxScore());
			}
			return new TopGroups<object>(new TopGroups<object>(groupSort.GetSort(), withinGroupSort
				 == null ? null : withinGroupSort.GetSort(), totalHitCount, totalGroupedHitCount
				, groups, maxScore), totalGroupCount);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void SetScorer(Scorer scorer)
		{
			this.scorer = scorer;
			foreach (FieldComparator<object> comparator in comparators)
			{
				comparator.SetScorer(scorer);
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Collect(int doc)
		{
			// System.out.println("C " + doc);
			if (doc > groupEndDocID)
			{
				// Group changed
				if (subDocUpto != 0)
				{
					ProcessGroup();
				}
				groupEndDocID = lastDocPerGroupBits.Advance(doc);
				//System.out.println("  adv " + groupEndDocID + " " + lastDocPerGroupBits);
				subDocUpto = 0;
				groupCompetes = !queueFull;
			}
			totalHitCount++;
			// Always cache doc/score within this group:
			if (subDocUpto == pendingSubDocs.Length)
			{
				pendingSubDocs = ArrayUtil.Grow(pendingSubDocs);
			}
			pendingSubDocs[subDocUpto] = doc;
			if (needsScores)
			{
				if (subDocUpto == pendingSubScores.Length)
				{
					pendingSubScores = ArrayUtil.Grow(pendingSubScores);
				}
				pendingSubScores[subDocUpto] = scorer.Score();
			}
			subDocUpto++;
			if (groupCompetes)
			{
				if (subDocUpto == 1)
				{
					//System.out.println("    init copy to bottomSlot=" + bottomSlot);
					foreach (FieldComparator<object> fc in !queueFull)
					{
						fc.Copy(bottomSlot, doc);
						fc.SetBottom(bottomSlot);
					}
					topGroupDoc = doc;
				}
				else
				{
					// Compare to bottomSlot
					for (int compIDX = 0; ; compIDX++)
					{
						int c = reversed[compIDX] * comparators[compIDX].CompareBottom(doc);
						if (c < 0)
						{
							// Definitely not competitive -- done
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
									// Ties with bottom, except we know this docID is
									// > docID in the queue (docs are visited in
									// order), so not competitive:
									return;
								}
							}
						}
					}
					//System.out.println("       best w/in group!");
					foreach (FieldComparator<object> fc in comparators)
					{
						fc.Copy(bottomSlot, doc);
						// Necessary because some comparators cache
						// details of bottom slot; this forces them to
						// re-cache:
						fc.SetBottom(bottomSlot);
					}
					topGroupDoc = doc;
				}
			}
			else
			{
				// We're not sure this group will make it into the
				// queue yet
				for (int compIDX = 0; ; compIDX++)
				{
					int c = reversed[compIDX] * comparators[compIDX].CompareBottom(doc);
					if (c < 0)
					{
						// Definitely not competitive -- done
						//System.out.println("    doc doesn't compete w/ top groups");
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
								// Ties with bottom, except we know this docID is
								// > docID in the queue (docs are visited in
								// order), so not competitive:
								//System.out.println("    doc doesn't compete w/ top groups");
								return;
							}
						}
					}
				}
				groupCompetes = true;
				foreach (FieldComparator<object> fc in comparators)
				{
					fc.Copy(bottomSlot, doc);
					// Necessary because some comparators cache
					// details of bottom slot; this forces them to
					// re-cache:
					fc.SetBottom(bottomSlot);
				}
				topGroupDoc = doc;
			}
		}

		//System.out.println("        doc competes w/ top groups");
		public override bool AcceptsDocsOutOfOrder()
		{
			return false;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void SetNextReader(AtomicReaderContext readerContext)
		{
			if (subDocUpto != 0)
			{
				ProcessGroup();
			}
			subDocUpto = 0;
			docBase = readerContext.docBase;
			//System.out.println("setNextReader base=" + docBase + " r=" + readerContext.reader);
			lastDocPerGroupBits = lastDocPerGroup.GetDocIdSet(readerContext, ((AtomicReader)readerContext
				.Reader()).GetLiveDocs()).Iterator();
			groupEndDocID = -1;
			currentReaderContext = readerContext;
			for (int i = 0; i < comparators.Length; i++)
			{
				comparators[i] = comparators[i].SetNextReader(readerContext);
			}
		}
	}
}
