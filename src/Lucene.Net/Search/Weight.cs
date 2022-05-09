using System;
using System.IO;

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
    using IBits = Lucene.Net.Util.IBits;

    /// <summary>
    /// Expert: Calculate query weights and build query scorers.
    /// <para/>
    /// The purpose of <see cref="Weight"/> is to ensure searching does not modify a
    /// <see cref="Search.Query"/>, so that a <see cref="Search.Query"/> instance can be reused.
    /// <para/>
    /// <see cref="IndexSearcher"/> dependent state of the query should reside in the
    /// <see cref="Weight"/>.
    /// <para/>
    /// <see cref="Index.AtomicReader"/> dependent state should reside in the <see cref="Scorer"/>.
    /// <para/>
    /// Since <see cref="Weight"/> creates <see cref="Scorer"/> instances for a given
    /// <see cref="AtomicReaderContext"/> (<see cref="GetScorer(AtomicReaderContext, IBits)"/>)
    /// callers must maintain the relationship between the searcher's top-level
    /// <see cref="Index.IndexReaderContext"/> and the context used to create a <see cref="Scorer"/>.
    /// <para/>
    /// A <see cref="Weight"/> is used in the following way:
    /// <list type="number">
    ///     <item><description>A <see cref="Weight"/> is constructed by a top-level query, given a
    ///         <see cref="IndexSearcher"/> (<see cref="Query.CreateWeight(IndexSearcher)"/>).</description></item>
    ///     <item><description>The <see cref="GetValueForNormalization()"/> method is called on the
    ///         <see cref="Weight"/> to compute the query normalization factor
    ///         <see cref="Similarities.Similarity.QueryNorm(float)"/> of the query clauses contained in the
    ///         query.</description></item>
    ///     <item><description>The query normalization factor is passed to <see cref="Normalize(float, float)"/>. At
    ///         this point the weighting is complete.</description></item>
    ///     <item><description>A <see cref="Scorer"/> is constructed by
    ///         <see cref="GetScorer(AtomicReaderContext, IBits)"/>.</description></item>
    /// </list>
    /// <para/>
    /// @since 2.9
    /// </summary>
    public abstract class Weight
    {
        /// <summary>
        /// An explanation of the score computation for the named document.
        /// </summary>
        /// <param name="context"> The readers context to create the <see cref="Explanation"/> for. </param>
        /// <param name="doc"> The document's id relative to the given context's reader </param>
        /// <returns> An <see cref="Explanation"/> for the score </returns>
        /// <exception cref="IOException"> if an <see cref="IOException"/> occurs </exception>
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
        /// Returns a <see cref="Scorer"/> which scores documents in/out-of order according
        /// to <c>scoreDocsInOrder</c>.
        /// <para/>
        /// <b>NOTE:</b> even if <c>scoreDocsInOrder</c> is <c>false</c>, it is
        /// recommended to check whether the returned <see cref="Scorer"/> indeed scores
        /// documents out of order (i.e., call <see cref="ScoresDocsOutOfOrder"/>), as
        /// some <see cref="Scorer"/> implementations will always return documents
        /// in-order.
        /// <para/>
        /// <b>NOTE:</b> <c>null</c> can be returned if no documents will be scored by this
        /// query.
        /// </summary>
        /// <param name="context">
        ///          The <see cref="AtomicReaderContext"/> for which to return the <see cref="Scorer"/>. </param>
        /// <param name="acceptDocs">
        ///          <see cref="IBits"/> that represent the allowable docs to match (typically deleted docs
        ///          but possibly filtering other documents)
        /// </param>
        /// <returns> A <see cref="Scorer"/> which scores documents in/out-of order. </returns>
        /// <exception cref="IOException"> if there is a low-level I/O error </exception>
        public abstract Scorer GetScorer(AtomicReaderContext context, IBits acceptDocs);

        /// <summary>
        /// Optional method, to return a <see cref="BulkScorer"/> to
        /// score the query and send hits to a <see cref="ICollector"/>.
        /// Only queries that have a different top-level approach
        /// need to override this; the default implementation
        /// pulls a normal <see cref="Scorer"/> and iterates and
        /// collects the resulting hits.
        /// </summary>
        /// <param name="context">
        ///          The <see cref="AtomicReaderContext"/> for which to return the <see cref="Scorer"/>. </param>
        /// <param name="scoreDocsInOrder">
        ///          Specifies whether in-order scoring of documents is required. Note
        ///          that if set to <c>false</c> (i.e., out-of-order scoring is required),
        ///          this method can return whatever scoring mode it supports, as every
        ///          in-order scorer is also an out-of-order one. However, an
        ///          out-of-order scorer may not support <see cref="DocIdSetIterator.NextDoc()"/>
        ///          and/or <see cref="DocIdSetIterator.Advance(int)"/>, therefore it is recommended to
        ///          request an in-order scorer if use of these
        ///          methods is required. </param>
        /// <param name="acceptDocs">
        ///          <see cref="IBits"/> that represent the allowable docs to match (typically deleted docs
        ///          but possibly filtering other documents)
        /// </param>
        /// <returns> A <see cref="BulkScorer"/> which scores documents and
        /// passes them to a collector. </returns>
        /// <exception cref="IOException"> if there is a low-level I/O error </exception>
        public virtual BulkScorer GetBulkScorer(AtomicReaderContext context, bool scoreDocsInOrder, IBits acceptDocs)
        {
            Scorer scorer = GetScorer(context, acceptDocs);
            if (scorer is null)
            {
                // No docs match
                return null;
            }

            // this impl always scores docs in order, so we can
            // ignore scoreDocsInOrder:
            return new DefaultBulkScorer(scorer);
        }

        /// <summary>
        /// Just wraps a <see cref="Scorer"/> and performs top scoring using it. </summary>
        internal class DefaultBulkScorer : BulkScorer
        {
            internal readonly Scorer scorer;

            public DefaultBulkScorer(Scorer scorer)
            {
                // LUCENENET: Changed from NullPointerException to ArgumentNullException
                this.scorer = scorer ?? throw new ArgumentNullException(nameof(scorer));
            }

            public override bool Score(ICollector collector, int max)
            {
                // TODO: this may be sort of weird, when we are
                // embedded in a BooleanScorer, because we are
                // called for every chunk of 2048 documents.  But,
                // then, scorer is a FakeScorer in that case, so any
                // Collector doing something "interesting" in
                // setScorer will be forced to use BS2 anyways:
                collector.SetScorer(scorer);
                if (max == DocIdSetIterator.NO_MORE_DOCS)
                {
                    ScoreAll(collector, scorer);
                    return false;
                }
                else
                {
                    int doc = scorer.DocID;
                    if (doc < 0)
                    {
                        doc = scorer.NextDoc();
                    }
                    return ScoreRange(collector, scorer, doc, max);
                }
            }

            internal static bool ScoreRange(ICollector collector, Scorer scorer, int currentDoc, int end)
            {
                while (currentDoc < end)
                {
                    collector.Collect(currentDoc);
                    currentDoc = scorer.NextDoc();
                }
                return currentDoc != DocIdSetIterator.NO_MORE_DOCS;
            }

            internal static void ScoreAll(ICollector collector, Scorer scorer)
            {
                int doc;
                while ((doc = scorer.NextDoc()) != DocIdSetIterator.NO_MORE_DOCS)
                {
                    collector.Collect(doc);
                }
            }
        }

        /// <summary>
        /// Returns true if this implementation scores docs only out of order. This
        /// method is used in conjunction with <see cref="ICollector"/>'s
        /// <see cref="ICollector.AcceptsDocsOutOfOrder"/> and
        /// <see cref="GetBulkScorer(AtomicReaderContext, bool, IBits)"/> to
        /// create a matching <see cref="Scorer"/> instance for a given <see cref="ICollector"/>, or
        /// vice versa.
        /// <para/>
        /// <b>NOTE:</b> the default implementation returns <c>false</c>, i.e.
        /// the <see cref="Scorer"/> scores documents in-order.
        /// </summary>
        public virtual bool ScoresDocsOutOfOrder => false;
    }
}