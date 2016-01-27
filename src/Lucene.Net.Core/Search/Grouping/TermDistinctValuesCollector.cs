using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Lucene.Net.Util;
using Lucene.Net;
using Lucene.Net.Search;
using System.Collections.ObjectModel;
using Lucene.Net.Index;
using Lucene.Net.Support;



namespace Lucene.Net.Search.Grouping
{
	public class TermDistinctValuesCollector : AbstractDistinctValuesCollector<TermDistinctValuesCollector.GroupCount>
	{

		private String groupField;

		private String countField;

		private List<GroupCount> groups;

		private SentinelIntSet ordSet;

		private GroupCount[] groupCounts;

		private SortedDocValues groupFieldTermIndex;

		private SortedDocValues countFieldTermIndex;

		public TermDistinctValuesCollector(String groupField, String countField, Collection<GroupCount> groups)
		{
			this.groupField = groupField;
			this.countField = countField;
			this.groups = new List<GroupCount>(groups.Count());
			foreach(GroupCount group in this.groups)
			{
				this.groups.Add(new GroupCount(group.groupValue));
			}

			this.ordSet = new SentinelIntSet(groups.Count(), -2);
			groupCounts = new GroupCount[this.ordSet.Keys.Count()];
		}

		public override void Collect(int doc)
		{
			int slot = this.ordSet.Find(this.groupFieldTermIndex.GetOrd(doc));
			if((slot < 0))
			{
				return;
			}

			GroupCount gc = groupCounts[slot];
			int countOrd = this.countFieldTermIndex.GetOrd(doc);
			if(this.doesNotContainOrd(countOrd, gc.ords))
			{
				if((countOrd == -1))
				{
					gc.uniqueValues.Add(null);
				}
				else
				{
					BytesRef br = new BytesRef();
					countFieldTermIndex.LookupOrd(countOrd, br);
					gc.uniqueValues.Add(br);
				}

				gc.ords = Arrays.CopyOf(gc.ords, (gc.ords.Length + 1));
				gc.ords[(gc.ords.Length - 1)] = countOrd;
				if((gc.ords.Length > 1))
				{
					Arrays.Sort(gc.ords);
				}

			}

		}

		private bool doesNotContainOrd(int ord, int[] ords)
		{
			if((ords.Length == 0))
			{
				return true;
			}
			else if((ords.Length == 1))
			{
				return (ord != ords[0]);
			}

			return (Arrays.BinarySearch(ords, ord) < 0);
		}

		public override List<GroupCount> getGroups()
		{
			return this.groups;
		}

		public void SetNextReader(AtomicReaderContext context)
		{
			this.groupFieldTermIndex = FieldCache.DEFAULT.GetTermsIndex(context.AtomicReader, this.groupField);
			this.countFieldTermIndex = FieldCache.DEFAULT.GetTermsIndex(context.AtomicReader, this.countField);
			this.ordSet.Clear();
			foreach(GroupCount group in this.groups)
			{
				int groupOrd = group.groupValue == null ? -1 : groupFieldTermIndex.LookupTerm(group.groupValue);
				if(group.groupValue != null && groupOrd < 0)
				{
					continue;
				}

				groupCounts[this.ordSet.Put(groupOrd)] = group;
				group.ords = new int[group.uniqueValues.Count()];
				Arrays.Fill(group.ords, -2);
				int i = 0;
				foreach(BytesRef value in group.uniqueValues)
				{
					int countOrd = value == null ? -1 : countFieldTermIndex.LookupTerm(value);
					if(value == null || countOrd >= 0)
					{
						group.ords[i++] = countOrd;
					}
				}

			}

		}

		public override AtomicReaderContext NextReader
		{
			set
			{
				SetNextReader(value);
			}
		}

		public class GroupCount : AbstractDistinctValuesCollector.GroupCount<BytesRef>
		{
			public int[] ords;

			public GroupCount(BytesRef groupValue)
				: base(groupValue)
			{
			}
		}
	}
}
