using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Processors;
using Lucene.Net.QueryParsers.Flexible.Core.Util;
using Lucene.Net.QueryParsers.Flexible.Standard.Nodes;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Processors
{
    /// <summary>
    /// The {@link StandardSyntaxParser} creates {@link PrefixWildcardQueryNode} nodes which
    /// have values containing the prefixed wildcard. However, Lucene
    /// {@link PrefixQuery} cannot contain the prefixed wildcard. So, this processor
    /// basically removed the prefixed wildcard from the
    /// {@link PrefixWildcardQueryNode} value.
    /// </summary>
    /// <seealso cref="PrefixQuery"/>
    /// <seealso cref="PrefixWildcardQueryNode"/>
    public class WildcardQueryNodeProcessor : QueryNodeProcessorImpl
    {
        public WildcardQueryNodeProcessor()
        {
            // empty constructor
        }


        protected override IQueryNode PostProcessNode(IQueryNode node)
        {

            // the old Lucene Parser ignores FuzzyQueryNode that are also PrefixWildcardQueryNode or WildcardQueryNode
            // we do the same here, also ignore empty terms
            if (node is FieldQueryNode || node is FuzzyQueryNode)
            {
                FieldQueryNode fqn = (FieldQueryNode)node;
                string text = fqn.Text.ToString();

                // do not process wildcards for TermRangeQueryNode children and 
                // QuotedFieldQueryNode to reproduce the old parser behavior
                if (fqn.GetParent() is TermRangeQueryNode
                    || fqn is QuotedFieldQueryNode
                    || text.Length <= 0)
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
                else if (IsWildcard(text))
                {
                    WildcardQueryNode wildcardQN = new WildcardQueryNode(fqn);
                    return wildcardQN;
                }

            }

            return node;

        }

        private bool IsWildcard(string text)
        {
            if (text == null || text.Length <= 0) return false;

            // If a un-escaped '*' or '?' if found return true
            // start at the end since it's more common to put wildcards at the end
            for (int i = text.Length - 1; i >= 0; i--)
            {
                if ((text[i] == '*' || text[i] == '?') && !UnescapedCharSequence.WasEscaped(new StringCharSequenceWrapper(text), i))
                {
                    return true;
                }
            }

            return false;
        }

        private bool IsPrefixWildcard(string text)
        {
            if (text == null || text.Length <= 0 || !IsWildcard(text)) return false;

            // Validate last character is a '*' and was not escaped
            // If single '*' is is a wildcard not prefix to simulate old queryparser
            if (text[text.Length - 1] != '*') return false;
            if (UnescapedCharSequence.WasEscaped(new StringCharSequenceWrapper(text), text.Length - 1)) return false;
            if (text.Length == 1) return false;

            // Only make a prefix if there is only one single star at the end and no '?' or '*' characters
            // If single wildcard return false to mimic old queryparser
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '?') return false;
                if (text[i] == '*' && !UnescapedCharSequence.WasEscaped(new StringCharSequenceWrapper(text), i))
                {
                    if (i == text.Length - 1)
                        return true;
                    else
                        return false;
                }
            }

            return false;
        }


        protected override IQueryNode PreProcessNode(IQueryNode node)
        {

            return node;

        }


        protected override IList<IQueryNode> SetChildrenOrder(IList<IQueryNode> children)
        {

            return children;

        }
    }
}
