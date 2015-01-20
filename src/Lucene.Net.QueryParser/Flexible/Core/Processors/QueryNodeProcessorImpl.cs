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
using Sharpen;

namespace Lucene.Net.Queryparser.Flexible.Core.Processors
{
	/// <summary>
	/// <p>
	/// This is a default implementation for the
	/// <see cref="QueryNodeProcessor">QueryNodeProcessor</see>
	/// interface, it's an abstract class, so it should be extended by classes that
	/// want to process a
	/// <see cref="Lucene.Net.Queryparser.Flexible.Core.Nodes.QueryNode">Lucene.Net.Queryparser.Flexible.Core.Nodes.QueryNode
	/// 	</see>
	/// tree.
	/// </p>
	/// <p>
	/// This class process
	/// <see cref="Lucene.Net.Queryparser.Flexible.Core.Nodes.QueryNode">Lucene.Net.Queryparser.Flexible.Core.Nodes.QueryNode
	/// 	</see>
	/// s from left to right in the tree. While
	/// it's walking down the tree, for every node,
	/// <see cref="PreProcessNode(Lucene.Net.Queryparser.Flexible.Core.Nodes.QueryNode)
	/// 	">PreProcessNode(Lucene.Net.Queryparser.Flexible.Core.Nodes.QueryNode)</see>
	/// is invoked. After a node's children are
	/// processed,
	/// <see cref="PostProcessNode(Lucene.Net.Queryparser.Flexible.Core.Nodes.QueryNode)
	/// 	">PostProcessNode(Lucene.Net.Queryparser.Flexible.Core.Nodes.QueryNode)</see>
	/// is invoked for that node.
	/// <see cref="SetChildrenOrder(System.Collections.Generic.IList{E})">SetChildrenOrder(System.Collections.Generic.IList&lt;E&gt;)
	/// 	</see>
	/// is invoked before
	/// <see cref="PostProcessNode(Lucene.Net.Queryparser.Flexible.Core.Nodes.QueryNode)
	/// 	">PostProcessNode(Lucene.Net.Queryparser.Flexible.Core.Nodes.QueryNode)</see>
	/// only if the node has at least one child,
	/// in
	/// <see cref="SetChildrenOrder(System.Collections.Generic.IList{E})">SetChildrenOrder(System.Collections.Generic.IList&lt;E&gt;)
	/// 	</see>
	/// the implementor might redefine the
	/// children order or remove any children from the children list.
	/// </p>
	/// <p>
	/// Here is an example about how it process the nodes:
	/// </p>
	/// <pre>
	/// a
	/// / \
	/// b   e
	/// / \
	/// c   d
	/// </pre>
	/// Here is the order the methods would be invoked for the tree described above:
	/// <pre>
	/// preProcessNode( a );
	/// preProcessNode( b );
	/// preProcessNode( c );
	/// postProcessNode( c );
	/// preProcessNode( d );
	/// postProcessNode( d );
	/// setChildrenOrder( bChildrenList );
	/// postProcessNode( b );
	/// preProcessNode( e );
	/// postProcessNode( e );
	/// setChildrenOrder( aChildrenList );
	/// postProcessNode( a )
	/// </pre>
	/// </summary>
	/// <seealso cref="QueryNodeProcessor">QueryNodeProcessor</seealso>
	public abstract class QueryNodeProcessorImpl : QueryNodeProcessor
	{
		private AList<QueryNodeProcessorImpl.ChildrenList> childrenListPool = new AList<QueryNodeProcessorImpl.ChildrenList
			>();

		private QueryConfigHandler queryConfig;

		public QueryNodeProcessorImpl()
		{
		}

		public QueryNodeProcessorImpl(QueryConfigHandler queryConfigHandler)
		{
			// empty constructor
			this.queryConfig = queryConfigHandler;
		}

		/// <exception cref="Lucene.Net.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		public virtual QueryNode Process(QueryNode queryTree)
		{
			return ProcessIteration(queryTree);
		}

		/// <exception cref="Lucene.Net.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		private QueryNode ProcessIteration(QueryNode queryTree)
		{
			queryTree = PreProcessNode(queryTree);
			ProcessChildren(queryTree);
			queryTree = PostProcessNode(queryTree);
			return queryTree;
		}

		/// <summary>This method is called every time a child is processed.</summary>
		/// <remarks>This method is called every time a child is processed.</remarks>
		/// <param name="queryTree">the query node child to be processed</param>
		/// <exception cref="Lucene.Net.Queryparser.Flexible.Core.QueryNodeException">if something goes wrong during the query node processing
		/// 	</exception>
		protected internal virtual void ProcessChildren(QueryNode queryTree)
		{
			IList<QueryNode> children = queryTree.GetChildren();
			QueryNodeProcessorImpl.ChildrenList newChildren;
			if (children != null && children.Count > 0)
			{
				newChildren = AllocateChildrenList();
				try
				{
					foreach (QueryNode child in children)
					{
						child = ProcessIteration(child);
						if (child == null)
						{
							throw new ArgumentNullException();
						}
						newChildren.AddItem(child);
					}
					IList<QueryNode> orderedChildrenList = SetChildrenOrder(newChildren);
					queryTree.Set(orderedChildrenList);
				}
				finally
				{
					newChildren.beingUsed = false;
				}
			}
		}

