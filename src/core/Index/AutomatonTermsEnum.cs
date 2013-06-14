using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Index
{
    internal class AutomatonTermsEnum : FilteredTermsEnum
    {
        // a tableized array-based form of the DFA
        private readonly ByteRunAutomaton runAutomaton;
        // common suffix of the automaton
        private readonly BytesRef commonSuffixRef;
        // true if the automaton accepts a finite language
        private readonly bool finite;
        // array of sorted transitions for each state, indexed by state number
        private readonly Transition[][] allTransitions;
        // for path tracking: each long records gen when we last
        // visited the state; we use gens to avoid having to clear
        private readonly long[] visited;
        private long curGen;
        // the reference used for seeking forwards through the term dictionary
        private BytesRef seekBytesRef = new BytesRef(10);
        // true if we are enumerating an infinite portion of the DFA.
        // in this case it is faster to drive the query based on the terms dictionary.
        // when this is true, linearUpperBound indicate the end of range
        // of terms where we should simply do sequential reads instead.
        private bool linear = false;
        private BytesRef linearUpperBound = new BytesRef(10);
        private IComparer<BytesRef> termComp;

        public AutomatonTermsEnum(TermsEnum tenum, CompiledAutomaton compiled)
            : base(tenum)
        {
            this.finite = compiled.finite;
            this.runAutomaton = compiled.runAutomaton;
            //assert this.runAutomaton != null;
            this.commonSuffixRef = compiled.commonSuffixRef;
            this.allTransitions = compiled.sortedTransitions;

            // used for path tracking, where each bit is a numbered state.
            visited = new long[runAutomaton.getSize()];

            termComp = getComparator();
        }


        protected override AcceptStatus accept(BytesRef term)
        {
            if (commonSuffixRef == null || StringHelper.EndsWith(term, commonSuffixRef))
            {
                if (runAutomaton.run(term.bytes, term.offset, term.length))
                    return linear ? AcceptStatus.YES : AcceptStatus.YES_AND_SEEK;
                else
                    return (linear && termComp.Compareompare(term, linearUpperBound) < 0) ?
                        AcceptStatus.NO : AcceptStatus.NO_AND_SEEK;
            }
            else
            {
                return (linear && termComp.Compare(term, linearUpperBound) < 0) ?
                    AcceptStatus.NO : AcceptStatus.NO_AND_SEEK;
            }
        }


        protected override BytesRef nextSeekTerm(BytesRef term)
        {
            //System.out.println("ATE.nextSeekTerm term=" + term);
            if (term == null)
            {
                //assert seekBytesRef.length == 0;
                // return the empty term, as its valid
                if (runAutomaton.isAccept(runAutomaton.getInitialState()))
                {
                    return seekBytesRef;
                }
            }
            else
            {
                seekBytesRef.CopyBytes(term);
            }

            // seek to the next possible string;
            if (nextString())
            {
                return seekBytesRef;  // reposition
            }
            else
            {
                return null;          // no more possible strings can match
            }
        }

        private void setLinear(int position)
        {
            //assert linear == false;

            int state = runAutomaton.getInitialState();
            int maxInterval = 0xff;
            for (int i = 0; i < position; i++)
            {
                state = runAutomaton.step(state, seekBytesRef.bytes[i] & 0xff);
                //assert state >= 0: "state=" + state;
            }
            for (int i = 0; i < allTransitions[state].Length; i++)
            {
                Transition t = allTransitions[state][i];
                if (t.getMin() <= (seekBytesRef.bytes[position] & 0xff) &&
                    (seekBytesRef.bytes[position] & 0xff) <= t.getMax())
                {
                    maxInterval = t.getMax();
                    break;
                }
            }
            // 0xff terms don't get the optimization... not worth the trouble.
            if (maxInterval != 0xff)
                maxInterval++;
            int length = position + 1; /* position + maxTransition */
            if (linearUpperBound.bytes.Length < length)
                linearUpperBound.bytes = new sbyte[length];
            Array.Copy(seekBytesRef.bytes, 0, linearUpperBound.bytes, 0, position);
            linearUpperBound.bytes[position] = (sbyte)maxInterval;
            linearUpperBound.length = length;

            linear = true;
        }

        private readonly IntsRef savedStates = new IntsRef(10);

        private bool nextString()
        {
            int state;
            int pos = 0;
            savedStates.Grow(seekBytesRef.length + 1);
            int[] states = savedStates.ints;
            states[0] = runAutomaton.getInitialState();

            while (true)
            {
                curGen++;
                linear = false;
                // walk the automaton until a character is rejected.
                for (state = states[pos]; pos < seekBytesRef.length; pos++)
                {
                    visited[state] = curGen;
                    int nextState = runAutomaton.step(state, seekBytesRef.bytes[pos] & 0xff);
                    if (nextState == -1)
                        break;
                    states[pos + 1] = nextState;
                    // we found a loop, record it for faster enumeration
                    if (!finite && !linear && visited[nextState] == curGen)
                    {
                        setLinear(pos);
                    }
                    state = nextState;
                }

                // take the useful portion, and the last non-reject state, and attempt to
                // append characters that will match.
                if (nextString(state, pos))
                {
                    return true;
                }
                else
                { /* no more solutions exist from this useful portion, backtrack */
                    if ((pos = backtrack(pos)) < 0) /* no more solutions at all */
                        return false;
                    int newState = runAutomaton.step(states[pos], seekBytesRef.bytes[pos] & 0xff);
                    if (newState >= 0 && runAutomaton.isAccept(newState))
                        /* String is good to go as-is */
                        return true;
                    /* else advance further */
                    // TODO: paranoia? if we backtrack thru an infinite DFA, the loop detection is important!
                    // for now, restart from scratch for all infinite DFAs 
                    if (!finite) pos = 0;
                }
            }
        }

        private bool nextString(int state, int position)
        {
            /* 
             * the next lexicographic character must be greater than the existing
             * character, if it exists.
             */
            int c = 0;
            if (position < seekBytesRef.length)
            {
                c = seekBytesRef.bytes[position] & 0xff;
                // if the next byte is 0xff and is not part of the useful portion,
                // then by definition it puts us in a reject state, and therefore this
                // path is dead. there cannot be any higher transitions. backtrack.
                if (c++ == 0xff)
                    return false;
            }

            seekBytesRef.length = position;
            visited[state] = curGen;

            Transition[] transitions = allTransitions[state];

            // find the minimal path (lexicographic order) that is >= c

            for (int i = 0; i < transitions.Length; i++)
            {
                Transition transition = transitions[i];
                if (transition.getMax() >= c)
                {
                    int nextChar = Math.Max(c, transition.getMin());
                    // append either the next sequential char, or the minimum transition
                    seekBytesRef.Grow(seekBytesRef.length + 1);
                    seekBytesRef.length++;
                    seekBytesRef.bytes[seekBytesRef.length - 1] = (sbyte)nextChar;
                    state = transition.getDest().getNumber();
                    /* 
                     * as long as is possible, continue down the minimal path in
                     * lexicographic order. if a loop or accept state is encountered, stop.
                     */
                    while (visited[state] != curGen && !runAutomaton.isAccept(state))
                    {
                        visited[state] = curGen;
                        /* 
                         * Note: we work with a DFA with no transitions to dead states.
                         * so the below is ok, if it is not an accept state,
                         * then there MUST be at least one transition.
                         */
                        transition = allTransitions[state][0];
                        state = transition.getDest().getNumber();

                        // append the minimum transition
                        seekBytesRef.Grow(seekBytesRef.length + 1);
                        seekBytesRef.length++;
                        seekBytesRef.bytes[seekBytesRef.length - 1] = (byte)transition.getMin();

                        // we found a loop, record it for faster enumeration
                        if (!finite && !linear && visited[state] == curGen)
                        {
                            setLinear(seekBytesRef.length - 1);
                        }
                    }
                    return true;
                }
            }
            return false;
        }

        private int backtrack(int position)
        {
            while (position-- > 0)
            {
                int nextChar = seekBytesRef.bytes[position] & 0xff;
                // if a character is 0xff its a dead-end too,
                // because there is no higher character in binary sort order.
                if (nextChar++ != 0xff)
                {
                    seekBytesRef.bytes[position] = (sbyte)nextChar;
                    seekBytesRef.length = position + 1;
                    return position;
                }
            }
            return -1; /* all solutions exhausted */
        }
    }
}
