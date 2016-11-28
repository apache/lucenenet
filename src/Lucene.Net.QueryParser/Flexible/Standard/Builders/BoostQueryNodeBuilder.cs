using Lucene.Net.QueryParsers.Flexible.Core.Builders;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Builders
{
    /// <summary>
    /// This builder basically reads the {@link Query} object set on the
    /// {@link BoostQueryNode} child using
    /// {@link QueryTreeBuilder#QUERY_TREE_BUILDER_TAGID} and applies the boost value
    /// defined in the {@link BoostQueryNode}.
    /// </summary>
    public class BoostQueryNodeBuilder : IStandardQueryBuilder
    {
        public BoostQueryNodeBuilder()
        {
            // empty constructor
        }

        public virtual Query Build(IQueryNode queryNode)
        {
            BoostQueryNode boostNode = (BoostQueryNode)queryNode;
            IQueryNode child = boostNode.GetChild();

            if (child == null)
            {
                return null;
            }

            Query query = (Query)child
                .GetTag(QueryTreeBuilder.QUERY_TREE_BUILDER_TAGID);
            query.Boost = boostNode.GetValue();

            return query;

        }

        /// <summary>
        /// LUCENENET specific overload for supporting IQueryBuilder
        /// </summary>
        object IQueryBuilder.Build(IQueryNode queryNode)
        {
            return Build(queryNode);
        }
    }
}
