/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Queries.Function;
using Org.Apache.Lucene.Search;
using Org.Apache.Lucene.Search.Grouping;
using Org.Apache.Lucene.Search.Grouping.Function;
using Org.Apache.Lucene.Util.Mutable;
using Sharpen;

namespace Org.Apache.Lucene.Search.Grouping.Function
{
	/// <summary>
	/// An implementation of
	/// <see cref="Org.Apache.Lucene.Search.Grouping.AbstractAllGroupHeadsCollector{GH}">Org.Apache.Lucene.Search.Grouping.AbstractAllGroupHeadsCollector&lt;GH&gt;
	/// 	</see>
	/// for retrieving the most relevant groups when grouping
	/// by
	/// <see cref="Org.Apache.Lucene.Queries.Function.ValueSource">Org.Apache.Lucene.Queries.Function.ValueSource
	/// 	</see>
	/// .
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public class FunctionAllGroupHeadsCollector : AbstractAllGroupHeadsCollector<FunctionAllGroupHeadsCollector.GroupHead
		>
	{
		private readonly ValueSource groupBy;

		private readonly IDictionary<object, object> vsContext;

		private readonly IDictionary<MutableValue, FunctionAllGroupHeadsCollector.GroupHead
			> groups;

		private readonly Sort sortWithinGroup;

		private FunctionValues.ValueFiller filler;

		private MutableValue mval;

		private AtomicReaderContext readerContext;

		private Scorer scorer;

		/// <summary>
		/// Constructs a
		/// <see cref="FunctionAllGroupHeadsCollector">FunctionAllGroupHeadsCollector</see>
		/// instance.
		/// </summary>
		/// <param name="groupBy">
		/// The
		/// <see cref="Org.Apache.Lucene.Queries.Function.ValueSource">Org.Apache.Lucene.Queries.Function.ValueSource
		/// 	</see>
		/// to group by
		/// </param>
		/// <param name="vsContext">The ValueSource context</param>
		/// <param name="sortWithinGroup">The sort within a group</param>
		public FunctionAllGroupHeadsCollector(ValueSource groupBy, IDictionary<object, object
			> vsContext, Sort sortWithinGroup) : base(sortWithinGroup.GetSort().Length)
		{
			groups = new Dictionary<MutableValue, FunctionAllGroupHeadsCollector.GroupHead>();
			this.sortWithinGroup = sortWithinGroup;
			this.groupBy = groupBy;
			this.vsContext = vsContext;
			SortField[] sortFields = sortWithinGroup.GetSort();
			for (int i = 0; i < sortFields.Length; i++)
			{
				reversed[i] = sortFields[i].GetReverse() ? -1 : 1;
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		protected internal override void RetrieveGroupHeadAndAddIfNotExist(int doc)
		{
			filler.FillValue(doc);
			FunctionAllGroupHeadsCollector.GroupHead groupHead = groups.Get(mval);
			if (groupHead == null)
			{
				MutableValue groupValue = mval.Duplicate();
				groupHead = new FunctionAllGroupHeadsCollector.GroupHead(this, groupValue, sortWithinGroup
					, doc);
				groups.Put(groupValue, groupHead);
				temporalResult.stop = true;
			}
			else
			{
				temporalResult.stop = false;
			}
			this.temporalResult.groupHead = groupHead;
		}

		protected internal override ICollection<FunctionAllGroupHeadsCollector.GroupHead>
			 GetCollectedGroupHeads()
		{
			return groups.Values;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void SetScorer(Scorer scorer)
		{
			this.scorer = scorer;
			foreach (FunctionAllGroupHeadsCollector.GroupHead groupHead in groups.Values)
			{
				foreach (FieldComparator<object> comparator in groupHead.comparators)
				{
					comparator.SetScorer(scorer);
				}
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void SetNextReader(AtomicReaderContext context)
		{
			this.readerContext = context;
			FunctionValues values = groupBy.GetValues(vsContext, context);
			filler = values.GetValueFiller();
			mval = filler.GetValue();
			foreach (FunctionAllGroupHeadsCollector.GroupHead groupHead in groups.Values)
			{
				for (int i = 0; i < groupHead.comparators.Length; i++)
				{
					groupHead.comparators[i] = groupHead.comparators[i].SetNextReader(context);
				}
			}
		}

		/// <summary>Holds current head document for a single group.</summary>
		/// <remarks>Holds current head document for a single group.</remarks>
		/// <lucene.experimental></lucene.experimental>
		public class GroupHead : AbstractAllGroupHeadsCollector.GroupHead<MutableValue>
		{
			internal readonly FieldComparator<object>[] comparators;

			/// <exception cref="System.IO.IOException"></exception>
			private GroupHead(FunctionAllGroupHeadsCollector _enclosing, MutableValue groupValue
				, Sort sort, int doc) : base(groupValue, doc + this._enclosing.readerContext.docBase
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

			private readonly FunctionAllGroupHeadsCollector _enclosing;
		}
	}
}
