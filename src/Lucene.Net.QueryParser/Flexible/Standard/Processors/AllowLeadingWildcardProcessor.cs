/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Lucene.Net.Queryparser.Flexible.Core;
using Lucene.Net.Queryparser.Flexible.Core.Messages;
using Lucene.Net.Queryparser.Flexible.Core.Nodes;
using Lucene.Net.Queryparser.Flexible.Core.Processors;
using Lucene.Net.Queryparser.Flexible.Core.Util;
using Lucene.Net.Queryparser.Flexible.Messages;
using Lucene.Net.Queryparser.Flexible.Standard.Config;
using Lucene.Net.Queryparser.Flexible.Standard.Nodes;
using Lucene.Net.Queryparser.Flexible.Standard.Parser;
using Sharpen;

namespace Lucene.Net.Queryparser.Flexible.Standard.Processors
{
	/// <summary>
	/// This processor verifies if
	/// <see cref="Lucene.Net.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler.ConfigurationKeys.ALLOW_LEADING_WILDCARD
	/// 	">Lucene.Net.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler.ConfigurationKeys.ALLOW_LEADING_WILDCARD
	/// 	</see>
	/// is defined in the
	/// <see cref="Lucene.Net.Queryparser.Flexible.Core.Config.QueryConfigHandler"
	/// 	>Lucene.Net.Queryparser.Flexible.Core.Config.QueryConfigHandler</see>
	/// . If it is and leading wildcard is not allowed, it
	/// looks for every
	/// <see cref="Lucene.Net.Queryparser.Flexible.Standard.Nodes.WildcardQueryNode
	/// 	">Lucene.Net.Queryparser.Flexible.Standard.Nodes.WildcardQueryNode</see>
	/// contained in the query node tree
	/// and throws an exception if any of them has a leading wildcard ('*' or '?'). <br/>
	/// </summary>
	/// <seealso cref="Lucene.Net.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler.ConfigurationKeys.ALLOW_LEADING_WILDCARD
	/// 	">Lucene.Net.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler.ConfigurationKeys.ALLOW_LEADING_WILDCARD
	/// 	</seealso>
	public class AllowLeadingWildcardProcessor : QueryNodeProcessorImpl
	{
		public AllowLeadingWildcardProcessor()
		{
		}

		// empty constructor
		/// <exception cref="Lucene.Net.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		public override QueryNode Process(QueryNode queryTree)
		{
			bool allowsLeadingWildcard = GetQueryConfigHandler().Get(StandardQueryConfigHandler.ConfigurationKeys
				.ALLOW_LEADING_WILDCARD);
			if (allowsLeadingWildcard != null)
			{
				if (!allowsLeadingWildcard)
				{
					return base.Process(queryTree);
				}
			}
			return queryTree;
		}

		/// <exception cref="Lucene.Net.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		protected internal override QueryNode PostProcessNode(QueryNode node)
		{
			if (node is WildcardQueryNode)
			{
				WildcardQueryNode wildcardNode = (WildcardQueryNode)node;
				if (wildcardNode.GetText().Length > 0)
				{
					// Validate if the wildcard was escaped
					if (UnescapedCharSequence.WasEscaped(wildcardNode.GetText(), 0))
					{
						return node;
					}
					switch (wildcardNode.GetText()[0])
					{
						case '*':
						case '?':
						{
							throw new QueryNodeException(new MessageImpl(QueryParserMessages.LEADING_WILDCARD_NOT_ALLOWED
								, node.ToQueryString(new EscapeQuerySyntaxImpl())));
						}
					}
				}
			}
			return node;
		}

		/// <exception cref="Lucene.Net.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		protected internal override QueryNode PreProcessNode(QueryNode node)
		{
			return node;
		}

		/// <exception cref="Lucene.Net.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		protected internal override IList<QueryNode> SetChildrenOrder(IList<QueryNode> children
			)
		{
			return children;
		}
	}
}
