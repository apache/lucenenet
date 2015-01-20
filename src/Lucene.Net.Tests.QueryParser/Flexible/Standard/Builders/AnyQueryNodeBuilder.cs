/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Org.Apache.Lucene.Queryparser.Flexible.Core;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Builders;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Messages;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes;
using Org.Apache.Lucene.Queryparser.Flexible.Messages;
using Org.Apache.Lucene.Queryparser.Flexible.Standard.Builders;
using Org.Apache.Lucene.Search;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Flexible.Standard.Builders
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
		/// <exception cref="Org.Apache.Lucene.Queryparser.Flexible.Core.QueryNodeException">
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
