using Lucene.Net.QueryParsers.Flexible.Core;
using Lucene.Net.QueryParsers.Flexible.Core.Builders;
using Lucene.Net.QueryParsers.Flexible.Core.Messages;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Messages;
using Lucene.Net.QueryParsers.Flexible.Standard.Parser;
using Lucene.Net.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Builders
{
    public class MatchNoDocsQueryNodeBuilder : IStandardQueryBuilder
    {
        public MatchNoDocsQueryNodeBuilder()
        {
            // empty constructor
        }

        public Query Build(IQueryNode queryNode)
        {
            // validates node
            if (!(queryNode is MatchNoDocsQueryNode))
            {
                throw new QueryNodeException(new Message(
                    QueryParserMessages.LUCENE_QUERY_CONVERSION_ERROR, queryNode
                        .ToQueryString(new EscapeQuerySyntaxImpl()), queryNode.GetType().FullName));
            }

            return new BooleanQuery();
        }

        object IQueryBuilder.Build(IQueryNode queryNode)
        {
            return Build(queryNode);
        }
    }
}
