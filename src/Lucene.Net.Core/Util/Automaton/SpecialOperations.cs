using Lucene.Net.Support;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
 * this SOFTWARE IS PROVIDED BY THE AUTHOR ``AS IS'' AND ANY EXPRESS OR
 * IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.
 * IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY DIRECT, INDIRECT,
 * INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 * NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
 * DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
 * THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF
 * this SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

namespace Lucene.Net.Util.Automaton
{
    using Util = Lucene.Net.Util.Fst.Util;

    /// <summary>
    /// Special automata operations.
    ///
    /// @lucene.experimental
    /// </summary>
    internal sealed class SpecialOperations
    {
        private SpecialOperations()
        {
        }

        /// <summary>
        /// Finds the largest entry whose value is less than or equal to c, or 0 if
        /// there is no such entry.
        /// </summary>
        internal static int FindIndex(int c, int[] points)
        {
            int a = 0;
            int b = points.Length;
            while (b - a > 1)
            {
                int d = (int)((uint)(a + b) >> 1);
                if (points[d] > c)
                {
                    b = d;
                }
                else if (points[d] < c)
                {
                    a = d;
                }
                else
                {
                    return d;
                }
            }
            return a;
        }

        /// <summary>
        /// Returns true if the language of this automaton is finite.
        /// </summary>
        public static bool IsFinite(Automaton a)
        {
            if (a.IsSingleton)
            {
                return true;
            }
            return IsFinite(a.initial, new BitArray(a.GetNumberOfStates()), new BitArray(a.GetNumberOfStates()));
        }

        /// <summary>
        /// Checks whether there is a loop containing s. (this is sufficient since
        /// there are never transitions to dead states.)
        /// </summary>
        // TODO: not great that this is recursive... in theory a
        // large automata could exceed java's stack
        private static bool IsFinite(State s, BitArray path, BitArray visited)
        {
            path.SafeSet(s.number, true);
            foreach (Transition t in s.GetTransitions())
            {
                if (path.SafeGet(t.to.number) || (!visited.SafeGet(t.to.number) && !IsFinite(t.to, path, visited)))
                {
                    return false;
                }
            }
            path.SafeSet(s.number, false);
            visited.SafeSet(s.number, true);
            return true;
        }

        /// <summary>
        /// Returns the longest string that is a prefix of all accepted strings and
        /// visits each state at most once.
        /// </summary>
        /// <returns> common prefix </returns>
        public static string GetCommonPrefix(Automaton a)
        {
            if (a.IsSingleton)
            {
                return a.singleton;
            }
            StringBuilder b = new StringBuilder();
            HashSet<State> visited = new HashSet<State>();
            State s = a.initial;
            bool done;
            do
            {
                done = true;
                visited.Add(s);
                if (!s.accept && s.NumTransitions == 1)
                {
                    var iter = s.GetTransitions().GetEnumerator();
                    iter.MoveNext();
                    Transition t = iter.Current;
                    if (t.min == t.max && !visited.Contains(t.to))
                    {
                        b.AppendCodePoint(t.min);
                        s = t.to;
                        done = false;
                    }
                }
            } while (!done);
            return b.ToString();
        }

        // TODO: this currently requites a determinized machine,
        // but it need not -- we can speed it up by walking the
        // NFA instead.  it'd still be fail fast.
        public static BytesRef GetCommonPrefixBytesRef(Automaton a)
        {
            if (a.IsSingleton)
            {
                return new BytesRef(a.singleton);
            }
            BytesRef @ref = new BytesRef(10);
            HashSet<State> visited = new HashSet<State>();
            State s = a.initial;
            bool done;
            do
            {
                done = true;
                visited.Add(s);
                if (!s.accept && s.NumTransitions == 1)
                {
                    var iter = s.GetTransitions().GetEnumerator();
                    iter.MoveNext();
                    Transition t = iter.Current;

                    if (t.min == t.max && !visited.Contains(t.to))
                    {
                        @ref.Grow(++@ref.Length);
                        @ref.Bytes[@ref.Length - 1] = (byte)t.min;
                        s = t.to;
                        done = false;
                    }
                }
            } while (!done);
            return @ref;
        }

        /// <summary>
        /// Returns the longest string that is a suffix of all accepted strings and
        /// visits each state at most once.
        /// </summary>
        /// <returns> common suffix </returns>
        public static string GetCommonSuffix(Automaton a)
        {
            if (a.IsSingleton) // if singleton, the suffix is the string itself.
            {
                return a.singleton;
            }

            // reverse the language of the automaton, then reverse its common prefix.
            Automaton r = (Automaton)a.Clone();
            Reverse(r);
            r.Determinize();
            return (new StringBuilder(SpecialOperations.GetCommonPrefix(r))).Reverse().ToString();
        }

