/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Builders;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes;
using Org.Apache.Lucene.Queryparser.Flexible.Standard.Builders;
using Org.Apache.Lucene.Search;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Flexible.Standard.Builders
{
	/// <summary>
	/// Builds a
	/// <see cref="Org.Apache.Lucene.Search.PhraseQuery">Org.Apache.Lucene.Search.PhraseQuery
	/// 	</see>
	/// object from a
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.TokenizedPhraseQueryNode
	/// 	">Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.TokenizedPhraseQueryNode</see>
	/// object.
	/// </summary>
	public class PhraseQueryNodeBuilder : StandardQueryBuilder
	{
		public PhraseQueryNodeBuilder()
		{
		}

		// empty constructor
		/// <exception cref="Org.Apache.Lucene.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		public virtual PhraseQuery Build(QueryNode queryNode)
		{
			TokenizedPhraseQueryNode phraseNode = (TokenizedPhraseQueryNode)queryNode;
			PhraseQuery phraseQuery = new PhraseQuery();
			IList<QueryNode> children = phraseNode.GetChildren();
			if (children != null)
			{
				foreach (QueryNode child in children)
				{
					TermQuery termQuery = (TermQuery)child.GetTag(QueryTreeBuilder.QUERY_TREE_BUILDER_TAGID
						);
					FieldQueryNode termNode = (FieldQueryNode)child;
					phraseQuery.Add(termQuery.GetTerm(), termNode.GetPositionIncrement());
				}
			}
			return phraseQuery;
		}
	}
}
