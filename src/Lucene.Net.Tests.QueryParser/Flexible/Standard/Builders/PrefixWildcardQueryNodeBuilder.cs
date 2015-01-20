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
	/// <see cref="Org.Apache.Lucene.Search.PrefixQuery">Org.Apache.Lucene.Search.PrefixQuery
	/// 	</see>
	/// object from a
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Standard.Nodes.PrefixWildcardQueryNode
	/// 	">Org.Apache.Lucene.Queryparser.Flexible.Standard.Nodes.PrefixWildcardQueryNode</see>
	/// object.
	/// </summary>
	public class PrefixWildcardQueryNodeBuilder : StandardQueryBuilder
	{
		public PrefixWildcardQueryNodeBuilder()
		{
		}

		// empty constructor
		/// <exception cref="Org.Apache.Lucene.Queryparser.Flexible.Core.QueryNodeException">
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
