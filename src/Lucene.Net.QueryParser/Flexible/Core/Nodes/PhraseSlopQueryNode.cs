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
	/// Query node for
	/// <see cref="Lucene.Net.Search.PhraseQuery">Lucene.Net.Search.PhraseQuery
	/// 	</see>
	/// 's slop factor.
	/// </summary>
	public class PhraseSlopQueryNode : QueryNodeImpl, FieldableNode
	{
		private int value = 0;

		/// <exception>
		/// QueryNodeError
		/// throw in overridden method to disallow
		/// </exception>
		public PhraseSlopQueryNode(QueryNode query, int value)
		{
			// javadocs
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
			return "<phraseslop value='" + GetValueString() + "'>" + "\n" + GetChild().ToString
				() + "\n</phraseslop>";
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
			Lucene.Net.Queryparser.Flexible.Core.Nodes.PhraseSlopQueryNode clone = (Lucene.Net.Queryparser.Flexible.Core.Nodes.PhraseSlopQueryNode
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
