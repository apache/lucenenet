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
	/// This interface should be implemented by every class that wants to build
	/// <see cref="Lucene.Net.Search.Query">Lucene.Net.Search.Query</see>
	/// objects from
	/// <see cref="Lucene.Net.Queryparser.Flexible.Core.Nodes.QueryNode">Lucene.Net.Queryparser.Flexible.Core.Nodes.QueryNode
	/// 	</see>
	/// objects. <br/>
	/// </summary>
	/// <seealso cref="Lucene.Net.Queryparser.Flexible.Core.Builders.QueryBuilder"
	/// 	>Lucene.Net.Queryparser.Flexible.Core.Builders.QueryBuilder</seealso>
	/// <seealso cref="Lucene.Net.Queryparser.Flexible.Core.Builders.QueryTreeBuilder
	/// 	">Lucene.Net.Queryparser.Flexible.Core.Builders.QueryTreeBuilder</seealso>
	public interface StandardQueryBuilder : QueryBuilder
	{
		/// <exception cref="Lucene.Net.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		Query Build(QueryNode queryNode);
	}
}
