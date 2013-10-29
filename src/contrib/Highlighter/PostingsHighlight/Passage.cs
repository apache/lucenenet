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

using Lucene.Net.Support;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Search.PostingsHighlight
{
    public sealed class Passage
    {
        int startOffset = -1;
        int endOffset = -1;
        float score = 0F;
        int[] matchStarts = new int[8];
        int[] matchEnds = new int[8];
        BytesRef[] matchTerms = new BytesRef[8];
        int numMatches = 0;
        
        internal void AddMatch(int startOffset, int endOffset, BytesRef term)
        {
            if (numMatches == matchStarts.Length)
            {
                int newLength = ArrayUtil.Oversize(numMatches + 1, RamUsageEstimator.NUM_BYTES_OBJECT_REF);
                int[] newMatchStarts = new int[newLength];
                int[] newMatchEnds = new int[newLength];
                BytesRef[] newMatchTerms = new BytesRef[newLength];
                Array.Copy(matchStarts, 0, newMatchStarts, 0, numMatches);
                Array.Copy(matchEnds, 0, newMatchEnds, 0, numMatches);
                Array.Copy(matchTerms, 0, newMatchTerms, 0, numMatches);
                matchStarts = newMatchStarts;
                matchEnds = newMatchEnds;
                matchTerms = newMatchTerms;
            }

            matchStarts[numMatches] = startOffset;
            matchEnds[numMatches] = endOffset;
            matchTerms[numMatches] = term;
            numMatches++;
        }

        internal void Sort()
        {
            int[] starts = matchStarts;
            int[] ends = matchEnds;
            BytesRef[] terms = matchTerms;
            new AnonymousSorterTemplate(this, starts, ends, terms).MergeSort(0, numMatches - 1);
        }

        private sealed class AnonymousSorterTemplate : SorterTemplate
        {
            public AnonymousSorterTemplate(Passage parent, int[] starts, int[] ends, BytesRef[] terms)
            {
                this.parent = parent;
                this.starts = starts;
                this.ends = ends;
                this.terms = terms;
            }

            private readonly Passage parent;
            private readonly int[] starts;
            private readonly int[] ends;
            private readonly BytesRef[] terms;

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
                return (((long)starts[i]) - starts[j]).Signum();
            }

            protected override void SetPivot(int i)
            {
                pivot = starts[i];
            }

            protected override int ComparePivot(int j)
            {
                return (((long)pivot) - starts[j]).Signum();
            }

            int pivot;
        }

        internal void Reset()
        {
            startOffset = endOffset = -1;
            score = 0F;
            numMatches = 0;
        }

        public int StartOffset
        {
            get
            {
                return startOffset;
            }
        }

        public int EndOffset
        {
            get
            {
                return endOffset;
            }
        }

        public float Score
        {
            get
            {
                return score;
            }
        }

        public int NumMatches
        {
            get
            {
                return numMatches;
            }
        }

        public int[] MatchStarts
        {
            get
            {
                return matchStarts;
            }
        }

        public int[] MatchEnds
        {
            get
            {
                return matchEnds;
            }
        }

        public BytesRef[] MatchTerms
        {
            get
            {
                return matchTerms;
            }
        }
    }
}
