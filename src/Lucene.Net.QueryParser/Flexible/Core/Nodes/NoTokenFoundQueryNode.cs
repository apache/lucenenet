using Lucene.Net.QueryParsers.Flexible.Core.Parser;

namespace Lucene.Net.QueryParsers.Flexible.Core.Nodes
{
    /// <summary>
    /// A {@link NoTokenFoundQueryNode} is used if a term is convert into no tokens
    /// by the tokenizer/lemmatizer/analyzer (null).
    /// </summary>
    public class NoTokenFoundQueryNode : DeletedQueryNode
    {
        public NoTokenFoundQueryNode()
        {
        }

        public override string ToQueryString(IEscapeQuerySyntax escaper)
        {
            return "[NTF]";
        }

        public override string ToString()
        {
            return "<notokenfound/>";
        }

        public override IQueryNode CloneTree()
        {
            NoTokenFoundQueryNode clone = (NoTokenFoundQueryNode)base.CloneTree();

            // nothing to do here

            return clone;
        }
    }
}
