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
	/// <see cref="AndQueryNode">AndQueryNode</see>
	/// represents an AND boolean operation performed on a
	/// list of nodes.
	/// </summary>
	public class AndQueryNode : BooleanQueryNode
	{
		/// <param name="clauses">- the query nodes to be and'ed</param>
		public AndQueryNode(IList<QueryNode> clauses) : base(clauses)
		{
			if ((clauses == null) || (clauses.Count == 0))
			{
				throw new ArgumentException("AND query must have at least one clause");
			}
		}

		public override string ToString()
		{
			if (GetChildren() == null || GetChildren().Count == 0)
			{
				return "<boolean operation='and'/>";
			}
			StringBuilder sb = new StringBuilder();
			sb.Append("<boolean operation='and'>");
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
				filler = " AND ";
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
