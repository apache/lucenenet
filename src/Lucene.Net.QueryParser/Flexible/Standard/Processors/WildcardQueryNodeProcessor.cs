using J2N.Text;
using Lucene.Net.QueryParsers.Flexible.Core.Nodes;
using Lucene.Net.QueryParsers.Flexible.Core.Processors;
using Lucene.Net.QueryParsers.Flexible.Core.Util;
using Lucene.Net.QueryParsers.Flexible.Standard.Nodes;
using System.Collections.Generic;

namespace Lucene.Net.QueryParsers.Flexible.Standard.Processors
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    /// <summary>
    /// The <see cref="Parser.StandardSyntaxParser"/> creates <see cref="PrefixWildcardQueryNode"/> nodes which
    /// have values containing the prefixed wildcard. However, Lucene
    /// <see cref="Search.PrefixQuery"/> cannot contain the prefixed wildcard. So, this processor
    /// basically removed the prefixed wildcard from the
    /// <see cref="PrefixWildcardQueryNode"/> value.
    /// </summary>
    /// <seealso cref="Search.PrefixQuery"/>
    /// <seealso cref="PrefixWildcardQueryNode"/>
    public class WildcardQueryNodeProcessor : QueryNodeProcessor
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
                if (fqn.Parent is TermRangeQueryNode
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

        private static bool IsWildcard(string text) // LUCENENET: CA1822: Mark members as static
        {
            if (text is null || text.Length <= 0) return false;

            // If a un-escaped '*' or '?' if found return true
            // start at the end since it's more common to put wildcards at the end
            for (int i = text.Length - 1; i >= 0; i--)
            {
                if ((text[i] == '*' || text[i] == '?') && !UnescapedCharSequence.WasEscaped(new StringCharSequence(text), i))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsPrefixWildcard(string text) // LUCENENET: CA1822: Mark members as static
        {
            if (text is null || text.Length <= 0 || !IsWildcard(text)) return false;

            // Validate last character is a '*' and was not escaped
            // If single '*' is is a wildcard not prefix to simulate old queryparser
            if (text[text.Length - 1] != '*') return false;
            if (UnescapedCharSequence.WasEscaped(new StringCharSequence(text), text.Length - 1)) return false;
            if (text.Length == 1) return false;

            // Only make a prefix if there is only one single star at the end and no '?' or '*' characters
            // If single wildcard return false to mimic old queryparser
            for (int i = 0; i < text.Length; i++)
            {
                if (text[i] == '?') return false;
                if (text[i] == '*' && !UnescapedCharSequence.WasEscaped(new StringCharSequence(text), i))
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
