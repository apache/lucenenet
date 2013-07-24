using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util.Automaton
{
    public class LevenshteinAutomata
    {
        public const int MAXIMUM_SUPPORTED_DISTANCE = 2;

        /* input word */
        readonly int[] word;
        /* the automata alphabet. */
        readonly int[] alphabet;
        /* the maximum symbol in the alphabet (e.g. 255 for UTF-8 or 10FFFF for UTF-32) */
        readonly int alphaMax;

        /* the ranges outside of alphabet */
        readonly int[] rangeLower;
        readonly int[] rangeUpper;
        int numRanges = 0;

        ParametricDescription[] descriptions;

        public LevenshteinAutomata(String input, bool withTranspositions)
            : this(CodePoints(input), Character.MAX_CODE_POINT, withTranspositions)
        {
        }

        public LevenshteinAutomata(int[] word, int alphaMax, bool withTranspositions)
        {
            this.word = word;
            this.alphaMax = alphaMax;

            // calculate the alphabet
            SortedSet<int> set = new SortedSet<int>();
            for (int i = 0; i < word.Length; i++)
            {
                int v = word[i];
                if (v > alphaMax)
                {
                    throw new ArgumentException("alphaMax exceeded by symbol " + v + " in word");
                }
                set.Add(v);
            }
            alphabet = new int[set.Count];
            IEnumerator<int> iterator = set.GetEnumerator();
            for (int i = 0; i < alphabet.Length && iterator.MoveNext(); i++)
                alphabet[i] = iterator.Current;

            rangeLower = new int[alphabet.Length + 2];
            rangeUpper = new int[alphabet.Length + 2];
            // calculate the unicode range intervals that exclude the alphabet
            // these are the ranges for all unicode characters not in the alphabet
            int lower = 0;
            for (int i = 0; i < alphabet.Length; i++)
            {
                int higher = alphabet[i];
                if (higher > lower)
                {
                    rangeLower[numRanges] = lower;
                    rangeUpper[numRanges] = higher - 1;
                    numRanges++;
                }
                lower = higher + 1;
            }
            /* add the readonly endpoint */
            if (lower <= alphaMax)
            {
                rangeLower[numRanges] = lower;
                rangeUpper[numRanges] = alphaMax;
                numRanges++;
            }

            descriptions = new ParametricDescription[] {
                null, /* for n=0, we do not need to go through the trouble */
                withTranspositions ? (ParametricDescription)new Lev1TParametricDescription(word.Length) : new Lev1ParametricDescription(word.Length),
                withTranspositions ? (ParametricDescription)new Lev2TParametricDescription(word.Length) : new Lev2ParametricDescription(word.Length),
            };
        }

        private static int[] CodePoints(String input)
        {
            int length = input.Length;
            int[] word = new int[length];
            for (int i = 0, j = 0, cp = 0; i < input.Length; i += 1)
            {
                word[j++] = cp = input[i];
            }
            return word;
        }

        public Automaton ToAutomaton(int n)
        {
            if (n == 0)
            {
                return BasicAutomata.MakeString(word, 0, word.Length);
            }

            if (n >= descriptions.Length)
                return null;

            int range = 2 * n + 1;
            ParametricDescription description = descriptions[n];
            // the number of states is based on the length of the word and n
            State[] states = new State[description.Size];
            // create all states, and mark as accept states if appropriate
            for (int i = 0; i < states.Length; i++)
            {
                states[i] = new State();
                states[i].number = i;
                states[i].Accept = description.IsAccept(i);
            }
            // create transitions from state to state
            for (int k = 0; k < states.Length; k++)
            {
                int xpos = description.GetPosition(k);
                if (xpos < 0)
                    continue;
                int end = xpos + Math.Min(word.Length - xpos, range);

                for (int x = 0; x < alphabet.Length; x++)
                {
                    int ch = alphabet[x];
                    // get the characteristic vector at this position wrt ch
                    int cvec = GetVector(ch, xpos, end);
                    int dest = description.Transition(k, xpos, cvec);
                    if (dest >= 0)
                        states[k].AddTransition(new Transition(ch, states[dest]));
                }
                // add transitions for all other chars in unicode
                // by definition, their characteristic vectors are always 0,
                // because they do not exist in the input string.
                int dest2 = description.Transition(k, xpos, 0); // by definition
                if (dest2 >= 0)
                    for (int r = 0; r < numRanges; r++)
                        states[k].AddTransition(new Transition(rangeLower[r], rangeUpper[r], states[dest2]));
            }

            Automaton a = new Automaton(states[0]);
            a.Deterministic = true;
            // we create some useless unconnected states, and its a net-win overall to remove these,
            // as well as to combine any adjacent transitions (it makes later algorithms more efficient).
            // so, while we could set our numberedStates here, its actually best not to, and instead to
            // force a traversal in reduce, pruning the unconnected states while we combine adjacent transitions.
            //a.setNumberedStates(states);
            a.Reduce();
            // we need not trim transitions to dead states, as they are not created.
            //a.restoreInvariant();
            return a;
        }

        int GetVector(int x, int pos, int end)
        {
            int vector = 0;
            for (int i = pos; i < end; i++)
            {
                vector <<= 1;
                if (word[i] == x)
                    vector |= 1;
            }
            return vector;
        }

        internal abstract class ParametricDescription
        {
            protected readonly int w;
            protected readonly int n;
            private readonly int[] minErrors;

            public ParametricDescription(int w, int n, int[] minErrors)
            {
                this.w = w;
                this.n = n;
                this.minErrors = minErrors;
            }

            /**
             * Return the number of states needed to compute a Levenshtein DFA
             */
            public int Size
            {
                get { return minErrors.Length * (w + 1); }
            }

            /**
             * Returns true if the <code>state</code> in any Levenshtein DFA is an accept state (final state).
             */
            public bool IsAccept(int absState)
            {
                // decode absState -> state, offset
                int state = absState / (w + 1);
                int offset = absState % (w + 1);
                //assert offset >= 0;
                return w - offset + minErrors[state] <= n;
            }

            /**
             * Returns the position in the input word for a given <code>state</code>.
             * This is the minimal boundary for the state.
             */
            public int GetPosition(int absState)
            {
                return absState % (w + 1);
            }

            /**
             * Returns the state number for a transition from the given <code>state</code>,
             * assuming <code>position</code> and characteristic vector <code>vector</code>
             */
            public abstract int Transition(int state, int position, int vector);

            private readonly static long[] MASKS = new long[] {0x1,0x3,0x7,0xf,
                                                    0x1f,0x3f,0x7f,0xff,
                                                    0x1ff,0x3ff,0x7ff,0xfff,
                                                    0x1fff,0x3fff,0x7fff,0xffff,
                                                    0x1ffff,0x3ffff,0x7ffff,0xfffff,
                                                    0x1fffff,0x3fffff,0x7fffff,0xffffff,
                                                    0x1ffffff,0x3ffffff,0x7ffffff,0xfffffff,
                                                    0x1fffffff,0x3fffffff,0x7fffffffL,0xffffffffL,
                                                    0x1ffffffffL,0x3ffffffffL,0x7ffffffffL,0xfffffffffL,
                                                    0x1fffffffffL,0x3fffffffffL,0x7fffffffffL,0xffffffffffL,
                                                    0x1ffffffffffL,0x3ffffffffffL,0x7ffffffffffL,0xfffffffffffL,
                                                    0x1fffffffffffL,0x3fffffffffffL,0x7fffffffffffL,0xffffffffffffL,
                                                    0x1ffffffffffffL,0x3ffffffffffffL,0x7ffffffffffffL,0xfffffffffffffL,
                                                    0x1fffffffffffffL,0x3fffffffffffffL,0x7fffffffffffffL,0xffffffffffffffL,
                                                    0x1ffffffffffffffL,0x3ffffffffffffffL,0x7ffffffffffffffL,0xfffffffffffffffL,
                                                    0x1fffffffffffffffL,0x3fffffffffffffffL,0x7fffffffffffffffL};

            protected int Unpack(long[] data, int index, int bitsPerValue)
            {
                long bitLoc = bitsPerValue * index;
                int dataLoc = (int)(bitLoc >> 6);
                int bitStart = (int)(bitLoc & 63);
                //System.out.println("index=" + index + " dataLoc=" + dataLoc + " bitStart=" + bitStart + " bitsPerV=" + bitsPerValue);
                if (bitStart + bitsPerValue <= 64)
                {
                    // not split
                    return (int)((data[dataLoc] >> bitStart) & MASKS[bitsPerValue - 1]);
                }
                else
                {
                    // split
                    int part = 64 - bitStart;
                    return (int)(((data[dataLoc] >> bitStart) & MASKS[part - 1]) +
                                  ((data[1 + dataLoc] & MASKS[bitsPerValue - part - 1]) << part));
                }
            }
        }
    }
}
