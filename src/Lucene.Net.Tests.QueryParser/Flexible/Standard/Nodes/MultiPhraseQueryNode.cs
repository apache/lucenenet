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

namespace Org.Apache.Lucene.Queryparser.Flexible.Standard.Nodes
{
	/// <summary>
	/// A
	/// <see cref="MultiPhraseQueryNode">MultiPhraseQueryNode</see>
	/// indicates that its children should be used to
	/// build a
	/// <see cref="Org.Apache.Lucene.Search.MultiPhraseQuery">Org.Apache.Lucene.Search.MultiPhraseQuery
	/// 	</see>
	/// instead of
	/// <see cref="Org.Apache.Lucene.Search.PhraseQuery">Org.Apache.Lucene.Search.PhraseQuery
	/// 	</see>
	/// .
	/// </summary>
	public class MultiPhraseQueryNode : QueryNodeImpl, FieldableNode
	{
		public MultiPhraseQueryNode()
		{
			SetLeaf(false);
			Allocate();
		}

		public override string ToString()
		{
			if (GetChildren() == null || GetChildren().Count == 0)
			{
				return "<multiPhrase/>";
			}
			StringBuilder sb = new StringBuilder();
			sb.Append("<multiPhrase>");
			foreach (QueryNode child in GetChildren())
			{
				sb.Append("\n");
				sb.Append(child.ToString());
			}
			sb.Append("\n</multiPhrase>");
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
				filler = ",";
			}
			return "[MTP[" + sb.ToString() + "]]";
		}

		/// <exception cref="Sharpen.CloneNotSupportedException"></exception>
		public override QueryNode CloneTree()
		{
			Org.Apache.Lucene.Queryparser.Flexible.Standard.Nodes.MultiPhraseQueryNode clone = 
				(Org.Apache.Lucene.Queryparser.Flexible.Standard.Nodes.MultiPhraseQueryNode)base
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
				foreach (QueryNode child in children)
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
