using Lucene.Net.Index;
using Lucene.Net.Queries.Function;
using Lucene.Net.Support;
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
    /// Function based implementation of <see cref="AbstractDistinctValuesCollector"/>.
    /// 
    /// @lucene.experimental
    /// </summary>
    public class FunctionDistinctValuesCollector : AbstractDistinctValuesCollector<FunctionDistinctValuesCollector.GroupCount>
    {
        private readonly IDictionary /* Map<?, ?> */ vsContext;
        private readonly ValueSource groupSource;
        private readonly ValueSource countSource;
        private readonly IDictionary<MutableValue, GroupCount> groupMap;

        private FunctionValues.AbstractValueFiller groupFiller;
        private FunctionValues.AbstractValueFiller countFiller;
        private MutableValue groupMval;
        private MutableValue countMval;

        public FunctionDistinctValuesCollector(IDictionary /*Map<?, ?>*/ vsContext, ValueSource groupSource, ValueSource countSource, IEnumerable<ISearchGroup<MutableValue>> groups)
        {
            this.vsContext = vsContext;
            this.groupSource = groupSource;
            this.countSource = countSource;
            groupMap = new LinkedHashMap<MutableValue, GroupCount>();
            foreach (SearchGroup<MutableValue> group in groups)
            {
                groupMap[group.GroupValue] = new GroupCount(group.GroupValue);
            }
        }

        public override IEnumerable<GroupCount> Groups
        {
            get { return new List<GroupCount>(groupMap.Values); }
        }

        public override void Collect(int doc)
        {
            groupFiller.FillValue(doc);
            GroupCount groupCount;
            if (groupMap.TryGetValue(groupMval, out groupCount))
            {
                countFiller.FillValue(doc);
                ((ISet<MutableValue>)groupCount.UniqueValues).Add(countMval.Duplicate());
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

        /// <summary>
        /// Holds distinct values for a single group.
        /// 
        /// @lucene.experimental
        /// </summary>
        public class GroupCount : AbstractDistinctValuesCollector.GroupCount<MutableValue>
        {
            internal GroupCount(MutableValue groupValue)
                : base(groupValue)
            {
            }
        }
    }
}
