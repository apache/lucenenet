/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Queryparser.Flexible.Core;
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
	/// <see cref="Lucene.Net.Search.MatchAllDocsQuery">Lucene.Net.Search.MatchAllDocsQuery
	/// 	</see>
	/// object from a
	/// <see cref="Lucene.Net.Queryparser.Flexible.Core.Nodes.MatchAllDocsQueryNode
	/// 	">Lucene.Net.Queryparser.Flexible.Core.Nodes.MatchAllDocsQueryNode</see>
	/// object.
	/// </summary>
	public class MatchAllDocsQueryNodeBuilder : StandardQueryBuilder
	{
		public MatchAllDocsQueryNodeBuilder()
		{
		}

		// empty constructor
		/// <exception cref="Lucene.Net.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		public virtual MatchAllDocsQuery Build(QueryNode queryNode)
		{
			// validates node
			if (!(queryNode is MatchAllDocsQueryNode))
			{
				throw new QueryNodeException(new MessageImpl(QueryParserMessages.LUCENE_QUERY_CONVERSION_ERROR
					, queryNode.ToQueryString(new EscapeQuerySyntaxImpl()), queryNode.GetType().FullName
					));
			}
			return new MatchAllDocsQuery();
		}
	}
}
