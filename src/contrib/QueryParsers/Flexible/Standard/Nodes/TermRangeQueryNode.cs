using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Nodes
{
    public class TermRangeQueryNode : AbstractRangeQueryNode<FieldQueryNode, string>
    {
        public TermRangeQueryNode(FieldQueryNode lower, FieldQueryNode upper, bool lowerInclusive, bool upperInclusive)
        {
            SetBounds(lower, upper, lowerInclusive, upperInclusive);
        }  
    }
}
