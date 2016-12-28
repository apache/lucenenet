using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Lucene.Net.Util.Automaton
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

    /// <summary>
    /// Class to construct DFAs that match a word within some edit distance.
    /// <p>
    /// Implements the algorithm described in:
    /// Schulz and Mihov: Fast String Correction with Levenshtein Automata
    /// <p>
    /// @lucene.experimental
    /// </summary>
    public class LevenshteinAutomata
    {
        /// <summary>
        /// @lucene.internal </summary>
        public const int MAXIMUM_SUPPORTED_DISTANCE = 2;

        /* input word */
        internal readonly int[] Word;
        /* the automata alphabet. */
        internal readonly int[] Alphabet;
        /* the maximum symbol in the alphabet (e.g. 255 for UTF-8 or 10FFFF for UTF-32) */
        internal readonly int AlphaMax;

        /* the ranges outside of alphabet */
        internal readonly int[] RangeLower;
        internal readonly int[] RangeUpper;
        internal int NumRanges = 0;

        internal ParametricDescription[] Descriptions;

        /// <summary>
        /// Create a new LevenshteinAutomata for some input String.
        /// Optionally count transpositions as a primitive edit.
        /// </summary>
        public LevenshteinAutomata(string input, bool withTranspositions)
            : this(CodePoints(input), Character.MAX_CODE_POINT, withTranspositions)
        {
        }

        /// <summary>
        /// Expert: specify a custom maximum possible symbol
        /// (alphaMax); default is Character.MAX_CODE_POINT.
        /// </summary>
        public LevenshteinAutomata(int[] word, int alphaMax, bool withTranspositions)
        {
            this.Word = word;
            this.AlphaMax = alphaMax;

            // calculate the alphabet
            SortedSet<int> set = new SortedSet<int>();
            for (int i = 0; i < word.Length; i++)
            {
                int v = word[i];
                if (v > alphaMax)
                {
                    throw new System.ArgumentException("alphaMax exceeded by symbol " + v + " in word");
                }
                set.Add(v);
            }
            Alphabet = new int[set.Count];
            IEnumerator<int> iterator = set.GetEnumerator();
            for (int i = 0; i < Alphabet.Length; i++)
            {
                iterator.MoveNext();
                Alphabet[i] = iterator.Current;
            }

            RangeLower = new int[Alphabet.Length + 2];
            RangeUpper = new int[Alphabet.Length + 2];
            // calculate the unicode range intervals that exclude the alphabet
            // these are the ranges for all unicode characters not in the alphabet
            int lower = 0;
            for (int i = 0; i < Alphabet.Length; i++)
            {
                int higher = Alphabet[i];
                if (higher > lower)
                {
                    RangeLower[NumRanges] = lower;
                    RangeUpper[NumRanges] = higher - 1;
                    NumRanges++;
                }
                lower = higher + 1;
            }
            /* add the final endpoint */
            if (lower <= alphaMax)
            {
                RangeLower[NumRanges] = lower;
                RangeUpper[NumRanges] = alphaMax;
                NumRanges++;
            }

            Descriptions = new ParametricDescription[] {
            null,
            withTranspositions ? (ParametricDescription)new Lev1TParametricDescription(word.Length) : new Lev1ParametricDescription(word.Length),
            withTranspositions ? (ParametricDescription)new Lev2TParametricDescription(word.Length) : new Lev2ParametricDescription(word.Length)
        };
        }

        private static int[] CodePoints(string input)
        {
            //int length = char.codePointCount(input, 0, input.Length);
            int length = input.Length;
            int[] word = new int[length];
            for (int i = 0, j = 0, cp = 0; i < input.Length; i += Character.CharCount(cp))
            {
                word[j++] = cp = Character.CodePointAt(input, i);
            }
            return word;
        }

        /// <summary>
        /// Compute a DFA that accepts all strings within an edit distance of <code>n</code>.
        /// <p>
        /// All automata have the following properties:
        /// <ul>
        /// <li>They are deterministic (DFA).
        /// <li>There are no transitions to dead states.
        /// <li>They are not minimal (some transitions could be combined).
        /// </ul>
        /// </p>
        /// </summary>
        public virtual Automaton ToAutomaton(int n)
        {
            if (n == 0)
            {
                return BasicAutomata.MakeString(Word, 0, Word.Length);
            }

            if (n >= Descriptions.Length)
            {
                return null;
            }

            int range = 2 * n + 1;
            ParametricDescription description = Descriptions[n];
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
                {
                    continue;
                }
                int end = xpos + Math.Min(Word.Length - xpos, range);

                for (int x = 0; x < Alphabet.Length; x++)
                {
                    int ch = Alphabet[x];
                    // get the characteristic vector at this position wrt ch
                    int cvec = GetVector(ch, xpos, end);
                    int dest = description.Transition(k, xpos, cvec);
                    if (dest >= 0)
                    {
                        states[k].AddTransition(new Transition(ch, states[dest]));
                    }
                }
                // add transitions for all other chars in unicode
                // by definition, their characteristic vectors are always 0,
                // because they do not exist in the input string.
                int dest_ = description.Transition(k, xpos, 0); // by definition
                if (dest_ >= 0)
                {
                    for (int r = 0; r < NumRanges; r++)
                    {
                        states[k].AddTransition(new Transition(RangeLower[r], RangeUpper[r], states[dest_]));
                    }
                }
            }

            Automaton a = new Automaton(states[0]);
            a.IsDeterministic = true;
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

        /// <summary>
        /// Get the characteristic vector <code>X(x, V)</code>
        /// where V is <code>substring(pos, end)</code>
        /// </summary>
        internal virtual int GetVector(int x, int pos, int end)
        {
            int vector = 0;
            for (int i = pos; i < end; i++)
            {
                vector <<= 1;
                if (Word[i] == x)
                {
                    vector |= 1;
                }
            }
            return vector;
        }

        /// <summary>
        /// A ParametricDescription describes the structure of a Levenshtein DFA for some degree n.
        /// <p>
        /// There are four components of a parametric description, all parameterized on the length
        /// of the word <code>w</code>:
        /// <ol>
        /// <li>The number of states: <seealso cref="#size()"/>
        /// <li>The set of final states: <seealso cref="#isAccept(int)"/>
        /// <li>The transition function: <seealso cref="#transition(int, int, int)"/>
        /// <li>Minimal boundary function: <seealso cref="#getPosition(int)"/>
        /// </ol>
        /// </summary>
        internal abstract class ParametricDescription
        {
            protected internal readonly int w;
            protected internal readonly int n;
            private readonly int[] MinErrors;

            internal ParametricDescription(int w, int n, int[] minErrors)
            {
                this.w = w;
                this.n = n;
                this.MinErrors = minErrors;
            }

            /// <summary>
            /// Return the number of states needed to compute a Levenshtein DFA
            /// </summary>
            internal virtual int Size // LUCENENET TODO: rename Count
            {
                get { return MinErrors.Length * (w + 1); }
            }

            /// <summary>
            /// Returns true if the <code>state</code> in any Levenshtein DFA is an accept state (final state).
            /// </summary>
            internal virtual bool IsAccept(int absState)
            {
                // decode absState -> state, offset
                int state = absState / (w + 1);
                int offset = absState % (w + 1);
                Debug.Assert(offset >= 0);
                return w - offset + MinErrors[state] <= n;
            }

            /// <summary>
            /// Returns the position in the input word for a given <code>state</code>.
            /// this is the minimal boundary for the state.
            /// </summary>
            internal virtual int GetPosition(int absState)
            {
                return absState % (w + 1);
            }

            /// <summary>
            /// Returns the state number for a transition from the given <code>state</code>,
            /// assuming <code>position</code> and characteristic vector <code>vector</code>
            /// </summary>
            internal abstract int Transition(int state, int position, int vector);

            private static readonly long[] MASKS = new long[] {
                0x1, 0x3, 0x7, 0xf,
                0x1f, 0x3f, 0x7f, 0xff,
                0x1ff, 0x3ff, 0x7ff, 0xfff,
                0x1fff, 0x3fff, 0x7fff, 0xffff,
                0x1ffff, 0x3ffff, 0x7ffff, 0xfffff,
                0x1fffff, 0x3fffff, 0x7fffff, 0xffffff,
                0x1ffffff, 0x3ffffff, 0x7ffffff, 0xfffffff,
                0x1fffffff, 0x3fffffff, 0x7fffffffL, 0xffffffffL,
                0x1ffffffffL, 0x3ffffffffL, 0x7ffffffffL, 0xfffffffffL,
                0x1fffffffffL, 0x3fffffffffL, 0x7fffffffffL, 0xffffffffffL,
                0x1ffffffffffL, 0x3ffffffffffL, 0x7ffffffffffL, 0xfffffffffffL,
                0x1fffffffffffL, 0x3fffffffffffL, 0x7fffffffffffL, 0xffffffffffffL,
                0x1ffffffffffffL, 0x3ffffffffffffL, 0x7ffffffffffffL, 0xfffffffffffffL,
                0x1fffffffffffffL, 0x3fffffffffffffL, 0x7fffffffffffffL, 0xffffffffffffffL,
                0x1ffffffffffffffL, 0x3ffffffffffffffL, 0x7ffffffffffffffL, 0xfffffffffffffffL,
                0x1fffffffffffffffL, 0x3fffffffffffffffL, 0x7fffffffffffffffL
            };

            protected internal virtual int Unpack(long[] data, int index, int bitsPerValue)
            {
                long bitLoc = bitsPerValue * index;
                int dataLoc = (int)(bitLoc >> 6);
                int bitStart = (int)(bitLoc & 63);
                if (bitStart + bitsPerValue <= 64)
                {
                    // not split
                    return (int)((data[dataLoc] >> bitStart) & MASKS[bitsPerValue - 1]);
                }
                else
                {
                    // split
                    int part = 64 - bitStart;
                    return (int)(((data[dataLoc] >> bitStart) & MASKS[part - 1]) + ((data[1 + dataLoc] & MASKS[bitsPerValue - part - 1]) << part));
                }
            }
        }
    }
}