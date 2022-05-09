using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Processors;
using Lucene.Net.Util;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

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
    /// This processor removes every <see cref="IQueryNode"/> that is not a leaf and has not
    /// children. If after processing the entire tree the root node is not a leaf and
    /// has no children, a <see cref="MatchNoDocsQueryNode"/> object is returned.
    /// <para/>
    /// This processor is used at the end of a pipeline to avoid invalid query node
    /// tree structures like a <see cref="GroupQueryNode"/> or <see cref="ModifierQueryNode"/>
    /// with no children.
    /// </summary>
    /// <seealso cref="IQueryNode"/>
    /// <seealso cref="MatchNoDocsQueryNode"/>
    public class RemoveEmptyNonLeafQueryNodeProcessor : QueryNodeProcessor
    {
        private readonly JCG.List<IQueryNode> childrenBuffer = new JCG.List<IQueryNode>(); // LUCENENET: marked readonly

        public RemoveEmptyNonLeafQueryNodeProcessor()
        {
            // empty constructor
        }

        public override IQueryNode Process(IQueryNode queryTree)
        {
            queryTree = base.Process(queryTree);

            if (!queryTree.IsLeaf)
            {
                IList<IQueryNode> children = queryTree.GetChildren();

                if (children is null || children.Count == 0)
                {
                    return new MatchNoDocsQueryNode();
                }
            }

            return queryTree;
        }

        protected override IQueryNode PostProcessNode(IQueryNode node)
        {
            return node;
        }

        protected override IQueryNode PreProcessNode(IQueryNode node)
        {
            return node;
        }

        protected override IList<IQueryNode> SetChildrenOrder(IList<IQueryNode> children)
        {
            try
            {
                foreach (IQueryNode child in children)
                {
                    if (!child.IsLeaf)
                    {
                        IList<IQueryNode> grandChildren = child.GetChildren();

                        if (grandChildren != null && grandChildren.Count > 0)
                        {
                            this.childrenBuffer.Add(child);
                        }
                    }
                    else
                    {
                        this.childrenBuffer.Add(child);
                    }
                }

                children.Clear();
                children.AddRange(this.childrenBuffer);
            }
            finally
            {
                this.childrenBuffer.Clear();
            }

            return children;
        }
    }
}
