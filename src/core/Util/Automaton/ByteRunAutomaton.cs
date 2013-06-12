using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util.Automaton
{
    public class ByteRunAutomaton : RunAutomaton
    {
        public ByteRunAutomaton(Automaton a)
            : this(a, false)
        {
        }

        public ByteRunAutomaton(Automaton a, bool utf8)
            : base(utf8 ? a : new UTF32ToUTF8().Convert(a), 256, true)
        {
        }

        public bool Run(sbyte[] s, int offset, int length)
        {
            int p = InitialState;
            int l = offset + length;
            for (int i = offset; i < l; i++)
            {
                p = Step(p, s[i] & 0xFF);
                if (p == -1) return false;
            }
            return accept[p];
        }
    }
}
