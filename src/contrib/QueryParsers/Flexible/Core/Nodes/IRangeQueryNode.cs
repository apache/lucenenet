using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Core.Nodes
{
    public interface IRangeQueryNode
    {
        // .NET Port: non-generic marker interface
    }

    public interface IRangeQueryNode<T, TInner> : IRangeQueryNode, IFieldableNode
        where T : IFieldValuePairQueryNode<TInner>
    {
        T LowerBound { get; }

        T UpperBound { get; }

        bool IsLowerInclusive { get; }

        bool IsUpperInclusive { get; }
    }
}
