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
    /// A collector that collects all groups that match the
    /// query. Only the group value is collected, and the order
    /// is undefined.  This collector does not determine
    /// the most relevant document of a group.
    /// 
    /// <para>
    /// Implementation detail: Uses <see cref="ValueSource"/> and <see cref="FunctionValues"/> to retrieve the
    /// field values to group by.
    /// </para>
    /// @lucene.experimental
    /// </summary>
    public class FunctionAllGroupsCollector : AbstractAllGroupsCollector<MutableValue>
    {
        private readonly IDictionary /* Map<?, ?> */ vsContext;
        private readonly ValueSource groupBy;
        private readonly SortedSet<MutableValue> groups = new SortedSet<MutableValue>();

        private FunctionValues.AbstractValueFiller filler;
        private MutableValue mval;

        /// <summary>
        /// Constructs a <see cref="FunctionAllGroupsCollector"/> instance.
        /// </summary>
        /// <param name="groupBy">The <see cref="ValueSource"/> to group by</param>
        /// <param name="vsContext">The <see cref="ValueSource"/> context</param>
        public FunctionAllGroupsCollector(ValueSource groupBy, IDictionary /* Map<?, ?> */ vsContext)
        {
            this.vsContext = vsContext;
            this.groupBy = groupBy;
        }

        public override IEnumerable<MutableValue> Groups
        {
            get
            {
                return groups;
            }
        }

        public override void Collect(int doc)
        {
            filler.FillValue(doc);
            if (!groups.Contains(mval))
            {
                groups.Add(mval.Duplicate());
            }
        }

        public override AtomicReaderContext NextReader
        {
            set
            {
                FunctionValues values = groupBy.GetValues(vsContext, value);
                filler = values.ValueFiller;
                mval = filler.Value;
            }
        }
    }
}
