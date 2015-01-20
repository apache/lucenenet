/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System;
using System.Collections.Generic;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Processors;
using Org.Apache.Lucene.Queryparser.Flexible.Standard.Config;
using Org.Apache.Lucene.Queryparser.Flexible.Standard.Nodes;
using Org.Apache.Lucene.Queryparser.Flexible.Standard.Processors;
using Org.Apache.Lucene.Search;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Flexible.Standard.Processors
{
	/// <summary>
	/// This processor instates the default
	/// <see cref="Org.Apache.Lucene.Search.MultiTermQuery.RewriteMethod">Org.Apache.Lucene.Search.MultiTermQuery.RewriteMethod
	/// 	</see>
	/// ,
	/// <see cref="Org.Apache.Lucene.Search.MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT
	/// 	">Org.Apache.Lucene.Search.MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT</see>
	/// , for multi-term
	/// query nodes.
	/// </summary>
	public class MultiTermRewriteMethodProcessor : QueryNodeProcessorImpl
	{
		public static readonly string TAG_ID = "MultiTermRewriteMethodConfiguration";

		protected internal override QueryNode PostProcessNode(QueryNode node)
		{
			// set setMultiTermRewriteMethod for WildcardQueryNode and
			// PrefixWildcardQueryNode
			if (node is WildcardQueryNode || node is AbstractRangeQueryNode || node is RegexpQueryNode)
			{
				MultiTermQuery.RewriteMethod rewriteMethod = GetQueryConfigHandler().Get(StandardQueryConfigHandler.ConfigurationKeys
					.MULTI_TERM_REWRITE_METHOD);
				if (rewriteMethod == null)
				{
					// This should not happen, this configuration is set in the
					// StandardQueryConfigHandler
					throw new ArgumentException("StandardQueryConfigHandler.ConfigurationKeys.MULTI_TERM_REWRITE_METHOD should be set on the QueryConfigHandler"
						);
				}
				// use a TAG to take the value to the Builder
				node.SetTag(MultiTermRewriteMethodProcessor.TAG_ID, rewriteMethod);
			}
			return node;
		}

		protected internal override QueryNode PreProcessNode(QueryNode node)
		{
			return node;
		}

		protected internal override IList<QueryNode> SetChildrenOrder(IList<QueryNode> children
			)
		{
			return children;
		}
	}
}
