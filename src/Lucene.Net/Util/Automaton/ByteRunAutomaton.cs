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
    /// Automaton representation for matching UTF-8 <see cref="T:byte[]"/>.
    /// </summary>
    public class ByteRunAutomaton : RunAutomaton
    {
        public ByteRunAutomaton(Automaton a)
            : this(a, false)
        {
        }

        /// <summary>
        /// Expert: if utf8 is true, the input is already byte-based </summary>
        public ByteRunAutomaton(Automaton a, bool utf8)
            : base(utf8 ? a : (new UTF32ToUTF8()).Convert(a), 256, true)
        {
        }

        /// <summary>
        /// Returns <c>true</c> if the given byte array is accepted by this automaton.
        /// </summary>
        public virtual bool Run(byte[] s, int offset, int length)
        {
            var p = m_initial;
            var l = offset + length;
            for (int i = offset; i < l; i++)
            {
                p = Step(p, ((sbyte)s[i]) & 0xFF);
                if (p == -1)
                {
                    return false;
                }
            }
            return m_accept[p];
        }
    }
}