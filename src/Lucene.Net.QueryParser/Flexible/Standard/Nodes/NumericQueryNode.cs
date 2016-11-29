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
    /// <summary>
    /// This query node represents a field query that holds a numeric value. It is
    /// similar to {@link FieldQueryNode}, however the {@link #getValue()} returns a
    /// {@link Number}.
    /// </summary>
    /// <seealso cref="NumericConfig"/>
    public class NumericQueryNode : QueryNodeImpl, IFieldValuePairQueryNode<object> // LUCENENET TODO: Can we use Decimal??
    {
        private /*NumberFormat*/ string numberFormat;

        private string field;

        private /*Number*/ object value;

        /**
         * Creates a {@link NumericQueryNode} object using the given field,
         * {@link Number} value and {@link NumberFormat} used to convert the value to
         * {@link String}.
         * 
         * @param field the field associated with this query node
         * @param value the value hold by this node
         * @param numberFormat the {@link NumberFormat} used to convert the value to {@link String}
         */
        public NumericQueryNode(string field, /*Number*/ object value,
            /*NumberFormat*/ string numberFormat)
            : base()
        {
            NumberFormat = numberFormat;
            Field = field;
            Value = value;

        }

        /**
         * Returns the field associated with this node.
         * 
         * @return the field associated with this node
         */

        public virtual string Field
        {
            get { return this.field; }
            set { this.field = value; }
        }

        /**
         * Sets the field associated with this node.
         * 
         * @param fieldName the field associated with this node
         */
        //      @Override
        //public void setField(CharSequence fieldName)
        //      {
        //          this.field = fieldName;
        //      }

        /**
         * This method is used to get the value converted to {@link String} and
         * escaped using the given {@link EscapeQuerySyntax}.
         * 
         * @param escaper the {@link EscapeQuerySyntax} used to escape the value {@link String}
         * 
         * @return the value converte to {@link String} and escaped
         */
        protected string GetTermEscaped(IEscapeQuerySyntax escaper)
        {
            return escaper.Escape(new StringCharSequenceWrapper(string.Format(numberFormat, this.value)) /*numberFormat.format(this.value)*/,
                CultureInfo.CurrentCulture, EscapeQuerySyntax.Type.NORMAL).ToString();
        }


        public override string ToQueryString(IEscapeQuerySyntax escapeSyntaxParser)
        {
            if (IsDefaultField(this.field))
            {
                return GetTermEscaped(escapeSyntaxParser);
            }
            else
            {
                return this.field + ":" + GetTermEscaped(escapeSyntaxParser);
            }
        }

        /**
         * Gets or Sets the {@link NumberFormat} used to convert the value to {@link String}.
         * 
         * @return the {@link NumberFormat} used to convert the value to {@link String}
         */
        public virtual /*NumberFormat*/ string NumberFormat
        {
            get { return this.numberFormat; }
            set { this.numberFormat = value; }
        }

        /**
         * Gets or Sets the numeric value as {@link Number}.
         * 
         * @return the numeric value
         */

        public virtual object Value
        {
            get { return value; }
            set { this.value = value; }
        }

        /**
         * Sets the numeric value.
         * 
         * @param value the numeric value
         */
        //      @Override
        //public void setValue(Number value)
        //      {
        //          this.value = value;
        //      }


        public override string ToString()
        {
            return "<numeric field='" + this.field + "' number='"
                + string.Format(numberFormat, value) + "'/>";
        }
    }
}
