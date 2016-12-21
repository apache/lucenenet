namespace Lucene.Net.Search
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

    using Lucene.Net.Index;

    /// <summary>
    /// Position of a term in a document that takes into account the term offset within the phrase.
    /// </summary>
    internal sealed class PhrasePositions
    {
        internal int Doc; // current doc
        internal int Position; // position in doc
        internal int Count; // remaining pos in this doc
        internal int Offset; // position in phrase
        internal readonly int Ord; // unique across all PhrasePositions instances
        internal readonly DocsAndPositionsEnum Postings; // stream of docs & positions
        internal PhrasePositions next; // used to make lists
        internal int RptGroup = -1; // >=0 indicates that this is a repeating PP
        internal int RptInd; // index in the rptGroup
        internal readonly Term[] Terms; // for repetitions initialization

        internal PhrasePositions(DocsAndPositionsEnum postings, int o, int ord, Term[] terms)
        {
            this.Postings = postings;
            Offset = o;
            this.Ord = ord;
            this.Terms = terms;
        }

        internal bool Next() // increments to next doc
        {
            Doc = Postings.NextDoc();
            if (Doc == DocIdSetIterator.NO_MORE_DOCS)
            {
                return false;
            }
            return true;
        }

        internal bool SkipTo(int target)
        {
            Doc = Postings.Advance(target);
            if (Doc == DocIdSetIterator.NO_MORE_DOCS)
            {
                return false;
            }
            return true;
        }

        internal void FirstPosition()
        {
            Count = Postings.Freq; // read first pos
            NextPosition();
        }

        /// <summary>
        /// Go to next location of this term current document, and set
        /// <code>position</code> as <code>location - offset</code>, so that a
        /// matching exact phrase is easily identified when all PhrasePositions
        /// have exactly the same <code>position</code>.
        /// </summary>
        internal bool NextPosition()
        {
            if (Count-- > 0) // read subsequent pos's
            {
                Position = Postings.NextPosition() - Offset;
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// for debug purposes </summary>
        public override string ToString()
        {
            string s = "d:" + Doc + " o:" + Offset + " p:" + Position + " c:" + Count;
            if (RptGroup >= 0)
            {
                s += " rpt:" + RptGroup + ",i" + RptInd;
            }
            return s;
        }
    }
}