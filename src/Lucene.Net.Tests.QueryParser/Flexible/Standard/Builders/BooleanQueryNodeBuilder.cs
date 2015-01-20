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
using Org.Apache.Lucene.Queryparser.Flexible.Standard.Parser;
using Org.Apache.Lucene.Search;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Flexible.Standard.Builders
{
	/// <summary>
	/// Builds a
	/// <see cref="Org.Apache.Lucene.Search.BooleanQuery">Org.Apache.Lucene.Search.BooleanQuery
	/// 	</see>
	/// object from a
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.BooleanQueryNode">Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.BooleanQueryNode
	/// 	</see>
	/// object.
	/// Every children in the
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.BooleanQueryNode">Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.BooleanQueryNode
	/// 	</see>
	/// object must be already tagged
	/// using
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Builders.QueryTreeBuilder.QUERY_TREE_BUILDER_TAGID
	/// 	">Org.Apache.Lucene.Queryparser.Flexible.Core.Builders.QueryTreeBuilder.QUERY_TREE_BUILDER_TAGID
	/// 	</see>
	/// with a
	/// <see cref="Org.Apache.Lucene.Search.Query">Org.Apache.Lucene.Search.Query</see>
	/// object. <br/>
	/// <br/>
	/// It takes in consideration if the children is a
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.ModifierQueryNode">Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.ModifierQueryNode
	/// 	</see>
	/// to
	/// define the
	/// <see cref="Org.Apache.Lucene.Search.BooleanClause">Org.Apache.Lucene.Search.BooleanClause
	/// 	</see>
	/// .
	/// </summary>
	public class BooleanQueryNodeBuilder : StandardQueryBuilder
	{
		public BooleanQueryNodeBuilder()
		{
		}

		// empty constructor
		/// <exception cref="Org.Apache.Lucene.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		public virtual BooleanQuery Build(QueryNode queryNode)
		{
			BooleanQueryNode booleanNode = (BooleanQueryNode)queryNode;
			BooleanQuery bQuery = new BooleanQuery();
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
				switch (mNode.GetModifier())
				{
					case ModifierQueryNode.Modifier.MOD_REQ:
					{
						return BooleanClause.Occur.MUST;
					}

					case ModifierQueryNode.Modifier.MOD_NOT:
					{
						return BooleanClause.Occur.MUST_NOT;
					}

					case ModifierQueryNode.Modifier.MOD_NONE:
					{
						return BooleanClause.Occur.SHOULD;
					}
				}
			}
			return BooleanClause.Occur.SHOULD;
		}
	}
}
