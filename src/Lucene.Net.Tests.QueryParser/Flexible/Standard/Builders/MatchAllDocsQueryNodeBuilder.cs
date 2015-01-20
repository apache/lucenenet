/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Queryparser.Flexible.Core;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Messages;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes;
using Org.Apache.Lucene.Queryparser.Flexible.Messages;
using Org.Apache.Lucene.Queryparser.Flexible.Standard.Builders;
using Org.Apache.Lucene.Queryparser.Flexible.Standard.Parser;
using Org.Apache.Lucene.Search;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Flexible.Standard.Builders
{
	/// <summary>
	/// Builds a
	/// <see cref="Org.Apache.Lucene.Search.MatchAllDocsQuery">Org.Apache.Lucene.Search.MatchAllDocsQuery
	/// 	</see>
	/// object from a
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.MatchAllDocsQueryNode
	/// 	">Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.MatchAllDocsQueryNode</see>
	/// object.
	/// </summary>
	public class MatchAllDocsQueryNodeBuilder : StandardQueryBuilder
	{
		public MatchAllDocsQueryNodeBuilder()
		{
		}

		// empty constructor
		/// <exception cref="Org.Apache.Lucene.Queryparser.Flexible.Core.QueryNodeException">
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
