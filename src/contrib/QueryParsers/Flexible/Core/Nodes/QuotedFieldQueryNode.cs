using Lucene.Net.QueryParsers.Flexible.Core.Parser;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Core.Nodes
{
    public class QuotedFieldQueryNode : FieldQueryNode
    {
        public QuotedFieldQueryNode(ICharSequence field, ICharSequence text, int begin, int end)
            : base(field, text, begin, end)
        {
        }

        public override ICharSequence ToQueryString(IEscapeQuerySyntax escaper)
        {
            if (IsDefaultField(this.field))
            {
                return new StringCharSequenceWrapper("\"" + GetTermEscapeQuoted(escaper) + "\"");
            }
            else
            {
                return new StringCharSequenceWrapper(this.field + ":" + "\"" + GetTermEscapeQuoted(escaper) + "\"");
            }
        }

        public override string ToString()
        {
            return "<quotedfield start='" + this.begin + "' end='" + this.end
                + "' field='" + this.field + "' term='" + this.text + "'/>";
        }

        public override IQueryNode CloneTree()
        {
            QuotedFieldQueryNode clone = (QuotedFieldQueryNode)base.CloneTree();
            // nothing to do here
            return clone;
        }
    }
}
