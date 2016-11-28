using Lucene.Net.QueryParsers.Flexible.Core.Builders;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Standard.Builders;
using Lucene.Net.Search;
using Lucene.Net.Search.Spans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Spans
{
    /// <summary>
    /// This builder creates {@link SpanOrQuery}s from a {@link BooleanQueryNode}.
    /// <para/>
    /// It assumes that the {@link BooleanQueryNode} instance has at least one child.
    /// </summary>
    public class SpanOrQueryNodeBuilder : IStandardQueryBuilder
    {
        public virtual Query Build(IQueryNode node)
        {

            // validates node
            BooleanQueryNode booleanNode = (BooleanQueryNode)node;

            IList<IQueryNode> children = booleanNode.GetChildren();
            SpanQuery[]
            spanQueries = new SpanQuery[children.size()];

            int i = 0;
            foreach (IQueryNode child in children)
            {
                spanQueries[i++] = (SpanQuery)child
                    .GetTag(QueryTreeBuilder.QUERY_TREE_BUILDER_TAGID);
            }

            return new SpanOrQuery(spanQueries);

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
