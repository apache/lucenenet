using Lucene.Net.Diagnostics;
using Lucene.Net.Documents;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
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

    using AttributeSource = Lucene.Net.Util.AttributeSource;
    using BytesRef = Lucene.Net.Util.BytesRef;
    using FilteredTermsEnum = Lucene.Net.Index.FilteredTermsEnum;
    using NumericUtils = Lucene.Net.Util.NumericUtils;
    using Terms = Lucene.Net.Index.Terms;
    using TermsEnum = Lucene.Net.Index.TermsEnum;
    using ToStringUtils = Lucene.Net.Util.ToStringUtils;

    /// <summary>
    /// <para>A <see cref="Query"/> that matches numeric values within a
    /// specified range.  To use this, you must first index the
    /// numeric values using <see cref="Int32Field"/>, 
    /// <see cref="SingleField"/>, <see cref="Int64Field"/> or <see cref="DoubleField"/> (expert: 
    /// <see cref="Analysis.NumericTokenStream"/>).  If your terms are instead textual,
    /// you should use <see cref="TermRangeQuery"/>.  
    /// <see cref="NumericRangeFilter"/> is the filter equivalent of this
    /// query.</para>
    ///
    /// <para>You create a new <see cref="NumericRangeQuery{T}"/> with the static
    /// factory methods, eg:
    ///
    /// <code>
    /// Query q = NumericRangeQuery.NewFloatRange("weight", 0.03f, 0.10f, true, true);
    /// </code>
    ///
    /// matches all documents whose <see cref="float"/> valued "weight" field
    /// ranges from 0.03 to 0.10, inclusive.</para>
    ///
    /// <para>The performance of <see cref="NumericRangeQuery{T}"/> is much better
    /// than the corresponding <see cref="TermRangeQuery"/> because the
    /// number of terms that must be searched is usually far
    /// fewer, thanks to trie indexing, described below.</para>
    ///
    /// <para>You can optionally specify a <a
    /// href="#precisionStepDesc"><see cref="precisionStep"/></a>
    /// when creating this query.  This is necessary if you've
    /// changed this configuration from its default (4) during
    /// indexing.  Lower values consume more disk space but speed
    /// up searching.  Suitable values are between <b>1</b> and
    /// <b>8</b>. A good starting point to test is <b>4</b>,
    /// which is the default value for all <c>Numeric*</c>
    /// classes.  See <a href="#precisionStepDesc">below</a> for
    /// details.</para>
    ///
    /// <para>This query defaults to 
    /// <see cref="MultiTermQuery.CONSTANT_SCORE_AUTO_REWRITE_DEFAULT"/>.
    /// With precision steps of &lt;=4, this query can be run with
    /// one of the <see cref="BooleanQuery"/> rewrite methods without changing
    /// <see cref="BooleanQuery"/>'s default max clause count.</para>
    ///
    /// <para/><h3>How it works</h3>
    ///
    /// <para>See the publication about <a target="_blank" href="http://www.panfmp.org">panFMP</a>,
    /// where this algorithm was described (referred to as <c>TrieRangeQuery</c>):
    /// </para>
    /// <blockquote><strong>Schindler, U, Diepenbroek, M</strong>, 2008.
    /// <em>Generic XML-based Framework for Metadata Portals.</em>
    /// Computers &amp; Geosciences 34 (12), 1947-1955.
    /// <a href="http://dx.doi.org/10.1016/j.cageo.2008.02.023"
    /// target="_blank">doi:10.1016/j.cageo.2008.02.023</a></blockquote>
    ///
    /// <para><em>A quote from this paper:</em> Because Apache Lucene is a full-text
    /// search engine and not a conventional database, it cannot handle numerical ranges
    /// (e.g., field value is inside user defined bounds, even dates are numerical values).
    /// We have developed an extension to Apache Lucene that stores
    /// the numerical values in a special string-encoded format with variable precision
    /// (all numerical values like <see cref="double"/>s, <see cref="long"/>s, <see cref="float"/>s, and <see cref="int"/>s are converted to
    /// lexicographic sortable string representations and stored with different precisions
    /// (for a more detailed description of how the values are stored,
    /// see <see cref="NumericUtils"/>). A range is then divided recursively into multiple intervals for searching:
    /// The center of the range is searched only with the lowest possible precision in the <em>trie</em>,
    /// while the boundaries are matched more exactly. This reduces the number of terms dramatically.</para>
    ///
    /// <para>For the variant that stores long values in 8 different precisions (each reduced by 8 bits) that
    /// uses a lowest precision of 1 byte, the index contains only a maximum of 256 distinct values in the
    /// lowest precision. Overall, a range could consist of a theoretical maximum of
    /// <code>7*255*2 + 255 = 3825</code> distinct terms (when there is a term for every distinct value of an
    /// 8-byte-number in the index and the range covers almost all of them; a maximum of 255 distinct values is used
    /// because it would always be possible to reduce the full 256 values to one term with degraded precision).
    /// In practice, we have seen up to 300 terms in most cases (index with 500,000 metadata records
    /// and a uniform value distribution).</para>
    ///
    /// <a name="precisionStepDesc"><h3>Precision Step</h3></a>
    /// <para/>You can choose any <see cref="precisionStep"/> when encoding values.
    /// Lower step values mean more precisions and so more terms in index (and index gets larger). The number
    /// of indexed terms per value is (those are generated by <see cref="Analysis.NumericTokenStream"/>):
    /// <para>
    /// &#160;&#160;indexedTermsPerValue = <b>ceil</b><big>(</big>bitsPerValue / precisionStep<big>)</big>
    /// </para>
    /// As the lower precision terms are shared by many values, the additional terms only
    /// slightly grow the term dictionary (approx. 7% for <c>precisionStep=4</c>), but have a larger
    /// impact on the postings (the postings file will have  more entries, as every document is linked to
    /// <c>indexedTermsPerValue</c> terms instead of one). The formula to estimate the growth
    /// of the term dictionary in comparison to one term per value:
    /// <para>
    /// <!-- the formula in the alt attribute was transformed from latex to PNG with http://1.618034.com/latex.php (with 110 dpi): -->
    /// &#160;&#160;<img src="doc-files/nrq-formula-1.png" alt="\mathrm{termDictOverhead} = \sum\limits_{i=0}^{\mathrm{indexedTermsPerValue}-1} \frac{1}{2^{\mathrm{precisionStep}\cdot i}}" />
    /// </para>
    /// <para>On the other hand, if the <see cref="precisionStep"/> is smaller, the maximum number of terms to match reduces,
    /// which optimizes query speed. The formula to calculate the maximum number of terms that will be visited while
    /// executing the query is:
    /// </para>
    /// <para>
    /// <!-- the formula in the alt attribute was transformed from latex to PNG with http://1.618034.com/latex.php (with 110 dpi): -->
    /// &#160;&#160;<img src="doc-files/nrq-formula-2.png" alt="\mathrm{maxQueryTerms} = \left[ \left( \mathrm{indexedTermsPerValue} - 1 \right) \cdot \left(2^\mathrm{precisionStep} - 1 \right) \cdot 2 \right] + \left( 2^\mathrm{precisionStep} - 1 \right)" />
    /// </para>
    /// <para>For longs stored using a precision step of 4, <c>maxQueryTerms = 15*15*2 + 15 = 465</c>, and for a precision
    /// step of 2, <c>maxQueryTerms = 31*3*2 + 3 = 189</c>. But the faster search speed is reduced by more seeking
    /// in the term enum of the index. Because of this, the ideal <see cref="precisionStep"/> value can only
    /// be found out by testing. <b>Important:</b> You can index with a lower precision step value and test search speed
    /// using a multiple of the original step value.</para>
    ///
    /// <para>Good values for <see cref="precisionStep"/> are depending on usage and data type:</para>
    /// <list type="bullet">
    ///  <item><description>The default for all data types is <b>4</b>, which is used, when no <code>precisionStep</code> is given.</description></item>
    ///  <item><description>Ideal value in most cases for <em>64 bit</em> data types <em>(long, double)</em> is <b>6</b> or <b>8</b>.</description></item>
    ///  <item><description>Ideal value in most cases for <em>32 bit</em> data types <em>(int, float)</em> is <b>4</b>.</description></item>
    ///  <item><description>For low cardinality fields larger precision steps are good. If the cardinality is &lt; 100, it is
    ///  fair to use <see cref="int.MaxValue"/> (see below).</description></item>
    ///  <item><description>Steps <b>&gt;=64</b> for <em>long/double</em> and <b>&gt;=32</b> for <em>int/float</em> produces one token
    ///  per value in the index and querying is as slow as a conventional <see cref="TermRangeQuery"/>. But it can be used
    ///  to produce fields, that are solely used for sorting (in this case simply use <see cref="int.MaxValue"/> as
    ///  <see cref="precisionStep"/>). Using <see cref="Int32Field"/>,
    ///  <see cref="Int64Field"/>, <see cref="SingleField"/> or <see cref="DoubleField"/> for sorting
    ///  is ideal, because building the field cache is much faster than with text-only numbers.
    ///  These fields have one term per value and therefore also work with term enumeration for building distinct lists
    ///  (e.g. facets / preselected values to search for).
    ///  Sorting is also possible with range query optimized fields using one of the above <see cref="precisionStep"/>s.</description></item>
    /// </list>
    ///
    /// <para>Comparisons of the different types of RangeQueries on an index with about 500,000 docs showed
    /// that <see cref="TermRangeQuery"/> in boolean rewrite mode (with raised <see cref="BooleanQuery"/> clause count)
    /// took about 30-40 secs to complete, <see cref="TermRangeQuery"/> in constant score filter rewrite mode took 5 secs
    /// and executing this class took &lt;100ms to complete (on an Opteron64 machine, Java 1.5, 8 bit
    /// precision step). This query type was developed for a geographic portal, where the performance for
    /// e.g. bounding boxes or exact date/time stamps is important.</para>
    ///
    /// @since 2.9
    /// </summary>
    public sealed class NumericRangeQuery<T> : MultiTermQuery
        where T : struct, IComparable<T> // best equiv constraint for java's number class
    {
        internal NumericRangeQuery(string field, int precisionStep, NumericType dataType, T? min, T? max, bool minInclusive, bool maxInclusive)
            : base(field)
        {
            if (precisionStep < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(precisionStep), "precisionStep must be >=1"); // LUCENENET specific - changed from IllegalArgumentException to ArgumentOutOfRangeException (.NET convention)
            }
            this.precisionStep = precisionStep;
            this.dataType = dataType;
            this.min = min;
            this.max = max;
            this.minInclusive = minInclusive;
            this.maxInclusive = maxInclusive;
        }

        // LUCENENET NOTE: Static methods were moved into the NumericRangeQuery class

        protected override TermsEnum GetTermsEnum(Terms terms, AttributeSource atts)
        {
            // very strange: java.lang.Number itself is not Comparable, but all subclasses used here are
            if (min.HasValue && max.HasValue && (min.Value).CompareTo(max.Value) > 0)
            {
                return TermsEnum.EMPTY;
            }
            return new NumericRangeTermsEnum(this, terms.GetEnumerator());
        }

        /// <summary>
        /// Returns <c>true</c> if the lower endpoint is inclusive </summary>
        public bool IncludesMin => minInclusive;

        /// <summary>
        /// Returns <c>true</c> if the upper endpoint is inclusive </summary>
        public bool IncludesMax => maxInclusive;

        /// <summary>
        /// Returns the lower value of this range query </summary>
        public T? Min => min;

        /// <summary>
        /// Returns the upper value of this range query </summary>
        public T? Max => max;

        /// <summary>
        /// Returns the precision step. </summary>
        public int PrecisionStep => precisionStep;

        public override string ToString(string field)
        {
            StringBuilder sb = new StringBuilder();
            if (!Field.Equals(field, StringComparison.Ordinal))
            {
                sb.Append(Field).Append(':');
            }
            return sb.Append(minInclusive ? '[' : '{').Append((min is null) ? "*" : min.ToString()).Append(" TO ").Append((max is null) ? "*" : max.ToString()).Append(maxInclusive ? ']' : '}').Append(ToStringUtils.Boost(Boost)).ToString();
        }

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
            if (o is NumericRangeQuery<T> q)
            {
                return
                    // LUCENENET specific - use Nullable.Equals to avoid boxing.
                    Nullable.Equals(q.min, min)
                    && Nullable.Equals(q.max, max)
                    && minInclusive == q.minInclusive
                    && maxInclusive == q.maxInclusive
                    && precisionStep == q.precisionStep;
            }
            return false;
        }

        public override int GetHashCode()
        {
            int hash = base.GetHashCode();
            hash += precisionStep ^ 0x64365465;
            if (min != null)
            {
                hash += min.GetHashCode() ^ 0x14fa55fb;
            }
            if (max != null)
            {
                hash += max.GetHashCode() ^ 0x733fa5fe;
            }
            return hash + (minInclusive.GetHashCode() ^ 0x14fa55fb) + (maxInclusive.GetHashCode() ^ 0x733fa5fe);
        }

        // members (package private, to be also fast accessible by NumericRangeTermEnum)
        internal readonly int precisionStep;

        internal readonly NumericType dataType;
        internal readonly T? min, max;
        internal readonly bool minInclusive, maxInclusive;

        // used to handle float/double infinity correcty
        /// <summary>
        /// NOTE: This was LONG_NEGATIVE_INFINITY in Lucene
        /// </summary>
        internal static readonly long INT64_NEGATIVE_INFINITY = NumericUtils.DoubleToSortableInt64(double.NegativeInfinity);

        /// <summary>
        /// NOTE: This was LONG_NEGATIVE_INFINITY in Lucene
        /// </summary>
        internal static readonly long INT64_POSITIVE_INFINITY = NumericUtils.DoubleToSortableInt64(double.PositiveInfinity);

        /// <summary>
        /// NOTE: This was INT_NEGATIVE_INFINITY in Lucene
        /// </summary>
        internal static readonly int INT32_NEGATIVE_INFINITY = NumericUtils.SingleToSortableInt32(float.NegativeInfinity);

        /// <summary>
        /// NOTE: This was INT_POSITIVE_INFINITY in Lucene
        /// </summary>
        internal static readonly int INT32_POSITIVE_INFINITY = NumericUtils.SingleToSortableInt32(float.PositiveInfinity);

        /// <summary>
        /// Subclass of <see cref="FilteredTermsEnum"/> for enumerating all terms that match the
        /// sub-ranges for trie range queries, using flex API.
        /// <para/>
        /// WARNING: this term enumeration is not guaranteed to be always ordered by
        /// <see cref="Index.Term.CompareTo(Index.Term)"/>.
        /// The ordering depends on how <see cref="NumericUtils.SplitInt64Range(NumericUtils.Int64RangeBuilder, int, long, long)"/> and
        /// <see cref="NumericUtils.SplitInt32Range(NumericUtils.Int32RangeBuilder, int, int, int)"/> generates the sub-ranges. For
        /// <see cref="MultiTermQuery"/> ordering is not relevant.
        /// </summary>
        private sealed class NumericRangeTermsEnum : FilteredTermsEnum
        {
            private readonly NumericRangeQuery<T> outerInstance;

            internal BytesRef currentLowerBound, currentUpperBound;

            internal readonly Queue<BytesRef> rangeBounds = new Queue<BytesRef>();
            internal readonly IComparer<BytesRef> termComp;

            internal NumericRangeTermsEnum(NumericRangeQuery<T> outerInstance, TermsEnum tenum)
                : base(tenum)
            {
                this.outerInstance = outerInstance;
                switch (this.outerInstance.dataType)
                {
                    case NumericType.INT64:
                    case NumericType.DOUBLE:
                        {
                            // lower
                            long minBound;
                            if (this.outerInstance.dataType == NumericType.INT64)
                            {
                                minBound = (!this.outerInstance.min.HasValue) ? long.MinValue : CastTo<long>.From(this.outerInstance.min.Value);
                            }
                            else
                            {
                                if (Debugging.AssertsEnabled) Debugging.Assert(this.outerInstance.dataType == NumericType.DOUBLE);
                                minBound = (!this.outerInstance.min.HasValue) ? INT64_NEGATIVE_INFINITY
                                    : NumericUtils.DoubleToSortableInt64(CastTo<double>.From(this.outerInstance.min.Value));
                            }
                            if (!this.outerInstance.minInclusive && this.outerInstance.min != null)
                            {
                                if (minBound == long.MaxValue)
                                {
                                    break;
                                }
                                minBound++;
                            }

                            // upper
                            long maxBound;
                            if (this.outerInstance.dataType == NumericType.INT64)
                            {
                                maxBound = (!this.outerInstance.max.HasValue) ? long.MaxValue : CastTo<long>.From(this.outerInstance.max.Value);
                            }
                            else
                            {
                                if (Debugging.AssertsEnabled) Debugging.Assert(this.outerInstance.dataType == NumericType.DOUBLE);
                                maxBound = (!this.outerInstance.max.HasValue) ? INT64_POSITIVE_INFINITY
                                    : NumericUtils.DoubleToSortableInt64(CastTo<double>.From(this.outerInstance.max.Value));
                            }
                            if (!this.outerInstance.maxInclusive && this.outerInstance.max != null)
                            {
                                if (maxBound == long.MinValue)
                                {
                                    break;
                                }
                                maxBound--;
                            }

                            NumericUtils.SplitInt64Range(new Int64RangeBuilderAnonymousClass(this), this.outerInstance.precisionStep, minBound, maxBound);
                            break;
                        }

                    case NumericType.INT32:
                    case NumericType.SINGLE:
                        {
                            // lower
                            int minBound;
                            if (this.outerInstance.dataType == NumericType.INT32)
                            {
                                minBound = (!this.outerInstance.min.HasValue) ? int.MinValue : CastTo<int>.From(this.outerInstance.min.Value);
                            }
                            else
                            {
                                if (Debugging.AssertsEnabled) Debugging.Assert(this.outerInstance.dataType == NumericType.SINGLE);
                                minBound = (!this.outerInstance.min.HasValue) ? INT32_NEGATIVE_INFINITY
                                    : NumericUtils.SingleToSortableInt32(CastTo<float>.From(this.outerInstance.min.Value));
                            }
                            if (!this.outerInstance.minInclusive && this.outerInstance.min != null)
                            {
                                if (minBound == int.MaxValue)
                                {
                                    break;
                                }
                                minBound++;
                            }

                            // upper
                            int maxBound;
                            if (this.outerInstance.dataType == NumericType.INT32)
                            {
                                maxBound = (!this.outerInstance.max.HasValue) ? int.MaxValue : CastTo<int>.From(this.outerInstance.max.Value);
                            }
                            else
                            {
                                if (Debugging.AssertsEnabled) Debugging.Assert(this.outerInstance.dataType == NumericType.SINGLE);
                                maxBound = (!this.outerInstance.max.HasValue) ? INT32_POSITIVE_INFINITY
                                    : NumericUtils.SingleToSortableInt32(CastTo<float>.From(this.outerInstance.max.Value));
                            }
                            if (!this.outerInstance.maxInclusive && this.outerInstance.max != null)
                            {
                                if (maxBound == int.MinValue)
                                {
                                    break;
                                }
                                maxBound--;
                            }

                            NumericUtils.SplitInt32Range(new Int32RangeBuilderAnonymousClass(this), this.outerInstance.precisionStep, minBound, maxBound);
                            break;
                        }

                    default:
                        // should never happen
                        throw new ArgumentException("Invalid NumericType");
                }

                termComp = Comparer;
            }

            private sealed class Int64RangeBuilderAnonymousClass : NumericUtils.Int64RangeBuilder
            {
                private readonly NumericRangeTermsEnum outerInstance;

                public Int64RangeBuilderAnonymousClass(NumericRangeTermsEnum outerInstance)
                {
                    this.outerInstance = outerInstance;
                }

                public override sealed void AddRange(BytesRef minPrefixCoded, BytesRef maxPrefixCoded)
                {
                    outerInstance.rangeBounds.Enqueue(minPrefixCoded);
                    outerInstance.rangeBounds.Enqueue(maxPrefixCoded);
                }
            }

            private sealed class Int32RangeBuilderAnonymousClass : NumericUtils.Int32RangeBuilder
            {
                private readonly NumericRangeTermsEnum outerInstance;

                public Int32RangeBuilderAnonymousClass(NumericRangeTermsEnum outerInstance)
                {
                    this.outerInstance = outerInstance;
                }

                public override sealed void AddRange(BytesRef minPrefixCoded, BytesRef maxPrefixCoded)
                {
                    outerInstance.rangeBounds.Enqueue(minPrefixCoded);
                    outerInstance.rangeBounds.Enqueue(maxPrefixCoded);
                }
            }

            private void NextRange()
            {
                if (Debugging.AssertsEnabled) Debugging.Assert(rangeBounds.Count % 2 == 0);

                currentLowerBound = rangeBounds.Dequeue();
                if (Debugging.AssertsEnabled) Debugging.Assert(currentUpperBound is null || termComp.Compare(currentUpperBound, currentLowerBound) <= 0, "The current upper bound must be <= the new lower bound");

                currentUpperBound = rangeBounds.Dequeue();
            }

            protected override sealed BytesRef NextSeekTerm(BytesRef term)
            {
                while (rangeBounds.Count >= 2)
                {
                    NextRange();

                    // if the new upper bound is before the term parameter, the sub-range is never a hit
                    if (term != null && termComp.Compare(term, currentUpperBound) > 0)
                    {
                        continue;
                    }
                    // never seek backwards, so use current term if lower bound is smaller
                    return (term != null && termComp.Compare(term, currentLowerBound) > 0) ? term : currentLowerBound;
                }

                // no more sub-range enums available
                if (Debugging.AssertsEnabled) Debugging.Assert(rangeBounds.Count == 0);
                currentLowerBound = currentUpperBound = null;
                return null;
            }

            protected override sealed AcceptStatus Accept(BytesRef term)
            {
                while (currentUpperBound is null || termComp.Compare(term, currentUpperBound) > 0)
                {
                    if (rangeBounds.Count == 0)
                    {
                        return AcceptStatus.END;
                    }
                    // peek next sub-range, only seek if the current term is smaller than next lower bound
                    if (termComp.Compare(term, rangeBounds.Peek()) < 0)
                    {
                        return AcceptStatus.NO_AND_SEEK;
                    }
                    // step forward to next range without seeking, as next lower range bound is less or equal current term
                    NextRange();
                }
                return AcceptStatus.YES;
            }
        }
    }

    /// <summary>
    /// LUCENENET specific class to provide access to static factory metods of <see cref="NumericRangeQuery{T}"/>
    /// without referring to its genereic closing type.
    /// </summary>
    public static class NumericRangeQuery
    {
        /// <summary>
        /// Factory that creates a <see cref="NumericRangeQuery{T}"/>, that queries a <see cref="long"/>
        /// range using the given <a href="#precisionStepDesc"><see cref="NumericRangeQuery{T}.precisionStep"/></a>.
        /// You can have half-open ranges (which are in fact &lt;/&lt;= or &gt;/&gt;= queries)
        /// by setting the min or max value to <c>null</c>. By setting inclusive to <c>false</c>, it will
        /// match all documents excluding the bounds, with inclusive on, the boundaries are hits, too.
        /// <para/>
        /// NOTE: This was newLongRange() in Lucene
        /// </summary>
        public static NumericRangeQuery<long> NewInt64Range(string field, int precisionStep, long? min, long? max, bool minInclusive, bool maxInclusive)
        {
            return new NumericRangeQuery<long>(field, precisionStep, NumericType.INT64, min, max, minInclusive, maxInclusive);
        }

        /// <summary>
        /// Factory that creates a <see cref="NumericRangeQuery{T}"/>, that queries a <see cref="long"/>
        /// range using the default <see cref="NumericRangeQuery{T}.precisionStep"/> <see cref="NumericUtils.PRECISION_STEP_DEFAULT"/> (4).
        /// You can have half-open ranges (which are in fact &lt;/&lt;= or &gt;/&gt;= queries)
        /// by setting the min or max value to <c>null</c>. By setting inclusive to <c>false</c>, it will
        /// match all documents excluding the bounds, with inclusive on, the boundaries are hits, too.
        /// <para/>
        /// NOTE: This was newLongRange() in Lucene
        /// </summary>
        public static NumericRangeQuery<long> NewInt64Range(string field, long? min, long? max, bool minInclusive, bool maxInclusive)
        {
            return new NumericRangeQuery<long>(field, NumericUtils.PRECISION_STEP_DEFAULT, NumericType.INT64, min, max, minInclusive, maxInclusive);
        }

        /// <summary>
        /// Factory that creates a <see cref="NumericRangeQuery{T}"/>, that queries a <see cref="int"/>
        /// range using the given <a href="#precisionStepDesc"><see cref="NumericRangeQuery{T}.precisionStep"/></a>.
        /// You can have half-open ranges (which are in fact &lt;/&lt;= or &gt;/&gt;= queries)
        /// by setting the min or max value to <c>null</c>. By setting inclusive to <c>false</c>, it will
        /// match all documents excluding the bounds, with inclusive on, the boundaries are hits, too.
        /// <para/>
        /// NOTE: This was newIntRange() in Lucene
        /// </summary>
        public static NumericRangeQuery<int> NewInt32Range(string field, int precisionStep, int? min, int? max, bool minInclusive, bool maxInclusive)
        {
            return new NumericRangeQuery<int>(field, precisionStep, NumericType.INT32, min, max, minInclusive, maxInclusive);
        }

        /// <summary>
        /// Factory that creates a <see cref="NumericRangeQuery{T}"/>, that queries a <see cref="int"/>
        /// range using the default <see cref="NumericRangeQuery{T}.precisionStep"/> <see cref="NumericUtils.PRECISION_STEP_DEFAULT"/> (4).
        /// You can have half-open ranges (which are in fact &lt;/&lt;= or &gt;/&gt;= queries)
        /// by setting the min or max value to <c>null</c>. By setting inclusive to <c>false</c>, it will
        /// match all documents excluding the bounds, with inclusive on, the boundaries are hits, too.
        /// <para/>
        /// NOTE: This was newIntRange() in Lucene
        /// </summary>
        public static NumericRangeQuery<int> NewInt32Range(string field, int? min, int? max, bool minInclusive, bool maxInclusive)
        {
            return new NumericRangeQuery<int>(field, NumericUtils.PRECISION_STEP_DEFAULT, NumericType.INT32, min, max, minInclusive, maxInclusive);
        }

        /// <summary>
        /// Factory that creates a <see cref="NumericRangeQuery{T}"/>, that queries a <see cref="double"/>
        /// range using the given <a href="#precisionStepDesc"><see cref="NumericRangeQuery{T}.precisionStep"/></a>.
        /// You can have half-open ranges (which are in fact &lt;/&lt;= or &gt;/&gt;= queries)
        /// by setting the min or max value to <c>null</c>.
        /// <see cref="double.NaN"/> will never match a half-open range, to hit <c>NaN</c> use a query
        /// with <c>min == max == System.Double.NaN</c>.  By setting inclusive to <c>false</c>, it will
        /// match all documents excluding the bounds, with inclusive on, the boundaries are hits, too.
        /// </summary>
        public static NumericRangeQuery<double> NewDoubleRange(string field, int precisionStep, double? min, double? max, bool minInclusive, bool maxInclusive)
        {
            return new NumericRangeQuery<double>(field, precisionStep, NumericType.DOUBLE, min, max, minInclusive, maxInclusive);
        }

        /// <summary>
        /// Factory that creates a <see cref="NumericRangeQuery{T}"/>, that queries a <see cref="double"/>
        /// range using the default <see cref="NumericRangeQuery{T}.precisionStep"/> <see cref="NumericUtils.PRECISION_STEP_DEFAULT"/> (4).
        /// You can have half-open ranges (which are in fact &lt;/&lt;= or &gt;/&gt;= queries)
        /// by setting the min or max value to <c>null</c>.
        /// <see cref="double.NaN"/> will never match a half-open range, to hit <c>NaN</c> use a query
        /// with <c>min == max == System.Double.NaN</c>.  By setting inclusive to <c>false</c>, it will
        /// match all documents excluding the bounds, with inclusive on, the boundaries are hits, too.
        /// </summary>
        public static NumericRangeQuery<double> NewDoubleRange(string field, double? min, double? max, bool minInclusive, bool maxInclusive)
        {
            return new NumericRangeQuery<double>(field, NumericUtils.PRECISION_STEP_DEFAULT, NumericType.DOUBLE, min, max, minInclusive, maxInclusive);
        }

        /// <summary>
        /// Factory that creates a <see cref="NumericRangeQuery{T}"/>, that queries a <see cref="float"/>
        /// range using the given <a href="#precisionStepDesc"><see cref="NumericRangeQuery{T}.precisionStep"/></a>.
        /// You can have half-open ranges (which are in fact &lt;/&lt;= or &gt;/&gt;= queries)
        /// by setting the min or max value to <c>null</c>.
        /// <see cref="float.NaN"/> will never match a half-open range, to hit <c>NaN</c> use a query
        /// with <c>min == max == System.Single.NaN</c>.  By setting inclusive to <c>false</c>, it will
        /// match all documents excluding the bounds, with inclusive on, the boundaries are hits, too.
        /// <para/>
        /// NOTE: This was newFloatRange() in Lucene
        /// </summary>
        public static NumericRangeQuery<float> NewSingleRange(string field, int precisionStep, float? min, float? max, bool minInclusive, bool maxInclusive)
        {
            return new NumericRangeQuery<float>(field, precisionStep, NumericType.SINGLE, min, max, minInclusive, maxInclusive);
        }

        /// <summary>
        /// Factory that creates a <see cref="NumericRangeQuery{T}"/>, that queries a <see cref="float"/>
        /// range using the default <see cref="NumericRangeQuery{T}.precisionStep"/> <see cref="NumericUtils.PRECISION_STEP_DEFAULT"/> (4).
        /// You can have half-open ranges (which are in fact &lt;/&lt;= or &gt;/&gt;= queries)
        /// by setting the min or max value to <c>null</c>.
        /// <see cref="float.NaN"/> will never match a half-open range, to hit <c>NaN</c> use a query
        /// with <c>min == max == System.Single.NaN</c>.  By setting inclusive to <c>false</c>, it will
        /// match all documents excluding the bounds, with inclusive on, the boundaries are hits, too.
        /// <para/>
        /// NOTE: This was newFloatRange() in Lucene
        /// </summary>
        public static NumericRangeQuery<float> NewSingleRange(string field, float? min, float? max, bool minInclusive, bool maxInclusive)
        {
            return new NumericRangeQuery<float>(field, NumericUtils.PRECISION_STEP_DEFAULT, NumericType.SINGLE, min, max, minInclusive, maxInclusive);
        }
    }
}