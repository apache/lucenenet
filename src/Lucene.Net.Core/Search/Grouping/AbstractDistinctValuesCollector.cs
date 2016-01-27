using Lucene.Net.Search;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Lucene.Net.Search.Grouping
{
	public abstract class AbstractDistinctValuesCollector<T> : Collector where T : AbstractDistinctValuesCollector.GroupCount
	{

		public abstract List<T> getGroups();

		public override bool AcceptsDocsOutOfOrder()
		{
			return true;
		}

		public override Scorer Scorer
		{
			set
			{
				throw new NotImplementedException();
			}
		}
	}


	public abstract class AbstractDistinctValuesCollector
	{
		public interface GroupCount
		{
		}

		public abstract class GroupCount<GROUP_VALUE_TYPE> : GroupCount
		{
			public GROUP_VALUE_TYPE groupValue;

			public HashSet<GROUP_VALUE_TYPE> uniqueValues;

			public GroupCount(GROUP_VALUE_TYPE groupValue)
			{
				this.groupValue = groupValue;
				this.uniqueValues = new HashSet<GROUP_VALUE_TYPE>();
			}
		}
	}
}
