using System.Collections.Generic;

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
    /// A <see cref="Scorer"/> for queries with a required subscorer
    /// and an excluding (prohibited) sub <see cref="DocIdSetIterator"/>.
    /// <para/>
    /// This <see cref="Scorer"/> implements <see cref="DocIdSetIterator.Advance(int)"/>,
    /// and it uses the SkipTo() on the given scorers.
    /// </summary>
    internal class ReqExclScorer : Scorer
    {
        private Scorer reqScorer;
        private DocIdSetIterator exclDisi;
        private int doc = -1;

        /// <summary>
        /// Construct a <see cref="ReqExclScorer"/>. </summary>
        /// <param name="reqScorer"> The scorer that must match, except where </param>
        /// <param name="exclDisi"> Indicates exclusion. </param>
        public ReqExclScorer(Scorer reqScorer, DocIdSetIterator exclDisi)
            : base(reqScorer.m_weight)
        {
            this.reqScorer = reqScorer;
            this.exclDisi = exclDisi;
        }

        public override int NextDoc()
        {
            if (reqScorer is null)
            {
                return doc;
            }
            doc = reqScorer.NextDoc();
            if (doc == NO_MORE_DOCS)
            {
                reqScorer = null; // exhausted, nothing left
                return doc;
            }
            if (exclDisi is null)
            {
                return doc;
            }
            return doc = ToNonExcluded();
        }

        /// <summary>
        /// Advance to non excluded doc.
        /// <para/>On entry:
        /// <list type="bullet">
        /// <item><description>reqScorer != null,</description></item>
        /// <item><description>exclScorer != null,</description></item>
        /// <item><description>reqScorer was advanced once via Next() or SkipTo()
        ///      and reqScorer.Doc may still be excluded.</description></item>
        /// </list>
        /// Advances reqScorer a non excluded required doc, if any. </summary>
        /// <returns> <c>true</c> if there is a non excluded required doc. </returns>
        private int ToNonExcluded()
        {
            int exclDoc = exclDisi.DocID;
            int reqDoc = reqScorer.DocID; // may be excluded
            do
            {
                if (reqDoc < exclDoc)
                {
                    return reqDoc; // reqScorer advanced to before exclScorer, ie. not excluded
                }
                else if (reqDoc > exclDoc)
                {
                    exclDoc = exclDisi.Advance(reqDoc);
                    if (exclDoc == NO_MORE_DOCS)
                    {
                        exclDisi = null; // exhausted, no more exclusions
                        return reqDoc;
                    }
                    if (exclDoc > reqDoc)
                    {
                        return reqDoc; // not excluded
                    }
                }
            } while ((reqDoc = reqScorer.NextDoc()) != NO_MORE_DOCS);
            reqScorer = null; // exhausted, nothing left
            return NO_MORE_DOCS;
        }

        public override int DocID => doc;

        /// <summary>
        /// Returns the score of the current document matching the query.
        /// Initially invalid, until <see cref="NextDoc()"/> is called the first time. </summary>
        /// <returns> The score of the required scorer. </returns>
        public override float GetScore()
        {
            return reqScorer.GetScore(); // reqScorer may be null when next() or skipTo() already return false
        }

        public override int Freq => reqScorer.Freq;

        public override ICollection<ChildScorer> GetChildren()
        {
            return new[] { new ChildScorer(reqScorer, "FILTERED") };
        }

        public override int Advance(int target)
        {
            if (reqScorer is null)
            {
                return doc = NO_MORE_DOCS;
            }
            if (exclDisi is null)
            {
                return doc = reqScorer.Advance(target);
            }
            if (reqScorer.Advance(target) == NO_MORE_DOCS)
            {
                reqScorer = null;
                return doc = NO_MORE_DOCS;
            }
            return doc = ToNonExcluded();
        }

        public override long GetCost()
        {
            return reqScorer.GetCost();
        }
    }
}