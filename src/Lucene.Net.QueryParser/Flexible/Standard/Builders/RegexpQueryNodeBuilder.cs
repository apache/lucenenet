using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Flexible.Core.Builders;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Standard.Nodes;
using Lucene.Net.QueryParsers.Flexible.Standard.Processors;
using Lucene.Net.Search;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Builders
{
    /// <summary>
    /// Builds a {@link RegexpQuery} object from a {@link RegexpQueryNode} object.
    /// </summary>
    public class RegexpQueryNodeBuilder : IStandardQueryBuilder
    {
        public RegexpQueryNodeBuilder()
        {
            // empty constructor
        }


        public virtual Query Build(IQueryNode queryNode)
        {
            RegexpQueryNode regexpNode = (RegexpQueryNode)queryNode;

            RegexpQuery q = new RegexpQuery(new Term(regexpNode.GetFieldAsString(),
                regexpNode.TextToBytesRef()));

            MultiTermQuery.RewriteMethod method = (MultiTermQuery.RewriteMethod)queryNode
                .GetTag(MultiTermRewriteMethodProcessor.TAG_ID);
            if (method != null)
            {
                q.SetRewriteMethod(method);
            }

            return q;
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
