/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.IO;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Search;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Org.Apache.Lucene.Index.Sorter
{
	/// <summary>Helper class to sort readers that contain blocks of documents.</summary>
	/// <remarks>
	/// Helper class to sort readers that contain blocks of documents.
	/// <p>
	/// Note that this class is intended to used with
	/// <see cref="SortingMergePolicy">SortingMergePolicy</see>
	/// ,
	/// and for other purposes has some limitations:
	/// <ul>
	/// <li>Cannot yet be used with
	/// <see cref="Org.Apache.Lucene.Search.IndexSearcher.SearchAfter(Org.Apache.Lucene.Search.ScoreDoc, Org.Apache.Lucene.Search.Query, int, Org.Apache.Lucene.Search.Sort)
	/// 	">IndexSearcher.searchAfter</see>
	/// <li>Filling sort field values is not yet supported.
	/// </ul>
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	public class BlockJoinComparatorSource : FieldComparatorSource
	{
		internal readonly Filter parentsFilter;

		internal readonly Sort parentSort;

		internal readonly Sort childSort;

		/// <summary>
		/// Create a new BlockJoinComparatorSource, sorting only blocks of documents
		/// with
		/// <code>parentSort</code>
		/// and not reordering children with a block.
		/// </summary>
		/// <param name="parentsFilter">Filter identifying parent documents</param>
		/// <param name="parentSort">Sort for parent documents</param>
		public BlockJoinComparatorSource(Filter parentsFilter, Sort parentSort) : this(parentsFilter
			, parentSort, new Sort(SortField.FIELD_DOC))
		{
		}

		/// <summary>
		/// Create a new BlockJoinComparatorSource, specifying the sort order for both
		/// blocks of documents and children within a block.
		/// </summary>
		/// <remarks>
		/// Create a new BlockJoinComparatorSource, specifying the sort order for both
		/// blocks of documents and children within a block.
		/// </remarks>
		/// <param name="parentsFilter">Filter identifying parent documents</param>
		/// <param name="parentSort">Sort for parent documents</param>
		/// <param name="childSort">Sort for child documents in the same block</param>
		public BlockJoinComparatorSource(Filter parentsFilter, Sort parentSort, Sort childSort
			)
		{
			// javadocs
			// javadocs
			// javadocs
			// TODO: can/should we clean this thing up (e.g. return a proper sort value)
			// and move to the join/ module?
			this.parentsFilter = parentsFilter;
			this.parentSort = parentSort;
			this.childSort = childSort;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override FieldComparator<object> NewComparator(string fieldname, int numHits
			, int sortPos, bool reversed)
		{
			// we keep parallel slots: the parent ids and the child ids
			int[] parentSlots = new int[numHits];
			int[] childSlots = new int[numHits];
			SortField[] parentFields = parentSort.GetSort();
			int[] parentReverseMul = new int[parentFields.Length];
			FieldComparator<object>[] parentComparators = new FieldComparator[parentFields.Length
				];
			for (int i = 0; i < parentFields.Length; i++)
			{
				parentReverseMul[i] = parentFields[i].GetReverse() ? -1 : 1;
				parentComparators[i] = parentFields[i].GetComparator(1, i);
			}
			SortField[] childFields = childSort.GetSort();
			int[] childReverseMul = new int[childFields.Length];
			FieldComparator<object>[] childComparators = new FieldComparator[childFields.Length
				];
			for (int i_1 = 0; i_1 < childFields.Length; i_1++)
			{
				childReverseMul[i_1] = childFields[i_1].GetReverse() ? -1 : 1;
				childComparators[i_1] = childFields[i_1].GetComparator(1, i_1);
			}
			// NOTE: we could return parent ID as value but really our sort "value" is more complex...
			// So we throw UOE for now. At the moment you really should only use this at indexing time.
			return new _FieldComparator_102(this, childSlots, parentSlots, parentComparators, 
				childComparators, childReverseMul, parentReverseMul);
		}

		private sealed class _FieldComparator_102 : FieldComparator<int>
		{
			public _FieldComparator_102(BlockJoinComparatorSource _enclosing, int[] childSlots
				, int[] parentSlots, FieldComparator<object>[] parentComparators, FieldComparator
				<object>[] childComparators, int[] childReverseMul, int[] parentReverseMul)
			{
				this._enclosing = _enclosing;
				this.childSlots = childSlots;
				this.parentSlots = parentSlots;
				this.parentComparators = parentComparators;
				this.childComparators = childComparators;
				this.childReverseMul = childReverseMul;
				this.parentReverseMul = parentReverseMul;
			}

			internal int bottomParent;

			internal int bottomChild;

			internal FixedBitSet parentBits;

			public override int Compare(int slot1, int slot2)
			{
				try
				{
					return this.Compare(childSlots[slot1], parentSlots[slot1], childSlots[slot2], parentSlots
						[slot2]);
				}
				catch (IOException e)
				{
					throw new RuntimeException(e);
				}
			}

			public override void SetBottom(int slot)
			{
				this.bottomParent = parentSlots[slot];
				this.bottomChild = childSlots[slot];
			}

			public override void SetTopValue(int value)
			{
				// we dont have enough information (the docid is needed)
				throw new NotSupportedException("this comparator cannot be used with deep paging"
					);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int CompareBottom(int doc)
			{
				return this.Compare(this.bottomChild, this.bottomParent, doc, this.Parent(doc));
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int CompareTop(int doc)
			{
				// we dont have enough information (the docid is needed)
				throw new NotSupportedException("this comparator cannot be used with deep paging"
					);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Copy(int slot, int doc)
			{
				childSlots[slot] = doc;
				parentSlots[slot] = this.Parent(doc);
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override FieldComparator<int> SetNextReader(AtomicReaderContext context)
			{
				DocIdSet parents = this._enclosing.parentsFilter.GetDocIdSet(context, null);
				if (parents == null)
				{
					throw new InvalidOperationException("AtomicReader " + ((AtomicReader)context.Reader
						()) + " contains no parents!");
				}
				if (!(parents is FixedBitSet))
				{
					throw new InvalidOperationException("parentFilter must return FixedBitSet; got " 
						+ parents);
				}
				this.parentBits = (FixedBitSet)parents;
				for (int i = 0; i < parentComparators.Length; i++)
				{
					parentComparators[i] = parentComparators[i].SetNextReader(context);
				}
				for (int i_1 = 0; i_1 < childComparators.Length; i_1++)
				{
					childComparators[i_1] = childComparators[i_1].SetNextReader(context);
				}
				return this;
			}

			public override int Value(int slot)
			{
				// really our sort "value" is more complex...
				throw new NotSupportedException("filling sort field values is not yet supported");
			}

			public override void SetScorer(Scorer scorer)
			{
				base.SetScorer(scorer);
				foreach (FieldComparator<object> comp in parentComparators)
				{
					comp.SetScorer(scorer);
				}
				foreach (FieldComparator<object> comp_1 in childComparators)
				{
					comp_1.SetScorer(scorer);
				}
			}

			internal int Parent(int doc)
			{
				return this.parentBits.NextSetBit(doc);
			}

			/// <exception cref="System.IO.IOException"></exception>
			internal int Compare(int docID1, int parent1, int docID2, int parent2)
			{
				if (parent1 == parent2)
				{
					// both are in the same block
					if (docID1 == parent1 || docID2 == parent2)
					{
						// keep parents at the end of blocks
						return docID1 - docID2;
					}
					else
					{
						return this.Compare(docID1, docID2, childComparators, childReverseMul);
					}
				}
				else
				{
					int cmp = this.Compare(parent1, parent2, parentComparators, parentReverseMul);
					if (cmp == 0)
					{
						return parent1 - parent2;
					}
					else
					{
						return cmp;
					}
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			internal int Compare(int docID1, int docID2, FieldComparator<object>[] comparators
				, int[] reverseMul)
			{
				for (int i = 0; i < comparators.Length; i++)
				{
					// TODO: would be better if copy() didnt cause a term lookup in TermOrdVal & co,
					// the segments are always the same here...
					comparators[i].Copy(0, docID1);
					comparators[i].SetBottom(0);
					int comp = reverseMul[i] * comparators[i].CompareBottom(docID2);
					if (comp != 0)
					{
						return comp;
					}
				}
				return 0;
			}

			private readonly BlockJoinComparatorSource _enclosing;

			private readonly int[] childSlots;

			private readonly int[] parentSlots;

			private readonly FieldComparator<object>[] parentComparators;

			private readonly FieldComparator<object>[] childComparators;

			private readonly int[] childReverseMul;

			private readonly int[] parentReverseMul;
		}

		// no need to docid tiebreak
		public override string ToString()
		{
			return "blockJoin(parentSort=" + parentSort + ",childSort=" + childSort + ")";
		}
	}
}
