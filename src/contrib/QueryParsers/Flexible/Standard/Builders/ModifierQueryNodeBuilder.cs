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
    public class ModifierQueryNodeBuilder : IStandardQueryBuilder
    {
        public ModifierQueryNodeBuilder()
        {
            // empty constructor
        }

        public Query Build(IQueryNode queryNode)
        {
            ModifierQueryNode modifierNode = (ModifierQueryNode)queryNode;

            return (Query)(modifierNode).Child.GetTag(QueryTreeBuilder.QUERY_TREE_BUILDER_TAGID);
        }

        object IQueryBuilder.Build(IQueryNode queryNode)
        {
            return Build(queryNode);
        }
    }
}
