using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Parser;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Nodes
{
    public class RegexpQueryNode : QueryNode, ITextableQueryNode, IFieldableNode
    {
        private ICharSequence text;
        private ICharSequence field;

        public RegexpQueryNode(ICharSequence field, ICharSequence text, int begin, int end)
        {
            this.field = field;
            this.text = text.SubSequence(begin, end);
        }

        public BytesRef TextToBytesRef()
        {
            return new BytesRef(text.ToString());
        }

        public override string ToString()
        {
            return "<regexp field='" + this.field + "' term='" + this.text + "'/>";
        }

        public override IQueryNode CloneTree()
        {
            RegexpQueryNode clone = (RegexpQueryNode)base.CloneTree();
            clone.field = this.field;
            clone.text = this.text;
            return clone;
        }
        
        public ICharSequence Text
        {
            get
            {
                return text;
            }
            set
            {
                this.text = value;
            }
        }
        
        public ICharSequence Field
        {
            get
            {
                return field;
            }
            set
            {
                this.field = value;
            }
        }

        public string FieldAsString
        {
            get
            {
                return field.ToString();
            }
        }

        public override ICharSequence ToQueryString(IEscapeQuerySyntax escapeSyntaxParser)
        {
            return new StringCharSequenceWrapper(IsDefaultField(field) ? "/" + text + "/" : field + ":/" + text + "/");
        }
    }
}
