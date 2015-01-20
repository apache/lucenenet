/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using System.Text;
using Lucene.Net.Queryparser.Flexible.Core.Nodes;
using Lucene.Net.Queryparser.Flexible.Core.Parser;
using Sharpen;

namespace Lucene.Net.Queryparser.Flexible.Core.Nodes
{
	/// <summary>
	/// A
	/// <see cref="OrQueryNode">OrQueryNode</see>
	/// represents an OR boolean operation performed on a list
	/// of nodes.
	/// </summary>
	public class OrQueryNode : BooleanQueryNode
	{
		/// <param name="clauses">- the query nodes to be or'ed</param>
		public OrQueryNode(IList<QueryNode> clauses) : base(clauses)
		{
			if ((clauses == null) || (clauses.Count == 0))
			{
				throw new ArgumentException("OR query must have at least one clause");
			}
		}

		public override string ToString()
		{
			if (GetChildren() == null || GetChildren().Count == 0)
			{
				return "<boolean operation='or'/>";
			}
			StringBuilder sb = new StringBuilder();
			sb.Append("<boolean operation='or'>");
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
			for (Iterator<QueryNode> it = GetChildren().Iterator(); it.HasNext(); )
			{
				sb.Append(filler).Append(it.Next().ToQueryString(escapeSyntaxParser));
				filler = " OR ";
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
	}
}
