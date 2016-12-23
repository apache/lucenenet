using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Lucene.Net.Search
{
    using Lucene.Net.Index;

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
    using IndexReader = Lucene.Net.Index.IndexReader;
    using Term = Lucene.Net.Index.Term;
    using ToStringUtils = Lucene.Net.Util.ToStringUtils;

    /// <summary>
    /// A query that applies a filter to the results of another query.
    ///
    /// <p>Note: the bits are retrieved from the filter each time this
    /// query is used in a search - use a CachingWrapperFilter to avoid
    /// regenerating the bits every time.
    /// @since   1.4 </summary>
    /// <seealso cref=     CachingWrapperFilter </seealso>
    public class FilteredQuery : Query
    {
        private readonly Query Query_Renamed; // LUCENENET TODO: Rename (private)
        private readonly Filter Filter_Renamed; // LUCENENET TODO: Rename (private)
        private readonly FilterStrategy strategy;

        /// <summary>
        /// Constructs a new query which applies a filter to the results of the original query.
        /// <seealso cref="Filter#getDocIdSet"/> will be called every time this query is used in a search. </summary>
        /// <param name="query">  Query to be filtered, cannot be <code>null</code>. </param>
        /// <param name="filter"> Filter to apply to query results, cannot be <code>null</code>. </param>
        public FilteredQuery(Query query, Filter filter)
            : this(query, filter, RANDOM_ACCESS_FILTER_STRATEGY)
        {
        }

        /// <summary>
        /// Expert: Constructs a new query which applies a filter to the results of the original query.
        /// <seealso cref="Filter#getDocIdSet"/> will be called every time this query is used in a search. </summary>
        /// <param name="query">  Query to be filtered, cannot be <code>null</code>. </param>
        /// <param name="filter"> Filter to apply to query results, cannot be <code>null</code>. </param>
        /// <param name="strategy"> a filter strategy used to create a filtered scorer.
        /// </param>
        /// <seealso cref= FilterStrategy </seealso>
        public FilteredQuery(Query query, Filter filter, FilterStrategy strategy)
        {
            if (query == null || filter == null)
            {
                throw new System.ArgumentException("Query and filter cannot be null.");
            }
            if (strategy == null)
            {
                throw new System.ArgumentException("FilterStrategy can not be null");
            }
            this.strategy = strategy;
            this.Query_Renamed = query;
            this.Filter_Renamed = filter;
        }

        /// <summary>
        /// Returns a Weight that applies the filter to the enclosed query's Weight.
        /// this is accomplished by overriding the Scorer returned by the Weight.
        /// </summary>
        public override Weight CreateWeight(IndexSearcher searcher)
        {
            Weight weight = Query_Renamed.CreateWeight(searcher);
            return new WeightAnonymousInnerClassHelper(this, weight);
        }

        private class WeightAnonymousInnerClassHelper : Weight
        {
            private readonly FilteredQuery OuterInstance;

            private Lucene.Net.Search.Weight Weight;

            public WeightAnonymousInnerClassHelper(FilteredQuery outerInstance, Lucene.Net.Search.Weight weight)
            {
                this.OuterInstance = outerInstance;
                this.Weight = weight;
            }

            public override bool ScoresDocsOutOfOrder()
            {
                return true;
            }

            public override float ValueForNormalization
            {
                get
                {
                    return Weight.ValueForNormalization * OuterInstance.Boost * OuterInstance.Boost; // boost sub-weight
                }
            }

            public override void Normalize(float norm, float topLevelBoost)
            {
                Weight.Normalize(norm, topLevelBoost * OuterInstance.Boost); // incorporate boost
            }

            public override Explanation Explain(AtomicReaderContext ir, int i)
            {
                Explanation inner = Weight.Explain(ir, i);
                Filter f = OuterInstance.Filter_Renamed;
                DocIdSet docIdSet = f.GetDocIdSet(ir, ir.AtomicReader.LiveDocs);
                DocIdSetIterator docIdSetIterator = docIdSet == null ? DocIdSetIterator.Empty() : docIdSet.GetIterator();
                if (docIdSetIterator == null)
                {
                    docIdSetIterator = DocIdSetIterator.Empty();
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
            public override Query Query
            {
                get
                {
                    return OuterInstance;
                }
            }

            // return a filtering scorer
            public override Scorer Scorer(AtomicReaderContext context, Bits acceptDocs)
            {
                Debug.Assert(OuterInstance.Filter_Renamed != null);

                DocIdSet filterDocIdSet = OuterInstance.Filter_Renamed.GetDocIdSet(context, acceptDocs);
                if (filterDocIdSet == null)
                {
                    // this means the filter does not accept any documents.
                    return null;
                }

                return OuterInstance.strategy.FilteredScorer(context, Weight, filterDocIdSet);
            }

            // return a filtering top scorer
            public override BulkScorer BulkScorer(AtomicReaderContext context, bool scoreDocsInOrder, Bits acceptDocs)
            {
                Debug.Assert(OuterInstance.Filter_Renamed != null);

                DocIdSet filterDocIdSet = OuterInstance.Filter_Renamed.GetDocIdSet(context, acceptDocs);
                if (filterDocIdSet == null)
                {
                    // this means the filter does not accept any documents.
                    return null;
                }

                return OuterInstance.strategy.FilteredBulkScorer(context, Weight, scoreDocsInOrder, filterDocIdSet);
            }
        }

        /// <summary>
        /// A scorer that consults the filter iff a document was matched by the
        /// delegate scorer. this is useful if the filter computation is more expensive
        /// than document scoring or if the filter has a linear running time to compute
        /// the next matching doc like exact geo distances.
        /// </summary>
        private sealed class QueryFirstScorer : Scorer
        {
            private readonly Scorer Scorer; // LUCENENET TODO: Rename (private)
            private int ScorerDoc = -1; // LUCENENET TODO: Rename (private)
            private readonly Bits FilterBits; // LUCENENET TODO: Rename (private)

            internal QueryFirstScorer(Weight weight, Bits filterBits, Scorer other)
                : base(weight)
            {
                this.Scorer = other;
                this.FilterBits = filterBits;
            }

            public override int NextDoc()
            {
                int doc;
                for (; ; )
                {
                    doc = Scorer.NextDoc();
                    if (doc == Scorer.NO_MORE_DOCS || FilterBits.Get(doc))
                    {
                        return ScorerDoc = doc;
                    }
                }
            }

            public override int Advance(int target)
            {
                int doc = Scorer.Advance(target);
                if (doc != Scorer.NO_MORE_DOCS && !FilterBits.Get(doc))
                {
                    return ScorerDoc = NextDoc();
                }
                else
                {
                    return ScorerDoc = doc;
                }
            }

            public override int DocID()
            {
                return ScorerDoc;
            }

            public override float Score()
            {
                return Scorer.Score();
            }

            public override int Freq
            {
                get { return Scorer.Freq; }
            }

            public override ICollection<ChildScorer> Children
            {
                get
                {
                    return new[] { new ChildScorer(Scorer, "FILTERED") };
                }
            }

            public override long Cost()
            {
                return Scorer.Cost();
            }
        }

        private class QueryFirstBulkScorer : BulkScorer
        {
            private readonly Scorer Scorer; // LUCENENET TODO: Rename (private)
            private readonly Bits FilterBits; // LUCENENET TODO: Rename (private)

            public QueryFirstBulkScorer(Scorer scorer, Bits filterBits)
            {
                this.Scorer = scorer;
                this.FilterBits = filterBits;
            }

            public override bool Score(Collector collector, int maxDoc)
            {
                // the normalization trick already applies the boost of this query,
                // so we can use the wrapped scorer directly:
                collector.Scorer = Scorer;
                if (Scorer.DocID() == -1)
                {
                    Scorer.NextDoc();
                }
                while (true)
                {
                    int scorerDoc = Scorer.DocID();
                    if (scorerDoc < maxDoc)
                    {
                        if (FilterBits.Get(scorerDoc))
                        {
                            collector.Collect(scorerDoc);
                        }
                        Scorer.NextDoc();
                    }
                    else
                    {
                        break;
                    }
                }

                return Scorer.DocID() != Scorer.NO_MORE_DOCS;
            }
        }

        /// <summary>
        /// A Scorer that uses a "leap-frog" approach (also called "zig-zag join"). The scorer and the filter
        /// take turns trying to advance to each other's next matching document, often
        /// jumping past the target document. When both land on the same document, it's
        /// collected.
        /// </summary>
        private class LeapFrogScorer : Scorer
        {
            private readonly DocIdSetIterator Secondary; // LUCENENET TODO: Rename (private)
            private readonly DocIdSetIterator Primary; // LUCENENET TODO: Rename (private)
            private readonly Scorer Scorer; // LUCENENET TODO: Rename (private)
            protected int PrimaryDoc = -1;  // LUCENENET TODO: Rename (private)
            protected int SecondaryDoc = -1; // LUCENENET TODO: Rename (private)

            protected internal LeapFrogScorer(Weight weight, DocIdSetIterator primary, DocIdSetIterator secondary, Scorer scorer)
                : base(weight)
            {
                this.Primary = primary;
                this.Secondary = secondary;
                this.Scorer = scorer;
            }

            private int AdvanceToNextCommonDoc()
            {
                for (; ; )
                {
                    if (SecondaryDoc < PrimaryDoc)
                    {
                        SecondaryDoc = Secondary.Advance(PrimaryDoc);
                    }
                    else if (SecondaryDoc == PrimaryDoc)
                    {
                        return PrimaryDoc;
                    }
                    else
                    {
                        PrimaryDoc = Primary.Advance(SecondaryDoc);
                    }
                }
            }

            public override sealed int NextDoc()
            {
                PrimaryDoc = PrimaryNext();
                return AdvanceToNextCommonDoc();
            }

            protected virtual int PrimaryNext()
            {
                return Primary.NextDoc();
            }

            public override sealed int Advance(int target)
            {
                if (target > PrimaryDoc)
                {
                    PrimaryDoc = Primary.Advance(target);
                }
                return AdvanceToNextCommonDoc();
            }

            public override sealed int DocID()
            {
                return SecondaryDoc;
            }

            public override sealed float Score()
            {
                return Scorer.Score();
            }

            public override sealed int Freq
            {
                get { return Scorer.Freq; }
            }

            public override sealed ICollection<ChildScorer> Children
            {
                get
                {
                    return new[] { new ChildScorer(Scorer, "FILTERED") };
                }
            }

            public override long Cost()
            {
                return Math.Min(Primary.Cost(), Secondary.Cost());
            }
        }

        // TODO once we have way to figure out if we use RA or LeapFrog we can remove this scorer
        private sealed class PrimaryAdvancedLeapFrogScorer : LeapFrogScorer
        {
            private readonly int FirstFilteredDoc; // LUCENENET TODO: Rename (private)

            internal PrimaryAdvancedLeapFrogScorer(Weight weight, int firstFilteredDoc, DocIdSetIterator filterIter, Scorer other)
                : base(weight, filterIter, other, other)
            {
                this.FirstFilteredDoc = firstFilteredDoc;
                this.PrimaryDoc = firstFilteredDoc; // initialize to prevent and advance call to move it further
            }

            protected override int PrimaryNext()
            {
                if (SecondaryDoc != -1)
                {
                    return base.PrimaryNext();
                }
                else
                {
                    return FirstFilteredDoc;
                }
            }
        }

        /// <summary>
        /// Rewrites the query. If the wrapped is an instance of
        /// <seealso cref="MatchAllDocsQuery"/> it returns a <seealso cref="ConstantScoreQuery"/>. Otherwise
        /// it returns a new {@code FilteredQuery} wrapping the rewritten query.
        /// </summary>
        public override Query Rewrite(IndexReader reader)
        {
            Query queryRewritten = Query_Renamed.Rewrite(reader);

            if (queryRewritten != Query_Renamed)
            {
                // rewrite to a new FilteredQuery wrapping the rewritten query
                Query rewritten = new FilteredQuery(queryRewritten, Filter_Renamed, strategy);
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
        /// Returns this FilteredQuery's (unfiltered) Query </summary>
        public Query Query
        {
            get
            {
                return Query_Renamed;
            }
        }

        /// <summary>
        /// Returns this FilteredQuery's filter </summary>
        public Filter Filter
        {
            get
            {
                return Filter_Renamed;
            }
        }

        /// <summary>
        /// Returns this FilteredQuery's <seealso cref="FilterStrategy"/> </summary>
        public virtual FilterStrategy Strategy
        {
            get
            {
                return this.strategy;
            }
        }

        // inherit javadoc
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
            buffer.Append(Query_Renamed.ToString(s));
            buffer.Append(")->");
            buffer.Append(Filter_Renamed);
            buffer.Append(ToStringUtils.Boost(Boost));
            return buffer.ToString();
        }

        /// <summary>
        /// Returns true iff <code>o</code> is equal to this. </summary>
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
            Debug.Assert(o is FilteredQuery);
            FilteredQuery fq = (FilteredQuery)o;
            return fq.Query_Renamed.Equals(this.Query_Renamed) && fq.Filter_Renamed.Equals(this.Filter_Renamed) && fq.strategy.Equals(this.strategy);
        }

        /// <summary>
        /// Returns a hash code value for this object. </summary>
        public override int GetHashCode()
        {
            int hash = base.GetHashCode();
            hash = hash * 31 + strategy.GetHashCode();
            hash = hash * 31 + Query_Renamed.GetHashCode();
            hash = hash * 31 + Filter_Renamed.GetHashCode();
            return hash;
        }

        /// <summary>
        /// A <seealso cref="FilterStrategy"/> that conditionally uses a random access filter if
        /// the given <seealso cref="DocIdSet"/> supports random access (returns a non-null value
        /// from <seealso cref="DocIdSet#bits()"/>) and
        /// <seealso cref="RandomAccessFilterStrategy#useRandomAccess(Bits, int)"/> returns
        /// <code>true</code>. Otherwise this strategy falls back to a "zig-zag join" (
        /// <seealso cref="FilteredQuery#LEAP_FROG_FILTER_FIRST_STRATEGY"/>) strategy.
        ///
        /// <p>
        /// Note: this strategy is the default strategy in <seealso cref="FilteredQuery"/>
        /// </p>
        /// </summary>
        public static readonly FilterStrategy RANDOM_ACCESS_FILTER_STRATEGY = new RandomAccessFilterStrategy();

        /// <summary>
        /// A filter strategy that uses a "leap-frog" approach (also called "zig-zag join").
        /// The scorer and the filter
        /// take turns trying to advance to each other's next matching document, often
        /// jumping past the target document. When both land on the same document, it's
        /// collected.
        /// <p>
        /// Note: this strategy uses the filter to lead the iteration.
        /// </p>
        /// </summary>
        public static readonly FilterStrategy LEAP_FROG_FILTER_FIRST_STRATEGY = new LeapFrogFilterStrategy(false);

        /// <summary>
        /// A filter strategy that uses a "leap-frog" approach (also called "zig-zag join").
        /// The scorer and the filter
        /// take turns trying to advance to each other's next matching document, often
        /// jumping past the target document. When both land on the same document, it's
        /// collected.
        /// <p>
        /// Note: this strategy uses the query to lead the iteration.
        /// </p>
        /// </summary>
        public static readonly FilterStrategy LEAP_FROG_QUERY_FIRST_STRATEGY = new LeapFrogFilterStrategy(true);

        /// <summary>
        /// A filter strategy that advances the Query or rather its <seealso cref="Scorer"/> first and consults the
        /// filter <seealso cref="DocIdSet"/> for each matched document.
        /// <p>
        /// Note: this strategy requires a <seealso cref="DocIdSet#bits()"/> to return a non-null value. Otherwise
        /// this strategy falls back to <seealso cref="FilteredQuery#LEAP_FROG_QUERY_FIRST_STRATEGY"/>
        /// </p>
        /// <p>
        /// Use this strategy if the filter computation is more expensive than document
        /// scoring or if the filter has a linear running time to compute the next
        /// matching doc like exact geo distances.
        /// </p>
        /// </summary>
        public static readonly FilterStrategy QUERY_FIRST_FILTER_STRATEGY = new QueryFirstFilterStrategy();

        /// <summary>
        /// Abstract class that defines how the filter (<seealso cref="DocIdSet"/>) applied during document collection. </summary>
        public abstract class FilterStrategy
        {
            /// <summary>
            /// Returns a filtered <seealso cref="Scorer"/> based on this strategy.
            /// </summary>
            /// <param name="context">
            ///          the <seealso cref="AtomicReaderContext"/> for which to return the <seealso cref="Scorer"/>. </param>
            /// <param name="weight"> the <seealso cref="FilteredQuery"/> <seealso cref="Weight"/> to create the filtered scorer. </param>
            /// <param name="docIdSet"> the filter <seealso cref="DocIdSet"/> to apply </param>
            /// <returns> a filtered scorer
            /// </returns>
            /// <exception cref="IOException"> if an <seealso cref="IOException"/> occurs </exception>
            public abstract Scorer FilteredScorer(AtomicReaderContext context, Weight weight, DocIdSet docIdSet);

            /// <summary>
            /// Returns a filtered <seealso cref="BulkScorer"/> based on this
            /// strategy.  this is an optional method: the default
            /// implementation just calls <seealso cref="#filteredScorer"/> and
            /// wraps that into a BulkScorer.
            /// </summary>
            /// <param name="context">
            ///          the <seealso cref="AtomicReaderContext"/> for which to return the <seealso cref="Scorer"/>. </param>
            /// <param name="weight"> the <seealso cref="FilteredQuery"/> <seealso cref="Weight"/> to create the filtered scorer. </param>
            /// <param name="docIdSet"> the filter <seealso cref="DocIdSet"/> to apply </param>
            /// <returns> a filtered top scorer </returns>
            public virtual BulkScorer FilteredBulkScorer(AtomicReaderContext context, Weight weight, bool scoreDocsInOrder, DocIdSet docIdSet)
            {
                Scorer scorer = FilteredScorer(context, weight, docIdSet);
                if (scorer == null)
                {
                    return null;
                }
                // this impl always scores docs in order, so we can
                // ignore scoreDocsInOrder:
                return new Weight.DefaultBulkScorer(scorer);
            }
        }

        /// <summary>
        /// A <seealso cref="FilterStrategy"/> that conditionally uses a random access filter if
        /// the given <seealso cref="DocIdSet"/> supports random access (returns a non-null value
        /// from <seealso cref="DocIdSet#bits()"/>) and
        /// <seealso cref="RandomAccessFilterStrategy#useRandomAccess(Bits, int)"/> returns
        /// <code>true</code>. Otherwise this strategy falls back to a "zig-zag join" (
        /// <seealso cref="FilteredQuery#LEAP_FROG_FILTER_FIRST_STRATEGY"/>) strategy .
        /// </summary>
        public class RandomAccessFilterStrategy : FilterStrategy
        {
            public override Scorer FilteredScorer(AtomicReaderContext context, Weight weight, DocIdSet docIdSet)
            {
                DocIdSetIterator filterIter = docIdSet.GetIterator();
                if (filterIter == null)
                {
                    // this means the filter does not accept any documents.
                    return null;
                }

                int firstFilterDoc = filterIter.NextDoc();
                if (firstFilterDoc == DocIdSetIterator.NO_MORE_DOCS)
                {
                    return null;
                }

                Bits filterAcceptDocs = docIdSet.GetBits();
                // force if RA is requested
                bool useRandomAccess = filterAcceptDocs != null && UseRandomAccess(filterAcceptDocs, firstFilterDoc);
                if (useRandomAccess)
                {
                    // if we are using random access, we return the inner scorer, just with other acceptDocs
                    return weight.Scorer(context, filterAcceptDocs);
                }
                else
                {
                    Debug.Assert(firstFilterDoc > -1);
                    // we are gonna advance() this scorer, so we set inorder=true/toplevel=false
                    // we pass null as acceptDocs, as our filter has already respected acceptDocs, no need to do twice
                    Scorer scorer = weight.Scorer(context, null);
                    // TODO once we have way to figure out if we use RA or LeapFrog we can remove this scorer
                    return (scorer == null) ? null : new PrimaryAdvancedLeapFrogScorer(weight, firstFilterDoc, filterIter, scorer);
                }
            }

            /// <summary>
            /// Expert: decides if a filter should be executed as "random-access" or not.
            /// random-access means the filter "filters" in a similar way as deleted docs are filtered
            /// in Lucene. this is faster when the filter accepts many documents.
            /// However, when the filter is very sparse, it can be faster to execute the query+filter
            /// as a conjunction in some cases.
            ///
            /// The default implementation returns <code>true</code> if the first document accepted by the
            /// filter is < 100.
            ///
            /// @lucene.internal
            /// </summary>
            protected virtual bool UseRandomAccess(Bits bits, int firstFilterDoc)
            {
                //TODO once we have a cost API on filters and scorers we should rethink this heuristic
                return firstFilterDoc < 100;
            }
        }

        private sealed class LeapFrogFilterStrategy : FilterStrategy
        {
            private readonly bool ScorerFirst; // LUCENENET TODO: Rename (private)

            internal LeapFrogFilterStrategy(bool scorerFirst)
            {
                this.ScorerFirst = scorerFirst;
            }

            public override Scorer FilteredScorer(AtomicReaderContext context, Weight weight, DocIdSet docIdSet)
            {
                DocIdSetIterator filterIter = docIdSet.GetIterator();
                if (filterIter == null)
                {
                    // this means the filter does not accept any documents.
                    return null;
                }
                // we pass null as acceptDocs, as our filter has already respected acceptDocs, no need to do twice
                Scorer scorer = weight.Scorer(context, null);
                if (scorer == null)
                {
                    return null;
                }

                if (ScorerFirst)
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
        /// A filter strategy that advances the <seealso cref="Scorer"/> first and consults the
        /// <seealso cref="DocIdSet"/> for each matched document.
        /// <p>
        /// Note: this strategy requires a <seealso cref="DocIdSet#bits()"/> to return a non-null value. Otherwise
        /// this strategy falls back to <seealso cref="FilteredQuery#LEAP_FROG_QUERY_FIRST_STRATEGY"/>
        /// </p>
        /// <p>
        /// Use this strategy if the filter computation is more expensive than document
        /// scoring or if the filter has a linear running time to compute the next
        /// matching doc like exact geo distances.
        /// </p>
        /// </summary>
        private sealed class QueryFirstFilterStrategy : FilterStrategy
        {
            public override Scorer FilteredScorer(AtomicReaderContext context, Weight weight, DocIdSet docIdSet)
            {
                Bits filterAcceptDocs = docIdSet.GetBits();
                if (filterAcceptDocs == null)
                {
                    // Filter does not provide random-access Bits; we
                    // must fallback to leapfrog:
                    return LEAP_FROG_QUERY_FIRST_STRATEGY.FilteredScorer(context, weight, docIdSet);
                }
                Scorer scorer = weight.Scorer(context, null);
                return scorer == null ? null : new QueryFirstScorer(weight, filterAcceptDocs, scorer);
            }

            public override BulkScorer FilteredBulkScorer(AtomicReaderContext context, Weight weight, bool scoreDocsInOrder, DocIdSet docIdSet) // ignored (we always top-score in order)
            {
                Bits filterAcceptDocs = docIdSet.GetBits();
                if (filterAcceptDocs == null)
                {
                    // Filter does not provide random-access Bits; we
                    // must fallback to leapfrog:
                    return LEAP_FROG_QUERY_FIRST_STRATEGY.FilteredBulkScorer(context, weight, scoreDocsInOrder, docIdSet);
                }
                Scorer scorer = weight.Scorer(context, null);
                return scorer == null ? null : new QueryFirstBulkScorer(scorer, filterAcceptDocs);
            }
        }
    }
}