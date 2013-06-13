using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util.Automaton
{
    public abstract class RunAutomaton
    {
        int maxInterval;
        int size;
        internal bool[] accept;
        int initial;
        int[] transitions; // delta(state,c) = transitions[state*points.length +
        // getCharClass(c)]
        int[] points; // char interval start points
        int[] classmap; // map from char number to class class

        public override string ToString()
        {
            StringBuilder b = new StringBuilder();
            b.Append("initial state: ").Append(initial).Append("\n");
            for (int i = 0; i < size; i++)
            {
                b.Append("state " + i);
                if (accept[i]) b.Append(" [accept]:\n");
                else b.Append(" [reject]:\n");
                for (int j = 0; j < points.Length; j++)
                {
                    int k = transitions[i * points.Length + j];
                    if (k != -1)
                    {
                        int min = points[j];
                        int max;
                        if (j + 1 < points.Length) max = (points[j + 1] - 1);
                        else max = maxInterval;
                        b.Append(" ");
                        Transition.AppendCharString(min, b);
                        if (min != max)
                        {
                            b.Append("-");
                            Transition.AppendCharString(max, b);
                        }
                        b.Append(" -> ").Append(k).Append("\n");
                    }
                }
            }
            return b.ToString();
        }

        public int Size
        {
            get { return size; }
        }

        public bool IsAccept(int state)
        {
            return accept[state];
        }

        public int InitialState
        {
            get { return initial; }
        }

        public int[] CharInterval
        {
            get { return (int[])points.Clone(); }
        }

        internal int GetCharClass(int c)
        {
            return SpecialOperations.FindIndex(c, points);
        }

        public RunAutomaton(Automaton a, int maxInterval, bool tableize)
        {
            this.maxInterval = maxInterval;
            a.Determinize();
            points = a.GetStartPoints();
            State[] states = a.GetNumberedStates();
            initial = a.InitialState.number;
            size = states.Length;
            accept = new bool[size];
            transitions = new int[size * points.Length];
            for (int n = 0; n < size * points.Length; n++)
                transitions[n] = -1;
            foreach (State s in states)
            {
                int n = s.number;
                accept[n] = s.Accept;
                for (int c = 0; c < points.Length; c++)
                {
                    State q = s.Step(points[c]);
                    if (q != null) transitions[n * points.Length + c] = q.number;
                }
            }
            /*
             * Set alphabet table for optimal run performance.
             */
            if (tableize)
            {
                classmap = new int[maxInterval + 1];
                int i = 0;
                for (int j = 0; j <= maxInterval; j++)
                {
                    if (i + 1 < points.Length && j == points[i + 1])
                        i++;
                    classmap[j] = i;
                }
            }
            else
            {
                classmap = null;
            }
        }

        public int Step(int state, int c)
        {
            if (classmap == null)
                return transitions[state * points.Length + GetCharClass(c)];
            else
                return transitions[state * points.Length + classmap[c]];
        }
    }
}
