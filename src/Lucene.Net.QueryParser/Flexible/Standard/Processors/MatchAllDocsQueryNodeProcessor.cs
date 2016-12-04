using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Processors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Processors
{
    /// <summary>
    /// This processor converts every {@link WildcardQueryNode} that is "*:*" to
    /// {@link MatchAllDocsQueryNode}.
    /// </summary>
    /// <seealso cref="MatchAllDocsQueryNode"/>
    /// <seealso cref="MatchAllDocsQuery"/>
    public class MatchAllDocsQueryNodeProcessor : QueryNodeProcessorImpl
    {
        public MatchAllDocsQueryNodeProcessor()
        {
            // empty constructor
        }

        protected override IQueryNode PostProcessNode(IQueryNode node)
        {
            if (node is FieldQueryNode)
            {
                FieldQueryNode fqn = (FieldQueryNode)node;

                if (fqn.Field.ToString().Equals("*")
                    && fqn.Text.ToString().Equals("*"))
                {
                    return new MatchAllDocsQueryNode();
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
