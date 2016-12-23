using System.Text;

namespace Lucene.Net.Search
{
    using AttributeSource = Lucene.Net.Util.AttributeSource;
    using Automaton = Lucene.Net.Util.Automaton.Automaton;
    using CompiledAutomaton = Lucene.Net.Util.Automaton.CompiledAutomaton;

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

    using Term = Lucene.Net.Index.Term;
    using Terms = Lucene.Net.Index.Terms;
    using TermsEnum = Lucene.Net.Index.TermsEnum;
    using ToStringUtils = Lucene.Net.Util.ToStringUtils;

    /// <summary>
    /// A <seealso cref="Query"/> that will match terms against a finite-state machine.
    /// <p>
    /// this query will match documents that contain terms accepted by a given
    /// finite-state machine. The automaton can be constructed with the
    /// <seealso cref="Lucene.Net.Util.Automaton"/> API. Alternatively, it can be
    /// created from a regular expression with <seealso cref="RegexpQuery"/> or from
    /// the standard Lucene wildcard syntax with <seealso cref="WildcardQuery"/>.
    /// </p>
    /// <p>
    /// When the query is executed, it will create an equivalent DFA of the
    /// finite-state machine, and will enumerate the term dictionary in an
    /// intelligent way to reduce the number of comparisons. For example: the regular
    /// expression of <code>[dl]og?</code> will make approximately four comparisons:
    /// do, dog, lo, and log.
    /// </p>
    /// @lucene.experimental
    /// </summary>
    public class AutomatonQuery : MultiTermQuery
    {
        /// <summary>
        /// the automaton to match index terms against </summary>
        protected readonly Automaton Automaton_Renamed; // LUCENENET TODO: rename

        protected readonly CompiledAutomaton Compiled; // LUCENENET TODO: rename

        /// <summary>
        /// term containing the field, and possibly some pattern structure </summary>
        protected readonly Term Term; // LUCENENET TODO: rename

        /// <summary>
        /// Create a new AutomatonQuery from an <seealso cref="Automaton"/>.
        /// </summary>
        /// <param name="term"> Term containing field and possibly some pattern structure. The
        ///        term text is ignored. </param>
        /// <param name="automaton"> Automaton to run, terms that are accepted are considered a
        ///        match. </param>
        public AutomatonQuery(Term term, Automaton automaton)
            : base(term.Field)
        {
            this.Term = term;
            this.Automaton_Renamed = automaton;
            this.Compiled = new CompiledAutomaton(automaton);
        }

        protected override TermsEnum GetTermsEnum(Terms terms, AttributeSource atts)
        {
            return Compiled.GetTermsEnum(terms);
        }

        public override int GetHashCode()
        {
            const int prime = 31;
            int result = base.GetHashCode();
            result = prime * result + Compiled.GetHashCode();
            result = prime * result + ((Term == null) ? 0 : Term.GetHashCode());
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
            if (!Compiled.Equals(other.Compiled))
            {
                return false;
            }
            if (Term == null)
            {
                if (other.Term != null)
                {
                    return false;
                }
            }
            else if (!Term.Equals(other.Term))
            {
                return false;
            }
            return true;
        }

        public override string ToString(string field)
        {
            StringBuilder buffer = new StringBuilder();
            if (!Term.Field.Equals(field))
            {
                buffer.Append(Term.Field);
                buffer.Append(":");
            }
            buffer.Append(this.GetType().Name);
            buffer.Append(" {");
            buffer.Append('\n');
            buffer.Append(Automaton_Renamed.ToString());
            buffer.Append("}");
            buffer.Append(ToStringUtils.Boost(Boost));
            return buffer.ToString();
        }

        /// <summary>
        /// Returns the automaton used to create this query </summary>
        public virtual Automaton Automaton
        {
            get
            {
                return Automaton_Renamed;
            }
        }
    }
}