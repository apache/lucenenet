/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Lucene.Net.Queryparser.Flexible.Core;
using Lucene.Net.Queryparser.Flexible.Core.Builders;
using Lucene.Net.Queryparser.Flexible.Core.Messages;
using Lucene.Net.Queryparser.Flexible.Core.Nodes;
using Lucene.Net.Queryparser.Flexible.Messages;
using Lucene.Net.Queryparser.Flexible.Standard.Builders;
using Lucene.Net.Search;
using Sharpen;

namespace Lucene.Net.Queryparser.Flexible.Standard.Builders
{
	/// <summary>
	/// Builds a BooleanQuery of SHOULD clauses, possibly with
	/// some minimum number to match.
	/// </summary>
	/// <remarks>
	/// Builds a BooleanQuery of SHOULD clauses, possibly with
	/// some minimum number to match.
	/// </remarks>
	public class AnyQueryNodeBuilder : StandardQueryBuilder
	{
		public AnyQueryNodeBuilder()
		{
		}

		// empty constructor
		/// <exception cref="Lucene.Net.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		public virtual BooleanQuery Build(QueryNode queryNode)
		{
			AnyQueryNode andNode = (AnyQueryNode)queryNode;
			BooleanQuery bQuery = new BooleanQuery();
			IList<QueryNode> children = andNode.GetChildren();
			if (children != null)
			{
				foreach (QueryNode child in children)
				{
					object obj = child.GetTag(QueryTreeBuilder.QUERY_TREE_BUILDER_TAGID);
					if (obj != null)
					{
						Query query = (Query)obj;
						try
						{
							bQuery.Add(query, BooleanClause.Occur.SHOULD);
						}
						catch (BooleanQuery.TooManyClauses ex)
						{
							throw new QueryNodeException(new MessageImpl(QueryParserMessages.EMPTY_MESSAGE), 
								ex);
						}
					}
				}
			}
			bQuery.SetMinimumNumberShouldMatch(andNode.GetMinimumMatchingElements());
			return bQuery;
		}
	}
}
