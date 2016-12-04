using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Standard.Nodes;
using Lucene.Net.QueryParsers.Flexible.Standard.Processors;
using Lucene.Net.Search;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Builders
{
    /// <summary>
    /// Builds a {@link WildcardQuery} object from a {@link WildcardQueryNode}
    /// object.
    /// </summary>
    public class WildcardQueryNodeBuilder : IStandardQueryBuilder
    {
        public WildcardQueryNodeBuilder()
        {
            // empty constructor
        }

        public virtual Query Build(IQueryNode queryNode)
        {
            WildcardQueryNode wildcardNode = (WildcardQueryNode)queryNode;

            WildcardQuery q = new WildcardQuery(new Term(wildcardNode.GetFieldAsString(),
                                                 wildcardNode.GetTextAsString()));

            MultiTermQuery.RewriteMethod method = (MultiTermQuery.RewriteMethod)queryNode.GetTag(MultiTermRewriteMethodProcessor.TAG_ID);
            if (method != null)
            {
                q.SetRewriteMethod(method);
            }

            return q;
        }
    }
}
