/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Lucene.Net.Queryparser.Flexible.Core.Builders;
using Lucene.Net.Queryparser.Flexible.Core.Nodes;
using Lucene.Net.Queryparser.Flexible.Standard.Builders;
using Lucene.Net.Search;
using Sharpen;

namespace Lucene.Net.Queryparser.Flexible.Standard.Builders
{
	/// <summary>
	/// Builds a
	/// <see cref="Lucene.Net.Search.PhraseQuery">Lucene.Net.Search.PhraseQuery
	/// 	</see>
	/// object from a
	/// <see cref="Lucene.Net.Queryparser.Flexible.Core.Nodes.TokenizedPhraseQueryNode
	/// 	">Lucene.Net.Queryparser.Flexible.Core.Nodes.TokenizedPhraseQueryNode</see>
	/// object.
	/// </summary>
	public class PhraseQueryNodeBuilder : StandardQueryBuilder
	{
		public PhraseQueryNodeBuilder()
		{
		}

		// empty constructor
		/// <exception cref="Lucene.Net.Queryparser.Flexible.Core.QueryNodeException">
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
