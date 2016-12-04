using Lucene.Net.QueryParsers.Flexible.Core;
using Lucene.Net.QueryParsers.Flexible.Core.Messages;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Messages;
using Lucene.Net.QueryParsers.Flexible.Standard.Parser;
using Lucene.Net.Search;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Builders
{
    /// <summary>
    /// Builds a {@link MatchAllDocsQuery} object from a
    /// {@link MatchAllDocsQueryNode} object.
    /// </summary>
    public class MatchAllDocsQueryNodeBuilder : IStandardQueryBuilder
    {
        public MatchAllDocsQueryNodeBuilder()
        {
            // empty constructor
        }

        public virtual Query Build(IQueryNode queryNode)
        {
            // validates node
            if (!(queryNode is MatchAllDocsQueryNode))
            {
                throw new QueryNodeException(new MessageImpl(
                    QueryParserMessages.LUCENE_QUERY_CONVERSION_ERROR, queryNode
                        .ToQueryString(new EscapeQuerySyntaxImpl()), queryNode.GetType()
                        .Name));
            }

            return new MatchAllDocsQuery();
        }
    }
}
