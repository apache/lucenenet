using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Nodes
{
    public class PrefixWildcardQueryNode : WildcardQueryNode
    {
        public PrefixWildcardQueryNode(ICharSequence field, ICharSequence text, int begin, int end)
            : base(field, text, begin, end)
        {
        }

        public PrefixWildcardQueryNode(FieldQueryNode fqn)
            : this(fqn.Field, fqn.Text, fqn.Begin, fqn.End)
        {
        }

        public override string ToString()
        {
            return "<prefixWildcard field='" + this.field + "' term='" + this.text + "'/>";
        }

        public override IQueryNode CloneTree()
        {
            PrefixWildcardQueryNode clone = (PrefixWildcardQueryNode)base.CloneTree();

            // nothing to do here

            return clone;
        }
    }
}
