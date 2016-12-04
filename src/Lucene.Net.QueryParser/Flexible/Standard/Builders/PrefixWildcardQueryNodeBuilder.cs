using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Standard.Nodes;
using Lucene.Net.QueryParsers.Flexible.Standard.Processors;
using Lucene.Net.Search;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Builders
{
    /// <summary>
    /// Builds a {@link PrefixQuery} object from a {@link PrefixWildcardQueryNode}
    /// object.
    /// </summary>
    public class PrefixWildcardQueryNodeBuilder : IStandardQueryBuilder
    {
        public PrefixWildcardQueryNodeBuilder()
        {
            // empty constructor
        }

        public virtual Query Build(IQueryNode queryNode)
        {
            PrefixWildcardQueryNode wildcardNode = (PrefixWildcardQueryNode)queryNode;

            string text = wildcardNode.Text.SubSequence(0, wildcardNode.Text.Length - 1).ToString();
            PrefixQuery q = new PrefixQuery(new Term(wildcardNode.GetFieldAsString(), text));

            MultiTermQuery.RewriteMethod method = (MultiTermQuery.RewriteMethod)queryNode.GetTag(MultiTermRewriteMethodProcessor.TAG_ID);
            if (method != null)
            {
                q.SetRewriteMethod(method);
            }

            return q;
        }
    }
}
