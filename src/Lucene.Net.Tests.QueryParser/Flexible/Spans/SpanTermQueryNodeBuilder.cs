using Lucene.Net.Index;
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
    /// This builder creates {@link SpanTermQuery}s from a {@link FieldQueryNode}
    /// object.
    /// </summary>
    public class SpanTermQueryNodeBuilder : IStandardQueryBuilder
    {

        public virtual Query Build(IQueryNode node)
        {
            FieldQueryNode fieldQueryNode = (FieldQueryNode)node;

            return new SpanTermQuery(new Term(fieldQueryNode.GetFieldAsString(),
                fieldQueryNode.GetTextAsString()));

        }

        ///// <summary>
        ///// LUCENENET specific overload for supporting IQueryBuilder
        ///// </summary>
        //object IQueryBuilder.Build(IQueryNode queryNode)
        //{
        //    return Build(queryNode);
        //}

    }
}
