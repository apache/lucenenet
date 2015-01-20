/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Processors;
using Org.Apache.Lucene.Queryparser.Flexible.Standard.Nodes;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Flexible.Standard.Processors
{
	/// <summary>
	/// This processor removes invalid
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.SlopQueryNode">Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.SlopQueryNode
	/// 	</see>
	/// objects in the query
	/// node tree. A
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.SlopQueryNode">Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.SlopQueryNode
	/// 	</see>
	/// is invalid if its child is neither a
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.TokenizedPhraseQueryNode
	/// 	">Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.TokenizedPhraseQueryNode</see>
	/// nor a
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Standard.Nodes.MultiPhraseQueryNode
	/// 	">Org.Apache.Lucene.Queryparser.Flexible.Standard.Nodes.MultiPhraseQueryNode</see>
	/// . <br/>
	/// </summary>
	/// <seealso cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.SlopQueryNode">Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.SlopQueryNode
	/// 	</seealso>
	public class PhraseSlopQueryNodeProcessor : QueryNodeProcessorImpl
	{
		public PhraseSlopQueryNodeProcessor()
		{
		}

		// empty constructor
		/// <exception cref="Org.Apache.Lucene.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		protected internal override QueryNode PostProcessNode(QueryNode node)
		{
			if (node is SlopQueryNode)
			{
				SlopQueryNode phraseSlopNode = (SlopQueryNode)node;
				if (!(phraseSlopNode.GetChild() is TokenizedPhraseQueryNode) && !(phraseSlopNode.
					GetChild() is MultiPhraseQueryNode))
				{
					return phraseSlopNode.GetChild();
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