        public static BytesRef GetCommonSuffixBytesRef(Automaton a)
        {
            if (a.IsSingleton) // if singleton, the suffix is the string itself.
            {
                return new BytesRef(a.singleton);
            }

            // reverse the language of the automaton, then reverse its common prefix.
            Automaton r = (Automaton)a.Clone();
            Reverse(r);
            r.Determinize();
            BytesRef @ref = SpecialOperations.GetCommonPrefixBytesRef(r);
            ReverseBytes(@ref);
            return @ref;
        }

        private static void ReverseBytes(BytesRef @ref)
        {
            if (@ref.Length <= 1)
            {
                return;
            }
            int num = @ref.Length >> 1;
            for (int i = @ref.Offset; i < (@ref.Offset + num); i++)
            {
                var b = @ref.Bytes[i];
                @ref.Bytes[i] = @ref.Bytes[@ref.Offset * 2 + @ref.Length - i - 1];
                @ref.Bytes[@ref.Offset * 2 + @ref.Length - i - 1] = b;
            }
        }

        /// <summary>
        /// Reverses the language of the given (non-singleton) automaton while returning
        /// the set of new initial states.
        /// </summary>
        public static ISet<State> Reverse(Automaton a)
        {
            a.ExpandSingleton();
            // reverse all edges
            Dictionary<State, HashSet<Transition>> m = new Dictionary<State, HashSet<Transition>>();
            State[] states = a.GetNumberedStates();
            HashSet<State> accept = new HashSet<State>();
            foreach (State s in states)
            {
                if (s.Accept)
                {
                    accept.Add(s);
                }
            }
            foreach (State r in states)
            {
                m[r] = new ValueHashSet<Transition>();
                r.accept = false;
            }
            foreach (State r in states)
            {
                foreach (Transition t in r.GetTransitions())
                {
                    m[t.to].Add(new Transition(t.min, t.max, r));
                }
            }
            foreach (State r in states)
            {
                HashSet<Transition> tr = m[r];
                r.SetTransitions(tr.ToArray(/*new Transition[tr.Count]*/));
            }
            // make new initial+final states
            a.initial.accept = true;
            a.initial = new State();
            foreach (State r in accept)
            {
                a.initial.AddEpsilon(r); // ensures that all initial states are reachable
            }
            a.deterministic = false;
            a.ClearNumberedStates();
            return accept;
        }

        // TODO: this is a dangerous method ... Automaton could be
        // huge ... and it's better in general for caller to
        // enumerate & process in a single walk:

        /// <summary>
        /// Returns the set of accepted strings, assuming that at most
        /// <code>limit</code> strings are accepted. If more than <code>limit</code>
        /// strings are accepted, the first limit strings found are returned. If <code>limit</code>&lt;0, then
        /// the limit is infinite.
        /// </summary>
        public static ISet<IntsRef> GetFiniteStrings(Automaton a, int limit)
        {
            HashSet<IntsRef> strings = new HashSet<IntsRef>();
            if (a.IsSingleton)
            {
                if (limit > 0)
                {
                    strings.Add(Util.ToUTF32(a.Singleton, new IntsRef()));
                }
            }
            else if (!GetFiniteStrings(a.initial, new HashSet<State>(), strings, new IntsRef(), limit))
            {
                return strings;
            }
            return strings;
        }

        /// <summary>
        /// Returns the strings that can be produced from the given state, or
        /// false if more than <code>limit</code> strings are found.
        /// <code>limit</code>&lt;0 means "infinite".
        /// </summary>
        private static bool GetFiniteStrings(State s, HashSet<State> pathstates, HashSet<IntsRef> strings, IntsRef path, int limit)
        {
            pathstates.Add(s);
            foreach (Transition t in s.GetTransitions())
            {
                if (pathstates.Contains(t.to))
                {
                    return false;
                }
                for (int n = t.min; n <= t.max; n++)
                {
                    path.Grow(path.Length + 1);
                    path.Ints[path.Length] = n;
                    path.Length++;
                    if (t.to.accept)
                    {
                        strings.Add(IntsRef.DeepCopyOf(path));
                        if (limit >= 0 && strings.Count > limit)
                        {
                            return false;
                        }
                    }
                    if (!GetFiniteStrings(t.to, pathstates, strings, path, limit))
                    {
                        return false;
                    }
                    path.Length--;
                }
            }
            pathstates.Remove(s);
            return true;
        }
    }
}