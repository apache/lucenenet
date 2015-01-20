/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Builders;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes;
using Org.Apache.Lucene.Queryparser.Flexible.Standard.Builders;
using Org.Apache.Lucene.Queryparser.Flexible.Standard.Nodes;
using Org.Apache.Lucene.Search;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Flexible.Standard.Builders
{
	/// <summary>
	/// Builds a
	/// <see cref="Org.Apache.Lucene.Search.MultiPhraseQuery">Org.Apache.Lucene.Search.MultiPhraseQuery
	/// 	</see>
	/// object from a
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Standard.Nodes.MultiPhraseQueryNode
	/// 	">Org.Apache.Lucene.Queryparser.Flexible.Standard.Nodes.MultiPhraseQueryNode</see>
	/// object.
	/// </summary>
	public class MultiPhraseQueryNodeBuilder : StandardQueryBuilder
	{
		public MultiPhraseQueryNodeBuilder()
		{
		}

		// empty constructor
		/// <exception cref="Org.Apache.Lucene.Queryparser.Flexible.Core.QueryNodeException">
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
