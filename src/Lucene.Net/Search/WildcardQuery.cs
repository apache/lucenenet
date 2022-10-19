using J2N;
using System;
using System.Collections.Generic;
using System.Text;
using JCG = J2N.Collections.Generic;

namespace Lucene.Net.Search
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

    using Automaton = Lucene.Net.Util.Automaton.Automaton;
    using BasicAutomata = Lucene.Net.Util.Automaton.BasicAutomata;
    using BasicOperations = Lucene.Net.Util.Automaton.BasicOperations;
    using Term = Lucene.Net.Index.Term;
    using ToStringUtils = Lucene.Net.Util.ToStringUtils;

    /// <summary>
    /// Implements the wildcard search query. Supported wildcards are <c>*</c>, which
    /// matches any character sequence (including the empty one), and <c>?</c>,
    /// which matches any single character. '\' is the escape character.
    /// <para/>
    /// Note this query can be slow, as it
    /// needs to iterate over many terms. In order to prevent extremely slow WildcardQueries,
    /// a Wildcard term should not start with the wildcard <c>*</c>
    ///
    /// <para/>This query uses the 
    /// <see cref="MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT"/>
    /// rewrite method.
    /// </summary>
    /// <seealso cref="AutomatonQuery"/>
    public class WildcardQuery : AutomatonQuery
    {
        /// <summary>
        /// String equality with support for wildcards </summary>
        public const char WILDCARD_STRING = '*';

        /// <summary>
        /// Char equality with support for wildcards </summary>
        public const char WILDCARD_CHAR = '?';

        /// <summary>
        /// Escape character </summary>
        public const char WILDCARD_ESCAPE = '\\';

        /// <summary>
        /// Constructs a query for terms matching <paramref name="term"/>.
        /// </summary>
        public WildcardQuery(Term term)
            : base(term, ToAutomaton(term))
        {
        }

        /// <summary>
        /// Convert Lucene wildcard syntax into an automaton.
        /// <para/>
        /// @lucene.internal
        /// </summary>
        public static Automaton ToAutomaton(Term wildcardquery)
        {
            IList<Automaton> automata = new JCG.List<Automaton>();

            string wildcardText = wildcardquery.Text;

            for (int i = 0; i < wildcardText.Length; )
            {
                int c = Character.CodePointAt(wildcardText, i);
                int length = Character.CharCount(c);
                switch (c)
                {
                    case WILDCARD_STRING:
                        automata.Add(BasicAutomata.MakeAnyString());
                        break;

                    case WILDCARD_CHAR:
                        automata.Add(BasicAutomata.MakeAnyChar());
                        break;

                    case WILDCARD_ESCAPE:
                        // add the next codepoint instead, if it exists
                        if (i + length < wildcardText.Length)
                        {
                            int nextChar = Character.CodePointAt(wildcardText, i + length);
                            length += Character.CharCount(nextChar);
                            automata.Add(BasicAutomata.MakeChar(nextChar));
                            break;
                        } // else fallthru, lenient parsing with a trailing \
                        goto default;
                    default:
                        automata.Add(BasicAutomata.MakeChar(c));
                        break;
                }
                i += length;
            }

            return BasicOperations.Concatenate(automata);
        }

        /// <summary>
        /// Returns the pattern term.
        /// </summary>
        public virtual Term Term => base.m_term;

        /// <summary>
        /// Prints a user-readable version of this query. </summary>
        public override string ToString(string field)
        {
            StringBuilder buffer = new StringBuilder();
            if (!Field.Equals(field, StringComparison.Ordinal))
            {
                buffer.Append(Field);
                buffer.Append(':');
            }
            buffer.Append(Term.Text);
            buffer.Append(ToStringUtils.Boost(Boost));
            return buffer.ToString();
        }
    }
}