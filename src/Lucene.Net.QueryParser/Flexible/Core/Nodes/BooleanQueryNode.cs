/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using System.Text;
using Lucene.Net.Queryparser.Flexible.Core.Nodes;
using Lucene.Net.Queryparser.Flexible.Core.Parser;
using Sharpen;

namespace Lucene.Net.Queryparser.Flexible.Core.Nodes
{
	/// <summary>
	/// A
	/// <see cref="BooleanQueryNode">BooleanQueryNode</see>
	/// represents a list of elements which do not have an
	/// explicit boolean operator defined between them. It can be used to express a
	/// boolean query that intends to use the default boolean operator.
	/// </summary>
	public class BooleanQueryNode : QueryNodeImpl
	{
		/// <param name="clauses">- the query nodes to be and'ed</param>
		public BooleanQueryNode(IList<QueryNode> clauses)
		{
			SetLeaf(false);
			Allocate();
			Set(clauses);
		}

		public override string ToString()
		{
			if (GetChildren() == null || GetChildren().Count == 0)
			{
				return "<boolean operation='default'/>";
			}
			StringBuilder sb = new StringBuilder();
			sb.Append("<boolean operation='default'>");
			foreach (QueryNode child in GetChildren())
			{
				sb.Append("\n");
				sb.Append(child.ToString());
			}
			sb.Append("\n</boolean>");
			return sb.ToString();
		}

		public override CharSequence ToQueryString(EscapeQuerySyntax escapeSyntaxParser)
		{
			if (GetChildren() == null || GetChildren().Count == 0)
			{
				return string.Empty;
			}
			StringBuilder sb = new StringBuilder();
			string filler = string.Empty;
			foreach (QueryNode child in GetChildren())
			{
				sb.Append(filler).Append(child.ToQueryString(escapeSyntaxParser));
				filler = " ";
			}
			// in case is root or the parent is a group node avoid parenthesis
			if ((GetParent() != null && GetParent() is GroupQueryNode) || IsRoot())
			{
				return sb.ToString();
			}
			else
			{
				return "( " + sb.ToString() + " )";
			}
		}

		/// <exception cref="Sharpen.CloneNotSupportedException"></exception>
		public override QueryNode CloneTree()
		{
			Lucene.Net.Queryparser.Flexible.Core.Nodes.BooleanQueryNode clone = (Lucene.Net.Queryparser.Flexible.Core.Nodes.BooleanQueryNode
				)base.CloneTree();
			// nothing to do here
			return clone;
		}
	}
}
