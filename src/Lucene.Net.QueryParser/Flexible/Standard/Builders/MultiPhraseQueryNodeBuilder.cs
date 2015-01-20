/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Lucene.Net.Index;
using Lucene.Net.Queryparser.Flexible.Core.Builders;
using Lucene.Net.Queryparser.Flexible.Core.Nodes;
using Lucene.Net.Queryparser.Flexible.Standard.Builders;
using Lucene.Net.Queryparser.Flexible.Standard.Nodes;
using Lucene.Net.Search;
using Sharpen;

namespace Lucene.Net.Queryparser.Flexible.Standard.Builders
{
	/// <summary>
	/// Builds a
	/// <see cref="Lucene.Net.Search.MultiPhraseQuery">Lucene.Net.Search.MultiPhraseQuery
	/// 	</see>
	/// object from a
	/// <see cref="Lucene.Net.Queryparser.Flexible.Standard.Nodes.MultiPhraseQueryNode
	/// 	">Lucene.Net.Queryparser.Flexible.Standard.Nodes.MultiPhraseQueryNode</see>
	/// object.
	/// </summary>
	public class MultiPhraseQueryNodeBuilder : StandardQueryBuilder
	{
		public MultiPhraseQueryNodeBuilder()
		{
		}

		// empty constructor
		/// <exception cref="Lucene.Net.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		public virtual MultiPhraseQuery Build(QueryNode queryNode)
		{
			MultiPhraseQueryNode phraseNode = (MultiPhraseQueryNode)queryNode;
			MultiPhraseQuery phraseQuery = new MultiPhraseQuery();
			IList<QueryNode> children = phraseNode.GetChildren();
			if (children != null)
			{
				SortedDictionary<int, IList<Term>> positionTermMap = new SortedDictionary<int, IList
					<Term>>();
				foreach (QueryNode child in children)
				{
					FieldQueryNode termNode = (FieldQueryNode)child;
					TermQuery termQuery = (TermQuery)termNode.GetTag(QueryTreeBuilder.QUERY_TREE_BUILDER_TAGID
						);
					IList<Term> termList = positionTermMap.Get(termNode.GetPositionIncrement());
					if (termList == null)
					{
						termList = new List<Term>();
						positionTermMap.Put(termNode.GetPositionIncrement(), termList);
					}
					termList.AddItem(termQuery.GetTerm());
				}
				foreach (int positionIncrement in positionTermMap.Keys)
				{
					IList<Term> termList = positionTermMap.Get(positionIncrement);
					phraseQuery.Add(Sharpen.Collections.ToArray(termList, new Term[termList.Count]), 
						positionIncrement);
				}
			}
			return phraseQuery;
		}
	}
}
