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
	/// Builds no object, it only returns the
	/// <see cref="Org.Apache.Lucene.Search.Query">Org.Apache.Lucene.Search.Query</see>
	/// object set on the
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.GroupQueryNode">Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.GroupQueryNode
	/// 	</see>
	/// object using a
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Builders.QueryTreeBuilder.QUERY_TREE_BUILDER_TAGID
	/// 	">Org.Apache.Lucene.Queryparser.Flexible.Core.Builders.QueryTreeBuilder.QUERY_TREE_BUILDER_TAGID
	/// 	</see>
	/// tag.
	/// </summary>
	public class GroupQueryNodeBuilder : StandardQueryBuilder
	{
		public GroupQueryNodeBuilder()
		{
		}

		// empty constructor
		/// <exception cref="Org.Apache.Lucene.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		public virtual Query Build(QueryNode queryNode)
		{
			GroupQueryNode groupNode = (GroupQueryNode)queryNode;
			return (Query)(groupNode).GetChild().GetTag(QueryTreeBuilder.QUERY_TREE_BUILDER_TAGID
				);
		}
	}
}
