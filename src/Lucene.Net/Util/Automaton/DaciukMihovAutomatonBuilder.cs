using J2N;
using J2N.Runtime.CompilerServices;
using J2N.Text;
using Lucene.Net.Diagnostics;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Arrays = Lucene.Net.Support.Arrays;
using JCG = J2N.Collections.Generic;

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
    /// Builds a minimal, deterministic <see cref="Automaton"/> that accepts a set of
    /// strings. The algorithm requires sorted input data, but is very fast
    /// (nearly linear with the input size).
    /// </summary>
    /// <seealso cref="Build(ICollection{BytesRef})"/>
    /// <seealso cref="BasicAutomata.MakeStringUnion(ICollection{BytesRef})"/>
    internal sealed class DaciukMihovAutomatonBuilder
    {
        /// <summary>
        /// DFSA state with <see cref="char"/> labels on transitions.
        /// </summary>
        public sealed class State // LUCENENET NOTE: Made public because it is returned from a public member
        {
            /// <summary>
            /// An empty set of labels. </summary>
            private static readonly int[] NO_LABELS = Arrays.Empty<int>();

            /// <summary>
            /// An empty set of states. </summary>
            private static readonly State[] NO_STATES = Arrays.Empty<State>();

            /// <summary>
            /// Labels of outgoing transitions. Indexed identically to <see cref="states"/>.
            /// Labels must be sorted lexicographically.
            /// </summary>
            internal int[] labels = NO_LABELS;

            /// <summary>
            /// States reachable from outgoing transitions. Indexed identically to
            /// <see cref="labels"/>.
            /// </summary>
            internal State[] states = NO_STATES;

            /// <summary>
            /// <c>true</c> if this state corresponds to the end of at least one
            /// input sequence.
            /// </summary>
            internal bool is_final;

            /// <summary>
            /// Returns the target state of a transition leaving this state and labeled
            /// with <paramref name="label"/>. If no such transition exists, returns
            /// <c>null</c>.
            /// </summary>
            internal State GetState(int label)
            {
                int index = Array.BinarySearch(labels, label);
                return index >= 0 ? states[index] : null;
            }

            /// <summary>
            /// Two states are equal if:
            /// <list type="bullet">
            ///     <item><description>They have an identical number of outgoing transitions, labeled with
            ///         the same labels.</description></item>
            ///     <item><description>Corresponding outgoing transitions lead to the same states (to states
            ///         with an identical right-language).</description></item>
            /// </list>
            /// </summary>
            public override bool Equals(object obj)
            {
                State other = (State)obj;
                return is_final == other.is_final && Arrays.Equals(this.labels, other.labels) && ReferenceEquals(this.states, other.states);
            }

            /// <summary>
            /// Compute the hash code of the <i>current</i> status of this state.
            /// </summary>
            public override int GetHashCode()
            {
                int hash = is_final ? 1 : 0;

                hash ^= hash * 31 + this.labels.Length;
                foreach (int c in this.labels)
                {
                    hash ^= hash * 31 + c;
                }

                /*
                 * Compare the right-language of this state using reference-identity of
                 * outgoing states. this is possible because states are interned (stored
                 * in registry) and traversed in post-order, so any outgoing transitions
                 * are already interned.
                 */
                foreach (State s in this.states)
                {
                    hash ^= s.GetHashCode();
                }

                return hash;
            }

            /// <summary>
            /// Return <c>true</c> if this state has any children (outgoing
            /// transitions).
            /// </summary>
            internal bool HasChildren => labels.Length > 0;

            /// <summary>
            /// Create a new outgoing transition labeled <paramref name="label"/> and return
            /// the newly created target state for this transition.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal State NewState(int label)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(Array.BinarySearch(labels, label) < 0, "State already has transition labeled: {0}", label);

                labels = Arrays.CopyOf(labels, labels.Length + 1);
                states = Arrays.CopyOf(states, states.Length + 1);

                labels[labels.Length - 1] = label;
                return states[states.Length - 1] = new State();
            }

            /// <summary>
            /// Return the most recent transitions's target state.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal State LastChild() // LUCENENET NOTE: Kept this a method because there is another overload
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(HasChildren, "No outgoing transitions.");
                return states[states.Length - 1];
            }

            /// <summary>
            /// Return the associated state if the most recent transition is labeled with
            /// <paramref name="label"/>.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal State LastChild(int label)
            {
                int index = labels.Length - 1;
                State s = null;
                if (index >= 0 && labels[index] == label)
                {
                    s = states[index];
                }
                if (Debugging.AssertsEnabled) Debugging.Assert(s == GetState(label));
                return s;
            }

            /// <summary>
            /// Replace the last added outgoing transition's target state with the given
            /// <paramref name="state"/>.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal void ReplaceLastChild(State state)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(HasChildren, "No outgoing transitions.");
                states[states.Length - 1] = state;
            }

            /// <summary>
            /// Compare two lists of objects for reference-equality.
            /// </summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        private Dictionary<State, State> stateRegistry = new Dictionary<State, State>();

        /// <summary>
        /// Root automaton state.
        /// </summary>
        private readonly State root = new State();

        /// <summary>
        /// Previous sequence added to the automaton in <see cref="Add(CharsRef)"/>.
        /// </summary>
        private CharsRef previous;

        /// <summary>
        /// A comparer used for enforcing sorted UTF8 order, used in assertions only.
        /// </summary>
        private static readonly IComparer<CharsRef> comparer =
