using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using NUnit.Framework;

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

    using Lucene.Net.Randomized.Generators;

    /// <summary>
    /// Utilities for testing automata.
    /// <p>
    /// Capable of generating random regular expressions,
    /// and automata, and also provides a number of very
    /// basic unoptimized implementations (*slow) for testing.
    /// </summary>
    public class AutomatonTestUtil
    {
        /// <summary>
        /// Returns random string, including full unicode range. </summary>
        public static string RandomRegexp(Random r)
        {
            while (true)
            {
                string regexp = RandomRegexpString(r);
                // we will also generate some undefined unicode queries
                if (!UnicodeUtil.ValidUTF16String(regexp.ToCharArray()))
                {
                    continue;
                }
                try
                {
                    new RegExp(regexp, RegExp.NONE);
                    return regexp;
                }
                catch (Exception e)
                {
                }
            }
        }

        private static string RandomRegexpString(Random r)
        {
            int end = r.Next(20);
            if (end == 0)
            {
                // allow 0 length
                return "";
            }
            char[] buffer = new char[end];
            for (int i = 0; i < end; i++)
            {
                int t = r.Next(15);
                if (0 == t && i < end - 1)
                {
                    // Make a surrogate pair
                    // High surrogate
                    buffer[i++] = (char)TestUtil.NextInt(r, 0xd800, 0xdbff);
                    // Low surrogate
                    buffer[i] = (char)TestUtil.NextInt(r, 0xdc00, 0xdfff);
                }
                else if (t <= 1)
                {
                    buffer[i] = (char)r.Next(0x80);
                }
                else if (2 == t)
                {
                    buffer[i] = (char)TestUtil.NextInt(r, 0x80, 0x800);
                }
                else if (3 == t)
                {
                    buffer[i] = (char)TestUtil.NextInt(r, 0x800, 0xd7ff);
                }
                else if (4 == t)
                {
                    buffer[i] = (char)TestUtil.NextInt(r, 0xe000, 0xffff);
                }
                else if (5 == t)
                {
                    buffer[i] = '.';
                }
                else if (6 == t)
                {
                    buffer[i] = '?';
                }
                else if (7 == t)
                {
                    buffer[i] = '*';
                }
                else if (8 == t)
                {
                    buffer[i] = '+';
                }
                else if (9 == t)
                {
                    buffer[i] = '(';
                }
                else if (10 == t)
                {
                    buffer[i] = ')';
                }
                else if (11 == t)
                {
                    buffer[i] = '-';
                }
                else if (12 == t)
                {
                    buffer[i] = '[';
                }
                else if (13 == t)
                {
                    buffer[i] = ']';
                }
                else if (14 == t)
                {
                    buffer[i] = '|';
                }
            }
            return new string(buffer, 0, end);
        }

        /// <summary>
        /// picks a random int code point, avoiding surrogates;
        /// throws IllegalArgumentException if this transition only
        /// accepts surrogates
        /// </summary>
        private static int GetRandomCodePoint(Random r, Transition t)
        {
            int code;
            if (t.Max < UnicodeUtil.UNI_SUR_HIGH_START || t.Min > UnicodeUtil.UNI_SUR_HIGH_END)
            {
                // easy: entire range is before or after surrogates
                code = t.Min + r.Next(t.Max - t.Min + 1);
            }
            else if (t.Min >= UnicodeUtil.UNI_SUR_HIGH_START)
            {
                if (t.Max > UnicodeUtil.UNI_SUR_LOW_END)
                {
                    // after surrogates
                    code = 1 + UnicodeUtil.UNI_SUR_LOW_END + r.Next(t.Max - UnicodeUtil.UNI_SUR_LOW_END);
                }
                else
                {
                    throw new System.ArgumentException("transition accepts only surrogates: " + t);
                }
            }
            else if (t.Max <= UnicodeUtil.UNI_SUR_LOW_END)
            {
                if (t.Min < UnicodeUtil.UNI_SUR_HIGH_START)
                {
                    // before surrogates
                    code = t.Min + r.Next(UnicodeUtil.UNI_SUR_HIGH_START - t.Min);
                }
                else
                {
                    throw new System.ArgumentException("transition accepts only surrogates: " + t);
                }
            }
            else
            {
                // range includes all surrogates
                int gap1 = UnicodeUtil.UNI_SUR_HIGH_START - t.Min;
                int gap2 = t.Max - UnicodeUtil.UNI_SUR_LOW_END;
                int c = r.Next(gap1 + gap2);
                if (c < gap1)
                {
                    code = t.Min + c;
                }
                else
                {
                    code = UnicodeUtil.UNI_SUR_LOW_END + c - gap1 + 1;
                }
            }

            Assert.True(code >= t.Min && code <= t.Max && (code < UnicodeUtil.UNI_SUR_HIGH_START || code > UnicodeUtil.UNI_SUR_LOW_END), "code=" + code + " min=" + t.Min + " max=" + t.Max);
            return code;
        }

        /// <summary>
        /// Lets you retrieve random strings accepted
        /// by an Automaton.
        /// <p>
        /// Once created, call <seealso cref="#getRandomAcceptedString(Random)"/>
        /// to get a new string (in UTF-32 codepoints).
        /// </summary>
        public class RandomAcceptedStrings
        {
            internal readonly IDictionary<Transition, bool?> LeadsToAccept;
            internal readonly Automaton a;

            private class ArrivingTransition
            {
                internal readonly State From;
                internal readonly Transition t;

                public ArrivingTransition(State from, Transition t)
                {
                    this.From = from;
                    this.t = t;
                }
            }

            public RandomAcceptedStrings(Automaton a)
            {
                this.a = a;
                if (!String.IsNullOrEmpty(a.Singleton))
                {
                    LeadsToAccept = null;
                    return;
                }

                // must use IdentityHashmap because two Transitions w/
                // different start nodes can be considered the same
                LeadsToAccept = new IdentityHashMap<Transition, bool?>();
                IDictionary<State, IList<ArrivingTransition>> allArriving = new Dictionary<State, IList<ArrivingTransition>>();

                LinkedList<State> q = new LinkedList<State>();
                HashSet<State> seen = new HashSet<State>();

                // reverse map the transitions, so we can quickly look
                // up all arriving transitions to a given state
                foreach (State s in a.NumberedStates)
                {
                    for (int i = 0; i < s.numTransitions; i++)
                    {
                        Transition t = s.TransitionsArray[i];
                        IList<ArrivingTransition> tl;
                        allArriving.TryGetValue(t.Dest, out tl);
                        if (tl == null)
                        {
                            tl = new List<ArrivingTransition>();
                            allArriving[t.Dest] = tl;
                        }
                        tl.Add(new ArrivingTransition(s, t));
                    }
                    if (s.Accept)
                    {
                        q.AddLast(s);
                        seen.Add(s);
                    }
                }

                // Breadth-first search, from accept states,
                // backwards:
                while (q.Count > 0)
                {
                    State s = q.First.Value;
                    q.RemoveFirst();
                    IList<ArrivingTransition> arriving;
                    allArriving.TryGetValue(s, out arriving);
                    if (arriving != null)
                    {
                        foreach (ArrivingTransition at in arriving)
                        {
                            State from = at.From;
                            if (!seen.Contains(from))
                            {
                                q.AddLast(from);
                                seen.Add(from);
                                LeadsToAccept[at.t] = true;
                            }
                        }
                    }
                }
            }

            public int[] GetRandomAcceptedString(Random r)
            {
                IList<int?> soFar = new List<int?>();
                if (a.IsSingleton)
                {
                    // accepts only one
                    var s = a.Singleton;

                    int charUpto = 0;
                    while (charUpto < s.Length)
                    {
                        int cp = Character.CodePointAt(s, charUpto);
                        charUpto += Character.CharCount(cp);
                        soFar.Add(cp);
                    }
                }
                else
                {
                    var s = a.InitialState;

                    while (true)
                    {
                        if (s.Accept)
                        {
                            if (s.numTransitions == 0)
                            {
                                // stop now
                                break;
                            }
                            else
                            {
                                if (r.NextBoolean())
                                {
                                    break;
                                }
                            }
                        }

                        if (s.numTransitions == 0)
                        {
                            throw new Exception("this automaton has dead states");
                        }

                        bool cheat = r.NextBoolean();

                        Transition t;
                        if (cheat)
                        {
                            // pick a transition that we know is the fastest
                            // path to an accept state
                            IList<Transition> toAccept = new List<Transition>();
                            for (int i = 0; i < s.numTransitions; i++)
                            {
                                Transition t0 = s.TransitionsArray[i];
                                if (LeadsToAccept.ContainsKey(t0))
                                {
                                    toAccept.Add(t0);
                                }
                            }
                            if (toAccept.Count == 0)
                            {
                                // this is OK -- it means we jumped into a cycle
                                t = s.TransitionsArray[r.Next(s.numTransitions)];
                            }
                            else
                            {
                                t = toAccept[r.Next(toAccept.Count)];
                            }
                        }
                        else
                        {
                            t = s.TransitionsArray[r.Next(s.numTransitions)];
                        }
                        soFar.Add(GetRandomCodePoint(r, t));
                        s = t.Dest;
                    }
                }

                return ArrayUtil.ToIntArray(soFar);
            }
        }

        /// <summary>
        /// return a random NFA/DFA for testing </summary>
        public static Automaton RandomAutomaton(Random random)
        {
            // get two random Automata from regexps
            Automaton a1 = (new RegExp(AutomatonTestUtil.RandomRegexp(random), RegExp.NONE)).ToAutomaton();
            if (random.NextBoolean())
            {
                a1 = BasicOperations.Complement(a1);
            }

            Automaton a2 = (new RegExp(AutomatonTestUtil.RandomRegexp(random), RegExp.NONE)).ToAutomaton();
            if (random.NextBoolean())
            {
                a2 = BasicOperations.Complement(a2);
            }

            // combine them in random ways
            switch (random.Next(4))
            {
                case 0:
                    return BasicOperations.Concatenate(a1, a2);

                case 1:
                    return BasicOperations.Union(a1, a2);

                case 2:
                    return BasicOperations.Intersection(a1, a2);

                default:
                    return BasicOperations.Minus(a1, a2);
            }
        }

        /// <summary>
        /// below are original, unoptimized implementations of DFA operations for testing.
        /// These are from brics automaton, full license (BSD) below:
        /// </summary>

        /*
         * dk.brics.automaton
         *
         * Copyright (c) 2001-2009 Anders Moeller
         * All rights reserved.
         *
         * Redistribution and use in source and binary forms, with or without
         * modification, are permitted provided that the following conditions
         * are met:
         * 1. Redistributions of source code must retain the above copyright
         *    notice, this list of conditions and the following disclaimer.
         * 2. Redistributions in binary form must reproduce the above copyright
         *    notice, this list of conditions and the following disclaimer in the
         *    documentation and/or other materials provided with the distribution.
         * 3. The name of the author may not be used to endorse or promote products
         *    derived from this software without specific prior written permission.
         *
         * THIS SOFTWARE IS PROVIDED BY THE AUTHOR ``AS IS'' AND ANY EXPRESS OR
         * IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
         * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.
         * IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY DIRECT, INDIRECT,
         * INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
         * NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
         * DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
         * THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
         * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF
         * THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
         */

        /// <summary>
        /// Simple, original brics implementation of Brzozowski minimize()
        /// </summary>
        public static void MinimizeSimple(Automaton a)
        {
            if (!String.IsNullOrEmpty(a.Singleton))
            {
                return;
            }
            DeterminizeSimple(a, SpecialOperations.Reverse(a));
            DeterminizeSimple(a, SpecialOperations.Reverse(a));
        }

        /// <summary>
        /// Simple, original brics implementation of determinize()
        /// </summary>
        public static void DeterminizeSimple(Automaton a)
        {
            if (a.Deterministic || a.IsSingleton)
            {
                return;
            }
            HashSet<State> initialset = new ValueHashSet<State>();
            initialset.Add(a.Initial);
            DeterminizeSimple(a, initialset);
        }

        /// <summary>
        /// Simple, original brics implementation of determinize()
        /// Determinizes the given automaton using the given set of initial states.
        /// </summary>
        public static void DeterminizeSimple(Automaton a, ISet<State> initialset)
        {
            int[] points = a.StartPoints;
            // subset construction
            IDictionary<ISet<State>, ISet<State>> sets = new Dictionary<ISet<State>, ISet<State>>();
            LinkedList<ISet<State>> worklist = new LinkedList<ISet<State>>();
            IDictionary<ISet<State>, State> newstate = new Dictionary<ISet<State>, State>();
            sets[initialset] = initialset;
            worklist.AddLast(initialset);
            a.Initial = new State();
            newstate[initialset] = a.Initial;
            while (worklist.Count > 0)
            {
                ISet<State> s = worklist.First.Value;
                worklist.RemoveFirst();
                State r = newstate[s];
                foreach (State q in s)
                {
                    if (q.Accept)
                    {
                        r.Accept = true;
                        break;
                    }
                }
                for (int n = 0; n < points.Length; n++)
                {
                    ISet<State> p = new ValueHashSet<State>();
                    foreach (State q in s)
                    {
                        foreach (Transition t in q.Transitions)
                        {
                            if (t.Min <= points[n] && points[n] <= t.Max)
                            {
                                p.Add(t.To);
                            }
                        }
                    }
                    if (!sets.ContainsKey(p))
                    {
                        sets[p] = p;
                        worklist.AddLast(p);
                        newstate[p] = new State();
                    }
                    State q_ = newstate[p];
                    int min = points[n];
                    int max;
                    if (n + 1 < points.Length)
                    {
                        max = points[n + 1] - 1;
                    }
                    else
                    {
                        max = Character.MAX_CODE_POINT;
                    }
                    r.AddTransition(new Transition(min, max, q_));
                }
            }
            a.Deterministic = true;
            a.ClearNumberedStates();
            a.RemoveDeadTransitions();
        }

        /// <summary>
        /// Returns true if the language of this automaton is finite.
        /// <p>
        /// WARNING: this method is slow, it will blow up if the automaton is large.
        /// this is only used to test the correctness of our faster implementation.
        /// </summary>
        public static bool IsFiniteSlow(Automaton a)
        {
            if (!String.IsNullOrEmpty(a.Singleton))
            {
                return true;
            }
            return IsFiniteSlow(a.InitialState, new HashSet<State>());
        }

        /// <summary>
        /// Checks whether there is a loop containing s. (this is sufficient since
        /// there are never transitions to dead states.)
        /// </summary>
        // TODO: not great that this is recursive... in theory a
        // large automata could exceed java's stack
        private static bool IsFiniteSlow(State s, HashSet<State> path)
        {
            path.Add(s);
            foreach (Transition t in s.Transitions)
            {
                if (path.Contains(t.Dest) || !IsFiniteSlow(t.Dest, path))
                {
                    return false;
                }
            }
            path.Remove(s);
            return true;
        }

        /// <summary>
        /// Checks that an automaton has no detached states that are unreachable
        /// from the initial state.
        /// </summary>
        public static void AssertNoDetachedStates(Automaton a)
        {
            int numStates = a.NumberOfStates;
            a.ClearNumberedStates(); // force recomputation of cached numbered states
            Assert.True(numStates == a.NumberOfStates, "automaton has " + (numStates - a.NumberOfStates) + " detached states");
        }
    }
}