using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Lucene.Net.Index
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

    using ByteRunAutomaton = Lucene.Net.Util.Automaton.ByteRunAutomaton;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using CompiledAutomaton = Lucene.Net.Util.Automaton.CompiledAutomaton;
    using Int32sRef = Lucene.Net.Util.Int32sRef;
    using StringHelper = Lucene.Net.Util.StringHelper;
    using Transition = Lucene.Net.Util.Automaton.Transition;

    /// <summary>
    /// A <see cref="FilteredTermsEnum"/> that enumerates terms based upon what is accepted by a
    /// DFA.
    /// <para/>
    /// The algorithm is such:
    /// <list type="number">
    ///     <item><description>As long as matches are successful, keep reading sequentially.</description></item>
    ///     <item><description>When a match fails, skip to the next string in lexicographic order that
    ///         does not enter a reject state.</description></item>
    /// </list>
    /// <para>
    /// The algorithm does not attempt to actually skip to the next string that is
    /// completely accepted. this is not possible when the language accepted by the
    /// FSM is not finite (i.e. * operator).
    /// </para>
    /// @lucene.experimental
    /// </summary>
    internal class AutomatonTermsEnum : FilteredTermsEnum
    {
        // a tableized array-based form of the DFA
        private readonly ByteRunAutomaton runAutomaton;

        // common suffix of the automaton
        private readonly BytesRef commonSuffixRef;

        // true if the automaton accepts a finite language
        private readonly bool? finite;

        // array of sorted transitions for each state, indexed by state number
        private readonly Transition[][] allTransitions;

        // for path tracking: each long records gen when we last
        // visited the state; we use gens to avoid having to clear
        private readonly long[] visited;

        private long curGen;

        // the reference used for seeking forwards through the term dictionary
        private readonly BytesRef seekBytesRef = new BytesRef(10);

        // true if we are enumerating an infinite portion of the DFA.
        // in this case it is faster to drive the query based on the terms dictionary.
        // when this is true, linearUpperBound indicate the end of range
        // of terms where we should simply do sequential reads instead.
        private bool linear = false;

        private readonly BytesRef linearUpperBound = new BytesRef(10);
        private readonly IComparer<BytesRef> termComp;

        /// <summary>
        /// Construct an enumerator based upon an automaton, enumerating the specified
        /// field, working on a supplied <see cref="TermsEnum"/>
        /// <para/>
        /// @lucene.experimental
        /// </summary>
        /// <param name="tenum"> TermsEnum </param>
        /// <param name="compiled"> CompiledAutomaton </param>
        public AutomatonTermsEnum(TermsEnum tenum, CompiledAutomaton compiled)
            : base(tenum)
        {
            this.finite = compiled.Finite;
            this.runAutomaton = compiled.RunAutomaton;
            if (Debugging.AssertsEnabled) Debugging.Assert(this.runAutomaton != null);
            this.commonSuffixRef = compiled.CommonSuffixRef;
            this.allTransitions = compiled.SortedTransitions;

            // used for path tracking, where each bit is a numbered state.
            visited = new long[runAutomaton.Count];

            termComp = Comparer;
        }

        /// <summary>
        /// Returns <c>true</c> if the term matches the automaton. Also stashes away the term
        /// to assist with smart enumeration.
        /// </summary>
        protected override AcceptStatus Accept(BytesRef term)
        {
            if (commonSuffixRef is null || StringHelper.EndsWith(term, commonSuffixRef))
            {
                if (runAutomaton.Run(term.Bytes, term.Offset, term.Length))
                {
                    return linear ? AcceptStatus.YES : AcceptStatus.YES_AND_SEEK;
                }
                else
                {
                    return (linear && termComp.Compare(term, linearUpperBound) < 0) ? AcceptStatus.NO : AcceptStatus.NO_AND_SEEK;
                }
            }
            else
            {
                return (linear && termComp.Compare(term, linearUpperBound) < 0) ? AcceptStatus.NO : AcceptStatus.NO_AND_SEEK;
            }
        }

        protected override BytesRef NextSeekTerm(BytesRef term)
        {
            //System.out.println("ATE.nextSeekTerm term=" + term);
            if (term is null)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(seekBytesRef.Length == 0);
                // return the empty term, as its valid
                if (runAutomaton.IsAccept(runAutomaton.InitialState))
                {
                    return seekBytesRef;
                }
            }
            else
            {
                seekBytesRef.CopyBytes(term);
            }

            // seek to the next possible string;
            if (NextString())
            {
                return seekBytesRef; // reposition
            }
            else
            {
                return null; // no more possible strings can match
            }
        }

        /// <summary>
        /// Sets the enum to operate in linear fashion, as we have found
        /// a looping transition at position: we set an upper bound and
        /// act like a <see cref="Search.TermRangeQuery"/> for this portion of the term space.
        /// </summary>
        private void SetLinear(int position)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(linear == false);

            int state = runAutomaton.InitialState;
            int maxInterval = 0xff;
            for (int i = 0; i < position; i++)
            {
                state = runAutomaton.Step(state, seekBytesRef.Bytes[i] & 0xff);
                if (Debugging.AssertsEnabled) Debugging.Assert(state >= 0,"state={0}", state);
            }
            for (int i = 0; i < allTransitions[state].Length; i++)
            {
                Transition t = allTransitions[state][i];
                if (t.Min <= (seekBytesRef.Bytes[position] & 0xff) && (seekBytesRef.Bytes[position] & 0xff) <= t.Max)
                {
                    maxInterval = t.Max;
                    break;
                }
            }
            // 0xff terms don't get the optimization... not worth the trouble.
            if (maxInterval != 0xff)
            {
                maxInterval++;
            }
            int length = position + 1; // value + maxTransition
            if (linearUpperBound.Bytes.Length < length)
            {
                linearUpperBound.Bytes = new byte[length];
            }
            Arrays.Copy(seekBytesRef.Bytes, 0, linearUpperBound.Bytes, 0, position);
            linearUpperBound.Bytes[position] = (byte)maxInterval;
            linearUpperBound.Length = length;

            linear = true;
        }

        private readonly Int32sRef savedStates = new Int32sRef(10);

        /// <summary>
        /// Increments the byte buffer to the next string in binary order after s that will not put
        /// the machine into a reject state. If such a string does not exist, returns
        /// <c>false</c>.
        /// <para/>
        /// The correctness of this method depends upon the automaton being deterministic,
        /// and having no transitions to dead states.
        /// </summary>
        /// <returns> <c>true</c> if more possible solutions exist for the DFA </returns>
        private bool NextString()
        {
            int state;
            int pos = 0;
            savedStates.Grow(seekBytesRef.Length + 1);
            int[] states = savedStates.Int32s;
            states[0] = runAutomaton.InitialState;

            while (true)
            {
                curGen++;
                linear = false;
                // walk the automaton until a character is rejected.
                for (state = states[pos]; pos < seekBytesRef.Length; pos++)
                {
                    visited[state] = curGen;
                    int nextState = runAutomaton.Step(state, seekBytesRef.Bytes[pos] & 0xff);
                    if (nextState == -1)
                    {
                        break;
                    }
                    states[pos + 1] = nextState;
                    // we found a loop, record it for faster enumeration
                    if ((finite == false) && !linear && visited[nextState] == curGen)
                    {
                        SetLinear(pos);
                    }
                    state = nextState;
                }

                // take the useful portion, and the last non-reject state, and attempt to
                // append characters that will match.
                if (NextString(state, pos))
                {
                    return true;
                } // no more solutions exist from this useful portion, backtrack
                else
                {
                    if ((pos = Backtrack(pos)) < 0) // no more solutions at all
                    {
                        return false;
                    }
                    int newState = runAutomaton.Step(states[pos], seekBytesRef.Bytes[pos] & 0xff);
                    if (newState >= 0 && runAutomaton.IsAccept(newState))
                    /* String is good to go as-is */
                    {
                        return true;
                    }
                    /* else advance further */
                    // TODO: paranoia? if we backtrack thru an infinite DFA, the loop detection is important!
                    // for now, restart from scratch for all infinite DFAs
                    if (finite == false)
                    {
                        pos = 0;
                    }
                }
            }
        }

        /// <summary>
        /// Returns the next string in lexicographic order that will not put
        /// the machine into a reject state.
        /// <para/>
        /// This method traverses the DFA from the given position in the string,
        /// starting at the given state.
        /// <para/>
        /// If this cannot satisfy the machine, returns <c>false</c>. This method will
        /// walk the minimal path, in lexicographic order, as long as possible.
        /// <para/>
        /// If this method returns <c>false</c>, then there might still be more solutions,
        /// it is necessary to backtrack to find out.
        /// </summary>
        /// <param name="state"> current non-reject state </param>
        /// <param name="position"> useful portion of the string </param>
        /// <returns> <c>true</c> if more possible solutions exist for the DFA from this
        ///         position </returns>
        private bool NextString(int state, int position)
        {
            /*
             * the next lexicographic character must be greater than the existing
             * character, if it exists.
             */
            int c = 0;
            if (position < seekBytesRef.Length)
            {
                c = seekBytesRef.Bytes[position] & 0xff;
                // if the next byte is 0xff and is not part of the useful portion,
                // then by definition it puts us in a reject state, and therefore this
                // path is dead. there cannot be any higher transitions. backtrack.
                if (c++ == 0xff)
                {
                    return false;
                }
            }

            seekBytesRef.Length = position;
            visited[state] = curGen;

            Transition[] transitions = allTransitions[state];

            // find the minimal path (lexicographic order) that is >= c

            for (int i = 0; i < transitions.Length; i++)
            {
                Transition transition = transitions[i];
                if (transition.Max >= c)
                {
                    int nextChar = Math.Max(c, transition.Min);
                    // append either the next sequential char, or the minimum transition
                    seekBytesRef.Grow(seekBytesRef.Length + 1);
                    seekBytesRef.Length++;
                    seekBytesRef.Bytes[seekBytesRef.Length - 1] = (byte)nextChar;
                    state = transition.Dest.Number;
                    /*
                     * as long as is possible, continue down the minimal path in
                     * lexicographic order. if a loop or accept state is encountered, stop.
                     */
                    while (visited[state] != curGen && !runAutomaton.IsAccept(state))
                    {
                        visited[state] = curGen;
                        /*
                         * Note: we work with a DFA with no transitions to dead states.
                         * so the below is ok, if it is not an accept state,
                         * then there MUST be at least one transition.
                         */
                        transition = allTransitions[state][0];
                        state = transition.Dest.Number;

                        // append the minimum transition
                        seekBytesRef.Grow(seekBytesRef.Length + 1);
                        seekBytesRef.Length++;
                        seekBytesRef.Bytes[seekBytesRef.Length - 1] = (byte)transition.Min;

                        // we found a loop, record it for faster enumeration
                        if ((finite == false) && !linear && visited[state] == curGen)
                        {
                            SetLinear(seekBytesRef.Length - 1);
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Attempts to backtrack thru the string after encountering a dead end
        /// at some given position. Returns <c>false</c> if no more possible strings
        /// can match.
        /// </summary>
        /// <param name="position"> current position in the input string </param>
        /// <returns> position &gt;=0 if more possible solutions exist for the DFA </returns>
        private int Backtrack(int position)
        {
            while (position-- > 0)
            {
                int nextChar = seekBytesRef.Bytes[position] & 0xff;
                // if a character is 0xff its a dead-end too,
                // because there is no higher character in binary sort order.
                if (nextChar++ != 0xff)
                {
                    seekBytesRef.Bytes[position] = (byte)nextChar;
                    seekBytesRef.Length = position + 1;
                    return position;
                }
            }
            return -1; // all solutions exhausted
        }
    }
}