using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Spans
{
    /// <summary>
    /// This attribute is used by the <see cref="UniqueFieldQueryNodeProcessor"/>
    /// processor. It holds a value that defines which is the unique field name that
    /// should be set in every <see cref="Core.Nodes.IFieldableNode"/>.
    /// </summary>
    /// <seealso cref="UniqueFieldQueryNodeProcessor"/>
    public interface IUniqueFieldAttribute : IAttribute
    {
        string UniqueField { get; set; }
    }
}
