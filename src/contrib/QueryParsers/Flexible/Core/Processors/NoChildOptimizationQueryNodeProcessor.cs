using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Core.Processors
{
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
                IList<IQueryNode> children = node.Children;

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
