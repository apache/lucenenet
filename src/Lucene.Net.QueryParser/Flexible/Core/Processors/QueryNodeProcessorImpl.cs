using Lucene.Net.QueryParsers.Flexible.Core.Config;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using System;
using System.Collections.Generic;
using JCG = J2N.Collections.Generic;

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
    /// This is a default implementation for the <see cref="IQueryNodeProcessor"/>
    /// interface, it's an abstract class, so it should be extended by classes that
    /// want to process a <see cref="IQueryNode"/> tree.
    /// <para>
    /// This class process <see cref="IQueryNode"/>s from left to right in the tree. While
    /// it's walking down the tree, for every node,
    /// <see cref="PreProcessNode(IQueryNode)"/> is invoked. After a node's children are
    /// processed, <see cref="PostProcessNode(IQueryNode)"/> is invoked for that node.
    /// <see cref="SetChildrenOrder(IList{IQueryNode})"/> is invoked before
    /// <see cref="PostProcessNode(IQueryNode)"/> only if the node has at least one child,
    /// in <see cref="SetChildrenOrder(IList{IQueryNode})"/> the implementor might redefine the
    /// children order or remove any children from the children list.
    /// </para>
    /// <para>
    /// Here is an example about how it process the nodes:
    /// </para>
    /// <pre>
    ///      a
    ///     / \
    ///    b   e
    ///   / \
    ///  c   d
    /// </pre>
    /// <para>
    /// Here is the order the methods would be invoked for the tree described above:
    /// </para>
    /// <code>
    ///     PreProcessNode( a );
    ///     PreProcessNode( b );
    ///     PreProcessNode( c );
    ///     PostProcessNode( c );
    ///     PreProcessNode( d );
    ///     PostProcessNode( d );
    ///     SetChildrenOrder( bChildrenList );
    ///     PostProcessNode( b );
    ///     PreProcessNode( e );
    ///     PostProcessNode( e );
    ///     SetChildrenOrder( aChildrenList );
    ///     PostProcessNode( a )
    /// </code>
    /// </summary>
    /// <seealso cref="IQueryNodeProcessor"/>
    public abstract class QueryNodeProcessor : IQueryNodeProcessor
    {
        private readonly IList<ChildrenList> childrenListPool = new JCG.List<ChildrenList>(); // LUCENENET: marked readonly

        private QueryConfigHandler queryConfig;

        protected QueryNodeProcessor() // LUCENENET: CA1012: Abstract types should not have constructors (marked protected)
        {
            // empty constructor
        }

        protected QueryNodeProcessor(QueryConfigHandler queryConfigHandler) // LUCENENET: CA1012: Abstract types should not have constructors (marked protected)
        {
            this.queryConfig = queryConfigHandler;
        }

        public virtual IQueryNode Process(IQueryNode queryTree)
        {
            return ProcessIteration(queryTree);
        }

        private IQueryNode ProcessIteration(IQueryNode queryTree)
        {
            queryTree = PreProcessNode(queryTree);

            ProcessChildren(queryTree);

            queryTree = PostProcessNode(queryTree);

            return queryTree;
        }

        /// <summary>
        /// This method is called every time a child is processed.
        /// </summary>
        /// <param name="queryTree">the query node child to be processed</param>
        /// <exception cref="QueryNodeException">if something goes wrong during the query node processing</exception>
        protected virtual void ProcessChildren(IQueryNode queryTree)
        {
            IList<IQueryNode> children = queryTree.GetChildren();
            ChildrenList newChildren;

            if (children != null && children.Count > 0)
            {
                newChildren = AllocateChildrenList();

                try
                {
                    foreach (IQueryNode child in children)
                    {
                        var child2 = ProcessIteration(child);

                        if (child2 is null)
                        {
                            // LUCENENET: Changed from NullPointerException to InvalidOperationException (which isn't caught anywhere outside of tests)
                            throw IllegalStateException.Create($"{this.GetType().Name}.PostProcessNode() must not return 'null'.");
                        }

                        newChildren.Add(child2);
                    }

                    IList<IQueryNode> orderedChildrenList = SetChildrenOrder(newChildren);

                    queryTree.Set(orderedChildrenList);
                }
                finally
                {
                    newChildren.beingUsed = false;
                }
            }
        }

        private ChildrenList AllocateChildrenList()
        {
            ChildrenList list = null;

            foreach (ChildrenList auxList in this.childrenListPool)
            {
                if (!auxList.beingUsed)
                {
                    list = auxList;
                    list.Clear();

                    break;
                }
            }

            if (list is null)
            {
                list = new ChildrenList();
                this.childrenListPool.Add(list);
            }

            list.beingUsed = true;

            return list;
        }

        /// <summary>
        /// For reference about this method check:
        /// <see cref="IQueryNodeProcessor.SetQueryConfigHandler(QueryConfigHandler)"/>.
        /// </summary>
        /// <param name="queryConfigHandler">the query configuration handler to be set.</param>
        /// <seealso cref="IQueryNodeProcessor.SetQueryConfigHandler(QueryConfigHandler)"/>
        /// <seealso cref="QueryConfigHandler"/>
        public virtual void SetQueryConfigHandler(QueryConfigHandler queryConfigHandler)
        {
            this.queryConfig = queryConfigHandler;
        }

        /// <summary>
        /// For reference about this method check:
        /// <see cref="IQueryNodeProcessor.GetQueryConfigHandler()"/>.
        /// </summary>
        /// <returns><see cref="QueryConfigHandler"/> the query configuration handler to be set.</returns>
        /// <seealso cref="IQueryNodeProcessor.SetQueryConfigHandler(QueryConfigHandler)"/>
        /// <seealso cref="QueryConfigHandler"/>
        public virtual QueryConfigHandler GetQueryConfigHandler()
        {
            return this.queryConfig;
        }

        /// <summary>
        /// This method is invoked for every node when walking down the tree.
        /// </summary>
        /// <param name="node">the query node to be pre-processed</param>
        /// <returns>a query node</returns>
        /// <exception cref="QueryNodeException">if something goes wrong during the query node processing</exception>
        protected abstract IQueryNode PreProcessNode(IQueryNode node);

        /// <summary>
        /// This method is invoked for every node when walking up the tree.
        /// </summary>
        /// <param name="node">node the query node to be post-processed</param>
        /// <returns>a query node</returns>
        /// <exception cref="QueryNodeException">if something goes wrong during the query node processing</exception>
        protected abstract IQueryNode PostProcessNode(IQueryNode node);

        /// <summary>
        /// This method is invoked for every node that has at least on child. It's
        /// invoked right before <see cref="PostProcessNode(IQueryNode)"/> is invoked.
        /// </summary>
        /// <param name="children">the list containing all current node's children</param>
        /// <returns>a new list containing all children that should be set to the current node</returns>
        /// <exception cref="QueryNodeException">if something goes wrong during the query node processing</exception>
        protected abstract IList<IQueryNode> SetChildrenOrder(IList<IQueryNode> children);

        private class ChildrenList : JCG.List<IQueryNode>
        {
            internal bool beingUsed;
        }
    }
}
