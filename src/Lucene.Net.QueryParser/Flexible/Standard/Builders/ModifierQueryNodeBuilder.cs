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
    /// Builds no object, it only returns the {@link Query} object set on the
    /// {@link ModifierQueryNode} object using a
    /// {@link QueryTreeBuilder#QUERY_TREE_BUILDER_TAGID} tag.
    /// </summary>
    public class ModifierQueryNodeBuilder : IStandardQueryBuilder
    {
        public ModifierQueryNodeBuilder()
        {
            // empty constructor
        }

        public virtual Query Build(IQueryNode queryNode)
        {
            ModifierQueryNode modifierNode = (ModifierQueryNode)queryNode;

            return (Query)(modifierNode).GetChild().GetTag(
                QueryTreeBuilder<Query>.QUERY_TREE_BUILDER_TAGID);
        }
    }
}
