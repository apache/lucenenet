using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Processors;
using Lucene.Net.QueryParsers.Flexible.Standard.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Processors
{
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
                IList<IQueryNode> children = node.Children;

                if (children != null && children.Count == 1)
                {
                    IQueryNode child = children[0];

                    if (child is ModifierQueryNode)
                    {
                        ModifierQueryNode modNode = (ModifierQueryNode)child;

                        if (modNode is BooleanModifierNode
                            || modNode.ModifierValue == ModifierQueryNode.Modifier.MOD_NONE)
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
