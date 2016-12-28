using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Lucene.Net.Util.Automaton
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    /// <summary>
    /// Builds a minimal, deterministic <seealso cref="Automaton"/> that accepts a set of
    /// strings. The algorithm requires sorted input data, but is very fast
    /// (nearly linear with the input size).
    /// </summary>
    /// <seealso cref= #build(Collection) </seealso>
    /// <seealso cref= BasicAutomata#makeStringUnion(Collection) </seealso>
    internal sealed class DaciukMihovAutomatonBuilder
    {
        /// <summary>
        /// DFSA state with <code>char</code> labels on transitions.
        /// </summary>
        public sealed class State // LUCENENET NOTE: Made public because it is returned from a public member
        {
            /// <summary>
            /// An empty set of labels. </summary>
            private static readonly int[] NO_LABELS = new int[0];

            /// <summary>
            /// An empty set of states. </summary>
            private static readonly State[] NO_STATES = new State[0];

            /// <summary>
            /// Labels of outgoing transitions. Indexed identically to <seealso cref="#states"/>.
            /// Labels must be sorted lexicographically.
            /// </summary>
            internal int[] Labels = NO_LABELS;

            /// <summary>
            /// States reachable from outgoing transitions. Indexed identically to
            /// <seealso cref="#labels"/>.
            /// </summary>
            internal State[] States = NO_STATES;

            /// <summary>
            /// <code>true</code> if this state corresponds to the end of at least one
            /// input sequence.
            /// </summary>
            internal bool Is_final;

            /// <summary>
            /// Returns the target state of a transition leaving this state and labeled
            /// with <code>label</code>. If no such transition exists, returns
            /// <code>null</code>.
            /// </summary>
            internal State GetState(int label)
            {
                int index = Array.BinarySearch(Labels, label);
                return index >= 0 ? States[index] : null;
            }

            /// <summary>
            /// Two states are equal if:
            /// <ul>
            /// <li>they have an identical number of outgoing transitions, labeled with
            /// the same labels</li>
            /// <li>corresponding outgoing transitions lead to the same states (to states
            /// with an identical right-language).
            /// </ul>
            /// </summary>
            public override bool Equals(object obj)
            {
                State other = (State)obj;
                return Is_final == other.Is_final && Array.Equals(this.Labels, other.Labels) && ReferenceEquals(this.States, other.States);
            }

            /// <summary>
            /// Compute the hash code of the <i>current</i> status of this state.
            /// </summary>
            public override int GetHashCode()
            {
                int hash = Is_final ? 1 : 0;

                hash ^= hash * 31 + this.Labels.Length;
                foreach (int c in this.Labels)
                {
                    hash ^= hash * 31 + c;
                }

                /*
                 * Compare the right-language of this state using reference-identity of
                 * outgoing states. this is possible because states are interned (stored
                 * in registry) and traversed in post-order, so any outgoing transitions
                 * are already interned.
                 */
                foreach (State s in this.States)
                {
                    hash ^= s.GetHashCode();
                }

                return hash;
            }

            /// <summary>
            /// Return <code>true</code> if this state has any children (outgoing
            /// transitions).
            /// </summary>
            internal bool HasChildren
            {
                get { return Labels.Length > 0; }
            }

            /// <summary>
            /// Create a new outgoing transition labeled <code>label</code> and return
            /// the newly created target state for this transition.
            /// </summary>
            internal State NewState(int label)
            {
                Debug.Assert(Array.BinarySearch(Labels, label) < 0, "State already has transition labeled: " + label);

                Labels = Arrays.CopyOf(Labels, Labels.Length + 1);
                States = Arrays.CopyOf(States, States.Length + 1);

                Labels[Labels.Length - 1] = label;
                return States[States.Length - 1] = new State();
            }

            /// <summary>
            /// Return the most recent transitions's target state.
            /// </summary>
            internal State LastChild() // LUCENENET NOTE: Kept this a method because there is another overload
            {
                Debug.Assert(HasChildren, "No outgoing transitions.");
                return States[States.Length - 1];
            }

            /// <summary>
            /// Return the associated state if the most recent transition is labeled with
            /// <code>label</code>.
            /// </summary>
            internal State LastChild(int label)
            {
                int index = Labels.Length - 1;
                State s = null;
                if (index >= 0 && Labels[index] == label)
                {
                    s = States[index];
                }
                Debug.Assert(s == GetState(label));
                return s;
            }

            /// <summary>
            /// Replace the last added outgoing transition's target state with the given
            /// state.
            /// </summary>
            internal void ReplaceLastChild(State state)
            {
                Debug.Assert(HasChildren, "No outgoing transitions.");
                States[States.Length - 1] = state;
            }

            /// <summary>
            /// Compare two lists of objects for reference-equality.
            /// </summary>
            private static bool ReferenceEquals(object[] a1, object[] a2)
            {
                if (a1.Length != a2.Length)
                {
                    return false;
                }

                for (int i = 0; i < a1.Length; i++)
                {
                    if (a1[i] != a2[i])
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// A "registry" for state interning.
        /// </summary>
        private Dictionary<State, State> StateRegistry = new Dictionary<State, State>();

        /// <summary>
        /// Root automaton state.
        /// </summary>
        private State Root = new State();

        /// <summary>
        /// Previous sequence added to the automaton in <seealso cref="#add(CharsRef)"/>.
        /// </summary>
        private CharsRef Previous_Renamed;

        /// <summary>
        /// A comparator used for enforcing sorted UTF8 order, used in assertions only.
        /// </summary>
        private static readonly IComparer<CharsRef> Comparator = CharsRef.UTF16SortedAsUTF8Comparer;

        /// <summary>
        /// Add another character sequence to this automaton. The sequence must be
        /// lexicographically larger or equal compared to any previous sequences added
        /// to this automaton (the input must be sorted).
        /// </summary>
        public void Add(CharsRef current)
        {
            Debug.Assert(StateRegistry != null, "Automaton already built.");
            Debug.Assert(Previous_Renamed == null || Comparator.Compare(Previous_Renamed, current) <= 0, "Input must be in sorted UTF-8 order: " + Previous_Renamed + " >= " + current);
            Debug.Assert(SetPrevious(current));

            // Descend in the automaton (find matching prefix).
            int pos = 0, max = current.Length;
            State next, state = Root;
            while (pos < max && (next = state.LastChild(Character.CodePointAt(current, pos))) != null)
            {
                state = next;
                // todo, optimize me
                pos += Character.CharCount(Character.CodePointAt(current, pos));
            }

            if (state.HasChildren)
            {
                ReplaceOrRegister(state);
            }

            AddSuffix(state, current, pos);
        }

        /// <summary>
        /// Finalize the automaton and return the root state. No more strings can be
        /// added to the builder after this call.
        /// </summary>
        /// <returns> Root automaton state. </returns>
        public State Complete()
        {
            if (this.StateRegistry == null)
            {
                throw new InvalidOperationException();
            }

            if (Root.HasChildren)
            {
                ReplaceOrRegister(Root);
            }

            StateRegistry = null;
            return Root;
        }

        /// <summary>
        /// Internal recursive traversal for conversion.
        /// </summary>
        private static Util.Automaton.State Convert(State s, IdentityHashMap<State, Lucene.Net.Util.Automaton.State> visited)
        {
            Util.Automaton.State converted = visited[s];
            if (converted != null)
            {
                return converted;
            }

            converted = new Util.Automaton.State();
            converted.Accept = s.Is_final;

            visited[s] = converted;
            int i = 0;
            int[] labels = s.Labels;
            foreach (DaciukMihovAutomatonBuilder.State target in s.States)
            {
                converted.AddTransition(new Transition(labels[i++], Convert(target, visited)));
            }

            return converted;
        }

        /// <summary>
        /// Build a minimal, deterministic automaton from a sorted list of <seealso cref="BytesRef"/> representing
        /// strings in UTF-8. These strings must be binary-sorted.
        /// </summary>
        public static Automaton Build(ICollection<BytesRef> input)
        {
            DaciukMihovAutomatonBuilder builder = new DaciukMihovAutomatonBuilder();

            CharsRef scratch = new CharsRef();
            foreach (BytesRef b in input)
            {
                UnicodeUtil.UTF8toUTF16(b, scratch);
                builder.Add(scratch);
            }

            Automaton a = new Automaton();
            a.initial = Convert(builder.Complete(), new IdentityHashMap<State, Lucene.Net.Util.Automaton.State>());
            a.deterministic = true;
            return a;
        }

        /// <summary>
        /// Copy <code>current</code> into an internal buffer.
        /// </summary>
        private bool SetPrevious(CharsRef current)
        {
            // don't need to copy, once we fix https://issues.apache.org/jira/browse/LUCENE-3277
            // still, called only from assert
            Previous_Renamed = CharsRef.DeepCopyOf(current);
            return true;
        }

        /// <summary>
        /// Replace last child of <code>state</code> with an already registered state
        /// or stateRegistry the last child state.
        /// </summary>
        private void ReplaceOrRegister(State state)
        {
            State child = state.LastChild();

            if (child.HasChildren)
            {
                ReplaceOrRegister(child);
            }

            State registered;
            if (StateRegistry.TryGetValue(child, out registered))
            {
                state.ReplaceLastChild(registered);
            }
            else
            {
                StateRegistry[child] = child;
            }
        }

        /// <summary>
        /// Add a suffix of <code>current</code> starting at <code>fromIndex</code>
        /// (inclusive) to state <code>state</code>.
        /// </summary>
        private void AddSuffix(State state, ICharSequence current, int fromIndex)
        {
            int len = current.Length;
            while (fromIndex < len)
            {
                int cp = Character.CodePointAt(current, fromIndex);
                state = state.NewState(cp);
                fromIndex += Character.CharCount(cp);
            }
            state.Is_final = true;
        }
    }
}