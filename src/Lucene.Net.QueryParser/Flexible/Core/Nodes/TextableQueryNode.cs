using Lucene.Net.Support;

namespace Lucene.Net.QueryParsers.Flexible.Core.Nodes
{
    /// <summary>
    /// Interface for a node that has text as a {@link CharSequence}
    /// </summary>
    public interface ITextableQueryNode
    {
        ICharSequence Text { get; set; }
    }
}
