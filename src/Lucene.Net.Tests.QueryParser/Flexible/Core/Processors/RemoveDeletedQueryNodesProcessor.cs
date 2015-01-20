/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Processors;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Flexible.Core.Processors
{
	/// <summary>
	/// A
	/// <see cref="QueryNodeProcessorPipeline">QueryNodeProcessorPipeline</see>
	/// class removes every instance of
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.DeletedQueryNode">Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.DeletedQueryNode
	/// 	</see>
	/// from a query node tree. If the resulting root node
	/// is a
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.DeletedQueryNode">Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.DeletedQueryNode
	/// 	</see>
	/// ,
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.MatchNoDocsQueryNode
	/// 	">Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.MatchNoDocsQueryNode</see>
	/// is returned.
	/// </summary>
	public class RemoveDeletedQueryNodesProcessor : QueryNodeProcessorImpl
	{
		public RemoveDeletedQueryNodesProcessor()
		{
		}

		// empty constructor
		/// <exception cref="Org.Apache.Lucene.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		public override QueryNode Process(QueryNode queryTree)
		{
			queryTree = base.Process(queryTree);
			if (queryTree is DeletedQueryNode && !(queryTree is MatchNoDocsQueryNode))
			{
				return new MatchNoDocsQueryNode();
			}
			return queryTree;
		}

		/// <exception cref="Org.Apache.Lucene.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		protected internal override QueryNode PostProcessNode(QueryNode node)
		{
			if (!node.IsLeaf())
			{
				IList<QueryNode> children = node.GetChildren();
				bool removeBoolean = false;
				if (children == null || children.Count == 0)
				{
					removeBoolean = true;
				}
				else
				{
					removeBoolean = true;
					for (Iterator<QueryNode> it = children.Iterator(); it.HasNext(); )
					{
						if (!(it.Next() is DeletedQueryNode))
						{
							removeBoolean = false;
							break;
						}
					}
				}
				if (removeBoolean)
				{
					return new DeletedQueryNode();
				}
			}
			return node;
		}

		/// <exception cref="Org.Apache.Lucene.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		protected internal override IList<QueryNode> SetChildrenOrder(IList<QueryNode> children
			)
		{
			for (int i = 0; i < children.Count; i++)
			{
				if (children[i] is DeletedQueryNode)
				{
					children.Remove(i--);
				}
			}
			return children;
		}

		/// <exception cref="Org.Apache.Lucene.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		protected internal override QueryNode PreProcessNode(QueryNode node)
		{
			return node;
		}
	}
}
