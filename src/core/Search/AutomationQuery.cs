using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Index;
using Lucene.Net.Util;
using Lucene.Net.Index;
using Lucene.Net.Util;


namespace Lucene.Net.Search
{
public class AutomatonQuery : MultiTermQuery 
{
  /** the automaton to match index terms against */
  protected const Automaton automaton;
  protected const CompiledAutomaton compiled;
  /** term containing the field, and possibly some pattern structure */
  protected const Term term;


  public AutomatonQuery(Term term, Automaton automaton) 
{
    base(term.field());
    this.term = term;
    this.automaton = automaton;
    this.compiled = new CompiledAutomaton(automaton);
  }


  protected override TermsEnum getTermsEnum(Terms terms, AttributeSource atts) 
{
    return compiled.getTermsEnum(terms);
  }
 
  public override int hashCode() 
  {
    const int prime = 31;
    int result = base.hashCode();
    if (automaton != null) 
	{
      // we already minimized the automaton in the ctor, so
      // this hash code will be the same for automata that
      // are the same:
      int automatonHashCode = automaton.getNumberOfStates() * 3 + automaton.getNumberOfTransitions() * 2;
      if (automatonHashCode == 0) 
	  {
        automatonHashCode = 1;
      }
      result = prime * result + automatonHashCode;
    }
    result = prime * result + ((term == null) ? 0 : term.hashCode());
    return result;
  }

  public override bool equals(Object obj) {
    if (this == obj)
      return true;
    if (!base.equals(obj))
      return false;
    if (getClass() != obj.getClass())
      return false;
    AutomatonQuery other = (AutomatonQuery) obj;
    if (automaton == null) {
      if (other.automaton != null)
        return false;
    } else if (!BasicOperations.sameLanguage(automaton, other.automaton))
      return false;
    if (term == null) {
      if (other.term != null)
        return false;
    } else if (!term.equals(other.term))
      return false;
    return true;
  }

  public override String toString(String field) {
    StringBuilder buffer = new StringBuilder();
    if (!term.field().equals(field)) {
      buffer.append(term.field());
      buffer.append(":");
    }
    buffer.append(getClass().getSimpleName());
    buffer.append(" {");
    buffer.append('\n');
    buffer.append(automaton.ToString());
    buffer.append("}");
    buffer.append(ToStringUtils.boost(getBoost()));
    return buffer.ToString();
  }
}
}
