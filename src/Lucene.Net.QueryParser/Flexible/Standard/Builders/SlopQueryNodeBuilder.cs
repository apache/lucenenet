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
	/// <see cref="Lucene.Net.Queryparser.Flexible.Core.Nodes.SlopQueryNode">Lucene.Net.Queryparser.Flexible.Core.Nodes.SlopQueryNode
	/// 	</see>
	/// child using
	/// <see cref="Lucene.Net.Queryparser.Flexible.Core.Builders.QueryTreeBuilder.QUERY_TREE_BUILDER_TAGID
	/// 	">Lucene.Net.Queryparser.Flexible.Core.Builders.QueryTreeBuilder.QUERY_TREE_BUILDER_TAGID
	/// 	</see>
	/// and applies the slop value
	/// defined in the
	/// <see cref="Lucene.Net.Queryparser.Flexible.Core.Nodes.SlopQueryNode">Lucene.Net.Queryparser.Flexible.Core.Nodes.SlopQueryNode
	/// 	</see>
	/// .
	/// </summary>
	public class SlopQueryNodeBuilder : StandardQueryBuilder
	{
		public SlopQueryNodeBuilder()
		{
		}

		// empty constructor
		/// <exception cref="Lucene.Net.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		public virtual Query Build(QueryNode queryNode)
		{
			SlopQueryNode phraseSlopNode = (SlopQueryNode)queryNode;
			Query query = (Query)phraseSlopNode.GetChild().GetTag(QueryTreeBuilder.QUERY_TREE_BUILDER_TAGID
				);
			if (query is PhraseQuery)
			{
				((PhraseQuery)query).SetSlop(phraseSlopNode.GetValue());
			}
			else
			{
				((MultiPhraseQuery)query).SetSlop(phraseSlopNode.GetValue());
			}
			return query;
		}
	}
}
