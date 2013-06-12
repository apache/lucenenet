using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util.Automaton
{
    public static class BasicAutomata
    {
        public static Automaton MakeEmpty()
        {
            Automaton a = new Automaton();
            State s = new State();
            a.InitialState = s;
            a.Deterministic = true;
            return a;
        }

        public static Automaton MakeEmptyString()
        {
            Automaton a = new Automaton();
            a.Singleton = "";
            a.Deterministic = true;
            return a;
        }

        public static Automaton MakeAnyString()
        {
            Automaton a = new Automaton();
            State s = new State();
            a.InitialState = s;
            s.Accept = true;
            s.AddTransition(new Transition(Character.MIN_CODE_POINT, Character.MAX_CODE_POINT,
                s));
            a.Deterministic = true;
            return a;
        }

        public static Automaton MakeAnyChar()
        {
            return MakeCharRange(Character.MIN_CODE_POINT, Character.MAX_CODE_POINT);
        }

        public static Automaton MakeChar(int c)
        {
            Automaton a = new Automaton();
            a.Singleton = c.ToString();
            a.Deterministic = true;
            return a;
        }

        public static Automaton MakeCharRange(int min, int max)
        {
            if (min == max) return MakeChar(min);
            Automaton a = new Automaton();
            State s1 = new State();
            State s2 = new State();
            a.InitialState = s1;
            s2.Accept = true;
            if (min <= max) s1.AddTransition(new Transition(min, max, s2));
            a.Deterministic = true;
            return a;
        }

        private static State AnyOfRightLength(String x, int n)
        {
            State s = new State();
            if (x.Length == n) s.Accept = true;
            else s.AddTransition(new Transition('0', '9', AnyOfRightLength(x, n + 1)));
            return s;
        }

        private static State AtLeast(String x, int n, ICollection<State> initials,
            bool zeros)
        {
            State s = new State();
            if (x.Length == n) s.Accept = true;
            else
            {
                if (zeros) initials.Add(s);
                char c = x[n];
                s.AddTransition(new Transition(c, AtLeast(x, n + 1, initials, zeros
                    && c == '0')));
                if (c < '9') s.AddTransition(new Transition((char)(c + 1), '9',
                    AnyOfRightLength(x, n + 1)));
            }
            return s;
        }

        private static State AtMost(String x, int n)
        {
            State s = new State();
            if (x.Length == n) s.Accept = true;
            else
            {
                char c = x[n];
                s.AddTransition(new Transition(c, AtMost(x, (char)n + 1)));
                if (c > '0') s.AddTransition(new Transition('0', (char)(c - 1),
                    AnyOfRightLength(x, n + 1)));
            }
            return s;
        }

        private static State Between(String x, String y, int n,
            ICollection<State> initials, bool zeros)
        {
            State s = new State();
            if (x.Length == n) s.Accept = true;
            else
            {
                if (zeros) initials.Add(s);
                char cx = x[n];
                char cy = y[n];
                if (cx == cy) s.AddTransition(new Transition(cx, Between(x, y, n + 1,
                    initials, zeros && cx == '0')));
                else
                { // cx<cy
                    s.AddTransition(new Transition(cx, AtLeast(x, n + 1, initials, zeros
                        && cx == '0')));
                    s.AddTransition(new Transition(cy, AtMost(y, n + 1)));
                    if (cx + 1 < cy) s.AddTransition(new Transition((char)(cx + 1),
                        (char)(cy - 1), AnyOfRightLength(x, n + 1)));
                }
            }
            return s;
        }

        public static Automaton MakeInterval(int min, int max, int digits)
        {
            Automaton a = new Automaton();
            String x = min.ToString();
            String y = max.ToString();
            if (min > max || (digits > 0 && y.Length > digits)) throw new ArgumentException();
            int d;
            if (digits > 0) d = digits;
            else d = y.Length;
            StringBuilder bx = new StringBuilder();
            for (int i = x.Length; i < d; i++)
                bx.Append('0');
            bx.Append(x);
            x = bx.ToString();
            StringBuilder by = new StringBuilder();
            for (int i = y.Length; i < d; i++)
                by.Append('0');
            by.Append(y);
            y = by.ToString();
            ICollection<State> initials = new List<State>();
            a.InitialState = Between(x, y, 0, initials, digits <= 0);
            if (digits <= 0)
            {
                List<StatePair> pairs = new List<StatePair>();
                foreach (State p in initials)
                    if (a.InitialState != p) pairs.Add(new StatePair(a.InitialState, p));
                BasicOperations.AddEpsilons(a, pairs);
                a.InitialState.AddTransition(new Transition('0', a.InitialState));
                a.Deterministic = false;
            }
            else a.Deterministic = true;
            a.CheckMinimizeAlways();
            return a;
        }

        public static Automaton MakeString(String s)
        {
            Automaton a = new Automaton();
            a.Singleton = s;
            a.Deterministic = true;
            return a;
        }

        public static Automaton MakeString(int[] word, int offset, int length)
        {
            Automaton a = new Automaton();
            a.Deterministic = true;
            State s = new State();
            a.InitialState = s;
            for (int i = offset; i < offset + length; i++)
            {
                State s2 = new State();
                s.AddTransition(new Transition(word[i], s2));
                s = s2;
            }
            s.Accept = true;
            return a;
        }

        public static Automaton MakeStringUnion(ICollection<BytesRef> utf8Strings)
        {
            if (utf8Strings.Count == 0)
            {
                return MakeEmpty();
            }
            else
            {
                return DaciukMihovAutomatonBuilder.Build(utf8Strings);
            }
        }
    }
}
