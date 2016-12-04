using Lucene.Net.QueryParsers.Flexible.Core.Parser;

namespace Lucene.Net.QueryParsers.Flexible.Core.Nodes
{
    /// <summary>
    /// A {@link MatchAllDocsQueryNode} indicates that a query node tree or subtree
    /// will match all documents if executed in the index.
    /// </summary>
    public class MatchAllDocsQueryNode : QueryNodeImpl
    {
        public MatchAllDocsQueryNode()
        {
            // empty constructor
        }

        public override string ToString()
        {
            return "<matchAllDocs field='*' term='*'/>";
        }

        public override string ToQueryString(IEscapeQuerySyntax escapeSyntaxParser)
        {
            return "*:*";
        }

        public override IQueryNode CloneTree()
        {
            MatchAllDocsQueryNode clone = (MatchAllDocsQueryNode)base.CloneTree();

            // nothing to clone

            return clone;
        }
    }
}
