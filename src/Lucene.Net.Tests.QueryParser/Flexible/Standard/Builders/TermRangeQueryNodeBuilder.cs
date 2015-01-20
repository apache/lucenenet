/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Util;
using Org.Apache.Lucene.Queryparser.Flexible.Standard.Builders;
using Org.Apache.Lucene.Queryparser.Flexible.Standard.Nodes;
using Org.Apache.Lucene.Queryparser.Flexible.Standard.Processors;
using Org.Apache.Lucene.Search;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Flexible.Standard.Builders
{
	/// <summary>
	/// Builds a
	/// <see cref="Org.Apache.Lucene.Search.TermRangeQuery">Org.Apache.Lucene.Search.TermRangeQuery
	/// 	</see>
	/// object from a
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Standard.Nodes.TermRangeQueryNode
	/// 	">Org.Apache.Lucene.Queryparser.Flexible.Standard.Nodes.TermRangeQueryNode</see>
	/// object.
	/// </summary>
	public class TermRangeQueryNodeBuilder : StandardQueryBuilder
	{
		public TermRangeQueryNodeBuilder()
		{
		}

		// empty constructor
		/// <exception cref="Org.Apache.Lucene.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		public virtual TermRangeQuery Build(QueryNode queryNode)
		{
			TermRangeQueryNode rangeNode = (TermRangeQueryNode)queryNode;
			FieldQueryNode upper = rangeNode.GetUpperBound();
			FieldQueryNode lower = rangeNode.GetLowerBound();
			string field = StringUtils.ToString(rangeNode.GetField());
			string lowerText = lower.GetTextAsString();
			string upperText = upper.GetTextAsString();
			if (lowerText.Length == 0)
			{
				lowerText = null;
			}
			if (upperText.Length == 0)
			{
				upperText = null;
			}
			TermRangeQuery rangeQuery = TermRangeQuery.NewStringRange(field, lowerText, upperText
				, rangeNode.IsLowerInclusive(), rangeNode.IsUpperInclusive());
			MultiTermQuery.RewriteMethod method = (MultiTermQuery.RewriteMethod)queryNode.GetTag
				(MultiTermRewriteMethodProcessor.TAG_ID);
			if (method != null)
			{
				rangeQuery.SetRewriteMethod(method);
			}
			return rangeQuery;
		}
	}
}
