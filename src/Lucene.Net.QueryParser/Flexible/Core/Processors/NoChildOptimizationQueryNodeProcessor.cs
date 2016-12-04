using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Core.Processors
{
    /// <summary>
    /// A {@link NoChildOptimizationQueryNodeProcessor} removes every
    /// BooleanQueryNode, BoostQueryNode, TokenizedPhraseQueryNode or
    /// ModifierQueryNode that do not have a valid children.
    /// <para>
    /// Example: When the children of these nodes are removed for any reason then the
    /// nodes may become invalid.
    /// </para>
    /// </summary>
    public class NoChildOptimizationQueryNodeProcessor : QueryNodeProcessorImpl
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
                IList<IQueryNode> children = node.GetChildren();

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
