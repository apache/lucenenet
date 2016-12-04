using Lucene.Net.QueryParsers.Flexible.Core.Builders;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.Search;

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
            IQueryNode child = boostNode.Child;

            if (child == null)
            {
                return null;
            }

            Query query = (Query)child
                .GetTag(QueryTreeBuilder<Query>.QUERY_TREE_BUILDER_TAGID);
            query.Boost = boostNode.Value;

            return query;
        }
    }
}
