using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Core.Nodes
{
    public class MatchNoDocsQueryNode : DeletedQueryNode
    {
        public MatchNoDocsQueryNode()
        {
            // empty constructor
        }

        public override string ToString()
        {
            return "<matchNoDocsQueryNode/>";
        }
    }
}
