using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Processors;
using Lucene.Net.QueryParsers.Flexible.Standard.Nodes;
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
    /// This processor removes every <see cref="BooleanQueryNode"/> that contains only one
    /// child and returns this child. If this child is <see cref="ModifierQueryNode"/> that
    /// was defined by the user. A modifier is not defined by the user when it's a
    /// <see cref="BooleanModifierNode"/>
    /// </summary>
    /// <seealso cref="ModifierQueryNode"/>
    public class BooleanSingleChildOptimizationQueryNodeProcessor : QueryNodeProcessor
    {
        public BooleanSingleChildOptimizationQueryNodeProcessor()
        {
            // empty constructor
        }

        protected override IQueryNode PostProcessNode(IQueryNode node)
        {
            if (node is BooleanQueryNode)
            {
                IList<IQueryNode> children = node.GetChildren();

                if (children != null && children.Count == 1)
                {
                    IQueryNode child = children[0];

                    if (child is ModifierQueryNode modNode)
                    {
                        if (modNode is BooleanModifierNode
                            || modNode.Modifier == Modifier.MOD_NONE)
                        {
                            return child;
                        }
                    }
                    else
                    {
                        return child;
                    }
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
