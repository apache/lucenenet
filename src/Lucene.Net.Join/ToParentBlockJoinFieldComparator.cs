/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Search.Join;
using Lucene.Net.Util;
using Sharpen;

namespace Lucene.Net.Search.Join
{
	/// <summary>
	/// A field comparator that allows parent documents to be sorted by fields
	/// from the nested / child documents.
	/// </summary>
	/// <remarks>
	/// A field comparator that allows parent documents to be sorted by fields
	/// from the nested / child documents.
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	public abstract class ToParentBlockJoinFieldComparator : FieldComparator<object>
	{
		private readonly Filter parentFilter;

		private readonly Filter childFilter;

		internal readonly int spareSlot;

		internal FieldComparator<object> wrappedComparator;

		internal FixedBitSet parentDocuments;

		internal FixedBitSet childDocuments;

		internal ToParentBlockJoinFieldComparator(FieldComparator<object> wrappedComparator
			, Filter parentFilter, Filter childFilter, int spareSlot)
		{
			this.wrappedComparator = wrappedComparator;
			this.parentFilter = parentFilter;
			this.childFilter = childFilter;
			this.spareSlot = spareSlot;
		}

		public override int Compare(int slot1, int slot2)
		{
			return wrappedComparator.Compare(slot1, slot2);
		}

		public override void SetBottom(int slot)
		{
			wrappedComparator.SetBottom(slot);
		}

		public override void SetTopValue(object value)
		{
			wrappedComparator.SetTopValue(value);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override FieldComparator<object> SetNextReader(AtomicReaderContext context
			)
		{
			DocIdSet innerDocuments = childFilter.GetDocIdSet(context, null);
			if (IsEmpty(innerDocuments))
			{
				this.childDocuments = null;
			}
			else
			{
				if (innerDocuments is FixedBitSet)
				{
					this.childDocuments = (FixedBitSet)innerDocuments;
				}
				else
				{
					DocIdSetIterator iterator = innerDocuments.Iterator();
					if (iterator != null)
					{
						this.childDocuments = ToFixedBitSet(iterator, ((AtomicReader)context.Reader()).MaxDoc
							());
					}
					else
					{
						childDocuments = null;
					}
				}
			}
			DocIdSet rootDocuments = parentFilter.GetDocIdSet(context, null);
			if (IsEmpty(rootDocuments))
			{
				this.parentDocuments = null;
			}
			else
			{
				if (rootDocuments is FixedBitSet)
				{
					this.parentDocuments = (FixedBitSet)rootDocuments;
				}
				else
				{
					DocIdSetIterator iterator = rootDocuments.Iterator();
					if (iterator != null)
					{
						this.parentDocuments = ToFixedBitSet(iterator, ((AtomicReader)context.Reader()).MaxDoc
							());
					}
					else
					{
						this.parentDocuments = null;
					}
				}
			}
			wrappedComparator = wrappedComparator.SetNextReader(context);
			return this;
		}

		private static bool IsEmpty(DocIdSet set)
		{
			return set == null;
		}

		/// <exception cref="System.IO.IOException"></exception>
		private static FixedBitSet ToFixedBitSet(DocIdSetIterator iterator, int numBits)
		{
			FixedBitSet set = new FixedBitSet(numBits);
			int doc;
			while ((doc = iterator.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
			{
				set.Set(doc);
			}
			return set;
		}

		public override object Value(int slot)
		{
			return wrappedComparator.Value(slot);
		}

		/// <summary>
		/// Concrete implementation of
		/// <see cref="ToParentBlockJoinSortField">ToParentBlockJoinSortField</see>
		/// to sorts the parent docs with the lowest values
		/// in the child / nested docs first.
		/// </summary>
		public sealed class Lowest : ToParentBlockJoinFieldComparator
		{
			/// <summary>Create ToParentBlockJoinFieldComparator.Lowest</summary>
			/// <param name="wrappedComparator">
			/// The
			/// <see cref="Lucene.Net.Search.FieldComparator{T}">Lucene.Net.Search.FieldComparator&lt;T&gt;
			/// 	</see>
			/// on the child / nested level.
			/// </param>
			/// <param name="parentFilter">Filter (must produce FixedBitSet per-segment) that identifies the parent documents.
			/// 	</param>
			/// <param name="childFilter">Filter that defines which child / nested documents participates in sorting.
			/// 	</param>
			/// <param name="spareSlot">
			/// The extra slot inside the wrapped comparator that is used to compare which nested document
			/// inside the parent document scope is most competitive.
			/// </param>
			internal Lowest(FieldComparator<object> wrappedComparator, Filter parentFilter, Filter
				 childFilter, int spareSlot) : base(wrappedComparator, parentFilter, childFilter
				, spareSlot)
			{
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int CompareBottom(int parentDoc)
			{
				if (parentDoc == 0 || parentDocuments == null || childDocuments == null)
				{
					return 0;
				}
				// We need to copy the lowest value from all child docs into slot.
				int prevParentDoc = parentDocuments.PrevSetBit(parentDoc - 1);
				int childDoc = childDocuments.NextSetBit(prevParentDoc + 1);
				if (childDoc >= parentDoc || childDoc == -1)
				{
					return 0;
				}
				// We only need to emit a single cmp value for any matching child doc
				int cmp = wrappedComparator.CompareBottom(childDoc);
				if (cmp > 0)
				{
					return cmp;
				}
				while (true)
				{
					childDoc = childDocuments.NextSetBit(childDoc + 1);
					if (childDoc >= parentDoc || childDoc == -1)
					{
						return cmp;
					}
					int cmp1 = wrappedComparator.CompareBottom(childDoc);
					if (cmp1 > 0)
					{
						return cmp1;
					}
					else
					{
						if (cmp1 == 0)
						{
							cmp = 0;
						}
					}
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Copy(int slot, int parentDoc)
			{
				if (parentDoc == 0 || parentDocuments == null || childDocuments == null)
				{
					return;
				}
				// We need to copy the lowest value from all child docs into slot.
				int prevParentDoc = parentDocuments.PrevSetBit(parentDoc - 1);
				int childDoc = childDocuments.NextSetBit(prevParentDoc + 1);
				if (childDoc >= parentDoc || childDoc == -1)
				{
					return;
				}
				wrappedComparator.Copy(spareSlot, childDoc);
				wrappedComparator.Copy(slot, childDoc);
				while (true)
				{
					childDoc = childDocuments.NextSetBit(childDoc + 1);
					if (childDoc >= parentDoc || childDoc == -1)
					{
						return;
					}
					wrappedComparator.Copy(spareSlot, childDoc);
					if (wrappedComparator.Compare(spareSlot, slot) < 0)
					{
						wrappedComparator.Copy(slot, childDoc);
					}
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int CompareTop(int parentDoc)
			{
				if (parentDoc == 0 || parentDocuments == null || childDocuments == null)
				{
					return 0;
				}
				// We need to copy the lowest value from all nested docs into slot.
				int prevParentDoc = parentDocuments.PrevSetBit(parentDoc - 1);
				int childDoc = childDocuments.NextSetBit(prevParentDoc + 1);
				if (childDoc >= parentDoc || childDoc == -1)
				{
					return 0;
				}
				// We only need to emit a single cmp value for any matching child doc
				int cmp = wrappedComparator.CompareBottom(childDoc);
				if (cmp > 0)
				{
					return cmp;
				}
				while (true)
				{
					childDoc = childDocuments.NextSetBit(childDoc + 1);
					if (childDoc >= parentDoc || childDoc == -1)
					{
						return cmp;
					}
					int cmp1 = wrappedComparator.CompareTop(childDoc);
					if (cmp1 > 0)
					{
						return cmp1;
					}
					else
					{
						if (cmp1 == 0)
						{
							cmp = 0;
						}
					}
				}
			}
		}

		/// <summary>
		/// Concrete implementation of
		/// <see cref="ToParentBlockJoinSortField">ToParentBlockJoinSortField</see>
		/// to sorts the parent docs with the highest values
		/// in the child / nested docs first.
		/// </summary>
		public sealed class Highest : ToParentBlockJoinFieldComparator
		{
			/// <summary>Create ToParentBlockJoinFieldComparator.Highest</summary>
			/// <param name="wrappedComparator">
			/// The
			/// <see cref="Lucene.Net.Search.FieldComparator{T}">Lucene.Net.Search.FieldComparator&lt;T&gt;
			/// 	</see>
			/// on the child / nested level.
			/// </param>
			/// <param name="parentFilter">Filter (must produce FixedBitSet per-segment) that identifies the parent documents.
			/// 	</param>
			/// <param name="childFilter">Filter that defines which child / nested documents participates in sorting.
			/// 	</param>
			/// <param name="spareSlot">
			/// The extra slot inside the wrapped comparator that is used to compare which nested document
			/// inside the parent document scope is most competitive.
			/// </param>
			internal Highest(FieldComparator<object> wrappedComparator, Filter parentFilter, 
				Filter childFilter, int spareSlot) : base(wrappedComparator, parentFilter, childFilter
				, spareSlot)
			{
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int CompareBottom(int parentDoc)
			{
				if (parentDoc == 0 || parentDocuments == null || childDocuments == null)
				{
					return 0;
				}
				int prevParentDoc = parentDocuments.PrevSetBit(parentDoc - 1);
				int childDoc = childDocuments.NextSetBit(prevParentDoc + 1);
				if (childDoc >= parentDoc || childDoc == -1)
				{
					return 0;
				}
				int cmp = wrappedComparator.CompareBottom(childDoc);
				if (cmp < 0)
				{
					return cmp;
				}
				while (true)
				{
					childDoc = childDocuments.NextSetBit(childDoc + 1);
					if (childDoc >= parentDoc || childDoc == -1)
					{
						return cmp;
					}
					int cmp1 = wrappedComparator.CompareBottom(childDoc);
					if (cmp1 < 0)
					{
						return cmp1;
					}
					else
					{
						if (cmp1 == 0)
						{
							cmp = 0;
						}
					}
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override void Copy(int slot, int parentDoc)
			{
				if (parentDoc == 0 || parentDocuments == null || childDocuments == null)
				{
					return;
				}
				int prevParentDoc = parentDocuments.PrevSetBit(parentDoc - 1);
				int childDoc = childDocuments.NextSetBit(prevParentDoc + 1);
				if (childDoc >= parentDoc || childDoc == -1)
				{
					return;
				}
				wrappedComparator.Copy(spareSlot, childDoc);
				wrappedComparator.Copy(slot, childDoc);
				while (true)
				{
					childDoc = childDocuments.NextSetBit(childDoc + 1);
					if (childDoc >= parentDoc || childDoc == -1)
					{
						return;
					}
					wrappedComparator.Copy(spareSlot, childDoc);
					if (wrappedComparator.Compare(spareSlot, slot) > 0)
					{
						wrappedComparator.Copy(slot, childDoc);
					}
				}
			}

			/// <exception cref="System.IO.IOException"></exception>
			public override int CompareTop(int parentDoc)
			{
				if (parentDoc == 0 || parentDocuments == null || childDocuments == null)
				{
					return 0;
				}
				int prevParentDoc = parentDocuments.PrevSetBit(parentDoc - 1);
				int childDoc = childDocuments.NextSetBit(prevParentDoc + 1);
				if (childDoc >= parentDoc || childDoc == -1)
				{
					return 0;
				}
				int cmp = wrappedComparator.CompareBottom(childDoc);
				if (cmp < 0)
				{
					return cmp;
				}
				while (true)
				{
					childDoc = childDocuments.NextSetBit(childDoc + 1);
					if (childDoc >= parentDoc || childDoc == -1)
					{
						return cmp;
					}
					int cmp1 = wrappedComparator.CompareTop(childDoc);
					if (cmp1 < 0)
					{
						return cmp1;
					}
					else
					{
						if (cmp1 == 0)
						{
							cmp = 0;
						}
					}
				}
			}
		}
	}
}
