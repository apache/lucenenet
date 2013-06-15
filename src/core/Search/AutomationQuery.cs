using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Index;
using Lucene.Net.Util;
using Lucene.Net.Index;
using Lucene.Net.Util;
using Lucene.Net.Util.Automaton;


namespace Lucene.Net.Search
{
    public class AutomatonQuery : MultiTermQuery
    {
        /** the automaton to match index terms against */
        protected readonly Automaton automaton;
        protected readonly CompiledAutomaton compiled;
        /** term containing the field, and possibly some pattern structure */
        protected readonly Term term;
        
        public AutomatonQuery(Term term, Automaton automaton)
            : base(term.Field)
        {
            this.term = term;
            this.automaton = automaton;
            this.compiled = new CompiledAutomaton(automaton);
        }

        protected override TermsEnum GetTermsEnum(Terms terms, AttributeSource atts)
        {
            return compiled.GetTermsEnum(terms);
        }

        public override int GetHashCode()
        {
            const int prime = 31;
            int result = base.GetHashCode();
            if (automaton != null)
            {
                // we already minimized the automaton in the ctor, so
                // this hash code will be the same for automata that
                // are the same:
                int automatonHashCode = automaton.GetNumberOfStates() * 3 + automaton.GetNumberOfTransitions() * 2;
                if (automatonHashCode == 0)
                {
                    automatonHashCode = 1;
                }
                result = prime * result + automatonHashCode;
            }
            result = prime * result + ((term == null) ? 0 : term.GetHashCode());
            return result;
        }

        public override bool Equals(Object obj)
        {
            if (this == obj)
                return true;
            if (!base.Equals(obj))
                return false;
            if (GetType() != obj.GetType())
                return false;
            AutomatonQuery other = (AutomatonQuery)obj;
            if (automaton == null)
            {
                if (other.automaton != null)
                    return false;
            }
            else if (!BasicOperations.SameLanguage(automaton, other.automaton))
                return false;
            if (term == null)
            {
                if (other.term != null)
                    return false;
            }
            else if (!term.Equals(other.term))
                return false;
            return true;
        }

        public override String ToString(String field)
        {
            StringBuilder buffer = new StringBuilder();
            if (!term.Field.Equals(field))
            {
                buffer.Append(term.Field);
                buffer.Append(":");
            }
            buffer.Append(GetType().Name);
            buffer.Append(" {");
            buffer.Append('\n');
            buffer.Append(automaton.ToString());
            buffer.Append("}");
            buffer.Append(ToStringUtils.Boost(Boost));
            return buffer.ToString();
        }
    }
}
