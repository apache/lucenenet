using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;

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
    public class UnsortedInputIterator : BufferedInputIterator
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
        public UnsortedInputIterator(IInputIterator source)
            : base(source)
        {
            ords = new int[entries.Size];
            Random random = new Random();
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
                Debug.Assert(currentOrd == ords[curPos]);
                return freqs[currentOrd];
            }
        }

        public override BytesRef Next()
        {
            if (++curPos < entries.Size)
            {
                currentOrd = ords[curPos];
                return entries.Get(spare, currentOrd);
            }
            return null;
        }

        public override BytesRef Payload
        {
            get
            {
                {
                    if (HasPayloads && curPos < payloads.Size)
                    {
                        Debug.Assert(currentOrd == ords[curPos]);
                        return payloads.Get(payloadSpare, currentOrd);
                    }
                    return null;
                }
            }
        }

        public override IEnumerable<BytesRef> Contexts
        {
            get
            {
                if (HasContexts && curPos < contextSets.Count)
                {
                    Debug.Assert(currentOrd == ords[curPos]);
                    return contextSets[currentOrd];
                }
                return null;
            }
        }
    }
}