using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Core.Nodes
{
    public interface IFieldableNode : IQueryNode
    {
        ICharSequence Field { get; set; }
    }
}
