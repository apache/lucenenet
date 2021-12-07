using Lucene.Net.Diagnostics;

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

    using DocsEnum = Lucene.Net.Index.DocsEnum;
    using Similarity = Lucene.Net.Search.Similarities.Similarity;

    /// <summary>
    /// Expert: A <see cref="Scorer"/> for documents matching a <see cref="Index.Term"/>.
    /// </summary>
    internal sealed class TermScorer : Scorer
    {
        private readonly DocsEnum docsEnum;
        private readonly Similarity.SimScorer docScorer;

        /// <summary>
        /// Construct a <see cref="TermScorer"/>.
        /// </summary>
        /// <param name="weight">
        ///          The weight of the <see cref="Index.Term"/> in the query. </param>
        /// <param name="td">
        ///          An iterator over the documents matching the <see cref="Index.Term"/>. </param>
        /// <param name="docScorer">
        ///          The <see cref="Similarity.SimScorer"/> implementation
        ///          to be used for score computations. </param>
        internal TermScorer(Weight weight, DocsEnum td, Similarity.SimScorer docScorer)
            : base(weight)
        {
            this.docScorer = docScorer;
            this.docsEnum = td;
        }

        public override int DocID => docsEnum.DocID;

        public override int Freq => docsEnum.Freq;

        /// <summary>
        /// Advances to the next document matching the query.
        /// </summary>
        /// <returns> The document matching the query or <see cref="DocIdSetIterator.NO_MORE_DOCS"/> if there are no more documents. </returns>
        public override int NextDoc()
        {
            return docsEnum.NextDoc();
        }

        public override float GetScore()
        {
            if (Debugging.AssertsEnabled) Debugging.Assert(DocID != NO_MORE_DOCS);

            // LUCENENET specific: The explicit cast to float is required here to prevent us from losing precision on x86 .NET Framework with optimizations enabled
            return (float)docScorer.Score(docsEnum.DocID, docsEnum.Freq);
        }

        /// <summary>
        /// Advances to the first match beyond the current whose document number is
        /// greater than or equal to a given target.
        /// <para/>
        /// The implementation uses <see cref="DocIdSetIterator.Advance(int)"/>.
        /// </summary>
        /// <param name="target">
        ///          The target document number. </param>
        /// <returns> The matching document or <see cref="DocIdSetIterator.NO_MORE_DOCS"/> if none exist. </returns>
        public override int Advance(int target)
        {
            return docsEnum.Advance(target);
        }

        public override long GetCost()
        {
            return docsEnum.GetCost();
        }

        /// <summary>
        /// Returns a string representation of this <see cref="TermScorer"/>. </summary>
        public override string ToString()
        {
            return "scorer(" + m_weight + ")";
        }
    }
}