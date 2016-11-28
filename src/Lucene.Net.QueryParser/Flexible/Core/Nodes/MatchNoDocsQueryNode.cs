using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Core.Nodes
{
    /// <summary>
    /// A {@link MatchNoDocsQueryNode} indicates that a query node tree or subtree
    /// will not match any documents if executed in the index.
    /// </summary>
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
