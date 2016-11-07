using Lucene.Net.Index;
using Lucene.Net.Queries.Function;
using Lucene.Net.Util.Mutable;
using System.Collections;
using System.Collections.Generic;

namespace Lucene.Net.Search.Grouping.Function
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
    /// Concrete implementation of <see cref="AbstractSecondPassGroupingCollector{TGroupValue}"/> that groups based on
    /// <see cref="ValueSource"/> instances.
    /// 
    /// @lucene.experimental
    /// </summary>
    public class FunctionSecondPassGroupingCollector : AbstractSecondPassGroupingCollector<MutableValue>
    {
        private readonly ValueSource groupByVS;
        private readonly IDictionary /* Map<?, ?> */ vsContext;

        private FunctionValues.AbstractValueFiller filler;
        private MutableValue mval;

        /// <summary>
        /// Constructs a <see cref="FunctionSecondPassGroupingCollector"/> instance.
        /// </summary>
        /// <param name="searchGroups">The <see cref="SearchGroup{TGroupValue}"/> instances collected during the first phase.</param>
        /// <param name="groupSort">The group sort</param>
        /// <param name="withinGroupSort">The sort inside a group</param>
        /// <param name="maxDocsPerGroup">The maximum number of documents to collect inside a group</param>
        /// <param name="getScores">Whether to include the scores</param>
        /// <param name="getMaxScores">Whether to include the maximum score</param>
        /// <param name="fillSortFields">Whether to fill the sort values in <see cref="TopGroups{TGroupValueType}.WithinGroupSort"/></param>
        /// <param name="groupByVS">The <see cref="ValueSource"/> to group by</param>
        /// <param name="vsContext">The value source context</param>
        /// <exception cref="IOException">When I/O related errors occur</exception>
        public FunctionSecondPassGroupingCollector(IEnumerable<ISearchGroup<MutableValue>> searchGroups, 
            Sort groupSort, Sort withinGroupSort, int maxDocsPerGroup, bool getScores, bool getMaxScores, 
            bool fillSortFields, ValueSource groupByVS, IDictionary /* Map<?, ?> */ vsContext)
            : base(searchGroups, groupSort, withinGroupSort, maxDocsPerGroup, getScores, getMaxScores, fillSortFields)
        {
            this.groupByVS = groupByVS;
            this.vsContext = vsContext;
        }

        protected override AbstractSecondPassGroupingCollector.SearchGroupDocs<MutableValue> RetrieveGroup(int doc)
        {
            filler.FillValue(doc);
            AbstractSecondPassGroupingCollector.SearchGroupDocs<MutableValue> result;
            groupMap.TryGetValue(mval, out result);
            return result;
        }

        public override AtomicReaderContext NextReader
        {
            set
            {
                base.NextReader = value;
                FunctionValues values = groupByVS.GetValues(vsContext, value);
                filler = values.ValueFiller;
                mval = filler.Value;
            }
        }
    }
}
