using Lucene.Net.QueryParsers.Flexible.Core;
using Lucene.Net.QueryParsers.Flexible.Core.Builders;
using Lucene.Net.QueryParsers.Flexible.Core.Messages;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Messages;
using Lucene.Net.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Builders
{
    /// <summary>
    /// Builds a BooleanQuery of SHOULD clauses, possibly with
    /// some minimum number to match.
    /// </summary>
    public class AnyQueryNodeBuilder : IStandardQueryBuilder
    {
        public AnyQueryNodeBuilder()
        {
            // empty constructor
        }

        public virtual Query Build(IQueryNode queryNode)
        {
            AnyQueryNode andNode = (AnyQueryNode)queryNode;

            BooleanQuery bQuery = new BooleanQuery();
            IList<IQueryNode> children = andNode.GetChildren();

            if (children != null)
            {
                foreach (IQueryNode child in children)
                {
                    object obj = child.GetTag(QueryTreeBuilder<Query>.QUERY_TREE_BUILDER_TAGID);

                    if (obj != null)
                    {
                        Query query = (Query)obj;

                        try
                        {
                            bQuery.Add(query, BooleanClause.Occur.SHOULD);
                        }
                        catch (BooleanQuery.TooManyClauses ex)
                        {
                            throw new QueryNodeException(new MessageImpl(
                               /*
                                * IQQQ.Q0028E_TOO_MANY_BOOLEAN_CLAUSES,
                                * BooleanQuery.getMaxClauseCount()
                                */QueryParserMessages.EMPTY_MESSAGE), ex);
                        }
                    }
                }
            }

            bQuery.MinimumNumberShouldMatch = andNode.MinimumMatchingElements;

            return bQuery;
        }
    }
}
