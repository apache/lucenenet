using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