#pragma warning disable 612, 618
            CharsRef.UTF16SortedAsUTF8Comparer;
#pragma warning restore 612, 618

        /// <summary>
        /// Add another character sequence to this automaton. The sequence must be
        /// lexicographically larger or equal compared to any previous sequences added
        /// to this automaton (the input must be sorted).
        /// </summary>
        public void Add(CharsRef current)
        {
            if (Debugging.AssertsEnabled)
            {
                Debugging.Assert(stateRegistry != null, "Automaton already built.");
                Debugging.Assert(previous is null || comparer.Compare(previous, current) <= 0, "Input must be in sorted UTF-8 order: {0} >= {1}", previous, current);
                Debugging.Assert(SetPrevious(current));
            }

            // Descend in the automaton (find matching prefix).
            int pos = 0, max = current.Length;
            State next, state = root;
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
            if (this.stateRegistry is null)
            {
                throw IllegalStateException.Create();
            }

            if (root.HasChildren)
            {
                ReplaceOrRegister(root);
            }

            stateRegistry = null;
            return root;
        }

        /// <summary>
        /// Internal recursive traversal for conversion.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="visited">Must use a dictionary with <see cref="IdentityEqualityComparer{State}.Default"/> passed into its constructor.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Util.Automaton.State Convert(State s, IDictionary<State, Util.Automaton.State> visited)
        {
            if (visited.TryGetValue(s, out Util.Automaton.State converted) && converted != null)
            {
                return converted;
            }

            converted = new Util.Automaton.State
            {
                Accept = s.is_final
            };

            visited[s] = converted;
            int i = 0;
            int[] labels = s.labels;
            foreach (DaciukMihovAutomatonBuilder.State target in s.states)
            {
                converted.AddTransition(new Transition(labels[i++], Convert(target, visited)));
            }

            return converted;
        }

        /// <summary>
        /// Build a minimal, deterministic automaton from a sorted list of <see cref="BytesRef"/> representing
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

            return new Automaton
            {
                initial = Convert(builder.Complete(), new JCG.Dictionary<State, Lucene.Net.Util.Automaton.State>(IdentityEqualityComparer<State>.Default)),
                deterministic = true
            };
        }

        /// <summary>
        /// Copy <paramref name="current"/> into an internal buffer.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool SetPrevious(CharsRef current)
        {
            // don't need to copy, once we fix https://issues.apache.org/jira/browse/LUCENE-3277
            // still, called only from assert
            previous = CharsRef.DeepCopyOf(current);
            return true;
        }

        /// <summary>
        /// Replace last child of <paramref name="state"/> with an already registered state
        /// or stateRegistry the last child state.
        /// </summary>
        private void ReplaceOrRegister(State state)
        {
            State child = state.LastChild();

            if (child.HasChildren)
            {
                ReplaceOrRegister(child);
            }

            if (stateRegistry.TryGetValue(child, out State registered))
            {
                state.ReplaceLastChild(registered);
            }
            else
            {
                stateRegistry[child] = child;
            }
        }

        /// <summary>
        /// Add a suffix of <paramref name="current"/> starting at <paramref name="fromIndex"/>
        /// (inclusive) to state <paramref name="state"/>.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void AddSuffix(State state, ICharSequence current, int fromIndex) // LUCENENET: CA1822: Mark members as static
        {
            int len = current.Length;
            while (fromIndex < len)
            {
                int cp = Character.CodePointAt(current, fromIndex);
                state = state.NewState(cp);
                fromIndex += Character.CharCount(cp);
            }
            state.is_final = true;
        }
    }
}