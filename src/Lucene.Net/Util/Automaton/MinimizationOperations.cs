using J2N;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using JCG = J2N.Collections.Generic;

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
    /// Operations for minimizing automata.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public static class MinimizationOperations // LUCENENET specific - made static since all members are static
    {
        /// <summary>
        /// Minimizes (and determinizes if not already deterministic) the given
        /// automaton.
        /// </summary>
        /// <seealso cref="Automaton.SetMinimization(int)"/>
        public static void Minimize(Automaton a)
        {
            if (!a.IsSingleton)
            {
                MinimizeHopcroft(a);
            }
            // recompute hash code
            //a.hash_code = 1a.getNumberOfStates() * 3 + a.getNumberOfTransitions() * 2;
            //if (a.hash_code == 0) a.hash_code = 1;
        }

        /// <summary>
        /// Minimizes the given automaton using Hopcroft's algorithm.
        /// </summary>
        public static void MinimizeHopcroft(Automaton a)
        {
            a.Determinize();
            if (a.initial.numTransitions == 1)
            {
                Transition t = a.initial.TransitionsArray[0];
                if (t.to == a.initial && t.min == Character.MinCodePoint && t.max == Character.MaxCodePoint)
                {
                    return;
                }
            }
            a.Totalize();

            // initialize data structures
            int[] sigma = a.GetStartPoints();
            State[] states = a.GetNumberedStates();
            int sigmaLen = sigma.Length, statesLen = states.Length;
            JCG.List<State>[,] reverse = new JCG.List<State>[statesLen, sigmaLen];
            ISet<State>[] partition = new JCG.HashSet<State>[statesLen];
            JCG.List<State>[] splitblock = new JCG.List<State>[statesLen];
            int[] block = new int[statesLen];
            StateList[,] active = new StateList[statesLen, sigmaLen];
            StateListNode[,] active2 = new StateListNode[statesLen, sigmaLen];
            Queue<Int32Pair> pending = new Queue<Int32Pair>(); // LUCENENET specific - Queue is much more performant than LinkedList
            OpenBitSet pending2 = new OpenBitSet(sigmaLen * statesLen);
            OpenBitSet split = new OpenBitSet(statesLen), 
                refine = new OpenBitSet(statesLen), refine2 = new OpenBitSet(statesLen);
            for (int q = 0; q < statesLen; q++)
            {
                splitblock[q] = new JCG.List<State>();
                partition[q] = new JCG.HashSet<State>();
                for (int x = 0; x < sigmaLen; x++)
                {
                    active[q, x] = new StateList();
                }
            }
            // find initial partition and reverse edges
            for (int q = 0; q < statesLen; q++)
            {
                State qq = states[q];
                int j = qq.accept ? 0 : 1;
                partition[j].Add(qq);
                block[q] = j;
                for (int x = 0; x < sigmaLen; x++)
                {
                    //JCG.List<State>[] r = reverse[qq.Step(sigma[x]).number];
                    var r = qq.Step(sigma[x]).number;
                    if (reverse[r, x] is null)
                    {
                        reverse[r, x] = new JCG.List<State>();
                    }
                    reverse[r, x].Add(qq);
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
                        {
                            active2[qq.number, x] = active[j, x].Add(qq);
                        }
                    }
                }
            }
            // initialize pending
            for (int x = 0; x < sigmaLen; x++)
            {
                int j = (active[0, x].Count <= active[1, x].Count) ? 0 : 1;
                pending.Enqueue(new Int32Pair(j, x));
                pending2.Set(x * statesLen + j);
            }
            // process pending until fixed point
            int k = 2;
            while (pending.Count > 0)
            {
                Int32Pair ip = pending.Dequeue();
                int p = ip.n1;
                int x = ip.n2;
                pending2.Clear(x * statesLen + p);
                // find states that need to be split off their blocks
                for (StateListNode m = active[p, x].First; m != null; m = m.Next)
                {
                    JCG.List<State> r = reverse[m.Q.number, x];
                    if (r != null)
                    {
                        foreach (State s in r)
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
                }
                // refine blocks
                for (int j = refine.NextSetBit(0); j >= 0; j = refine.NextSetBit(j + 1))
                {
                    JCG.List<State> sb = splitblock[j];
                    if (sb.Count < partition[j].Count)
                    {
                        ISet<State> b1 = partition[j];
                        ISet<State> b2 = partition[k];
                        foreach (State s in sb)
                        {
                            b1.Remove(s);
                            b2.Add(s);
                            block[s.number] = k;
                            for (int c = 0; c < sigmaLen; c++)
                            {
                                StateListNode sn = active2[s.number, c];
                                if (sn != null && sn.Sl == active[j, c])
                                {
                                    sn.Remove();
                                    active2[s.number, c] = active[k, c].Add(s);
                                }
                            }
                        }
                        // update pending
                        for (int c = 0; c < sigmaLen; c++)
                        {
                            int aj = active[j, c].Count, ak = active[k, c].Count, ofs = c * statesLen;
                            if (!pending2.Get(ofs + j) && 0 < aj && aj <= ak)
                            {
                                pending2.Set(ofs + j);
                                pending.Enqueue(new Int32Pair(j, c));
                            }
                            else
                            {
                                pending2.Set(ofs + k);
                                pending.Enqueue(new Int32Pair(k, c));
                            }
                        }
                        k++;
                    }
                    refine2.Clear(j);
                    foreach (State s in sb)
                    {
                        split.Clear(s.number);
                    }
                    sb.Clear();
                }
                refine.Clear(0, refine.Length);
            }
            // make a new state for each equivalence class, set initial state
            State[] newstates = new State[k];
            for (int n = 0; n < newstates.Length; n++)
            {
                State s = new State();
                newstates[n] = s;
                foreach (State q in partition[n])
                {
                    if (q == a.initial)
                    {
                        a.initial = s;
                    }
                    s.accept = q.accept;
                    s.number = q.number; // select representative
                    q.number = n;
                }
            }
            // build transitions and set acceptance
            for (int n = 0; n < newstates.Length; n++)
            {
                State s = newstates[n];
                s.accept = states[s.number].accept;
                foreach (Transition t in states[s.number].GetTransitions())
                {
                    s.AddTransition(new Transition(t.min, t.max, newstates[t.to.number]));
                }
            }
            a.ClearNumberedStates();
            a.RemoveDeadTransitions();
        }

        /// <summary>
        /// NOTE: This was IntPair in Lucene
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        internal struct Int32Pair
        {
            internal int n1;
            internal int n2;
            internal Int32Pair(int n1, int n2)
            {
                this.n1 = n1;
                this.n2 = n2;
            }
        }

        internal sealed class StateList
        {
            internal int Count { get; set; } // LUCENENET NOTE: This was size() in Lucene.

            internal StateListNode First { get; set; }

            internal StateListNode Last { get; set; }

            internal StateListNode Add(State q)
            {
                return new StateListNode(q, this);
            }
        }

        internal sealed class StateListNode
        {
            internal State Q { get; private set; }

            internal StateListNode Next { get; set; }

            internal StateListNode Prev { get; set; }

            internal StateList Sl { get; private set; }

            internal StateListNode(State q, StateList sl)
            {
                this.Q = q;
                this.Sl = sl;
                if (sl.Count++ == 0)
                {
                    sl.First = sl.Last = this;
                }
                else
                {
                    sl.Last.Next = this;
                    Prev = sl.Last;
                    sl.Last = this;
                }
            }

            internal void Remove()
            {
                Sl.Count--;
                if (Sl.First == this)
                {
                    Sl.First = Next;
                }
                else
                {
                    Prev.Next = Next;
                }
                if (Sl.Last == this)
                {
                    Sl.Last = Prev;
                }
                else
                {
                    Next.Prev = Prev;
                }
            }
        }
    }
}