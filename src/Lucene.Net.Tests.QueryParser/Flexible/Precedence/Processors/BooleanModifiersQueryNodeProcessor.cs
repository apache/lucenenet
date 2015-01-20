/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Processors;
using Org.Apache.Lucene.Queryparser.Flexible.Standard.Config;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Flexible.Precedence.Processors
{
	/// <summary>
	/// <p>
	/// This processor is used to apply the correct
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.ModifierQueryNode">Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.ModifierQueryNode
	/// 	</see>
	/// to
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.BooleanQueryNode">Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.BooleanQueryNode
	/// 	</see>
	/// s children.
	/// </p>
	/// <p>
	/// It walks through the query node tree looking for
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.BooleanQueryNode">Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.BooleanQueryNode
	/// 	</see>
	/// s. If an
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.AndQueryNode">Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.AndQueryNode
	/// 	</see>
	/// is found,
	/// every child, which is not a
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.ModifierQueryNode">Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.ModifierQueryNode
	/// 	</see>
	/// or the
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.ModifierQueryNode">Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.ModifierQueryNode
	/// 	</see>
	/// 
	/// is
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.ModifierQueryNode.Modifier.MOD_NONE
	/// 	">Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.ModifierQueryNode.Modifier.MOD_NONE
	/// 	</see>
	/// , becomes a
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.ModifierQueryNode.Modifier.MOD_REQ
	/// 	">Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.ModifierQueryNode.Modifier.MOD_REQ
	/// 	</see>
	/// . For any other
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.BooleanQueryNode">Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.BooleanQueryNode
	/// 	</see>
	/// which is not an
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.OrQueryNode">Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.OrQueryNode
	/// 	</see>
	/// , it checks the default operator is
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler.Operator.AND
	/// 	">Org.Apache.Lucene.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler.Operator.AND
	/// 	</see>
	/// ,
	/// if it is, the same operation when an
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.AndQueryNode">Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.AndQueryNode
	/// 	</see>
	/// is found is applied to it.
	/// </p>
	/// </summary>
	/// <seealso cref="Org.Apache.Lucene.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler.ConfigurationKeys.DEFAULT_OPERATOR
	/// 	">Org.Apache.Lucene.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler.ConfigurationKeys.DEFAULT_OPERATOR
	/// 	</seealso>
	/// <seealso cref="Org.Apache.Lucene.Queryparser.Flexible.Standard.StandardQueryParser.SetDefaultOperator(Org.Apache.Lucene.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler.Operator)
	/// 	">Org.Apache.Lucene.Queryparser.Flexible.Standard.StandardQueryParser.SetDefaultOperator(Org.Apache.Lucene.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler.Operator)
	/// 	</seealso>
	public class BooleanModifiersQueryNodeProcessor : QueryNodeProcessorImpl
	{
		private AList<QueryNode> childrenBuffer = new AList<QueryNode>();

		private bool usingAnd = false;

		public BooleanModifiersQueryNodeProcessor()
		{
		}

		// empty constructor
		/// <exception cref="Org.Apache.Lucene.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		public override QueryNode Process(QueryNode queryTree)
		{
			StandardQueryConfigHandler.Operator op = GetQueryConfigHandler().Get(StandardQueryConfigHandler.ConfigurationKeys
				.DEFAULT_OPERATOR);
			if (op == null)
			{
				throw new ArgumentException("StandardQueryConfigHandler.ConfigurationKeys.DEFAULT_OPERATOR should be set on the QueryConfigHandler"
					);
			}
			this.usingAnd = StandardQueryConfigHandler.Operator.AND == op;
			return base.Process(queryTree);
		}

		/// <exception cref="Org.Apache.Lucene.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		protected internal override QueryNode PostProcessNode(QueryNode node)
		{
			if (node is AndQueryNode)
			{
				this.childrenBuffer.Clear();
				IList<QueryNode> children = node.GetChildren();
				foreach (QueryNode child in children)
				{
					this.childrenBuffer.AddItem(ApplyModifier(child, ModifierQueryNode.Modifier.MOD_REQ
						));
				}
				node.Set(this.childrenBuffer);
			}
			else
			{
				if (this.usingAnd && node is BooleanQueryNode && !(node is OrQueryNode))
				{
					this.childrenBuffer.Clear();
					IList<QueryNode> children = node.GetChildren();
					foreach (QueryNode child in children)
					{
						this.childrenBuffer.AddItem(ApplyModifier(child, ModifierQueryNode.Modifier.MOD_REQ
							));
					}
					node.Set(this.childrenBuffer);
				}
			}
			return node;
		}

		private QueryNode ApplyModifier(QueryNode node, ModifierQueryNode.Modifier mod)
		{
			// check if modifier is not already defined and is default
			if (!(node is ModifierQueryNode))
			{
				return new ModifierQueryNode(node, mod);
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

		/// <exception cref="Org.Apache.Lucene.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		protected internal override QueryNode PreProcessNode(QueryNode node)
		{
			return node;
		}

		/// <exception cref="Org.Apache.Lucene.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		protected internal override IList<QueryNode> SetChildrenOrder(IList<QueryNode> children
			)
		{
			return children;
		}
	}
}
