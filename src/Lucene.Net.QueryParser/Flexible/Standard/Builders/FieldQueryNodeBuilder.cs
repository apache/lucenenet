using Lucene.Net.Index;
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
    /// Builds a {@link TermQuery} object from a {@link FieldQueryNode} object.
    /// </summary>
    public class FieldQueryNodeBuilder : IStandardQueryBuilder
    {
        public FieldQueryNodeBuilder()
        {
            // empty constructor
        }


        public virtual Query Build(IQueryNode queryNode)
        {
            FieldQueryNode fieldNode = (FieldQueryNode)queryNode;

            return new TermQuery(new Term(fieldNode.GetFieldAsString(), fieldNode
                .GetTextAsString()));
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
