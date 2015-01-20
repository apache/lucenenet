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
	/// <see cref="DeletedQueryNode">DeletedQueryNode</see>
	/// represents a node that was deleted from the query
	/// node tree. It can be removed from the tree using the
	/// <see cref="Lucene.Net.Queryparser.Flexible.Core.Processors.RemoveDeletedQueryNodesProcessor
	/// 	">Lucene.Net.Queryparser.Flexible.Core.Processors.RemoveDeletedQueryNodesProcessor
	/// 	</see>
	/// processor.
	/// </summary>
	public class DeletedQueryNode : QueryNodeImpl
	{
		public DeletedQueryNode()
		{
		}

		// empty constructor
		public override CharSequence ToQueryString(EscapeQuerySyntax escaper)
		{
			return "[DELETEDCHILD]";
		}

		public override string ToString()
		{
			return "<deleted/>";
		}

		/// <exception cref="Sharpen.CloneNotSupportedException"></exception>
		public override QueryNode CloneTree()
		{
			Lucene.Net.Queryparser.Flexible.Core.Nodes.DeletedQueryNode clone = (Lucene.Net.Queryparser.Flexible.Core.Nodes.DeletedQueryNode
				)base.CloneTree();
			return clone;
		}
	}
}
