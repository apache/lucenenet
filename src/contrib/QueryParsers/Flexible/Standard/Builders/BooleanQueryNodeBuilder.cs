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
    public class BooleanQueryNodeBuilder : IStandardQueryBuilder
    {
        public BooleanQueryNodeBuilder()
        {
            // empty constructor
        }

        public Query Build(IQueryNode queryNode)
        {
            BooleanQueryNode booleanNode = (BooleanQueryNode)queryNode;

            BooleanQuery bQuery = new BooleanQuery();
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
                                QueryParserMessages.TOO_MANY_BOOLEAN_CLAUSES, BooleanQuery.MaxClauseCount, queryNode.ToQueryString(new EscapeQuerySyntaxImpl())), ex);

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
                switch (mNode.ModifierValue)
                {
                    case ModifierQueryNode.Modifier.MOD_REQ:
                        return Occur.MUST;
                    case ModifierQueryNode.Modifier.MOD_NOT:
                        return Occur.MUST_NOT;
                    case ModifierQueryNode.Modifier.MOD_NONE:
                        return Occur.SHOULD;
                }
            }

            return Occur.SHOULD;
        }
    }
}
