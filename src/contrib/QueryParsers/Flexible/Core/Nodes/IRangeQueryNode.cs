using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Core.Nodes
{
    public interface IRangeQueryNode<T, TInner> : IFieldableNode
        where T : IFieldValuePairQueryNode<TInner>
    {
        T LowerBound { get; }

        T UpperBound { get; }

        bool IsLowerInclusive { get; }

        bool IsUpperInclusive { get; }
    }
}
