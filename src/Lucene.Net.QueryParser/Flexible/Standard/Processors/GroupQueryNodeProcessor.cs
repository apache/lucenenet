/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Lucene.Net.Queryparser.Flexible.Core.Config;
using Lucene.Net.Queryparser.Flexible.Core.Nodes;
using Lucene.Net.Queryparser.Flexible.Core.Processors;
using Lucene.Net.Queryparser.Flexible.Standard.Config;
using Lucene.Net.Queryparser.Flexible.Standard.Nodes;
using Sharpen;

namespace Lucene.Net.Queryparser.Flexible.Standard.Processors
{
	/// <summary>
	/// The
	/// <see cref="Lucene.Net.Queryparser.Flexible.Core.Parser.SyntaxParser">Lucene.Net.Queryparser.Flexible.Core.Parser.SyntaxParser
	/// 	</see>
	/// generates query node trees that consider the boolean operator precedence, but
	/// Lucene current syntax does not support boolean precedence, so this processor
	/// remove all the precedence and apply the equivalent modifier according to the
	/// boolean operation defined on an specific query node. <br/>
	/// <br/>
	/// If there is a
	/// <see cref="Lucene.Net.Queryparser.Flexible.Core.Nodes.GroupQueryNode">Lucene.Net.Queryparser.Flexible.Core.Nodes.GroupQueryNode
	/// 	</see>
	/// in the query node tree, the query node
	/// tree is not merged with the one above it.
	/// Example: TODO: describe a good example to show how this processor works
	/// </summary>
	/// <seealso cref="Lucene.Net.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler
	/// 	">Lucene.Net.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler
	/// 	</seealso>
	[System.ObsoleteAttribute(@"use BooleanQuery2ModifierNodeProcessor instead")]
	public class GroupQueryNodeProcessor : QueryNodeProcessor
	{
		private AList<QueryNode> queryNodeList;

		private bool latestNodeVerified;

		private QueryConfigHandler queryConfig;

		private bool usingAnd = false;

		public GroupQueryNodeProcessor()
		{
		}

		// empty constructor
		/// <exception cref="Lucene.Net.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		public virtual QueryNode Process(QueryNode queryTree)
		{
			StandardQueryConfigHandler.Operator defaultOperator = GetQueryConfigHandler().Get
				(StandardQueryConfigHandler.ConfigurationKeys.DEFAULT_OPERATOR);
			if (defaultOperator == null)
			{
				throw new ArgumentException("DEFAULT_OPERATOR should be set on the QueryConfigHandler"
					);
			}
			this.usingAnd = StandardQueryConfigHandler.Operator.AND == defaultOperator;
			if (queryTree is GroupQueryNode)
			{
				queryTree = ((GroupQueryNode)queryTree).GetChild();
			}
			this.queryNodeList = new AList<QueryNode>();
			this.latestNodeVerified = false;
			ReadTree(queryTree);
			IList<QueryNode> actualQueryNodeList = this.queryNodeList;
			for (int i = 0; i < actualQueryNodeList.Count; i++)
			{
				QueryNode node = actualQueryNodeList[i];
				if (node is GroupQueryNode)
				{
					actualQueryNodeList.Set(i, Process(node));
				}
			}
			this.usingAnd = false;
			if (queryTree is BooleanQueryNode)
			{
				queryTree.Set(actualQueryNodeList);
				return queryTree;
			}
			else
			{
				return new BooleanQueryNode(actualQueryNodeList);
			}
		}

		private QueryNode ApplyModifier(QueryNode node, QueryNode parent)
		{
			if (this.usingAnd)
			{
				if (parent is OrQueryNode)
				{
					if (node is ModifierQueryNode)
					{
						ModifierQueryNode modNode = (ModifierQueryNode)node;
						if (modNode.GetModifier() == ModifierQueryNode.Modifier.MOD_REQ)
						{
							return modNode.GetChild();
						}
					}
				}
				else
				{
					if (node is ModifierQueryNode)
					{
						ModifierQueryNode modNode = (ModifierQueryNode)node;
						if (modNode.GetModifier() == ModifierQueryNode.Modifier.MOD_NONE)
						{
							return new BooleanModifierNode(modNode.GetChild(), ModifierQueryNode.Modifier.MOD_REQ
								);
						}
					}
					else
					{
						return new BooleanModifierNode(node, ModifierQueryNode.Modifier.MOD_REQ);
					}
				}
			}
			else
			{
				if (node.GetParent() is AndQueryNode)
				{
					if (node is ModifierQueryNode)
					{
						ModifierQueryNode modNode = (ModifierQueryNode)node;
						if (modNode.GetModifier() == ModifierQueryNode.Modifier.MOD_NONE)
						{
							return new BooleanModifierNode(modNode.GetChild(), ModifierQueryNode.Modifier.MOD_REQ
								);
						}
					}
					else
					{
						return new BooleanModifierNode(node, ModifierQueryNode.Modifier.MOD_REQ);
					}
				}
			}
			return node;
		}

		private void ReadTree(QueryNode node)
		{
			if (node is BooleanQueryNode)
			{
				IList<QueryNode> children = node.GetChildren();
				if (children != null && children.Count > 0)
				{
					for (int i = 0; i < children.Count - 1; i++)
					{
						ReadTree(children[i]);
					}
					ProcessNode(node);
					ReadTree(children[children.Count - 1]);
				}
				else
				{
					ProcessNode(node);
				}
			}
			else
			{
				ProcessNode(node);
			}
		}

		private void ProcessNode(QueryNode node)
		{
			if (node is AndQueryNode || node is OrQueryNode)
			{
				if (!this.latestNodeVerified && !this.queryNodeList.IsEmpty())
				{
					this.queryNodeList.AddItem(ApplyModifier(this.queryNodeList.Remove(this.queryNodeList
						.Count - 1), node));
					this.latestNodeVerified = true;
				}
			}
			else
			{
				if (!(node is BooleanQueryNode))
				{
					this.queryNodeList.AddItem(ApplyModifier(node, node.GetParent()));
					this.latestNodeVerified = false;
				}
			}
		}

		public virtual QueryConfigHandler GetQueryConfigHandler()
		{
			return this.queryConfig;
		}

		public virtual void SetQueryConfigHandler(QueryConfigHandler queryConfigHandler)
		{
			this.queryConfig = queryConfigHandler;
		}
	}
}
