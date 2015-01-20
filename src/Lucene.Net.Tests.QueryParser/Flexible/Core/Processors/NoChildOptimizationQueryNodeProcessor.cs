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
	/// <p>
	/// A
	/// <see cref="NoChildOptimizationQueryNodeProcessor">NoChildOptimizationQueryNodeProcessor
	/// 	</see>
	/// removes every
	/// BooleanQueryNode, BoostQueryNode, TokenizedPhraseQueryNode or
	/// ModifierQueryNode that do not have a valid children.
	/// </p>
	/// <p>
	/// Example: When the children of these nodes are removed for any reason then the
	/// nodes may become invalid.
	/// </p>
	/// </summary>
	public class NoChildOptimizationQueryNodeProcessor : QueryNodeProcessorImpl
	{
		public NoChildOptimizationQueryNodeProcessor()
		{
		}

		// empty constructor
		/// <exception cref="Org.Apache.Lucene.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		protected internal override QueryNode PostProcessNode(QueryNode node)
		{
			if (node is BooleanQueryNode || node is BoostQueryNode || node is TokenizedPhraseQueryNode
				 || node is ModifierQueryNode)
			{
				IList<QueryNode> children = node.GetChildren();
				if (children != null && children.Count > 0)
				{
					foreach (QueryNode child in children)
					{
						if (!(child is DeletedQueryNode))
						{
							return node;
						}
					}
				}
				return new MatchNoDocsQueryNode();
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
