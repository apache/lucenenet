using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Nodes
{
    public class StandardBooleanQueryNode : BooleanQueryNode
    {
        private bool disableCoord;

        public StandardBooleanQueryNode(IList<IQueryNode> clauses, bool disableCoord)
            : base(clauses)
        {
            this.disableCoord = disableCoord;
        }

        public bool IsDisableCoord
        {
            get
            {
                return this.disableCoord;
            }
        }
    }
}
