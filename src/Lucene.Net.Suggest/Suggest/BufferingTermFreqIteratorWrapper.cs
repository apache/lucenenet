using Lucene.Net.Search.Spell;
using Lucene.Net.Util;
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
    /// This wrapper buffers incoming elements.
    /// @lucene.experimental
    /// </summary>
    public class BufferingTermFreqIteratorWrapper : ITermFreqIterator
    {
        // TODO keep this for now
        /// <summary>
        /// buffered term entries </summary>
        protected BytesRefArray m_entries = new BytesRefArray(Counter.NewCounter());
        /// <summary>
        /// current buffer position </summary>
        protected int m_curPos = -1;
        /// <summary>
        /// buffered weights, parallel with <see cref="m_entries"/> </summary>
        protected long[] m_freqs = new long[1];
        private readonly BytesRef spare = new BytesRef();
        private readonly IComparer<BytesRef> comp;

        /// <summary>
        /// Creates a new iterator, buffering entries from the specified iterator
        /// </summary>
        public BufferingTermFreqIteratorWrapper(ITermFreqIterator source)
        {
            this.comp = source.Comparer;
            BytesRef spare;
            int freqIndex = 0;
            while ((spare = source.Next()) != null)
            {
                m_entries.Append(spare);
                if (freqIndex >= m_freqs.Length)
                {
                    m_freqs = ArrayUtil.Grow(m_freqs, m_freqs.Length + 1);
                }
                m_freqs[freqIndex++] = source.Weight;
            }

        }

        public virtual long Weight => m_freqs[m_curPos];

        public virtual BytesRef Next()
        {
            if (++m_curPos < m_entries.Length)
            {
                m_entries.Get(spare, m_curPos);
                return spare;
            }
            return null;
        }

        public virtual IComparer<BytesRef> Comparer => comp;
    }
}