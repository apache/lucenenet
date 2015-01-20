/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Index;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes;
using Org.Apache.Lucene.Queryparser.Flexible.Standard.Builders;
using Org.Apache.Lucene.Queryparser.Flexible.Standard.Nodes;
using Org.Apache.Lucene.Queryparser.Flexible.Standard.Processors;
using Org.Apache.Lucene.Search;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Flexible.Standard.Builders
{
	/// <summary>
	/// Builds a
	/// <see cref="Org.Apache.Lucene.Search.WildcardQuery">Org.Apache.Lucene.Search.WildcardQuery
	/// 	</see>
	/// object from a
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Standard.Nodes.WildcardQueryNode
	/// 	">Org.Apache.Lucene.Queryparser.Flexible.Standard.Nodes.WildcardQueryNode</see>
	/// object.
	/// </summary>
	public class WildcardQueryNodeBuilder : StandardQueryBuilder
	{
		public WildcardQueryNodeBuilder()
		{
		}

		// empty constructor
		/// <exception cref="Org.Apache.Lucene.Queryparser.Flexible.Core.QueryNodeException">
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
