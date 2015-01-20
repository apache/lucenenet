/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Parser;
using Org.Apache.Lucene.Util;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Flexible.Standard.Nodes
{
	/// <summary>
	/// A
	/// <see cref="RegexpQueryNode">RegexpQueryNode</see>
	/// represents
	/// <see cref="Org.Apache.Lucene.Search.RegexpQuery">Org.Apache.Lucene.Search.RegexpQuery
	/// 	</see>
	/// query Examples: /[a-z]|[0-9]/
	/// </summary>
	public class RegexpQueryNode : QueryNodeImpl, TextableQueryNode, FieldableNode
	{
		private CharSequence text;

		private CharSequence field;

		/// <param name="field">- field name</param>
		/// <param name="text">- value that contains a regular expression</param>
		/// <param name="begin">- position in the query string</param>
		/// <param name="end">- position in the query string</param>
		public RegexpQueryNode(CharSequence field, CharSequence text, int begin, int end)
		{
			this.field = field;
			this.text = text.SubSequence(begin, end);
		}

		public virtual BytesRef TextToBytesRef()
		{
			return new BytesRef(text);
		}

		public override string ToString()
		{
			return "<regexp field='" + this.field + "' term='" + this.text + "'/>";
		}

		/// <exception cref="Sharpen.CloneNotSupportedException"></exception>
		public override QueryNode CloneTree()
		{
			Org.Apache.Lucene.Queryparser.Flexible.Standard.Nodes.RegexpQueryNode clone = (Org.Apache.Lucene.Queryparser.Flexible.Standard.Nodes.RegexpQueryNode
				)base.CloneTree();
			clone.field = this.field;
			clone.text = this.text;
			return clone;
		}

		public virtual CharSequence GetText()
		{
			return text;
		}

		public virtual void SetText(CharSequence text)
		{
			this.text = text;
		}

		public virtual CharSequence GetField()
		{
			return field;
		}

		public virtual string GetFieldAsString()
		{
			return field.ToString();
		}

		public virtual void SetField(CharSequence field)
		{
			this.field = field;
		}

		public override CharSequence ToQueryString(EscapeQuerySyntax escapeSyntaxParser)
		{
			return IsDefaultField(field) ? "/" + text + "/" : field + ":/" + text + "/";
		}
	}
}
