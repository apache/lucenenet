using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Processors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Processors
{
    public class RemoveEmptyNonLeafQueryNodeProcessor : QueryNodeProcessor
    {
        private List<IQueryNode> childrenBuffer = new List<IQueryNode>();

        public RemoveEmptyNonLeafQueryNodeProcessor()
        {
            // empty constructor
        }

        public override IQueryNode Process(IQueryNode queryTree)
        {
            queryTree = base.Process(queryTree);

            if (!queryTree.IsLeaf)
            {
                IList<IQueryNode> children = queryTree.Children;

                if (children == null || children.Count == 0)
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
                        IList<IQueryNode> grandChildren = child.Children;

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

                // IList<T> doesn't have AddRange or AddAll
                foreach (var child in childrenBuffer)
                {
                    children.Add(child);
                }
            }
            finally
            {
                this.childrenBuffer.Clear();
            }

            return children;
        }
    }
}
