using System;
using System.Diagnostics;
using System.Collections.Generic;
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
	/// <tt>Automaton</tt> state.
	/// 
	/// @lucene.experimental
	/// </summary>
	public class State : IComparable<State>
	{

	  internal bool Accept_Renamed;
	  public Transition[] TransitionsArray;
	  public int NumTransitions_Renamed;

	  internal int Number_Renamed;

	  internal int Id;
	  internal static int Next_id;

	  /// <summary>
	  /// Constructs a new state. Initially, the new state is a reject state.
	  /// </summary>
	  public State()
	  {
		ResetTransitions();
		Id = Next_id++;
	  }

	  /// <summary>
	  /// Resets transition set.
	  /// </summary>
	  internal void ResetTransitions()
	  {
		TransitionsArray = new Transition[0];
		NumTransitions_Renamed = 0;
	  }

	  private class TransitionsIterable : IEnumerable<Transition>
	  {
		  private readonly State OuterInstance;

		  public TransitionsIterable(State outerInstance)
		  {
			  this.OuterInstance = outerInstance;
		  }

		public virtual IEnumerator<Transition> GetEnumerator()
		{
		  return new IteratorAnonymousInnerClassHelper(this);
		}

		private class IteratorAnonymousInnerClassHelper : IEnumerator<Transition>
		{
			private readonly TransitionsIterable OuterInstance;

			public IteratorAnonymousInnerClassHelper(TransitionsIterable outerInstance)
			{
				this.OuterInstance = outerInstance;
			}

			internal int upto;
			public virtual bool HasNext()
			{
			  return upto < outerInstance.outerInstance.NumTransitions_Renamed;
			}
			public virtual Transition Next()
			{
			  return outerInstance.outerInstance.TransitionsArray[upto++];
			}
			public virtual void Remove()
			{
			  throw new System.NotSupportedException();
			}
		}
	  }

	  /// <summary>
	  /// Returns the set of outgoing transitions. Subsequent changes are reflected
	  /// in the automaton.
	  /// </summary>
	  /// <returns> transition set </returns>
	  public virtual IEnumerable<Transition> Transitions
	  {
		  get
		  {
			return new TransitionsIterable(this);
		  }
		  set
		  {
			this.NumTransitions_Renamed = value.Length;
			this.TransitionsArray = value;
		  }
	  }

	  public virtual int NumTransitions()
	  {
		return NumTransitions_Renamed;
	  }


	  /// <summary>
	  /// Adds an outgoing transition.
	  /// </summary>
	  /// <param name="t"> transition </param>
	  public virtual void AddTransition(Transition t)
	  {
		if (NumTransitions_Renamed == TransitionsArray.Length)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Transition[] newArray = new Transition[Lucene.Net.Util.ArrayUtil.oversize(1+numTransitions, Lucene.Net.Util.RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
		  Transition[] newArray = new Transition[ArrayUtil.Oversize(1 + NumTransitions_Renamed, RamUsageEstimator.NUM_BYTES_OBJECT_REF)];
		  Array.Copy(TransitionsArray, 0, newArray, 0, NumTransitions_Renamed);
		  TransitionsArray = newArray;
		}
		TransitionsArray[NumTransitions_Renamed++] = t;
	  }

	  /// <summary>
	  /// Sets acceptance for this state.
	  /// </summary>
	  /// <param name="accept"> if true, this state is an accept state </param>
	  public virtual bool Accept
	  {
		  set
		  {
			this.Accept_Renamed = value;
		  }
		  get
		  {
			return Accept_Renamed;
		  }
	  }


	  /// <summary>
	  /// Performs lookup in transitions, assuming determinism.
	  /// </summary>
	  /// <param name="c"> codepoint to look up </param>
	  /// <returns> destination state, null if no matching outgoing transition </returns>
	  /// <seealso cref= #step(int, Collection) </seealso>
	  public virtual State Step(int c)
	  {
		Debug.Assert(c >= 0);
		for (int i = 0;i < NumTransitions_Renamed;i++)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Transition t = transitionsArray[i];
		  Transition t = TransitionsArray[i];
		  if (t.Min_Renamed <= c && c <= t.Max_Renamed)
		  {
			  return t.To;
		  }
		}
		return null;
	  }

	  /// <summary>
	  /// Performs lookup in transitions, allowing nondeterminism.
	  /// </summary>
	  /// <param name="c"> codepoint to look up </param>
	  /// <param name="dest"> collection where destination states are stored </param>
	  /// <seealso cref= #step(int) </seealso>
	  public virtual void Step(int c, ICollection<State> dest)
	  {
		for (int i = 0;i < NumTransitions_Renamed;i++)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Transition t = transitionsArray[i];
		  Transition t = TransitionsArray[i];
		  if (t.Min_Renamed <= c && c <= t.Max_Renamed)
		  {
			  dest.Add(t.To);
		  }
		}
	  }

	  /// <summary>
	  /// Virtually adds an epsilon transition to the target
	  ///  {@code to} state.  this is implemented by copying all
	  ///  transitions from {@code to} to this state, and if {@code
	  ///  to} is an accept state then set accept for this state. 
	  /// </summary>
	  internal virtual void AddEpsilon(State to)
	  {
		if (to.Accept_Renamed)
		{
			Accept_Renamed = true;
		}
		foreach (Transition t in to.Transitions)
		{
		  AddTransition(t);
		}
	  }

	  /// <summary>
	  /// Downsizes transitionArray to numTransitions </summary>
	  public virtual void TrimTransitionsArray()
	  {
		if (NumTransitions_Renamed < TransitionsArray.Length)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Transition[] newArray = new Transition[numTransitions];
		  Transition[] newArray = new Transition[NumTransitions_Renamed];
		  Array.Copy(TransitionsArray, 0, newArray, 0, NumTransitions_Renamed);
		  TransitionsArray = newArray;
		}
	  }

	  /// <summary>
	  /// Reduces this state. A state is "reduced" by combining overlapping
	  /// and adjacent edge intervals with same destination.
	  /// </summary>
	  public virtual void Reduce()
	  {
		if (NumTransitions_Renamed <= 1)
		{
		  return;
		}
		SortTransitions(Transition.CompareByDestThenMinMax);
		State p = null;
		int min = -1, max = -1;
		int upto = 0;
		for (int i = 0;i < NumTransitions_Renamed;i++)
		{
//JAVA TO C# CONVERTER WARNING: The original Java variable was marked 'final':
//ORIGINAL LINE: final Transition t = transitionsArray[i];
		  Transition t = TransitionsArray[i];
		  if (p == t.To)
		  {
			if (t.Min_Renamed <= max + 1)
			{
			  if (t.Max_Renamed > max)
			  {
				  max = t.Max_Renamed;
			  }
			}
			else
			{
			  if (p != null)
			  {
				TransitionsArray[upto++] = new Transition(min, max, p);
			  }
			  min = t.Min_Renamed;
			  max = t.Max_Renamed;
			}
		  }
		  else
		  {
			if (p != null)
			{
			  TransitionsArray[upto++] = new Transition(min, max, p);
			}
			p = t.To;
			min = t.Min_Renamed;
			max = t.Max_Renamed;
		  }
		}

		if (p != null)
		{
		  TransitionsArray[upto++] = new Transition(min, max, p);
		}
		NumTransitions_Renamed = upto;
	  }

	  /// <summary>
	  /// Returns sorted list of outgoing transitions.
	  /// </summary>
	  /// <param name="to_first"> if true, order by (to, min, reverse max); otherwise (min,
	  ///          reverse max, to) </param>
	  /// <returns> transition list </returns>

	  /// <summary>
	  /// Sorts transitions array in-place. </summary>
	  public virtual void SortTransitions(IComparer<Transition> comparator)
	  {
		// mergesort seems to perform better on already sorted arrays:
		if (NumTransitions_Renamed > 1)
		{
			ArrayUtil.TimSort(TransitionsArray, 0, NumTransitions_Renamed, comparator);
		}
	  }

	  /// <summary>
	  /// Return this state's number. 
	  /// <p>
	  /// Expert: Will be useless unless <seealso cref="Automaton#getNumberedStates"/>
	  /// has been called first to number the states. </summary>
	  /// <returns> the number </returns>
	  public virtual int Number
	  {
		  get
		  {
			return Number_Renamed;
		  }
	  }

	  /// <summary>
	  /// Returns string describing this state. Normally invoked via
	  /// <seealso cref="Automaton#toString()"/>.
	  /// </summary>
	  public override string ToString()
	  {
		StringBuilder b = new StringBuilder();
		b.Append("state ").Append(Number_Renamed);
		if (Accept_Renamed)
		{
			b.Append(" [accept]");
		}
		else
		{
			b.Append(" [reject]");
		}
		b.Append(":\n");
		foreach (Transition t in Transitions)
		{
		  b.Append("  ").Append(t.ToString()).Append("\n");
		}
		return b.ToString();
	  }

	  /// <summary>
	  /// Compares this object with the specified object for order. States are
	  /// ordered by the time of construction.
	  /// </summary>
	  public override int CompareTo(State s)
	  {
		return s.Id - Id;
	  }

	  public override int HashCode()
	  {
		return Id;
	  }
	}

}