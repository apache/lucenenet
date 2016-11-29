using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Core.Processors
{
    /// <summary>
    /// A {@link QueryNodeProcessorPipeline} class removes every instance of
    /// {@link DeletedQueryNode} from a query node tree. If the resulting root node
    /// is a {@link DeletedQueryNode}, {@link MatchNoDocsQueryNode} is returned.
    /// </summary>
    public class RemoveDeletedQueryNodesProcessor : QueryNodeProcessorImpl
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
                bool removeBoolean = false;

                if (children == null || children.Count == 0)
                {
                    removeBoolean = true;

                }
                else
                {
                    removeBoolean = true;

                    for (IEnumerator<IQueryNode> it = children.GetEnumerator(); it.MoveNext();)
                    {

                        if (!(it.Current is DeletedQueryNode))
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
