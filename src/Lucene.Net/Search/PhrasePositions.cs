using Lucene.Net.Index;

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

    /// <summary>
    /// Position of a term in a document that takes into account the term offset within the phrase.
    /// </summary>
    internal sealed class PhrasePositions
    {
        internal int doc; // current doc
        internal int position; // position in doc
        internal int count; // remaining pos in this doc
        internal int offset; // position in phrase
        internal readonly int ord; // unique across all PhrasePositions instances
        internal readonly DocsAndPositionsEnum postings; // stream of docs & positions
        internal PhrasePositions next; // used to make lists
        internal int rptGroup = -1; // >=0 indicates that this is a repeating PP
        internal int rptInd; // index in the rptGroup
        internal readonly Term[] terms; // for repetitions initialization

        internal PhrasePositions(DocsAndPositionsEnum postings, int o, int ord, Term[] terms)
        {
            this.postings = postings;
            offset = o;
            this.ord = ord;
            this.terms = terms;
        }

        internal bool Next() // increments to next doc
        {
            doc = postings.NextDoc();
            if (doc == DocIdSetIterator.NO_MORE_DOCS)
            {
                return false;
            }
            return true;
        }

        internal bool SkipTo(int target)
        {
            doc = postings.Advance(target);
            if (doc == DocIdSetIterator.NO_MORE_DOCS)
            {
                return false;
            }
            return true;
        }

        internal void FirstPosition()
        {
            count = postings.Freq; // read first pos
            NextPosition();
        }

        /// <summary>
        /// Go to next location of this term current document, and set
        /// <c>position</c> as <c>location - offset</c>, so that a
        /// matching exact phrase is easily identified when all <see cref="PhrasePositions"/>
        /// have exactly the same <c>position</c>.
        /// </summary>
        internal bool NextPosition()
        {
            if (count-- > 0) // read subsequent pos's
            {
                position = postings.NextPosition() - offset;
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// For debug purposes </summary>
        public override string ToString()
        {
            string s = "d:" + doc + " o:" + offset + " p:" + position + " c:" + count;
            if (rptGroup >= 0)
            {
                s += " rpt:" + rptGroup + ",i" + rptInd;
            }
            return s;
        }
    }
}