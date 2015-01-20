/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Queryparser.Flexible.Core.Builders;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Flexible.Core.Builders
{
	/// <summary>
	/// This interface is used by implementors classes that builds some kind of
	/// object from a query tree.
	/// </summary>
	/// <remarks>
	/// This interface is used by implementors classes that builds some kind of
	/// object from a query tree.
	/// </remarks>
	/// <seealso cref="QueryTreeBuilder">QueryTreeBuilder</seealso>
	public interface QueryBuilder
	{
		/// <summary>Builds some kind of object from a query tree.</summary>
		/// <remarks>Builds some kind of object from a query tree.</remarks>
		/// <param name="queryNode">the query tree root node</param>
		/// <returns>some object generated from the query tree</returns>
		/// <exception cref="Org.Apache.Lucene.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		object Build(QueryNode queryNode);
	}
}
