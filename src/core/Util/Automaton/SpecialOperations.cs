using Lucene.Net.Support;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util.Automaton
{
    public sealed class SpecialOperations
    {
        public static int FindIndex(int c, int[] points)
        {
            int a = 0;
            int b = points.Length;
            while (b - a > 1)
            {
                int d = Number.URShift(a + b, 1);
                if (points[d] > c) b = d;
                else if (points[d] < c) a = d;
                else return d;
            }
            return a;
        }

        public static bool IsFinite(Automaton a)
        {
            if (a.IsSingleton()) return true;
            return IsFinite(a.InitialState, new BitArray(a.GetNumberOfStates()), new BitArray(a.GetNumberOfStates()));
        }

        private static bool IsFinite(State s, BitArray path, BitArray visited)
        {
            path.Set(s.number);
            foreach (Transition t in s.GetTransitions())
                if (path.Get(t.to.number) || (!visited.Get(t.to.number) && !IsFinite(t.to, path, visited))) return false;
            path.Clear(s.number);
            visited.Set(s.number);
            return true;
        }

        public static String GetCommonPrefix(Automaton a)
        {
            if (a.IsSingleton()) return a.Singleton;
            StringBuilder b = new StringBuilder();
            HashSet<State> visited = new HashSet<State>();
            State s = a.InitialState;
            bool done;
            do
            {
                done = true;
                visited.Add(s);
                if (!s.Accept && s.NumTransitions == 1)
                {
                    Transition t = s.GetTransitions().First();
                    if (t.min == t.max && !visited.Contains(t.to))
                    {
                        b.Append((char)t.min); // TODO: is this correct? no "code point" support in .NET
                        s = t.to;
                        done = false;
                    }
                }
            } while (!done);
            return b.ToString();
        }

        public static BytesRef GetCommonPrefixBytesRef(Automaton a)
        {
            if (a.IsSingleton()) return new BytesRef(a.Singleton);
            BytesRef bytesref = new BytesRef(10);
            HashSet<State> visited = new HashSet<State>();
            State s = a.InitialState;
            bool done;
            do
            {
                done = true;
                visited.Add(s);
                if (!s.Accept && s.NumTransitions == 1)
                {
                    Transition t = s.GetTransitions().First();
                    if (t.min == t.max && !visited.Contains(t.to))
                    {
                        bytesref.Grow(++bytesref.length);
                        bytesref.bytes[bytesref.length - 1] = (sbyte)t.min;
                        s = t.to;
                        done = false;
                    }
                }
            } while (!done);
            return bytesref;
        }

        public static String GetCommonSuffix(Automaton a)
        {
            if (a.IsSingleton()) // if singleton, the suffix is the string itself.
                return a.Singleton;

            // reverse the language of the automaton, then reverse its common prefix.
            Automaton r = (Automaton)a.Clone();
            Reverse(r);
            r.Determinize();
            return new StringBuilder(SpecialOperations.GetCommonPrefix(r)).Reverse().ToString();
        }

        public static BytesRef GetCommonSuffixBytesRef(Automaton a)
        {
            if (a.IsSingleton()) // if singleton, the suffix is the string itself.
                return new BytesRef(a.Singleton);

            // reverse the language of the automaton, then reverse its common prefix.
            Automaton r = (Automaton)a.Clone();
            Reverse(r);
            r.Determinize();
            BytesRef bytesref = SpecialOperations.GetCommonPrefixBytesRef(r);
            ReverseBytes(bytesref);
            return bytesref;
        }

        private static void ReverseBytes(BytesRef bytesref)
        {
            if (bytesref.length <= 1) return;
            int num = bytesref.length >> 1;
            for (int i = bytesref.offset; i < (bytesref.offset + num); i++)
            {
                sbyte b = bytesref.bytes[i];
                bytesref.bytes[i] = bytesref.bytes[bytesref.offset * 2 + bytesref.length - i - 1];
                bytesref.bytes[bytesref.offset * 2 + bytesref.length - i - 1] = b;
            }
        }

        public static ISet<State> Reverse(Automaton a)
        {
            a.ExpandSingleton();
            // reverse all edges
            HashMap<State, HashSet<Transition>> m = new HashMap<State, HashSet<Transition>>();
            State[] states = a.GetNumberedStates();
            ISet<State> accept = new HashSet<State>();
            foreach (State s in states)
                if (s.Accept)
                    accept.Add(s);
            foreach (State r in states)
            {
                m[r] = new HashSet<Transition>();
                r.Accept = false;
            }
            foreach (State r in states)
                foreach (Transition t in r.GetTransitions())
                    m[t.to].Add(new Transition(t.min, t.max, r));
            foreach (State r in states)
            {
                ISet<Transition> tr = m[r];
                r.SetTransitions(tr.ToArray());
            }
            // make new initial+final states
            a.InitialState.Accept = true;
            a.InitialState = new State();
            foreach (State r in accept)
                a.InitialState.AddEpsilon(r); // ensures that all initial states are reachable
            a.Deterministic = false;
            a.ClearNumberedStates();
            return accept;
        }

        public static ISet<IntsRef> GetFiniteStrings(Automaton a, int limit)
        {
            HashSet<IntsRef> strings = new HashSet<IntsRef>();
            if (a.IsSingleton())
            {
                if (limit > 0)
                {
                    strings.Add(Lucene.Net.Util.Fst.Util.ToUTF32(a.Singleton, new IntsRef()));
                }
                else
                {
                    return null;
                }
            }
            else if (!GetFiniteStrings(a.InitialState, new HashSet<State>(), strings, new IntsRef(), limit))
            {
                return null;
            }
            return strings;
        }

        private static bool GetFiniteStrings(State s, HashSet<State> pathstates,
            HashSet<IntsRef> strings, IntsRef path, int limit)
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
                    path.Grow(path.length + 1);
                    path.ints[path.length] = n;
                    path.length++;
                    if (t.to.Accept)
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
                    path.length--;
                }
            }
            pathstates.Remove(s);
            return true;
        }
    }
}
