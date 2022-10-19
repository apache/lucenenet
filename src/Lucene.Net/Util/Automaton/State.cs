using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
    /// <see cref="Automaton"/> state.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public class State : IComparable<State>
    {
        internal bool accept;
        [WritableArray]
        [SuppressMessage("Microsoft.Performance", "CA1819", Justification = "Lucene's design requires some writable array properties")]
        public Transition[] TransitionsArray => transitionsArray;

        // LUCENENET NOTE: Setter removed because it is apparently not in use outside of this class
        private Transition[] transitionsArray = Arrays.Empty<Transition>();

        internal int numTransitions = 0;// LUCENENET NOTE: Made internal because we already have a public property for access

        internal int number;

        internal int id;
        internal static int next_id;

        /// <summary>
        /// Constructs a new state. Initially, the new state is a reject state.
        /// </summary>
        public State()
        {
            //ResetTransitions(); // LUCENENET: Let class initializer set these
            id = next_id++;
        }

        /// <summary>
        /// Resets transition set.
        /// </summary>
        internal void ResetTransitions()
        {
            transitionsArray = Arrays.Empty<Transition>();
            numTransitions = 0;
        }

        internal class TransitionsEnumerable : IEnumerable<Transition>
        {
            private readonly State outerInstance;

            public TransitionsEnumerable(State outerInstance)
            {
                this.outerInstance = outerInstance;
            }

            public virtual IEnumerator<Transition> GetEnumerator()
            {
                return new TransitionsEnumerator(this);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            private struct TransitionsEnumerator : IEnumerator<Transition>
            {
                private readonly TransitionsEnumerable outerInstance;
                private Transition current;
                private int i;
                private readonly int upTo;

                public TransitionsEnumerator(TransitionsEnumerable outerInstance)
                {
                    this.outerInstance = outerInstance;
                    upTo = this.outerInstance.outerInstance.numTransitions;
                    i = 0;
                    current = default;
                }

                public bool MoveNext()
                {
                    if (i < upTo)
                    {
                        current = outerInstance.outerInstance.transitionsArray[i++];
                        return true;
                    }
                    return false;
                }

                public Transition Current => current;

                object IEnumerator.Current => Current;

                public void Reset()
                {
                    throw UnsupportedOperationException.Create();
                }

                public void Dispose()
                {
                    // LUCENENET: Intentionally blank
                }
            }
        }

        /// <summary>
        /// Returns the set of outgoing transitions. Subsequent changes are reflected
        /// in the automaton.
        /// </summary>
        /// <returns> Transition set. </returns>
        public virtual IEnumerable<Transition> GetTransitions()
        {
            return new TransitionsEnumerable(this);
        }

        public virtual int NumTransitions => numTransitions;

        public virtual void SetTransitions(Transition[] transitions)
        {
            this.numTransitions = transitions.Length;
            this.transitionsArray = transitions;
        }

        /// <summary>
        /// Adds an outgoing transition.
        /// </summary>
        /// <param name="t"> Transition. </param>
        public virtual void AddTransition(Transition t)
        {
            if (numTransitions == transitionsArray.Length)
            {
                // LUCENENET: Resize rather than copy
                Array.Resize(ref transitionsArray, ArrayUtil.Oversize(1 + numTransitions, RamUsageEstimator.NUM_BYTES_OBJECT_REF));
            }
            transitionsArray[numTransitions++] = t;
        }

        /// <summary>
        /// Sets acceptance for this state. If <c>true</c>, this state is an accept state.
        /// </summary>
        public virtual bool Accept
        {
            get => accept;
            set => this.accept = value;
        }

        /// <summary>
        /// Performs lookup in transitions, assuming determinism.
        /// </summary>
        /// <param name="c"> Codepoint to look up. </param>
        /// <returns> Destination state, <c>null</c> if no matching outgoing transition. </returns>
        /// <seealso cref="Step(int, ICollection{State})"/>
        public virtual State Step(int c)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(c >= 0);
            for (int i = 0; i < numTransitions; i++)
            {
                Transition t = transitionsArray[i];
                if (t.min <= c && c <= t.max)
                {
                    return t.to;
                }
            }
            return null;
        }

        /// <summary>
        /// Performs lookup in transitions, allowing nondeterminism.
        /// </summary>
        /// <param name="c"> Codepoint to look up. </param>
        /// <param name="dest"> Collection where destination states are stored. </param>
        /// <seealso cref="Step(int)"/>
        public virtual void Step(int c, ICollection<State> dest)
        {
            for (int i = 0; i < numTransitions; i++)
            {
                Transition t = transitionsArray[i];
                if (t.min <= c && c <= t.max)
                {
                    dest.Add(t.to);
                }
            }
        }

        /// <summary>
        /// Virtually adds an epsilon transition to the target
        /// <paramref name="to"/> state.  this is implemented by copying all
        /// transitions from <paramref name="to"/> to this state, and if 
        /// <paramref name="to"/> is an accept state then set accept for this state.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal virtual void AddEpsilon(State to)
        {
            if (to.accept)
            {
                accept = true;
            }
            foreach (Transition t in to.GetTransitions())
            {
                AddTransition(t);
            }
        }

        /// <summary>
        /// Downsizes transitionArray to numTransitions. </summary>
        public virtual void TrimTransitionsArray()
        {
            if (numTransitions < transitionsArray.Length)
            {
                Array.Resize(ref transitionsArray, numTransitions); // LUCENENET: Resize rather than copy
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
            SortTransitions(Transition.COMPARE_BY_DEST_THEN_MIN_MAX);
            State p = null;
            int min = -1, max = -1;
            int upto = 0;
            for (int i = 0; i < numTransitions; i++)
            {
                Transition t = transitionsArray[i];
                if (p == t.to)
                {
                    if (t.min <= max + 1)
                    {
                        if (t.max > max)
                        {
                            max = t.max;
                        }
                    }
                    else
                    {
                        if (p != null)
                        {
                            transitionsArray[upto++] = new Transition(min, max, p);
                        }
                        min = t.min;
                        max = t.max;
                    }
                }
                else
                {
                    if (p != null)
                    {
                        transitionsArray[upto++] = new Transition(min, max, p);
                    }
                    p = t.to;
                    min = t.min;
                    max = t.max;
                }
            }

            if (p != null)
            {
                transitionsArray[upto++] = new Transition(min, max, p);
            }
            numTransitions = upto;
        }

        /// <summary>
        /// Returns sorted list of outgoing transitions.
        /// </summary>
        /// <param name="comparer"> Comparer to sort with. </param>
        /// <returns> Transition list. </returns>

        /// <summary>
        /// Sorts transitions array in-place. </summary>
        public virtual void SortTransitions(IComparer<Transition> comparer)
        {
            // mergesort seems to perform better on already sorted arrays:
            if (numTransitions > 1)
            {
                ArrayUtil.TimSort(transitionsArray, 0, numTransitions, comparer);
            }
        }

        /// <summary>
        /// Return this state's number.
        /// <para/>
        /// Expert: Will be useless unless <see cref="Automaton.GetNumberedStates()"/>
        /// has been called first to number the states. </summary>
        /// <returns> The number. </returns>
        public virtual int Number => number;

        /// <summary>
        /// Returns string describing this state. Normally invoked via
        /// <see cref="Automaton.ToString()"/>.
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
            foreach (Transition t in GetTransitions())
            {
                b.Append("  ").Append(t.ToString()).Append('\n');
            }
            return b.ToString();
        }

        /// <summary>
        /// Compares this object with the specified object for order. States are
        /// ordered by the time of construction.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual int CompareTo(State s)
        {
            return s.id - id;
        }

        // LUCENENET NOTE: DO NOT IMPLEMENT Equals()!!!
        // Although it doesn't match GetHashCode(), checking for
        // reference equality is by design.
        // Implementing Equals() causes difficult to diagnose
        // IndexOutOfRangeExceptions when using FuzzyTermsEnum.
        // See GH-296.

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return id;
        }
    }
}