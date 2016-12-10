using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    /// A passage contains {@link #getNumMatches} highlights from the query,
    /// and the offsets and query terms that correspond with each match.
    /// @lucene.experimental
    /// </summary>
    public sealed class Passage : IComparable<Passage> // LUCENENET specific: must implement IComarable to satisfy contract of PriorityQueue (even though it is not used)
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
            Debug.Assert(startOffset >= this.startOffset && startOffset <= this.endOffset);
            if (numMatches == matchStarts.Length)
            {
                int newLength = ArrayUtil.Oversize(numMatches + 1, RamUsageEstimator.NUM_BYTES_OBJECT_REF);
                int[] newMatchStarts = new int[newLength];
                int[] newMatchEnds = new int[newLength];
                BytesRef[] newMatchTerms = new BytesRef[newLength];
                System.Array.Copy(matchStarts, 0, newMatchStarts, 0, numMatches);
                System.Array.Copy(matchEnds, 0, newMatchEnds, 0, numMatches);
                System.Array.Copy(matchTerms, 0, newMatchTerms, 0, numMatches);
                matchStarts = newMatchStarts;
                matchEnds = newMatchEnds;
                matchTerms = newMatchTerms;
            }
            Debug.Assert(matchStarts.Length == matchEnds.Length && matchEnds.Length == matchTerms.Length);
            matchStarts[numMatches] = startOffset;
            matchEnds[numMatches] = endOffset;
            matchTerms[numMatches] = term;
            numMatches++;
        }

        internal class InPlaceMergeSorterAnonymousHelper : InPlaceMergeSorter
        {
            private readonly int[] starts;
            private readonly int[] ends;
            private readonly BytesRef[] terms;

            public InPlaceMergeSorterAnonymousHelper(int[] starts, int[] ends, BytesRef[] terms)
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
            new InPlaceMergeSorterAnonymousHelper(starts, ends, terms)
                .Sort(0, numMatches);


            //        new InPlaceMergeSorter() {
            //  @Override
            //  protected void swap(int i, int j)
            //    {

            //    }

            //    @Override
            //  protected int compare(int i, int j)
            //    {
            //        return Integer.compare(starts[i], starts[j]);
            //    }

            //}.sort(0, numMatches);
        }

        internal void Reset()
        {
            startOffset = endOffset = -1;
            score = 0.0f;
            numMatches = 0;
        }

        /**
         * Start offset of this passage.
         * @return start index (inclusive) of the passage in the 
         *         original content: always &gt;= 0.
         */
        public int StartOffset
        {
            get { return startOffset; }
        }

        /**
         * End offset of this passage.
         * @return end index (exclusive) of the passage in the 
         *         original content: always &gt;= {@link #getStartOffset()}
         */
        public int EndOffset
        {
            get { return endOffset; }
        }

        /**
         * Passage's score.
         */
        public float Score
        {
            get { return score; }
        }

        /**
         * Number of term matches available in 
         * {@link #getMatchStarts}, {@link #getMatchEnds}, 
         * {@link #getMatchTerms}
         */
        public int NumMatches
        {
            get { return numMatches; }
        }

        /**
         * Start offsets of the term matches, in increasing order.
         * <p>
         * Only {@link #getNumMatches} are valid. Note that these
         * offsets are absolute (not relative to {@link #getStartOffset()}).
         */
        public int[] GetMatchStarts()
        {
            return matchStarts;
        }

        /**
         * End offsets of the term matches, corresponding with {@link #getMatchStarts}. 
         * <p>
         * Only {@link #getNumMatches} are valid. Note that its possible that an end offset 
         * could exceed beyond the bounds of the passage ({@link #getEndOffset()}), if the 
         * Analyzer produced a term which spans a passage boundary.
         */
        public int[] GetMatchEnds()
        {
            return matchEnds;
        }

        /**
         * BytesRef (term text) of the matches, corresponding with {@link #getMatchStarts()}.
         * <p>
         * Only {@link #getNumMatches()} are valid.
         */
        public BytesRef[] GetMatchTerms()
        {
            return matchTerms;
        }

        // LUCENENET specific - this is just to satisfy the generic constraint of PriorityQueue, but it is not used.
        public int CompareTo(Passage other)
        {
            throw new NotImplementedException();
        }
    }
}
