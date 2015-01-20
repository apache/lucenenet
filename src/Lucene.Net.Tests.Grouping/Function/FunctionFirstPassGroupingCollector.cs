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
using Org.Apache.Lucene.Util.Mutable;
using Sharpen;

namespace Org.Apache.Lucene.Search.Grouping.Function
{
	/// <summary>
	/// Concrete implementation of
	/// <see cref="Org.Apache.Lucene.Search.Grouping.AbstractFirstPassGroupingCollector{GROUP_VALUE_TYPE}
	/// 	">Org.Apache.Lucene.Search.Grouping.AbstractFirstPassGroupingCollector&lt;GROUP_VALUE_TYPE&gt;
	/// 	</see>
	/// that groups based on
	/// <see cref="Org.Apache.Lucene.Queries.Function.ValueSource">Org.Apache.Lucene.Queries.Function.ValueSource
	/// 	</see>
	/// instances.
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public class FunctionFirstPassGroupingCollector : AbstractFirstPassGroupingCollector
		<MutableValue>
	{
		private readonly ValueSource groupByVS;

		private readonly IDictionary<object, object> vsContext;

		private FunctionValues.ValueFiller filler;

		private MutableValue mval;

		/// <summary>Creates a first pass collector.</summary>
		/// <remarks>Creates a first pass collector.</remarks>
		/// <param name="groupByVS">
		/// The
		/// <see cref="Org.Apache.Lucene.Queries.Function.ValueSource">Org.Apache.Lucene.Queries.Function.ValueSource
		/// 	</see>
		/// instance to group by
		/// </param>
		/// <param name="vsContext">The ValueSource context</param>
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
		/// <exception cref="System.IO.IOException">When I/O related errors occur</exception>
		public FunctionFirstPassGroupingCollector(ValueSource groupByVS, IDictionary<object
			, object> vsContext, Sort groupSort, int topNGroups) : base(groupSort, topNGroups
			)
		{
			this.groupByVS = groupByVS;
			this.vsContext = vsContext;
		}

		protected internal override MutableValue GetDocGroupValue(int doc)
		{
			filler.FillValue(doc);
			return mval;
		}

		protected internal override MutableValue CopyDocGroupValue(MutableValue groupValue
			, MutableValue reuse)
		{
			if (reuse != null)
			{
				reuse.Copy(groupValue);
				return reuse;
			}
			return groupValue.Duplicate();
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void SetNextReader(AtomicReaderContext readerContext)
		{
			base.SetNextReader(readerContext);
			FunctionValues values = groupByVS.GetValues(vsContext, readerContext);
			filler = values.GetValueFiller();
			mval = filler.GetValue();
		}
	}
}
