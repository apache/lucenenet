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
    public class BoostQueryNodeBuilder : IStandardQueryBuilder
    {
        public BoostQueryNodeBuilder()
        {
            // empty constructor
        }
        
        public Query Build(IQueryNode queryNode)
        {
            BoostQueryNode boostNode = (BoostQueryNode)queryNode;
            IQueryNode child = boostNode.Child;

            if (child == null)
            {
                return null;
            }

            Query query = (Query)child.GetTag(QueryTreeBuilder.QUERY_TREE_BUILDER_TAGID);
            query.Boost = boostNode.Value;

            return query;
        }

        object IQueryBuilder.Build(IQueryNode queryNode)
        {
            return Build(queryNode);
        }
    }
}
