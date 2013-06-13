using Lucene.Net.Support;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util.Automaton
{
    public static class BasicOperations
    {
        public static Automaton Concatenate(Automaton a1, Automaton a2)
        {
            if (a1.IsSingleton() && a2.IsSingleton()) return BasicAutomata
                .MakeString(a1.Singleton + a2.Singleton);
            if (IsEmpty(a1) || IsEmpty(a2))
                return BasicAutomata.MakeEmpty();
            // adding epsilon transitions with the NFA concatenation algorithm
            // in this case always produces a resulting DFA, preventing expensive
            // redundant determinize() calls for this common case.
            bool deterministic = a1.IsSingleton() && a2.Deterministic;
            if (a1 == a2)
            {
                a1 = a1.CloneExpanded();
                a2 = a2.CloneExpanded();
            }
            else
            {
                a1 = a1.CloneExpandedIfRequired();
                a2 = a2.CloneExpandedIfRequired();
            }
            foreach (State s in a1.GetAcceptStates())
            {
                s.Accept = false;
                s.AddEpsilon(a2.InitialState);
            }
            a1.Deterministic = deterministic;
            //a1.clearHashCode();
            a1.ClearNumberedStates();
            a1.CheckMinimizeAlways();
            return a1;
        }

        public static Automaton Concatenate(IList<Automaton> l)
        {
            if (l.Count == 0) return BasicAutomata.MakeEmptyString();
            bool all_singleton = true;
            foreach (Automaton a in l)
                if (!a.IsSingleton())
                {
                    all_singleton = false;
                    break;
                }
            if (all_singleton)
            {
                StringBuilder b = new StringBuilder();
                foreach (Automaton a in l)
                    b.Append(a.Singleton);
                return BasicAutomata.MakeString(b.ToString());
            }
            else
            {
                foreach (Automaton a in l)
                    if (BasicOperations.IsEmpty(a)) return BasicAutomata.MakeEmpty();
                ISet<int> ids = new HashSet<int>();
                foreach (Automaton a in l)
                    ids.Add(a.GetHashCode());
                bool has_aliases = ids.Count != l.Count;
                Automaton b = l[0];
                if (has_aliases) b = b.CloneExpanded();
                else b = b.CloneExpandedIfRequired();
                ISet<State> ac = b.GetAcceptStates();
                bool first = true;
                foreach (Automaton a in l)
                    if (first) first = false;
                    else
                    {
                        if (a.IsEmptyString()) continue;
                        Automaton aa = a;
                        if (has_aliases) aa = aa.CloneExpanded();
                        else aa = aa.CloneExpandedIfRequired();
                        ISet<State> ns = aa.GetAcceptStates();
                        foreach (State s in ac)
                        {
                            s.Accept = false;
                            s.AddEpsilon(aa.InitialState);
                            if (s.Accept) ns.Add(s);
                        }
                        ac = ns;
                    }
                b.Deterministic = false;
                //b.clearHashCode();
                b.ClearNumberedStates();
                b.CheckMinimizeAlways();
                return b;
            }
        }

        public static Automaton Optional(Automaton a)
        {
            a = a.CloneExpandedIfRequired();
            State s = new State();
            s.AddEpsilon(a.InitialState);
            s.Accept = true;
            a.InitialState = s;
            a.Deterministic = false;
            //a.clearHashCode();
            a.ClearNumberedStates();
            a.CheckMinimizeAlways();
            return a;
        }

        public static Automaton Repeat(Automaton a)
        {
            a = a.CloneExpanded();
            State s = new State();
            s.Accept = true;
            s.AddEpsilon(a.InitialState);
            foreach (State p in a.GetAcceptStates())
                p.AddEpsilon(s);
            a.InitialState = s;
            a.Deterministic = false;
            //a.clearHashCode();
            a.ClearNumberedStates();
            a.CheckMinimizeAlways();
            return a;
        }

        public static Automaton Repeat(Automaton a, int min)
        {
            if (min == 0) return Repeat(a);
            List<Automaton> al = new List<Automaton>();
            while (min-- > 0)
                al.Add(a);
            al.Add(Repeat(a));
            return Concatenate(al);
        }

        public static Automaton Repeat(Automaton a, int min, int max)
        {
            if (min > max) return BasicAutomata.MakeEmpty();
            max -= min;
            a.ExpandSingleton();
            Automaton b;
            if (min == 0) b = BasicAutomata.MakeEmptyString();
            else if (min == 1) b = (Automaton)a.Clone();
            else
            {
                List<Automaton> al = new List<Automaton>();
                while (min-- > 0)
                    al.Add(a);
                b = Concatenate(al);
            }
            if (max > 0)
            {
                Automaton d = (Automaton)a.Clone();
                while (--max > 0)
                {
                    Automaton c = (Automaton)a.Clone();
                    foreach (State p in c.GetAcceptStates())
                        p.AddEpsilon(d.InitialState);
                    d = c;
                }
                foreach (State p in b.GetAcceptStates())
                    p.AddEpsilon(d.InitialState);
                b.Deterministic = false;
                //b.clearHashCode();
                b.ClearNumberedStates();
                b.CheckMinimizeAlways();
            }
            return b;
        }

        public static Automaton Complement(Automaton a)
        {
            a = a.CloneExpandedIfRequired();
            a.Determinize();
            a.Totalize();
            foreach (State p in a.GetNumberedStates())
                p.Accept = !p.Accept;
            a.RemoveDeadTransitions();
            return a;
        }

        public static Automaton Minus(Automaton a1, Automaton a2)
        {
            if (BasicOperations.IsEmpty(a1) || a1 == a2) return BasicAutomata
                .MakeEmpty();
            if (BasicOperations.IsEmpty(a2)) return a1.CloneIfRequired();
            if (a1.IsSingleton())
            {
                if (BasicOperations.Run(a2, a1.Singleton)) return BasicAutomata.MakeEmpty();
                else return a1.CloneIfRequired();
            }
            return Intersection(a1, a2.Complement());
        }

        public static Automaton Intersection(Automaton a1, Automaton a2)
        {
            if (a1.IsSingleton())
            {
                if (BasicOperations.Run(a2, a1.Singleton)) return a1.CloneIfRequired();
                else return BasicAutomata.MakeEmpty();
            }
            if (a2.IsSingleton())
            {
                if (BasicOperations.Run(a1, a2.Singleton)) return a2.CloneIfRequired();
                else return BasicAutomata.MakeEmpty();
            }
            if (a1 == a2) return a1.CloneIfRequired();
            Transition[][] transitions1 = a1.GetSortedTransitions();
            Transition[][] transitions2 = a2.GetSortedTransitions();
            Automaton c = new Automaton();
            LinkedList<StatePair> worklist = new LinkedList<StatePair>();
            HashMap<StatePair, StatePair> newstates = new HashMap<StatePair, StatePair>();
            StatePair p = new StatePair(c.InitialState, a1.InitialState, a2.InitialState);
            worklist.AddLast(p);
            newstates[p] = p;
            while (worklist.Count > 0)
            {
                p = worklist.First.Value;
                worklist.RemoveFirst();
                p.State.Accept = p.FirstState.Accept && p.SecondState.Accept;
                Transition[] t1 = transitions1[p.FirstState.number];
                Transition[] t2 = transitions2[p.SecondState.number];
                for (int n1 = 0, b2 = 0; n1 < t1.Length; n1++)
                {
                    while (b2 < t2.Length && t2[b2].max < t1[n1].min)
                        b2++;
                    for (int n2 = b2; n2 < t2.Length && t1[n1].max >= t2[n2].min; n2++)
                        if (t2[n2].max >= t1[n1].min)
                        {
                            StatePair q = new StatePair(t1[n1].to, t2[n2].to);
                            StatePair r = newstates[q];
                            if (r == null)
                            {
                                q.State = new State();
                                worklist.AddLast(q);
                                newstates[q] = q;
                                r = q;
                            }
                            int min = t1[n1].min > t2[n2].min ? t1[n1].min : t2[n2].min;
                            int max = t1[n1].max < t2[n2].max ? t1[n1].max : t2[n2].max;
                            p.State.AddTransition(new Transition(min, max, r.State));
                        }
                }
            }
            c.Deterministic = a1.Deterministic && a2.Deterministic;
            c.RemoveDeadTransitions();
            c.CheckMinimizeAlways();
            return c;
        }

        public static bool SameLanguage(Automaton a1, Automaton a2)
        {
            if (a1 == a2)
            {
                return true;
            }
            if (a1.IsSingleton() && a2.IsSingleton())
            {
                return a1.Singleton.Equals(a2.Singleton);
            }
            else if (a1.IsSingleton())
            {
                // subsetOf is faster if the first automaton is a singleton
                return SubsetOf(a1, a2) && SubsetOf(a2, a1);
            }
            else
            {
                return SubsetOf(a2, a1) && SubsetOf(a1, a2);
            }
        }

        public static bool SubsetOf(Automaton a1, Automaton a2)
        {
            if (a1 == a2) return true;
            if (a1.IsSingleton())
            {
                if (a2.IsSingleton()) return a1.Singleton.Equals(a2.Singleton);
                return BasicOperations.Run(a2, a1.Singleton);
            }
            a2.Determinize();
            Transition[][] transitions1 = a1.GetSortedTransitions();
            Transition[][] transitions2 = a2.GetSortedTransitions();
            LinkedList<StatePair> worklist = new LinkedList<StatePair>();
            HashSet<StatePair> visited = new HashSet<StatePair>();
            StatePair p = new StatePair(a1.InitialState, a2.InitialState);
            worklist.AddLast(p);
            visited.Add(p);
            while (worklist.Count > 0)
            {
                p = worklist.First.Value;
                worklist.RemoveFirst();
                if (p.FirstState.Accept && !p.SecondState.Accept)
                {
                    return false;
                }
                Transition[] t1 = transitions1[p.FirstState.number];
                Transition[] t2 = transitions2[p.SecondState.number];
                for (int n1 = 0, b2 = 0; n1 < t1.Length; n1++)
                {
                    while (b2 < t2.Length && t2[b2].max < t1[n1].min)
                        b2++;
                    int min1 = t1[n1].min, max1 = t1[n1].max;

                    for (int n2 = b2; n2 < t2.Length && t1[n1].max >= t2[n2].min; n2++)
                    {
                        if (t2[n2].min > min1)
                        {
                            return false;
                        }
                        if (t2[n2].max < Character.MAX_CODE_POINT) min1 = t2[n2].max + 1;
                        else
                        {
                            min1 = Character.MAX_CODE_POINT;
                            max1 = Character.MIN_CODE_POINT;
                        }
                        StatePair q = new StatePair(t1[n1].to, t2[n2].to);
                        if (!visited.Contains(q))
                        {
                            worklist.AddLast(q);
                            visited.Add(q);
                        }
                    }
                    if (min1 <= max1)
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public static Automaton Union(Automaton a1, Automaton a2)
        {
            if ((a1.IsSingleton() && a2.IsSingleton() && a1.Singleton
                .Equals(a2.Singleton))
                || a1 == a2) return a1.CloneIfRequired();
            if (a1 == a2)
            {
                a1 = a1.CloneExpanded();
                a2 = a2.CloneExpanded();
            }
            else
            {
                a1 = a1.CloneExpandedIfRequired();
                a2 = a2.CloneExpandedIfRequired();
            }
            State s = new State();
            s.AddEpsilon(a1.InitialState);
            s.AddEpsilon(a2.InitialState);
            a1.InitialState = s;
            a1.Deterministic = false;
            //a1.clearHashCode();
            a1.ClearNumberedStates();
            a1.CheckMinimizeAlways();
            return a1;
        }

        public static Automaton Union(ICollection<Automaton> l)
        {
            ISet<int> ids = new HashSet<int>();
            foreach (Automaton a in l)
                ids.Add(a.GetHashCode());
            bool has_aliases = ids.Count != l.Count;
            State s = new State();
            foreach (Automaton b in l)
            {
                if (BasicOperations.IsEmpty(b)) continue;
                Automaton bb = b;
                if (has_aliases) bb = bb.CloneExpanded();
                else bb = bb.CloneExpandedIfRequired();
                s.AddEpsilon(bb.InitialState);
            }
            Automaton a2 = new Automaton();
            a2.InitialState = s;
            a2.Deterministic = false;
            //a.clearHashCode();
            a2.ClearNumberedStates();
            a2.CheckMinimizeAlways();
            return a2;
        }

        private sealed class TransitionList
        {
            internal Transition[] transitions = new Transition[2];
            internal int count;

            public void Add(Transition t)
            {
                if (transitions.Length == count)
                {
                    Transition[] newArray = new Transition[ArrayUtil.Oversize(1 + count, RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
                    Array.Copy(transitions, 0, newArray, 0, count);
                    transitions = newArray;
                }
                transitions[count++] = t;
            }
        }

        private sealed class PointTransitions : IComparable<PointTransitions>
        {
            internal int point;
            internal readonly TransitionList ends = new TransitionList();
            internal readonly TransitionList starts = new TransitionList();

            public int CompareTo(PointTransitions other)
            {
                return point - other.point;
            }

            public void Reset(int point)
            {
                this.point = point;
                ends.count = 0;
                starts.count = 0;
            }


            public override bool Equals(Object other)
            {
                return ((PointTransitions)other).point == point;
            }

            public override int GetHashCode()
            {
                return point;
            }
        }

        private sealed class PointTransitionSet
        {
            internal int count;
            internal PointTransitions[] points = new PointTransitions[5];

            private const int HASHMAP_CUTOVER = 30;
            private readonly HashMap<int, PointTransitions> map = new HashMap<int, PointTransitions>();
            private bool useHash = false;

            private PointTransitions Next(int point)
            {
                // 1st time we are seeing this point
                if (count == points.Length)
                {
                    PointTransitions[] newArray = new PointTransitions[ArrayUtil.Oversize(1 + count, RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
                    Array.Copy(points, 0, newArray, 0, count);
                    points = newArray;
                }
                PointTransitions points0 = points[count];
                if (points0 == null)
                {
                    points0 = points[count] = new PointTransitions();
                }
                points0.Reset(point);
                count++;
                return points0;
            }

            private PointTransitions Find(int point)
            {
                if (useHash)
                {
                    int pi = point;
                    PointTransitions p = map[pi];
                    if (p == null)
                    {
                        p = Next(point);
                        map[pi] = p;
                    }
                    return p;
                }
                else
                {
                    for (int i = 0; i < count; i++)
                    {
                        if (points[i].point == point)
                        {
                            return points[i];
                        }
                    }

                    PointTransitions p = Next(point);
                    if (count == HASHMAP_CUTOVER)
                    {
                        // switch to HashMap on the fly
                        //assert map.size() == 0;
                        for (int i = 0; i < count; i++)
                        {
                            map[points[i].point] = points[i];
                        }
                        useHash = true;
                    }
                    return p;
                }
            }

            public void Reset()
            {
                if (useHash)
                {
                    map.Clear();
                    useHash = false;
                }
                count = 0;
            }

            public void Sort()
            {
                // mergesort seems to perform better on already sorted arrays:
                if (count > 1) ArrayUtil.MergeSort(points, 0, count);
            }

            public void Add(Transition t)
            {
                Find(t.min).starts.Add(t);
                Find(1 + t.max).ends.Add(t);
            }

            public override String ToString()
            {
                StringBuilder s = new StringBuilder();
                for (int i = 0; i < count; i++)
                {
                    if (i > 0)
                    {
                        s.Append(' ');
                    }
                    s.Append(points[i].point).Append(':').Append(points[i].starts.count).Append(',').Append(points[i].ends.count);
                }
                return s.ToString();
            }
        }

        public static void Determinize(Automaton a)
        {
            if (a.Deterministic || a.IsSingleton())
            {
                return;
            }

            State[] allStates = a.GetNumberedStates();

            // subset construction
            bool initAccept = a.InitialState.Accept;
            int initNumber = a.InitialState.number;
            a.InitialState = new State();
            SortedIntSet.FrozenIntSet initialset = new SortedIntSet.FrozenIntSet(initNumber, a.InitialState);

            LinkedList<SortedIntSet.FrozenIntSet> worklist = new LinkedList<SortedIntSet.FrozenIntSet>();
            IDictionary<SortedIntSet.FrozenIntSet, State> newstate = new HashMap<SortedIntSet.FrozenIntSet, State>();

            worklist.AddLast(initialset);

            a.InitialState.Accept = initAccept;
            newstate[initialset] = a.InitialState;

            int newStateUpto = 0;
            State[] newStatesArray = new State[5];
            newStatesArray[newStateUpto] = a.InitialState;
            a.InitialState.number = newStateUpto;
            newStateUpto++;

            // like Set<Integer,PointTransitions>
            PointTransitionSet points = new PointTransitionSet();

            // like SortedMap<Integer,Integer>
            SortedIntSet statesSet = new SortedIntSet(5);

            while (worklist.Count > 0)
            {
                SortedIntSet.FrozenIntSet s = worklist.First.Value;
                worklist.RemoveFirst();

                // Collate all outgoing transitions by min/1+max:
                for (int i = 0; i < s.values.Length; i++)
                {
                    State s0 = allStates[s.values[i]];
                    for (int j = 0; j < s0.numTransitions; j++)
                    {
                        points.Add(s0.transitionsArray[j]);
                    }
                }

                if (points.count == 0)
                {
                    // No outgoing transitions -- skip it
                    continue;
                }

                points.Sort();

                int lastPoint = -1;
                int accCount = 0;

                State r = s.state;
                for (int i = 0; i < points.count; i++)
                {

                    int point = points.points[i].point;

                    if (statesSet.upto > 0)
                    {
                        //assert lastPoint != -1;

                        statesSet.ComputeHash();

                        State q = null;
                        // code was q = newstate[statesSet] but that doesn't work since statesSet is not FrozenIntSet.

                        if (q == null)
                        {
                            q = new State();
                            SortedIntSet.FrozenIntSet p = statesSet.Freeze(q);
                            worklist.AddLast(p);
                            if (newStateUpto == newStatesArray.Length)
                            {
                                State[] newArray = new State[ArrayUtil.Oversize(1 + newStateUpto, RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
                                Array.Copy(newStatesArray, 0, newArray, 0, newStateUpto);
                                newStatesArray = newArray;
                            }
                            newStatesArray[newStateUpto] = q;
                            q.number = newStateUpto;
                            newStateUpto++;
                            q.Accept = accCount > 0;
                            newstate[p] = q;
                        }
                        else
                        {
                            //assert (accCount > 0 ? true:false) == q.accept: "accCount=" + accCount + " vs existing accept=" + q.accept + " states=" + statesSet;
                        }

                        r.AddTransition(new Transition(lastPoint, point - 1, q));
                    }

                    // process transitions that end on this point
                    // (closes an overlapping interval)
                    Transition[] transitions = points.points[i].ends.transitions;
                    int limit = points.points[i].ends.count;
                    for (int j = 0; j < limit; j++)
                    {
                        Transition t = transitions[j];
                        int num = t.to.number;
                        statesSet.Decr(num);
                        accCount -= t.to.Accept ? 1 : 0;
                    }
                    points.points[i].ends.count = 0;

                    // process transitions that start on this point
                    // (opens a new interval)
                    transitions = points.points[i].starts.transitions;
                    limit = points.points[i].starts.count;
                    for (int j = 0; j < limit; j++)
                    {
                        Transition t = transitions[j];
                        int num = t.to.number;
                        statesSet.Incr(num);
                        accCount += t.to.Accept ? 1 : 0;
                    }
                    lastPoint = point;
                    points.points[i].starts.count = 0;
                }
                points.Reset();
                //assert statesSet.upto == 0: "upto=" + statesSet.upto;
            }
            a.Deterministic = true;
            a.SetNumberedStates(newStatesArray, newStateUpto);
        }

        public static void AddEpsilons(Automaton a, ICollection<StatePair> pairs)
        {
            a.ExpandSingleton();
            HashMap<State, HashSet<State>> forward = new HashMap<State, HashSet<State>>();
            HashMap<State, HashSet<State>> back = new HashMap<State, HashSet<State>>();
            foreach (StatePair p in pairs)
            {
                HashSet<State> to = forward[p.FirstState];
                if (to == null)
                {
                    to = new HashSet<State>();
                    forward[p.FirstState] = to;
                }
                to.Add(p.SecondState);
                HashSet<State> from = back[p.SecondState];
                if (from == null)
                {
                    from = new HashSet<State>();
                    back[p.SecondState] = from;
                }
                from.Add(p.FirstState);
            }
            // calculate epsilon closure
            LinkedList<StatePair> worklist = new LinkedList<StatePair>(pairs);
            HashSet<StatePair> workset = new HashSet<StatePair>(pairs);
            while (worklist.Count > 0)
            {
                StatePair p = worklist.First.Value;
                worklist.RemoveFirst();
                workset.Remove(p);
                HashSet<State> to = forward[p.SecondState];
                HashSet<State> from = back[p.FirstState];
                if (to != null)
                {
                    foreach (State s in to)
                    {
                        StatePair pp = new StatePair(p.FirstState, s);
                        if (!pairs.Contains(pp))
                        {
                            pairs.Add(pp);
                            forward[p.FirstState].Add(s);
                            back[s].Add(p.FirstState);
                            worklist.AddLast(pp);
                            workset.Add(pp);
                            if (from != null)
                            {
                                foreach (State q in from)
                                {
                                    StatePair qq = new StatePair(q, p.FirstState);
                                    if (!workset.Contains(qq))
                                    {
                                        worklist.AddLast(qq);
                                        workset.Add(qq);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            // add transitions
            foreach (StatePair p in pairs)
                p.FirstState.AddEpsilon(p.SecondState);
            a.Deterministic = false;
            //a.clearHashCode();
            a.ClearNumberedStates();
            a.CheckMinimizeAlways();
        }

        public static bool IsEmptyString(Automaton a)
        {
            if (a.IsSingleton()) return a.Singleton.Length == 0;
            else return a.InitialState.Accept && a.InitialState.NumTransitions == 0;
        }

        public static bool IsEmpty(Automaton a)
        {
            if (a.IsSingleton()) return false;
            return !a.InitialState.Accept && a.InitialState.NumTransitions == 0;
        }

        public static bool IsTotal(Automaton a)
        {
            if (a.IsSingleton()) return false;
            if (a.InitialState.Accept && a.InitialState.NumTransitions == 1)
            {
                Transition t = a.InitialState.GetTransitions().First();
                return t.to == a.InitialState && t.min == Character.MIN_CODE_POINT
                    && t.max == Character.MAX_CODE_POINT;
            }
            return false;
        }

        public static bool Run(Automaton a, String s)
        {
            if (a.IsSingleton()) return s.Equals(a.Singleton);
            if (a.Deterministic)
            {
                State p = a.InitialState;
                for (int i = 0, cp = 0; i < s.Length; i += 1)
                {
                    State q = p.Step(cp = s[i]);
                    if (q == null) return false;
                    p = q;
                }
                return p.Accept;
            }
            else
            {
                State[] states = a.GetNumberedStates();
                LinkedList<State> pp = new LinkedList<State>();
                LinkedList<State> pp_other = new LinkedList<State>();
                BitArray bb = new BitArray(states.Length);
                BitArray bb_other = new BitArray(states.Length);
                pp.AddLast(a.InitialState);
                List<State> dest = new List<State>();
                bool accept = a.InitialState.Accept;
                for (int i = 0, c = 0; i < s.Length; i += 1)
                {
                    c = s[i];
                    accept = false;
                    pp_other.Clear();
                    bb_other.SetAll(false);
                    foreach (State p in pp)
                    {
                        dest.Clear();
                        p.Step(c, dest);
                        foreach (State q in dest)
                        {
                            if (q.Accept) accept = true;
                            if (!bb_other.Get(q.number))
                            {
                                bb_other.Set(q.number, true);
                                pp_other.AddLast(q);
                            }
                        }
                    }
                    LinkedList<State> tp = pp;
                    pp = pp_other;
                    pp_other = tp;
                    BitArray tb = bb;
                    bb = bb_other;
                    bb_other = tb;
                }
                return accept;
            }
        }
    }
}
