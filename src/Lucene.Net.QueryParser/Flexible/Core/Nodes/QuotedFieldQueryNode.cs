/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Queryparser.Flexible.Core.Nodes;
using Lucene.Net.Queryparser.Flexible.Core.Parser;
using Sharpen;

namespace Lucene.Net.Queryparser.Flexible.Core.Nodes
{
	/// <summary>
	/// A
	/// <see cref="QuotedFieldQueryNode">QuotedFieldQueryNode</see>
	/// represents phrase query. Example:
	/// "life is great"
	/// </summary>
	public class QuotedFieldQueryNode : FieldQueryNode
	{
		/// <param name="field">- field name</param>
		/// <param name="text">- value</param>
		/// <param name="begin">- position in the query string</param>
		/// <param name="end">- position in the query string</param>
		public QuotedFieldQueryNode(CharSequence field, CharSequence text, int begin, int
			 end) : base(field, text, begin, end)
		{
		}

		public override CharSequence ToQueryString(EscapeQuerySyntax escaper)
		{
			if (IsDefaultField(this.field))
			{
				return "\"" + GetTermEscapeQuoted(escaper) + "\"";
			}
			else
			{
				return this.field + ":" + "\"" + GetTermEscapeQuoted(escaper) + "\"";
			}
		}

		public override string ToString()
		{
			return "<quotedfield start='" + this.begin + "' end='" + this.end + "' field='" +
				 this.field + "' term='" + this.text + "'/>";
		}

		/// <exception cref="Sharpen.CloneNotSupportedException"></exception>
		public override QueryNode CloneTree()
		{
			Lucene.Net.Queryparser.Flexible.Core.Nodes.QuotedFieldQueryNode clone = (Lucene.Net.Queryparser.Flexible.Core.Nodes.QuotedFieldQueryNode
				)base.CloneTree();
			// nothing to do here
			return clone;
		}
	}
}
