using Lucene.Net.Support;
using System;
using System.Runtime.CompilerServices;
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
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public abstract class RunAutomaton
    {
        private readonly int _maxInterval;
        private readonly int _size;
        protected readonly bool[] m_accept;
        protected readonly int m_initial;
        protected readonly int[] m_transitions; // delta(state,c) = transitions[state*points.length +

        // getCharClass(c)]
        private readonly int[] _points; // char interval start points

        private readonly int[] _classmap; // map from char number to class class

        /// <summary>
        /// Returns a string representation of this automaton.
        /// </summary>
        public override string ToString()
        {
            var b = new StringBuilder();
            b.Append("initial state: ").Append(m_initial).Append("\n");
            for (int i = 0; i < _size; i++)
            {
                b.Append("state " + i);
                if (m_accept[i])
                {
                    b.Append(" [accept]:\n");
                }
                else
                {
                    b.Append(" [reject]:\n");
                }
                for (int j = 0; j < _points.Length; j++)
                {
                    int k = m_transitions[i * _points.Length + j];
                    if (k != -1)
                    {
                        int min = _points[j];
                        int max;
                        if (j + 1 < _points.Length)
                        {
                            max = (_points[j + 1] - 1);
                        }
                        else
                        {
                            max = _maxInterval;
                        }
                        b.Append(' ');
                        Transition.AppendCharString(min, b);
                        if (min != max)
                        {
                            b.Append('-');
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
        /// <para/>
        /// NOTE: This was size() in Lucene.
        /// </summary>
        public int Count => _size;

        /// <summary>
        /// Returns acceptance status for given state.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsAccept(int state)
        {
            return m_accept[state];
        }

        /// <summary>
        /// Returns initial state.
        /// </summary>
        public int InitialState => m_initial;

        /// <summary>
        /// Returns array of codepoint class interval start points. The array should
        /// not be modified by the caller.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int[] GetCharIntervals()
        {
            return (int[])(Array)_points.Clone();
        }

        /// <summary>
        /// Gets character class of given codepoint.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int GetCharClass(int c)
        {
            return SpecialOperations.FindIndex(c, _points);
        }

        /// <summary>
        /// Constructs a new <see cref="RunAutomaton"/> from a deterministic
        /// <see cref="Automaton"/>.
        /// </summary>
        /// <param name="a"> An automaton. </param>
        /// <param name="maxInterval"></param>
        /// <param name="tableize"></param>
        protected RunAutomaton(Automaton a, int maxInterval, bool tableize) // LUCENENET specific - marked protected instead of public
        {
            this._maxInterval = maxInterval;
            a.Determinize();
            _points = a.GetStartPoints();
            State[] states = a.GetNumberedStates();
            m_initial = a.initial.Number;
            _size = states.Length;
            m_accept = new bool[_size];
            m_transitions = new int[_size * _points.Length];
            for (int n = 0; n < _size * _points.Length; n++)
            {
                m_transitions[n] = -1;
            }
            foreach (State s in states)
            {
                int n = s.number;
                m_accept[n] = s.accept;
                for (int c = 0; c < _points.Length; c++)
                {
                    State q = s.Step(_points[c]);
                    if (q != null)
                    {
                        m_transitions[n * _points.Length + c] = q.number;
                    }
                }
            }
            /*
             * Set alphabet table for optimal run performance.
             */
            if (tableize)
            {
                _classmap = new int[maxInterval + 1];
                int i = 0;
                for (int j = 0; j <= maxInterval; j++)
                {
                    if (i + 1 < _points.Length && j == _points[i + 1])
                    {
                        i++;
                    }
                    _classmap[j] = i;
                }
            }
            else
            {
                _classmap = null;
            }
        }

        /// <summary>
        /// Returns the state obtained by reading the given char from the given state.
        /// Returns -1 if not obtaining any such state. (If the original
        /// <see cref="Automaton"/> had no dead states, -1 is returned here if and only
        /// if a dead state is entered in an equivalent automaton with a total
        /// transition function.)
        /// </summary>
        public int Step(int state, int c)
        {
            if (_classmap is null)
            {
                return m_transitions[state * _points.Length + GetCharClass(c)];
            }
            else
            {
                return m_transitions[state * _points.Length + _classmap[c]];
            }
        }

        public override int GetHashCode()
        {
            const int prime = 31;
            int result = 1;
            result = prime * result + m_initial;
            result = prime * result + _maxInterval;
            result = prime * result + _points.Length;
            result = prime * result + _size;
            return result;
        }

        public override bool Equals(object obj)
        {
            if (this == obj)
            {
                return true;
            }
            if (obj is null)
            {
                return false;
            }
            if (this.GetType() != obj.GetType())
            {
                return false;
            }
            RunAutomaton other = (RunAutomaton)obj;
            if (m_initial != other.m_initial)
            {
                return false;
            }
            if (_maxInterval != other._maxInterval)
            {
                return false;
            }
            if (_size != other._size)
            {
                return false;
            }
            if (!Arrays.Equals(_points, other._points))
            {
                return false;
            }
            if (!Arrays.Equals(m_accept, other.m_accept))
            {
                return false;
            }
            if (!Arrays.Equals(m_transitions, other.m_transitions))
            {
                return false;
            }
            return true;
        }
    }
}