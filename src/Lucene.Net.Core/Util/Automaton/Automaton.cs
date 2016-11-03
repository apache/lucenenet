using Lucene.Net.Support;
using System;
using System.Collections;
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
    /// Finite-state automaton with regular expression operations.
    /// <p>
    /// Class invariants:
    /// <ul>
    /// <li>An automaton is either represented explicitly (with <seealso cref="State"/> and
    /// <seealso cref="Transition"/> objects) or with a singleton string (see
    /// <seealso cref="#getSingleton()"/> and <seealso cref="#expandSingleton()"/>) in case the automaton
    /// is known to accept exactly one string. (Implicitly, all states and
    /// transitions of an automaton are reachable from its initial state.)
    /// <li>Automata are always reduced (see <seealso cref="#reduce()"/>) and have no
    /// transitions to dead states (see <seealso cref="#removeDeadTransitions()"/>).
    /// <li>If an automaton is nondeterministic, then <seealso cref="#isDeterministic()"/>
    /// returns false (but the converse is not required).
    /// <li>Automata provided as input to operations are generally assumed to be
    /// disjoint.
    /// </ul>
    /// <p>
    /// If the states or transitions are manipulated manually, the
    /// <seealso cref="#restoreInvariant()"/> and <seealso cref="#setDeterministic(boolean)"/> methods
    /// should be used afterwards to restore representation invariants that are
    /// assumed by the built-in automata operations.
    ///
    /// <p>
    /// <p>
    /// Note: this class has internal mutable state and is not thread safe. It is
    /// the caller's responsibility to ensure any necessary synchronization if you
    /// wish to use the same Automaton from multiple threads. In general it is instead
    /// recommended to use a <seealso cref="RunAutomaton"/> for multithreaded matching: it is immutable,
    /// thread safe, and much faster.
    /// </p>
    /// @lucene.experimental
    /// </summary>
    public class Automaton
    {
        /// <summary>
        /// Minimize using Hopcroft's O(n log n) algorithm. this is regarded as one of
        /// the most generally efficient algorithms that exist.
        /// </summary>
        /// <seealso cref= #setMinimization(int) </seealso>
        public const int MINIMIZE_HOPCROFT = 2;

        /// <summary>
        /// Selects minimization algorithm (default: <code>MINIMIZE_HOPCROFT</code>). </summary>
        internal static int Minimization_Renamed = MINIMIZE_HOPCROFT;

        /// <summary>
        /// Initial state of this automaton. </summary>
        internal State Initial;

        /// <summary>
        /// If true, then this automaton is definitely deterministic (i.e., there are
        /// no choices for any run, but a run may crash).
        /// </summary>
        internal bool deterministic;

        /// <summary>
        /// Extra data associated with this automaton. </summary>
        
        internal object info;

        /// <summary>
        /// Hash code. Recomputed by <seealso cref="MinimizationOperations#minimize(Automaton)"/>
        /// </summary>
        //int hash_code;

        /// <summary>
        /// Singleton string. Null if not applicable. </summary>
        internal string singleton;

        /// <summary>
        /// Minimize always flag. </summary>
        internal static bool Minimize_always = false;

        /// <summary>
        /// Selects whether operations may modify the input automata (default:
        /// <code>false</code>).
        /// </summary>
        internal static bool Allow_mutation = false;

        /// <summary>
        /// Constructs a new automaton that accepts the empty language. Using this
        /// constructor, automata can be constructed manually from <seealso cref="State"/> and
        /// <seealso cref="Transition"/> objects.
        /// </summary>
        /// <seealso cref= State </seealso>
        /// <seealso cref= Transition </seealso>
        public Automaton(State initial)
        {
            this.Initial = initial;
            deterministic = true;
            singleton = null;
        }

        public Automaton()
            : this(new State())
        {
        }

        /// <summary>
        /// Selects minimization algorithm (default: <code>MINIMIZE_HOPCROFT</code>).
        /// </summary>
        /// <param name="algorithm"> minimization algorithm </param>
        public static int Minimization
        {
            set
            {
                Minimization_Renamed = value;
            }
        }

        /// <summary>
        /// Sets or resets minimize always flag. If this flag is set, then
        /// <seealso cref="MinimizationOperations#minimize(Automaton)"/> will automatically be
        /// invoked after all operations that otherwise may produce non-minimal
        /// automata. By default, the flag is not set.
        /// </summary>
        /// <param name="flag"> if true, the flag is set </param>
        public static bool MinimizeAlways
        {
            set
            {
                Minimize_always = value;
            }
        }

        /// <summary>
        /// Sets or resets allow mutate flag. If this flag is set, then all automata
        /// operations may modify automata given as input; otherwise, operations will
        /// always leave input automata languages unmodified. By default, the flag is
        /// not set.
        /// </summary>
        /// <param name="flag"> if true, the flag is set </param>
        /// <returns> previous value of the flag </returns>
        public static bool SetAllowMutate(bool flag)
        {
            bool b = Allow_mutation;
            Allow_mutation = flag;
            return b;
        }

        /// <summary>
        /// Returns the state of the allow mutate flag. If this flag is set, then all
        /// automata operations may modify automata given as input; otherwise,
        /// operations will always leave input automata languages unmodified. By
        /// default, the flag is not set.
        /// </summary>
        /// <returns> current value of the flag </returns>
        internal static bool AllowMutate
        {
            get
            {
                return Allow_mutation;
            }
            set
            {
                Allow_mutation = value;
            }
        }

        internal virtual void CheckMinimizeAlways()
        {
            if (Minimize_always)
            {
                MinimizationOperations.Minimize(this);
            }
        }

        public bool IsSingleton
        {
            get
            {
                return Singleton != null;
            }
        }

        /// <summary>
        /// Returns the singleton string for this automaton. An automaton that accepts
        /// exactly one string <i>may</i> be represented in singleton mode. In that
        /// case, this method may be used to obtain the string.
        /// </summary>
        /// <returns> string, null if this automaton is not in singleton mode. </returns>
        public virtual string Singleton
        {
            get
            {
                return singleton;
            }
        }

        /// <summary>
        /// Sets initial state.
        /// </summary>
        /// <param name="s"> state </param>
        /*
        public void setInitialState(State s) {
          initial = s;
          singleton = null;
        }
        */

        /// <summary>
        /// Gets initial state.
        /// </summary>
        /// <returns> state </returns>
        public virtual State InitialState
        {
            get
            {
                ExpandSingleton();
                return Initial;
            }

            set { Initial = value; }
        }

        /// <summary>
        /// Returns deterministic flag for this automaton.
        /// </summary>
        /// <returns> true if the automaton is definitely deterministic, false if the
        ///         automaton may be nondeterministic </returns>
        public virtual bool Deterministic
        {
            get
            {
                return deterministic;
            }
            set
            {
                this.deterministic = value;
            }
        }

        /// <summary>
        /// Associates extra information with this automaton.
        /// </summary>
        /// <param name="info"> extra information </param>
        public virtual object Info
        {
            set
            {
                this.info = value;
            }
            get
            {
                return info;
            }
        }

        // cached
        private State[] NumberedStates_Renamed;

        public virtual State[] NumberedStates
        {
            get
            {
                if (NumberedStates_Renamed == null)
                {
                    ExpandSingleton();
                    HashSet<State> visited = new HashSet<State>();
                    LinkedList<State> worklist = new LinkedList<State>();
                    State[] states = new State[4];
                    int upto = 0;
                    worklist.AddLast(Initial);
                    visited.Add(Initial);
                    Initial.number = upto;
                    states[upto] = Initial;
                    upto++;
                    while (worklist.Count > 0)
                    {
                        State s = worklist.First.Value;
                        worklist.RemoveFirst();
                        for (int i = 0; i < s.numTransitions; i++)
                        {
                            Transition t = s.TransitionsArray[i];
                            if (!visited.Contains(t.To))
                            {
                                visited.Add(t.To);
                                worklist.AddLast(t.To);
                                t.To.Number = upto;
                                if (upto == states.Length)
                                {
                                    State[] newArray = new State[ArrayUtil.Oversize(1 + upto, RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
                                    Array.Copy(states, 0, newArray, 0, upto);
                                    states = newArray;
                                }
                                states[upto] = t.To;
                                upto++;
                            }
                        }
                    }
                    if (states.Length != upto)
                    {
                        State[] newArray = new State[upto];
                        Array.Copy(states, 0, newArray, 0, upto);
                        states = newArray;
                    }
                    NumberedStates_Renamed = states;
                }

                return NumberedStates_Renamed;
            }
            set
            {
                SetNumberedStates(value, value.Length);
            }
        }

        public virtual void SetNumberedStates(State[] states, int count)
        {
            Debug.Assert(count <= states.Length);
            // TODO: maybe we can eventually allow for oversizing here...
            if (count < states.Length)
            {
                State[] newArray = new State[count];
                Array.Copy(states, 0, newArray, 0, count);
                NumberedStates_Renamed = newArray;
            }
            else
            {
                NumberedStates_Renamed = states;
            }
        }

        public virtual void ClearNumberedStates()
        {
            NumberedStates_Renamed = null;
        }

        /// <summary>
        /// Returns the set of reachable accept states.
        /// </summary>
        /// <returns> set of <seealso cref="State"/> objects </returns>
        public virtual ISet<State> AcceptStates
        {
            get
            {
                ExpandSingleton();
                HashSet<State> accepts = new HashSet<State>();
                HashSet<State> visited = new HashSet<State>();
                LinkedList<State> worklist = new LinkedList<State>();
                worklist.AddLast(Initial);
                visited.Add(Initial);
                while (worklist.Count > 0)
                {
                    State s = worklist.First.Value;
                    worklist.RemoveFirst();
                    if (s.accept)
                    {
                        accepts.Add(s);
                    }
                    foreach (Transition t in s.Transitions)
                    {
                        if (!visited.Contains(t.To))
                        {
                            visited.Add(t.To);
                            worklist.AddLast(t.To);
                        }
                    }
                }
                return accepts;
            }
        }

        /// <summary>
        /// Adds transitions to explicit crash state to ensure that transition function
        /// is total.
        /// </summary>
        internal virtual void Totalize()
        {
            State s = new State();
            s.AddTransition(new Transition(Character.MIN_CODE_POINT, Character.MAX_CODE_POINT, s));
            foreach (State p in NumberedStates)
            {
                int maxi = Character.MIN_CODE_POINT;
                p.SortTransitions(Transition.CompareByMinMaxThenDest);
                foreach (Transition t in p.Transitions)
                {
                    if (t.Min_Renamed > maxi)
                    {
                        p.AddTransition(new Transition(maxi, (t.Min_Renamed - 1), s));
                    }
                    if (t.Max_Renamed + 1 > maxi)
                    {
                        maxi = t.Max_Renamed + 1;
                    }
                }
                if (maxi <= Character.MAX_CODE_POINT)
                {
                    p.AddTransition(new Transition(maxi, Character.MAX_CODE_POINT, s));
                }
            }
            ClearNumberedStates();
        }

        /// <summary>
        /// Restores representation invariant. this method must be invoked before any
        /// built-in automata operation is performed if automaton states or transitions
        /// are manipulated manually.
        /// </summary>
        /// <seealso cref= #setDeterministic(boolean) </seealso>
        public virtual void RestoreInvariant()
        {
            RemoveDeadTransitions();
        }

        /// <summary>
        /// Reduces this automaton. An automaton is "reduced" by combining overlapping
        /// and adjacent edge intervals with same destination.
        /// </summary>
        public virtual void Reduce()
        {
            State[] states = NumberedStates;
            if (IsSingleton)
            {
                return;
            }
            foreach (State s in states)
            {
                s.Reduce();
            }
        }

        /// <summary>
        /// Returns sorted array of all interval start points.
        /// </summary>
        public virtual int[] StartPoints
        {
            get
            {
                State[] states = NumberedStates;
                HashSet<int> pointset = new HashSet<int>();
                pointset.Add(Character.MIN_CODE_POINT);
                foreach (State s in states)
                {
                    foreach (Transition t in s.Transitions)
                    {
                        pointset.Add(t.Min_Renamed);
                        if (t.Max_Renamed < Character.MAX_CODE_POINT)
                        {
                            pointset.Add((t.Max_Renamed + 1));
                        }
                    }
                }
                int[] points = new int[pointset.Count];
                int n = 0;
                foreach (int m in pointset)
                {
                    points[n++] = m;
                }
                Array.Sort(points);
                return points;
            }
        }

        /// <summary>
        /// Returns the set of live states. A state is "live" if an accept state is
        /// reachable from it.
        /// </summary>
        /// <returns> set of <seealso cref="State"/> objects </returns>
        private State[] LiveStates
        {
            get
            {
                State[] states = NumberedStates;
                HashSet<State> live = new HashSet<State>();
                foreach (State q in states)
                {
                    if (q.Accept)
                    {
                        live.Add(q);
                    }
                }
                // map<state, set<state>>
                ISet<State>[] map = new HashSet<State>[states.Length];
                for (int i = 0; i < map.Length; i++)
                {
                    map[i] = new HashSet<State>();
                }
                foreach (State s in states)
                {
                    for (int i = 0; i < s.numTransitions; i++)
                    {
                        map[s.TransitionsArray[i].To.Number].Add(s);
                    }
                }
                LinkedList<State> worklist = new LinkedList<State>(live);
                while (worklist.Count > 0)
                {
                    State s = worklist.First.Value;
                    worklist.RemoveFirst();
                    foreach (State p in map[s.number])
                    {
                        if (!live.Contains(p))
                        {
                            live.Add(p);
                            worklist.AddLast(p);
                        }
                    }
                }

                return live.ToArray(/*new State[live.Count]*/);
            }
        }

        /// <summary>
        /// Removes transitions to dead states and calls <seealso cref="#reduce()"/>.
        /// (A state is "dead" if no accept state is
        /// reachable from it.)
        /// </summary>
        public virtual void RemoveDeadTransitions()
        {
            State[] states = NumberedStates;
            //clearHashCode();
            if (IsSingleton)
            {
                return;
            }
            State[] live = LiveStates;

            BitArray liveSet = new BitArray(states.Length);
            foreach (State s in live)
            {
                liveSet.SafeSet(s.number, true);
            }

            foreach (State s in states)
            {
                // filter out transitions to dead states:
                int upto = 0;
                for (int i = 0; i < s.numTransitions; i++)
                {
                    Transition t = s.TransitionsArray[i];
                    if (liveSet.SafeGet(t.To.Number))
                    {
                        s.TransitionsArray[upto++] = s.TransitionsArray[i];
                    }
                }
                s.numTransitions = upto;
            }
            for (int i = 0; i < live.Length; i++)
            {
                live[i].number = i;
            }
            if (live.Length > 0)
            {
                NumberedStates = live;
            }
            else
            {
                // sneaky corner case -- if machine accepts no strings
                ClearNumberedStates();
            }
            Reduce();
        }

        /// <summary>
        /// Returns a sorted array of transitions for each state (and sets state
        /// numbers).
        /// </summary>
        public virtual Transition[][] SortedTransitions
        {
            get
            {
                State[] states = NumberedStates;
                Transition[][] transitions = new Transition[states.Length][];
                foreach (State s in states)
                {
                    s.SortTransitions(Transition.CompareByMinMaxThenDest);
                    s.TrimTransitionsArray();
                    transitions[s.number] = s.TransitionsArray;
                    Debug.Assert(s.TransitionsArray != null);
                }
                return transitions;
            }
        }

        /// <summary>
        /// Expands singleton representation to normal representation. Does nothing if
        /// not in singleton representation.
        /// </summary>
        public virtual void ExpandSingleton()
        {
            if (IsSingleton)
            {
                State p = new State();
                Initial = p;
                for (int i = 0, cp = 0; i < singleton.Length; i += Character.CharCount(cp))
                {
                    State q = new State();
                    p.AddTransition(new Transition(cp = Character.CodePointAt(singleton, i), q));
                    p = q;
                }
                p.accept = true;
                deterministic = true;
                singleton = null;
            }
        }

        /// <summary>
        /// Returns the number of states in this automaton.
        /// </summary>
        public virtual int NumberOfStates
        {
            get
            {
                if (IsSingleton)
                {
                    return singleton.Length;// codePointCount(0, singleton.Length) + 1;
                }
                return NumberedStates.Length;
            }
        }

        /// <summary>
        /// Returns the number of transitions in this automaton. this number is counted
        /// as the total number of edges, where one edge may be a character interval.
        /// </summary>
        public virtual int NumberOfTransitions
        {
            get
            {
                if (IsSingleton)
                {
                    return singleton.Length;// codePointCount(0, singleton.Length);
                }
                int c = 0;
                foreach (State s in NumberedStates)
                {
                    c += s.NumTransitions();
                }
                return c;
            }
        }

        public override bool Equals(object obj)
        {
            throw new System.NotSupportedException("use BasicOperations.sameLanguage instead");
        }

        /*public override int GetHashCode()
        {
          throw new System.NotSupportedException();
        }*/

        /// <summary>
        /// Must be invoked when the stored hash code may no longer be valid.
        /// </summary>
        /*
        void clearHashCode() {
          hash_code = 0;
        }
        */

        /// <summary>
        /// Returns a string representation of this automaton.
        /// </summary>
        public override string ToString()
        {
            StringBuilder b = new StringBuilder();
            if (IsSingleton)
            {
                b.Append("singleton: ");
                int length = singleton.Length;// codePointCount(0, singleton.Length);
                int[] codepoints = new int[length];
                for (int i = 0, j = 0, cp = 0; i < singleton.Length; i += Character.CharCount(cp))
                {
                    codepoints[j++] = cp = singleton[i];
                }
                foreach (int c in codepoints)
                {
                    Transition.AppendCharString(c, b);
                }
                b.Append("\n");
            }
            else
            {
                State[] states = NumberedStates;
                b.Append("initial state: ").Append(Initial.number).Append("\n");
                foreach (State s in states)
                {
                    b.Append(s.ToString());
                }
            }
            return b.ToString();
        }

        /// <summary>
        /// Returns <a href="http://www.research.att.com/sw/tools/graphviz/"
        /// target="_top">Graphviz Dot</a> representation of this automaton.
        /// </summary>
        public virtual string ToDot()
        {
            StringBuilder b = new StringBuilder("digraph Automaton {\n");
            b.Append("  rankdir = LR;\n");
            State[] states = NumberedStates;
            foreach (State s in states)
            {
                b.Append("  ").Append(s.number);
                if (s.accept)
                {
                    b.Append(" [shape=doublecircle,label=\"\"];\n");
                }
                else
                {
                    b.Append(" [shape=circle,label=\"\"];\n");
                }
                if (s == Initial)
                {
                    b.Append("  initial [shape=plaintext,label=\"\"];\n");
                    b.Append("  initial -> ").Append(s.number).Append("\n");
                }
                foreach (Transition t in s.Transitions)
                {
                    b.Append("  ").Append(s.number);
                    t.AppendDot(b);
                }
            }
            return b.Append("}\n").ToString();
        }

        /// <summary>
        /// Returns a clone of this automaton, expands if singleton.
        /// </summary>
        public virtual Automaton CloneExpanded()
        {
            Automaton a = (Automaton)Clone();
            a.ExpandSingleton();
            return a;
        }

        /// <summary>
        /// Returns a clone of this automaton unless <code>allow_mutation</code> is
        /// set, expands if singleton.
        /// </summary>
        internal virtual Automaton CloneExpandedIfRequired()
        {
            if (Allow_mutation)
            {
                ExpandSingleton();
                return this;
            }
            else
            {
                return CloneExpanded();
            }
        }

        /// <summary>
        /// Returns a clone of this automaton.
        /// </summary>
        public object Clone()
        {
            Automaton a = (Automaton)base.MemberwiseClone();
            if (!IsSingleton)
            {
                Dictionary<State, State> m = new Dictionary<State, State>();
                State[] states = NumberedStates;
                foreach (State s in states)
                {
                    m[s] = new State();
                }
                foreach (State s in states)
                {
                    State p = m[s];
                    p.accept = s.accept;
                    if (s == Initial)
                    {
                        a.Initial = p;
                    }
                    foreach (Transition t in s.Transitions)
                    {
                        p.AddTransition(new Transition(t.Min_Renamed, t.Max_Renamed, m[t.To]));
                    }
                }
            }
            a.ClearNumberedStates();
            return a;
        }

        /// <summary>
        /// Returns a clone of this automaton, or this automaton itself if
        /// <code>allow_mutation</code> flag is set.
        /// </summary>
        internal virtual Automaton CloneIfRequired()
        {
            if (Allow_mutation)
            {
                return this;
            }
            else
            {
                return (Automaton)Clone();
            }
        }

        /// <summary>
        /// See <seealso cref="BasicOperations#concatenate(Automaton, Automaton)"/>.
        /// </summary>
        public virtual Automaton Concatenate(Automaton a)
        {
            return BasicOperations.Concatenate(this, a);
        }

        /// <summary>
        /// See <seealso cref="BasicOperations#concatenate(List)"/>.
        /// </summary>
        public static Automaton Concatenate(IList<Automaton> l)
        {
            return BasicOperations.Concatenate(l);
        }

        /// <summary>
        /// See <seealso cref="BasicOperations#optional(Automaton)"/>.
        /// </summary>
        public virtual Automaton Optional()
        {
            return BasicOperations.Optional(this);
        }

        /// <summary>
        /// See <seealso cref="BasicOperations#repeat(Automaton)"/>.
        /// </summary>
        public virtual Automaton Repeat()
        {
            return BasicOperations.Repeat(this);
        }

        /// <summary>
        /// See <seealso cref="BasicOperations#repeat(Automaton, int)"/>.
        /// </summary>
        public virtual Automaton Repeat(int min)
        {
            return BasicOperations.Repeat(this, min);
        }

        /// <summary>
        /// See <seealso cref="BasicOperations#repeat(Automaton, int, int)"/>.
        /// </summary>
        public virtual Automaton Repeat(int min, int max)
        {
            return BasicOperations.Repeat(this, min, max);
        }

        /// <summary>
        /// See <seealso cref="BasicOperations#complement(Automaton)"/>.
        /// </summary>
        public virtual Automaton Complement()
        {
            return BasicOperations.Complement(this);
        }

        /// <summary>
        /// See <seealso cref="BasicOperations#minus(Automaton, Automaton)"/>.
        /// </summary>
        public virtual Automaton Minus(Automaton a)
        {
            return BasicOperations.Minus(this, a);
        }

        /// <summary>
        /// See <seealso cref="BasicOperations#intersection(Automaton, Automaton)"/>.
        /// </summary>
        public virtual Automaton Intersection(Automaton a)
        {
            return BasicOperations.Intersection(this, a);
        }

        /// <summary>
        /// See <seealso cref="BasicOperations#subsetOf(Automaton, Automaton)"/>.
        /// </summary>
        public virtual bool SubsetOf(Automaton a)
        {
            return BasicOperations.SubsetOf(this, a);
        }

        /// <summary>
        /// See <seealso cref="BasicOperations#union(Automaton, Automaton)"/>.
        /// </summary>
        public virtual Automaton Union(Automaton a)
        {
            return BasicOperations.Union(this, a);
        }

        /// <summary>
        /// See <seealso cref="BasicOperations#union(Collection)"/>.
        /// </summary>
        public static Automaton Union(ICollection<Automaton> l)
        {
            return BasicOperations.Union(l);
        }

        /// <summary>
        /// See <seealso cref="BasicOperations#determinize(Automaton)"/>.
        /// </summary>
        public virtual void Determinize()
        {
            BasicOperations.Determinize(this);
        }

        /// <summary>
        /// See <seealso cref="BasicOperations#isEmptyString(Automaton)"/>.
        /// </summary>
        public virtual bool EmptyString
        {
            get
            {
                return BasicOperations.IsEmptyString(this);
            }
        }

        /// <summary>
        /// See <seealso cref="MinimizationOperations#minimize(Automaton)"/>. Returns the
        /// automaton being given as argument.
        /// </summary>
        public static Automaton Minimize(Automaton a)
        {
            MinimizationOperations.Minimize(a);
            return a;
        }
    }
}