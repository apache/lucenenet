/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Queries.Function;
using Org.Apache.Lucene.Search.Grouping;
using Org.Apache.Lucene.Util.Mutable;
using Sharpen;

namespace Org.Apache.Lucene.Search.Grouping.Function
{
	/// <summary>
	/// A collector that collects all groups that match the
	/// query.
	/// </summary>
	/// <remarks>
	/// A collector that collects all groups that match the
	/// query. Only the group value is collected, and the order
	/// is undefined.  This collector does not determine
	/// the most relevant document of a group.
	/// <p/>
	/// Implementation detail: Uses
	/// <see cref="Org.Apache.Lucene.Queries.Function.ValueSource">Org.Apache.Lucene.Queries.Function.ValueSource
	/// 	</see>
	/// and
	/// <see cref="Org.Apache.Lucene.Queries.Function.FunctionValues">Org.Apache.Lucene.Queries.Function.FunctionValues
	/// 	</see>
	/// to retrieve the
	/// field values to group by.
	/// </remarks>
	/// <lucene.experimental></lucene.experimental>
	public class FunctionAllGroupsCollector : AbstractAllGroupsCollector<MutableValue
		>
	{
		private readonly IDictionary<object, object> vsContext;

		private readonly ValueSource groupBy;

		private readonly ICollection<MutableValue> groups = new TreeSet<MutableValue>();

		private FunctionValues.ValueFiller filler;

		private MutableValue mval;

		/// <summary>
		/// Constructs a
		/// <see cref="FunctionAllGroupsCollector">FunctionAllGroupsCollector</see>
		/// instance.
		/// </summary>
		/// <param name="groupBy">
		/// The
		/// <see cref="Org.Apache.Lucene.Queries.Function.ValueSource">Org.Apache.Lucene.Queries.Function.ValueSource
		/// 	</see>
		/// to group by
		/// </param>
		/// <param name="vsContext">The ValueSource context</param>
		public FunctionAllGroupsCollector(ValueSource groupBy, IDictionary<object, object
			> vsContext)
		{
			this.vsContext = vsContext;
			this.groupBy = groupBy;
		}

		public override ICollection<MutableValue> GetGroups()
		{
			return groups;
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Collect(int doc)
		{
			filler.FillValue(doc);
			if (!groups.Contains(mval))
			{
				groups.AddItem(mval.Duplicate());
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void SetNextReader(AtomicReaderContext context)
		{
			FunctionValues values = groupBy.GetValues(vsContext, context);
			filler = values.GetValueFiller();
			mval = filler.GetValue();
		}
	}
}
