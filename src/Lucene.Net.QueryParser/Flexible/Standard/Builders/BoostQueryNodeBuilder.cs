/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Queryparser.Flexible.Core.Builders;
using Lucene.Net.Queryparser.Flexible.Core.Nodes;
using Lucene.Net.Queryparser.Flexible.Standard.Builders;
using Lucene.Net.Search;
using Sharpen;

namespace Lucene.Net.Queryparser.Flexible.Standard.Builders
{
	/// <summary>
	/// This builder basically reads the
	/// <see cref="Lucene.Net.Search.Query">Lucene.Net.Search.Query</see>
	/// object set on the
	/// <see cref="Lucene.Net.Queryparser.Flexible.Core.Nodes.BoostQueryNode">Lucene.Net.Queryparser.Flexible.Core.Nodes.BoostQueryNode
	/// 	</see>
	/// child using
	/// <see cref="Lucene.Net.Queryparser.Flexible.Core.Builders.QueryTreeBuilder.QUERY_TREE_BUILDER_TAGID
	/// 	">Lucene.Net.Queryparser.Flexible.Core.Builders.QueryTreeBuilder.QUERY_TREE_BUILDER_TAGID
	/// 	</see>
	/// and applies the boost value
	/// defined in the
	/// <see cref="Lucene.Net.Queryparser.Flexible.Core.Nodes.BoostQueryNode">Lucene.Net.Queryparser.Flexible.Core.Nodes.BoostQueryNode
	/// 	</see>
	/// .
	/// </summary>
	public class BoostQueryNodeBuilder : StandardQueryBuilder
	{
		public BoostQueryNodeBuilder()
		{
		}

		// empty constructor
		/// <exception cref="Lucene.Net.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		public virtual Query Build(QueryNode queryNode)
		{
			BoostQueryNode boostNode = (BoostQueryNode)queryNode;
			QueryNode child = boostNode.GetChild();
			if (child == null)
			{
				return null;
			}
			Query query = (Query)child.GetTag(QueryTreeBuilder.QUERY_TREE_BUILDER_TAGID);
			query.SetBoost(boostNode.GetValue());
			return query;
		}
	}
}
