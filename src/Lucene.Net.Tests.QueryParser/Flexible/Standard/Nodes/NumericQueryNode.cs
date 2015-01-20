/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Globalization;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Parser;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Flexible.Standard.Nodes
{
	/// <summary>This query node represents a field query that holds a numeric value.</summary>
	/// <remarks>
	/// This query node represents a field query that holds a numeric value. It is
	/// similar to
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.FieldQueryNode">Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.FieldQueryNode
	/// 	</see>
	/// , however the
	/// <see cref="GetValue()">GetValue()</see>
	/// returns a
	/// <see cref="Sharpen.Number">Sharpen.Number</see>
	/// .
	/// </remarks>
	/// <seealso cref="Org.Apache.Lucene.Queryparser.Flexible.Standard.Config.NumericConfig
	/// 	">Org.Apache.Lucene.Queryparser.Flexible.Standard.Config.NumericConfig</seealso>
	public class NumericQueryNode : QueryNodeImpl, FieldValuePairQueryNode<Number>
	{
		private NumberFormat numberFormat;

		private CharSequence field;

		private Number value;

		/// <summary>
		/// Creates a
		/// <see cref="NumericQueryNode">NumericQueryNode</see>
		/// object using the given field,
		/// <see cref="Sharpen.Number">Sharpen.Number</see>
		/// value and
		/// <see cref="Sharpen.NumberFormat">Sharpen.NumberFormat</see>
		/// used to convert the value to
		/// <see cref="string">string</see>
		/// .
		/// </summary>
		/// <param name="field">the field associated with this query node</param>
		/// <param name="value">the value hold by this node</param>
		/// <param name="numberFormat">
		/// the
		/// <see cref="Sharpen.NumberFormat">Sharpen.NumberFormat</see>
		/// used to convert the value to
		/// <see cref="string">string</see>
		/// </param>
		public NumericQueryNode(CharSequence field, Number value, NumberFormat numberFormat
			) : base()
		{
			SetNumberFormat(numberFormat);
			SetField(field);
			SetValue(value);
		}

		/// <summary>Returns the field associated with this node.</summary>
		/// <remarks>Returns the field associated with this node.</remarks>
		/// <returns>the field associated with this node</returns>
		public virtual CharSequence GetField()
		{
			return this.field;
		}

		/// <summary>Sets the field associated with this node.</summary>
		/// <remarks>Sets the field associated with this node.</remarks>
		/// <param name="fieldName">the field associated with this node</param>
		public virtual void SetField(CharSequence fieldName)
		{
			this.field = fieldName;
		}

		/// <summary>
		/// This method is used to get the value converted to
		/// <see cref="string">string</see>
		/// and
		/// escaped using the given
		/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Parser.EscapeQuerySyntax">Org.Apache.Lucene.Queryparser.Flexible.Core.Parser.EscapeQuerySyntax
		/// 	</see>
		/// .
		/// </summary>
		/// <param name="escaper">
		/// the
		/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Parser.EscapeQuerySyntax">Org.Apache.Lucene.Queryparser.Flexible.Core.Parser.EscapeQuerySyntax
		/// 	</see>
		/// used to escape the value
		/// <see cref="string">string</see>
		/// </param>
		/// <returns>
		/// the value converte to
		/// <see cref="string">string</see>
		/// and escaped
		/// </returns>
		protected internal virtual CharSequence GetTermEscaped(EscapeQuerySyntax escaper)
		{
			return escaper.Escape(numberFormat.Format(this.value), CultureInfo.ROOT, EscapeQuerySyntax.Type
				.NORMAL);
		}

		public override CharSequence ToQueryString(EscapeQuerySyntax escapeSyntaxParser)
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

		/// <summary>
		/// Sets the
		/// <see cref="Sharpen.NumberFormat">Sharpen.NumberFormat</see>
		/// used to convert the value to
		/// <see cref="string">string</see>
		/// .
		/// </summary>
		/// <param name="format">
		/// the
		/// <see cref="Sharpen.NumberFormat">Sharpen.NumberFormat</see>
		/// used to convert the value to
		/// <see cref="string">string</see>
		/// </param>
		public virtual void SetNumberFormat(NumberFormat format)
		{
			this.numberFormat = format;
		}

		/// <summary>
		/// Returns the
		/// <see cref="Sharpen.NumberFormat">Sharpen.NumberFormat</see>
		/// used to convert the value to
		/// <see cref="string">string</see>
		/// .
		/// </summary>
		/// <returns>
		/// the
		/// <see cref="Sharpen.NumberFormat">Sharpen.NumberFormat</see>
		/// used to convert the value to
		/// <see cref="string">string</see>
		/// </returns>
		public virtual NumberFormat GetNumberFormat()
		{
			return this.numberFormat;
		}

		/// <summary>
		/// Returns the numeric value as
		/// <see cref="Sharpen.Number">Sharpen.Number</see>
		/// .
		/// </summary>
		/// <returns>the numeric value</returns>
		public virtual Number GetValue()
		{
			return value;
		}

		/// <summary>Sets the numeric value.</summary>
		/// <remarks>Sets the numeric value.</remarks>
		/// <param name="value">the numeric value</param>
		public virtual void SetValue(Number value)
		{
			this.value = value;
		}

		public override string ToString()
		{
			return "<numeric field='" + this.field + "' number='" + numberFormat.Format(value
				) + "'/>";
		}
	}
}
