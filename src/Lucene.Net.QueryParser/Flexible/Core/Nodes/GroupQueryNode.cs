/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Lucene.Net.Queryparser.Flexible.Core;
using Lucene.Net.Queryparser.Flexible.Core.Messages;
using Lucene.Net.Queryparser.Flexible.Core.Nodes;
using Lucene.Net.Queryparser.Flexible.Core.Parser;
using Lucene.Net.Queryparser.Flexible.Messages;
using Sharpen;

namespace Lucene.Net.Queryparser.Flexible.Core.Nodes
{
	/// <summary>
	/// A
	/// <see cref="GroupQueryNode">GroupQueryNode</see>
	/// represents a location where the original user typed
	/// real parenthesis on the query string. This class is useful for queries like:
	/// a) a AND b OR c b) ( a AND b) OR c
	/// Parenthesis might be used to define the boolean operation precedence.
	/// </summary>
	public class GroupQueryNode : QueryNodeImpl
	{
		/// <summary>This QueryNode is used to identify parenthesis on the original query string
		/// 	</summary>
		public GroupQueryNode(QueryNode query)
		{
			if (query == null)
			{
				throw new QueryNodeError(new MessageImpl(QueryParserMessages.PARAMETER_VALUE_NOT_SUPPORTED
					, "query", "null"));
			}
			Allocate();
			SetLeaf(false);
			Add(query);
		}

		public virtual QueryNode GetChild()
		{
			return GetChildren()[0];
		}

		public override string ToString()
		{
			return "<group>" + "\n" + GetChild().ToString() + "\n</group>";
		}

		public override CharSequence ToQueryString(EscapeQuerySyntax escapeSyntaxParser)
		{
			if (GetChild() == null)
			{
				return string.Empty;
			}
			return "( " + GetChild().ToQueryString(escapeSyntaxParser) + " )";
		}

		/// <exception cref="Sharpen.CloneNotSupportedException"></exception>
		public override QueryNode CloneTree()
		{
			Lucene.Net.Queryparser.Flexible.Core.Nodes.GroupQueryNode clone = (Lucene.Net.Queryparser.Flexible.Core.Nodes.GroupQueryNode
				)base.CloneTree();
			return clone;
		}

		public virtual void SetChild(QueryNode child)
		{
			IList<QueryNode> list = new AList<QueryNode>();
			list.AddItem(child);
			this.Set(list);
		}
	}
}
