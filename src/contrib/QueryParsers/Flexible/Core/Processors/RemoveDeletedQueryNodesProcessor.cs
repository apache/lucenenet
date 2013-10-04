using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Core.Processors
{
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
                IList<IQueryNode> children = node.Children;
                bool removeBoolean = false;

                if (children == null || children.Count == 0)
                {
                    removeBoolean = true;
                }
                else
                {
                    removeBoolean = true;

                    foreach (IQueryNode child in children)
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
