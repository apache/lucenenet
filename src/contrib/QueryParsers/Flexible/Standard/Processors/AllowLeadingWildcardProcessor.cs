using Lucene.Net.QueryParsers.Flexible.Core;
using Lucene.Net.QueryParsers.Flexible.Core.Messages;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Processors;
using Lucene.Net.QueryParsers.Flexible.Core.Util;
using Lucene.Net.QueryParsers.Flexible.Messages;
using Lucene.Net.QueryParsers.Flexible.Standard.Config;
using Lucene.Net.QueryParsers.Flexible.Standard.Nodes;
using Lucene.Net.QueryParsers.Flexible.Standard.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Processors
{
    public class AllowLeadingWildcardProcessor : QueryNodeProcessor
    {
        public AllowLeadingWildcardProcessor()
        {
            // empty constructor
        }

        public override IQueryNode Process(IQueryNode queryTree)
        {
            bool? allowsLeadingWildcard = QueryConfigHandler.Get(StandardQueryConfigHandler.ConfigurationKeys.ALLOW_LEADING_WILDCARD);

            if (allowsLeadingWildcard != null)
            {
                if (!allowsLeadingWildcard.Value)
                {
                    return base.Process(queryTree);
                }
            }

            return queryTree;
        }

        protected override IQueryNode PostProcessNode(IQueryNode node)
        {
            if (node is WildcardQueryNode)
            {
                WildcardQueryNode wildcardNode = (WildcardQueryNode)node;

                if (wildcardNode.Text.Length > 0)
                {
                    // Validate if the wildcard was escaped
                    if (UnescapedCharSequence.WasEscaped(wildcardNode.Text, 0))
                        return node;

                    switch (wildcardNode.Text.CharAt(0))
                    {
                        case '*':
                        case '?':
                            throw new QueryNodeException(new Message(
                                QueryParserMessages.LEADING_WILDCARD_NOT_ALLOWED, node
                                    .ToQueryString(new EscapeQuerySyntaxImpl())));
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
