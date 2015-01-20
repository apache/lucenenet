/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Queryparser.Flexible.Core.Nodes;
using Sharpen;

namespace Lucene.Net.Queryparser.Flexible.Standard.Nodes
{
	/// <summary>
	/// A
	/// <see cref="BooleanModifierNode">BooleanModifierNode</see>
	/// has the same behaviour as
	/// <see cref="Lucene.Net.Queryparser.Flexible.Core.Nodes.ModifierQueryNode">Lucene.Net.Queryparser.Flexible.Core.Nodes.ModifierQueryNode
	/// 	</see>
	/// , it only indicates that this modifier was added by
	/// <see cref="Lucene.Net.Queryparser.Flexible.Standard.Processors.GroupQueryNodeProcessor
	/// 	">Lucene.Net.Queryparser.Flexible.Standard.Processors.GroupQueryNodeProcessor
	/// 	</see>
	/// and not by the user. <br/>
	/// </summary>
	/// <seealso cref="Lucene.Net.Queryparser.Flexible.Core.Nodes.ModifierQueryNode
	/// 	">Lucene.Net.Queryparser.Flexible.Core.Nodes.ModifierQueryNode</seealso>
	public class BooleanModifierNode : ModifierQueryNode
	{
		public BooleanModifierNode(QueryNode node, ModifierQueryNode.Modifier mod) : base
			(node, mod)
		{
		}
	}
}
