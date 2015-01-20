/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Search;
using Lucene.Net.Search.Join;
using Sharpen;

namespace Lucene.Net.Search.Join
{
	/// <summary>A special sort field that allows sorting parent docs based on nested / child level fields.
	/// 	</summary>
	/// <remarks>
	/// A special sort field that allows sorting parent docs based on nested / child level fields.
	/// Based on the sort order it either takes the document with the lowest or highest field value into account.
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	public class ToParentBlockJoinSortField : SortField
	{
		private readonly bool order;

		private readonly Filter parentFilter;

		private readonly Filter childFilter;

		/// <summary>Create ToParentBlockJoinSortField.</summary>
		/// <remarks>Create ToParentBlockJoinSortField. The parent document ordering is based on child document ordering (reverse).
		/// 	</remarks>
		/// <param name="field">The sort field on the nested / child level.</param>
		/// <param name="type">The sort type on the nested / child level.</param>
		/// <param name="reverse">Whether natural order should be reversed on the nested / child level.
		/// 	</param>
		/// <param name="parentFilter">Filter that identifies the parent documents.</param>
		/// <param name="childFilter">Filter that defines which child documents participates in sorting.
		/// 	</param>
		public ToParentBlockJoinSortField(string field, SortField.Type type, bool reverse
			, Filter parentFilter, Filter childFilter) : base(field, type, reverse)
		{
			this.order = reverse;
			this.parentFilter = parentFilter;
			this.childFilter = childFilter;
		}

		/// <summary>Create ToParentBlockJoinSortField.</summary>
		/// <remarks>Create ToParentBlockJoinSortField.</remarks>
		/// <param name="field">The sort field on the nested / child level.</param>
		/// <param name="type">The sort type on the nested / child level.</param>
		/// <param name="reverse">Whether natural order should be reversed on the nested / child document level.
		/// 	</param>
		/// <param name="order">Whether natural order should be reversed on the parent level.
		/// 	</param>
		/// <param name="parentFilter">Filter that identifies the parent documents.</param>
		/// <param name="childFilter">Filter that defines which child documents participates in sorting.
		/// 	</param>
		public ToParentBlockJoinSortField(string field, SortField.Type type, bool reverse
			, bool order, Filter parentFilter, Filter childFilter) : base(field, type, reverse
			)
		{
			this.order = order;
			this.parentFilter = parentFilter;
			this.childFilter = childFilter;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override FieldComparator<object> GetComparator(int numHits, int sortPos)
		{
			FieldComparator<object> wrappedFieldComparator = (FieldComparator)base.GetComparator
				(numHits + 1, sortPos);
			if (order)
			{
				return new ToParentBlockJoinFieldComparator.Highest(wrappedFieldComparator, parentFilter
					, childFilter, numHits);
			}
			else
			{
				return new ToParentBlockJoinFieldComparator.Lowest(wrappedFieldComparator, parentFilter
					, childFilter, numHits);
			}
		}
	}
}
