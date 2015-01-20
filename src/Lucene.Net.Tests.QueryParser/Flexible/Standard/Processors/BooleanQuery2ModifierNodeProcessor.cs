/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Config;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Processors;
using Org.Apache.Lucene.Queryparser.Flexible.Standard.Config;
using Org.Apache.Lucene.Queryparser.Flexible.Standard.Nodes;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Flexible.Standard.Processors
{
	/// <summary>
	/// <p>
	/// This processor is used to apply the correct
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.ModifierQueryNode">Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.ModifierQueryNode
	/// 	</see>
	/// to
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.BooleanQueryNode">Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.BooleanQueryNode
	/// 	</see>
	/// s children. This is a variant of
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Precedence.Processors.BooleanModifiersQueryNodeProcessor
	/// 	">Org.Apache.Lucene.Queryparser.Flexible.Precedence.Processors.BooleanModifiersQueryNodeProcessor
	/// 	</see>
	/// which ignores precedence.
	/// </p>
	/// <p>
	/// The
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Standard.Parser.StandardSyntaxParser
	/// 	">Org.Apache.Lucene.Queryparser.Flexible.Standard.Parser.StandardSyntaxParser</see>
	/// knows the rules of precedence, but lucene
	/// does not. e.g. <code>(A AND B OR C AND D)</code> ist treated like
	/// <code>(+A +B +C +D)</code>.
	/// </p>
	/// <p>
	/// This processor walks through the query node tree looking for
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.BooleanQueryNode">Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.BooleanQueryNode
	/// 	</see>
	/// s. If an
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.AndQueryNode">Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.AndQueryNode
	/// 	</see>
	/// is found, every child,
	/// which is not a
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.ModifierQueryNode">Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.ModifierQueryNode
	/// 	</see>
	/// or the
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.ModifierQueryNode">Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.ModifierQueryNode
	/// 	</see>
	/// is
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.ModifierQueryNode.Modifier.MOD_NONE
	/// 	">Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.ModifierQueryNode.Modifier.MOD_NONE
	/// 	</see>
	/// , becomes a
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.ModifierQueryNode.Modifier.MOD_REQ
	/// 	">Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.ModifierQueryNode.Modifier.MOD_REQ
	/// 	</see>
	/// . For default
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.BooleanQueryNode">Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.BooleanQueryNode
	/// 	</see>
	/// , it checks the default operator is
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler.Operator.AND
	/// 	">Org.Apache.Lucene.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler.Operator.AND
	/// 	</see>
	/// , if it is, the same operation when an
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.AndQueryNode">Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.AndQueryNode
	/// 	</see>
	/// is found is applied to it. Each
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.BooleanQueryNode">Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.BooleanQueryNode
	/// 	</see>
	/// which direct parent is also a
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.BooleanQueryNode">Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.BooleanQueryNode
	/// 	</see>
	/// is removed (to ignore
	/// the rules of precedence).
	/// </p>
	/// </summary>
	/// <seealso cref="Org.Apache.Lucene.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler.ConfigurationKeys.DEFAULT_OPERATOR
	/// 	">Org.Apache.Lucene.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler.ConfigurationKeys.DEFAULT_OPERATOR
	/// 	</seealso>
	/// <seealso cref="Org.Apache.Lucene.Queryparser.Flexible.Precedence.Processors.BooleanModifiersQueryNodeProcessor
	/// 	">Org.Apache.Lucene.Queryparser.Flexible.Precedence.Processors.BooleanModifiersQueryNodeProcessor
	/// 	</seealso>
	public class BooleanQuery2ModifierNodeProcessor : QueryNodeProcessor
	{
		internal static readonly string TAG_REMOVE = "remove";

		internal static readonly string TAG_MODIFIER = "wrapWithModifier";

		internal static readonly string TAG_BOOLEAN_ROOT = "booleanRoot";

		internal QueryConfigHandler queryConfigHandler;

		private readonly AList<QueryNode> childrenBuffer = new AList<QueryNode>();

		private bool usingAnd = false;

		public BooleanQuery2ModifierNodeProcessor()
		{
		}

		// empty constructor
		/// <exception cref="Org.Apache.Lucene.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		public virtual QueryNode Process(QueryNode queryTree)
		{
			StandardQueryConfigHandler.Operator op = GetQueryConfigHandler().Get(StandardQueryConfigHandler.ConfigurationKeys
				.DEFAULT_OPERATOR);
			if (op == null)
			{
				throw new ArgumentException("StandardQueryConfigHandler.ConfigurationKeys.DEFAULT_OPERATOR should be set on the QueryConfigHandler"
					);
			}
			this.usingAnd = StandardQueryConfigHandler.Operator.AND == op;
			return ProcessIteration(queryTree);
		}

		/// <exception cref="Org.Apache.Lucene.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		protected internal virtual void ProcessChildren(QueryNode queryTree)
		{
			IList<QueryNode> children = queryTree.GetChildren();
			if (children != null && children.Count > 0)
			{
				foreach (QueryNode child in children)
				{
					child = ProcessIteration(child);
				}
			}
		}

		/// <exception cref="Org.Apache.Lucene.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		private QueryNode ProcessIteration(QueryNode queryTree)
		{
			queryTree = PreProcessNode(queryTree);
			ProcessChildren(queryTree);
			queryTree = PostProcessNode(queryTree);
			return queryTree;
		}

		protected internal virtual void FillChildrenBufferAndApplyModifiery(QueryNode parent
			)
		{
			foreach (QueryNode node in parent.GetChildren())
			{
				if (node.ContainsTag(TAG_REMOVE))
				{
					FillChildrenBufferAndApplyModifiery(node);
				}
				else
				{
					if (node.ContainsTag(TAG_MODIFIER))
					{
						childrenBuffer.AddItem(ApplyModifier(node, (ModifierQueryNode.Modifier)node.GetTag
							(TAG_MODIFIER)));
					}
					else
					{
						childrenBuffer.AddItem(node);
					}
				}
			}
		}

		/// <exception cref="Org.Apache.Lucene.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		protected internal virtual QueryNode PostProcessNode(QueryNode node)
		{
			if (node.ContainsTag(TAG_BOOLEAN_ROOT))
			{
				this.childrenBuffer.Clear();
				FillChildrenBufferAndApplyModifiery(node);
				node.Set(childrenBuffer);
			}
			return node;
		}

		/// <exception cref="Org.Apache.Lucene.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		protected internal virtual QueryNode PreProcessNode(QueryNode node)
		{
			QueryNode parent = node.GetParent();
			if (node is BooleanQueryNode)
			{
				if (parent is BooleanQueryNode)
				{
					node.SetTag(TAG_REMOVE, true);
				}
				else
				{
					// no precedence
					node.SetTag(TAG_BOOLEAN_ROOT, true);
				}
			}
			else
			{
				if (parent is BooleanQueryNode)
				{
					if ((parent is AndQueryNode) || (usingAnd && IsDefaultBooleanQueryNode(parent)))
					{
						TagModifierButDoNotOverride(node, ModifierQueryNode.Modifier.MOD_REQ);
					}
				}
			}
			return node;
		}

		protected internal virtual bool IsDefaultBooleanQueryNode(QueryNode toTest)
		{
			return toTest != null && typeof(BooleanQueryNode).Equals(toTest.GetType());
		}

		private QueryNode ApplyModifier(QueryNode node, ModifierQueryNode.Modifier mod)
		{
			// check if modifier is not already defined and is default
			if (!(node is ModifierQueryNode))
			{
				return new BooleanModifierNode(node, mod);
			}
			else
			{
				ModifierQueryNode modNode = (ModifierQueryNode)node;
				if (modNode.GetModifier() == ModifierQueryNode.Modifier.MOD_NONE)
				{
					return new ModifierQueryNode(modNode.GetChild(), mod);
				}
			}
			return node;
		}

		protected internal virtual void TagModifierButDoNotOverride(QueryNode node, ModifierQueryNode.Modifier
			 mod)
		{
			if (node is ModifierQueryNode)
			{
				ModifierQueryNode modNode = (ModifierQueryNode)node;
				if (modNode.GetModifier() == ModifierQueryNode.Modifier.MOD_NONE)
				{
					node.SetTag(TAG_MODIFIER, mod);
				}
			}
			else
			{
				node.SetTag(TAG_MODIFIER, ModifierQueryNode.Modifier.MOD_REQ);
			}
		}

		public virtual void SetQueryConfigHandler(QueryConfigHandler queryConfigHandler)
		{
			this.queryConfigHandler = queryConfigHandler;
		}

		public virtual QueryConfigHandler GetQueryConfigHandler()
		{
			return queryConfigHandler;
		}
	}
}
