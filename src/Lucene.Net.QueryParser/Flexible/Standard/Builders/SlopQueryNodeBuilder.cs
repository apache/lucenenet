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
    /// {@link SlopQueryNode} child using
    /// {@link QueryTreeBuilder#QUERY_TREE_BUILDER_TAGID} and applies the slop value
    /// defined in the {@link SlopQueryNode}.
    /// </summary>
    public class SlopQueryNodeBuilder : IStandardQueryBuilder
    {
        public SlopQueryNodeBuilder()
        {
            // empty constructor
        }

        public virtual Query Build(IQueryNode queryNode)
        {
            SlopQueryNode phraseSlopNode = (SlopQueryNode)queryNode;

            Query query = (Query)phraseSlopNode.GetChild().GetTag(
                QueryTreeBuilder<Query>.QUERY_TREE_BUILDER_TAGID);

            if (query is PhraseQuery)
            {
                ((PhraseQuery)query).Slop = phraseSlopNode.Value;
            }
            else
            {
                ((MultiPhraseQuery)query).Slop = phraseSlopNode.Value;
            }

            return query;
        }
    }
}
