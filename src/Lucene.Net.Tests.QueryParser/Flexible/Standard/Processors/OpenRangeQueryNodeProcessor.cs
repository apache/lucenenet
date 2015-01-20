/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Nodes;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Processors;
using Org.Apache.Lucene.Queryparser.Flexible.Core.Util;
using Org.Apache.Lucene.Queryparser.Flexible.Standard.Nodes;
using Sharpen;

namespace Org.Apache.Lucene.Queryparser.Flexible.Standard.Processors
{
	/// <summary>
	/// Processes
	/// <see cref="Org.Apache.Lucene.Search.TermRangeQuery">Org.Apache.Lucene.Search.TermRangeQuery
	/// 	</see>
	/// s with open ranges.
	/// </summary>
	public class OpenRangeQueryNodeProcessor : QueryNodeProcessorImpl
	{
		public static readonly string OPEN_RANGE_TOKEN = "*";

		public OpenRangeQueryNodeProcessor()
		{
		}

		// javadocs
		/// <exception cref="Org.Apache.Lucene.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		protected internal override QueryNode PostProcessNode(QueryNode node)
		{
			if (node is TermRangeQueryNode)
			{
				TermRangeQueryNode rangeNode = (TermRangeQueryNode)node;
				FieldQueryNode lowerNode = rangeNode.GetLowerBound();
				FieldQueryNode upperNode = rangeNode.GetUpperBound();
				CharSequence lowerText = lowerNode.GetText();
				CharSequence upperText = upperNode.GetText();
				if (OPEN_RANGE_TOKEN.Equals(upperNode.GetTextAsString()) && (!(upperText is UnescapedCharSequence
					) || !((UnescapedCharSequence)upperText).WasEscaped(0)))
				{
					upperText = string.Empty;
				}
				if (OPEN_RANGE_TOKEN.Equals(lowerNode.GetTextAsString()) && (!(lowerText is UnescapedCharSequence
					) || !((UnescapedCharSequence)lowerText).WasEscaped(0)))
				{
					lowerText = string.Empty;
				}
				lowerNode.SetText(lowerText);
				upperNode.SetText(upperText);
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
