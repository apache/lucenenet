using Lucene.Net.QueryParsers.Flexible.Core;
using Lucene.Net.QueryParsers.Flexible.Core.Builders;
using Lucene.Net.QueryParsers.Flexible.Core.Messages;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Messages;
using Lucene.Net.QueryParsers.Flexible.Standard.Nodes;
using Lucene.Net.QueryParsers.Flexible.Standard.Parser;
using Lucene.Net.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Builders
{
    public class StandardBooleanQueryNodeBuilder : IStandardQueryBuilder
    {
        public StandardBooleanQueryNodeBuilder()
        {
            // empty constructor
        }

        public Query Build(IQueryNode queryNode)
        {
            StandardBooleanQueryNode booleanNode = (StandardBooleanQueryNode)queryNode;

            BooleanQuery bQuery = new BooleanQuery(booleanNode.IsDisableCoord);
            IList<IQueryNode> children = booleanNode.Children;

            if (children != null)
            {
                foreach (IQueryNode child in children)
                {
                    Object obj = child.GetTag(QueryTreeBuilder.QUERY_TREE_BUILDER_TAGID);

                    if (obj != null)
                    {
                        Query query = (Query)obj;

                        try
                        {
                            bQuery.Add(query, GetModifierValue(child));
                        }
                        catch (BooleanQuery.TooManyClauses ex)
                        {
                            throw new QueryNodeException(new Message(
                                QueryParserMessages.TOO_MANY_BOOLEAN_CLAUSES, BooleanQuery
                                    .MaxClauseCount, queryNode
                                    .ToQueryString(new EscapeQuerySyntaxImpl())), ex);
                        }
                    }
                }
            }

            return bQuery;
        }

        object IQueryBuilder.Build(IQueryNode queryNode)
        {
            return Build(queryNode);
        }

        private static Occur GetModifierValue(IQueryNode node)
        {
            if (node is ModifierQueryNode)
            {
                ModifierQueryNode mNode = ((ModifierQueryNode)node);
                ModifierQueryNode.Modifier modifier = mNode.ModifierValue;

                if (ModifierQueryNode.Modifier.MOD_NONE.Equals(modifier))
                {
                    return Occur.SHOULD;
                }
                else if (ModifierQueryNode.Modifier.MOD_NOT.Equals(modifier))
                {
                    return Occur.MUST_NOT;
                }
                else
                {
                    return Occur.MUST;
                }
            }

            return Occur.SHOULD;
        }
    }
}
