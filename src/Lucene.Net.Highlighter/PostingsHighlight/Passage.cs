#if FEATURE_BREAKITERATOR
using Lucene.Net.Diagnostics;
using Lucene.Net.Support;
using Lucene.Net.Util;
using System.Collections.Generic;
using System.Diagnostics;

namespace Lucene.Net.Search.PostingsHighlight
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
    /// Represents a passage (typically a sentence of the document).
    /// <para/>
    /// A passage contains <see cref="NumMatches"/> highlights from the query,
    /// and the offsets and query terms that correspond with each match.
    /// @lucene.experimental
    /// </summary>
    public sealed class Passage
    {
        internal int startOffset = -1;
        internal int endOffset = -1;
        internal float score = 0.0f;

        internal int[] matchStarts = new int[8];
        internal int[] matchEnds = new int[8];
        internal BytesRef[] matchTerms = new BytesRef[8];
        internal int numMatches = 0;

        internal void AddMatch(int startOffset, int endOffset, BytesRef term)
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(startOffset >= this.startOffset && startOffset <= this.endOffset);
            if (numMatches == matchStarts.Length)
            {
                int newLength = ArrayUtil.Oversize(numMatches + 1, RamUsageEstimator.NUM_BYTES_OBJECT_REF);
                int[] newMatchStarts = new int[newLength];
                int[] newMatchEnds = new int[newLength];
                BytesRef[] newMatchTerms = new BytesRef[newLength];
                Arrays.Copy(matchStarts, 0, newMatchStarts, 0, numMatches);
                Arrays.Copy(matchEnds, 0, newMatchEnds, 0, numMatches);
                Arrays.Copy(matchTerms, 0, newMatchTerms, 0, numMatches);
                matchStarts = newMatchStarts;
                matchEnds = newMatchEnds;
                matchTerms = newMatchTerms;
            }
            if (Debugging.AssertsEnabled) Debugging.Assert(matchStarts.Length == matchEnds.Length && matchEnds.Length == matchTerms.Length);
            matchStarts[numMatches] = startOffset;
            matchEnds[numMatches] = endOffset;
            matchTerms[numMatches] = term;
            numMatches++;
        }

        private sealed class InPlaceMergeSorterAnonymousClass : InPlaceMergeSorter
        {
            private readonly int[] starts;
            private readonly int[] ends;
            private readonly BytesRef[] terms;

            public InPlaceMergeSorterAnonymousClass(int[] starts, int[] ends, BytesRef[] terms)
            {
                this.starts = starts;
                this.ends = ends;
                this.terms = terms;
            }

            protected override void Swap(int i, int j)
            {
                int temp = starts[i];
                starts[i] = starts[j];
                starts[j] = temp;

                temp = ends[i];
                ends[i] = ends[j];
                ends[j] = temp;

                BytesRef tempTerm = terms[i];
                terms[i] = terms[j];
                terms[j] = tempTerm;
            }

            protected override int Compare(int i, int j)
            {
                return starts[i].CompareTo(starts[j]);
            }
        }

        internal void Sort()
        {
            int[] starts = matchStarts;
            int[] ends = matchEnds;
            BytesRef[] terms = matchTerms;
            new InPlaceMergeSorterAnonymousClass(starts, ends, terms)
                .Sort(0, numMatches);
        }

        internal void Reset()
        {
            startOffset = endOffset = -1;
            score = 0.0f;
            numMatches = 0;
        }

        /// <summary>
        /// Gets the start index (inclusive) of the passage in the
        /// original content: always &gt;= 0.
        /// </summary>
        public int StartOffset => startOffset;

        /// <summary>
        /// Gets the end index (exclusive) of the passage in the 
        ///  original content: always &gt;= <see cref="StartOffset"/>
        /// </summary>
        public int EndOffset => endOffset;

        /// <summary>
        /// Passage's score.
        /// </summary>
        public float Score => score;

        /// <summary>
        /// Number of term matches available in 
        /// <see cref="MatchStarts"/>, <see cref="MatchEnds"/>,
        /// <see cref="MatchTerms"/>
        /// </summary>
        public int NumMatches => numMatches;

        /// <summary>
        /// Start offsets of the term matches, in increasing order.
        /// <para/>
        /// Only <see cref="NumMatches"/> are valid. Note that these
        /// offsets are absolute (not relative to <see cref="StartOffset"/>).
        /// </summary>
        /// <returns></returns>
        public IReadOnlyList<int> MatchStarts => matchStarts;

        /// <summary>
        /// End offsets of the term matches, corresponding with <see cref="MatchStarts"/>. 
        /// <para/>
        /// Only <see cref="NumMatches"/> are valid. Note that its possible that an end offset 
        /// could exceed beyond the bounds of the passage <see cref="EndOffset"/>, if the 
        /// <see cref="Analysis.Analyzer"/> produced a term which spans a passage boundary.
        /// </summary>
        public IReadOnlyList<int> MatchEnds => matchEnds;

        /// <summary>
        /// BytesRef (term text) of the matches, corresponding with <see cref="MatchStarts"/>.
        /// <para/>
        /// Only <see cref="NumMatches"/> are valid.
        /// </summary>
        public IReadOnlyList<BytesRef> MatchTerms => matchTerms;
    }
}
#endif