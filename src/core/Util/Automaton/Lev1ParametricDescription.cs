using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util.Automaton
{
    internal class Lev1ParametricDescription : LevenshteinAutomata.ParametricDescription
    {
        public override int Transition(int absState, int position, int vector)
        {
            // null absState should never be passed in
            //assert absState != -1;

            // decode absState -> state, offset
            int state = absState / (w + 1);
            int offset = absState % (w + 1);
            //assert offset >= 0;

            if (position == w)
            {
                if (state < 2)
                {
                    int loc = vector * 2 + state;
                    offset += Unpack(offsetIncrs0, loc, 1);
                    state = Unpack(toStates0, loc, 2) - 1;
                }
            }
            else if (position == w - 1)
            {
                if (state < 3)
                {
                    int loc = vector * 3 + state;
                    offset += Unpack(offsetIncrs1, loc, 1);
                    state = Unpack(toStates1, loc, 2) - 1;
                }
            }
            else if (position == w - 2)
            {
                if (state < 5)
                {
                    int loc = vector * 5 + state;
                    offset += Unpack(offsetIncrs2, loc, 2);
                    state = Unpack(toStates2, loc, 3) - 1;
                }
            }
            else
            {
                if (state < 5)
                {
                    int loc = vector * 5 + state;
                    offset += Unpack(offsetIncrs3, loc, 2);
                    state = Unpack(toStates3, loc, 3) - 1;
                }
            }

            if (state == -1)
            {
                // null state
                return -1;
            }
            else
            {
                // translate back to abs
                return state * (w + 1) + offset;
            }
        }

        // 1 vectors; 2 states per vector; array length = 2
        private readonly static long[] toStates0 = new long[] /*2 bits per value */ {
            0x2L
          };
        private readonly static long[] offsetIncrs0 = new long[] /*1 bits per value */ {
            0x0L
          };

        // 2 vectors; 3 states per vector; array length = 6
        private readonly static long[] toStates1 = new long[] /*2 bits per value */ {
            0xa43L
          };
        private readonly static long[] offsetIncrs1 = new long[] /*1 bits per value */ {
            0x38L
          };

        // 4 vectors; 5 states per vector; array length = 20
        private readonly static long[] toStates2 = new long[] /*3 bits per value */ {
            0x69a292450428003L
          };
        private readonly static long[] offsetIncrs2 = new long[] /*2 bits per value */ {
            0x5555588000L
          };

        // 8 vectors; 5 states per vector; array length = 40
        private readonly static long[] toStates3 = new long[] /*3 bits per value */ {
            0x1690a82152018003L,0xb1a2d346448a49L
          };
        private readonly static long[] offsetIncrs3 = new long[] /*2 bits per value */ {
            0x555555b8220f0000L,0x5555L
          };

        // state map
        //   0 -> [(0, 0)]
        //   1 -> [(0, 1)]
        //   2 -> [(0, 1), (1, 1)]
        //   3 -> [(0, 1), (2, 1)]
        //   4 -> [(0, 1), (1, 1), (2, 1)]


        public Lev1ParametricDescription(int w)
            : base(w, 1, new int[] { 0, 1, 0, -1, -1 })
        {

        }
    }
}
