using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    /// <summary>
    /// <tt>Automaton</tt> transition.
    /// <p>
    /// A transition, which belongs to a source state, consists of a Unicode
    /// codepoint interval and a destination state.
    ///
    /// @lucene.experimental
    /// </summary>
    public class Transition
    {
        /*
         * CLASS INVARIANT: min<=max
         */

        internal readonly int Min_Renamed;
        internal readonly int Max_Renamed;
        internal readonly State To;

        /// <summary>
        /// Constructs a new singleton interval transition.
        /// </summary>
        /// <param name="c"> transition codepoint </param>
        /// <param name="to"> destination state </param>
        public Transition(int c, State to)
        {
            Debug.Assert(c >= 0);
            Min_Renamed = Max_Renamed = c;
            this.To = to;
        }

        /// <summary>
        /// Constructs a new transition. Both end points are included in the interval.
        /// </summary>
        /// <param name="min"> transition interval minimum </param>
        /// <param name="max"> transition interval maximum </param>
        /// <param name="to"> destination state </param>
        public Transition(int min, int max, State to)
        {
            Debug.Assert(min >= 0);
            Debug.Assert(max >= 0);
            if (max < min)
            {
                int t = max;
                max = min;
                min = t;
            }
            this.Min_Renamed = min;
            this.Max_Renamed = max;
            this.To = to;
        }

        /// <summary>
        /// Returns minimum of this transition interval. </summary>
        public virtual int Min
        {
            get
            {
                return Min_Renamed;
            }
        }

        /// <summary>
        /// Returns maximum of this transition interval. </summary>
        public virtual int Max
        {
            get
            {
                return Max_Renamed;
            }
        }

        /// <summary>
        /// Returns destination of this transition. </summary>
        public virtual State Dest
        {
            get
            {
                return To;
            }
        }

        /// <summary>
        /// Checks for equality.
        /// </summary>
        /// <param name="obj"> object to compare with </param>
        /// <returns> true if <tt>obj</tt> is a transition with same character interval
        ///         and destination state as this transition. </returns>
        public override bool Equals(object obj)
        {
            if (obj is Transition)
            {
                Transition t = (Transition)obj;
                return t.Min_Renamed == Min_Renamed && t.Max_Renamed == Max_Renamed && t.To == To;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Returns hash code. The hash code is based on the character interval (not
        /// the destination state).
        /// </summary>
        /// <returns> hash code </returns>
        public override int GetHashCode()
        {
            return Min_Renamed * 2 + Max_Renamed * 3;
        }

        /// <summary>
        /// Clones this transition.
        /// </summary>
        /// <returns> clone with same character interval and destination state </returns>
        public virtual object Clone()
        {
            return (Transition)base.MemberwiseClone();
        }

        internal static void AppendCharString(int c, StringBuilder b)
        {
            if (c >= 0x21 && c <= 0x7e && c != '\\' && c != '"')
            {
                b.Append(c);
            }
            else
            {
                b.Append("\\\\U");
                string s = c.ToString("x");
                if (c < 0x10)
                {
                    b.Append("0000000").Append(s);
                }
                else if (c < 0x100)
                {
                    b.Append("000000").Append(s);
                }
                else if (c < 0x1000)
                {
                    b.Append("00000").Append(s);
                }
                else if (c < 0x10000)
                {
                    b.Append("0000").Append(s);
                }
                else if (c < 0x100000)
                {
                    b.Append("000").Append(s);
                }
                else if (c < 0x1000000)
                {
                    b.Append("00").Append(s);
                }
                else if (c < 0x10000000)
                {
                    b.Append("0").Append(s);
                }
                else
                {
                    b.Append(s);
                }
            }
        }

        /// <summary>
        /// Returns a string describing this state. Normally invoked via
        /// <seealso cref="Automaton#toString()"/>.
        /// </summary>
        public override string ToString()
        {
            StringBuilder b = new StringBuilder();
            AppendCharString(Min_Renamed, b);
            if (Min_Renamed != Max_Renamed)
            {
                b.Append("-");
                AppendCharString(Max_Renamed, b);
            }
            b.Append(" -> ").Append(To.number);
            return b.ToString();
        }

        internal virtual void AppendDot(StringBuilder b)
        {
            b.Append(" -> ").Append(To.number).Append(" [label=\"");
            AppendCharString(Min_Renamed, b);
            if (Min_Renamed != Max_Renamed)
            {
                b.Append("-");
                AppendCharString(Max_Renamed, b);
            }
            b.Append("\"]\n");
        }

        private sealed class CompareByDestThenMinMaxSingle : IComparer<Transition>
        {
            public int Compare(Transition t1, Transition t2)
            {
                if (t1.To != t2.To)
                {
                    if (t1.To.number < t2.To.number)
                    {
                        return -1;
                    }
                    else if (t1.To.number > t2.To.number)
                    {
                        return 1;
                    }
                }
                if (t1.Min_Renamed < t2.Min_Renamed)
                {
                    return -1;
                }
                if (t1.Min_Renamed > t2.Min_Renamed)
                {
                    return 1;
                }
                if (t1.Max_Renamed > t2.Max_Renamed)
                {
                    return -1;
                }
                if (t1.Max_Renamed < t2.Max_Renamed)
                {
                    return 1;
                }
                return 0;
            }
        }

        public static readonly IComparer<Transition> CompareByDestThenMinMax = new CompareByDestThenMinMaxSingle();

        private sealed class CompareByMinMaxThenDestSingle : IComparer<Transition>
        {
            public int Compare(Transition t1, Transition t2)
            {
                if (t1.Min_Renamed < t2.Min_Renamed)
                {
                    return -1;
                }
                if (t1.Min_Renamed > t2.Min_Renamed)
                {
                    return 1;
                }
                if (t1.Max_Renamed > t2.Max_Renamed)
                {
                    return -1;
                }
                if (t1.Max_Renamed < t2.Max_Renamed)
                {
                    return 1;
                }
                if (t1.To != t2.To)
                {
                    if (t1.To.number < t2.To.number)
                    {
                        return -1;
                    }
                    if (t1.To.number > t2.To.number)
                    {
                        return 1;
                    }
                }
                return 0;
            }
        }

        public static readonly IComparer<Transition> CompareByMinMaxThenDest = new CompareByMinMaxThenDestSingle();
    }
}