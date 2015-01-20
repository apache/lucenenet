/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using System.Globalization;
using Lucene.Net.Queryparser.Flexible.Core.Nodes;
using Lucene.Net.Queryparser.Flexible.Core.Processors;
using Lucene.Net.Queryparser.Flexible.Core.Util;
using Lucene.Net.Queryparser.Flexible.Standard.Config;
using Lucene.Net.Queryparser.Flexible.Standard.Nodes;
using Sharpen;

namespace Lucene.Net.Queryparser.Flexible.Standard.Processors
{
	/// <summary>
	/// This processor verifies if
	/// <see cref="Lucene.Net.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler.ConfigurationKeys.LOWERCASE_EXPANDED_TERMS
	/// 	">Lucene.Net.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler.ConfigurationKeys.LOWERCASE_EXPANDED_TERMS
	/// 	</see>
	/// is defined in the
	/// <see cref="Lucene.Net.Queryparser.Flexible.Core.Config.QueryConfigHandler"
	/// 	>Lucene.Net.Queryparser.Flexible.Core.Config.QueryConfigHandler</see>
	/// . If it is and the expanded terms should be
	/// lower-cased, it looks for every
	/// <see cref="Lucene.Net.Queryparser.Flexible.Standard.Nodes.WildcardQueryNode
	/// 	">Lucene.Net.Queryparser.Flexible.Standard.Nodes.WildcardQueryNode</see>
	/// ,
	/// <see cref="Lucene.Net.Queryparser.Flexible.Core.Nodes.FuzzyQueryNode">Lucene.Net.Queryparser.Flexible.Core.Nodes.FuzzyQueryNode
	/// 	</see>
	/// and children of a
	/// <see cref="Lucene.Net.Queryparser.Flexible.Core.Nodes.RangeQueryNode{T}">Lucene.Net.Queryparser.Flexible.Core.Nodes.RangeQueryNode&lt;T&gt;
	/// 	</see>
	/// and lower-case its
	/// term. <br/>
	/// </summary>
	/// <seealso cref="Lucene.Net.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler.ConfigurationKeys.LOWERCASE_EXPANDED_TERMS
	/// 	">Lucene.Net.Queryparser.Flexible.Standard.Config.StandardQueryConfigHandler.ConfigurationKeys.LOWERCASE_EXPANDED_TERMS
	/// 	</seealso>
	public class LowercaseExpandedTermsQueryNodeProcessor : QueryNodeProcessorImpl
	{
		public LowercaseExpandedTermsQueryNodeProcessor()
		{
		}

		/// <exception cref="Lucene.Net.Queryparser.Flexible.Core.QueryNodeException">
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

		/// <exception cref="Lucene.Net.Queryparser.Flexible.Core.QueryNodeException">
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
