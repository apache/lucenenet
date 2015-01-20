/*
 * This code is derived from MyJavaLibrary (http://somelinktomycoollibrary)
 * 
 * If this is an open source Java library, include the proper license and copyright attributions here!
 */

using System.Collections.Generic;
using Lucene.Net.Queryparser.Flexible.Core.Nodes;
using Lucene.Net.Queryparser.Flexible.Core.Processors;
using Lucene.Net.Queryparser.Flexible.Core.Util;
using Lucene.Net.Queryparser.Flexible.Standard.Nodes;
using Sharpen;

namespace Lucene.Net.Queryparser.Flexible.Standard.Processors
{
	/// <summary>
	/// The
	/// <see cref="Lucene.Net.Queryparser.Flexible.Standard.Parser.StandardSyntaxParser
	/// 	">Lucene.Net.Queryparser.Flexible.Standard.Parser.StandardSyntaxParser</see>
	/// creates
	/// <see cref="Lucene.Net.Queryparser.Flexible.Standard.Nodes.PrefixWildcardQueryNode
	/// 	">Lucene.Net.Queryparser.Flexible.Standard.Nodes.PrefixWildcardQueryNode</see>
	/// nodes which
	/// have values containing the prefixed wildcard. However, Lucene
	/// <see cref="Lucene.Net.Search.PrefixQuery">Lucene.Net.Search.PrefixQuery
	/// 	</see>
	/// cannot contain the prefixed wildcard. So, this processor
	/// basically removed the prefixed wildcard from the
	/// <see cref="Lucene.Net.Queryparser.Flexible.Standard.Nodes.PrefixWildcardQueryNode
	/// 	">Lucene.Net.Queryparser.Flexible.Standard.Nodes.PrefixWildcardQueryNode</see>
	/// value. <br/>
	/// </summary>
	/// <seealso cref="Lucene.Net.Search.PrefixQuery">Lucene.Net.Search.PrefixQuery
	/// 	</seealso>
	/// <seealso cref="Lucene.Net.Queryparser.Flexible.Standard.Nodes.PrefixWildcardQueryNode
	/// 	">Lucene.Net.Queryparser.Flexible.Standard.Nodes.PrefixWildcardQueryNode</seealso>
	public class WildcardQueryNodeProcessor : QueryNodeProcessorImpl
	{
		public WildcardQueryNodeProcessor()
		{
		}

		// empty constructor
		/// <exception cref="Lucene.Net.Queryparser.Flexible.Core.QueryNodeException">
		/// 	</exception>
		protected internal override QueryNode PostProcessNode(QueryNode node)
		{
			// the old Lucene Parser ignores FuzzyQueryNode that are also PrefixWildcardQueryNode or WildcardQueryNode
			// we do the same here, also ignore empty terms
			if (node is FieldQueryNode || node is FuzzyQueryNode)
			{
				FieldQueryNode fqn = (FieldQueryNode)node;
				CharSequence text = fqn.GetText();
				// do not process wildcards for TermRangeQueryNode children and 
				// QuotedFieldQueryNode to reproduce the old parser behavior
				if (fqn.GetParent() is TermRangeQueryNode || fqn is QuotedFieldQueryNode || text.
					Length <= 0)
				{
					// Ignore empty terms
					return node;
				}
				// Code below simulates the old lucene parser behavior for wildcards
				if (IsPrefixWildcard(text))
				{
					PrefixWildcardQueryNode prefixWildcardQN = new PrefixWildcardQueryNode(fqn);
					return prefixWildcardQN;
				}
				else
				{
					if (IsWildcard(text))
					{
						WildcardQueryNode wildcardQN = new WildcardQueryNode(fqn);
						return wildcardQN;
					}
				}
			}
			return node;
		}

		private bool IsWildcard(CharSequence text)
		{
			if (text == null || text.Length <= 0)
			{
				return false;
			}
			// If a un-escaped '*' or '?' if found return true
			// start at the end since it's more common to put wildcards at the end
			for (int i = text.Length - 1; i >= 0; i--)
			{
				if ((text[i] == '*' || text[i] == '?') && !UnescapedCharSequence.WasEscaped(text, 
					i))
				{
					return true;
				}
			}
			return false;
		}

		private bool IsPrefixWildcard(CharSequence text)
		{
			if (text == null || text.Length <= 0 || !IsWildcard(text))
			{
				return false;
			}
			// Validate last character is a '*' and was not escaped
			// If single '*' is is a wildcard not prefix to simulate old queryparser
			if (text[text.Length - 1] != '*')
			{
				return false;
			}
			if (UnescapedCharSequence.WasEscaped(text, text.Length - 1))
			{
				return false;
			}
			if (text.Length == 1)
			{
				return false;
			}
			// Only make a prefix if there is only one single star at the end and no '?' or '*' characters
			// If single wildcard return false to mimic old queryparser
			for (int i = 0; i < text.Length; i++)
			{
				if (text[i] == '?')
				{
					return false;
				}
				if (text[i] == '*' && !UnescapedCharSequence.WasEscaped(text, i))
				{
					if (i == text.Length - 1)
					{
						return true;
					}
					else
					{
						return false;
					}
				}
			}
			return false;
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
