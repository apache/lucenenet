using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Core.Nodes
{
    /// <summary>
    /// This interface should be implemented by {@link QueryNode} that holds an
    /// arbitrary value.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IValueQueryNode<T> : IQueryNode
    {
        T Value { get; set; }
    }
}
