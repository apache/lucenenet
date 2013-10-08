using Lucene.Net.QueryParsers.Flexible.Core.Config;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Core.Processors
{
    public abstract class QueryNodeProcessor : IQueryNodeProcessor
    {
        private List<ChildrenList> childrenListPool = new List<ChildrenList>();

        private QueryConfigHandler queryConfig;

        public QueryNodeProcessor()
        {
            // empty constructor
        }

        public QueryNodeProcessor(QueryConfigHandler queryConfigHandler)
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

        protected void ProcessChildren(IQueryNode queryTree)
        {
            IList<IQueryNode> children = queryTree.Children;
            ChildrenList newChildren;

            if (children != null && children.Count > 0)
            {
                newChildren = AllocateChildrenList();

                try
                {
                    IQueryNode child;

                    // .NET Port: can't modify range variable, changed to for loop
                    for (int i = 0; i < children.Count; i++)
                    {
                        child = children[i];

                        child = ProcessIteration(child);

                        if (child == null)
                        {
                            throw new NullReferenceException();
                        }

                        newChildren.Add(child);
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

        public virtual QueryConfigHandler QueryConfigHandler
        {
            get
            {
                return this.queryConfig;
            }
            set
            {
                this.queryConfig = value;
            }
        }

        protected abstract IQueryNode PreProcessNode(IQueryNode node);

        protected abstract IQueryNode PostProcessNode(IQueryNode node);

        protected abstract IList<IQueryNode> SetChildrenOrder(IList<IQueryNode> children);

        private class ChildrenList : List<IQueryNode>
        {
            internal bool beingUsed;
        }
    }
}
