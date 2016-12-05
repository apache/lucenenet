using Lucene.Net.QueryParsers.Flexible.Core.Nodes;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Nodes
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
    /// This query node represents a range query composed by <see cref="FieldQueryNode"/>
    /// bounds, which means the bound values are strings.
    /// </summary>
    /// <seealso cref="FieldQueryNode"/>
    /// <seealso cref="AbstractRangeQueryNode{T}"/>
    public class TermRangeQueryNode : AbstractRangeQueryNode<FieldQueryNode>
    {
        /// <summary>
        /// Constructs a <see cref="TermRangeQueryNode"/> object using the given
        /// <see cref="FieldQueryNode"/> as its bounds.
        /// </summary>
        /// <param name="lower">the lower bound</param>
        /// <param name="upper">the upper bound</param>
        /// <param name="lowerInclusive"><c>true</c> if the lower bound is inclusive, otherwise, <c>false</c></param>
        /// <param name="upperInclusive"><c>true</c> if the upper bound is inclusive, otherwise, <c>false</c></param>
        public TermRangeQueryNode(FieldQueryNode lower, FieldQueryNode upper,
            bool lowerInclusive, bool upperInclusive)
        {
            SetBounds(lower, upper, lowerInclusive, upperInclusive);
        }
    }
}
