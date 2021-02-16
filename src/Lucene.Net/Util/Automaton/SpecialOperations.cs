using J2N.Collections.Generic.Extensions;
using J2N.Numerics;
using J2N.Text;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using JCG = J2N.Collections.Generic;

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
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public static class SpecialOperations // LUCENENET specific - made static since all members are static
    {
        /// <summary>
        /// Finds the largest entry whose value is less than or equal to <paramref name="c"/>, or 0 if
        /// there is no such entry.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int FindIndex(int c, int[] points)
        {
            int a = 0;
            int b = points.Length;
            while (b - a > 1)
            {
                int d = (a + b).TripleShift(1);
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
        /// Returns <c>true</c> if the language of this automaton is finite.
        /// </summary>
        public static bool IsFinite(Automaton a)
        {
            if (a.IsSingleton)
            {
                return true;
            }
            return IsFinite(a.initial, new OpenBitSet(a.GetNumberOfStates()), new OpenBitSet(a.GetNumberOfStates()));
        }

        /// <summary>
        /// Checks whether there is a loop containing <paramref name="s"/>. (This is sufficient since
        /// there are never transitions to dead states.)
        /// </summary>
        // TODO: not great that this is recursive... in theory a
        // large automata could exceed java's stack
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsFinite(State s, OpenBitSet path, OpenBitSet visited)
        {
            path.Set(s.number);
            foreach (Transition t in s.GetTransitions())
            {
                if (path.Get(t.to.number) || (!visited.Get(t.to.number) && !IsFinite(t.to, path, visited)))
                {
                    return false;
                }
            }
            path.Clear(s.number);
            visited.Set(s.number);
            return true;
        }

        /// <summary>
        /// Returns the longest string that is a prefix of all accepted strings and
        /// visits each state at most once.
        /// </summary>
        /// <returns> Common prefix. </returns>
        public static string GetCommonPrefix(Automaton a)
        {
            if (a.IsSingleton)
            {
                return a.singleton;
            }
            StringBuilder b = new StringBuilder();
            JCG.HashSet<State> visited = new JCG.HashSet<State>();
            State s = a.initial;
            bool done;
            do
            {
                done = true;
                visited.Add(s);
                if (!s.accept && s.NumTransitions == 1)
                {
                    Transition t = s.GetTransitions().First();
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
            JCG.HashSet<State> visited = new JCG.HashSet<State>();
            State s = a.initial;
            bool done;
            do
            {
                done = true;
                visited.Add(s);
                if (!s.accept && s.NumTransitions == 1)
                {
                    Transition t = s.GetTransitions().First();

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
        /// <returns> Common suffix. </returns>
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
            Dictionary<State, ISet<Transition>> m = new Dictionary<State, ISet<Transition>>();
            State[] states = a.GetNumberedStates();
            ISet<State> accept = new JCG.HashSet<State>();
            foreach (State s in states)
            {
                if (s.Accept)
                {
                    accept.Add(s);
                }
            }
            foreach (State r in states)
            {
                m[r] = new JCG.HashSet<Transition>();
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
                ISet<Transition> tr = m[r];
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
        /// <paramref name="limit"/> strings are accepted. If more than <paramref name="limit"/>
        /// strings are accepted, the first limit strings found are returned. If <paramref name="limit"/>&lt;0, then
        /// the limit is infinite.
        /// </summary>
        public static ISet<Int32sRef> GetFiniteStrings(Automaton a, int limit)
        {
            JCG.HashSet<Int32sRef> strings = new JCG.HashSet<Int32sRef>();
            if (a.IsSingleton)
            {
                if (limit > 0)
                {
                    strings.Add(Util.ToUTF32(a.Singleton, new Int32sRef()));
                }
            }
            else if (!GetFiniteStrings(a.initial, new JCG.HashSet<State>(), strings, new Int32sRef(), limit))
            {
                return strings;
            }
            return strings;
        }

        /// <summary>
        /// Returns the strings that can be produced from the given state, or
        /// <c>false</c> if more than <paramref name="limit"/> strings are found.
        /// <paramref name="limit"/>&lt;0 means "infinite".
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool GetFiniteStrings(State s, JCG.HashSet<State> pathstates, JCG.HashSet<Int32sRef> strings, Int32sRef path, int limit)
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
                    path.Int32s[path.Length] = n;
                    path.Length++;
                    if (t.to.accept)
                    {
                        strings.Add(Int32sRef.DeepCopyOf(path));
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