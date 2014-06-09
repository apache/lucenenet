using System;
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
	/// Finite-state automaton with fast run operation.
	/// 
	/// @lucene.experimental
	/// </summary>
	public abstract class RunAutomaton
	{
	  internal readonly int MaxInterval;
	  internal readonly int Size_Renamed;
	  internal readonly bool[] Accept;
	  internal readonly int Initial;
	  internal readonly int[] Transitions; // delta(state,c) = transitions[state*points.length +
						 // getCharClass(c)]
	  internal readonly int[] Points; // char interval start points
	  internal readonly int[] Classmap; // map from char number to class class

	  /// <summary>
	  /// Returns a string representation of this automaton.
	  /// </summary>
	  public override string ToString()
	  {
		StringBuilder b = new StringBuilder();
		b.Append("initial state: ").Append(Initial).Append("\n");
		for (int i = 0; i < Size_Renamed; i++)
		{
		  b.Append("state " + i);
		  if (Accept[i])
		  {
			  b.Append(" [accept]:\n");
		  }
		  else
		  {
			  b.Append(" [reject]:\n");
		  }
		  for (int j = 0; j < Points.Length; j++)
		  {
			int k = Transitions[i * Points.Length + j];
			if (k != -1)
			{
			  int min = Points[j];
			  int max;
			  if (j + 1 < Points.Length)
			  {
				  max = (Points[j + 1] - 1);
			  }
			  else
			  {
				  max = MaxInterval;
			  }
			  b.Append(" ");
			  Transition.AppendCharString(min, b);
			  if (min != max)
			  {
				b.Append("-");
				Transition.AppendCharString(max, b);
			  }
			  b.Append(" -> ").Append(k).Append("\n");
			}
		  }
		}
		return b.ToString();
	  }

	  /// <summary>
	  /// Returns number of states in automaton.
	  /// </summary>
	  public int Size
	  {
		  get
		  {
			return Size_Renamed;
		  }
	  }

	  /// <summary>
	  /// Returns acceptance status for given state.
	  /// </summary>
	  public bool IsAccept(int state)
	  {
		return Accept[state];
	  }

	  /// <summary>
	  /// Returns initial state.
	  /// </summary>
	  public int InitialState
	  {
		  get
		  {
			return Initial;
		  }
	  }

	  /// <summary>
	  /// Returns array of codepoint class interval start points. The array should
	  /// not be modified by the caller.
	  /// </summary>
	  public int[] CharIntervals
	  {
		  get
		  {
			return (int[])(Array)Points.Clone();
		  }
	  }

	  /// <summary>
	  /// Gets character class of given codepoint
	  /// </summary>
	  internal int GetCharClass(int c)
	  {
		return SpecialOperations.FindIndex(c, Points);
	  }

	  /// <summary>
	  /// Constructs a new <code>RunAutomaton</code> from a deterministic
	  /// <code>Automaton</code>.
	  /// </summary>
	  /// <param name="a"> an automaton </param>
	  public RunAutomaton(Automaton a, int maxInterval, bool tableize)
	  {
		this.MaxInterval = maxInterval;
		a.Determinize();
		Points = a.StartPoints;
		State[] states = a.NumberedStates;
		Initial = a.Initial.Number;
		Size_Renamed = states.Length;
		Accept = new bool[Size_Renamed];
		Transitions = new int[Size_Renamed * Points.Length];
		for (int n = 0; n < Size_Renamed * Points.Length; n++)
		{
		  Transitions[n] = -1;
		}
		foreach (State s in states)
		{
		  int n = s.number;
		  Accept[n] = s.accept;
		  for (int c = 0; c < Points.Length; c++)
		  {
			State q = s.Step(Points[c]);
			if (q != null)
			{
				Transitions[n * Points.Length + c] = q.number;
			}
		  }
		}
		/*
		 * Set alphabet table for optimal run performance.
		 */
		if (tableize)
		{
		  Classmap = new int[maxInterval + 1];
		  int i = 0;
		  for (int j = 0; j <= maxInterval; j++)
		  {
			if (i + 1 < Points.Length && j == Points[i + 1])
			{
			  i++;
			}
			Classmap[j] = i;
		  }
		}
		else
		{
		  Classmap = null;
		}
	  }

	  /// <summary>
	  /// Returns the state obtained by reading the given char from the given state.
	  /// Returns -1 if not obtaining any such state. (If the original
	  /// <code>Automaton</code> had no dead states, -1 is returned here if and only
	  /// if a dead state is entered in an equivalent automaton with a total
	  /// transition function.)
	  /// </summary>
	  public int Step(int state, int c)
	  {
		if (Classmap == null)
		{
		  return Transitions[state * Points.Length + GetCharClass(c)];
		}
		else
		{
		  return Transitions[state * Points.Length + Classmap[c]];
		}
	  }

	  public override int GetHashCode()
	  {
		const int prime = 31;
		int result = 1;
		result = prime * result + Initial;
		result = prime * result + MaxInterval;
		result = prime * result + Points.Length;
		result = prime * result + Size_Renamed;
		return result;
	  }

	  public override bool Equals(object obj)
	  {
		if (this == obj)
		{
			return true;
		}
		if (obj == null)
		{
			return false;
		}
		if (this.GetType() != obj.GetType())
		{
			return false;
		}
		RunAutomaton other = (RunAutomaton) obj;
		if (Initial != other.Initial)
		{
			return false;
		}
		if (MaxInterval != other.MaxInterval)
		{
			return false;
		}
		if (Size_Renamed != other.Size_Renamed)
		{
			return false;
		}
		if (!Array.Equals(Points, other.Points))
		{
			return false;
		}
		if (!Array.Equals(Accept, other.Accept))
		{
			return false;
		}
		if (!Array.Equals(Transitions, other.Transitions))
		{
			return false;
		}
		return true;
	  }
	}

}