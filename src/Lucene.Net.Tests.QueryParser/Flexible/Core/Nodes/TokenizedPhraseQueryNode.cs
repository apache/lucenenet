/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using System.Text;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Parser;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes
{
	/// <summary>
	/// A
	/// <see cref="TokenizedPhraseQueryNode">TokenizedPhraseQueryNode</see>
	/// represents a node created by a code that
	/// tokenizes/lemmatizes/analyzes.
	/// </summary>
	public class TokenizedPhraseQueryNode : QueryNodeImpl, FieldableNode
	{
		public TokenizedPhraseQueryNode()
		{
			SetLeaf(false);
			Allocate();
		}

		public override string ToString()
		{
			if (GetChildren() == null || GetChildren().Count == 0)
			{
				return "<tokenizedphrase/>";
			}
			StringBuilder sb = new StringBuilder();
			sb.Append("<tokenizedtphrase>");
			foreach (QueryNode child in GetChildren())
			{
				sb.Append("\n");
				sb.Append(child.ToString());
			}
			sb.Append("\n</tokenizedphrase>");
			return sb.ToString();
		}

		// This text representation is not re-parseable
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
				filler = ",";
			}
			return "[TP[" + sb.ToString() + "]]";
		}

		/// <exception cref="Sharpen.CloneNotSupportedException"></exception>
		public override QueryNode CloneTree()
		{
			Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.TokenizedPhraseQueryNode clone = 
				(Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.TokenizedPhraseQueryNode)base
				.CloneTree();
			// nothing to do
			return clone;
		}

		public virtual CharSequence GetField()
		{
			IList<QueryNode> children = GetChildren();
			if (children == null || children.Count == 0)
			{
				return null;
			}
			else
			{
				return ((FieldableNode)children[0]).GetField();
			}
		}

		public virtual void SetField(CharSequence fieldName)
		{
			IList<QueryNode> children = GetChildren();
			if (children != null)
			{
				foreach (QueryNode child in GetChildren())
				{
					if (child is FieldableNode)
					{
						((FieldableNode)child).SetField(fieldName);
					}
				}
			}
		}
	}
}
