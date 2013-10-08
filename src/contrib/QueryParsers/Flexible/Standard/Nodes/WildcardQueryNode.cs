using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Parser;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Nodes
{
    public class WildcardQueryNode : FieldQueryNode
    {
        public WildcardQueryNode(string field, string text, int begin, int end)
            : base(field, text, begin, end)
        {
        }

        public WildcardQueryNode(FieldQueryNode fqn)
            : this(fqn.Field, fqn.Text, fqn.Begin, fqn.End)
        {
        }

        public override ICharSequence ToQueryString(IEscapeQuerySyntax escaper)
        {
            if (IsDefaultField(this.field))
            {
                return new StringCharSequenceWrapper(this.text);
            }
            else
            {
                return new StringCharSequenceWrapper(this.field + ":" + this.text);
            }
        }

        public override string ToString()
        {
            return "<wildcard field='" + this.field + "' term='" + this.text + "'/>";
        }

        public override IQueryNode CloneTree()
        {
            WildcardQueryNode clone = (WildcardQueryNode)base.CloneTree();

            // nothing to do here

            return clone;
        }
    }
}
