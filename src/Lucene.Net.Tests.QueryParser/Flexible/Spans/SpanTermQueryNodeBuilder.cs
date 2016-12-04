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
    /// This builder creates <see cref="SpanTermQuery"/>s from a <see cref="FieldQueryNode"/>
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
    }
}
