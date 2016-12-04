using Lucene.Net.QueryParsers.Flexible.Core.Nodes;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Nodes
{
    /// <summary>
    /// This query node represents a range query composed by {@link FieldQueryNode}
    /// bounds, which means the bound values are strings.
    /// </summary>
    /// <seealso cref="FieldQueryNode"/>
    /// <seealso cref="AbstractRangeQueryNode{T}"/>
    public class TermRangeQueryNode : AbstractRangeQueryNode<FieldQueryNode>
    {
        /**
   * Constructs a {@link TermRangeQueryNode} object using the given
   * {@link FieldQueryNode} as its bounds.
   * 
   * @param lower the lower bound
   * @param upper the upper bound
   * @param lowerInclusive <code>true</code> if the lower bound is inclusive, otherwise, <code>false</code>
   * @param upperInclusive <code>true</code> if the upper bound is inclusive, otherwise, <code>false</code>
   */
        public TermRangeQueryNode(FieldQueryNode lower, FieldQueryNode upper,
            bool lowerInclusive, bool upperInclusive)
        {
            SetBounds(lower, upper, lowerInclusive, upperInclusive);
        }
    }
}
