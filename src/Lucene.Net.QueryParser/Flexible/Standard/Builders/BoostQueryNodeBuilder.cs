using Lucene.Net.QueryParsers.Flexible.Core.Builders;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.Search;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Builders
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
    /// This builder basically reads the <see cref="Query"/> object set on the
    /// <see cref="BoostQueryNode"/> child using
    /// <see cref="QueryTreeBuilder.QUERY_TREE_BUILDER_TAGID"/> and applies the boost value
    /// defined in the <see cref="BoostQueryNode"/>.
    /// </summary>
    public class BoostQueryNodeBuilder : IStandardQueryBuilder
    {
        public BoostQueryNodeBuilder()
        {
            // empty constructor
        }

        public virtual Query Build(IQueryNode queryNode)
        {
            BoostQueryNode boostNode = (BoostQueryNode)queryNode;
            IQueryNode child = boostNode.Child;

            if (child is null)
            {
                return null;
            }

            Query query = (Query)child
                .GetTag(QueryTreeBuilder.QUERY_TREE_BUILDER_TAGID);
            query.Boost = boostNode.Value;

            return query;
        }
    }
}
