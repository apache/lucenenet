using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util.Automaton
{
    public class Transition : ICloneable
    {
        internal readonly int min;
        internal readonly int max;
        internal readonly State to;

        public Transition(int c, State to)
        {
            //assert c >= 0;
            min = max = c;
            this.to = to;
        }

        public Transition(int min, int max, State to)
        {
            //assert min >= 0;
            //assert max >= 0;
            if (max < min)
            {
                int t = max;
                max = min;
                min = t;
            }
            this.min = min;
            this.max = max;
            this.to = to;
        }

        public int Min
        {
            get { return min; }
        }

        public int Max
        {
            get { return max; }
        }

        public State Dest
        {
            get { return to; }
        }

        public override bool Equals(object obj)
        {
            if (obj is Transition)
            {
                Transition t = (Transition)obj;
                return t.min == min && t.max == max && t.to == to;
            }
            else return false;
        }

        public override int GetHashCode()
        {
            return min * 2 + max * 3;
        }

        public object Clone()
        {
            return this.MemberwiseClone();
        }

        internal static void AppendCharString(int c, StringBuilder b)
        {
            if (c >= 0x21 && c <= 0x7e && c != '\\' && c != '"') b.Append(c);
            else
            {
                b.Append("\\\\U");
                String s = ((short)c).ToString("X");
                if (c < 0x10) b.Append("0000000").Append(s);
                else if (c < 0x100) b.Append("000000").Append(s);
                else if (c < 0x1000) b.Append("00000").Append(s);
                else if (c < 0x10000) b.Append("0000").Append(s);
                else if (c < 0x100000) b.Append("000").Append(s);
                else if (c < 0x1000000) b.Append("00").Append(s);
                else if (c < 0x10000000) b.Append("0").Append(s);
                else b.Append(s);
            }
        }

        public override string ToString()
        {
            StringBuilder b = new StringBuilder();
            AppendCharString(min, b);
            if (min != max)
            {
                b.Append("-");
                AppendCharString(max, b);
            }
            b.Append(" -> ").Append(to.number);
            return b.ToString();
        }

        internal void AppendDot(StringBuilder b)
        {
            b.Append(" -> ").Append(to.number).Append(" [label=\"");
            AppendCharString(min, b);
            if (min != max)
            {
                b.Append("-");
                AppendCharString(max, b);
            }
            b.Append("\"]\n");
        }

        private sealed class CompareByDestThenMinMaxSingle : IComparer<Transition>
        {
            public int Compare(Transition t1, Transition t2)
            {
                if (t1.to != t2.to)
                {
                    if (t1.to.number < t2.to.number) return -1;
                    else if (t1.to.number > t2.to.number) return 1;
                }
                if (t1.min < t2.min) return -1;
                if (t1.min > t2.min) return 1;
                if (t1.max > t2.max) return -1;
                if (t1.max < t2.max) return 1;
                return 0;
            }
        }

        public static readonly IComparer<Transition> CompareByDestThenMinMax = new CompareByDestThenMinMaxSingle();

        private sealed class CompareByMinMaxThenDestSingle : IComparer<Transition>
        {
            public int Compare(Transition t1, Transition t2)
            {
                if (t1.min < t2.min) return -1;
                if (t1.min > t2.min) return 1;
                if (t1.max > t2.max) return -1;
                if (t1.max < t2.max) return 1;
                if (t1.to != t2.to)
                {
                    if (t1.to.number < t2.to.number) return -1;
                    if (t1.to.number > t2.to.number) return 1;
                }
                return 0;
            }
        }

        public static readonly IComparer<Transition> CompareByMinMaxThenDest = new CompareByMinMaxThenDestSingle();
    }
}
