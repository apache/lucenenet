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
using Lucene.Net.Queryparser.Flexible.Standard.Parser;
using Lucene.Net.Search;
using Sharpen;

namespace Lucene.Net.Queryparser.Flexible.Standard.Builders
{
	/// <summary>
	/// Builds a
	/// <see cref="Lucene.Net.Search.BooleanQuery">Lucene.Net.Search.BooleanQuery
	/// 	</see>
	/// object from a
	/// <see cref="Lucene.Net.Queryparser.Flexible.Core.Nodes.BooleanQueryNode">Lucene.Net.Queryparser.Flexible.Core.Nodes.BooleanQueryNode
	/// 	</see>
	/// object.
	/// Every children in the
	/// <see cref="Lucene.Net.Queryparser.Flexible.Core.Nodes.BooleanQueryNode">Lucene.Net.Queryparser.Flexible.Core.Nodes.BooleanQueryNode
	/// 	</see>
	/// object must be already tagged
	/// using
	/// <see cref="Lucene.Net.Queryparser.Flexible.Core.Builders.QueryTreeBuilder.QUERY_TREE_BUILDER_TAGID
	/// 	">Lucene.Net.Queryparser.Flexible.Core.Builders.QueryTreeBuilder.QUERY_TREE_BUILDER_TAGID
	/// 	</see>
	/// with a
	/// <see cref="Lucene.Net.Search.Query">Lucene.Net.Search.Query</see>
	/// object. <br/>
	/// <br/>
	/// It takes in consideration if the children is a
	/// <see cref="Lucene.Net.Queryparser.Flexible.Core.Nodes.ModifierQueryNode">Lucene.Net.Queryparser.Flexible.Core.Nodes.ModifierQueryNode
	/// 	</see>
	/// to
	/// define the
	/// <see cref="Lucene.Net.Search.BooleanClause">Lucene.Net.Search.BooleanClause
	/// 	</see>
	/// .
	/// </summary>
	public class BooleanQueryNodeBuilder : StandardQueryBuilder
	{
		public BooleanQueryNodeBuilder()
		{
		}

		// empty constructor
		/// <exception cref="Lucene.Net.Queryparser.Flexible.Core.QueryNodeException">
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
