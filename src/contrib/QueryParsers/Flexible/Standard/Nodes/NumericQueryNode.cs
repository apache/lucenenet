using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Parser;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Nodes
{
    public class NumericQueryNode<T> : QueryNode, IFieldValuePairQueryNode<T>
        where T : struct
    {
        private NumberFormatInfo numberFormat;

        private ICharSequence field;

        private T value;

        public NumericQueryNode(ICharSequence field, T value, NumberFormatInfo numberFormat)
            : base()
        {
            NumberFormat = numberFormat;
            Field = field;
            Value = value;
        }

        public ICharSequence Field
        {
            get
            {
                return this.field;
            }
            set
            {
                this.field = value;
            }
        }

        protected ICharSequence GetTermEscaped(IEscapeQuerySyntax escaper)
        {
            return escaper.Escape(new StringCharSequenceWrapper(Convert.ToString(this.value, this.numberFormat)),
                CultureInfo.InvariantCulture, EscapeQuerySyntax.Type.NORMAL);
        }

        public override ICharSequence ToQueryString(IEscapeQuerySyntax escapeSyntaxParser)
        {
            if (IsDefaultField(this.field))
            {
                return GetTermEscaped(escapeSyntaxParser);
            }
            else
            {
                return new StringCharSequenceWrapper(this.field + ":" + GetTermEscaped(escapeSyntaxParser));
            }
        }

        public NumberFormatInfo NumberFormat
        {
            get { return this.numberFormat; }
            set { this.numberFormat = value; }
        }

        public T Value
        {
            get
            {
                return this.value;
            }
            set
            {
                this.value = value;
            }
        }

        public override string ToString()
        {
            return "<numeric field='" + this.field + "' number='"
                + Convert.ToString(this.value, this.numberFormat) + "'/>";
        }
    }
}
