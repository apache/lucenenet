using Lucene.Net.Index;
using Lucene.Net.Util;
using System.Collections.Generic;

namespace Lucene.Net.Search.Grouping.Terms
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    /// <summary>
    /// Concrete implementation of <see cref="AbstractSecondPassGroupingCollector{BytesRef}"/> that groups based on
    /// field values and more specifically uses <see cref="SortedDocValues"/>
    /// to collect grouped docs.
    /// 
    /// @lucene.experimental
    /// </summary>
    public class TermSecondPassGroupingCollector : AbstractSecondPassGroupingCollector<BytesRef>
    {
        private readonly SentinelInt32Set ordSet;
        private SortedDocValues index;
        private readonly string groupField;

        public TermSecondPassGroupingCollector(string groupField, IEnumerable<ISearchGroup<BytesRef>> groups, Sort groupSort, Sort withinGroupSort,
                                               int maxDocsPerGroup, bool getScores, bool getMaxScores, bool fillSortFields)
                  : base(groups, groupSort, withinGroupSort, maxDocsPerGroup, getScores, getMaxScores, fillSortFields)
        {
            ordSet = new SentinelInt32Set(m_groupMap.Count, -2);
            this.groupField = groupField;
            m_groupDocs = /*(SearchGroupDocs<BytesRef>[])*/ new AbstractSecondPassGroupingCollector.SearchGroupDocs<BytesRef>[ordSet.Keys.Length];
        }

        public override void SetNextReader(AtomicReaderContext context)
        {
            base.SetNextReader(context);
            index = FieldCache.DEFAULT.GetTermsIndex(context.AtomicReader, groupField);

            // Rebuild ordSet
            ordSet.Clear();
            foreach (AbstractSecondPassGroupingCollector.SearchGroupDocs<BytesRef> group in m_groupMap.Values)
            {
                //      System.out.println("  group=" + (group.groupValue is null ? "null" : group.groupValue.utf8ToString()));
                int ord = group.GroupValue is null ? -1 : index.LookupTerm(group.GroupValue);
                if (group.GroupValue is null || ord >= 0)
                {
                    m_groupDocs[ordSet.Put(ord)] = group;
                }
            }
        }

        protected override AbstractSecondPassGroupingCollector.SearchGroupDocs<BytesRef> RetrieveGroup(int doc)
        {
            int slot = ordSet.Find(index.GetOrd(doc));
            if (slot >= 0)
            {
                return m_groupDocs[slot];
            }
            return null;
        }
    }
}
