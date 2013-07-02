/* 
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 * 
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;

namespace Lucene.Net.Search
{

    /// <summary>A Scorer for OR like queries, counterpart of <c>ConjunctionScorer</c>.
    /// This Scorer implements <see cref="DocIdSetIterator.Advance(int)" /> and uses skipTo() on the given Scorers.
    /// </summary>
    class DisjunctionSumScorer : DisjunctionScorer
    {
        /** The document number of the current match. */
        private int doc = -1;

        /** The number of subscorers that provide the current match. */
        protected int nrMatchers = -1;

        protected double score = float.NaN;
        private readonly float[] coord;


        public DisjunctionSumScorer(Weight weight, Scorer[] subScorers, float[] coord)
            : base(weight, subScorers, subScorers.Length)
        {
            if (numScorers <= 1)
            {
                throw new ArgumentException("There must be at least 2 subScorers");
            }

            this.coord = coord;
        }

        public override int NextDoc()
        {
            //assert doc != NO_MORE_DOCS;
            while (true)
            {
                if (subScorers[0].NextDoc() != NO_MORE_DOCS)
                {
                    HeapAdjust(0);
                }
                else
                {
                    HeapRemoveRoot();
                    if (numScorers == 0)
                    {
                        return doc = NO_MORE_DOCS;
                    }
                }
                if (subScorers[0].DocID != doc)
                {
                    AfterNext();
                    return doc;
                }
            }
        }

        private void AfterNext()
        {
            Scorer sub = subScorers[0];
            doc = sub.DocID;
            if (doc != NO_MORE_DOCS)
            {
                score = sub.Score();
                nrMatchers = 1;
                CountMatches(1);
                CountMatches(2);
            }
        }

        // TODO: this currently scores, but so did the previous impl
        // TODO: remove recursion.
        // TODO: if we separate scoring, out of here, 
        // then change freq() to just always compute it from scratch
        private void CountMatches(int root)
        {
            if (root < numScorers && subScorers[root].DocID == doc)
            {
                nrMatchers++;
                score += subScorers[root].Score();
                CountMatches((root << 1) + 1);
                CountMatches((root << 1) + 2);
            }
        }

        /// <summary>Returns the score of the current document matching the query.
        /// Initially invalid, until <see cref="NextDoc()" /> is called the first time.
        /// </summary>
        public override float Score()
        {
            return (float)score * coord[nrMatchers];
        }

        public override int DocID
        {
            get { return doc; }
        }

        public override int Freq
        {
            get { return nrMatchers; }
        }

        /// <summary> Advances to the first match beyond the current whose document number is
        /// greater than or equal to a given target. <br/>
        /// The implementation uses the skipTo() method on the subscorers.
        /// 
        /// </summary>
        /// <param name="target">The target document number.
        /// </param>
        /// <returns> the document whose number is greater than or equal to the given
        /// target, or -1 if none exist.
        /// </returns>
        public override int Advance(int target)
        {
            //assert doc != NO_MORE_DOCS;
            while (true)
            {
                if (subScorers[0].Advance(target) != NO_MORE_DOCS)
                {
                    HeapAdjust(0);
                }
                else
                {
                    HeapRemoveRoot();
                    if (numScorers == 0)
                    {
                        return doc = NO_MORE_DOCS;
                    }
                }
                if (subScorers[0].DocID >= target)
                {
                    AfterNext();
                    return doc;
                }
            }
        }
    }
}