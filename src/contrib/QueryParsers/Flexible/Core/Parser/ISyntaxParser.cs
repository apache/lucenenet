using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Core.Parser
{
    public interface ISyntaxParser
    {
        IQueryNode Parse(string query, string field);
    }
}
