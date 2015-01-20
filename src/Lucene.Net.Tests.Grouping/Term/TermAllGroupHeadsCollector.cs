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
	/// A base implementation of
	/// <see cref="Org.Apache.Lucene.Search.Grouping.AbstractAllGroupHeadsCollector{GH}">Org.Apache.Lucene.Search.Grouping.AbstractAllGroupHeadsCollector&lt;GH&gt;
	/// 	</see>
	/// for retrieving the most relevant groups when grouping
	/// on a string based group field. More specifically this all concrete implementations of this base implementation
	/// use
	/// <see cref="Org.Apache.Lucene.Index.SortedDocValues">Org.Apache.Lucene.Index.SortedDocValues
	/// 	</see>
	/// .
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public abstract class TermAllGroupHeadsCollector<GH> : AbstractAllGroupHeadsCollector
		<GH> where GH:AbstractAllGroupHeadsCollector.GroupHead<object>
	{
		private const int DEFAULT_INITIAL_SIZE = 128;

		internal readonly string groupField;

		internal readonly BytesRef scratchBytesRef = new BytesRef();

		internal SortedDocValues groupIndex;

		internal AtomicReaderContext readerContext;

		protected internal TermAllGroupHeadsCollector(string groupField, int numberOfSorts
			) : base(numberOfSorts)
		{
			this.groupField = groupField;
		}

		/// <summary>Creates an <code>AbstractAllGroupHeadsCollector</code> instance based on the supplied arguments.
		/// 	</summary>
		/// <remarks>
		/// Creates an <code>AbstractAllGroupHeadsCollector</code> instance based on the supplied arguments.
		/// This factory method decides with implementation is best suited.
		/// Delegates to
		/// <see cref="TermAllGroupHeadsCollector{GH}.Create(string, Org.Apache.Lucene.Search.Sort, int)
		/// 	">TermAllGroupHeadsCollector&lt;GH&gt;.Create(string, Org.Apache.Lucene.Search.Sort, int)
		/// 	</see>
		/// with an initialSize of 128.
		/// </remarks>
		/// <param name="groupField">The field to group by</param>
		/// <param name="sortWithinGroup">The sort within each group</param>
		/// <returns>an <code>AbstractAllGroupHeadsCollector</code> instance based on the supplied arguments
		/// 	</returns>
		public static AbstractAllGroupHeadsCollector<object> Create(string groupField, Sort
			 sortWithinGroup)
		{
			return Create(groupField, sortWithinGroup, DEFAULT_INITIAL_SIZE);
		}

		/// <summary>Creates an <code>AbstractAllGroupHeadsCollector</code> instance based on the supplied arguments.
		/// 	</summary>
		/// <remarks>
		/// Creates an <code>AbstractAllGroupHeadsCollector</code> instance based on the supplied arguments.
		/// This factory method decides with implementation is best suited.
		/// </remarks>
		/// <param name="groupField">The field to group by</param>
		/// <param name="sortWithinGroup">The sort within each group</param>
		/// <param name="initialSize">
		/// The initial allocation size of the internal int set and group list which should roughly match
		/// the total number of expected unique groups. Be aware that the heap usage is
		/// 4 bytes * initialSize.
		/// </param>
		/// <returns>an <code>AbstractAllGroupHeadsCollector</code> instance based on the supplied arguments
		/// 	</returns>
		public static AbstractAllGroupHeadsCollector<object> Create(string groupField, Sort
			 sortWithinGroup, int initialSize)
		{
			bool sortAllScore = true;
			bool sortAllFieldValue = true;
			foreach (SortField sortField in sortWithinGroup.GetSort())
			{
				if (sortField.GetType() == SortField.Type.SCORE)
				{
					sortAllFieldValue = false;
				}
				else
				{
					if (NeedGeneralImpl(sortField))
					{
						return new TermAllGroupHeadsCollector.GeneralAllGroupHeadsCollector(groupField, sortWithinGroup
							);
					}
					else
					{
						sortAllScore = false;
					}
				}
			}
			if (sortAllScore)
			{
				return new TermAllGroupHeadsCollector.ScoreAllGroupHeadsCollector(groupField, sortWithinGroup
					, initialSize);
			}
			else
			{
				if (sortAllFieldValue)
				{
					return new TermAllGroupHeadsCollector.OrdAllGroupHeadsCollector(groupField, sortWithinGroup
						, initialSize);
				}
				else
				{
					return new TermAllGroupHeadsCollector.OrdScoreAllGroupHeadsCollector(groupField, 
						sortWithinGroup, initialSize);
				}
			}
		}

		// Returns when a sort field needs the general impl.
		private static bool NeedGeneralImpl(SortField sortField)
		{
			SortField.Type sortType = sortField.GetType();
			// Note (MvG): We can also make an optimized impl when sorting is SortField.DOC
			return sortType != SortField.Type.STRING_VAL && sortType != SortField.Type.STRING
				 && sortType != SortField.Type.SCORE;
		}

		internal class GeneralAllGroupHeadsCollector : TermAllGroupHeadsCollector<TermAllGroupHeadsCollector.GeneralAllGroupHeadsCollector.GroupHead
			>
		{
			private readonly Sort sortWithinGroup;

			private readonly IDictionary<BytesRef, TermAllGroupHeadsCollector.GeneralAllGroupHeadsCollector.GroupHead
				> groups;

			private Scorer scorer;

			internal GeneralAllGroupHeadsCollector(string groupField, Sort sortWithinGroup) : 
				base(groupField, sortWithinGroup.GetSort().Length)
			{
				// A general impl that works for any group sort.
				this.sortWithinGroup = sortWithinGroup;
				groups = new Dictionary<BytesRef, TermAllGroupHeadsCollector.GeneralAllGroupHeadsCollector.GroupHead
					>();
				SortField[] sortFields = sortWithinGroup.GetSort();
				for (int i = 0; i < sortFields.Length; i++)
				{
					reversed[i] = sortFields[i].GetReverse() ? -1 : 1;
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			protected internal override void RetrieveGroupHeadAndAddIfNotExist(int doc)
			{
				int ord = groupIndex.GetOrd(doc);
				BytesRef groupValue;
				if (ord == -1)
				{
					groupValue = null;
				}
				else
				{
					groupIndex.LookupOrd(ord, scratchBytesRef);
					groupValue = scratchBytesRef;
				}
				TermAllGroupHeadsCollector.GeneralAllGroupHeadsCollector.GroupHead groupHead = groups
					.Get(groupValue);
				if (groupHead == null)
				{
					groupHead = new TermAllGroupHeadsCollector.GeneralAllGroupHeadsCollector.GroupHead
						(this, groupValue, sortWithinGroup, doc);
					groups.Put(groupValue == null ? null : BytesRef.DeepCopyOf(groupValue), groupHead
						);
					temporalResult.stop = true;
				}
				else
				{
					temporalResult.stop = false;
				}
				temporalResult.groupHead = groupHead;
			}

			protected internal override ICollection<TermAllGroupHeadsCollector.GeneralAllGroupHeadsCollector.GroupHead
				> GetCollectedGroupHeads()
			{
				return groups.Values;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void SetNextReader(AtomicReaderContext context)
			{
				this.readerContext = context;
				groupIndex = FieldCache.DEFAULT.GetTermsIndex(((AtomicReader)context.Reader()), groupField
					);
				foreach (TermAllGroupHeadsCollector.GeneralAllGroupHeadsCollector.GroupHead groupHead
					 in groups.Values)
				{
					for (int i = 0; i < groupHead.comparators.Length; i++)
					{
						groupHead.comparators[i] = groupHead.comparators[i].SetNextReader(context);
					}
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void SetScorer(Scorer scorer)
			{
				this.scorer = scorer;
				foreach (TermAllGroupHeadsCollector.GeneralAllGroupHeadsCollector.GroupHead groupHead
					 in groups.Values)
				{
					foreach (FieldComparator<object> comparator in groupHead.comparators)
					{
						comparator.SetScorer(scorer);
					}
				}
			}

			internal class GroupHead : AbstractAllGroupHeadsCollector.GroupHead<BytesRef>
			{
				internal readonly FieldComparator<object>[] comparators;

				/// <exception cref="System.IO.IOException"></exception>
				private GroupHead(GeneralAllGroupHeadsCollector _enclosing, BytesRef groupValue, 
					Sort sort, int doc) : base(groupValue, doc + this._enclosing.readerContext.docBase
					)
				{
					this._enclosing = _enclosing;
					SortField[] sortFields = sort.GetSort();
					this.comparators = new FieldComparator[sortFields.Length];
					for (int i = 0; i < sortFields.Length; i++)
					{
						this.comparators[i] = sortFields[i].GetComparator(1, i).SetNextReader(this._enclosing
							.readerContext);
						this.comparators[i].SetScorer(this._enclosing.scorer);
						this.comparators[i].Copy(0, doc);
						this.comparators[i].SetBottom(0);
					}
				}

				/// <exception cref="System.IO.IOException"></exception>
				protected internal override int Compare(int compIDX, int doc)
				{
					return this.comparators[compIDX].CompareBottom(doc);
				}

				/// <exception cref="System.IO.IOException"></exception>
				protected internal override void UpdateDocHead(int doc)
				{
					foreach (FieldComparator<object> comparator in this.comparators)
					{
						comparator.Copy(0, doc);
						comparator.SetBottom(0);
					}
					this.doc = doc + this._enclosing.readerContext.docBase;
				}

				private readonly GeneralAllGroupHeadsCollector _enclosing;
			}
		}

		internal class OrdScoreAllGroupHeadsCollector : TermAllGroupHeadsCollector<TermAllGroupHeadsCollector.OrdScoreAllGroupHeadsCollector.GroupHead
			>
		{
			private readonly SentinelIntSet ordSet;

			private readonly IList<TermAllGroupHeadsCollector.OrdScoreAllGroupHeadsCollector.GroupHead
				> collectedGroups;

			private readonly SortField[] fields;

			private SortedDocValues[] sortsIndex;

			private Scorer scorer;

			private TermAllGroupHeadsCollector.OrdScoreAllGroupHeadsCollector.GroupHead[] segmentGroupHeads;

			internal OrdScoreAllGroupHeadsCollector(string groupField, Sort sortWithinGroup, 
				int initialSize) : base(groupField, sortWithinGroup.GetSort().Length)
			{
				// AbstractAllGroupHeadsCollector optimized for ord fields and scores.
				ordSet = new SentinelIntSet(initialSize, -2);
				collectedGroups = new AList<TermAllGroupHeadsCollector.OrdScoreAllGroupHeadsCollector.GroupHead
					>(initialSize);
				SortField[] sortFields = sortWithinGroup.GetSort();
				fields = new SortField[sortFields.Length];
				sortsIndex = new SortedDocValues[sortFields.Length];
				for (int i = 0; i < sortFields.Length; i++)
				{
					reversed[i] = sortFields[i].GetReverse() ? -1 : 1;
					fields[i] = sortFields[i];
				}
			}

			protected internal override ICollection<TermAllGroupHeadsCollector.OrdScoreAllGroupHeadsCollector.GroupHead
				> GetCollectedGroupHeads()
			{
				return collectedGroups;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void SetScorer(Scorer scorer)
			{
				this.scorer = scorer;
			}

			/// <exception cref="System.IO.IOException"></exception>
			protected internal override void RetrieveGroupHeadAndAddIfNotExist(int doc)
			{
				int key = groupIndex.GetOrd(doc);
				TermAllGroupHeadsCollector.OrdScoreAllGroupHeadsCollector.GroupHead groupHead;
				if (!ordSet.Exists(key))
				{
					ordSet.Put(key);
					BytesRef term;
					if (key == -1)
					{
						term = null;
					}
					else
					{
						term = new BytesRef();
						groupIndex.LookupOrd(key, term);
					}
					groupHead = new TermAllGroupHeadsCollector.OrdScoreAllGroupHeadsCollector.GroupHead
						(this, doc, term);
					collectedGroups.AddItem(groupHead);
					segmentGroupHeads[key + 1] = groupHead;
					temporalResult.stop = true;
				}
				else
				{
					temporalResult.stop = false;
					groupHead = segmentGroupHeads[key + 1];
				}
				temporalResult.groupHead = groupHead;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void SetNextReader(AtomicReaderContext context)
			{
				this.readerContext = context;
				groupIndex = FieldCache.DEFAULT.GetTermsIndex(((AtomicReader)context.Reader()), groupField
					);
				for (int i = 0; i < fields.Length; i++)
				{
					if (fields[i].GetType() == SortField.Type.SCORE)
					{
						continue;
					}
					sortsIndex[i] = FieldCache.DEFAULT.GetTermsIndex(((AtomicReader)context.Reader())
						, fields[i].GetField());
				}
				// Clear ordSet and fill it with previous encountered groups that can occur in the current segment.
				ordSet.Clear();
				segmentGroupHeads = new TermAllGroupHeadsCollector.OrdScoreAllGroupHeadsCollector.GroupHead
					[groupIndex.GetValueCount() + 1];
				foreach (TermAllGroupHeadsCollector.OrdScoreAllGroupHeadsCollector.GroupHead collectedGroup
					 in collectedGroups)
				{
					int ord;
					if (collectedGroup.groupValue == null)
					{
						ord = -1;
					}
					else
					{
						ord = groupIndex.LookupTerm(collectedGroup.groupValue);
					}
					if (collectedGroup.groupValue == null || ord >= 0)
					{
						ordSet.Put(ord);
						segmentGroupHeads[ord + 1] = collectedGroup;
						for (int i_1 = 0; i_1 < sortsIndex.Length; i_1++)
						{
							if (fields[i_1].GetType() == SortField.Type.SCORE)
							{
								continue;
							}
							int sortOrd;
							if (collectedGroup.sortValues[i_1] == null)
							{
								sortOrd = -1;
							}
							else
							{
								sortOrd = sortsIndex[i_1].LookupTerm(collectedGroup.sortValues[i_1]);
							}
							collectedGroup.sortOrds[i_1] = sortOrd;
						}
					}
				}
			}

			internal class GroupHead : AbstractAllGroupHeadsCollector.GroupHead<BytesRef>
			{
				internal BytesRef[] sortValues;

				internal int[] sortOrds;

				internal float[] scores;

				/// <exception cref="System.IO.IOException"></exception>
				private GroupHead(OrdScoreAllGroupHeadsCollector _enclosing, int doc, BytesRef groupValue
					) : base(groupValue, doc + this._enclosing.readerContext.docBase)
				{
					this._enclosing = _enclosing;
					this.sortValues = new BytesRef[this._enclosing.sortsIndex.Length];
					this.sortOrds = new int[this._enclosing.sortsIndex.Length];
					this.scores = new float[this._enclosing.sortsIndex.Length];
					for (int i = 0; i < this._enclosing.sortsIndex.Length; i++)
					{
						if (this._enclosing.fields[i].GetType() == SortField.Type.SCORE)
						{
							this.scores[i] = this._enclosing.scorer.Score();
						}
						else
						{
							this.sortOrds[i] = this._enclosing.sortsIndex[i].GetOrd(doc);
							this.sortValues[i] = new BytesRef();
							if (this.sortOrds[i] != -1)
							{
								this._enclosing.sortsIndex[i].Get(doc, this.sortValues[i]);
							}
						}
					}
				}

				/// <exception cref="System.IO.IOException"></exception>
				protected internal override int Compare(int compIDX, int doc)
				{
					if (this._enclosing.fields[compIDX].GetType() == SortField.Type.SCORE)
					{
						float score = this._enclosing.scorer.Score();
						if (this.scores[compIDX] < score)
						{
							return 1;
						}
						else
						{
							if (this.scores[compIDX] > score)
							{
								return -1;
							}
						}
						return 0;
					}
					else
					{
						if (this.sortOrds[compIDX] < 0)
						{
							// The current segment doesn't contain the sort value we encountered before. Therefore the ord is negative.
							if (this._enclosing.sortsIndex[compIDX].GetOrd(doc) == -1)
							{
								this._enclosing.scratchBytesRef.length = 0;
							}
							else
							{
								this._enclosing.sortsIndex[compIDX].Get(doc, this._enclosing.scratchBytesRef);
							}
							return this.sortValues[compIDX].CompareTo(this._enclosing.scratchBytesRef);
						}
						else
						{
							return this.sortOrds[compIDX] - this._enclosing.sortsIndex[compIDX].GetOrd(doc);
						}
					}
				}

				/// <exception cref="System.IO.IOException"></exception>
				protected internal override void UpdateDocHead(int doc)
				{
					for (int i = 0; i < this._enclosing.sortsIndex.Length; i++)
					{
						if (this._enclosing.fields[i].GetType() == SortField.Type.SCORE)
						{
							this.scores[i] = this._enclosing.scorer.Score();
						}
						else
						{
							this.sortOrds[i] = this._enclosing.sortsIndex[i].GetOrd(doc);
							if (this.sortOrds[i] == -1)
							{
								this.sortValues[i].length = 0;
							}
							else
							{
								this._enclosing.sortsIndex[i].Get(doc, this.sortValues[i]);
							}
						}
					}
					this.doc = doc + this._enclosing.readerContext.docBase;
				}

				private readonly OrdScoreAllGroupHeadsCollector _enclosing;
			}
		}

		internal class OrdAllGroupHeadsCollector : TermAllGroupHeadsCollector<TermAllGroupHeadsCollector.OrdAllGroupHeadsCollector.GroupHead
			>
		{
			private readonly SentinelIntSet ordSet;

			private readonly IList<TermAllGroupHeadsCollector.OrdAllGroupHeadsCollector.GroupHead
				> collectedGroups;

			private readonly SortField[] fields;

			private SortedDocValues[] sortsIndex;

			private TermAllGroupHeadsCollector.OrdAllGroupHeadsCollector.GroupHead[] segmentGroupHeads;

			internal OrdAllGroupHeadsCollector(string groupField, Sort sortWithinGroup, int initialSize
				) : base(groupField, sortWithinGroup.GetSort().Length)
			{
				// AbstractAllGroupHeadsCollector optimized for ord fields.
				ordSet = new SentinelIntSet(initialSize, -2);
				collectedGroups = new AList<TermAllGroupHeadsCollector.OrdAllGroupHeadsCollector.GroupHead
					>(initialSize);
				SortField[] sortFields = sortWithinGroup.GetSort();
				fields = new SortField[sortFields.Length];
				sortsIndex = new SortedDocValues[sortFields.Length];
				for (int i = 0; i < sortFields.Length; i++)
				{
					reversed[i] = sortFields[i].GetReverse() ? -1 : 1;
					fields[i] = sortFields[i];
				}
			}

			protected internal override ICollection<TermAllGroupHeadsCollector.OrdAllGroupHeadsCollector.GroupHead
				> GetCollectedGroupHeads()
			{
				return collectedGroups;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void SetScorer(Scorer scorer)
			{
			}

			/// <exception cref="System.IO.IOException"></exception>
			protected internal override void RetrieveGroupHeadAndAddIfNotExist(int doc)
			{
				int key = groupIndex.GetOrd(doc);
				TermAllGroupHeadsCollector.OrdAllGroupHeadsCollector.GroupHead groupHead;
				if (!ordSet.Exists(key))
				{
					ordSet.Put(key);
					BytesRef term;
					if (key == -1)
					{
						term = null;
					}
					else
					{
						term = new BytesRef();
						groupIndex.LookupOrd(key, term);
					}
					groupHead = new TermAllGroupHeadsCollector.OrdAllGroupHeadsCollector.GroupHead(this
						, doc, term);
					collectedGroups.AddItem(groupHead);
					segmentGroupHeads[key + 1] = groupHead;
					temporalResult.stop = true;
				}
				else
				{
					temporalResult.stop = false;
					groupHead = segmentGroupHeads[key + 1];
				}
				temporalResult.groupHead = groupHead;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void SetNextReader(AtomicReaderContext context)
			{
				this.readerContext = context;
				groupIndex = FieldCache.DEFAULT.GetTermsIndex(((AtomicReader)context.Reader()), groupField
					);
				for (int i = 0; i < fields.Length; i++)
				{
					sortsIndex[i] = FieldCache.DEFAULT.GetTermsIndex(((AtomicReader)context.Reader())
						, fields[i].GetField());
				}
				// Clear ordSet and fill it with previous encountered groups that can occur in the current segment.
				ordSet.Clear();
				segmentGroupHeads = new TermAllGroupHeadsCollector.OrdAllGroupHeadsCollector.GroupHead
					[groupIndex.GetValueCount() + 1];
				foreach (TermAllGroupHeadsCollector.OrdAllGroupHeadsCollector.GroupHead collectedGroup
					 in collectedGroups)
				{
					int groupOrd;
					if (collectedGroup.groupValue == null)
					{
						groupOrd = -1;
					}
					else
					{
						groupOrd = groupIndex.LookupTerm(collectedGroup.groupValue);
					}
					if (collectedGroup.groupValue == null || groupOrd >= 0)
					{
						ordSet.Put(groupOrd);
						segmentGroupHeads[groupOrd + 1] = collectedGroup;
						for (int i_1 = 0; i_1 < sortsIndex.Length; i_1++)
						{
							int sortOrd;
							if (collectedGroup.sortOrds[i_1] == -1)
							{
								sortOrd = -1;
							}
							else
							{
								sortOrd = sortsIndex[i_1].LookupTerm(collectedGroup.sortValues[i_1]);
							}
							collectedGroup.sortOrds[i_1] = sortOrd;
						}
					}
				}
			}

			internal class GroupHead : AbstractAllGroupHeadsCollector.GroupHead<BytesRef>
			{
				internal BytesRef[] sortValues;

				internal int[] sortOrds;

				private GroupHead(OrdAllGroupHeadsCollector _enclosing, int doc, BytesRef groupValue
					) : base(groupValue, doc + this._enclosing.readerContext.docBase)
				{
					this._enclosing = _enclosing;
					this.sortValues = new BytesRef[this._enclosing.sortsIndex.Length];
					this.sortOrds = new int[this._enclosing.sortsIndex.Length];
					for (int i = 0; i < this._enclosing.sortsIndex.Length; i++)
					{
						this.sortOrds[i] = this._enclosing.sortsIndex[i].GetOrd(doc);
						this.sortValues[i] = new BytesRef();
						if (this.sortOrds[i] != -1)
						{
							this._enclosing.sortsIndex[i].Get(doc, this.sortValues[i]);
						}
					}
				}

				/// <exception cref="System.IO.IOException"></exception>
				protected internal override int Compare(int compIDX, int doc)
				{
					if (this.sortOrds[compIDX] < 0)
					{
						// The current segment doesn't contain the sort value we encountered before. Therefore the ord is negative.
						if (this._enclosing.sortsIndex[compIDX].GetOrd(doc) == -1)
						{
							this._enclosing.scratchBytesRef.length = 0;
						}
						else
						{
							this._enclosing.sortsIndex[compIDX].Get(doc, this._enclosing.scratchBytesRef);
						}
						return this.sortValues[compIDX].CompareTo(this._enclosing.scratchBytesRef);
					}
					else
					{
						return this.sortOrds[compIDX] - this._enclosing.sortsIndex[compIDX].GetOrd(doc);
					}
				}

				/// <exception cref="System.IO.IOException"></exception>
				protected internal override void UpdateDocHead(int doc)
				{
					for (int i = 0; i < this._enclosing.sortsIndex.Length; i++)
					{
						this.sortOrds[i] = this._enclosing.sortsIndex[i].GetOrd(doc);
						if (this.sortOrds[i] == -1)
						{
							this.sortValues[i].length = 0;
						}
						else
						{
							this._enclosing.sortsIndex[i].LookupOrd(this.sortOrds[i], this.sortValues[i]);
						}
					}
					this.doc = doc + this._enclosing.readerContext.docBase;
				}

				private readonly OrdAllGroupHeadsCollector _enclosing;
			}
		}

		internal class ScoreAllGroupHeadsCollector : TermAllGroupHeadsCollector<TermAllGroupHeadsCollector.ScoreAllGroupHeadsCollector.GroupHead
			>
		{
			private readonly SentinelIntSet ordSet;

			private readonly IList<TermAllGroupHeadsCollector.ScoreAllGroupHeadsCollector.GroupHead
				> collectedGroups;

			private readonly SortField[] fields;

			private Scorer scorer;

			private TermAllGroupHeadsCollector.ScoreAllGroupHeadsCollector.GroupHead[] segmentGroupHeads;

			internal ScoreAllGroupHeadsCollector(string groupField, Sort sortWithinGroup, int
				 initialSize) : base(groupField, sortWithinGroup.GetSort().Length)
			{
				// AbstractAllGroupHeadsCollector optimized for scores.
				ordSet = new SentinelIntSet(initialSize, -2);
				collectedGroups = new AList<TermAllGroupHeadsCollector.ScoreAllGroupHeadsCollector.GroupHead
					>(initialSize);
				SortField[] sortFields = sortWithinGroup.GetSort();
				fields = new SortField[sortFields.Length];
				for (int i = 0; i < sortFields.Length; i++)
				{
					reversed[i] = sortFields[i].GetReverse() ? -1 : 1;
					fields[i] = sortFields[i];
				}
			}

			protected internal override ICollection<TermAllGroupHeadsCollector.ScoreAllGroupHeadsCollector.GroupHead
				> GetCollectedGroupHeads()
			{
				return collectedGroups;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void SetScorer(Scorer scorer)
			{
				this.scorer = scorer;
			}

			/// <exception cref="System.IO.IOException"></exception>
			protected internal override void RetrieveGroupHeadAndAddIfNotExist(int doc)
			{
				int key = groupIndex.GetOrd(doc);
				TermAllGroupHeadsCollector.ScoreAllGroupHeadsCollector.GroupHead groupHead;
				if (!ordSet.Exists(key))
				{
					ordSet.Put(key);
					BytesRef term;
					if (key == -1)
					{
						term = null;
					}
					else
					{
						term = new BytesRef();
						groupIndex.LookupOrd(key, term);
					}
					groupHead = new TermAllGroupHeadsCollector.ScoreAllGroupHeadsCollector.GroupHead(
						this, doc, term);
					collectedGroups.AddItem(groupHead);
					segmentGroupHeads[key + 1] = groupHead;
					temporalResult.stop = true;
				}
				else
				{
					temporalResult.stop = false;
					groupHead = segmentGroupHeads[key + 1];
				}
				temporalResult.groupHead = groupHead;
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void SetNextReader(AtomicReaderContext context)
			{
				this.readerContext = context;
				groupIndex = FieldCache.DEFAULT.GetTermsIndex(((AtomicReader)context.Reader()), groupField
					);
				// Clear ordSet and fill it with previous encountered groups that can occur in the current segment.
				ordSet.Clear();
				segmentGroupHeads = new TermAllGroupHeadsCollector.ScoreAllGroupHeadsCollector.GroupHead
					[groupIndex.GetValueCount() + 1];
				foreach (TermAllGroupHeadsCollector.ScoreAllGroupHeadsCollector.GroupHead collectedGroup
					 in collectedGroups)
				{
					int ord;
					if (collectedGroup.groupValue == null)
					{
						ord = -1;
					}
					else
					{
						ord = groupIndex.LookupTerm(collectedGroup.groupValue);
					}
					if (collectedGroup.groupValue == null || ord >= 0)
					{
						ordSet.Put(ord);
						segmentGroupHeads[ord + 1] = collectedGroup;
					}
				}
			}

			internal class GroupHead : AbstractAllGroupHeadsCollector.GroupHead<BytesRef>
			{
				internal float[] scores;

				/// <exception cref="System.IO.IOException"></exception>
				private GroupHead(ScoreAllGroupHeadsCollector _enclosing, int doc, BytesRef groupValue
					) : base(groupValue, doc + this._enclosing.readerContext.docBase)
				{
					this._enclosing = _enclosing;
					this.scores = new float[this._enclosing.fields.Length];
					float score = this._enclosing.scorer.Score();
					for (int i = 0; i < this.scores.Length; i++)
					{
						this.scores[i] = score;
					}
				}

				/// <exception cref="System.IO.IOException"></exception>
				protected internal override int Compare(int compIDX, int doc)
				{
					float score = this._enclosing.scorer.Score();
					if (this.scores[compIDX] < score)
					{
						return 1;
					}
					else
					{
						if (this.scores[compIDX] > score)
						{
							return -1;
						}
					}
					return 0;
				}

				/// <exception cref="System.IO.IOException"></exception>
				protected internal override void UpdateDocHead(int doc)
				{
					float score = this._enclosing.scorer.Score();
					for (int i = 0; i < this.scores.Length; i++)
					{
						this.scores[i] = score;
					}
					this.doc = doc + this._enclosing.readerContext.docBase;
				}

				private readonly ScoreAllGroupHeadsCollector _enclosing;
			}
		}
	}
}
