using Lucene.Net.QueryParsers.Flexible.Core.Config;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Processors;
using Lucene.Net.QueryParsers.Flexible.Standard.Config;
using Lucene.Net.QueryParsers.Flexible.Standard.Nodes;
using System.Collections.Generic;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Processors
{
    /// <summary>
    /// This processor verifies if {@link ConfigurationKeys#PHRASE_SLOP}
    /// is defined in the {@link QueryConfigHandler}. If it is, it looks for every
    /// {@link TokenizedPhraseQueryNode} and {@link MultiPhraseQueryNode} that does
    /// not have any {@link SlopQueryNode} applied to it and creates an
    /// {@link SlopQueryNode} and apply to it. The new {@link SlopQueryNode} has the
    /// same slop value defined in the configuration.
    /// </summary>
    /// <seealso cref="SlopQueryNode"/>
    /// <seealso cref="ConfigurationKeys#PHRASE_SLOP"/>
    public class DefaultPhraseSlopQueryNodeProcessor : QueryNodeProcessorImpl
    {
        private bool processChildren = true;

        private int defaultPhraseSlop;

        public DefaultPhraseSlopQueryNodeProcessor()
        {
            // empty constructor
        }

        public override IQueryNode Process(IQueryNode queryTree)
        {
            QueryConfigHandler queryConfig = GetQueryConfigHandler();

            if (queryConfig != null)
            {
                int? defaultPhraseSlop = queryConfig.Get(ConfigurationKeys.PHRASE_SLOP);

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
