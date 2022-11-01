using Lucene.Net.Diagnostics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

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
    using IndexReader = Lucene.Net.Index.IndexReader;
    using Term = Lucene.Net.Index.Term;
    using ToStringUtils = Lucene.Net.Util.ToStringUtils;

    /// <summary>
    /// A query that applies a filter to the results of another query.
    ///
    /// <para/>Note: the bits are retrieved from the filter each time this
    /// query is used in a search - use a <see cref="CachingWrapperFilter"/> to avoid
    /// regenerating the bits every time.
    /// <para/>
    /// @since   1.4 </summary>
    /// <seealso cref="CachingWrapperFilter"/>
    public class FilteredQuery : Query
    {
        private readonly Query query;
        private readonly Filter filter;
        private readonly FilterStrategy strategy;

        /// <summary>
        /// Constructs a new query which applies a filter to the results of the original query.
        /// <see cref="Filter.GetDocIdSet(AtomicReaderContext, IBits)"/> will be called every time this query is used in a search. </summary>
        /// <param name="query">  Query to be filtered, cannot be <c>null</c>. </param>
        /// <param name="filter"> Filter to apply to query results, cannot be <c>null</c>. </param>
        public FilteredQuery(Query query, Filter filter)
            : this(query, filter, RANDOM_ACCESS_FILTER_STRATEGY)
        {
        }

        /// <summary>
        /// Expert: Constructs a new query which applies a filter to the results of the original query.
        /// <see cref="Filter.GetDocIdSet(AtomicReaderContext, IBits)"/> will be called every time this query is used in a search. </summary>
        /// <param name="query">  Query to be filtered, cannot be <c>null</c>. </param>
        /// <param name="filter"> Filter to apply to query results, cannot be <c>null</c>. </param>
        /// <param name="strategy"> A filter strategy used to create a filtered scorer.
        /// </param>
        /// <seealso cref="FilterStrategy"/>
        public FilteredQuery(Query query, Filter filter, FilterStrategy strategy)
        {
            // LUCENENET specific - rearranged order to take advantage of throw expressions and
            // changed from IllegalArgumentException to ArgumentNullException (.NET convention)
            this.query = query ?? throw new ArgumentNullException(nameof(query), "Query cannot be null.");
            this.filter = filter ?? throw new ArgumentNullException(nameof(filter), "filter can not be null");
            this.strategy = strategy ?? throw new ArgumentNullException(nameof(strategy), "FilterStrategy can not be null");
        }

        /// <summary>
        /// Returns a <see cref="Weight"/> that applies the filter to the enclosed query's <see cref="Weight"/>.
        /// this is accomplished by overriding the <see cref="Scorer"/> returned by the <see cref="Weight"/>.
        /// </summary>
        public override Weight CreateWeight(IndexSearcher searcher)
        {
            Weight weight = query.CreateWeight(searcher);
            return new WeightAnonymousClass(this, weight);
        }

        private sealed class WeightAnonymousClass : Weight
        {
            private readonly FilteredQuery outerInstance;

            private readonly Weight weight;

            public WeightAnonymousClass(FilteredQuery outerInstance, Weight weight)
            {
                this.outerInstance = outerInstance;
                this.weight = weight;
            }

            public override bool ScoresDocsOutOfOrder => true;

            public override float GetValueForNormalization()
            {
                return weight.GetValueForNormalization() * outerInstance.Boost * outerInstance.Boost; // boost sub-weight
            }

            public override void Normalize(float norm, float topLevelBoost)
            {
                weight.Normalize(norm, topLevelBoost * outerInstance.Boost); // incorporate boost
            }

            public override Explanation Explain(AtomicReaderContext ir, int i)
            {
                Explanation inner = weight.Explain(ir, i);
                Filter f = outerInstance.filter;
                DocIdSet docIdSet = f.GetDocIdSet(ir, ir.AtomicReader.LiveDocs);
                DocIdSetIterator docIdSetIterator = docIdSet is null ? DocIdSetIterator.GetEmpty() : docIdSet.GetIterator();
                if (docIdSetIterator is null)
                {
                    docIdSetIterator = DocIdSetIterator.GetEmpty();
                }
                if (docIdSetIterator.Advance(i) == i)
                {
                    return inner;
                }
                else
                {
                    Explanation result = new Explanation(0.0f, "failure to match filter: " + f.ToString());
                    result.AddDetail(inner);
                    return result;
                }
            }

            // return this query
            public override Query Query => outerInstance;

            // return a filtering scorer
            public override Scorer GetScorer(AtomicReaderContext context, IBits acceptDocs)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(outerInstance.filter != null);

                DocIdSet filterDocIdSet = outerInstance.filter.GetDocIdSet(context, acceptDocs);
                if (filterDocIdSet is null)
                {
                    // this means the filter does not accept any documents.
                    return null;
                }

                return outerInstance.strategy.FilteredScorer(context, weight, filterDocIdSet);
            }

            // return a filtering top scorer
            public override BulkScorer GetBulkScorer(AtomicReaderContext context, bool scoreDocsInOrder, IBits acceptDocs)
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(outerInstance.filter != null);

                DocIdSet filterDocIdSet = outerInstance.filter.GetDocIdSet(context, acceptDocs);
                if (filterDocIdSet is null)
                {
                    // this means the filter does not accept any documents.
                    return null;
                }

                return outerInstance.strategy.FilteredBulkScorer(context, weight, scoreDocsInOrder, filterDocIdSet);
            }
        }

        /// <summary>
        /// A scorer that consults the filter if a document was matched by the
        /// delegate scorer. This is useful if the filter computation is more expensive
        /// than document scoring or if the filter has a linear running time to compute
        /// the next matching doc like exact geo distances.
        /// </summary>
        private sealed class QueryFirstScorer : Scorer
        {
            private readonly Scorer scorer;
            private int scorerDoc = -1;
            private readonly IBits filterBits;

            internal QueryFirstScorer(Weight weight, IBits filterBits, Scorer other)
                : base(weight)
            {
                this.scorer = other;
                this.filterBits = filterBits;
            }

            public override int NextDoc()
            {
                int doc;
                for (; ; )
                {
                    doc = scorer.NextDoc();
                    if (doc == Scorer.NO_MORE_DOCS || filterBits.Get(doc))
                    {
                        return scorerDoc = doc;
                    }
                }
            }

            public override int Advance(int target)
            {
                int doc = scorer.Advance(target);
                if (doc != Scorer.NO_MORE_DOCS && !filterBits.Get(doc))
                {
                    return scorerDoc = NextDoc();
                }
                else
                {
                    return scorerDoc = doc;
                }
            }

            public override int DocID => scorerDoc;

            public override float GetScore()
            {
                return scorer.GetScore();
            }

            public override int Freq => scorer.Freq;

            public override ICollection<ChildScorer> GetChildren()
            {
                return new[] { new ChildScorer(scorer, "FILTERED") };
            }

            public override long GetCost()
            {
                return scorer.GetCost();
            }
        }

        private class QueryFirstBulkScorer : BulkScorer
        {
            private readonly Scorer scorer;
            private readonly IBits filterBits;

            public QueryFirstBulkScorer(Scorer scorer, IBits filterBits)
            {
                this.scorer = scorer;
                this.filterBits = filterBits;
            }

            public override bool Score(ICollector collector, int maxDoc)
            {
                // the normalization trick already applies the boost of this query,
                // so we can use the wrapped scorer directly:
                collector.SetScorer(scorer);
                if (scorer.DocID == -1)
                {
                    scorer.NextDoc();
                }
                while (true)
                {
                    int scorerDoc = scorer.DocID;
                    if (scorerDoc < maxDoc)
                    {
                        if (filterBits.Get(scorerDoc))
                        {
                            collector.Collect(scorerDoc);
                        }
                        scorer.NextDoc();
                    }
                    else
                    {
                        break;
                    }
                }

                return scorer.DocID != Scorer.NO_MORE_DOCS;
            }
        }

        /// <summary>
        /// A <see cref="Scorer"/> that uses a "leap-frog" approach (also called "zig-zag join"). The scorer and the filter
        /// take turns trying to advance to each other's next matching document, often
        /// jumping past the target document. When both land on the same document, it's
        /// collected.
        /// </summary>
        private class LeapFrogScorer : Scorer
        {
            private readonly DocIdSetIterator secondary;
            private readonly DocIdSetIterator primary;
            private readonly Scorer scorer;
            protected int m_primaryDoc = -1;
            protected int m_secondaryDoc = -1;

            protected internal LeapFrogScorer(Weight weight, DocIdSetIterator primary, DocIdSetIterator secondary, Scorer scorer)
                : base(weight)
            {
                this.primary = primary;
                this.secondary = secondary;
                this.scorer = scorer;
            }

            private int AdvanceToNextCommonDoc()
            {
                for (; ; )
                {
                    if (m_secondaryDoc < m_primaryDoc)
                    {
                        m_secondaryDoc = secondary.Advance(m_primaryDoc);
                    }
                    else if (m_secondaryDoc == m_primaryDoc)
                    {
                        return m_primaryDoc;
                    }
                    else
                    {
                        m_primaryDoc = primary.Advance(m_secondaryDoc);
                    }
                }
            }

            public override sealed int NextDoc()
            {
                m_primaryDoc = PrimaryNext();
                return AdvanceToNextCommonDoc();
            }

            protected virtual int PrimaryNext()
            {
                return primary.NextDoc();
            }

            public override sealed int Advance(int target)
            {
                if (target > m_primaryDoc)
                {
                    m_primaryDoc = primary.Advance(target);
                }
                return AdvanceToNextCommonDoc();
            }

            public override sealed int DocID => m_secondaryDoc;

            public override sealed float GetScore()
            {
                return scorer.GetScore();
            }

            public override sealed int Freq => scorer.Freq;

            public override sealed ICollection<ChildScorer> GetChildren()
            {
                return new[] { new ChildScorer(scorer, "FILTERED") };
            }

            public override long GetCost()
            {
                return Math.Min(primary.GetCost(), secondary.GetCost());
            }
        }

        // TODO once we have way to figure out if we use RA or LeapFrog we can remove this scorer
        private sealed class PrimaryAdvancedLeapFrogScorer : LeapFrogScorer
        {
            private readonly int firstFilteredDoc;

            internal PrimaryAdvancedLeapFrogScorer(Weight weight, int firstFilteredDoc, DocIdSetIterator filterIter, Scorer other)
                : base(weight, filterIter, other, other)
            {
                this.firstFilteredDoc = firstFilteredDoc;
                this.m_primaryDoc = firstFilteredDoc; // initialize to prevent and advance call to move it further
            }

            protected override int PrimaryNext()
            {
                if (m_secondaryDoc != -1)
                {
                    return base.PrimaryNext();
                }
                else
                {
                    return firstFilteredDoc;
                }
            }
        }

        /// <summary>
        /// Rewrites the query. If the wrapped is an instance of
        /// <see cref="MatchAllDocsQuery"/> it returns a <see cref="ConstantScoreQuery"/>. Otherwise
        /// it returns a new <see cref="FilteredQuery"/> wrapping the rewritten query.
        /// </summary>
        public override Query Rewrite(IndexReader reader)
        {
            Query queryRewritten = query.Rewrite(reader);

            if (queryRewritten != query)
            {
                // rewrite to a new FilteredQuery wrapping the rewritten query
                Query rewritten = new FilteredQuery(queryRewritten, filter, strategy);
                rewritten.Boost = this.Boost;
                return rewritten;
            }
            else
            {
                // nothing to rewrite, we are done!
                return this;
            }
        }

        /// <summary>
        /// Returns this <see cref="FilteredQuery"/>'s (unfiltered) <see cref="Query"/> </summary>
        public Query Query => query;

        /// <summary>
        /// Returns this <see cref="FilteredQuery"/>'s filter </summary>
        public Filter Filter => filter;

        /// <summary>
        /// Returns this <see cref="FilteredQuery"/>'s <seealso cref="FilterStrategy"/> </summary>
        public virtual FilterStrategy Strategy => this.strategy;

        /// <summary>
        /// Expert: adds all terms occurring in this query to the terms set. Only
        /// works if this query is in its rewritten (<see cref="Rewrite(IndexReader)"/>) form.
        /// </summary>
        /// <exception cref="InvalidOperationException"> If this query is not yet rewritten </exception>
        public override void ExtractTerms(ISet<Term> terms)
        {
            Query.ExtractTerms(terms);
        }

        /// <summary>
        /// Prints a user-readable version of this query. </summary>
        public override string ToString(string s)
        {
            StringBuilder buffer = new StringBuilder();
            buffer.Append("filtered(");
            buffer.Append(query.ToString(s));
            buffer.Append(")->");
            buffer.Append(filter);
            buffer.Append(ToStringUtils.Boost(Boost));
            return buffer.ToString();
        }

        /// <summary>
        /// Returns true if <paramref name="o"/> is equal to this. </summary>
        public override bool Equals(object o)
        {
            if (o == this)
            {
                return true;
            }
            if (!base.Equals(o))
            {
                return false;
            }
            if (Debugging.AssertsEnabled) Debugging.Assert(o is FilteredQuery);
            FilteredQuery fq = (FilteredQuery)o;
            return fq.query.Equals(this.query) && fq.filter.Equals(this.filter) && fq.strategy.Equals(this.strategy);
        }

        /// <summary>
        /// Returns a hash code value for this object. </summary>
        public override int GetHashCode()
        {
            int hash = base.GetHashCode();
            hash = hash * 31 + strategy.GetHashCode();
            hash = hash * 31 + query.GetHashCode();
            hash = hash * 31 + filter.GetHashCode();
            return hash;
        }

        /// <summary>
        /// A <see cref="FilterStrategy"/> that conditionally uses a random access filter if
        /// the given <see cref="DocIdSet"/> supports random access (returns a non-null value
        /// from <see cref="DocIdSet.Bits"/>) and
        /// <see cref="RandomAccessFilterStrategy.UseRandomAccess(IBits, int)"/> returns
        /// <c>true</c>. Otherwise this strategy falls back to a "zig-zag join" (
        /// <see cref="FilteredQuery.LEAP_FROG_FILTER_FIRST_STRATEGY"/>) strategy.
        ///
        /// <para>
        /// Note: this strategy is the default strategy in <see cref="FilteredQuery"/>
        /// </para>
        /// </summary>
        public static readonly FilterStrategy RANDOM_ACCESS_FILTER_STRATEGY = new RandomAccessFilterStrategy();

        /// <summary>
        /// A filter strategy that uses a "leap-frog" approach (also called "zig-zag join").
        /// The scorer and the filter
        /// take turns trying to advance to each other's next matching document, often
        /// jumping past the target document. When both land on the same document, it's
        /// collected.
        /// <para>
        /// Note: this strategy uses the filter to lead the iteration.
        /// </para>
        /// </summary>
        public static readonly FilterStrategy LEAP_FROG_FILTER_FIRST_STRATEGY = new LeapFrogFilterStrategy(false);

        /// <summary>
        /// A filter strategy that uses a "leap-frog" approach (also called "zig-zag join").
        /// The scorer and the filter
        /// take turns trying to advance to each other's next matching document, often
        /// jumping past the target document. When both land on the same document, it's
        /// collected.
        /// <para>
        /// Note: this strategy uses the query to lead the iteration.
        /// </para>
        /// </summary>
        public static readonly FilterStrategy LEAP_FROG_QUERY_FIRST_STRATEGY = new LeapFrogFilterStrategy(true);

        /// <summary>
        /// A filter strategy that advances the <see cref="Search.Query"/> or rather its <see cref="Scorer"/> first and consults the
        /// filter <see cref="DocIdSet"/> for each matched document.
        /// <para>
        /// Note: this strategy requires a <see cref="DocIdSet.Bits"/> to return a non-null value. Otherwise
        /// this strategy falls back to <see cref="FilteredQuery.LEAP_FROG_QUERY_FIRST_STRATEGY"/>
        /// </para>
        /// <para>
        /// Use this strategy if the filter computation is more expensive than document
        /// scoring or if the filter has a linear running time to compute the next
        /// matching doc like exact geo distances.
        /// </para>
        /// </summary>
        public static readonly FilterStrategy QUERY_FIRST_FILTER_STRATEGY = new QueryFirstFilterStrategy();

        /// <summary>
        /// Abstract class that defines how the filter (<see cref="DocIdSet"/>) applied during document collection. </summary>
        public abstract class FilterStrategy
        {
            /// <summary>
            /// Returns a filtered <see cref="Scorer"/> based on this strategy.
            /// </summary>
            /// <param name="context">
            ///          the <see cref="AtomicReaderContext"/> for which to return the <see cref="Scorer"/>. </param>
            /// <param name="weight"> the <see cref="FilteredQuery"/> <see cref="Weight"/> to create the filtered scorer. </param>
            /// <param name="docIdSet"> the filter <see cref="DocIdSet"/> to apply </param>
            /// <returns> a filtered scorer
            /// </returns>
            /// <exception cref="IOException"> if an <see cref="IOException"/> occurs </exception>
            public abstract Scorer FilteredScorer(AtomicReaderContext context, Weight weight, DocIdSet docIdSet);

            /// <summary>
            /// Returns a filtered <see cref="BulkScorer"/> based on this
            /// strategy.  this is an optional method: the default
            /// implementation just calls <see cref="FilteredScorer(AtomicReaderContext, Weight, DocIdSet)"/> and
            /// wraps that into a <see cref="BulkScorer"/>.
            /// </summary>
            /// <param name="context">
            ///          the <seealso cref="AtomicReaderContext"/> for which to return the <seealso cref="Scorer"/>. </param>
            /// <param name="weight"> the <seealso cref="FilteredQuery"/> <seealso cref="Weight"/> to create the filtered scorer. </param>
            /// <param name="scoreDocsInOrder"> <c>true</c> to score docs in order </param>
            /// <param name="docIdSet"> the filter <seealso cref="DocIdSet"/> to apply </param>
            /// <returns> a filtered top scorer </returns>
            public virtual BulkScorer FilteredBulkScorer(AtomicReaderContext context, Weight weight, bool scoreDocsInOrder, DocIdSet docIdSet)
            {
                Scorer scorer = FilteredScorer(context, weight, docIdSet);
                if (scorer is null)
                {
                    return null;
                }
                // this impl always scores docs in order, so we can
                // ignore scoreDocsInOrder:
                return new Weight.DefaultBulkScorer(scorer);
            }
        }

        /// <summary>
        /// A <see cref="FilterStrategy"/> that conditionally uses a random access filter if
        /// the given <see cref="DocIdSet"/> supports random access (returns a non-null value
        /// from <see cref="DocIdSet.Bits"/>) and
        /// <see cref="RandomAccessFilterStrategy.UseRandomAccess(IBits, int)"/> returns
        /// <code>true</code>. Otherwise this strategy falls back to a "zig-zag join" (
        /// <see cref="FilteredQuery.LEAP_FROG_FILTER_FIRST_STRATEGY"/>) strategy .
        /// </summary>
        public class RandomAccessFilterStrategy : FilterStrategy
        {
            public override Scorer FilteredScorer(AtomicReaderContext context, Weight weight, DocIdSet docIdSet)
            {
                DocIdSetIterator filterIter = docIdSet.GetIterator();
                if (filterIter is null)
                {
                    // this means the filter does not accept any documents.
                    return null;
                }

                int firstFilterDoc = filterIter.NextDoc();
                if (firstFilterDoc == DocIdSetIterator.NO_MORE_DOCS)
                {
                    return null;
                }

                IBits filterAcceptDocs = docIdSet.Bits;
                // force if RA is requested
                bool useRandomAccess = filterAcceptDocs != null && UseRandomAccess(filterAcceptDocs, firstFilterDoc);
                if (useRandomAccess)
                {
                    // if we are using random access, we return the inner scorer, just with other acceptDocs
                    return weight.GetScorer(context, filterAcceptDocs);
                }
                else
                {
                    if (Debugging.AssertsEnabled) Debugging.Assert(firstFilterDoc > -1);
                    // we are gonna advance() this scorer, so we set inorder=true/toplevel=false
                    // we pass null as acceptDocs, as our filter has already respected acceptDocs, no need to do twice
                    Scorer scorer = weight.GetScorer(context, null);
                    // TODO once we have way to figure out if we use RA or LeapFrog we can remove this scorer
                    return (scorer is null) ? null : new PrimaryAdvancedLeapFrogScorer(weight, firstFilterDoc, filterIter, scorer);
                }
            }

            /// <summary>
            /// Expert: decides if a filter should be executed as "random-access" or not.
            /// Random-access means the filter "filters" in a similar way as deleted docs are filtered
            /// in Lucene. This is faster when the filter accepts many documents.
            /// However, when the filter is very sparse, it can be faster to execute the query+filter
            /// as a conjunction in some cases.
            /// <para/>
            /// The default implementation returns <c>true</c> if the first document accepted by the
            /// filter is &lt; 100.
            /// <para/>
            /// @lucene.internal
            /// </summary>
            protected virtual bool UseRandomAccess(IBits bits, int firstFilterDoc)
            {
                //TODO once we have a cost API on filters and scorers we should rethink this heuristic
                return firstFilterDoc < 100;
            }
        }

        private sealed class LeapFrogFilterStrategy : FilterStrategy
        {
            private readonly bool scorerFirst;

            internal LeapFrogFilterStrategy(bool scorerFirst)
            {
                this.scorerFirst = scorerFirst;
            }

            public override Scorer FilteredScorer(AtomicReaderContext context, Weight weight, DocIdSet docIdSet)
            {
                DocIdSetIterator filterIter = docIdSet.GetIterator();
                if (filterIter is null)
                {
                    // this means the filter does not accept any documents.
                    return null;
                }
                // we pass null as acceptDocs, as our filter has already respected acceptDocs, no need to do twice
                Scorer scorer = weight.GetScorer(context, null);
                if (scorer is null)
                {
                    return null;
                }

                if (scorerFirst)
                {
                    return new LeapFrogScorer(weight, scorer, filterIter, scorer);
                }
                else
                {
                    return new LeapFrogScorer(weight, filterIter, scorer, scorer);
                }
            }
        }

        /// <summary>
        /// A filter strategy that advances the <see cref="Scorer"/> first and consults the
        /// <see cref="DocIdSet"/> for each matched document.
        /// <para>
        /// Note: this strategy requires a <see cref="DocIdSet.Bits"/> to return a non-null value. Otherwise
        /// this strategy falls back to <see cref="FilteredQuery.LEAP_FROG_QUERY_FIRST_STRATEGY"/>
        /// </para>
        /// <para>
        /// Use this strategy if the filter computation is more expensive than document
        /// scoring or if the filter has a linear running time to compute the next
        /// matching doc like exact geo distances.
        /// </para>
        /// </summary>
        private sealed class QueryFirstFilterStrategy : FilterStrategy
        {
            public override Scorer FilteredScorer(AtomicReaderContext context, Weight weight, DocIdSet docIdSet)
            {
                IBits filterAcceptDocs = docIdSet.Bits;
                if (filterAcceptDocs is null)
                {
                    // Filter does not provide random-access Bits; we
                    // must fallback to leapfrog:
                    return LEAP_FROG_QUERY_FIRST_STRATEGY.FilteredScorer(context, weight, docIdSet);
                }
                Scorer scorer = weight.GetScorer(context, null);
                return scorer is null ? null : new QueryFirstScorer(weight, filterAcceptDocs, scorer);
            }

            public override BulkScorer FilteredBulkScorer(AtomicReaderContext context, Weight weight, bool scoreDocsInOrder, DocIdSet docIdSet) // ignored (we always top-score in order)
            {
                IBits filterAcceptDocs = docIdSet.Bits;
                if (filterAcceptDocs is null)
                {
                    // Filter does not provide random-access Bits; we
                    // must fallback to leapfrog:
                    return LEAP_FROG_QUERY_FIRST_STRATEGY.FilteredBulkScorer(context, weight, scoreDocsInOrder, docIdSet);
                }
                Scorer scorer = weight.GetScorer(context, null);
                return scorer is null ? null : new QueryFirstBulkScorer(scorer, filterAcceptDocs);
            }
        }
    }
}