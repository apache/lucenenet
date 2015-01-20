using System.Collections.Generic;
using Lucene.Net.Index;
using Lucene.Net.Queries.Function;
using Lucene.Net.Search.Grouping;
using Lucene.Net.Support;
using Lucene.Net.Util.Mutable;

namespace Lucene.Net.Grouping.Function
{
	/// <summary>
	/// Function based implementation of
	/// <see cref="Lucene.Net.Search.Grouping.AbstractDistinctValuesCollector{GC}"
	/// 	>Lucene.Net.Search.Grouping.AbstractDistinctValuesCollector&lt;GC&gt;</see>
	/// .
	/// </summary>
	/// <lucene.experimental></lucene.experimental>
	public class FunctionDistinctValuesCollector : AbstractDistinctValuesCollector<FunctionDistinctValuesCollector.GroupCount
		>
	{
		private readonly IDictionary<object, object> vsContext;

		private readonly ValueSource groupSource;

		private readonly ValueSource countSource;

		private readonly IDictionary<MutableValue, GroupCount> groupMap;

		private FunctionValues.AbstractValueFiller groupFiller;

		private FunctionValues.AbstractValueFiller countFiller;

		private MutableValue groupMval;

		private MutableValue countMval;

		public FunctionDistinctValuesCollector(IDictionary<object, object> vsContext, ValueSource
			 groupSource, ValueSource countSource, ICollection<SearchGroup<MutableValue>> groups
			)
		{
			this.vsContext = vsContext;
			this.groupSource = groupSource;
			this.countSource = countSource;
			groupMap = new HashMap<MutableValue, GroupCount>();
			foreach (SearchGroup<MutableValue> group in groups)
			{
				groupMap[group.groupValue] = new GroupCount(group.groupValue);
			}
		}

		public override IList<GroupCount> GetGroups()
		{
			return new List<GroupCount>(groupMap.Values);
		}

		
		public override void Collect(int doc)
		{
			groupFiller.FillValue(doc);
			GroupCount groupCount = groupMap[groupMval];
			if (groupCount != null)
			{
				countFiller.FillValue(doc);
				groupCount.uniqueValues.Add(countMval.Duplicate());
			}
		}


        public override AtomicReaderContext NextReader
		{
            set
            {
                FunctionValues values = groupSource.GetValues(vsContext, value);
                groupFiller = values.ValueFiller;
                groupMval = groupFiller.Value;
                values = countSource.GetValues(vsContext, value);
                countFiller = values.ValueFiller;
                countMval = countFiller.Value;
            }
		}

		/// <summary>Holds distinct values for a single group.</summary>
		/// <remarks>Holds distinct values for a single group.</remarks>
		/// <lucene.experimental></lucene.experimental>
		public class GroupCount : AbstractDistinctValuesCollector.GroupCount<MutableValue>
		{
			public GroupCount(MutableValue groupValue) : base(groupValue)
			{
			}
		}
	}
}
