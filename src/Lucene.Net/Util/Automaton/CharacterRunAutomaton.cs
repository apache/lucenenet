using J2N;

namespace Lucene.Net.Util.Automaton
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    /// <summary>
    /// Automaton representation for matching <see cref="T:char[]"/>.
    /// </summary>
    public class CharacterRunAutomaton : RunAutomaton
    {
        public CharacterRunAutomaton(Automaton a)
            : base(a, Character.MaxCodePoint, false)
        {
        }

        /// <summary>
        /// Returns <c>true</c> if the given string is accepted by this automaton.
        /// </summary>
        public virtual bool Run(string s)
        {
            int p = m_initial;
            int l = s.Length;
            int cp; // LUCENENET: Removed unnecessary assignment
            for (int i = 0; i < l; i += Character.CharCount(cp))
            {
                p = Step(p, cp = Character.CodePointAt(s, i));
                if (p == -1) return false;
            }
            return m_accept[p];
        }

        /// <summary>
        /// Returns <c>true</c> if the given string is accepted by this automaton.
        /// </summary>
        public virtual bool Run(char[] s, int offset, int length)
        {
            int p = m_initial;
            int l = offset + length;
            int cp; // LUCENENET: Removed unnecessary assignment
            for (int i = offset; i < l; i += Character.CharCount(cp))
            {
                p = Step(p, cp = Character.CodePointAt(s, i, l));
                if (p == -1) return false;
            }
            return m_accept[p];
        }
    }
}