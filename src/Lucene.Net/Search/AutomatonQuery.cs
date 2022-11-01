using System;
using System.Text;

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

    using AttributeSource = Lucene.Net.Util.AttributeSource;
    using Automaton = Lucene.Net.Util.Automaton.Automaton;
    using CompiledAutomaton = Lucene.Net.Util.Automaton.CompiledAutomaton;
    using Term = Lucene.Net.Index.Term;
    using Terms = Lucene.Net.Index.Terms;
    using TermsEnum = Lucene.Net.Index.TermsEnum;
    using ToStringUtils = Lucene.Net.Util.ToStringUtils;

    /// <summary>
    /// A <see cref="Query"/> that will match terms against a finite-state machine.
    /// <para>
    /// This query will match documents that contain terms accepted by a given
    /// finite-state machine. The automaton can be constructed with the
    /// <see cref="Lucene.Net.Util.Automaton"/> API. Alternatively, it can be
    /// created from a regular expression with <see cref="RegexpQuery"/> or from
    /// the standard Lucene wildcard syntax with <see cref="WildcardQuery"/>.
    /// </para>
    /// <para>
    /// When the query is executed, it will create an equivalent DFA of the
    /// finite-state machine, and will enumerate the term dictionary in an
    /// intelligent way to reduce the number of comparisons. For example: the regular
    /// expression of <c>[dl]og?</c> will make approximately four comparisons:
    /// do, dog, lo, and log.
    /// </para>
    /// @lucene.experimental
    /// </summary>
    public class AutomatonQuery : MultiTermQuery
    {
        /// <summary>
        /// The automaton to match index terms against </summary>
        protected readonly Automaton m_automaton;

        protected readonly CompiledAutomaton m_compiled;

        /// <summary>
        /// Term containing the field, and possibly some pattern structure </summary>
        protected readonly Term m_term;

        /// <summary>
        /// Create a new AutomatonQuery from an <see cref="Automaton"/>.
        /// </summary>
        /// <param name="term"> <see cref="Term"/> containing field and possibly some pattern structure. The
        ///        term text is ignored. </param>
        /// <param name="automaton"> <see cref="Automaton"/> to run, terms that are accepted are considered a
        ///        match. </param>
        public AutomatonQuery(Term term, Automaton automaton)
            : base(term.Field)
        {
            this.m_term = term;
            this.m_automaton = automaton;
            this.m_compiled = new CompiledAutomaton(automaton);
        }

        protected override TermsEnum GetTermsEnum(Terms terms, AttributeSource atts)
        {
            return m_compiled.GetTermsEnum(terms);
        }

        public override int GetHashCode()
        {
            const int prime = 31;
            int result = base.GetHashCode();
            result = prime * result + m_compiled.GetHashCode();
            result = prime * result + ((m_term is null) ? 0 : m_term.GetHashCode());
            return result;
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }
            if (!base.Equals(obj))
            {
                return false;
            }
            if (this.GetType() != obj.GetType())
            {
                return false;
            }
            AutomatonQuery other = (AutomatonQuery)obj;
            if (!m_compiled.Equals(other.m_compiled))
            {
                return false;
            }
            if (m_term is null)
            {
                if (other.m_term != null)
                {
                    return false;
                }
            }
            else if (!m_term.Equals(other.m_term))
            {
                return false;
            }
            return true;
        }

        public override string ToString(string field)
        {
            StringBuilder buffer = new StringBuilder();
            if (!m_term.Field.Equals(field, StringComparison.Ordinal))
            {
                buffer.Append(m_term.Field);
                buffer.Append(':');
            }
            buffer.Append(this.GetType().Name);
            buffer.Append(" {");
            buffer.Append('\n');
            buffer.Append(m_automaton.ToString());
            buffer.Append('}');
            buffer.Append(ToStringUtils.Boost(Boost));
            return buffer.ToString();
        }

        /// <summary>
        /// Returns the automaton used to create this query </summary>
        public virtual Automaton Automaton => m_automaton;
    }
}