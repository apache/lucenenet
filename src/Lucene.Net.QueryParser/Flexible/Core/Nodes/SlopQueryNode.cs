/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

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
	/// <see cref="SlopQueryNode">SlopQueryNode</see>
	/// represents phrase query with a slop.
	/// From Lucene FAQ: Is there a way to use a proximity operator (like near or
	/// within) with Lucene? There is a variable called slop that allows you to
	/// perform NEAR/WITHIN-like queries. By default, slop is set to 0 so that only
	/// exact phrases will match. When using TextParser you can use this syntax to
	/// specify the slop: "doug cutting"~2 will find documents that contain
	/// "doug cutting" as well as ones that contain "cutting doug".
	/// </summary>
	public class SlopQueryNode : QueryNodeImpl, FieldableNode
	{
		private int value = 0;

		/// <param name="query">- QueryNode Tree with the phrase</param>
		/// <param name="value">- slop value</param>
		public SlopQueryNode(QueryNode query, int value)
		{
			if (query == null)
			{
				throw new QueryNodeError(new MessageImpl(QueryParserMessages.NODE_ACTION_NOT_SUPPORTED
					, "query", "null"));
			}
			this.value = value;
			SetLeaf(false);
			Allocate();
			Add(query);
		}

		public virtual QueryNode GetChild()
		{
			return GetChildren()[0];
		}

		public virtual int GetValue()
		{
			return this.value;
		}

		private CharSequence GetValueString()
		{
			float f = float.ValueOf(this.value);
			if (f == f)
			{
				return string.Empty + f;
			}
			else
			{
				return string.Empty + f;
			}
		}

		public override string ToString()
		{
			return "<slop value='" + GetValueString() + "'>" + "\n" + GetChild().ToString() +
				 "\n</slop>";
		}

		public override CharSequence ToQueryString(EscapeQuerySyntax escapeSyntaxParser)
		{
			if (GetChild() == null)
			{
				return string.Empty;
			}
			return GetChild().ToQueryString(escapeSyntaxParser) + "~" + GetValueString();
		}

		/// <exception cref="Sharpen.CloneNotSupportedException"></exception>
		public override QueryNode CloneTree()
		{
			Lucene.Net.Queryparser.Flexible.Core.Nodes.SlopQueryNode clone = (Lucene.Net.Queryparser.Flexible.Core.Nodes.SlopQueryNode
				)base.CloneTree();
			clone.value = this.value;
			return clone;
		}

		public virtual CharSequence GetField()
		{
			QueryNode child = GetChild();
			if (child is FieldableNode)
			{
				return ((FieldableNode)child).GetField();
			}
			return null;
		}

		public virtual void SetField(CharSequence fieldName)
		{
			QueryNode child = GetChild();
			if (child is FieldableNode)
			{
				((FieldableNode)child).SetField(fieldName);
			}
		}
	}
}
