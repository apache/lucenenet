using Lucene.Net.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util.Automaton
{
    internal sealed class DaciukMihovAutomatonBuilder
    {
        public sealed class State
        {
            private static readonly int[] NO_LABELS = new int[0];

            private static readonly State[] NO_STATES = new State[0];

            internal int[] labels = NO_LABELS;

            internal State[] states = NO_STATES;

            internal bool is_final;

            internal State GetState(int label)
            {
                int index = Array.BinarySearch(labels, label);
                return index >= 0 ? states[index] : null;
            }

            public override bool Equals(object obj)
            {
                State other = (State)obj;
                return is_final == other.is_final
                    && Arrays.Equals(this.labels, other.labels)
                    && ReferenceEquals(this.states, other.states);
            }

            public override int GetHashCode()
            {
                int hash = is_final ? 1 : 0;

                hash ^= hash * 31 + this.labels.Length;
                foreach (int c in this.labels)
                    hash ^= hash * 31 + c;

                /*
                 * Compare the right-language of this state using reference-identity of
                 * outgoing states. This is possible because states are interned (stored
                 * in registry) and traversed in post-order, so any outgoing transitions
                 * are already interned.
                 */
                foreach (State s in this.states)
                {
                    hash ^= s.GetHashCode();
                }

                return hash;
            }

            internal bool HasChildren()
            {
                return labels.Length > 0;
            }

            internal State NewState(int label)
            {
                //assert Arrays.binarySearch(labels, label) < 0 : "State already has transition labeled: "
                //    + label;

                labels = Arrays.CopyOf(labels, labels.Length + 1);
                states = Arrays.CopyOf(states, states.Length + 1);

                labels[labels.Length - 1] = label;
                return states[states.Length - 1] = new State();
            }

            internal State LastChild()
            {
                //assert hasChildren() : "No outgoing transitions.";
                return states[states.Length - 1];
            }

            internal State LastChild(int label)
            {
                int index = labels.Length - 1;
                State s = null;
                if (index >= 0 && labels[index] == label)
                {
                    s = states[index];
                }
                //assert s == GetState(label);
                return s;
            }

            internal void ReplaceLastChild(State state)
            {
                //assert hasChildren() : "No outgoing transitions.";
                states[states.Length - 1] = state;
            }

            private static bool ReferenceEquals(Object[] a1, Object[] a2)
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

        private HashMap<State, State> stateRegistry = new HashMap<State, State>();

        private State root = new State();

        private CharsRef previous;

        private static readonly IComparer<CharsRef> comparator = CharsRef.UTF16SortedAsUTF8Comparator;

        public void Add(CharsRef current)
        {
            //assert stateRegistry != null : "Automaton already built.";
            //assert previous == null
            //    || comparator.compare(previous, current) <= 0 : "Input must be in sorted UTF-8 order: "
            //    + previous + " >= " + current;
            //assert setPrevious(current);

            // Descend in the automaton (find matching prefix).
            int pos = 0, max = current.Length;
            State next, state = root;
            while (pos < max && (next = state.LastChild(current.CharAt(pos))) != null)
            {
                state = next;
                // todo, optimize me
                pos += 1;
            }

            if (state.HasChildren()) ReplaceOrRegister(state);

            AddSuffix(state, current, pos);
        }

        public State Complete()
        {
            if (this.stateRegistry == null) throw new InvalidOperationException();

            if (root.HasChildren()) ReplaceOrRegister(root);

            stateRegistry = null;
            return root;
        }

        private static Lucene.Net.Util.Automaton.State Convert(State s,
            IdentityDictionary<State, Lucene.Net.Util.Automaton.State> visited)
        {
            Lucene.Net.Util.Automaton.State converted = visited[s];
            if (converted != null) return converted;

            converted = new Lucene.Net.Util.Automaton.State();
            converted.Accept = s.is_final;

            visited[s] = converted;
            int i = 0;
            int[] labels = s.labels;
            foreach (DaciukMihovAutomatonBuilder.State target in s.states)
            {
                converted.AddTransition(
                    new Transition(labels[i++], Convert(target, visited)));
            }

            return converted;
        }

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
            a.InitialState = Convert(
                builder.Complete(),
                new IdentityDictionary<State, Lucene.Net.Util.Automaton.State>());
            a.Deterministic = true;
            return a;
        }

        private bool SetPrevious(CharsRef current)
        {
            // don't need to copy, once we fix https://issues.apache.org/jira/browse/LUCENE-3277
            // still, called only from assert
            previous = CharsRef.DeepCopyOf(current);
            return true;
        }

        private void ReplaceOrRegister(State state)
        {
            State child = state.LastChild();

            if (child.HasChildren()) ReplaceOrRegister(child);

            State registered = stateRegistry[child];
            if (registered != null)
            {
                state.ReplaceLastChild(registered);
            }
            else
            {
                stateRegistry[child] = child;
            }
        }

        private void AddSuffix(State state, ICharSequence current, int fromIndex)
        {
            int len = current.Length;
            while (fromIndex < len)
            {
                int cp = current.CharAt(fromIndex);
                state = state.NewState(cp);
                fromIndex += 1;
            }
            state.is_final = true;
        }
    }
}
