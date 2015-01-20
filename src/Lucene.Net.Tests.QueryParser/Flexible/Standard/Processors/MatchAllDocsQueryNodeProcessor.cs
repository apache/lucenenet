/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Processors;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Flexible.Standard.Processors
{
	/// <summary>
	/// This processor converts every
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Standard.Nodes.WildcardQueryNode
	/// 	">Org.Apache.Lucene.Queryparser.Flexible.Standard.Nodes.WildcardQueryNode</see>
	/// that is "*:*" to
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.MatchAllDocsQueryNode
	/// 	">Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.MatchAllDocsQueryNode</see>
	/// .
	/// </summary>
	/// <seealso cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.MatchAllDocsQueryNode
	/// 	">Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.MatchAllDocsQueryNode</seealso>
	/// <seealso cref="Org.Apache.Lucene.Search.MatchAllDocsQuery">Org.Apache.Lucene.Search.MatchAllDocsQuery
	/// 	</seealso>
	public class MatchAllDocsQueryNodeProcessor : QueryNodeProcessorImpl
	{
		public MatchAllDocsQueryNodeProcessor()
		{
		}

		// empty constructor
		/// <exception cref="Org.Apache.Lucene.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		protected internal override QueryNode PostProcessNode(QueryNode node)
		{
			if (node is FieldQueryNode)
			{
				FieldQueryNode fqn = (FieldQueryNode)node;
				if (fqn.GetField().ToString().Equals("*") && fqn.GetText().ToString().Equals("*"))
				{
					return new MatchAllDocsQueryNode();
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
