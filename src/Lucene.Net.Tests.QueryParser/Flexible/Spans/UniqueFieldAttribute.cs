using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Spans
{
    /// <summary>
    /// This attribute is used by the {@link UniqueFieldQueryNodeProcessor}
    /// processor. It holds a value that defines which is the unique field name that
    /// should be set in every {@link FieldableNode}.
    /// </summary>
    /// <seealso cref="UniqueFieldQueryNodeProcessor"/>
    public interface IUniqueFieldAttribute : IAttribute
    {
        void SetUniqueField(string uniqueField);

        string GetUniqueField();
    }
}
