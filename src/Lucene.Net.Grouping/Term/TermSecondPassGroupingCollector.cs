using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.Search.Grouping.Terms
{
    /// <summary>
    /// Concrete implementation of <see cref="AbstractSecondPassGroupingCollector{BytesRef}"/> that groups based on
    /// field values and more specifically uses <see cref="SortedDocValues"/>
    /// to collect grouped docs.
    /// 
    /// @lucene.experimental
    /// </summary>
    public class TermSecondPassGroupingCollector : AbstractSecondPassGroupingCollector<BytesRef>
    {
        private readonly SentinelIntSet ordSet;
        private SortedDocValues index;
        private readonly string groupField;

        public TermSecondPassGroupingCollector(string groupField, ICollection<SearchGroup<BytesRef>> groups, Sort groupSort, Sort withinGroupSort,
                                               int maxDocsPerGroup, bool getScores, bool getMaxScores, bool fillSortFields)
                  : base(groups, groupSort, withinGroupSort, maxDocsPerGroup, getScores, getMaxScores, fillSortFields)
        {
            ordSet = new SentinelIntSet(groupMap.Count, -2);
            this.groupField = groupField;
            groupDocs = /*(SearchGroupDocs<BytesRef>[])*/ new AbstractSecondPassGroupingCollector.SearchGroupDocs<BytesRef>[ordSet.Keys.Length];
        }

        public override AtomicReaderContext NextReader
        {
            set
            {
                base.NextReader = value;
                index = FieldCache.DEFAULT.GetTermsIndex(value.AtomicReader, groupField);

                // Rebuild ordSet
                ordSet.Clear();
                foreach (AbstractSecondPassGroupingCollector.SearchGroupDocs<BytesRef> group in groupMap.Values)
                {
                    //      System.out.println("  group=" + (group.groupValue == null ? "null" : group.groupValue.utf8ToString()));
                    int ord = group.groupValue == null ? -1 : index.LookupTerm(group.groupValue);
                    if (group.groupValue == null || ord >= 0)
                    {
                        groupDocs[ordSet.Put(ord)] = group;
                    }
                }
            }
        }

        protected override AbstractSecondPassGroupingCollector.SearchGroupDocs<BytesRef> RetrieveGroup(int doc)
        {
            int slot = ordSet.Find(index.GetOrd(doc));
            if (slot >= 0)
            {
                return groupDocs[slot];
            }
            return null;
        }
    }
}
