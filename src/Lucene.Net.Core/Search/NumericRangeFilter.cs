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

    /// <summary>
    /// A <seealso cref="Filter"/> that only accepts numeric values within
    /// a specified range. To use this, you must first index the
    /// numeric values using <seealso cref="IntField"/>, {@link
    /// FloatField}, <seealso cref="LongField"/> or <seealso cref="DoubleField"/> (expert: {@link
    /// NumericTokenStream}).
    ///
    /// <p>You create a new NumericRangeFilter with the static
    /// factory methods, eg:
    ///
    /// <pre class="prettyprint">
    /// Filter f = NumericRangeFilter.newFloatRange("weight", 0.03f, 0.10f, true, true);
    /// </pre>
    ///
    /// accepts all documents whose float valued "weight" field
    /// ranges from 0.03 to 0.10, inclusive.
    /// See <seealso cref="NumericRangeQuery"/> for details on how Lucene
    /// indexes and searches numeric valued fields.
    ///
    /// @since 2.9
    ///
    /// </summary>
    public sealed class NumericRangeFilter<T> : MultiTermQueryWrapperFilter<NumericRangeQuery<T>>
        where T : struct, IComparable<T>
    // real numbers in C# are structs and IComparable with themselves, best constraint we have
    {
        internal NumericRangeFilter(NumericRangeQuery<T> query)
            : base(query)
        {
        }

        // LUCENENET NOTE: Static methods were moved into NumericRangeFilter class

        /// <summary>
        /// Returns <code>true</code> if the lower endpoint is inclusive </summary>
        public bool IncludesMin
        {
            get { return Query.IncludesMin; }
        }

        /// <summary>
        /// Returns <code>true</code> if the upper endpoint is inclusive </summary>
        public bool IncludesMax
        {
            get { return Query.IncludesMax; }
        }

        /// <summary>
        /// Returns the lower value of this range filter </summary>
        public T? Min
        {
            get
            {
                return Query.Min;
            }
        }

        /// <summary>
        /// Returns the upper value of this range filter </summary>
        public T? Max
        {
            get
            {
                return Query.Max;
            }
        }

        /// <summary>
        /// Returns the precision step. </summary>
        public int PrecisionStep
        {
            get
            {
                return Query.PrecisionStep;
            }
        }
    }

    public static class NumericRangeFilter
    {
        /// <summary>
        /// Factory that creates a <code>NumericRangeFilter</code>, that filters a <code>long</code>
        /// range using the given <a href="NumericRangeQuery.html#precisionStepDesc"><code>precisionStep</code></a>.
        /// You can have half-open ranges (which are in fact &lt;/&lt;= or &gt;/&gt;= queries)
        /// by setting the min or max value to <code>null</code>. By setting inclusive to false, it will
        /// match all documents excluding the bounds, with inclusive on, the boundaries are hits, too.
        /// </summary>
         // LUCENENET TODO: Rename NewInt64Range
        public static NumericRangeFilter<long> NewLongRange(string field, int precisionStep, long? min, long? max, bool minInclusive, bool maxInclusive)
        {
            return new NumericRangeFilter<long>(NumericRangeQuery.NewLongRange(field, precisionStep, min, max, minInclusive, maxInclusive));
        }

        /// <summary>
        /// Factory that creates a <code>NumericRangeFilter</code>, that queries a <code>long</code>
        /// range using the default <code>precisionStep</code> <seealso cref="NumericUtils#PRECISION_STEP_DEFAULT"/> (4).
        /// You can have half-open ranges (which are in fact &lt;/&lt;= or &gt;/&gt;= queries)
        /// by setting the min or max value to <code>null</code>. By setting inclusive to false, it will
        /// match all documents excluding the bounds, with inclusive on, the boundaries are hits, too.
        /// </summary>
         // LUCENENET TODO: Rename NewInt64Range
        public static NumericRangeFilter<long> NewLongRange(string field, long? min, long? max, bool minInclusive, bool maxInclusive)
        {
            return new NumericRangeFilter<long>(NumericRangeQuery.NewLongRange(field, min, max, minInclusive, maxInclusive));
        }

        /// <summary>
        /// Factory that creates a <code>NumericRangeFilter</code>, that filters a <code>int</code>
        /// range using the given <a href="NumericRangeQuery.html#precisionStepDesc"><code>precisionStep</code></a>.
        /// You can have half-open ranges (which are in fact &lt;/&lt;= or &gt;/&gt;= queries)
        /// by setting the min or max value to <code>null</code>. By setting inclusive to false, it will
        /// match all documents excluding the bounds, with inclusive on, the boundaries are hits, too.
        /// </summary>
         // LUCENENET TODO: Rename NewInt32Range
        public static NumericRangeFilter<int> NewIntRange(string field, int precisionStep, int? min, int? max, bool minInclusive, bool maxInclusive)
        {
            return new NumericRangeFilter<int>(NumericRangeQuery.NewIntRange(field, precisionStep, min, max, minInclusive, maxInclusive));
        }

        /// <summary>
        /// Factory that creates a <code>NumericRangeFilter</code>, that queries a <code>int</code>
        /// range using the default <code>precisionStep</code> <seealso cref="NumericUtils#PRECISION_STEP_DEFAULT"/> (4).
        /// You can have half-open ranges (which are in fact &lt;/&lt;= or &gt;/&gt;= queries)
        /// by setting the min or max value to <code>null</code>. By setting inclusive to false, it will
        /// match all documents excluding the bounds, with inclusive on, the boundaries are hits, too.
        /// </summary>
         // LUCENENET TODO: Rename NewInt32Range
        public static NumericRangeFilter<int> NewIntRange(string field, int? min, int? max, bool minInclusive, bool maxInclusive)
        {
            return new NumericRangeFilter<int>(NumericRangeQuery.NewIntRange(field, min, max, minInclusive, maxInclusive));
        }

        /// <summary>
        /// Factory that creates a <code>NumericRangeFilter</code>, that filters a <code>double</code>
        /// range using the given <a href="NumericRangeQuery.html#precisionStepDesc"><code>precisionStep</code></a>.
        /// You can have half-open ranges (which are in fact &lt;/&lt;= or &gt;/&gt;= queries)
        /// by setting the min or max value to <code>null</code>.
        /// <seealso cref="Double#NaN"/> will never match a half-open range, to hit {@code NaN} use a query
        /// with {@code min == max == Double.NaN}. By setting inclusive to false, it will
        /// match all documents excluding the bounds, with inclusive on, the boundaries are hits, too.
        /// </summary>
        public static NumericRangeFilter<double> NewDoubleRange(string field, int precisionStep, double? min, double? max, bool minInclusive, bool maxInclusive)
        {
            return new NumericRangeFilter<double>(NumericRangeQuery.NewDoubleRange(field, precisionStep, min, max, minInclusive, maxInclusive));
        }

        /// <summary>
        /// Factory that creates a <code>NumericRangeFilter</code>, that queries a <code>double</code>
        /// range using the default <code>precisionStep</code> <seealso cref="NumericUtils#PRECISION_STEP_DEFAULT"/> (4).
        /// You can have half-open ranges (which are in fact &lt;/&lt;= or &gt;/&gt;= queries)
        /// by setting the min or max value to <code>null</code>.
        /// <seealso cref="Double#NaN"/> will never match a half-open range, to hit {@code NaN} use a query
        /// with {@code min == max == Double.NaN}. By setting inclusive to false, it will
        /// match all documents excluding the bounds, with inclusive on, the boundaries are hits, too.
        /// </summary>
        public static NumericRangeFilter<double> NewDoubleRange(string field, double? min, double? max, bool minInclusive, bool maxInclusive)
        {
            return new NumericRangeFilter<double>(NumericRangeQuery.NewDoubleRange(field, min, max, minInclusive, maxInclusive));
        }

        /// <summary>
        /// Factory that creates a <code>NumericRangeFilter</code>, that filters a <code>float</code>
        /// range using the given <a href="NumericRangeQuery.html#precisionStepDesc"><code>precisionStep</code></a>.
        /// You can have half-open ranges (which are in fact &lt;/&lt;= or &gt;/&gt;= queries)
        /// by setting the min or max value to <code>null</code>.
        /// <seealso cref="Float#NaN"/> will never match a half-open range, to hit {@code NaN} use a query
        /// with {@code min == max == Float.NaN}. By setting inclusive to false, it will
        /// match all documents excluding the bounds, with inclusive on, the boundaries are hits, too.
        /// </summary>
         // LUCENENET TODO: Rename NewSingleRange
        public static NumericRangeFilter<float> NewFloatRange(string field, int precisionStep, float? min, float? max, bool minInclusive, bool maxInclusive)
        {
            return new NumericRangeFilter<float>(NumericRangeQuery.NewFloatRange(field, precisionStep, min, max, minInclusive, maxInclusive));
        }

        /// <summary>
        /// Factory that creates a <code>NumericRangeFilter</code>, that queries a <code>float</code>
        /// range using the default <code>precisionStep</code> <seealso cref="NumericUtils#PRECISION_STEP_DEFAULT"/> (4).
        /// You can have half-open ranges (which are in fact &lt;/&lt;= or &gt;/&gt;= queries)
        /// by setting the min or max value to <code>null</code>.
        /// <seealso cref="Float#NaN"/> will never match a half-open range, to hit {@code NaN} use a query
        /// with {@code min == max == Float.NaN}. By setting inclusive to false, it will
        /// match all documents excluding the bounds, with inclusive on, the boundaries are hits, too.
        /// </summary>
         // LUCENENET TODO: Rename NewSingleRange
        public static NumericRangeFilter<float> NewFloatRange(string field, float? min, float? max, bool minInclusive, bool maxInclusive)
        {
            return new NumericRangeFilter<float>(NumericRangeQuery.NewFloatRange(field, min, max, minInclusive, maxInclusive));
        }
    }
}