using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.QueryParsers.Flexible.Core.Nodes
{
    public interface IValueQueryNode<T> : IQueryNode
    {
        T Value { get; set; }
    }
}
