/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Lucene.Net.Queryparser.Flexible.Core.Nodes;
using Lucene.Net.Queryparser.Flexible.Core.Processors;
using Sharpen;

namespace Lucene.Net.Queryparser.Flexible.Standard.Processors
{
	/// <summary>
	/// This processor removes every
	/// <see cref="Lucene.Net.Queryparser.Flexible.Core.Nodes.QueryNode">Lucene.Net.Queryparser.Flexible.Core.Nodes.QueryNode
	/// 	</see>
	/// that is not a leaf and has not
	/// children. If after processing the entire tree the root node is not a leaf and
	/// has no children, a
	/// <see cref="Lucene.Net.Queryparser.Flexible.Core.Nodes.MatchNoDocsQueryNode
	/// 	">Lucene.Net.Queryparser.Flexible.Core.Nodes.MatchNoDocsQueryNode</see>
	/// object is returned. <br/>
	/// <br/>
	/// This processor is used at the end of a pipeline to avoid invalid query node
	/// tree structures like a
	/// <see cref="Lucene.Net.Queryparser.Flexible.Core.Nodes.GroupQueryNode">Lucene.Net.Queryparser.Flexible.Core.Nodes.GroupQueryNode
	/// 	</see>
	/// or
	/// <see cref="Lucene.Net.Queryparser.Flexible.Core.Nodes.ModifierQueryNode">Lucene.Net.Queryparser.Flexible.Core.Nodes.ModifierQueryNode
	/// 	</see>
	/// with no children. <br/>
	/// </summary>
	/// <seealso cref="Lucene.Net.Queryparser.Flexible.Core.Nodes.QueryNode">Lucene.Net.Queryparser.Flexible.Core.Nodes.QueryNode
	/// 	</seealso>
	/// <seealso cref="Lucene.Net.Queryparser.Flexible.Core.Nodes.MatchNoDocsQueryNode
	/// 	">Lucene.Net.Queryparser.Flexible.Core.Nodes.MatchNoDocsQueryNode</seealso>
	public class RemoveEmptyNonLeafQueryNodeProcessor : QueryNodeProcessorImpl
	{
		private List<QueryNode> childrenBuffer = new List<QueryNode>();

		public RemoveEmptyNonLeafQueryNodeProcessor()
		{
		}

		// empty constructor
		/// <exception cref="Lucene.Net.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		public override QueryNode Process(QueryNode queryTree)
		{
			queryTree = base.Process(queryTree);
			if (!queryTree.IsLeaf())
			{
				IList<QueryNode> children = queryTree.GetChildren();
				if (children == null || children.Count == 0)
				{
					return new MatchNoDocsQueryNode();
				}
			}
			return queryTree;
		}

		/// <exception cref="Lucene.Net.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		protected internal override QueryNode PostProcessNode(QueryNode node)
		{
			return node;
		}

		/// <exception cref="Lucene.Net.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		protected internal override QueryNode PreProcessNode(QueryNode node)
		{
			return node;
		}

		/// <exception cref="Lucene.Net.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		protected internal override IList<QueryNode> SetChildrenOrder(IList<QueryNode> children
			)
		{
			try
			{
				foreach (QueryNode child in children)
				{
					if (!child.IsLeaf())
					{
						IList<QueryNode> grandChildren = child.GetChildren();
						if (grandChildren != null && grandChildren.Count > 0)
						{
							this.childrenBuffer.AddItem(child);
						}
					}
					else
					{
						this.childrenBuffer.AddItem(child);
					}
				}
				children.Clear();
				Sharpen.Collections.AddAll(children, this.childrenBuffer);
			}
			finally
			{
				this.childrenBuffer.Clear();
			}
			return children;
		}
	}
}
