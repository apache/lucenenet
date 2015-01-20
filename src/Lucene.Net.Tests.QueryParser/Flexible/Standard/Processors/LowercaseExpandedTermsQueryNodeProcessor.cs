/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using System.Globalization;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Processors;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Util;
using Org.Apache.Lucene.Queryparser.Flexible.Standard.Config;
using Org.Apache.Lucene.Queryparser.Flexible.Standard.Nodes;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Flexible.Standard.Processors
{
	/// <summary>
	/// This processor verifies if
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler.ConfigurationKeys.LOWERCASE_EXPANDED_TERMS
	/// 	">Org.Apache.Lucene.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler.ConfigurationKeys.LOWERCASE_EXPANDED_TERMS
	/// 	</see>
	/// is defined in the
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Config.QueryConfigHandler"
	/// 	>Org.Apache.Lucene.Queryparser.Flexible.Core.Config.QueryConfigHandler</see>
	/// . If it is and the expanded terms should be
	/// lower-cased, it looks for every
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Standard.Nodes.WildcardQueryNode
	/// 	">Org.Apache.Lucene.Queryparser.Flexible.Standard.Nodes.WildcardQueryNode</see>
	/// ,
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.FuzzyQueryNode">Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.FuzzyQueryNode
	/// 	</see>
	/// and children of a
	/// <see cref="Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.RangeQueryNode{T}">Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes.RangeQueryNode&lt;T&gt;
	/// 	</see>
	/// and lower-case its
	/// term. <br/>
	/// </summary>
	/// <seealso cref="Org.Apache.Lucene.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler.ConfigurationKeys.LOWERCASE_EXPANDED_TERMS
	/// 	">Org.Apache.Lucene.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler.ConfigurationKeys.LOWERCASE_EXPANDED_TERMS
	/// 	</seealso>
	public class LowercaseExpandedTermsQueryNodeProcessor : QueryNodeProcessorImpl
	{
		public LowercaseExpandedTermsQueryNodeProcessor()
		{
		}

		/// <exception cref="Org.Apache.Lucene.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		public override QueryNode Process(QueryNode queryTree)
		{
			bool lowercaseExpandedTerms = GetQueryConfigHandler().Get(StandardQueryConfigHandler.ConfigurationKeys
				.LOWERCASE_EXPANDED_TERMS);
			if (lowercaseExpandedTerms != null && lowercaseExpandedTerms)
			{
				return base.Process(queryTree);
			}
			return queryTree;
		}

		/// <exception cref="Org.Apache.Lucene.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		protected internal override QueryNode PostProcessNode(QueryNode node)
		{
			CultureInfo locale = GetQueryConfigHandler().Get(StandardQueryConfigHandler.ConfigurationKeys
				.LOCALE);
			if (locale == null)
			{
				locale = CultureInfo.CurrentCulture;
			}
			if (node is WildcardQueryNode || node is FuzzyQueryNode || (node is FieldQueryNode
				 && node.GetParent() is RangeQueryNode) || node is RegexpQueryNode)
			{
				TextableQueryNode txtNode = (TextableQueryNode)node;
				CharSequence text = txtNode.GetText();
				txtNode.SetText(text != null ? UnescapedCharSequence.ToLowerCase(text, locale) : 
					null);
			}
			return node;
		}

		/// <exception cref="Org.Apache.Lucene.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		protected internal override QueryNode PreProcessNode(QueryNode node)
		{
			return node;
		}

		/// <exception cref="Org.Apache.Lucene.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		protected internal override IList<QueryNode> SetChildrenOrder(IList<QueryNode> children
			)
		{
			return children;
		}
	}
}
