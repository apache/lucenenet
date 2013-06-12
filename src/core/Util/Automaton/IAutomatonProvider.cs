using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util.Automaton
{
    public interface IAutomatonProvider
    {
        public Automaton GetAutomaton(String name);
    }
}
