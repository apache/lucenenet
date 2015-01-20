/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Lucene.Net.Index;
using Lucene.Net.Queryparser.Flexible.Core.Nodes;
using Lucene.Net.Queryparser.Flexible.Standard.Builders;
using Lucene.Net.Queryparser.Flexible.Standard.Nodes;
using Lucene.Net.Queryparser.Flexible.Standard.Processors;
using Lucene.Net.Search;
using Sharpen;

namespace Lucene.Net.Queryparser.Flexible.Standard.Builders
{
	/// <summary>
	/// Builds a
	/// <see cref="Lucene.Net.Search.WildcardQuery">Lucene.Net.Search.WildcardQuery
	/// 	</see>
	/// object from a
	/// <see cref="Lucene.Net.Queryparser.Flexible.Standard.Nodes.WildcardQueryNode
	/// 	">Lucene.Net.Queryparser.Flexible.Standard.Nodes.WildcardQueryNode</see>
	/// object.
	/// </summary>
	public class WildcardQueryNodeBuilder : StandardQueryBuilder
	{
		public WildcardQueryNodeBuilder()
		{
		}

		// empty constructor
		/// <exception cref="Lucene.Net.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		public virtual WildcardQuery Build(QueryNode queryNode)
		{
			WildcardQueryNode wildcardNode = (WildcardQueryNode)queryNode;
			WildcardQuery q = new WildcardQuery(new Term(wildcardNode.GetFieldAsString(), wildcardNode
				.GetTextAsString()));
			MultiTermQuery.RewriteMethod method = (MultiTermQuery.RewriteMethod)queryNode.GetTag
				(MultiTermRewriteMethodProcessor.TAG_ID);
			if (method != null)
			{
				q.SetRewriteMethod(method);
			}
			return q;
		}
	}
}
