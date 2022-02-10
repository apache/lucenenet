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
    /// A <see cref="QueryNodeProcessorPipeline"/> class removes every instance of
    /// <see cref="DeletedQueryNode"/> from a query node tree. If the resulting root node
    /// is a <see cref="DeletedQueryNode"/>, <see cref="MatchNoDocsQueryNode"/> is returned.
    /// </summary>
    public class RemoveDeletedQueryNodesProcessor : QueryNodeProcessor
    {
        public RemoveDeletedQueryNodesProcessor()
        {
            // empty constructor
        }

        public override IQueryNode Process(IQueryNode queryTree)
        {
            queryTree = base.Process(queryTree);

            if (queryTree is DeletedQueryNode
                && !(queryTree is MatchNoDocsQueryNode))
            {
                return new MatchNoDocsQueryNode();
            }

            return queryTree;
        }

        protected override IQueryNode PostProcessNode(IQueryNode node)
        {
            if (!node.IsLeaf)
            {
                IList<IQueryNode> children = node.GetChildren();
                bool removeBoolean; // LUCENENET: IDE0059: Remove unnecessary value assignment

                if (children is null || children.Count == 0)
                {
                    removeBoolean = true;
                }
                else
                {
                    removeBoolean = true;

                    foreach (var child in children)
                    {

                        if (!(child is DeletedQueryNode))
                        {
                            removeBoolean = false;
                            break;
                        }
                    }
                }

                if (removeBoolean)
                {
                    return new DeletedQueryNode();
                }
            }

            return node;
        }

        protected override IList<IQueryNode> SetChildrenOrder(IList<IQueryNode> children)
        {
            for (int i = 0; i < children.Count; i++)
            {
                if (children[i] is DeletedQueryNode)
                {
                    children.RemoveAt(i--);
                }
            }

            return children;
        }

        protected override IQueryNode PreProcessNode(IQueryNode node)
        {
            return node;
        }
    }
}
