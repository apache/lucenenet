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
    /// <summary>
    /// This processor removes invalid {@link SlopQueryNode} objects in the query
    /// node tree. A {@link SlopQueryNode} is invalid if its child is neither a
    /// {@link TokenizedPhraseQueryNode} nor a {@link MultiPhraseQueryNode}.
    /// </summary>
    /// <seealso cref="SlopQueryNode"/>
    public class PhraseSlopQueryNodeProcessor : QueryNodeProcessorImpl
    {
        public PhraseSlopQueryNodeProcessor()
        {
            // empty constructor
        }

        protected override IQueryNode PostProcessNode(IQueryNode node)
        {
            if (node is SlopQueryNode)
            {
                SlopQueryNode phraseSlopNode = (SlopQueryNode)node;

                if (!(phraseSlopNode.GetChild() is TokenizedPhraseQueryNode)
                    && !(phraseSlopNode.GetChild() is MultiPhraseQueryNode))
                {
                    return phraseSlopNode.GetChild();
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
