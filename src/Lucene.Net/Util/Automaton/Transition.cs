using J2N.Text;
using Lucene.Net.Diagnostics;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
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
    /// <see cref="Automaton"/> transition.
    /// <para/>
    /// A transition, which belongs to a source state, consists of a Unicode
    /// codepoint interval and a destination state.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public class Transition // LUCENENET specific: Not implementing ICloneable per Microsoft's recommendation
    {
        /*
         * CLASS INVARIANT: min<=max
         */

        internal readonly int min;
        internal readonly int max;
        internal readonly State to;

        /// <summary>
        /// Constructs a new singleton interval transition.
        /// </summary>
        /// <param name="c"> Transition codepoint. </param>
        /// <param name="to"> Destination state. </param>
        public Transition(int c, State to)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(c >= 0);
            min = max = c;
            this.to = to;
        }

        /// <summary>
        /// Constructs a new transition. Both end points are included in the interval.
        /// </summary>
        /// <param name="min"> Transition interval minimum. </param>
        /// <param name="max"> Transition interval maximum. </param>
        /// <param name="to"> Destination state. </param>
        public Transition(int min, int max, State to)
        {
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(min >= 0);
                Debugging.Assert(max >= 0);
            }
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

        /// <summary>
        /// Returns minimum of this transition interval. </summary>
        public virtual int Min => min;

        /// <summary>
        /// Returns maximum of this transition interval. </summary>
        public virtual int Max => max;

        /// <summary>
        /// Returns destination of this transition. </summary>
        public virtual State Dest => to;

        /// <summary>
        /// Checks for equality.
        /// </summary>
        /// <param name="obj"> Object to compare with. </param>
        /// <returns> <c>true</c> if <paramref name="obj"/> is a transition with same character interval
        ///         and destination state as this transition. </returns>
        public override bool Equals(object obj)
        {
            if (obj is Transition t)
            {
                return t.min == min && t.max == max && t.to == to;
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
        /// <returns> Hash code. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return min * 2 + max * 3;
        }

        /// <summary>
        /// Clones this transition.
        /// </summary>
        /// <returns> Clone with same character interval and destination state. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual object Clone()
        {
            return (Transition)base.MemberwiseClone();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void AppendCharString(int c, StringBuilder b)
        {
            if (c >= 0x21 && c <= 0x7e && c != '\\' && c != '"')
            {
                b.AppendCodePoint(c);
            }
            else
            {
                b.Append("\\\\U");
                string s = c.ToString("x", CultureInfo.InvariantCulture);
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
                    b.Append('0').Append(s);
                }
                else
                {
                    b.Append(s);
                }
            }
        }

        /// <summary>
        /// Returns a string describing this state. Normally invoked via
        /// <seealso cref="Automaton.ToString()"/>.
        /// </summary>
        public override string ToString()
        {
            StringBuilder b = new StringBuilder();
            AppendCharString(min, b);
            if (min != max)
            {
                b.Append('-');
                AppendCharString(max, b);
            }
            b.Append(" -> ").Append(to.number);
            return b.ToString();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void AppendDot(StringBuilder b)
        {
            b.Append(" -> ").Append(to.number).Append(" [label=\"");
            AppendCharString(min, b);
            if (min != max)
            {
                b.Append('-');
                AppendCharString(max, b);
            }
            b.Append("\"]\n");
        }

        private sealed class CompareByDestThenMinMaxSingle : IComparer<Transition>
        {
            public int Compare(Transition t1, Transition t2)
            {
                //if (t1.to != t2.to)
                if (!ReferenceEquals(t1.to, t2.to))
                {
                    if (t1.to.number < t2.to.number)
                    {
                        return -1;
                    }
                    else if (t1.to.number > t2.to.number)
                    {
                        return 1;
                    }
                }
                if (t1.min < t2.min)
                {
                    return -1;
                }
                if (t1.min > t2.min)
                {
                    return 1;
                }
                if (t1.max > t2.max)
                {
                    return -1;
                }
                if (t1.max < t2.max)
                {
                    return 1;
                }
                return 0;
            }
        }
        
        // LUCENENET NOTE: Renamed to follow convention of static fields/constants
        public static readonly IComparer<Transition> COMPARE_BY_DEST_THEN_MIN_MAX = new CompareByDestThenMinMaxSingle();

        private sealed class CompareByMinMaxThenDestSingle : IComparer<Transition>
        {
            public int Compare(Transition t1, Transition t2)
            {
                if (t1.min < t2.min)
                {
                    return -1;
                }
                if (t1.min > t2.min)
                {
                    return 1;
                }
                if (t1.max > t2.max)
                {
                    return -1;
                }
                if (t1.max < t2.max)
                {
                    return 1;
                }
                //if (t1.to != t2.to)
                if (!ReferenceEquals(t1.to, t2.to))
                {
                    if (t1.to.number < t2.to.number)
                    {
                        return -1;
                    }
                    if (t1.to.number > t2.to.number)
                    {
                        return 1;
                    }
                }
                return 0;
            }
        }

        // LUCENENET NOTE: Renamed to follow convention of static fields/constants
        public static readonly IComparer<Transition> COMPARE_BY_MIN_MAX_THEN_DEST = new CompareByMinMaxThenDestSingle();
    }
}