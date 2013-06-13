using Lucene.Net.Support;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Util.Automaton
{
    public static class MinimizationOperations
    {
        public static void Minimize(Automaton a)
        {
            if (!a.IsSingleton())
            {
                MinimizeHopcroft(a);
            }
            // recompute hash code
            //a.hash_code = 1a.getNumberOfStates() * 3 + a.getNumberOfTransitions() * 2;
            //if (a.hash_code == 0) a.hash_code = 1;
        }

        public static void MinimizeHopcroft(Automaton a)
        {
            a.Determinize();
            if (a.InitialState.numTransitions == 1)
            {
                Transition t = a.InitialState.transitionsArray[0];
                if (t.to == a.InitialState && t.min == Character.MIN_CODE_POINT
                    && t.max == Character.MAX_CODE_POINT) return;
            }
            a.Totalize();

            // initialize data structures
            int[] sigma = a.GetStartPoints();
            State[] states = a.GetNumberedStates();
            int sigmaLen = sigma.Length, statesLen = states.Length;
            List<State>[,] reverse =
              new List<State>[statesLen, sigmaLen];
            HashSet<State>[] partition =
              new HashSet<State>[statesLen];
            List<State>[] splitblock =
              new List<State>[statesLen];
            int[] block = new int[statesLen];
            StateList[,] active = new StateList[statesLen, sigmaLen];
            StateListNode[,] active2 = new StateListNode[statesLen, sigmaLen];
            LinkedList<IntPair> pending = new LinkedList<IntPair>();
            BitArray pending2 = new BitArray(sigmaLen * statesLen);
            BitArray split = new BitArray(statesLen),
              refine = new BitArray(statesLen), refine2 = new BitArray(statesLen);
            for (int q = 0; q < statesLen; q++)
            {
                splitblock[q] = new List<State>();
                partition[q] = new HashSet<State>();
                for (int x = 0; x < sigmaLen; x++)
                {
                    active[q, x] = new StateList();
                }
            }
            // find initial partition and reverse edges
            for (int q = 0; q < statesLen; q++)
            {
                State qq = states[q];
                int j = qq.Accept ? 0 : 1;
                partition[j].Add(qq);
                block[q] = j;
                for (int x = 0; x < sigmaLen; x++)
                {
                    var firstIndex = qq.Step(sigma[x]).number;

                    if (reverse[firstIndex, x] == null)
                        reverse[firstIndex, x] = new List<State>();
                    reverse[firstIndex, x].Add(qq);
                }
            }
            // initialize active sets
            for (int j = 0; j <= 1; j++)
            {
                for (int x = 0; x < sigmaLen; x++)
                {
                    foreach (State qq in partition[j])
                    {
                        if (reverse[qq.number, x] != null)
                            active2[qq.number, x] = active[j, x].Add(qq);
                    }
                }
            }
            // initialize pending
            for (int x = 0; x < sigmaLen; x++)
            {
                int j = (active[0, x].size <= active[1, x].size) ? 0 : 1;
                pending.AddLast(new IntPair(j, x));
                pending2.Set(x * statesLen + j, true);
            }
            // process pending until fixed point
            int k = 2;
            while (pending.Count > 0)
            {
                IntPair ip = pending.First.Value;
                pending.RemoveFirst();
                int p = ip.n1;
                int x = ip.n2;
                pending2.Clear(x * statesLen + p);
                // find states that need to be split off their blocks
                for (StateListNode m = active[p, x].first; m != null; m = m.next)
                {
                    List<State> r = reverse[m.q.number, x];
                    if (r != null) foreach (State s in r)
                        {
                            int i = s.number;
                            if (!split.Get(i))
                            {
                                split.Set(i);
                                int j = block[i];
                                splitblock[j].Add(s);
                                if (!refine2.Get(j))
                                {
                                    refine2.Set(j);
                                    refine.Set(j);
                                }
                            }
                        }
                }
                // refine blocks
                for (int j = refine.NextSetBit(0); j >= 0; j = refine.NextSetBit(j + 1))
                {
                    List<State> sb = splitblock[j];
                    if (sb.Count < partition[j].Count)
                    {
                        HashSet<State> b1 = partition[j];
                        HashSet<State> b2 = partition[k];
                        foreach (State s in sb)
                        {
                            b1.Remove(s);
                            b2.Add(s);
                            block[s.number] = k;
                            for (int c = 0; c < sigmaLen; c++)
                            {
                                StateListNode sn = active2[s.number, c];
                                if (sn != null && sn.sl == active[j, c])
                                {
                                    sn.Remove();
                                    active2[s.number, c] = active[k, c].Add(s);
                                }
                            }
                        }
                        // update pending
                        for (int c = 0; c < sigmaLen; c++)
                        {
                            int aj = active[j, c].size,
                              ak = active[k, c].size,
                              ofs = c * statesLen;
                            if (!pending2.Get(ofs + j) && 0 < aj && aj <= ak)
                            {
                                pending2.Set(ofs + j, true);
                                pending.AddLast(new IntPair(j, c));
                            }
                            else
                            {
                                pending2.Set(ofs + k, true);
                                pending.AddLast(new IntPair(k, c));
                            }
                        }
                        k++;
                    }
                    refine2.Set(j, false);
                    foreach (State s in sb)
                        split.Set(s.number, false);
                    sb.Clear();
                }
                refine.SetAll(false);
            }
            // make a new state for each equivalence class, set initial state
            State[] newstates = new State[k];
            for (int n = 0; n < newstates.Length; n++)
            {
                State s = new State();
                newstates[n] = s;
                foreach (State q in partition[n])
                {
                    if (q == a.InitialState) a.InitialState = s;
                    s.Accept = q.Accept;
                    s.number = q.number; // select representative
                    q.number = n;
                }
            }
            // build transitions and set acceptance
            for (int n = 0; n < newstates.Length; n++)
            {
                State s = newstates[n];
                s.Accept = states[s.number].Accept;
                foreach (Transition t in states[s.number].GetTransitions())
                    s.AddTransition(new Transition(t.min, t.max, newstates[t.to.number]));
            }
            a.ClearNumberedStates();
            a.RemoveDeadTransitions();
        }

        internal sealed class IntPair
        {
            internal readonly int n1, n2;

            public IntPair(int n1, int n2)
            {
                this.n1 = n1;
                this.n2 = n2;
            }
        }

        internal class StateList
        {
            internal int size;

            internal StateListNode first, last;

            internal StateListNode Add(State q)
            {
                return new StateListNode(q, this);
            }
        }

        internal class StateListNode
        {
            internal readonly State q;

            internal StateListNode next, prev;

            internal readonly StateList sl;

            internal StateListNode(State q, StateList sl)
            {
                this.q = q;
                this.sl = sl;
                if (sl.size++ == 0) sl.first = sl.last = this;
                else
                {
                    sl.last.next = this;
                    prev = sl.last;
                    sl.last = this;
                }
            }

            internal void Remove()
            {
                sl.size--;
                if (sl.first == this) sl.first = next;
                else prev.next = next;
                if (sl.last == this) sl.last = prev;
                else next.prev = prev;
            }
        }
    }
}
