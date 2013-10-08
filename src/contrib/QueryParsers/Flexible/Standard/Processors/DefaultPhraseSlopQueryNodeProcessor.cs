using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Processors;
using Lucene.Net.QueryParsers.Flexible.Standard.Config;
using Lucene.Net.QueryParsers.Flexible.Standard.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Processors
{
    public class DefaultPhraseSlopQueryNodeProcessor : QueryNodeProcessor
    {
        private bool processChildren = true;

        private int defaultPhraseSlop;

        public DefaultPhraseSlopQueryNodeProcessor()
        {
            // empty constructor
        }

        public override IQueryNode Process(IQueryNode queryTree)
        {
            var queryConfig = QueryConfigHandler;

            if (queryConfig != null)
            {
                int? defaultPhraseSlop = queryConfig.Get(StandardQueryConfigHandler.ConfigurationKeys.PHRASE_SLOP);

                if (defaultPhraseSlop != null)
                {
                    this.defaultPhraseSlop = defaultPhraseSlop.Value;

                    return base.Process(queryTree);
                }
            }

            return queryTree;
        }

        protected override IQueryNode PostProcessNode(IQueryNode node)
        {
            if (node is TokenizedPhraseQueryNode
                || node is MultiPhraseQueryNode)
            {
                return new SlopQueryNode(node, this.defaultPhraseSlop);
            }

            return node;
        }

        protected override IQueryNode PreProcessNode(IQueryNode node)
        {
            if (node is SlopQueryNode)
            {
                this.processChildren = false;
            }

            return node;
        }

        protected override void ProcessChildren(IQueryNode queryTree)
        {
            if (this.processChildren)
            {
                base.ProcessChildren(queryTree);
            }
            else
            {
                this.processChildren = true;
            }
        }

        protected override IList<IQueryNode> SetChildrenOrder(IList<IQueryNode> children)
        {
            return children;
        }
    }
}
