using Lucene.Net.Diagnostics;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;

namespace Lucene.Net.Search.Suggest
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
    /// This wrapper buffers the incoming elements and makes sure they are in
    /// random order.
    /// @lucene.experimental
    /// </summary>
    public class UnsortedInputEnumerator : BufferedInputEnumerator
    {
        // TODO keep this for now
        private readonly int[] ords;
        private int currentOrd = -1;
        private readonly BytesRef spare = new BytesRef();
        private readonly BytesRef payloadSpare = new BytesRef();

        /// <summary>
        /// Creates a new iterator, wrapping the specified iterator and
        /// returning elements in a random order.
        /// </summary>
        public UnsortedInputEnumerator(IInputEnumerator source)
            : base(source)
        {
            ords = new int[m_entries.Length];
            Random random = new J2N.Randomizer();
            for (int i = 0; i < ords.Length; i++)
            {
                ords[i] = i;
            }
            for (int i = 0; i < ords.Length; i++)
            {
                int randomPosition = random.Next(ords.Length);
                int temp = ords[i];
                ords[i] = ords[randomPosition];
                ords[randomPosition] = temp;
            }
        }

        public override long Weight
        {
            get
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(currentOrd == ords[m_curPos]);
                return m_freqs[currentOrd];
            }
        }

        public override bool MoveNext()
        {
            if (++m_curPos < m_entries.Length)
            {
                currentOrd = ords[m_curPos];
                m_current = m_entries.Get(spare, currentOrd);
                return true;
            }
            m_current = null;
            return false;
        }

        public override BytesRef Payload
        {
            get
            {
                if (HasPayloads && m_curPos < m_payloads.Length)
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(currentOrd == ords[m_curPos]);
                    return m_payloads.Get(payloadSpare, currentOrd);
                }
                return null;
            }
        }

        public override ICollection<BytesRef> Contexts
        {
            get
            {
                if (HasContexts && m_curPos < m_contextSets.Count)
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(currentOrd == ords[m_curPos]);
                    return m_contextSets[currentOrd];
                }
                return null;
            }
        }
    }
}