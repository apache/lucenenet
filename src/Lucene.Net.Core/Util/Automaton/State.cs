using System;
using System.Collections.Generic;
using System.Diagnostics;
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
    /// <summary>
    /// <tt>Automaton</tt> state.
    ///
    /// @lucene.experimental
    /// </summary>
    public class State : IComparable<State>
    {
        internal bool accept;
        public Transition[] TransitionsArray; // LUCENENET TODO: make property ?
        internal int numTransitions; // LUCENENET NOTE: Made internal because we already have a public property for access

        internal int number;

        internal int Id;
        internal static int Next_id;

        /// <summary>
        /// Constructs a new state. Initially, the new state is a reject state.
        /// </summary>
        public State()
        {
            ResetTransitions();
            Id = Next_id++;
        }

        /// <summary>
        /// Resets transition set.
        /// </summary>
        internal void ResetTransitions()
        {
            TransitionsArray = new Transition[0];
            numTransitions = 0;
        }

        private class TransitionsIterable : IEnumerable<Transition>
        {
            private readonly State OuterInstance;

            public TransitionsIterable(State outerInstance)
            {
                this.OuterInstance = outerInstance;
            }

            public virtual IEnumerator<Transition> GetEnumerator()
            {
                return new IteratorAnonymousInnerClassHelper(this);
            }

            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            private class IteratorAnonymousInnerClassHelper : IEnumerator<Transition>
            {
                private readonly TransitionsIterable OuterInstance;
                private Transition current;
                private int i, upTo;

                public IteratorAnonymousInnerClassHelper(TransitionsIterable outerInstance)
                {
                    this.OuterInstance = outerInstance;
                    upTo = OuterInstance.OuterInstance.numTransitions;
                    i = 0;
                }

                public bool MoveNext()
                {
                    if (i < upTo)
                    {
                        current = OuterInstance.OuterInstance.TransitionsArray[i++];
                        return true;
                    }
                    return false;
                }

                public Transition Current
                {
                    get
                    {
                        return current;
                    }
                }

                object System.Collections.IEnumerator.Current
                {
                    get
                    {
                        return Current;
                    }
                }

                public void Reset()
                {
                    throw new NotImplementedException();
                }

                public void Dispose()
                {
                }
            }
        }

        /// <summary>
        /// Returns the set of outgoing transitions. Subsequent changes are reflected
        /// in the automaton.
        /// </summary>
        /// <returns> transition set </returns>
        public virtual IEnumerable<Transition> Transitions // LUCENENET TODO: Make GetTransitions(), SetTransitions() (conversion, new instance)
        {
            get
            {
                return new TransitionsIterable(this);
            }
            set
            {
                this.TransitionsArray = value.ToArray();
                this.numTransitions = value.ToArray().Length;
            }
        }

        public virtual int NumTransitions
        {
            get { return numTransitions; }
        }

        /// <summary>
        /// Adds an outgoing transition.
        /// </summary>
        /// <param name="t"> transition </param>
        public virtual void AddTransition(Transition t)
        {
            if (numTransitions == TransitionsArray.Length)
            {
                Transition[] newArray = new Transition[ArrayUtil.Oversize(1 + numTransitions, RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
                Array.Copy(TransitionsArray, 0, newArray, 0, numTransitions);
                TransitionsArray = newArray;
            }
            TransitionsArray[numTransitions++] = t;
        }

        /// <summary>
        /// Sets acceptance for this state.
        /// </summary>
        /// <param name="accept"> if true, this state is an accept state </param>
        public virtual bool Accept
        {
            set
            {
                this.accept = value;
            }
            get
            {
                return accept;
            }
        }

        /// <summary>
        /// Performs lookup in transitions, assuming determinism.
        /// </summary>
        /// <param name="c"> codepoint to look up </param>
        /// <returns> destination state, null if no matching outgoing transition </returns>
        /// <seealso cref= #step(int, Collection) </seealso>
        public virtual State Step(int c)
        {
            Debug.Assert(c >= 0);
            for (int i = 0; i < numTransitions; i++)
            {
                Transition t = TransitionsArray[i];
                if (t.Min_Renamed <= c && c <= t.Max_Renamed)
                {
                    return t.To;
                }
            }
            return null;
        }

        /// <summary>
        /// Performs lookup in transitions, allowing nondeterminism.
        /// </summary>
        /// <param name="c"> codepoint to look up </param>
        /// <param name="dest"> collection where destination states are stored </param>
        /// <seealso cref= #step(int) </seealso>
        public virtual void Step(int c, ICollection<State> dest)
        {
            for (int i = 0; i < numTransitions; i++)
            {
                Transition t = TransitionsArray[i];
                if (t.Min_Renamed <= c && c <= t.Max_Renamed)
                {
                    dest.Add(t.To);
                }
            }
        }

        /// <summary>
        /// Virtually adds an epsilon transition to the target
        ///  {@code to} state.  this is implemented by copying all
        ///  transitions from {@code to} to this state, and if {@code
        ///  to} is an accept state then set accept for this state.
        /// </summary>
        internal virtual void AddEpsilon(State to)
        {
            if (to.accept)
            {
                accept = true;
            }
            foreach (Transition t in to.Transitions)
            {
                AddTransition(t);
            }
        }

        /// <summary>
        /// Downsizes transitionArray to numTransitions </summary>
        public virtual void TrimTransitionsArray()
        {
            if (numTransitions < TransitionsArray.Length)
            {
                Transition[] newArray = new Transition[numTransitions];
                Array.Copy(TransitionsArray, 0, newArray, 0, numTransitions);
                TransitionsArray = newArray;
            }
        }

        /// <summary>
        /// Reduces this state. A state is "reduced" by combining overlapping
        /// and adjacent edge intervals with same destination.
        /// </summary>
        public virtual void Reduce()
        {
            if (numTransitions <= 1)
            {
                return;
            }
            SortTransitions(Transition.CompareByDestThenMinMax);
            State p = null;
            int min = -1, max = -1;
            int upto = 0;
            for (int i = 0; i < numTransitions; i++)
            {
                Transition t = TransitionsArray[i];
                if (p == t.To)
                {
                    if (t.Min_Renamed <= max + 1)
                    {
                        if (t.Max_Renamed > max)
                        {
                            max = t.Max_Renamed;
                        }
                    }
                    else
                    {
                        if (p != null)
                        {
                            TransitionsArray[upto++] = new Transition(min, max, p);
                        }
                        min = t.Min_Renamed;
                        max = t.Max_Renamed;
                    }
                }
                else
                {
                    if (p != null)
                    {
                        TransitionsArray[upto++] = new Transition(min, max, p);
                    }
                    p = t.To;
                    min = t.Min_Renamed;
                    max = t.Max_Renamed;
                }
            }

            if (p != null)
            {
                TransitionsArray[upto++] = new Transition(min, max, p);
            }
            numTransitions = upto;
        }

        /// <summary>
        /// Returns sorted list of outgoing transitions.
        /// </summary>
        /// <param name="to_first"> if true, order by (to, min, reverse max); otherwise (min,
        ///          reverse max, to) </param>
        /// <returns> transition list </returns>

        /// <summary>
        /// Sorts transitions array in-place. </summary>
        public virtual void SortTransitions(IComparer<Transition> comparator)
        {
            // mergesort seems to perform better on already sorted arrays:
            if (numTransitions > 1)
            {
                ArrayUtil.TimSort(TransitionsArray, 0, numTransitions, comparator);
            }
        }

        /// <summary>
        /// Return this state's number.
        /// <p>
        /// Expert: Will be useless unless <seealso cref="Automaton#getNumberedStates"/>
        /// has been called first to number the states. </summary>
        /// <returns> the number </returns>
        public virtual int Number
        {
            get
            {
                return number;
            }
        }

        /// <summary>
        /// Returns string describing this state. Normally invoked via
        /// <seealso cref="Automaton#toString()"/>.
        /// </summary>
        public override string ToString()
        {
            StringBuilder b = new StringBuilder();
            b.Append("state ").Append(number);
            if (accept)
            {
                b.Append(" [accept]");
            }
            else
            {
                b.Append(" [reject]");
            }
            b.Append(":\n");
            foreach (Transition t in Transitions)
            {
                b.Append("  ").Append(t.ToString()).Append("\n");
            }
            return b.ToString();
        }

        /// <summary>
        /// Compares this object with the specified object for order. States are
        /// ordered by the time of construction.
        /// </summary>
        public virtual int CompareTo(State s)
        {
            return s.Id - Id;
        }

        public override int GetHashCode()
        {
            return Id;
        }
    }
}