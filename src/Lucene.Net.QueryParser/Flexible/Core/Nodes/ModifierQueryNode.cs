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
	/// <see cref="ModifierQueryNode">ModifierQueryNode</see>
	/// indicates the modifier value (+,-,?,NONE) for
	/// each term on the query string. For example "+t1 -t2 t3" will have a tree of:
	/// <blockquote>
	/// &lt;BooleanQueryNode&gt; &lt;ModifierQueryNode modifier="MOD_REQ"&gt; &lt;t1/&gt;
	/// &lt;/ModifierQueryNode&gt; &lt;ModifierQueryNode modifier="MOD_NOT"&gt; &lt;t2/&gt;
	/// &lt;/ModifierQueryNode&gt; &lt;t3/&gt; &lt;/BooleanQueryNode&gt;
	/// </blockquote>
	/// </summary>
	public class ModifierQueryNode : QueryNodeImpl
	{
		/// <summary>Modifier type: such as required (REQ), prohibited (NOT)</summary>
		public enum Modifier
		{
			MOD_NONE,
			MOD_NOT,
			MOD_REQ
		}

		internal class ModifierHelper
		{
			//HM:revisit
			public static string ToString(ModifierQueryNode.Modifier mod)
			{
				switch (mod)
				{
					case ModifierQueryNode.Modifier.MOD_NONE:
					{
						return "MOD_NONE";
					}

					case ModifierQueryNode.Modifier.MOD_NOT:
					{
						return "MOD_NOT";
					}

					case ModifierQueryNode.Modifier.MOD_REQ:
					{
						return "MOD_REQ";
					}
				}
				// this code is never executed
				return "MOD_DEFAULT";
			}

			public static string ToDigitString(ModifierQueryNode.Modifier mod)
			{
				switch (mod)
				{
					case ModifierQueryNode.Modifier.MOD_NONE:
					{
						return string.Empty;
					}

					case ModifierQueryNode.Modifier.MOD_NOT:
					{
						return "-";
					}

					case ModifierQueryNode.Modifier.MOD_REQ:
					{
						return "+";
					}
				}
				// this code is never executed
				return string.Empty;
			}

			public static string ToLargeString(ModifierQueryNode.Modifier mod)
			{
				switch (mod)
				{
					case ModifierQueryNode.Modifier.MOD_NONE:
					{
						return string.Empty;
					}

					case ModifierQueryNode.Modifier.MOD_NOT:
					{
						return "NOT ";
					}

					case ModifierQueryNode.Modifier.MOD_REQ:
					{
						return "+";
					}
				}
				// this code is never executed
				return string.Empty;
			}
		}

		private ModifierQueryNode.Modifier modifier = ModifierQueryNode.Modifier.MOD_NONE;

		/// <summary>Used to store the modifier value on the original query string</summary>
		/// <param name="query">- QueryNode subtree</param>
		/// <param name="mod">- Modifier Value</param>
		public ModifierQueryNode(QueryNode query, ModifierQueryNode.Modifier mod)
		{
			if (query == null)
			{
				throw new QueryNodeError(new MessageImpl(QueryParserMessages.PARAMETER_VALUE_NOT_SUPPORTED
					, "query", "null"));
			}
			Allocate();
			SetLeaf(false);
			Add(query);
			this.modifier = mod;
		}

		public virtual QueryNode GetChild()
		{
			return GetChildren()[0];
		}

		public virtual ModifierQueryNode.Modifier GetModifier()
		{
			return this.modifier;
		}

		public override string ToString()
		{
			return "<modifier operation='" + this.modifier.ToString() + "'>" + "\n" + GetChild
				().ToString() + "\n</modifier>";
		}

		public override CharSequence ToQueryString(EscapeQuerySyntax escapeSyntaxParser)
		{
			if (GetChild() == null)
			{
				return string.Empty;
			}
			string leftParenthensis = string.Empty;
			string rightParenthensis = string.Empty;
			if (GetChild() != null && GetChild() is ModifierQueryNode)
			{
				leftParenthensis = "(";
				rightParenthensis = ")";
			}
			if (GetChild() is BooleanQueryNode)
			{
				return ModifierQueryNode.ModifierHelper.ToLargeString(this.modifier) + leftParenthensis
					 + GetChild().ToQueryString(escapeSyntaxParser) + rightParenthensis;
			}
			else
			{
				return ModifierQueryNode.ModifierHelper.ToDigitString(this.modifier) + leftParenthensis
					 + GetChild().ToQueryString(escapeSyntaxParser) + rightParenthensis;
			}
		}

		/// <exception cref="Sharpen.CloneNotSupportedException"></exception>
		public override QueryNode CloneTree()
		{
			ModifierQueryNode clone = (ModifierQueryNode)base.CloneTree();
			clone.modifier = this.modifier;
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
