/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Queryparser.Flexible.Core.Nodes;
using Lucene.Net.Queryparser.Flexible.Core.Parser;
using Sharpen;

namespace Lucene.Net.Queryparser.Flexible.Standard.Nodes
{
	/// <summary>
	/// A
	/// <see cref="WildcardQueryNode">WildcardQueryNode</see>
	/// represents wildcard query This does not apply to
	/// phrases. Examples: a*b*c Fl?w? m?ke*g
	/// </summary>
	public class WildcardQueryNode : FieldQueryNode
	{
		/// <param name="field">- field name</param>
		/// <param name="text">- value that contains one or more wild card characters (? or *)
		/// 	</param>
		/// <param name="begin">- position in the query string</param>
		/// <param name="end">- position in the query string</param>
		public WildcardQueryNode(CharSequence field, CharSequence text, int begin, int end
			) : base(field, text, begin, end)
		{
		}

		public WildcardQueryNode(FieldQueryNode fqn) : this(fqn.GetField(), fqn.GetText()
			, fqn.GetBegin(), fqn.GetEnd())
		{
		}

		public override CharSequence ToQueryString(EscapeQuerySyntax escaper)
		{
			if (IsDefaultField(this.field))
			{
				return this.text;
			}
			else
			{
				return this.field + ":" + this.text;
			}
		}

		public override string ToString()
		{
			return "<wildcard field='" + this.field + "' term='" + this.text + "'/>";
		}

		/// <exception cref="Sharpen.CloneNotSupportedException"></exception>
		public override QueryNode CloneTree()
		{
			Lucene.Net.Queryparser.Flexible.Standard.Nodes.WildcardQueryNode clone = (
				Lucene.Net.Queryparser.Flexible.Standard.Nodes.WildcardQueryNode)base.CloneTree
				();
			// nothing to do here
			return clone;
		}
	}
}
