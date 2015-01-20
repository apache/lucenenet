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
	/// <see cref="OpaqueQueryNode">OpaqueQueryNode</see>
	/// is used for specify values that are not supposed to
	/// be parsed by the parser. For example: and XPATH query in the middle of a
	/// query string a b @xpath:'/bookstore/book[1]/title' c d
	/// </summary>
	public class OpaqueQueryNode : QueryNodeImpl
	{
		private CharSequence schema = null;

		private CharSequence value = null;

		/// <param name="schema">- schema identifier</param>
		/// <param name="value">- value that was not parsed</param>
		public OpaqueQueryNode(CharSequence schema, CharSequence value)
		{
			this.SetLeaf(true);
			this.schema = schema;
			this.value = value;
		}

		public override string ToString()
		{
			return "<opaque schema='" + this.schema + "' value='" + this.value + "'/>";
		}

		public override CharSequence ToQueryString(EscapeQuerySyntax escapeSyntaxParser)
		{
			return "@" + this.schema + ":'" + this.value + "'";
		}

		/// <exception cref="Sharpen.CloneNotSupportedException"></exception>
		public override QueryNode CloneTree()
		{
			Lucene.Net.Queryparser.Flexible.Core.Nodes.OpaqueQueryNode clone = (Lucene.Net.Queryparser.Flexible.Core.Nodes.OpaqueQueryNode
				)base.CloneTree();
			clone.schema = this.schema;
			clone.value = this.value;
			return clone;
		}

		/// <returns>the schema</returns>
		public virtual CharSequence GetSchema()
		{
			return this.schema;
		}

		/// <returns>the value</returns>
		public virtual CharSequence GetValue()
		{
			return this.value;
		}
	}
}
