using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Parser;
using Lucene.Net.Support;
using System.Globalization;

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
        private NumberFormat numberFormat;

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
            NumberFormat numberFormat)
            : base()
        {
            NumberFormat = numberFormat;
            Field = field;
            Value = value;

        }

        /**
         * Gets or Sets the field associated with this node.
         * 
         * @return the field associated with this node
         */

        public virtual string Field
        {
            get { return this.field; }
            set { this.field = value; }
        }

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
            return escaper.Escape(numberFormat.Format(this.value),
                CultureInfo.CurrentCulture, EscapeQuerySyntax.Type.NORMAL);
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
        public virtual NumberFormat NumberFormat
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

        public override string ToString()
        {
            return "<numeric field='" + this.field + "' number='"
                + numberFormat.Format(value) + "'/>";
        }
    }
}
