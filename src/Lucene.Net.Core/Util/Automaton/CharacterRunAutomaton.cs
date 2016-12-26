using Lucene.Net.Support;

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
    /// Automaton representation for matching char[].
    /// </summary>
    public class CharacterRunAutomaton : RunAutomaton
    {
        public CharacterRunAutomaton(Automaton a)
            : base(a, Character.MAX_CODE_POINT, false)
        {
        }

        /// <summary>
        /// Returns true if the given string is accepted by this automaton.
        /// </summary>
        public virtual bool Run(string s)
        {
            int p = Initial;
            int l = s.Length;
            for (int i = 0, cp = 0; i < l; i += Character.CharCount(cp))
            {
                p = Step(p, cp = Character.CodePointAt(s, i));
                if (p == -1) return false;
            }
            return Accept[p];
        }

        /// <summary>
        /// Returns true if the given string is accepted by this automaton
        /// </summary>
        public virtual bool Run(char[] s, int offset, int length)
        {
            int p = Initial;
            int l = offset + length;
            
            for (int i = offset, cp = 0; i < l; i += Character.CharCount(cp))
            {
                p = Step(p, cp = Character.CodePointAt(s, i, l));
                if (p == -1) return false;
            }
            return Accept[p];
        }
    }
}