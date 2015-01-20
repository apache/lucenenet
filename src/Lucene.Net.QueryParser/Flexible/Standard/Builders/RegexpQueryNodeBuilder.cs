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
	/// <see cref="Lucene.Net.Search.RegexpQuery">Lucene.Net.Search.RegexpQuery
	/// 	</see>
	/// object from a
	/// <see cref="Lucene.Net.Queryparser.Flexible.Standard.Nodes.RegexpQueryNode"
	/// 	>Lucene.Net.Queryparser.Flexible.Standard.Nodes.RegexpQueryNode</see>
	/// object.
	/// </summary>
	public class RegexpQueryNodeBuilder : StandardQueryBuilder
	{
		public RegexpQueryNodeBuilder()
		{
		}

		// empty constructor
		/// <exception cref="Lucene.Net.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		public virtual RegexpQuery Build(QueryNode queryNode)
		{
			RegexpQueryNode regexpNode = (RegexpQueryNode)queryNode;
			RegexpQuery q = new RegexpQuery(new Term(regexpNode.GetFieldAsString(), regexpNode
				.TextToBytesRef()));
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
