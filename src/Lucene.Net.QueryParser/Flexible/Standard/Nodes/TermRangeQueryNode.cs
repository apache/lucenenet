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
	/// This query node represents a range query composed by
	/// <see cref="Lucene.Net.Queryparser.Flexible.Core.Nodes.FieldQueryNode">Lucene.Net.Queryparser.Flexible.Core.Nodes.FieldQueryNode
	/// 	</see>
	/// bounds, which means the bound values are strings.
	/// </summary>
	/// <seealso cref="Lucene.Net.Queryparser.Flexible.Core.Nodes.FieldQueryNode">Lucene.Net.Queryparser.Flexible.Core.Nodes.FieldQueryNode
	/// 	</seealso>
	/// <seealso cref="AbstractRangeQueryNode{T}">AbstractRangeQueryNode&lt;T&gt;</seealso>
	public class TermRangeQueryNode : AbstractRangeQueryNode<FieldQueryNode>
	{
		/// <summary>
		/// Constructs a
		/// <see cref="TermRangeQueryNode">TermRangeQueryNode</see>
		/// object using the given
		/// <see cref="Lucene.Net.Queryparser.Flexible.Core.Nodes.FieldQueryNode">Lucene.Net.Queryparser.Flexible.Core.Nodes.FieldQueryNode
		/// 	</see>
		/// as its bounds.
		/// </summary>
		/// <param name="lower">the lower bound</param>
		/// <param name="upper">the upper bound</param>
		/// <param name="lowerInclusive"><code>true</code> if the lower bound is inclusive, otherwise, <code>false</code>
		/// 	</param>
		/// <param name="upperInclusive"><code>true</code> if the upper bound is inclusive, otherwise, <code>false</code>
		/// 	</param>
		public TermRangeQueryNode(FieldQueryNode lower, FieldQueryNode upper, bool lowerInclusive
			, bool upperInclusive)
		{
			SetBounds(lower, upper, lowerInclusive, upperInclusive);
		}
	}
}
