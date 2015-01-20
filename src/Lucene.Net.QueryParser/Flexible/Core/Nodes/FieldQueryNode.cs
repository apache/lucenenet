/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Globalization;
using Lucene.Net.Queryparser.Flexible.Core.Nodes;
using Lucene.Net.Queryparser.Flexible.Core.Parser;
using Sharpen;

namespace Lucene.Net.Queryparser.Flexible.Core.Nodes
{
	/// <summary>
	/// A
	/// <see cref="FieldQueryNode">FieldQueryNode</see>
	/// represents a element that contains field/text tuple
	/// </summary>
	public class FieldQueryNode : QueryNodeImpl, FieldValuePairQueryNode<CharSequence
		>, TextableQueryNode
	{
		/// <summary>The term's field</summary>
		protected internal CharSequence field;

		/// <summary>The term's text.</summary>
		/// <remarks>The term's text.</remarks>
		protected internal CharSequence text;

		/// <summary>The term's begin position.</summary>
		/// <remarks>The term's begin position.</remarks>
		protected internal int begin;

		/// <summary>The term's end position.</summary>
		/// <remarks>The term's end position.</remarks>
		protected internal int end;

		/// <summary>The term's position increment.</summary>
		/// <remarks>The term's position increment.</remarks>
		protected internal int positionIncrement;

		/// <param name="field">- field name</param>
		/// <param name="text">- value</param>
		/// <param name="begin">- position in the query string</param>
		/// <param name="end">- position in the query string</param>
		public FieldQueryNode(CharSequence field, CharSequence text, int begin, int end)
		{
			this.field = field;
			this.text = text;
			this.begin = begin;
			this.end = end;
			this.SetLeaf(true);
		}

		protected internal virtual CharSequence GetTermEscaped(EscapeQuerySyntax escaper)
		{
			return escaper.Escape(this.text, CultureInfo.CurrentCulture, EscapeQuerySyntax.Type
				.NORMAL);
		}

		protected internal virtual CharSequence GetTermEscapeQuoted(EscapeQuerySyntax escaper
			)
		{
			return escaper.Escape(this.text, CultureInfo.CurrentCulture, EscapeQuerySyntax.Type
				.STRING);
		}

		public override CharSequence ToQueryString(EscapeQuerySyntax escaper)
		{
			if (IsDefaultField(this.field))
			{
				return GetTermEscaped(escaper);
			}
			else
			{
				return this.field + ":" + GetTermEscaped(escaper);
			}
		}

		public override string ToString()
		{
			return "<field start='" + this.begin + "' end='" + this.end + "' field='" + this.
				field + "' text='" + this.text + "'/>";
		}

		/// <returns>the term</returns>
		public virtual string GetTextAsString()
		{
			if (this.text == null)
			{
				return null;
			}
			else
			{
				return this.text.ToString();
			}
		}

		/// <summary>returns null if the field was not specified in the query string</summary>
		/// <returns>the field</returns>
		public virtual string GetFieldAsString()
		{
			if (this.field == null)
			{
				return null;
			}
			else
			{
				return this.field.ToString();
			}
		}

		public virtual int GetBegin()
		{
			return this.begin;
		}

		public virtual void SetBegin(int begin)
		{
			this.begin = begin;
		}

		public virtual int GetEnd()
		{
			return this.end;
		}

		public virtual void SetEnd(int end)
		{
			this.end = end;
		}

		public virtual CharSequence GetField()
		{
			return this.field;
		}

		public virtual void SetField(CharSequence field)
		{
			this.field = field;
		}

		public virtual int GetPositionIncrement()
		{
			return this.positionIncrement;
		}

		public virtual void SetPositionIncrement(int pi)
		{
			this.positionIncrement = pi;
		}

		/// <summary>Returns the term.</summary>
		/// <remarks>Returns the term.</remarks>
		/// <returns>The "original" form of the term.</returns>
		public virtual CharSequence GetText()
		{
			return this.text;
		}

		/// <param name="text">the text to set</param>
		public virtual void SetText(CharSequence text)
		{
			this.text = text;
		}

		/// <exception cref="Sharpen.CloneNotSupportedException"></exception>
		public override QueryNode CloneTree()
		{
			Lucene.Net.Queryparser.Flexible.Core.Nodes.FieldQueryNode fqn = (Lucene.Net.Queryparser.Flexible.Core.Nodes.FieldQueryNode
				)base.CloneTree();
			fqn.begin = this.begin;
			fqn.end = this.end;
			fqn.field = this.field;
			fqn.text = this.text;
			fqn.positionIncrement = this.positionIncrement;
			fqn.toQueryStringIgnoreFields = this.toQueryStringIgnoreFields;
			return fqn;
		}

		public virtual CharSequence GetValue()
		{
			return GetText();
		}

		public virtual void SetValue(CharSequence value)
		{
			SetText(value);
		}
	}
}
