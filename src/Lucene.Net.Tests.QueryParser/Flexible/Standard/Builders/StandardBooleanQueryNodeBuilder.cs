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
using Org.Apache.Lucene.Queryparser.Flexible.Standard.Nodes;
using Org.Apache.Lucene.Queryparser.Flexible.Standard.Parser;
using Org.Apache.Lucene.Search;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Flexible.Standard.Builders
{
	/// <summary>
	/// This builder does the same as the
	/// <see cref="BooleanQueryNodeBuilder">BooleanQueryNodeBuilder</see>
	/// , but this
	/// considers if the built
	/// <see cref="Org.Apache.Lucene.Search.BooleanQuery">Org.Apache.Lucene.Search.BooleanQuery
	/// 	</see>
	/// should have its coord disabled or
	/// not. <br/>
	/// </summary>
	/// <seealso cref="BooleanQueryNodeBuilder">BooleanQueryNodeBuilder</seealso>
	/// <seealso cref="Org.Apache.Lucene.Search.BooleanQuery">Org.Apache.Lucene.Search.BooleanQuery
	/// 	</seealso>
	/// <seealso cref="Org.Apache.Lucene.Search.Similarities.Similarity.Coord(int, int)">Org.Apache.Lucene.Search.Similarities.Similarity.Coord(int, int)
	/// 	</seealso>
	public class StandardBooleanQueryNodeBuilder : StandardQueryBuilder
	{
		public StandardBooleanQueryNodeBuilder()
		{
		}

		// empty constructor
		/// <exception cref="Org.Apache.Lucene.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		public virtual BooleanQuery Build(QueryNode queryNode)
		{
			StandardBooleanQueryNode booleanNode = (StandardBooleanQueryNode)queryNode;
			BooleanQuery bQuery = new BooleanQuery(booleanNode.IsDisableCoord());
			IList<QueryNode> children = booleanNode.GetChildren();
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
							bQuery.Add(query, GetModifierValue(child));
						}
						catch (BooleanQuery.TooManyClauses ex)
						{
							throw new QueryNodeException(new MessageImpl(QueryParserMessages.TOO_MANY_BOOLEAN_CLAUSES
								, BooleanQuery.GetMaxClauseCount(), queryNode.ToQueryString(new EscapeQuerySyntaxImpl
								())), ex);
						}
					}
				}
			}
			return bQuery;
		}

		private static BooleanClause.Occur GetModifierValue(QueryNode node)
		{
			if (node is ModifierQueryNode)
			{
				ModifierQueryNode mNode = ((ModifierQueryNode)node);
				ModifierQueryNode.Modifier modifier = mNode.GetModifier();
				if (ModifierQueryNode.Modifier.MOD_NONE.Equals(modifier))
				{
					return BooleanClause.Occur.SHOULD;
				}
				else
				{
					if (ModifierQueryNode.Modifier.MOD_NOT.Equals(modifier))
					{
						return BooleanClause.Occur.MUST_NOT;
					}
					else
					{
						return BooleanClause.Occur.MUST;
					}
				}
			}
			return BooleanClause.Occur.SHOULD;
		}
	}
}
