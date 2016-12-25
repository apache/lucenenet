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

    using AtomicReaderContext = Lucene.Net.Index.AtomicReaderContext;
    using Bits = Lucene.Net.Util.Bits;

    /// <summary>
    /// Expert: Calculate query weights and build query scorers.
    /// <p>
    /// The purpose of <seealso cref="Weight"/> is to ensure searching does not modify a
    /// <seealso cref="Query"/>, so that a <seealso cref="Query"/> instance can be reused. <br>
    /// <seealso cref="IndexSearcher"/> dependent state of the query should reside in the
    /// <seealso cref="Weight"/>. <br>
    /// <seealso cref="AtomicReader"/> dependent state should reside in the <seealso cref="Scorer"/>.
    /// <p>
    /// Since <seealso cref="Weight"/> creates <seealso cref="Scorer"/> instances for a given
    /// <seealso cref="AtomicReaderContext"/> (<seealso cref="#scorer(AtomicReaderContext, Bits)"/>)
    /// callers must maintain the relationship between the searcher's top-level
    /// <seealso cref="IndexReaderContext"/> and the context used to create a <seealso cref="Scorer"/>.
    /// <p>
    /// A <code>Weight</code> is used in the following way:
    /// <ol>
    /// <li>A <code>Weight</code> is constructed by a top-level query, given a
    /// <code>IndexSearcher</code> (<seealso cref="Query#createWeight(IndexSearcher)"/>).
    /// <li>The <seealso cref="#getValueForNormalization()"/> method is called on the
    /// <code>Weight</code> to compute the query normalization factor
    /// <seealso cref="Similarity#queryNorm(float)"/> of the query clauses contained in the
    /// query.
    /// <li>The query normalization factor is passed to <seealso cref="#normalize(float, float)"/>. At
    /// this point the weighting is complete.
    /// <li>A <code>Scorer</code> is constructed by
    /// <seealso cref="#scorer(AtomicReaderContext, Bits)"/>.
    /// </ol>
    ///
    /// @since 2.9
    /// </summary>
    public abstract class Weight
    {
        /// <summary>
        /// An explanation of the score computation for the named document.
        /// </summary>
        /// <param name="context"> the readers context to create the <seealso cref="Explanation"/> for. </param>
        /// <param name="doc"> the document's id relative to the given context's reader </param>
        /// <returns> an Explanation for the score </returns>
        /// <exception cref="IOException"> if an <seealso cref="IOException"/> occurs </exception>
        public abstract Explanation Explain(AtomicReaderContext context, int doc);

        /// <summary>
        /// The query that this concerns. </summary>
        public abstract Query Query { get; }

        /// <summary>
        /// The value for normalization of contained query clauses (e.g. sum of squared weights). </summary>
        public abstract float GetValueForNormalization();

        /// <summary>
        /// Assigns the query normalization factor and boost from parent queries to this. </summary>
        public abstract void Normalize(float norm, float topLevelBoost);

        /// <summary>
        /// Returns a <seealso cref="Scorer"/> which scores documents in/out-of order according
        /// to <code>scoreDocsInOrder</code>.
        /// <p>
        /// <b>NOTE:</b> even if <code>scoreDocsInOrder</code> is false, it is
        /// recommended to check whether the returned <code>Scorer</code> indeed scores
        /// documents out of order (i.e., call <seealso cref="#scoresDocsOutOfOrder()"/>), as
        /// some <code>Scorer</code> implementations will always return documents
        /// in-order.<br>
        /// <b>NOTE:</b> null can be returned if no documents will be scored by this
        /// query.
        /// </summary>
        /// <param name="context">
        ///          the <seealso cref="AtomicReaderContext"/> for which to return the <seealso cref="Scorer"/>. </param>
        /// <param name="acceptDocs">
        ///          Bits that represent the allowable docs to match (typically deleted docs
        ///          but possibly filtering other documents)
        /// </param>
        /// <returns> a <seealso cref="Scorer"/> which scores documents in/out-of order. </returns>
        /// <exception cref="IOException"> if there is a low-level I/O error </exception>
        public abstract Scorer Scorer(AtomicReaderContext context, Bits acceptDocs); // LUCENENET TODO: Rename GetScorer() ?

        /// <summary>
        /// Optional method, to return a <seealso cref="BulkScorer"/> to
        /// score the query and send hits to a <seealso cref="Collector"/>.
        /// Only queries that have a different top-level approach
        /// need to override this; the default implementation
        /// pulls a normal <seealso cref="Scorer"/> and iterates and
        /// collects the resulting hits.
        /// </summary>
        /// <param name="context">
        ///          the <seealso cref="AtomicReaderContext"/> for which to return the <seealso cref="Scorer"/>. </param>
        /// <param name="scoreDocsInOrder">
        ///          specifies whether in-order scoring of documents is required. Note
        ///          that if set to false (i.e., out-of-order scoring is required),
        ///          this method can return whatever scoring mode it supports, as every
        ///          in-order scorer is also an out-of-order one. However, an
        ///          out-of-order scorer may not support <seealso cref="Scorer#nextDoc()"/>
        ///          and/or <seealso cref="Scorer#advance(int)"/>, therefore it is recommended to
        ///          request an in-order scorer if use of these
        ///          methods is required. </param>
        /// <param name="acceptDocs">
        ///          Bits that represent the allowable docs to match (typically deleted docs
        ///          but possibly filtering other documents)
        /// </param>
        /// <returns> a <seealso cref="BulkScorer"/> which scores documents and
        /// passes them to a collector. </returns>
        /// <exception cref="IOException"> if there is a low-level I/O error </exception>
        public virtual BulkScorer BulkScorer(AtomicReaderContext context, bool scoreDocsInOrder, Bits acceptDocs) // LUCENENET TODO: Rename GetBulkScorer ?
        {
            Scorer scorer = Scorer(context, acceptDocs);
            if (scorer == null)
            {
                // No docs match
                return null;
            }

            // this impl always scores docs in order, so we can
            // ignore scoreDocsInOrder:
            return new DefaultBulkScorer(scorer);
        }

        /// <summary>
        /// Just wraps a Scorer and performs top scoring using it. </summary>
        internal class DefaultBulkScorer : BulkScorer
        {
            internal readonly Scorer Scorer; // LUCENENET TODO: Rename (private)

            public DefaultBulkScorer(Scorer scorer)
            {
                if (scorer == null)
                {
                    throw new System.NullReferenceException();
                }
                this.Scorer = scorer;
            }

            public override bool Score(Collector collector, int max)
            {
                // TODO: this may be sort of weird, when we are
                // embedded in a BooleanScorer, because we are
                // called for every chunk of 2048 documents.  But,
                // then, scorer is a FakeScorer in that case, so any
                // Collector doing something "interesting" in
                // setScorer will be forced to use BS2 anyways:
                collector.SetScorer(Scorer);
                if (max == DocIdSetIterator.NO_MORE_DOCS)
                {
                    ScoreAll(collector, Scorer);
                    return false;
                }
                else
                {
                    int doc = Scorer.DocID();
                    if (doc < 0)
                    {
                        doc = Scorer.NextDoc();
                    }
                    return ScoreRange(collector, Scorer, doc, max);
                }
            }

            internal static bool ScoreRange(Collector collector, Scorer scorer, int currentDoc, int end)
            {
                while (currentDoc < end)
                {
                    collector.Collect(currentDoc);
                    currentDoc = scorer.NextDoc();
                }
                return currentDoc != DocIdSetIterator.NO_MORE_DOCS;
            }

            internal static void ScoreAll(Collector collector, Scorer scorer)
            {
                int doc;
                while ((doc = scorer.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
                {
                    collector.Collect(doc);
                }
            }
        }

        /// <summary>
        /// Returns true iff this implementation scores docs only out of order. this
        /// method is used in conjunction with <seealso cref="Collector"/>'s
        /// <seealso cref="Collector#acceptsDocsOutOfOrder() acceptsDocsOutOfOrder"/> and
        /// <seealso cref="#bulkScorer(AtomicReaderContext, boolean, Bits)"/> to
        /// create a matching <seealso cref="Scorer"/> instance for a given <seealso cref="Collector"/>, or
        /// vice versa.
        /// <p>
        /// <b>NOTE:</b> the default implementation returns <code>false</code>, i.e.
        /// the <code>Scorer</code> scores documents in-order.
        /// </summary>
        public virtual bool ScoresDocsOutOfOrder
        {
            get { return false; }
        }
    }
}