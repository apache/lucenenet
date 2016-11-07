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
    /// An implementation of <see cref="AbstractAllGroupHeadsCollector"/> for retrieving the most relevant groups when grouping
    /// by <see cref="ValueSource"/>.
    /// 
    /// @lucene.experimental
    /// </summary>
    public class FunctionAllGroupHeadsCollector : AbstractAllGroupHeadsCollector<FunctionAllGroupHeadsCollector.GroupHead>
    {
        private readonly ValueSource groupBy;
        private readonly IDictionary /* Map<?, ?> */ vsContext;
        private readonly IDictionary<MutableValue, GroupHead> groups;
        private readonly Sort sortWithinGroup;

        private FunctionValues.AbstractValueFiller filler;
        private MutableValue mval;
        private AtomicReaderContext readerContext;
        private Scorer scorer;

        /// <summary>
        /// Constructs a <see cref="FunctionAllGroupHeadsCollector"/> instance.
        /// </summary>
        /// <param name="groupBy">The <see cref="ValueSource"/> to group by</param>
        /// <param name="vsContext">The <see cref="ValueSource"/> context</param>
        /// <param name="sortWithinGroup">The sort within a group</param>
        public FunctionAllGroupHeadsCollector(ValueSource groupBy, IDictionary /* Map<?, ?> */ vsContext, Sort sortWithinGroup)
            : base(sortWithinGroup.GetSort().Length)
        {
            groups = new Dictionary<MutableValue, GroupHead>();
            this.sortWithinGroup = sortWithinGroup;
            this.groupBy = groupBy;
            this.vsContext = vsContext;

            SortField[] sortFields = sortWithinGroup.GetSort();
            for (int i = 0; i < sortFields.Length; i++)
            {
                reversed[i] = sortFields[i].Reverse ? -1 : 1;
            }
        }

        protected override void RetrieveGroupHeadAndAddIfNotExist(int doc)
        {
            filler.FillValue(doc);
            GroupHead groupHead;
            if (!groups.TryGetValue(mval, out groupHead))
            {
                MutableValue groupValue = mval.Duplicate();
                groupHead = new GroupHead(this, groupValue, sortWithinGroup, doc);
                groups[groupValue] = groupHead;
                temporalResult.stop = true;
            }
            else
            {
                temporalResult.stop = false;
            }
            this.temporalResult.groupHead = groupHead;
        }

        protected override ICollection<GroupHead> CollectedGroupHeads
        {
            get { return groups.Values; }
        }

        public override Scorer Scorer
        {
            set
            {
                this.scorer = value;
                foreach (GroupHead groupHead in groups.Values)
                {
                    foreach (FieldComparator comparator in groupHead.comparators)
                    {
                        comparator.Scorer = value;
                    }
                }
            }
        }

        public override AtomicReaderContext NextReader
        {
            set
            {
                this.readerContext = value;
                FunctionValues values = groupBy.GetValues(vsContext, value);
                filler = values.ValueFiller;
                mval = filler.Value;

                foreach (GroupHead groupHead in groups.Values)
                {
                    for (int i = 0; i < groupHead.comparators.Length; i++)
                    {
                        groupHead.comparators[i] = groupHead.comparators[i].SetNextReader(value);
                    }
                }
            }
        }

        /// <summary>
        /// Holds current head document for a single group.
        /// 
        /// @lucene.experimental
        /// </summary>
        public class GroupHead : AbstractAllGroupHeadsCollector_GroupHead
        {

            private readonly FunctionAllGroupHeadsCollector outerInstance;
            // LUCENENET: Moved this here from the base class, AbstractAllGroupHeadsCollector_GroupHead so it doesn't
            // need to reference the generic closing type MutableValue.
            public readonly MutableValue groupValue;
            internal readonly FieldComparator[] comparators;

            internal GroupHead(FunctionAllGroupHeadsCollector outerInstance, MutableValue groupValue, Sort sort, int doc)
                        : base(doc + outerInstance.readerContext.DocBase)
            {
                this.outerInstance = outerInstance;
                this.groupValue = groupValue;

                SortField[] sortFields = sort.GetSort();
                comparators = new FieldComparator[sortFields.Length];
                for (int i = 0; i < sortFields.Length; i++)
                {
                    comparators[i] = sortFields[i].GetComparator(1, i).SetNextReader(outerInstance.readerContext);
                    comparators[i].Scorer = outerInstance.scorer;
                    comparators[i].Copy(0, doc);
                    comparators[i].Bottom = 0;
                }
            }

            public override int Compare(int compIDX, int doc)
            {
                return comparators[compIDX].CompareBottom(doc);
            }

            public override void UpdateDocHead(int doc)
            {
                foreach (FieldComparator comparator in comparators)
                {
                    comparator.Copy(0, doc);
                    comparator.Bottom = 0;
                }
                this.Doc = doc + outerInstance.readerContext.DocBase;
            }
        }
    }
}