		private QueryNodeProcessorImpl.ChildrenList AllocateChildrenList()
		{
			QueryNodeProcessorImpl.ChildrenList list = null;
			foreach (QueryNodeProcessorImpl.ChildrenList auxList in this.childrenListPool)
			{
				if (!auxList.beingUsed)
				{
					list = auxList;
					list.Clear();
					break;
				}
			}
			if (list == null)
			{
				list = new QueryNodeProcessorImpl.ChildrenList();
				this.childrenListPool.AddItem(list);
			}
			list.beingUsed = true;
			return list;
		}

		/// <summary>
		/// For reference about this method check:
		/// <see cref="QueryNodeProcessor.SetQueryConfigHandler(Lucene.Net.Queryparser.Flexible.Core.Config.QueryConfigHandler)
		/// 	">QueryNodeProcessor.SetQueryConfigHandler(Lucene.Net.Queryparser.Flexible.Core.Config.QueryConfigHandler)
		/// 	</see>
		/// .
		/// </summary>
		/// <param name="queryConfigHandler">the query configuration handler to be set.</param>
		/// <seealso cref="QueryNodeProcessor.GetQueryConfigHandler()">QueryNodeProcessor.GetQueryConfigHandler()
		/// 	</seealso>
		/// <seealso cref="Lucene.Net.Queryparser.Flexible.Core.Config.QueryConfigHandler
		/// 	">Lucene.Net.Queryparser.Flexible.Core.Config.QueryConfigHandler</seealso>
		public virtual void SetQueryConfigHandler(QueryConfigHandler queryConfigHandler)
		{
			this.queryConfig = queryConfigHandler;
		}

		/// <summary>
		/// For reference about this method check:
		/// <see cref="QueryNodeProcessor.GetQueryConfigHandler()">QueryNodeProcessor.GetQueryConfigHandler()
		/// 	</see>
		/// .
		/// </summary>
		/// <returns>QueryConfigHandler the query configuration handler to be set.</returns>
		/// <seealso cref="QueryNodeProcessor.SetQueryConfigHandler(Lucene.Net.Queryparser.Flexible.Core.Config.QueryConfigHandler)
		/// 	">QueryNodeProcessor.SetQueryConfigHandler(Lucene.Net.Queryparser.Flexible.Core.Config.QueryConfigHandler)
		/// 	</seealso>
		/// <seealso cref="Lucene.Net.Queryparser.Flexible.Core.Config.QueryConfigHandler
		/// 	">Lucene.Net.Queryparser.Flexible.Core.Config.QueryConfigHandler</seealso>
		public virtual QueryConfigHandler GetQueryConfigHandler()
		{
			return this.queryConfig;
		}

		/// <summary>This method is invoked for every node when walking down the tree.</summary>
		/// <remarks>This method is invoked for every node when walking down the tree.</remarks>
		/// <param name="node">the query node to be pre-processed</param>
		/// <returns>a query node</returns>
		/// <exception cref="Lucene.Net.Queryparser.Flexible.Core.QueryNodeException">if something goes wrong during the query node processing
		/// 	</exception>
		protected internal abstract QueryNode PreProcessNode(QueryNode node);

		/// <summary>This method is invoked for every node when walking up the tree.</summary>
		/// <remarks>This method is invoked for every node when walking up the tree.</remarks>
		/// <param name="node">node the query node to be post-processed</param>
		/// <returns>a query node</returns>
		/// <exception cref="Lucene.Net.Queryparser.Flexible.Core.QueryNodeException">if something goes wrong during the query node processing
		/// 	</exception>
		protected internal abstract QueryNode PostProcessNode(QueryNode node);

		/// <summary>This method is invoked for every node that has at least on child.</summary>
		/// <remarks>
		/// This method is invoked for every node that has at least on child. It's
		/// invoked right before
		/// <see cref="PostProcessNode(Lucene.Net.Queryparser.Flexible.Core.Nodes.QueryNode)
		/// 	">PostProcessNode(Lucene.Net.Queryparser.Flexible.Core.Nodes.QueryNode)</see>
		/// is invoked.
		/// </remarks>
		/// <param name="children">the list containing all current node's children</param>
		/// <returns>
		/// a new list containing all children that should be set to the
		/// current node
		/// </returns>
		/// <exception cref="Lucene.Net.Queryparser.Flexible.Core.QueryNodeException">if something goes wrong during the query node processing
		/// 	</exception>
		protected internal abstract IList<QueryNode> SetChildrenOrder(IList<QueryNode> children
			);

		[System.Serializable]
		private class ChildrenList : AList<QueryNode>
		{
			internal bool beingUsed;
		}
	}
}
