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
	/// <see cref="Lucene.Net.Search.PrefixQuery">Lucene.Net.Search.PrefixQuery
	/// 	</see>
	/// object from a
	/// <see cref="Lucene.Net.Queryparser.Flexible.Standard.Nodes.PrefixWildcardQueryNode
	/// 	">Lucene.Net.Queryparser.Flexible.Standard.Nodes.PrefixWildcardQueryNode</see>
	/// object.
	/// </summary>
	public class PrefixWildcardQueryNodeBuilder : StandardQueryBuilder
	{
		public PrefixWildcardQueryNodeBuilder()
		{
		}

		// empty constructor
		/// <exception cref="Lucene.Net.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		public virtual PrefixQuery Build(QueryNode queryNode)
		{
			PrefixWildcardQueryNode wildcardNode = (PrefixWildcardQueryNode)queryNode;
			string text = wildcardNode.GetText().SubSequence(0, wildcardNode.GetText().Length
				 - 1).ToString();
			PrefixQuery q = new PrefixQuery(new Term(wildcardNode.GetFieldAsString(), text));
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
