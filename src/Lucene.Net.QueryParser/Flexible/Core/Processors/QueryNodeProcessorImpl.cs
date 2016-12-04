using Lucene.Net.QueryParsers.Flexible.Core.Config;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using System;
using System.Collections.Generic;

namespace Lucene.Net.QueryParsers.Flexible.Core.Processors
{
    public abstract class QueryNodeProcessorImpl : IQueryNodeProcessor
    {
        private List<ChildrenList> childrenListPool = new List<ChildrenList>();

        private QueryConfigHandler queryConfig;

        public QueryNodeProcessorImpl()
        {
            // empty constructor
        }

        public QueryNodeProcessorImpl(QueryConfigHandler queryConfigHandler)
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

        /**
         * This method is called every time a child is processed.
         * 
         * @param queryTree
         *          the query node child to be processed
         * @throws QueryNodeException
         *           if something goes wrong during the query node processing
         */
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

                        if (child2 == null)
                        {
                            throw new NullReferenceException();

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

            if (list == null)
            {
                list = new ChildrenList();
                this.childrenListPool.Add(list);
            }

            list.beingUsed = true;

            return list;
        }

        /**
         * For reference about this method check:
         * {@link QueryNodeProcessor#setQueryConfigHandler(QueryConfigHandler)}.
         * 
         * @param queryConfigHandler
         *          the query configuration handler to be set.
         * 
         * @see QueryNodeProcessor#getQueryConfigHandler()
         * @see QueryConfigHandler
         */
        public virtual void SetQueryConfigHandler(QueryConfigHandler queryConfigHandler)
        {
            this.queryConfig = queryConfigHandler;
        }

        /**
         * For reference about this method check:
         * {@link QueryNodeProcessor#getQueryConfigHandler()}.
         * 
         * @return QueryConfigHandler the query configuration handler to be set.
         * 
         * @see QueryNodeProcessor#setQueryConfigHandler(QueryConfigHandler)
         * @see QueryConfigHandler
         */
        public virtual QueryConfigHandler GetQueryConfigHandler()
        {
            return this.queryConfig;
        }

        /**
         * This method is invoked for every node when walking down the tree.
         * 
         * @param node
         *          the query node to be pre-processed
         * 
         * @return a query node
         * 
         * @throws QueryNodeException
         *           if something goes wrong during the query node processing
         */
        protected abstract IQueryNode PreProcessNode(IQueryNode node);

        /**
         * This method is invoked for every node when walking up the tree.
         * 
         * @param node
         *          node the query node to be post-processed
         * 
         * @return a query node
         * 
         * @throws QueryNodeException
         *           if something goes wrong during the query node processing
         */
        protected abstract IQueryNode PostProcessNode(IQueryNode node);

        /**
         * This method is invoked for every node that has at least on child. It's
         * invoked right before {@link #postProcessNode(QueryNode)} is invoked.
         * 
         * @param children
         *          the list containing all current node's children
         * 
         * @return a new list containing all children that should be set to the
         *         current node
         * 
         * @throws QueryNodeException
         *           if something goes wrong during the query node processing
         */
        protected abstract IList<IQueryNode> SetChildrenOrder(IList<IQueryNode> children);

        private class ChildrenList : List<IQueryNode>
        {
            internal bool beingUsed;
        }
    }
}
