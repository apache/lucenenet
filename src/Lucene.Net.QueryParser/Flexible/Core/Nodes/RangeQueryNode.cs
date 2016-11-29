using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Core.Nodes
{
    /// <summary>
    /// This interface should be implemented by a {@link QueryNode} that represents
    /// some kind of range query.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IRangeQueryNode<T> : IRangeQueryNode, IFieldableNode
        where T : IFieldableNode /*IFieldValuePairQueryNode<?>*/
    {
        T LowerBound { get; }

        T UpperBound { get; }
    }

    /// <summary>
    /// LUCENENET specific interface for identifying a
    /// RangeQueryNode without specifying its generic closing type
    /// </summary>
    public interface IRangeQueryNode
    {
        bool IsLowerInclusive { get; }

        bool IsUpperInclusive { get; }
    }
}
