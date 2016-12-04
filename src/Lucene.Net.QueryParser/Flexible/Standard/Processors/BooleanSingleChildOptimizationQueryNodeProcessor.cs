using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Processors;
using Lucene.Net.QueryParsers.Flexible.Standard.Nodes;
using System.Collections.Generic;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Processors
{
    /// <summary>
    /// This processor removes every {@link BooleanQueryNode} that contains only one
    /// child and returns this child. If this child is {@link ModifierQueryNode} that
    /// was defined by the user. A modifier is not defined by the user when it's a
    /// {@link BooleanModifierNode}
    /// </summary>
    /// <seealso cref="ModifierQueryNode"/>
    public class BooleanSingleChildOptimizationQueryNodeProcessor : QueryNodeProcessorImpl
    {
        public BooleanSingleChildOptimizationQueryNodeProcessor()
        {
            // empty constructor
        }

        protected override IQueryNode PostProcessNode(IQueryNode node)
        {
            if (node is BooleanQueryNode)
            {
                IList<IQueryNode> children = node.GetChildren();

                if (children != null && children.Count == 1)
                {
                    IQueryNode child = children[0];

                    if (child is ModifierQueryNode)
                    {
                        ModifierQueryNode modNode = (ModifierQueryNode)child;

                        if (modNode is BooleanModifierNode
                            || modNode.Modifier == Modifier.MOD_NONE)
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
