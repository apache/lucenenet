using Lucene.Net.QueryParsers.Flexible.Core.Parser;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Core.Nodes
{
    public class FieldQueryNode : QueryNode, IFieldValuePairQueryNode<string>, ITextableQueryNode
    {
        protected string field;

        protected string text;

        protected int begin;

        protected int end;

        protected int positionIncrement;

        public FieldQueryNode(string field, string text, int begin, int end)
        {
            this.field = field;
            this.text = text;
            this.begin = begin;
            this.end = end;
            this.SetLeaf(true);
        }

        protected ICharSequence GetTermEscaped(IEscapeQuerySyntax escaper)
        {
            return escaper.Escape(new StringCharSequenceWrapper(this.text), CultureInfo.CurrentCulture, EscapeQuerySyntax.Type.NORMAL);
        }

        protected ICharSequence GetTermEscapeQuoted(EscapeQuerySyntax escaper)
        {
            return escaper.Escape(new StringCharSequenceWrapper(this.text), CultureInfo.CurrentCulture, EscapeQuerySyntax.Type.STRING);
        }

        public override ICharSequence ToQueryString(IEscapeQuerySyntax escaper)
        {
            if (IsDefaultField(this.field))
            {
                return GetTermEscaped(escaper);
            }
            else
            {
                return new StringCharSequenceWrapper(this.field + ":" + GetTermEscaped(escaper));
            }
        }

        public override string ToString()
        {
            return "<field start='" + this.begin + "' end='" + this.end + "' field='"
                + this.field + "' text='" + this.text + "'/>";
        }

        public string TextAsString
        {
            get
            {
                return this.text;
            }
        }

        public string FieldAsString
        {
            get
            {
                return this.field;
            }
        }

        public int Begin
        {
            get { return this.begin; }
            set { this.begin = value; }
        }

        public int End
        {
            get { return this.end; }
            set { this.end = value; }
        }
        

        public string Field
        {
            get { return this.field; }
            set { this.field = value; }
        }

        public string Value
        {
            get { return Text; }
            set { this.Text = value; }
        }

        public string Text
        {
            get { return this.text; }
            set { this.text = value; }
        }

        public int PositionIncrement
        {
            get { return this.positionIncrement; }
            set { this.positionIncrement = value; }
        }

        public override IQueryNode CloneTree()
        {
            FieldQueryNode fqn = (FieldQueryNode)base.CloneTree();
            fqn.begin = this.begin;
            fqn.end = this.end;
            fqn.field = this.field;
            fqn.text = this.text;
            fqn.positionIncrement = this.positionIncrement;
            fqn.toQueryStringIgnoreFields = this.toQueryStringIgnoreFields;

            return fqn;
        }


    }
}
