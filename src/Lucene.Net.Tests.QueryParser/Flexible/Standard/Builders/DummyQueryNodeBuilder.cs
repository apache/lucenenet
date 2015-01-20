/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes;
using Org.Apache.Lucene.Queryparser.Flexible.Standard.Builders;
using Org.Apache.Lucene.Search;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Flexible.Standard.Builders
{
	/// <summary>This builder does nothing.</summary>
	/// <remarks>
	/// This builder does nothing. Commonly used for
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.QueryNode">Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.QueryNode
	/// 	</see>
	/// objects that
	/// are built by its parent's builder.
	/// </remarks>
	/// <seealso cref="StandardQueryBuilder">StandardQueryBuilder</seealso>
	/// <seealso cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Builders.QueryTreeBuilder
	/// 	">Org.Apache.Lucene.Queryparser.Flexible.Core.Builders.QueryTreeBuilder</seealso>
	public class DummyQueryNodeBuilder : StandardQueryBuilder
	{
		/// <summary>
		/// Constructs a
		/// <see cref="DummyQueryNodeBuilder">DummyQueryNodeBuilder</see>
		/// object.
		/// </summary>
		public DummyQueryNodeBuilder()
		{
		}

		// empty constructor
		/// <summary>Always return <code>null</code>.</summary>
		/// <remarks>
		/// Always return <code>null</code>.
		/// return <code>null</code>
		/// </remarks>
		/// <exception cref="Org.Apache.Lucene.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		public virtual TermQuery Build(QueryNode queryNode)
		{
			return null;
		}
	}
}
