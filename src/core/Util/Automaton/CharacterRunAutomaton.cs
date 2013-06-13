using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util.Automaton
{
    public class CharacterRunAutomaton : RunAutomaton
    {
        public CharacterRunAutomaton(Automaton a)
            : base(a, Character.MAX_CODE_POINT, false)
        {
        }

        public bool Run(String s)
        {
            int p = InitialState;
            int l = s.Length;
            for (int i = 0, cp = 0; i < l; i += 1)
            {
                p = Step(p, cp = s[i]);
                if (p == -1) return false;
            }
            return accept[p];
        }

        public bool Run(char[] s, int offset, int length)
        {
            int p = InitialState;
            int l = offset + length;
            for (int i = offset, cp = 0; i < l; i += 1)
            {
                p = Step(p, cp = s[i] /* Character.codePointAt(s, i, l) */ );
                if (p == -1) return false;
            }
            return accept[p];
        }
    }
}
