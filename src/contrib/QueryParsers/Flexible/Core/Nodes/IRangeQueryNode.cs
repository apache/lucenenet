using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Core.Nodes
{
    public interface IRangeQueryNode<T> : IFieldableNode
        where T : IFieldValuePairQueryNode<T>
    {
        T LowerBound { get; }

        T UpperBound { get; }

        bool IsLowerInclusive { get; }

        bool IsUpperInclusive { get; }
    }
}
