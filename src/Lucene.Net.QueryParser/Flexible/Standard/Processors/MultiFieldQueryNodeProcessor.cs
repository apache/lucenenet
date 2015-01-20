/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Lucene.Net.Queryparser.Flexible.Core.Nodes;
using Lucene.Net.Queryparser.Flexible.Core.Processors;
using Lucene.Net.Queryparser.Flexible.Standard.Config;
using Sharpen;

namespace Lucene.Net.Queryparser.Flexible.Standard.Processors
{
	/// <summary>
	/// This processor is used to expand terms so the query looks for the same term
	/// in different fields.
	/// </summary>
	/// <remarks>
	/// This processor is used to expand terms so the query looks for the same term
	/// in different fields. It also boosts a query based on its field. <br/>
	/// <br/>
	/// This processor looks for every
	/// <see cref="Lucene.Net.Queryparser.Flexible.Core.Nodes.FieldableNode">Lucene.Net.Queryparser.Flexible.Core.Nodes.FieldableNode
	/// 	</see>
	/// contained in the query
	/// node tree. If a
	/// <see cref="Lucene.Net.Queryparser.Flexible.Core.Nodes.FieldableNode">Lucene.Net.Queryparser.Flexible.Core.Nodes.FieldableNode
	/// 	</see>
	/// is found, it checks if there is a
	/// <see cref="Lucene.Net.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler.ConfigurationKeys.MULTI_FIELDS
	/// 	">Lucene.Net.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler.ConfigurationKeys.MULTI_FIELDS
	/// 	</see>
	/// defined in the
	/// <see cref="Lucene.Net.Queryparser.Flexible.Core.Config.QueryConfigHandler"
	/// 	>Lucene.Net.Queryparser.Flexible.Core.Config.QueryConfigHandler</see>
	/// . If
	/// there is, the
	/// <see cref="Lucene.Net.Queryparser.Flexible.Core.Nodes.FieldableNode">Lucene.Net.Queryparser.Flexible.Core.Nodes.FieldableNode
	/// 	</see>
	/// is cloned N times and the clones are
	/// added to a
	/// <see cref="Lucene.Net.Queryparser.Flexible.Core.Nodes.BooleanQueryNode">Lucene.Net.Queryparser.Flexible.Core.Nodes.BooleanQueryNode
	/// 	</see>
	/// together with the original node. N is
	/// defined by the number of fields that it will be expanded to. The
	/// <see cref="Lucene.Net.Queryparser.Flexible.Core.Nodes.BooleanQueryNode">Lucene.Net.Queryparser.Flexible.Core.Nodes.BooleanQueryNode
	/// 	</see>
	/// is returned. <br/>
	/// </remarks>
	/// <seealso cref="Lucene.Net.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler.ConfigurationKeys.MULTI_FIELDS
	/// 	">Lucene.Net.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler.ConfigurationKeys.MULTI_FIELDS
	/// 	</seealso>
	public class MultiFieldQueryNodeProcessor : QueryNodeProcessorImpl
	{
		private bool processChildren = true;

		public MultiFieldQueryNodeProcessor()
		{
		}

		// empty constructor
		/// <exception cref="Lucene.Net.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		protected internal override QueryNode PostProcessNode(QueryNode node)
		{
			return node;
		}

		/// <exception cref="Lucene.Net.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		protected internal override void ProcessChildren(QueryNode queryTree)
		{
			if (this.processChildren)
			{
				base.ProcessChildren(queryTree);
			}
			else
			{
				this.processChildren = true;
			}
		}

		/// <exception cref="Lucene.Net.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		protected internal override QueryNode PreProcessNode(QueryNode node)
		{
			if (node is FieldableNode)
			{
				this.processChildren = false;
				FieldableNode fieldNode = (FieldableNode)node;
				if (fieldNode.GetField() == null)
				{
					CharSequence[] fields = GetQueryConfigHandler().Get(StandardQueryConfigHandler.ConfigurationKeys
						.MULTI_FIELDS);
					if (fields == null)
					{
						throw new ArgumentException("StandardQueryConfigHandler.ConfigurationKeys.MULTI_FIELDS should be set on the QueryConfigHandler"
							);
					}
					if (fields != null && fields.Length > 0)
					{
						fieldNode.SetField(fields[0]);
						if (fields.Length == 1)
						{
							return fieldNode;
						}
						else
						{
							List<QueryNode> children = new List<QueryNode>();
							children.AddItem(fieldNode);
							for (int i = 1; i < fields.Length; i++)
							{
								fieldNode = (FieldableNode)fieldNode.CloneTree();
								fieldNode.SetField(fields[i]);
								children.AddItem(fieldNode);
							}
							// should never happen
							return new GroupQueryNode(new OrQueryNode(children));
						}
					}
				}
			}
			return node;
		}

		/// <exception cref="Lucene.Net.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		protected internal override IList<QueryNode> SetChildrenOrder(IList<QueryNode> children
			)
		{
			return children;
		}
	}
}
