using Lucene.Net.Util;
using System;

namespace Lucene.Net.Index.Memory
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

    public partial class MemoryIndex
    {
        /// <summary>
        /// Index data structure for a field; Contains the tokenized term texts and
        /// their positions.
        /// </summary>
        private sealed class Info
        {
            /// <summary>
            /// Term strings and their positions for this field: <see cref="System.Collections.Generic.IDictionary{String, ArrayIntList}"/>
            /// </summary>
            internal readonly BytesRefHash terms;

            internal readonly SliceByteStartArray sliceArray;

            /// <summary>
            /// Terms sorted ascending by term text; computed on demand 
            /// </summary>
            internal int[] sortedTerms;

            /// <summary>
            /// Number of added tokens for this field 
            /// </summary>
            internal readonly int numTokens;

            /// <summary>
            /// Number of overlapping tokens for this field 
            /// </summary>
            internal readonly int numOverlapTokens;

            /// <summary>
            /// Boost factor for hits for this field 
            /// </summary>
            internal readonly float boost;

            internal readonly long sumTotalTermFreq;

            /// <summary>
            /// the last position encountered in this field for multi field support 
            /// </summary>
            internal int lastPosition;

            /// <summary>
            /// the last offset encountered in this field for multi field support 
            /// </summary>
            internal int lastOffset;

            public Info(BytesRefHash terms, SliceByteStartArray sliceArray, int numTokens, int numOverlapTokens, float boost, int lastPosition, int lastOffset, long sumTotalTermFreq)
            {
                this.terms = terms;
                this.sliceArray = sliceArray;
                this.numTokens = numTokens;
                this.numOverlapTokens = numOverlapTokens;
                this.boost = boost;
                this.sumTotalTermFreq = sumTotalTermFreq;
                this.lastPosition = lastPosition;
                this.lastOffset = lastOffset;
            }

            public long SumTotalTermFreq => sumTotalTermFreq;

            /// <summary>
            /// Sorts hashed terms into ascending order, reusing memory along the
            /// way. Note that sorting is lazily delayed until required (often it's
            /// not required at all). If a sorted view is required then hashing +
            /// sort + binary search is still faster and smaller than TreeMap usage
            /// (which would be an alternative and somewhat more elegant approach,
            /// apart from more sophisticated Tries / prefix trees).
            /// </summary>
            public void SortTerms()
            {
                if (sortedTerms is null)
                {
                    sortedTerms = terms.Sort(BytesRef.UTF8SortedAsUnicodeComparer);
                }
            }

            public float Boost => boost;
        }
    }
}
