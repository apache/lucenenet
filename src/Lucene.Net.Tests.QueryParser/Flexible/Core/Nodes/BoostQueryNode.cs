/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Org.Apache.Lucene.Queryparser.Flexible.Core;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Messages;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Parser;
using Org.Apache.Lucene.Queryparser.Flexible.Messages;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes
{
	/// <summary>
	/// A
	/// <see cref="BoostQueryNode">BoostQueryNode</see>
	/// boosts the QueryNode tree which is under this node.
	/// So, it must only and always have one child.
	/// The boost value may vary from 0.0 to 1.0.
	/// </summary>
	public class BoostQueryNode : QueryNodeImpl
	{
		private float value = 0;

		/// <summary>Constructs a boost node</summary>
		/// <param name="query">the query to be boosted</param>
		/// <param name="value">the boost value, it may vary from 0.0 to 1.0</param>
		public BoostQueryNode(QueryNode query, float value)
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

		/// <summary>Returns the single child which this node boosts.</summary>
		/// <remarks>Returns the single child which this node boosts.</remarks>
		/// <returns>the single child which this node boosts</returns>
		public virtual QueryNode GetChild()
		{
			IList<QueryNode> children = GetChildren();
			if (children == null || children.Count == 0)
			{
				return null;
			}
			return children[0];
		}

		/// <summary>Returns the boost value.</summary>
		/// <remarks>Returns the boost value. It may vary from 0.0 to 1.0.</remarks>
		/// <returns>the boost value</returns>
		public virtual float GetValue()
		{
			return this.value;
		}

		/// <summary>Returns the boost value parsed to a string.</summary>
		/// <remarks>Returns the boost value parsed to a string.</remarks>
		/// <returns>the parsed value</returns>
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
			return "<boost value='" + GetValueString() + "'>" + "\n" + GetChild().ToString() 
				+ "\n</boost>";
		}

		public override CharSequence ToQueryString(EscapeQuerySyntax escapeSyntaxParser)
		{
			if (GetChild() == null)
			{
				return string.Empty;
			}
			return GetChild().ToQueryString(escapeSyntaxParser) + "^" + GetValueString();
		}

		/// <exception cref="Sharpen.CloneNotSupportedException"></exception>
		public override QueryNode CloneTree()
		{
			Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.BoostQueryNode clone = (Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.BoostQueryNode
				)base.CloneTree();
			clone.value = this.value;
			return clone;
		}
	}
}
