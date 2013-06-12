using Lucene.Net.Support;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util.Automaton
{
    [Serializable]
    public class Automaton : ICloneable
    {
        public const int MINIMIZE_HOPCROFT = 2;

        static int minimization = MINIMIZE_HOPCROFT;

        private State initial;

        private bool deterministic;

        [NonSerialized]
        private Object info;

        private String singleton;

        static bool minimize_always = false;

        static bool allow_mutation = false;

        public Automaton(State initial)
        {
            this.initial = initial;
            deterministic = true;
            singleton = null;
        }

        public Automaton()
            : this(new State())
        {
        }

        public static int Minimization
        {
            get { return minimization; }
            set { minimization = value; }
        }

        public static bool MinimizeAlways
        {
            get { return minimize_always; }
            set { minimize_always = value; }
        }

        public static bool SetAllowMutate(bool flag)
        {
            bool b = allow_mutation;
            allow_mutation = flag;
            return b;
        }

        public static bool GetAllowMutate()
        {
            return allow_mutation;
        }

        internal virtual void CheckMinimizeAlways()
        {
            if (minimize_always) MinimizationOperations.Minimize(this);
        }

        internal virtual bool IsSingleton()
        {
            return singleton != null;
        }

        public virtual string Singleton
        {
            get { return singleton; }
            set
            {
                singleton = value;
            }
        }

        public virtual State InitialState
        {
            get
            {
                ExpandSingleton();
                return initial;
            }
            set
            {
                initial = value;
            }
        }

        public virtual bool Deterministic
        {
            get { return deterministic; }
            set { deterministic = value; }
        }

        public virtual Object Info
        {
            get { return info; }
            set { info = value; }
        }

        // cached
        private State[] numberedStates;

        public virtual State[] GetNumberedStates()
        {
            if (numberedStates == null)
            {
                ExpandSingleton();
                ISet<State> visited = new HashSet<State>();
                LinkedList<State> worklist = new LinkedList<State>();
                numberedStates = new State[4];
                int upto = 0;
                worklist.AddLast(initial);
                visited.Add(initial);
                initial.number = upto;
                numberedStates[upto] = initial;
                upto++;
                while (worklist.Count > 0)
                {
                    State s = worklist.First.Value;
                    worklist.RemoveFirst();

                    for (int i = 0; i < s.numTransitions; i++)
                    {
                        Transition t = s.transitionsArray[i];
                        if (!visited.Contains(t.to))
                        {
                            visited.Add(t.to);
                            worklist.AddLast(t.to);
                            t.to.number = upto;
                            if (upto == numberedStates.Length)
                            {
                                State[] newArray = new State[ArrayUtil.Oversize(1 + upto, RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
                                Array.Copy(numberedStates, 0, newArray, 0, upto);
                                numberedStates = newArray;
                            }
                            numberedStates[upto] = t.to;
                            upto++;
                        }
                    }
                }
                if (numberedStates.Length != upto)
                {
                    State[] newArray = new State[upto];
                    Array.Copy(numberedStates, 0, newArray, 0, upto);
                    numberedStates = newArray;
                }
            }

            return numberedStates;
        }

        public virtual void SetNumberedStates(State[] states)
        {
            SetNumberedStates(states, states.Length);
        }

        public virtual void SetNumberedStates(State[] states, int count)
        {
            //assert count <= states.length;
            // TODO: maybe we can eventually allow for oversizing here...
            if (count < states.Length)
            {
                State[] newArray = new State[count];
                Array.Copy(states, 0, newArray, 0, count);
                numberedStates = newArray;
            }
            else
            {
                numberedStates = states;
            }
        }

        public virtual void ClearNumberedStates()
        {
            numberedStates = null;
        }

        public ISet<State> GetAcceptStates()
        {
            ExpandSingleton();
            HashSet<State> accepts = new HashSet<State>();
            HashSet<State> visited = new HashSet<State>();
            LinkedList<State> worklist = new LinkedList<State>();
            worklist.AddLast(initial);
            visited.Add(initial);
            while (worklist.Count > 0)
            {
                State s = worklist.First.Value;
                worklist.RemoveFirst();
                if (s.Accept) accepts.Add(s);
                foreach (Transition t in s.GetTransitions())
                    if (!visited.Contains(t.to))
                    {
                        visited.Add(t.to);
                        worklist.AddLast(t.to);
                    }
            }
            return accepts;
        }

        internal virtual void Totalize()
        {
            State s = new State();
            s.AddTransition(new Transition(Character.MIN_CODE_POINT, Character.MAX_CODE_POINT,
                s));
            foreach (State p in GetNumberedStates())
            {
                int maxi = Character.MIN_CODE_POINT;
                p.SortTransitions(Transition.CompareByMinMaxThenDest);
                foreach (Transition t in p.GetTransitions())
                {
                    if (t.min > maxi) p.AddTransition(new Transition(maxi,
                        (t.min - 1), s));
                    if (t.max + 1 > maxi) maxi = t.max + 1;
                }
                if (maxi <= Character.MAX_CODE_POINT) p.AddTransition(new Transition(
                    maxi, Character.MAX_CODE_POINT, s));
            }
            ClearNumberedStates();
        }

        public virtual void RestoreInvariant()
        {
            RemoveDeadTransitions();
        }

        public virtual void Reduce()
        {
            State[] states = GetNumberedStates();
            if (IsSingleton()) return;
            foreach (State s in states)
                s.Reduce();
        }

        int[] GetStartPoints()
        {
            State[] states = GetNumberedStates();
            ISet<int> pointset = new HashSet<int>();
            pointset.Add(Character.MIN_CODE_POINT);
            foreach (State s in states)
            {
                foreach (Transition t in s.GetTransitions())
                {
                    pointset.Add(t.min);
                    if (t.max < Character.MAX_CODE_POINT) pointset.Add((t.max + 1));
                }
            }
            int[] points = new int[pointset.Count];
            int n = 0;
            foreach (int m in pointset)
                points[n++] = m;
            Array.Sort(points);
            return points;
        }

        private State[] GetLiveStates()
        {
            State[] states = GetNumberedStates();
            ISet<State> live = new HashSet<State>();
            foreach (State q in states)
            {
                if (q.Accept)
                {
                    live.Add(q);
                }
            }
            // map<state, set<state>>
            ISet<State>[] map = new ISet<State>[states.Length];
            for (int i = 0; i < map.Length; i++)
                map[i] = new HashSet<State>();
            foreach (State s in states)
            {
                for (int i = 0; i < s.numTransitions; i++)
                {
                    map[s.transitionsArray[i].to.number].Add(s);
                }
            }
            LinkedList<State> worklist = new LinkedList<State>(live);
            while (worklist.Count > 0)
            {
                State s = worklist.First.Value;
                worklist.RemoveFirst();
                foreach (State p in map[s.number])
                    if (!live.Contains(p))
                    {
                        live.Add(p);
                        worklist.AddLast(p);
                    }
            }

            return live.ToArray();
        }

        public void RemoveDeadTransitions()
        {
            State[] states = GetNumberedStates();
            //clearHashCode();
            if (IsSingleton()) return;
            State[] live = GetLiveStates();

            BitArray liveSet = new BitArray(states.Length);
            foreach (State s in live)
                liveSet.Set(s.number, true);

            foreach (State s in states)
            {
                // filter out transitions to dead states:
                int upto = 0;
                for (int i = 0; i < s.numTransitions; i++)
                {
                    Transition t = s.transitionsArray[i];
                    if (liveSet.Get(t.to.number))
                    {
                        s.transitionsArray[upto++] = s.transitionsArray[i];
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
                SetNumberedStates(live);
            }
            else
            {
                // sneaky corner case -- if machine accepts no strings
                ClearNumberedStates();
            }
            Reduce();
        }

        public Transition[][] GetSortedTransitions()
        {
            State[] states = GetNumberedStates();
            Transition[][] transitions = new Transition[states.Length][];
            foreach (State s in states)
            {
                s.SortTransitions(Transition.CompareByMinMaxThenDest);
                s.TrimTransitionsArray();
                transitions[s.number] = s.transitionsArray;
                //assert s.transitionsArray != null;
            }
            return transitions;
        }

        public void ExpandSingleton()
        {
            if (IsSingleton())
            {
                State p = new State();
                initial = p;
                for (int i = 0, cp = 0; i < singleton.Length; i += 1)
                {
                    State q = new State();
                    // TODO: check this cp = singleton[i] logic for .NET
                    p.AddTransition(new Transition(cp = singleton[i], q));
                    p = q;
                }
                p.Accept = true;
                deterministic = true;
                singleton = null;
            }
        }

        public int GetNumberOfStates()
        {
            // TODO: check this singleton.Length + 1 logic for .NET
            if (IsSingleton()) return singleton.Length + 1;
            return GetNumberedStates().Length;
        }

        public int GetNumberOfTransitions()
        {
            if (IsSingleton()) return singleton.Length;
            int c = 0;
            foreach (State s in GetNumberedStates())
                c += s.NumTransitions;
            return c;
        }

        public override bool Equals(object obj)
        {
            // this seems like a bad idea, calling base behavior instead:
            // throw new UnsupportedOperationException("use BasicOperations.sameLanguage instead");
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            // this seems like a bad idea, calling base behavior instead:
            // throw new UnsupportedOperationException();
            return base.GetHashCode();
        }

        public override string ToString()
        {
            StringBuilder b = new StringBuilder();
            if (IsSingleton())
            {
                b.Append("singleton: ");
                int length = singleton.Length;
                int[] codepoints = new int[length];
                for (int i = 0, j = 0, cp = 0; i < singleton.Length; i += 1)
                    codepoints[j++] = cp = singleton[i];
                foreach (int c in codepoints)
                    Transition.AppendCharString(c, b);
                b.Append("\n");
            }
            else
            {
                State[] states = GetNumberedStates();
                b.Append("initial state: ").Append(initial.number).Append("\n");
                foreach (State s in states)
                    b.Append(s.ToString());
            }
            return b.ToString();
        }

        public String ToDot()
        {
            StringBuilder b = new StringBuilder("digraph Automaton {\n");
            b.Append("  rankdir = LR;\n");
            State[] states = GetNumberedStates();
            foreach (State s in states)
            {
                b.Append("  ").Append(s.number);
                if (s.Accept) b.Append(" [shape=doublecircle,label=\"\"];\n");
                else b.Append(" [shape=circle,label=\"\"];\n");
                if (s == initial)
                {
                    b.Append("  initial [shape=plaintext,label=\"\"];\n");
                    b.Append("  initial -> ").Append(s.number).Append("\n");
                }
                foreach (Transition t in s.GetTransitions())
                {
                    b.Append("  ").Append(s.number);
                    t.AppendDot(b);
                }
            }
            return b.Append("}\n").ToString();
        }

        internal Automaton CloneExpanded()
        {
            Automaton a = (Automaton)Clone();
            a.ExpandSingleton();
            return a;
        }

        internal Automaton CloneExpandedIfRequired()
        {
            if (allow_mutation)
            {
                ExpandSingleton();
                return this;
            }
            else return CloneExpanded();
        }

        public object Clone()
        {
            Automaton a = (Automaton)this.MemberwiseClone();
            if (!IsSingleton())
            {
                HashMap<State, State> m = new HashMap<State, State>();
                State[] states = GetNumberedStates();
                foreach (State s in states)
                    m[s] = new State();
                foreach (State s in states)
                {
                    State p = m[s];
                    p.Accept = s.Accept;
                    if (s == initial) a.initial = p;
                    foreach (Transition t in s.GetTransitions())
                        p.AddTransition(new Transition(t.min, t.max, m[t.to]));
                }
            }
            a.ClearNumberedStates();
            return a;
        }

        internal Automaton CloneIfRequired()
        {
            if (allow_mutation) return this;
            else return (Automaton)Clone();
        }

        public Automaton Concatenate(Automaton a)
        {
            return BasicOperations.Concatenate(this, a);
        }

        public static Automaton Concatenate(IList<Automaton> l)
        {
            return BasicOperations.Concatenate(l);
        }

        public Automaton Optional()
        {
            return BasicOperations.Optional(this);
        }

        public Automaton Repeat()
        {
            return BasicOperations.Repeat(this);
        }

        public Automaton Repeat(int min)
        {
            return BasicOperations.Repeat(this, min);
        }

        public Automaton Repeat(int min, int max)
        {
            return BasicOperations.Repeat(this, min, max);
        }

        public Automaton Complement()
        {
            return BasicOperations.Complement(this);
        }

        public Automaton Minus(Automaton a)
        {
            return BasicOperations.Minus(this, a);
        }

        public Automaton Intersection(Automaton a)
        {
            return BasicOperations.Intersection(this, a);
        }

        public bool SubsetOf(Automaton a)
        {
            return BasicOperations.SubsetOf(this, a);
        }

        public Automaton Union(Automaton a)
        {
            return BasicOperations.Union(this, a);
        }

        public static Automaton Union(ICollection<Automaton> l)
        {
            return BasicOperations.Union(l);
        }

        public void Determinize()
        {
            BasicOperations.Determinize(this);
        }

        public bool IsEmptyString()
        {
            return BasicOperations.IsEmptyString(this);
        }

        public static Automaton Minimize(Automaton a)
        {
            MinimizationOperations.Minimize(a);
            return a;
        }
    }
}
