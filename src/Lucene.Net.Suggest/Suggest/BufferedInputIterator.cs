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
    public class BufferedInputIterator : IInputIterator
    {
        // TODO keep this for now
        /// <summary>
        /// buffered term entries </summary>
        protected internal BytesRefArray entries = new BytesRefArray(Counter.NewCounter());
        /// <summary>
        /// buffered payload entries </summary>
        protected internal BytesRefArray payloads = new BytesRefArray(Counter.NewCounter());
        /// <summary>
        /// buffered context set entries </summary>
        protected internal IList<IEnumerable<BytesRef>> contextSets = new List<IEnumerable<BytesRef>>();
        /// <summary>
        /// current buffer position </summary>
        protected internal int curPos = -1;
        /// <summary>
        /// buffered weights, parallel with <see cref="entries"/> </summary>
        protected internal long[] freqs = new long[1];
        private readonly BytesRef spare = new BytesRef();
        private readonly BytesRef payloadSpare = new BytesRef();
        private readonly bool hasPayloads;
        private readonly IComparer<BytesRef> comp;

        private readonly bool hasContexts;

        /// <summary>
        /// Creates a new iterator, buffering entries from the specified iterator </summary>
        public BufferedInputIterator(IInputIterator source)
        {
            BytesRef spare;
            int freqIndex = 0;
            hasPayloads = source.HasPayloads;
            hasContexts = source.HasContexts;
            while ((spare = source.Next()) != null)
            {
                entries.Append(spare);
                if (hasPayloads)
                {
                    payloads.Append(source.Payload);
                }
                if (hasContexts)
                {
                    contextSets.Add(source.Contexts);
                }
                if (freqIndex >= freqs.Length)
                {
                    freqs = ArrayUtil.Grow(freqs, freqs.Length + 1);
                }
                freqs[freqIndex++] = source.Weight;
            }
            comp = source.Comparator;
        }

        public virtual long Weight
        {
            get { return freqs[curPos]; }
        }

        public virtual BytesRef Next()
        {
            if (++curPos < entries.Length)
            {
                entries.Get(spare, curPos);
                return spare;
            }
            return null;
        }

        public virtual BytesRef Payload
        {
            get
            {
                if (hasPayloads && curPos < payloads.Length)
                {
                    return payloads.Get(payloadSpare, curPos);
                }
                return null;
            }
        }

        public virtual bool HasPayloads
        {
            get { return hasPayloads; }
        }

        public virtual IComparer<BytesRef> Comparator
        {
            get
            {
                return comp;
            }
        }

        public virtual IEnumerable<BytesRef> Contexts
        {
            get
            {
                if (hasContexts && curPos < contextSets.Count)
                {
                    return contextSets[curPos];
                }
                return null;
            }
        }

        public virtual bool HasContexts
        {
            get { return hasContexts; }
        }
    }
}