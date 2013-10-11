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
    public class PhraseSlopQueryNodeProcessor : QueryNodeProcessor
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

                if (!(phraseSlopNode.Child is TokenizedPhraseQueryNode)
                    && !(phraseSlopNode.Child is MultiPhraseQueryNode))
                {
                    return phraseSlopNode.Child;
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
