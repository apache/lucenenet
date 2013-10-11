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
    public class SlopQueryNodeBuilder : IStandardQueryBuilder
    {
        public SlopQueryNodeBuilder()
        {
            // empty constructor
        }

        public Query Build(IQueryNode queryNode)
        {
            SlopQueryNode phraseSlopNode = (SlopQueryNode)queryNode;

            Query query = (Query)phraseSlopNode.Child.GetTag(
                QueryTreeBuilder.QUERY_TREE_BUILDER_TAGID);

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

        object IQueryBuilder.Build(IQueryNode queryNode)
        {
            return Build(queryNode);
        }
    }
}
