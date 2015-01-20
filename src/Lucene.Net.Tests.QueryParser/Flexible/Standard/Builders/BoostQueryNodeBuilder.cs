/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Queryparser.Flexible.Core.Builders;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes;
using Org.Apache.Lucene.Queryparser.Flexible.Standard.Builders;
using Org.Apache.Lucene.Search;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Flexible.Standard.Builders
{
	/// <summary>
	/// This builder basically reads the
	/// <see cref="Org.Apache.Lucene.Search.Query">Org.Apache.Lucene.Search.Query</see>
	/// object set on the
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.BoostQueryNode">Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.BoostQueryNode
	/// 	</see>
	/// child using
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Builders.QueryTreeBuilder.QUERY_TREE_BUILDER_TAGID
	/// 	">Org.Apache.Lucene.Queryparser.Flexible.Core.Builders.QueryTreeBuilder.QUERY_TREE_BUILDER_TAGID
	/// 	</see>
	/// and applies the boost value
	/// defined in the
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.BoostQueryNode">Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.BoostQueryNode
	/// 	</see>
	/// .
	/// </summary>
	public class BoostQueryNodeBuilder : StandardQueryBuilder
	{
		public BoostQueryNodeBuilder()
		{
		}

		// empty constructor
		/// <exception cref="Org.Apache.Lucene.Queryparser.Flexible.Core.QueryNodeException">
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
