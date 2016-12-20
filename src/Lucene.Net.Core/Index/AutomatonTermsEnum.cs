using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Lucene.Net.Index
{
    using ByteRunAutomaton = Lucene.Net.Util.Automaton.ByteRunAutomaton;

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

    using BytesRef = Lucene.Net.Util.BytesRef;
    using CompiledAutomaton = Lucene.Net.Util.Automaton.CompiledAutomaton;
    using IntsRef = Lucene.Net.Util.IntsRef;
    using StringHelper = Lucene.Net.Util.StringHelper;
    using Transition = Lucene.Net.Util.Automaton.Transition;

    /// <summary>
    /// A FilteredTermsEnum that enumerates terms based upon what is accepted by a
    /// DFA.
    /// <p>
    /// The algorithm is such:
    /// <ol>
    ///   <li>As long as matches are successful, keep reading sequentially.
    ///   <li>When a match fails, skip to the next string in lexicographic order that
    /// does not enter a reject state.
    /// </ol>
    /// <p>
    /// The algorithm does not attempt to actually skip to the next string that is
    /// completely accepted. this is not possible when the language accepted by the
    /// FSM is not finite (i.e. * operator).
    /// </p>
    /// @lucene.experimental
    /// </summary>
    internal class AutomatonTermsEnum : FilteredTermsEnum
    {
        // a tableized array-based form of the DFA
        private readonly ByteRunAutomaton RunAutomaton;

        // common suffix of the automaton
        private readonly BytesRef CommonSuffixRef;

        // true if the automaton accepts a finite language
        private readonly bool? Finite;

        // array of sorted transitions for each state, indexed by state number
        private readonly Transition[][] AllTransitions;

        // for path tracking: each long records gen when we last
        // visited the state; we use gens to avoid having to clear
        private readonly long[] Visited;

        private long CurGen;

        // the reference used for seeking forwards through the term dictionary
        private readonly BytesRef SeekBytesRef = new BytesRef(10);

        // true if we are enumerating an infinite portion of the DFA.
        // in this case it is faster to drive the query based on the terms dictionary.
        // when this is true, linearUpperBound indicate the end of range
        // of terms where we should simply do sequential reads instead.
        private bool Linear_Renamed = false;

        private readonly BytesRef LinearUpperBound = new BytesRef(10);
        private readonly IComparer<BytesRef> TermComp;

        /// <summary>
        /// Construct an enumerator based upon an automaton, enumerating the specified
        /// field, working on a supplied TermsEnum
        /// <p>
        /// @lucene.experimental
        /// <p> </summary>
        /// <param name="compiled"> CompiledAutomaton </param>
        public AutomatonTermsEnum(TermsEnum tenum, CompiledAutomaton compiled)
            : base(tenum)
        {
            this.Finite = compiled.Finite;
            this.RunAutomaton = compiled.RunAutomaton;
            Debug.Assert(this.RunAutomaton != null);
            this.CommonSuffixRef = compiled.CommonSuffixRef;
            this.AllTransitions = compiled.SortedTransitions;

            // used for path tracking, where each bit is a numbered state.
            Visited = new long[RunAutomaton.Size];

            TermComp = Comparator;
        }

        /// <summary>
        /// Returns true if the term matches the automaton. Also stashes away the term
        /// to assist with smart enumeration.
        /// </summary>
        protected internal override AcceptStatus Accept(BytesRef term)
        {
            if (CommonSuffixRef == null || StringHelper.EndsWith(term, CommonSuffixRef))
            {
                if (RunAutomaton.Run(term.Bytes, term.Offset, term.Length))
                {
                    return Linear_Renamed ? AcceptStatus.YES : AcceptStatus.YES_AND_SEEK;
                }
                else
                {
                    return (Linear_Renamed && TermComp.Compare(term, LinearUpperBound) < 0) ? AcceptStatus.NO : AcceptStatus.NO_AND_SEEK;
                }
            }
            else
            {
                return (Linear_Renamed && TermComp.Compare(term, LinearUpperBound) < 0) ? AcceptStatus.NO : AcceptStatus.NO_AND_SEEK;
            }
        }

        protected internal override BytesRef NextSeekTerm(BytesRef term)
        {
            //System.out.println("ATE.nextSeekTerm term=" + term);
            if (term == null)
            {
                Debug.Assert(SeekBytesRef.Length == 0);
                // return the empty term, as its valid
                if (RunAutomaton.IsAccept(RunAutomaton.InitialState))
                {
                    return SeekBytesRef;
                }
            }
            else
            {
                SeekBytesRef.CopyBytes(term);
            }

            // seek to the next possible string;
            if (NextString())
            {
                return SeekBytesRef; // reposition
            }
            else
            {
                return null; // no more possible strings can match
            }
        }

        /// <summary>
        /// Sets the enum to operate in linear fashion, as we have found
        /// a looping transition at position: we set an upper bound and
        /// act like a TermRangeQuery for this portion of the term space.
        /// </summary>
        private int Linear // LUCENENET TODO: Change to SetLinear(int)
        {
            set
            {
                Debug.Assert(Linear_Renamed == false);

                int state = RunAutomaton.InitialState;
                int maxInterval = 0xff;
                for (int i = 0; i < value; i++)
                {
                    state = RunAutomaton.Step(state, SeekBytesRef.Bytes[i] & 0xff);
                    Debug.Assert(state >= 0, "state=" + state);
                }
                for (int i = 0; i < AllTransitions[state].Length; i++)
                {
                    Transition t = AllTransitions[state][i];
                    if (t.Min <= (SeekBytesRef.Bytes[value] & 0xff) && (SeekBytesRef.Bytes[value] & 0xff) <= t.Max)
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
                int length = value + 1; // value + maxTransition
                if (LinearUpperBound.Bytes.Length < length)
                {
                    LinearUpperBound.Bytes = new byte[length];
                }
                Array.Copy(SeekBytesRef.Bytes, 0, LinearUpperBound.Bytes, 0, value);
                LinearUpperBound.Bytes[value] = (byte)maxInterval;
                LinearUpperBound.Length = length;

                Linear_Renamed = true;
            }
        }

        private readonly IntsRef SavedStates = new IntsRef(10);

        /// <summary>
        /// Increments the byte buffer to the next String in binary order after s that will not put
        /// the machine into a reject state. If such a string does not exist, returns
        /// false.
        ///
        /// The correctness of this method depends upon the automaton being deterministic,
        /// and having no transitions to dead states.
        /// </summary>
        /// <returns> true if more possible solutions exist for the DFA </returns>
        private bool NextString()
        {
            int state;
            int pos = 0;
            SavedStates.Grow(SeekBytesRef.Length + 1);
            int[] states = SavedStates.Ints;
            states[0] = RunAutomaton.InitialState;

            while (true)
            {
                CurGen++;
                Linear_Renamed = false;
                // walk the automaton until a character is rejected.
                for (state = states[pos]; pos < SeekBytesRef.Length; pos++)
                {
                    Visited[state] = CurGen;
                    int nextState = RunAutomaton.Step(state, SeekBytesRef.Bytes[pos] & 0xff);
                    if (nextState == -1)
                    {
                        break;
                    }
                    states[pos + 1] = nextState;
                    // we found a loop, record it for faster enumeration
                    if ((Finite == false) && !Linear_Renamed && Visited[nextState] == CurGen)
                    {
                        Linear = pos;
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
                    int newState = RunAutomaton.Step(states[pos], SeekBytesRef.Bytes[pos] & 0xff);
                    if (newState >= 0 && RunAutomaton.IsAccept(newState))
                    /* String is good to go as-is */
                    {
                        return true;
                    }
                    /* else advance further */
                    // TODO: paranoia? if we backtrack thru an infinite DFA, the loop detection is important!
                    // for now, restart from scratch for all infinite DFAs
                    if (Finite == false)
                    {
                        pos = 0;
                    }
                }
            }
        }

        /// <summary>
        /// Returns the next String in lexicographic order that will not put
        /// the machine into a reject state.
        ///
        /// this method traverses the DFA from the given position in the String,
        /// starting at the given state.
        ///
        /// If this cannot satisfy the machine, returns false. this method will
        /// walk the minimal path, in lexicographic order, as long as possible.
        ///
        /// If this method returns false, then there might still be more solutions,
        /// it is necessary to backtrack to find out.
        /// </summary>
        /// <param name="state"> current non-reject state </param>
        /// <param name="position"> useful portion of the string </param>
        /// <returns> true if more possible solutions exist for the DFA from this
        ///         position </returns>
        private bool NextString(int state, int position)
        {
            /*
             * the next lexicographic character must be greater than the existing
             * character, if it exists.
             */
            int c = 0;
            if (position < SeekBytesRef.Length)
            {
                c = SeekBytesRef.Bytes[position] & 0xff;
                // if the next byte is 0xff and is not part of the useful portion,
                // then by definition it puts us in a reject state, and therefore this
                // path is dead. there cannot be any higher transitions. backtrack.
                if (c++ == 0xff)
                {
                    return false;
                }
            }

            SeekBytesRef.Length = position;
            Visited[state] = CurGen;

            Transition[] transitions = AllTransitions[state];

            // find the minimal path (lexicographic order) that is >= c

            for (int i = 0; i < transitions.Length; i++)
            {
                Transition transition = transitions[i];
                if (transition.Max >= c)
                {
                    int nextChar = Math.Max(c, transition.Min);
                    // append either the next sequential char, or the minimum transition
                    SeekBytesRef.Grow(SeekBytesRef.Length + 1);
                    SeekBytesRef.Length++;
                    SeekBytesRef.Bytes[SeekBytesRef.Length - 1] = (byte)nextChar;
                    state = transition.Dest.Number;
                    /*
                     * as long as is possible, continue down the minimal path in
                     * lexicographic order. if a loop or accept state is encountered, stop.
                     */
                    while (Visited[state] != CurGen && !RunAutomaton.IsAccept(state))
                    {
                        Visited[state] = CurGen;
                        /*
                         * Note: we work with a DFA with no transitions to dead states.
                         * so the below is ok, if it is not an accept state,
                         * then there MUST be at least one transition.
                         */
                        transition = AllTransitions[state][0];
                        state = transition.Dest.Number;

                        // append the minimum transition
                        SeekBytesRef.Grow(SeekBytesRef.Length + 1);
                        SeekBytesRef.Length++;
                        SeekBytesRef.Bytes[SeekBytesRef.Length - 1] = (byte)transition.Min;

                        // we found a loop, record it for faster enumeration
                        if ((Finite == false) && !Linear_Renamed && Visited[state] == CurGen)
                        {
                            Linear = SeekBytesRef.Length - 1;
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Attempts to backtrack thru the string after encountering a dead end
        /// at some given position. Returns false if no more possible strings
        /// can match.
        /// </summary>
        /// <param name="position"> current position in the input String </param>
        /// <returns> position >=0 if more possible solutions exist for the DFA </returns>
        private int Backtrack(int position)
        {
            while (position-- > 0)
            {
                int nextChar = SeekBytesRef.Bytes[position] & 0xff;
                // if a character is 0xff its a dead-end too,
                // because there is no higher character in binary sort order.
                if (nextChar++ != 0xff)
                {
                    SeekBytesRef.Bytes[position] = (byte)nextChar;
                    SeekBytesRef.Length = position + 1;
                    return position;
                }
            }
            return -1; // all solutions exhausted
        }
    }
}