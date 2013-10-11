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
    public class PrefixWildcardQueryNodeBuilder : IStandardQueryBuilder
    {
        public PrefixWildcardQueryNodeBuilder()
        {
            // empty constructor
        }

        public Query Build(IQueryNode queryNode)
        {
            PrefixWildcardQueryNode wildcardNode = (PrefixWildcardQueryNode)queryNode;

            String text = wildcardNode.Text.SubSequence(0, wildcardNode.Text.Length - 1).ToString();
            PrefixQuery q = new PrefixQuery(new Term(wildcardNode.FieldAsString, text));

            MultiTermQuery.RewriteMethod method = (MultiTermQuery.RewriteMethod)queryNode.GetTag(MultiTermRewriteMethodProcessor.TAG_ID);
            if (method != null)
            {
                q.SetRewriteMethod(method);
            }

            return q;
        }

        object IQueryBuilder.Build(IQueryNode queryNode)
        {
            return Build(queryNode);
        }
    }
}
