using System.Collections;
using System.Collections.Generic;

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
	/// 
	/// @lucene.experimental
	/// </summary>
	public sealed class MinimizationOperations
	{

	  private MinimizationOperations()
	  {
	  }

	  /// <summary>
	  /// Minimizes (and determinizes if not already deterministic) the given
	  /// automaton.
	  /// </summary>
	  /// <seealso cref= Automaton#setMinimization(int) </seealso>
	  public static void Minimize(Automaton a)
	  {
		if (!a.Singleton)
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
		if (a.Initial.numTransitions == 1)
		{
		  Transition t = a.Initial.transitionsArray[0];
		  if (t.To == a.Initial && t.Min_Renamed == char.MIN_CODE_POINT && t.Max_Renamed == char.MAX_CODE_POINT)
		  {
			  return;
		  }
		}
		a.Totalize();

		// initialize data structures
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int[] sigma = a.getStartPoints();
		int[] sigma = a.StartPoints;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final State[] states = a.getNumberedStates();
		State[] states = a.NumberedStates;
		const int sigmaLen = sigma.Length, statesLen = states.Length;
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @SuppressWarnings({"rawtypes","unchecked"}) final java.util.ArrayList<State>[][] reverse = (java.util.ArrayList<State>[][]) new java.util.ArrayList[statesLen][sigmaLen];
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//JAVA TO C# CONVERTER NOTE: The following call to the 'RectangularArrays' helper class reproduces the rectangular array initialization that is automatic in Java:
//ORIGINAL LINE: ArrayList<State>[][] reverse = (ArrayList<State>[][]) new ArrayList[statesLen][sigmaLen];
		List<State>[][] reverse = (List<State>[][]) RectangularArrays.ReturnRectangularArrayListArray(statesLen, sigmaLen);
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @SuppressWarnings({"rawtypes","unchecked"}) final java.util.HashSet<State>[] partition = (java.util.HashSet<State>[]) new java.util.HashSet[statesLen];
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
		HashSet<State>[] partition = (HashSet<State>[]) new HashSet[statesLen];
//JAVA TO C# CONVERTER TODO TASK: Most Java annotations will not have direct .NET equivalent attributes:
//ORIGINAL LINE: @SuppressWarnings({"rawtypes","unchecked"}) final java.util.ArrayList<State>[] splitblock = (java.util.ArrayList<State>[]) new java.util.ArrayList[statesLen];
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
		List<State>[] splitblock = (List<State>[]) new ArrayList[statesLen];
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int[] block = new int[statesLen];
		int[] block = new int[statesLen];
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final StateList[][] active = new StateList[statesLen][sigmaLen];
//JAVA TO C# CONVERTER NOTE: The following call to the 'RectangularArrays' helper class reproduces the rectangular array initialization that is automatic in Java:
//ORIGINAL LINE: StateList[][] active = new StateList[statesLen][sigmaLen];
		StateList[][] active = RectangularArrays.ReturnRectangularStateListArray(statesLen, sigmaLen);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final StateListNode[][] active2 = new StateListNode[statesLen][sigmaLen];
//JAVA TO C# CONVERTER NOTE: The following call to the 'RectangularArrays' helper class reproduces the rectangular array initialization that is automatic in Java:
//ORIGINAL LINE: StateListNode[][] active2 = new StateListNode[statesLen][sigmaLen];
		StateListNode[][] active2 = RectangularArrays.ReturnRectangularStateListNodeArray(statesLen, sigmaLen);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.LinkedList<IntPair> pending = new java.util.LinkedList<>();
		LinkedList<IntPair> pending = new LinkedList<IntPair>();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.BitSet pending2 = new java.util.BitSet(sigmaLen*statesLen);
		BitArray pending2 = new BitArray(sigmaLen * statesLen);
		const BitArray split = new BitArray(statesLen), refine = new BitArray(statesLen), refine2 = new BitArray(statesLen);
		for (int q = 0; q < statesLen; q++)
		{
		  splitblock[q] = new List<>();
		  partition[q] = new HashSet<>();
		  for (int x = 0; x < sigmaLen; x++)
		  {
			active[q][x] = new StateList();
		  }
		}
		// find initial partition and reverse edges
		for (int q = 0; q < statesLen; q++)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final State qq = states[q];
		  State qq = states[q];
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int j = qq.accept ? 0 : 1;
		  int j = qq.Accept_Renamed ? 0 : 1;
		  partition[j].Add(qq);
		  block[q] = j;
		  for (int x = 0; x < sigmaLen; x++)
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.ArrayList<State>[] r = reverse[qq.step(sigma[x]).number];
			List<State>[] r = reverse[qq.Step(sigma[x]).Number_Renamed];
			if (r[x] == null)
			{
			  r[x] = new List<>();
			}
			r[x].Add(qq);
		  }
		}
		// initialize active sets
		for (int j = 0; j <= 1; j++)
		{
		  for (int x = 0; x < sigmaLen; x++)
		  {
			foreach (State qq in partition[j])
			{
			  if (reverse[qq.number][x] != null)
			  {
				active2[qq.number][x] = active[j][x].Add(qq);
			  }
			}
		  }
		}
		// initialize pending
		for (int x = 0; x < sigmaLen; x++)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int j = (active[0][x].size <= active[1][x].size) ? 0 : 1;
		  int j = (active[0][x].Size <= active[1][x].Size) ? 0 : 1;
		  pending.AddLast(new IntPair(j, x));
		  pending2.Set(x * statesLen + j, true);
		}
		// process pending until fixed point
		int k = 2;
		while (pending.Count > 0)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final IntPair ip = pending.removeFirst();
		  IntPair ip = pending.RemoveFirst();
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int p = ip.n1;
		  int p = ip.N1;
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int x = ip.n2;
		  int x = ip.N2;
		  pending2.Set(x * statesLen + p, false);
		  // find states that need to be split off their blocks
		  for (StateListNode m = active[p][x].First; m != null; m = m.Next)
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.ArrayList<State> r = reverse[m.q.number][x];
			List<State> r = reverse[m.q.Number_Renamed][x];
			if (r != null)
			{
				foreach (State s in r)
				{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int i = s.number;
			  int i = s.number;
			  if (!split.Get(i))
			  {
				split.Set(i, true);
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final int j = block[i];
				int j = block[i];
				splitblock[j].Add(s);
				if (!refine2.Get(j))
				{
				  refine2.Set(j, true);
				  refine.Set(j, true);
				}
			  }
				}
			}
		  }
		  // refine blocks
		  for (int j = refine.nextSetBit(0); j >= 0; j = refine.nextSetBit(j + 1))
		  {
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.ArrayList<State> sb = splitblock[j];
			List<State> sb = splitblock[j];
			if (sb.Count < partition[j].Count)
			{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.HashSet<State> b1 = partition[j];
			  HashSet<State> b1 = partition[j];
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final java.util.HashSet<State> b2 = partition[k];
			  HashSet<State> b2 = partition[k];
			  foreach (State s in sb)
			  {
				b1.Remove(s);
				b2.Add(s);
				block[s.number] = k;
				for (int c = 0; c < sigmaLen; c++)
				{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final StateListNode sn = active2[s.number][c];
				  StateListNode sn = active2[s.number][c];
				  if (sn != null && sn.Sl == active[j][c])
				  {
					sn.Remove();
					active2[s.number][c] = active[k][c].Add(s);
				  }
				}
			  }
			  // update pending
			  for (int c = 0; c < sigmaLen; c++)
			  {
				const int aj = active[j][c].Size, ak = active[k][c].Size, ofs = c * statesLen;
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
			{
			  split.Set(s.number, false);
			}
			sb.Clear();
		  }
		  refine.SetAll(false);
		}
		// make a new state for each equivalence class, set initial state
		State[] newstates = new State[k];
		for (int n = 0; n < newstates.Length; n++)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final State s = new State();
		  State s = new State();
		  newstates[n] = s;
		  foreach (State q in partition[n])
		  {
			if (q == a.Initial)
			{
				a.Initial = s;
			}
			s.Accept_Renamed = q.Accept_Renamed;
			s.Number_Renamed = q.Number_Renamed; // select representative
			q.Number_Renamed = n;
		  }
		}
		// build transitions and set acceptance
		for (int n = 0; n < newstates.Length; n++)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final State s = newstates[n];
		  State s = newstates[n];
		  s.Accept_Renamed = states[s.Number_Renamed].Accept_Renamed;
		  foreach (Transition t in states[s.Number_Renamed].Transitions)
		  {
			s.AddTransition(new Transition(t.Min_Renamed, t.Max_Renamed, newstates[t.To.number]));
		  }
		}
		a.ClearNumberedStates();
		a.RemoveDeadTransitions();
	  }

	  internal sealed class IntPair
	  {

		internal readonly int N1, N2;

		internal IntPair(int n1, int n2)
		{
		  this.N1 = n1;
		  this.N2 = n2;
		}
	  }

	  internal sealed class StateList
	  {

		internal int Size;

		internal StateListNode First, Last;

		internal StateListNode Add(State q)
		{
		  return new StateListNode(q, this);
		}
	  }

	  internal sealed class StateListNode
	  {

		internal readonly State q;

		internal StateListNode Next, Prev;

		internal readonly StateList Sl;

		internal StateListNode(State q, StateList sl)
		{
		  this.q = q;
		  this.Sl = sl;
		  if (sl.Size++ == 0)
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
		  Sl.Size--;
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