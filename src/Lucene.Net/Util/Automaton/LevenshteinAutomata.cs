using J2N;
using Lucene.Net.Diagnostics;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using JCG = J2N.Collections.Generic;

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
    /// <para/>
    /// Implements the algorithm described in:
    /// Schulz and Mihov: Fast String Correction with Levenshtein Automata
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public class LevenshteinAutomata
    {
        /// <summary>
        /// @lucene.internal </summary>
        public const int MAXIMUM_SUPPORTED_DISTANCE = 2;

        /* input word */
        internal readonly int[] word;
        /* the automata alphabet. */
        internal readonly int[] alphabet;
        /* the maximum symbol in the alphabet (e.g. 255 for UTF-8 or 10FFFF for UTF-32) */
        internal readonly int alphaMax;

        /* the ranges outside of alphabet */
        internal readonly int[] rangeLower;
        internal readonly int[] rangeUpper;
        internal int numRanges = 0;

        internal ParametricDescription[] descriptions;

        /// <summary>
        /// Create a new <see cref="LevenshteinAutomata"/> for some <paramref name="input"/> string.
        /// Optionally count transpositions as a primitive edit.
        /// </summary>
        public LevenshteinAutomata(string input, bool withTranspositions)
            : this(CodePoints(input), Character.MaxCodePoint, withTranspositions)
        {
        }

        /// <summary>
        /// Expert: specify a custom maximum possible symbol
        /// (alphaMax); default is <see cref="Character.MaxCodePoint"/>.
        /// </summary>
        public LevenshteinAutomata(int[] word, int alphaMax, bool withTranspositions)
        {
            this.word = word;
            this.alphaMax = alphaMax;

            // calculate the alphabet
            ISet<int> set = new JCG.SortedSet<int>();
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
            using (IEnumerator<int> iterator = set.GetEnumerator())
            {
                for (int i = 0; i < alphabet.Length; i++)
                {
                    iterator.MoveNext();
                    alphabet[i] = iterator.Current;
                }
            }

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
            /* add the final endpoint */
            if (lower <= alphaMax)
            {
                rangeLower[numRanges] = lower;
                rangeUpper[numRanges] = alphaMax;
                numRanges++;
            }

            descriptions = new ParametricDescription[] {
                null,
                withTranspositions ? (ParametricDescription)new Lev1TParametricDescription(word.Length) : new Lev1ParametricDescription(word.Length),
                withTranspositions ? (ParametricDescription)new Lev2TParametricDescription(word.Length) : new Lev2ParametricDescription(word.Length)
            };
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int[] CodePoints(string input)
        {
            int length = Character.CodePointCount(input, 0, input.Length);
            int[] word = new int[length];
            int cp; // LUCENENET: Removed unnecessary assignment
            for (int i = 0, j = 0; i < input.Length; i += Character.CharCount(cp))
            {
                word[j++] = cp = Character.CodePointAt(input, i);
            }
            return word;
        }

        /// <summary>
        /// Compute a DFA that accepts all strings within an edit distance of <paramref name="n"/>.
        /// <para>
        /// All automata have the following properties:
        /// <list type="bullet">
        ///     <item><description>They are deterministic (DFA).</description></item>
        ///     <item><description>There are no transitions to dead states.</description></item>
        ///     <item><description>They are not minimal (some transitions could be combined).</description></item>
        /// </list>
        /// </para>
        /// </summary>
        public virtual Automaton ToAutomaton(int n)
        {
            if (n == 0)
            {
                return BasicAutomata.MakeString(word, 0, word.Length);
            }

            if (n >= descriptions.Length)
            {
                return null;
            }

            int range = 2 * n + 1;
            ParametricDescription description = descriptions[n];
            // the number of states is based on the length of the word and n
            State[] states = new State[description.Count];
            // create all states, and mark as accept states if appropriate
            for (int i = 0; i < states.Length; i++)
            {
                states[i] = new State
                {
                    number = i,
                    accept = description.IsAccept(i)
                };
            }
            // create transitions from state to state
            for (int k = 0; k < states.Length; k++)
            {
                int xpos = description.GetPosition(k);
                if (xpos < 0)
                {
                    continue;
                }
                int end = xpos + Math.Min(word.Length - xpos, range);

                for (int x = 0; x < alphabet.Length; x++)
                {
                    int ch = alphabet[x];
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
                    for (int r = 0; r < numRanges; r++)
                    {
                        states[k].AddTransition(new Transition(rangeLower[r], rangeUpper[r], states[dest_]));
                    }
                }
            }

            Automaton a = new Automaton(states[0])
            {
                deterministic = true
            };
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
        /// Get the characteristic vector <c>X(x, V)</c>
        /// where V is <c>Substring(pos, end - pos)</c>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual int GetVector(int x, int pos, int end)
        {
            int vector = 0;
            for (int i = pos; i < end; i++)
            {
                vector <<= 1;
                if (word[i] == x)
                {
                    vector |= 1;
                }
            }
            return vector;
        }

        /// <summary>
        /// A <see cref="ParametricDescription"/> describes the structure of a Levenshtein DFA for some degree <c>n</c>.
        /// <para/>
        /// There are four components of a parametric description, all parameterized on the length
        /// of the word <c>w</c>:
        /// <list type="number">
        ///     <item><description>The number of states: <see cref="Count"/></description></item>
        ///     <item><description>The set of final states: <see cref="IsAccept(int)"/></description></item>
        ///     <item><description>The transition function: <see cref="Transition(int, int, int)"/></description></item>
        ///     <item><description>Minimal boundary function: <see cref="GetPosition(int)"/></description></item>
        /// </list>
        /// </summary>
        internal abstract class ParametricDescription
        {
            protected readonly int m_w;
            protected readonly int m_n;
            private readonly int[] minErrors;

            private protected ParametricDescription(int w, int n, int[] minErrors) // LUCENENET: Changed from internal to private protected
            {
                this.m_w = w;
                this.m_n = n;
                this.minErrors = minErrors;
            }

            /// <summary>
            /// Return the number of states needed to compute a Levenshtein DFA.
            /// <para/>
            /// NOTE: This was size() in Lucene.
            /// </summary>
            internal virtual int Count => minErrors.Length * (m_w + 1);

            /// <summary>
            /// Returns <c>true</c> if the <c>state</c> in any Levenshtein DFA is an accept state (final state).
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal virtual bool IsAccept(int absState)
            {
                // decode absState -> state, offset
                int state = absState / (m_w + 1);
                int offset = absState % (m_w + 1);
                if (Debugging.AssertsEnabled) Debugging.Assert(offset >= 0);
                return m_w - offset + minErrors[state] <= m_n;
            }

            /// <summary>
            /// Returns the position in the input word for a given <c>state</c>.
            /// this is the minimal boundary for the state.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal virtual int GetPosition(int absState)
            {
                return absState % (m_w + 1);
            }

            /// <summary>
            /// Returns the state number for a transition from the given <paramref name="state"/>,
            /// assuming <paramref name="position"/> and characteristic vector <paramref name="vector"/>.
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