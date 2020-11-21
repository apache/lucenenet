using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Processors;
using System;
using System.Collections.Generic;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Processors
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
    /// This processor converts every <see cref="Nodes.WildcardQueryNode"/> that is "*:*" to
    /// <see cref="MatchAllDocsQueryNode"/>.
    /// </summary>
    /// <seealso cref="MatchAllDocsQueryNode"/>
    /// <seealso cref="Search.MatchAllDocsQuery"/>
    public class MatchAllDocsQueryNodeProcessor : QueryNodeProcessor
    {
        public MatchAllDocsQueryNodeProcessor()
        {
            // empty constructor
        }

        protected override IQueryNode PostProcessNode(IQueryNode node)
        {
            if (node is FieldQueryNode fqn)
            {
                if (fqn.Field.ToString().Equals("*", StringComparison.Ordinal)
                    && fqn.Text.ToString().Equals("*", StringComparison.Ordinal))
                {
                    return new MatchAllDocsQueryNode();
                }
            }

            return node;
        }

        protected override IQueryNode PreProcessNode(IQueryNode node)
        {
            return node;
        }

        protected override IList<IQueryNode> SetChildrenOrder(IList<IQueryNode> children)
        {
            return children;
        }
    }
}
