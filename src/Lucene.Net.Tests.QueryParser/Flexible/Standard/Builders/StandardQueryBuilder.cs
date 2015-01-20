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
	/// This interface should be implemented by every class that wants to build
	/// <see cref="Org.Apache.Lucene.Search.Query">Org.Apache.Lucene.Search.Query</see>
	/// objects from
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.QueryNode">Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.QueryNode
	/// 	</see>
	/// objects. <br/>
	/// </summary>
	/// <seealso cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Builders.QueryBuilder"
	/// 	>Org.Apache.Lucene.Queryparser.Flexible.Core.Builders.QueryBuilder</seealso>
	/// <seealso cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Builders.QueryTreeBuilder
	/// 	">Org.Apache.Lucene.Queryparser.Flexible.Core.Builders.QueryTreeBuilder</seealso>
	public interface StandardQueryBuilder : QueryBuilder
	{
		/// <exception cref="Org.Apache.Lucene.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		Query Build(QueryNode queryNode);
	}
}
