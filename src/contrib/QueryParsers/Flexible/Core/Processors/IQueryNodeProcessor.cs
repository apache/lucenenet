using Lucene.Net.QueryParsers.Flexible.Core.Config;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Core.Processors
{
    public interface IQueryNodeProcessor
    {
        IQueryNode Process(IQueryNode queryTree);

        QueryConfigHandler QueryConfigHandler { get; set; }
    }
}
