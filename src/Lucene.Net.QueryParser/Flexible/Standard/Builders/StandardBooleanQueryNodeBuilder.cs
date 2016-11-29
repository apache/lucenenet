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
    /// <summary>
    /// This builder does the same as the {@link BooleanQueryNodeBuilder}, but this
    /// considers if the built {@link BooleanQuery} should have its coord disabled or
    /// not.
    /// </summary>
    /// <seealso cref="BooleanQueryNodeBuilder"/>
    /// <seealso cref="BooleanQuery"/>
    /// <seealso cref="Similarity#coord(int, int)"/>
    public class StandardBooleanQueryNodeBuilder : IStandardQueryBuilder
    {
        public StandardBooleanQueryNodeBuilder()
        {
            // empty constructor
        }


        public virtual Query Build(IQueryNode queryNode)
        {
            StandardBooleanQueryNode booleanNode = (StandardBooleanQueryNode)queryNode;

            BooleanQuery bQuery = new BooleanQuery(booleanNode.DisableCoord);
            IList<IQueryNode> children = booleanNode.GetChildren();

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
                            bQuery.Add(query, GetModifierValue(child));
                        }
                        catch (BooleanQuery.TooManyClauses ex)
                        {

                            throw new QueryNodeException(new MessageImpl(
                                QueryParserMessages.TOO_MANY_BOOLEAN_CLAUSES, BooleanQuery
                                    .MaxClauseCount, queryNode
                                    .ToQueryString(new EscapeQuerySyntaxImpl())), ex);

                        }

                    }

                }

            }

            return bQuery;

        }

        ///// <summary>
        ///// LUCENENET specific overload for supporting IQueryBuilder
        ///// </summary>
        //object IQueryBuilder.Build(IQueryNode queryNode)
        //{
        //    return Build(queryNode);
        //}

        private static BooleanClause.Occur GetModifierValue(IQueryNode node)
        {

            if (node is ModifierQueryNode)
            {
                ModifierQueryNode mNode = ((ModifierQueryNode)node);
                Modifier modifier = mNode.Modifier;

                if (Modifier.MOD_NONE.Equals(modifier))
                {
                    return BooleanClause.Occur.SHOULD;

                }
                else if (Modifier.MOD_NOT.Equals(modifier))
                {
                    return BooleanClause.Occur.MUST_NOT;

                }
                else
                {
                    return BooleanClause.Occur.MUST;
                }
            }

            return BooleanClause.Occur.SHOULD;

        }

    }
}
