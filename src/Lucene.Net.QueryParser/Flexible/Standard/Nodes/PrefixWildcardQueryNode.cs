/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Queryparser.Flexible.Core.Nodes;
using Lucene.Net.Queryparser.Flexible.Standard.Nodes;
using Sharpen;

namespace Lucene.Net.Queryparser.Flexible.Standard.Nodes
{
	/// <summary>
	/// A
	/// <see cref="PrefixWildcardQueryNode">PrefixWildcardQueryNode</see>
	/// represents wildcardquery that matches abc
	/// or *. This does not apply to phrases, this is a special case on the original
	/// lucene parser. TODO: refactor the code to remove this special case from the
	/// parser. and probably do it on a Processor
	/// </summary>
	public class PrefixWildcardQueryNode : WildcardQueryNode
	{
		/// <param name="field">- field name</param>
		/// <param name="text">- value including the wildcard</param>
		/// <param name="begin">- position in the query string</param>
		/// <param name="end">- position in the query string</param>
		public PrefixWildcardQueryNode(CharSequence field, CharSequence text, int begin, 
			int end) : base(field, text, begin, end)
		{
		}

		public PrefixWildcardQueryNode(FieldQueryNode fqn) : this(fqn.GetField(), fqn.GetText
			(), fqn.GetBegin(), fqn.GetEnd())
		{
		}

		public override string ToString()
		{
			return "<prefixWildcard field='" + this.field + "' term='" + this.text + "'/>";
		}

		/// <exception cref="Sharpen.CloneNotSupportedException"></exception>
		public override QueryNode CloneTree()
		{
			Lucene.Net.Queryparser.Flexible.Standard.Nodes.PrefixWildcardQueryNode clone
				 = (Lucene.Net.Queryparser.Flexible.Standard.Nodes.PrefixWildcardQueryNode
				)base.CloneTree();
			// nothing to do here
			return clone;
		}
	}
}
