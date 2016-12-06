using Lucene.Net.QueryParsers.Flexible.Core.Builders;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Standard.Builders;
using Lucene.Net.Search;
using Lucene.Net.Search.Spans;
using System.Collections.Generic;

namespace Lucene.Net.QueryParsers.Flexible.Spans
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
    /// This builder creates <see cref="SpanOrQuery"/>s from a <see cref="BooleanQueryNode"/>.
    /// <para/>
    /// It assumes that the <see cref="BooleanQueryNode"/> instance has at least one child.
    /// </summary>
    public class SpanOrQueryNodeBuilder : IStandardQueryBuilder
    {
        public virtual Query Build(IQueryNode node)
        {
            // validates node
            BooleanQueryNode booleanNode = (BooleanQueryNode)node;

            IList<IQueryNode> children = booleanNode.GetChildren();
            SpanQuery[]
            spanQueries = new SpanQuery[children.size()];

            int i = 0;
            foreach (IQueryNode child in children)
            {
                spanQueries[i++] = (SpanQuery)child
                    .GetTag(QueryTreeBuilder.QUERY_TREE_BUILDER_TAGID);
            }

            return new SpanOrQuery(spanQueries);
        }
    }
}
