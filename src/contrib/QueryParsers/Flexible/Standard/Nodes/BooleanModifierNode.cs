using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Nodes
{
    public class BooleanModifierNode : ModifierQueryNode
    {
        public BooleanModifierNode(IQueryNode node, Modifier mod)
            : base(node, mod)
        {
        }
    }
}
