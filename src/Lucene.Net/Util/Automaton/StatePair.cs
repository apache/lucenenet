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

using System.Runtime.CompilerServices;

namespace Lucene.Net.Util.Automaton
{
    /// <summary>
    /// Pair of states.
    /// <para/>
    /// @lucene.experimental
    /// </summary>
    public class StatePair
    {
        internal State s;
        internal State s1;
        internal State s2;

        internal StatePair(State s, State s1, State s2)
        {
            this.s = s;
            this.s1 = s1;
            this.s2 = s2;
        }

        /// <summary>
        /// Constructs a new state pair.
        /// </summary>
        /// <param name="s1"> First state. </param>
        /// <param name="s2"> Second state. </param>
        public StatePair(State s1, State s2)
        {
            this.s1 = s1;
            this.s2 = s2;
        }

        /// <summary>
        /// Returns first component of this pair.
        /// </summary>
        /// <returns> First state. </returns>
        public virtual State FirstState => s1;

        /// <summary>
        /// Returns second component of this pair.
        /// </summary>
        /// <returns> Second state. </returns>
        public virtual State SecondState => s2;

        /// <summary>
        /// Checks for equality.
        /// </summary>
        /// <param name="obj"> Object to compare with. </param>
        /// <returns> <c>true</c> if <paramref name="obj"/> represents the same pair of states as this
        ///         pair. </returns>
        public override bool Equals(object obj)
        {
            if (obj is StatePair p)
            {
                return p.s1 == s1 && p.s2 == s2;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Returns hash code.
        /// </summary>
        /// <returns> Hash code. </returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode()
        {
            return s1.GetHashCode() + s2.GetHashCode();
        }
    }
}