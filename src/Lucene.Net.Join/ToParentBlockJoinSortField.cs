using System;
using Lucene.Net.Search;

namespace Lucene.Net.Join
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
	/// A special sort field that allows sorting parent docs based on nested / child level fields.
	/// Based on the sort order it either takes the document with the lowest or highest field value into account.
	/// 
	/// @lucene.experimental
	/// </summary>
	public class ToParentBlockJoinSortField : SortField
    {
        private readonly bool Order;
        private readonly Filter ParentFilter;
        private readonly Filter ChildFilter;

        /// <summary>
        /// Create ToParentBlockJoinSortField. The parent document ordering is based on child document ordering (reverse).
        /// </summary>
        /// <param name="field"> The sort field on the nested / child level. </param>
        /// <param name="type"> The sort type on the nested / child level. </param>
        /// <param name="reverse"> Whether natural order should be reversed on the nested / child level. </param>
        /// <param name="parentFilter"> Filter that identifies the parent documents. </param>
        /// <param name="childFilter"> Filter that defines which child documents participates in sorting. </param>
        public ToParentBlockJoinSortField(string field, SortFieldType type, bool reverse, Filter parentFilter, Filter childFilter) : base(field, type, reverse)
        {
            Order = reverse;
            ParentFilter = parentFilter;
            ChildFilter = childFilter;
        }

        /// <summary>
        /// Create ToParentBlockJoinSortField.
        /// </summary>
        /// <param name="field"> The sort field on the nested / child level. </param>
        /// <param name="type"> The sort type on the nested / child level. </param>
        /// <param name="reverse"> Whether natural order should be reversed on the nested / child document level. </param>
        /// <param name="order"> Whether natural order should be reversed on the parent level. </param>
        /// <param name="parentFilter"> Filter that identifies the parent documents. </param>
        /// <param name="childFilter"> Filter that defines which child documents participates in sorting. </param>
        public ToParentBlockJoinSortField(string field, SortFieldType type, bool reverse, bool order, Filter parentFilter, Filter childFilter) 
            : base(field, type, reverse)
        {
            Order = order;
            ParentFilter = parentFilter;
            ChildFilter = childFilter;
        }
        
        public override FieldComparer GetComparer(int numHits, int sortPos)
        {
            var wrappedFieldComparer = base.GetComparer(numHits + 1, sortPos);
            if (Order)
            {
                return new ToParentBlockJoinFieldComparer.Highest(wrappedFieldComparer, ParentFilter, ChildFilter, numHits);
            }

            return new ToParentBlockJoinFieldComparer.Lowest(wrappedFieldComparer, ParentFilter, ChildFilter, numHits);
        }
    }
}