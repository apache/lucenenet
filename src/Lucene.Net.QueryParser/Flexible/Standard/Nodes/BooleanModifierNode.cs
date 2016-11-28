using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Nodes
{
    /// <summary>
    /// A {@link BooleanModifierNode} has the same behaviour as
    /// {@link ModifierQueryNode}, it only indicates that this modifier was added by
    /// {@link GroupQueryNodeProcessor} and not by the user. 
    /// </summary>
    /// <seealso cref="ModifierQueryNode"/>
    public class BooleanModifierNode : ModifierQueryNode
    {
        public BooleanModifierNode(IQueryNode node, Modifier mod)
            : base(node, mod)
        {
        }
    }
}
