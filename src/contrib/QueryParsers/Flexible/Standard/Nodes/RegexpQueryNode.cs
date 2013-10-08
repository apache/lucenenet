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
        private string text;
        private string field;

        public RegexpQueryNode(string field, string text, int begin, int end)
        {
            this.field = field;
            this.text = text.Substring(begin, end);
        }

        public BytesRef TextToBytesRef()
        {
            return new BytesRef(text);
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
        
        public string Text
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
        
        public string Field
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
                return field;
            }
        }

        public override ICharSequence ToQueryString(IEscapeQuerySyntax escapeSyntaxParser)
        {
            return new StringCharSequenceWrapper(IsDefaultField(field) ? "/" + text + "/" : field + ":/" + text + "/");
        }
    }
}
