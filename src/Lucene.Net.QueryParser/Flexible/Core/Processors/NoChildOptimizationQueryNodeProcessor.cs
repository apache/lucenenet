using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using System.Collections.Generic;

namespace Lucene.Net.QueryParsers.Flexible.Core.Processors
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
    /// A <see cref="NoChildOptimizationQueryNodeProcessor"/> removes every
    /// BooleanQueryNode, BoostQueryNode, TokenizedPhraseQueryNode or
    /// ModifierQueryNode that do not have a valid children.
    /// <para>
    /// Example: When the children of these nodes are removed for any reason then the
    /// nodes may become invalid.
    /// </para>
    /// </summary>
    public class NoChildOptimizationQueryNodeProcessor : QueryNodeProcessor
    {
        public NoChildOptimizationQueryNodeProcessor()
        {
            // empty constructor
        }

        protected override IQueryNode PostProcessNode(IQueryNode node)
        {
            if (node is BooleanQueryNode || node is BoostQueryNode
                || node is TokenizedPhraseQueryNode
                || node is ModifierQueryNode)
            {
                IList<IQueryNode> children = node.GetChildren();

                if (children != null && children.Count > 0)
                {
                    foreach (IQueryNode child in children)
                    {
                        if (!(child is DeletedQueryNode))
                        {
                            return node;
                        }
                    }
                }

                return new MatchNoDocsQueryNode();
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
