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
	/// This processor converts every
	/// <see cref="Lucene.Net.Queryparser.Flexible.Standard.Nodes.WildcardQueryNode
	/// 	">Lucene.Net.Queryparser.Flexible.Standard.Nodes.WildcardQueryNode</see>
	/// that is "*:*" to
	/// <see cref="Lucene.Net.Queryparser.Flexible.Core.Nodes.MatchAllDocsQueryNode
	/// 	">Lucene.Net.Queryparser.Flexible.Core.Nodes.MatchAllDocsQueryNode</see>
	/// .
	/// </summary>
	/// <seealso cref="Lucene.Net.Queryparser.Flexible.Core.Nodes.MatchAllDocsQueryNode
	/// 	">Lucene.Net.Queryparser.Flexible.Core.Nodes.MatchAllDocsQueryNode</seealso>
	/// <seealso cref="Lucene.Net.Search.MatchAllDocsQuery">Lucene.Net.Search.MatchAllDocsQuery
	/// 	</seealso>
	public class MatchAllDocsQueryNodeProcessor : QueryNodeProcessorImpl
	{
		public MatchAllDocsQueryNodeProcessor()
		{
		}

		// empty constructor
		/// <exception cref="Lucene.Net.Queryparser.Flexible.Core.QueryNodeException">
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
			return children;
		}
	}
}
