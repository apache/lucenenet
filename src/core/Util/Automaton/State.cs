using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util.Automaton
{
    [Serializable]
    public class State : IComparable<State>
    {
        bool accept;
        public Transition[] transitionsArray;
        public int numTransitions;

        internal int number;

        int id;
        static int next_id;

        /**
         * Constructs a new state. Initially, the new state is a reject state.
         */
        public State()
        {
            ResetTransitions();
            id = next_id++;
        }

        void ResetTransitions()
        {
            transitionsArray = new Transition[0];
            numTransitions = 0;
        }

        private class TransitionsEnumerator : IEnumerator<Transition>
        {
            private readonly Transition[] transitionsArray;
            private readonly int numTransitions;
            private int upto = 0;

            public TransitionsEnumerator(Transition[] transitionsArray, int numTransitions)
            {
                this.transitionsArray = transitionsArray;
                this.numTransitions = numTransitions;
            }

            public Transition Current
            {
                get { return transitionsArray[upto]; }
            }

            public void Dispose()
            {
            }

            object System.Collections.IEnumerator.Current
            {
                get { return Current; }
            }

            public bool MoveNext()
            {
                if (upto >= numTransitions)
                    return false;

                upto++;

                return true;
            }

            public void Reset()
            {
                throw new NotImplementedException();
            }
        }

        private class TransitionsEnumerable : IEnumerable<Transition>
        {
            private readonly Transition[] transitionsArray;
            private readonly int numTransitions;

            public TransitionsEnumerable(Transition[] transitionsArray, int numTransitions)
            {
                this.transitionsArray = transitionsArray;
                this.numTransitions = numTransitions;
            }

            public IEnumerator<Transition> GetEnumerator()
            {
                return new TransitionsEnumerator(transitionsArray, numTransitions);
            }
        }

        public IEnumerable<Transition> GetTransitions()
        {
            return new TransitionsEnumerable(transitionsArray, numTransitions);
        }

        public int NumTransitions
        {
            get { return numTransitions; }
        }

        public void SetTransitions(Transition[] transitions)
        {
            this.numTransitions = transitions.Length;
            this.transitionsArray = transitions;
        }

        public void AddTransition(Transition t)
        {
            if (numTransitions == transitionsArray.Length)
            {
                Transition[] newArray = new Transition[ArrayUtil.Oversize(1 + numTransitions, RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
                Array.Copy(transitionsArray, 0, newArray, 0, numTransitions);
                transitionsArray = newArray;
            }
            transitionsArray[numTransitions++] = t;
        }

        public bool Accept
        {
            get { return accept; }
            set { accept = value; }
        }

        public State Step(int c)
        {
            //assert c >= 0;
            for (int i = 0; i < numTransitions; i++)
            {
                Transition t = transitionsArray[i];
                if (t.min <= c && c <= t.max) return t.to;
            }
            return null;
        }

        public void Step(int c, ICollection<State> dest)
        {
            for (int i = 0; i < numTransitions; i++)
            {
                Transition t = transitionsArray[i];
                if (t.min <= c && c <= t.max) dest.Add(t.to);
            }
        }

        internal void AddEpsilon(State to)
        {
            if (to.accept) accept = true;
            foreach (Transition t in to.GetTransitions())
                AddTransition(t);
        }

        public void TrimTransitionsArray()
        {
            if (numTransitions < transitionsArray.Length)
            {
                Transition[] newArray = new Transition[numTransitions];
                Array.Copy(transitionsArray, 0, newArray, 0, numTransitions);
                transitionsArray = newArray;
            }
        }

        public void Reduce()
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
                Transition t = transitionsArray[i];
                if (p == t.to)
                {
                    if (t.min <= max + 1)
                    {
                        if (t.max > max) max = t.max;
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

        public void SortTransitions(IComparer<Transition> comparator)
        {
            // mergesort seems to perform better on already sorted arrays:
            if (numTransitions > 1) ArrayUtil.MergeSort(transitionsArray, 0, numTransitions, comparator);
        }

        public int Number
        {
            get { return number; }
        }

        public override string ToString()
        {
            StringBuilder b = new StringBuilder();
            b.Append("state ").Append(number);
            if (accept) b.Append(" [accept]");
            else b.Append(" [reject]");
            b.Append(":\n");
            foreach (Transition t in GetTransitions())
                b.Append("  ").Append(t.ToString()).Append("\n");
            return b.ToString();
        }

        public int CompareTo(State s)
        {
            return s.id - id;
        }

        public override int GetHashCode()
        {
            return id;
        }
    }
}
