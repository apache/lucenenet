/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Queries.Function;
using Org.Apache.Lucene.Search.Grouping;
using Org.Apache.Lucene.Search.Grouping.Function;
using Org.Apache.Lucene.Util.Mutable;
using Sharpen;

namespace Org.Apache.Lucene.Search.Grouping.Function
{
	/// <summary>
	/// Function based implementation of
	/// <see cref="Org.Apache.Lucene.Search.Grouping.AbstractDistinctValuesCollector{GC}"
	/// 	>Org.Apache.Lucene.Search.Grouping.AbstractDistinctValuesCollector&lt;GC&gt;</see>
	/// .
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public class FunctionDistinctValuesCollector : AbstractDistinctValuesCollector<FunctionDistinctValuesCollector.GroupCount
		>
	{
		private readonly IDictionary<object, object> vsContext;

		private readonly ValueSource groupSource;

		private readonly ValueSource countSource;

		private readonly IDictionary<MutableValue, FunctionDistinctValuesCollector.GroupCount
			> groupMap;

		private FunctionValues.ValueFiller groupFiller;

		private FunctionValues.ValueFiller countFiller;

		private MutableValue groupMval;

		private MutableValue countMval;

		public FunctionDistinctValuesCollector(IDictionary<object, object> vsContext, ValueSource
			 groupSource, ValueSource countSource, ICollection<SearchGroup<MutableValue>> groups
			)
		{
			this.vsContext = vsContext;
			this.groupSource = groupSource;
			this.countSource = countSource;
			groupMap = new LinkedHashMap<MutableValue, FunctionDistinctValuesCollector.GroupCount
				>();
			foreach (SearchGroup<MutableValue> group in groups)
			{
				groupMap.Put(group.groupValue, new FunctionDistinctValuesCollector.GroupCount(group
					.groupValue));
			}
		}

		public override IList<FunctionDistinctValuesCollector.GroupCount> GetGroups()
		{
			return new AList<FunctionDistinctValuesCollector.GroupCount>(groupMap.Values);
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void Collect(int doc)
		{
			groupFiller.FillValue(doc);
			FunctionDistinctValuesCollector.GroupCount groupCount = groupMap.Get(groupMval);
			if (groupCount != null)
			{
				countFiller.FillValue(doc);
				groupCount.uniqueValues.AddItem(countMval.Duplicate());
			}
		}

		/// <exception cref="System.IO.IOException"></exception>
		public override void SetNextReader(AtomicReaderContext context)
		{
			FunctionValues values = groupSource.GetValues(vsContext, context);
			groupFiller = values.GetValueFiller();
			groupMval = groupFiller.GetValue();
			values = countSource.GetValues(vsContext, context);
			countFiller = values.GetValueFiller();
			countMval = countFiller.GetValue();
		}

		/// <summary>Holds distinct values for a single group.</summary>
		/// <remarks>Holds distinct values for a single group.</remarks>
		/// <lucene.experimental></lucene.experimental>
		public class GroupCount : AbstractDistinctValuesCollector.GroupCount<MutableValue
			>
		{
			public GroupCount(MutableValue groupValue) : base(groupValue)
			{
			}
		}
	}
}
