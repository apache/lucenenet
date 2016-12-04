using Lucene.Net.QueryParsers.Flexible.Core.Builders;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.Search;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Builders
{
    /// <summary>
    /// Builds no object, it only returns the {@link Query} object set on the
    /// {@link GroupQueryNode} object using a
    /// {@link QueryTreeBuilder#QUERY_TREE_BUILDER_TAGID} tag.
    /// </summary>
    public class GroupQueryNodeBuilder : IStandardQueryBuilder
    {
        public GroupQueryNodeBuilder()
        {
            // empty constructor
        }

        public virtual Query Build(IQueryNode queryNode)
        {
            GroupQueryNode groupNode = (GroupQueryNode)queryNode;

            return (Query)(groupNode).GetChild().GetTag(
                QueryTreeBuilder<Query>.QUERY_TREE_BUILDER_TAGID);
        }
    }
}
