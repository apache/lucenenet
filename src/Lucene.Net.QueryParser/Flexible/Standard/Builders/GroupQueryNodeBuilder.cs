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
	/// Builds no object, it only returns the
	/// <see cref="Lucene.Net.Search.Query">Lucene.Net.Search.Query</see>
	/// object set on the
	/// <see cref="Lucene.Net.Queryparser.Flexible.Core.Nodes.GroupQueryNode">Lucene.Net.Queryparser.Flexible.Core.Nodes.GroupQueryNode
	/// 	</see>
	/// object using a
	/// <see cref="Lucene.Net.Queryparser.Flexible.Core.Builders.QueryTreeBuilder.QUERY_TREE_BUILDER_TAGID
	/// 	">Lucene.Net.Queryparser.Flexible.Core.Builders.QueryTreeBuilder.QUERY_TREE_BUILDER_TAGID
	/// 	</see>
	/// tag.
	/// </summary>
	public class GroupQueryNodeBuilder : StandardQueryBuilder
	{
		public GroupQueryNodeBuilder()
		{
		}

		// empty constructor
		/// <exception cref="Lucene.Net.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		public virtual Query Build(QueryNode queryNode)
		{
			GroupQueryNode groupNode = (GroupQueryNode)queryNode;
			return (Query)(groupNode).GetChild().GetTag(QueryTreeBuilder.QUERY_TREE_BUILDER_TAGID
				);
		}
	}
}
