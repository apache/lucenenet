using System;

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

    /// <summary>
    /// <para>Expert: Collectors are primarily meant to be used to
    /// gather raw results from a search, and implement sorting
    /// or custom result filtering, collation, etc. </para>
    ///
    /// <para>Lucene's core collectors are derived from Collector.
    /// Likely your application can use one of these classes, or
    /// subclass <see cref="TopDocsCollector{T}"/>, instead of
    /// implementing <see cref="ICollector"/> directly:
    ///
    /// <list type="bullet">
    ///
    ///   <item><description><see cref="TopDocsCollector{T}"/> is an abstract base class
    ///   that assumes you will retrieve the top N docs,
    ///   according to some criteria, after collection is
    ///   done.  </description></item>
    ///
    ///   <item><description><see cref="TopScoreDocCollector"/> is a concrete subclass
    ///   <see cref="TopDocsCollector{T}"/> and sorts according to score +
    ///   docID.  This is used internally by the 
    ///   <see cref="IndexSearcher"/> search methods that do not take an
    ///   explicit <see cref="Sort"/>. It is likely the most frequently
    ///   used collector.</description></item>
    ///
    ///   <item><description><see cref="TopFieldCollector"/> subclasses 
    ///   <see cref="TopDocsCollector{T}"/> and sorts according to a specified
    ///   <see cref="Sort"/> object (sort by field).  This is used
    ///   internally by the <see cref="IndexSearcher"/> search methods
    ///   that take an explicit <see cref="Sort"/>.</description></item>
    ///
    ///   <item><description><see cref="TimeLimitingCollector"/>, which wraps any other
    ///   Collector and aborts the search if it's taken too much
    ///   time.</description></item>
    ///
    ///   <item><description><see cref="PositiveScoresOnlyCollector"/> wraps any other
    ///   <see cref="ICollector"/> and prevents collection of hits whose score
    ///   is &lt;= 0.0</description></item>
    ///
    /// </list>
    /// </para>
    ///
    /// <para><see cref="ICollector"/> decouples the score from the collected doc:
    /// the score computation is skipped entirely if it's not
    /// needed.  Collectors that do need the score should
    /// implement the <see cref="SetScorer(Scorer)"/> method, to hold onto the
    /// passed <see cref="Scorer"/> instance, and call 
    /// <see cref="Scorer.GetScore()"/> within the collect method to compute the
    /// current hit's score.  If your collector may request the
    /// score for a single hit multiple times, you should use
    /// <see cref="ScoreCachingWrappingScorer"/>. </para>
    ///
    /// <para><b>NOTE:</b> The doc that is passed to the collect
    /// method is relative to the current reader. If your
    /// collector needs to resolve this to the docID space of the
    /// Multi*Reader, you must re-base it by recording the
    /// docBase from the most recent <see cref="SetNextReader(AtomicReaderContext)"/> call.  Here's
    /// a simple example showing how to collect docIDs into an
    /// <see cref="Util.OpenBitSet"/>:</para>
    ///
    /// <code>
    /// private class MySearchCollector : ICollector
    /// {
    ///     private readonly OpenBitSet bits;
    ///     private int docBase;
    /// 
    ///     public MySearchCollector(OpenBitSet bits)
    ///     {
    ///         if (bits is null) throw new ArgumentNullException("bits");
    ///         this.bits = bits;
    ///     }
    /// 
    ///     // ignore scorer
    ///     public void SetScorer(Scorer scorer)
    ///     { 
    ///     }
    ///     
    ///     // accept docs out of order (for a BitSet it doesn't matter)
    ///     public bool AcceptDocsOutOfOrder
    ///     {
    ///         get { return true; }
    ///     }
    ///     
    ///     public void Collect(int doc)
    ///     {
    ///         bits.Set(doc + docBase);
    ///     }
    ///     
    ///     public void SetNextReader(AtomicReaderContext context)
    ///     {
    ///         this.docBase = context.DocBase;
    ///     }
    /// }
    /// 
    /// IndexSearcher searcher = new IndexSearcher(indexReader);
    /// OpenBitSet bits = new OpenBitSet(indexReader.MaxDoc);
    /// searcher.Search(query, new MySearchCollector(bits));
    /// </code>
    ///
    /// <para>Not all collectors will need to rebase the docID.  For
    /// example, a collector that simply counts the total number
    /// of hits would skip it.</para>
    ///
    /// <para><b>NOTE:</b> Prior to 2.9, Lucene silently filtered
    /// out hits with score &lt;= 0.  As of 2.9, the core <see cref="ICollector"/>s
    /// no longer do that.  It's very unusual to have such hits
    /// (a negative query boost, or function query returning
    /// negative custom scores, could cause it to happen).  If
    /// you need that behavior, use 
    /// <see cref="PositiveScoresOnlyCollector"/>.</para>
    ///
    /// @lucene.experimental
    /// <para/>
    /// @since 2.9
    /// </summary>
    public interface ICollector // LUCENENET NOTE: This was an abstract class in Lucene, but made into an interface since we need one for Grouping's covariance
    {
        /// <summary>
        /// Called before successive calls to <see cref="Collect(int)"/>. Implementations
        /// that need the score of the current document (passed-in to
        /// <see cref="Collect(int)"/>), should save the passed-in <see cref="Scorer"/> and call
        /// <c>scorer.GetScore()</c> when needed.
        /// </summary>
        void SetScorer(Scorer scorer);

        /// <summary>
        /// Called once for every document matching a query, with the unbased document
        /// number.
        /// <para/>Note: The collection of the current segment can be terminated by throwing
        /// a <see cref="CollectionTerminatedException"/>. In this case, the last docs of the
        /// current <see cref="AtomicReaderContext"/> will be skipped and <see cref="IndexSearcher"/>
        /// will swallow the exception and continue collection with the next leaf.
        /// <para/>
        /// Note: this is called in an inner search loop. For good search performance,
        /// implementations of this method should not call <see cref="IndexSearcher.Doc(int)"/> or
        /// <see cref="Lucene.Net.Index.IndexReader.Document(int)"/> on every hit.
        /// Doing so can slow searches by an order of magnitude or more.
        /// </summary>
        void Collect(int doc);

        /// <summary>
        /// Called before collecting from each <see cref="AtomicReaderContext"/>. All doc ids in
        /// <see cref="Collect(int)"/> will correspond to <see cref="Index.IndexReaderContext.Reader"/>.
        /// <para/>
        /// Add <see cref="AtomicReaderContext.DocBase"/> to the current <see cref="Index.IndexReaderContext.Reader"/>'s
        /// internal document id to re-base ids in <see cref="Collect(int)"/>.
        /// </summary>
        /// <param name="context">next atomic reader context </param>
        void SetNextReader(AtomicReaderContext context);

        /// <summary>
        /// Return <c>true</c> if this collector does not
        /// require the matching docIDs to be delivered in int sort
        /// order (smallest to largest) to <see cref="Collect"/>.
        ///
        /// <para> Most Lucene Query implementations will visit
        /// matching docIDs in order.  However, some queries
        /// (currently limited to certain cases of <see cref="BooleanQuery"/>) 
        /// can achieve faster searching if the
        /// <see cref="ICollector"/> allows them to deliver the
        /// docIDs out of order.</para>
        ///
        /// <para> Many collectors don't mind getting docIDs out of
        /// order, so it's important to return <c>true</c>
        /// here.</para>
        /// </summary>
        bool AcceptsDocsOutOfOrder { get; }
    }

    /// <summary>
    /// LUCENENET specific class used to hold the 
    /// <see cref="NewAnonymous(Action{Scorer}, Action{int}, Action{AtomicReaderContext}, Func{bool})"/> static method.
    /// </summary>
    public static class Collector
    {
        /// <summary>
        /// Creates a new instance with the ability to specify the body of the <see cref="ICollector.SetScorer(Scorer)"/>
        /// method through the <paramref name="setScorer"/> parameter, the body of the <see cref="ICollector.Collect(int)"/>
        /// method through the <paramref name="collect"/> parameter, the body of the <see cref="ICollector.SetNextReader(AtomicReaderContext)"/>
        /// method through the <paramref name="setNextReader"/> parameter, and the body of the <see cref="ICollector.AcceptsDocsOutOfOrder"/>
        /// property through the <paramref name="acceptsDocsOutOfOrder"/> parameter.
        /// Simple example:
        /// <code>
        ///     IndexSearcher searcher = new IndexSearcher(indexReader);
        ///     OpenBitSet bits = new OpenBitSet(indexReader.MaxDoc);
        ///     int docBase;
        ///     searcher.Search(query, 
        ///         Collector.NewAnonymous(setScorer: (scorer) =>
        ///         {
        ///             // ignore scorer
        ///         }, collect: (doc) =>
        ///         {
        ///             bits.Set(doc + docBase);
        ///         }, setNextReader: (context) =>
        ///         {
        ///             docBase = context.DocBase;
        ///         }, acceptsDocsOutOfOrder: () =>
        ///         {
        ///             return true;
        ///         })
        ///     );
        /// </code>
        /// </summary>
        /// <param name="setScorer">
        /// A delegate method that represents (is called by) the <see cref="ICollector.SetScorer(Scorer)"/> 
        /// method. It accepts a <see cref="Scorer"/> scorer and 
        /// has no return value.
        /// </param>
        /// <param name="collect">
        /// A delegate method that represents (is called by) the <see cref="ICollector.Collect(int)"/> 
        /// method. It accepts an <see cref="int"/> doc and 
        /// has no return value.
        /// </param>
        /// <param name="setNextReader">
        /// A delegate method that represents (is called by) the <see cref="ICollector.SetNextReader(AtomicReaderContext)"/> 
        /// method. It accepts a <see cref="AtomicReaderContext"/> context and 
        /// has no return value.
        /// </param>
        /// <param name="acceptsDocsOutOfOrder">
        /// A delegate method that represents (is called by) the <see cref="ICollector.AcceptsDocsOutOfOrder"/> 
        /// property. It returns a <see cref="bool"/> value.
        /// </param>
        /// <returns> A new <see cref="AnonymousCollector"/> instance. </returns>
        public static ICollector NewAnonymous(Action<Scorer> setScorer, Action<int> collect, Action<AtomicReaderContext> setNextReader, Func<bool> acceptsDocsOutOfOrder)
        {
            return new AnonymousCollector(setScorer, collect, setNextReader, acceptsDocsOutOfOrder);
        }

        // LUCENENET specific
        private class AnonymousCollector : ICollector
        {
            private readonly Action<Scorer> setScorer;
            private readonly Action<int> collect;
            private readonly Action<AtomicReaderContext> setNextReader;
            private readonly Func<bool> acceptsDocsOutOfOrder;

            public AnonymousCollector(Action<Scorer> setScorer, Action<int> collect, Action<AtomicReaderContext> setNextReader, Func<bool> acceptsDocsOutOfOrder)
            {
                this.setScorer = setScorer ?? throw new ArgumentNullException(nameof(setScorer));
                this.collect = collect ?? throw new ArgumentNullException(nameof(collect));
                this.setNextReader = setNextReader ?? throw new ArgumentNullException(nameof(setNextReader));
                this.acceptsDocsOutOfOrder = acceptsDocsOutOfOrder ?? throw new ArgumentNullException(nameof(acceptsDocsOutOfOrder));
            }

            public bool AcceptsDocsOutOfOrder => this.acceptsDocsOutOfOrder();

            public void Collect(int doc)
            {
                this.collect(doc);
            }

            public void SetNextReader(AtomicReaderContext context)
            {
                this.setNextReader(context);
            }

            public void SetScorer(Scorer scorer)
            {
                this.setScorer(scorer);
            }
        }
    }
}