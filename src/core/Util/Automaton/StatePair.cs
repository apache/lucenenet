using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util.Automaton
{
    public class StatePair
    {
        private State s;
        private State s1;
        private State s2;

        public StatePair(State s, State s1, State s2)
        {
            this.s = s;
            this.s1 = s1;
            this.s2 = s2;
        }

        public StatePair(State s1, State s2)
        {
            this.s1 = s1;
            this.s2 = s2;
        }

        public State State
        {
            get { return s; }
            set { s = value; }
        }

        public State FirstState
        {
            get { return s1; }
        }

        public State SecondState
        {
            get { return s2; }
        }

        public override bool Equals(object obj)
        {
            if (obj is StatePair)
            {
                StatePair p = (StatePair)obj;
                return p.s1 == s1 && p.s2 == s2;
            }
            else return false;
        }

        public override int GetHashCode()
        {
            return s1.GetHashCode() + s2.GetHashCode();
        }
    }
}
